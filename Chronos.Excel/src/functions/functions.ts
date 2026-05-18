import {Mutation, Query, SingleQuery, getIdOrError, MutationWithId} from './queries';
import {v4 as uuidv4} from 'uuid';
import * as webpack from "webpack";

/**
 * @customfunction
 * @param invocation Custom function invocation
 * @requiresAddress
 */
export async function guid(invocation: CustomFunctions.Invocation) : Promise<any> {

  let guid = uuidv4();
  freezeGuidFormula(invocation, guid);
  return guid;
}

function freezeGuidFormula(invocation: CustomFunctions.Invocation, guid: string) {
  if (!invocation || !invocation.address || (invocation as any).isInValuePreview) {
    return;
  }

  setTimeout(async () => {
    try {
      await Excel.run(async context => {
        const range = getRangeFromAddress(context, invocation.address);
        range.values = [[guid]];
        await context.sync();
      });
    }
    catch (error) {
      console.error(error);
    }
  }, 0);
}

function getRangeFromAddress(context: Excel.RequestContext, address: string): Excel.Range {
  const separator = address.lastIndexOf("!");
  if (separator < 0) {
    return context.workbook.worksheets.getActiveWorksheet().getRange(address);
  }

  const worksheetName = address.substring(0, separator).replace(/^'|'$/g, "").replace(/''/g, "'");
  const rangeAddress = address.substring(separator + 1);
  return context.workbook.worksheets.getItem(worksheetName).getRange(rangeAddress);
}

function OptionalString(value : string) : string | null {
  if (value == '' || value === null || value === undefined)
    return null;
  
  return `"${value}"`;
}

function OptionalExcelNumber(value: any): number {
  if (value === null || value === undefined || value === "")
    return null;

  return Number(value);
}

function ExcelDateToISO( serial : number ) : string {
  let date = ExcelDateToJSDate(serial);
  if ( date == null )
    return null;
  return `"${date.toISOString()}"`
}

function ExcelDateToJSDate (serial : number) : Date {
  if (serial == null)
    return null
  var utc_days  = Math.floor(serial - 25569);
  var utc_value = utc_days * 86400;
  var date_info = new Date(utc_value * 1000);

  var fractional_day = serial - Math.floor(serial) + 0.0000001;

  var total_seconds = Math.floor(86400 * fractional_day);

  var seconds = total_seconds % 60;

  total_seconds -= seconds;

  var hours = Math.floor(total_seconds / (60 * 60));
  var minutes = Math.floor(total_seconds / 60) % 60;

  return new Date(date_info.getUTCFullYear(), date_info.getUTCMonth(), date_info.getUTCDate(), hours, minutes, seconds);
}

function JSDateToExcelDate(date : Date) : number {
  let converted = 25569.0 + ((date.getTime() - (date.getTimezoneOffset() * 60 * 1000)) / (1000 * 60 * 60 * 24));
  return converted
}

function CleanNumber(value : number, decimals : number = 10) : number {
  if(value === null || value === undefined)
    return 0

  const threshold = Math.pow(10, -decimals)
  if(Math.abs(value) < threshold)
    return 0

  const factor = Math.pow(10, decimals)
  return Math.round(value * factor) / factor
}

function CleanMoney(value : number) : number {
  return CleanNumber(value, 8)
}

function SerialisedTimeToExcelDate(value : string) : number {
  if(value === null || value === undefined || value === "")
    return null

  const ticks = value.toString().split(';')[0]
  if(/^-?\d+$/.test(ticks)) {
    const milliseconds = Number(ticks) / 10000
    return JSDateToExcelDate(new Date(milliseconds))
  }

  return JSDateToExcelDate(new Date(value))
}

function DateTimeToExcelDate(value : string) : number {
  if(value === null || value === undefined || value === "")
    return null

  return JSDateToExcelDate(new Date(value))
}

/**
 * @customfunction
 * @param account Account name
 * @param denominator Denominator asset
 * @param date Date to update
 */
export async function updateQuotes(account: string, denominator: string, date?: number)
{
  const mutation = `mutation {
    updateQuotes( account : "${account}", denominator : "${denominator}", date : ${ExcelDateToISO(date)} )
  }`
  
  let result = await MutationWithId(account, mutation)
  return result
}

/**
 * @customfunction
 * @param forCcy Foreign currency
 * @param domCcy Domestic currency
 */
export async function registerCurrencyPair(forCcy: string, domCcy: string)
{
  const mutation = `mutation {
    registerCurrencyPair( forCcy : "${forCcy}", domCcy : "${domCcy}", supportsIntraday :  )
  }`
  
  let result = await MutationWithId(forCcy, mutation)
  return result
}

/**
 * @customfunction
 * @param forAssetId Foreign asset id
 * @param forAssetType Foreign asset type
 * @param domAssetId Domestic asset id
 * @param domAssetType Domestic asset type
 * @param guid Command guid
 * @param {string} [holidayCalendar] Holiday calendar identifier
 * @param {boolean} [supportsIntraday] Whether this asset pair supports intraday quoting
 * @param {boolean} [useStaleQuotes] Whether stale quotes are permitted for this asset pair
 */
export async function registerAssetPair(forAssetId : string, forAssetType : string, domAssetId : string, domAssetType : string, guid: string, holidayCalendar? : string, supportsIntraday? : boolean, useStaleQuotes? : boolean) : Promise<any>
{
  if(supportsIntraday === undefined || supportsIntraday == null)
    supportsIntraday = true

  if(useStaleQuotes === undefined || useStaleQuotes == null)
    useStaleQuotes = false
  
  const mutation = `mutation {
    registerAssetPair( 
      forAsset : {assetId : "${forAssetId}",
      assetType : ${forAssetType.toUpperCase()}},
      domAsset : {assetId : "${domAssetId}",
      assetType : ${domAssetType.toUpperCase()}},
      guid : "${guid}",
      holidayCalendar : ${OptionalString(holidayCalendar)},
      supportsIntraday : ${supportsIntraday},
      useStaleQuotes : ${useStaleQuotes})
  }`
 
  const fordom = forAssetId + domAssetId;
  
  let result = await SingleQuery(mutation, getIdOrError(fordom, data => data.registerAssetPair.toString()))
  return result;
}

/**
 * @customfunction
 * @param {string} assetId - The unique identifier of the asset for which the stock split is being added.
 * @param {number} ratio - The factor by which the stock is being split.
 * @param {string} [domAssetId] - An optional identifier for the domestic asset related to the stock split.
 * @param {number} [date] - An optional date specifying when the stock split occurs.
 * @param {string} [guid] Command guid
 * @return {Promise<any>} A promise that resolves to the result of the stock split addition operation.
 */
export async function addStockSplit(assetId : string, ratio : number, domAssetId? : string, date? : number, guid? : string)
{
  const mutation = `mutation {
    addStockSplit( assetId: "${assetId}", ratio: ${ratio}, domAssetId : "${domAssetId}", date : ${ExcelDateToISO(date)}, guid : "${guid}" ) 
  }`
  
  let result = await SingleQuery(mutation, getIdOrError(assetId, data => data.addStockSplit.toString()))
  return result;
}

/**
 * @customfunction
 * @param {string} assetId - The unique identifier of the asset for which the ticker is being added.
 * @param {string} ticker - The ticker symbol to associate with the asset.
 * @param {string} [domAssetId] - An optional ID for the domestic asset associated with the ticker.
 * @param {number} [date] - An optional date in Excel date format to associate with the ticker addition.
 * @param {string} [guid] - An optional globally unique identifier (GUID) for tracking the operation.
 */
export async function addQuoteTicker(assetId : string, ticker : string, domAssetId? : string, date? : number, guid? : string)
{
  const mutation = `mutation {
    addQuoteTicker( assetId: "${assetId}", ticker: "${ticker}", domAssetId : "${domAssetId}", date : ${ExcelDateToISO(date)}, guid : "${guid}" ) 
  }`
  
  let result = await SingleQuery(mutation, getIdOrError(assetId, data => data.addQuoteTicker.toString()))
  return result
}

/**
 * @customfunction
 * @param {string} assetId - The ID of the quoted asset.
 * @param {number} close - The closing quote price.
 * @param {number} date - The quote date, represented as an Excel serial date number.
 * @param {string} guid - A unique identifier for the command.
 * @param {string} [domAssetId] - The optional ID of the quote denominator asset.
 * @param {number} [open] - The opening quote price.
 * @param {number} [low] - The low quote price.
 * @param {number} [high] - The high quote price.
 */
export async function addQuote(assetId : string, close : number, date : number, guid : string, domAssetId? : string, open? : number, low? : number, high? : number)
{
  const mutation = `mutation {
    addQuote(
      assetId: "${assetId}",
      domAssetId: ${OptionalString(domAssetId)},
      close: ${close},
      date: ${ExcelDateToISO(date)},
      guid: "${guid}",
      open: ${OptionalExcelNumber(open)},
      low: ${OptionalExcelNumber(low)},
      high: ${OptionalExcelNumber(high)}
    )
  }`

  let result = await SingleQuery(mutation, getIdOrError(assetId, data => data.addQuote.toString()))
  return result
}

/**
 * @customfunction
 * @param name Account name
 * @param amount Amount to deposit
 * @param assetId Asset id
 * @param date Date to deposit
 * @param guid Command guid
 */
export async function depositAsset(name : string, amount : number, assetId : string, date : number, guid : string)
{
  const mutation = `mutation {
    depositAsset( name : "${name}", amount : ${amount}, assetId : "${assetId}", date : ${ExcelDateToISO(date)}, guid : "${guid}" )
  }`
  
  let result = await MutationWithId(name, mutation)
  return result
}

/**
 * @customfunction
 * @param {string} txId - The unique identifier of the transaction.
 * @param {string} fromAccount - The account initiating the transfer.
 * @param {string} toAccount - The account receiving the transfer.
 * @param {number} amount - The amount to be transferred.
 * @param {string} assetId - The identifier of the asset being transferred.
 * @param {number} fee - The fee amount for the transfer.
 * @param {string} feeAssetId - The identifier of the asset used for the fee.
 * @param {number} [date] - The date of the transaction in Excel date format. If not provided, the current date is used.
 */
export async function createTransfer(txId : string, fromAccount : string, toAccount : string, amount : number, assetId : string, fee? : number, feeAssetId? : string, date? : number)
{
  const mutation = `mutation {
    createTransfer(
      txId : "${txId}",
      fromAccount: "${fromAccount}",
      toAccount: "${toAccount}",
      amount: ${amount},
      assetId: "${assetId}",
      date: ${ExcelDateToISO(date)},
      fee: ${OptionalExcelNumber(fee)},
      feeAssetId: ${OptionalString(feeAssetId)},
    )
  }`;

  let result = await SingleQuery(mutation, getIdOrError(txId, data => data.createTransfer.toString()))
  return result 
}

/**
 * @customfunction
 * @param {string} account - The account identifier associated with the transaction.
 * @param {string} assetId - The unique identifier of the asset being transacted.
 * @param {number} amount - The quantity of the asset being transacted.
 * @param {string} costAssetId - The unique identifier of the asset used for costing the transaction.
 * @param {any} cost - The quantity of the cost asset for this transaction.
 * @param {number} date - The transaction date, represented as an Excel serial date number.
 * @param {string} guid - A unique identifier for the transaction.
 */
export async function spendAsset(account: string, assetId : string, amount : number, costAssetId? : string, cost?: any, date? : number, guid? : string)
{
  const costAmount = OptionalExcelNumber(cost);

  const mutation = `mutation {
    spendAsset(
      account: "${account}",
      assetId: "${assetId}",
      amount: ${amount},
      ${costAssetId !== undefined ? `costAssetId: "${costAssetId}",` : ""}
      cost: ${costAmount},
      date: ${ExcelDateToISO(date)},
      guid: "${guid}"
    )
  }`;  
  
  let result = await SingleQuery(mutation, getIdOrError(guid, data => data.spendAsset.toString()))
  return result
}

/**
 * @customfunction
 * @param {string} account - The account identifier associated with the received asset.
 * @param {string} assetId - The unique identifier of the asset being received.
 * @param {number} amount - The quantity of the asset being received.
 * @param {string} costAssetId - The unique identifier of the asset used for costing the receipt.
 * @param {any} cost - The quantity of the cost asset for this receipt.
 * @param {number} date - The receipt date, represented as an Excel serial date number.
 * @param {string} guid - A unique identifier for the command.
 */
export async function receiveAsset(account: string, assetId : string, amount : number, costAssetId? : string, cost?: any, date? : number, guid? : string)
{
  const costAmount = OptionalExcelNumber(cost);

  const mutation = `mutation {
    receiveAsset(
      account: "${account}",
      assetId: "${assetId}",
      amount: ${amount},
      ${costAssetId !== undefined ? `costAssetId: "${costAssetId}",` : ""}
      cost: ${costAmount},
      date: ${ExcelDateToISO(date)},
      guid: "${guid}"
    )
  }`;
  
  let result = await SingleQuery(mutation, getIdOrError(guid, data => data.receiveAsset.toString()))
  return result
}

/**
 * @customfunction
 * @param {string} account - The account identifier associated with the transaction.
 * @param {string} assetId - The unique identifier of the asset being transacted.
 * @param {number} amount - The quantity of the asset being transacted.
 * @param {string} costAssetId - The unique identifier of the asset used for costing the transaction.
 * @param {any} cost - The quantity of the cost asset for this transaction.
 * @param {number} date - The transaction date, represented as an Excel serial date number.
 * @param {string} guid - A unique identifier for the transaction.
 * @param {number} fee - The fee associated with the transaction.
 */
export async function transactAsset(account: string, assetId : string, amount : number, costAssetId? : string, cost?: any, date? : number, guid? : string, fee? : number)
{
  const costAmount = OptionalExcelNumber(cost);
  
  const mutation = `mutation {
    transactAsset(
      account: "${account}",
      assetId: "${assetId}",
      amount: ${amount},
      ${costAssetId !== undefined ? `costAssetId: "${costAssetId}",` : ""}
      cost: ${costAmount},
      date: ${ExcelDateToISO(date)},
      guid: "${guid}",
      fee: ${fee}
    )
  }`;  
  
  let result = await SingleQuery(mutation, getIdOrError(account, data => data.transactAsset.toString()))
  return result
}

/**
 * @customfunction
 * @param {string} account - The identifier of the account to which the transaction is added.
 * @param {string} txId - The transaction ID to be added.
 * @param {number} date - The date of the transaction.
 * @param {string} guid - A unique identifier associated with the transaction.
 * @return {Promise<any>} A promise resolving to the result of the mutation.
 */
export async function addTransaction(account: string, txId: string, date : number, guid : string)
{
  const mutation = `mutation {
    addTransaction( account : "${account}", txId : "${txId}", date : ${ExcelDateToISO(date)}, guid : "${guid}" )
  }`

  let result = await SingleQuery(mutation, getIdOrError(txId, data => data.addTransaction.toString()))
  return result
}

/**
 * @customfunction
 * @param {string} txId - The unique identifier for the transaction.
 * @param {string} transactionType Transaction type
 * @param {string} assetId - The identifier for the asset involved in the transaction.
 * @param {number} amount - The amount associated with the transaction.
 * @param {number} date - The timestamp representing the date of the transaction.
 * @param {string} [comment] - An optional comment or description for the transaction.
 * @param {string} [guid] - A globally unique identifier for the transaction.
 * @param {string} [relatedAssetId] - Related asset id
 * @param {string} [account] - The account associated with the transaction.
 * @return {Promise<any>} A promise that resolves with the result of the transaction creation.
 */
export async function createTransaction(txId : string, transactionType : string, assetId : string, amount : number, date : number, comment? : string, guid? : string, relatedAssetId? : string, account? : string) : Promise<any>
{
  const mutation = `mutation {
    createTransaction( txId : "${txId}", assetId : "${assetId}", amount : ${amount}, date : ${ExcelDateToISO(date)}, transactionType : "${transactionType}", comment : "${comment}", guid : "${guid}" ${relatedAssetId != undefined ? `, relatedAssetId : "${relatedAssetId}"` : ``}, account: ${OptionalString(account)} )
  }`
  
  let result = await SingleQuery(mutation, getIdOrError(txId, data => data.createTransaction.toString()))
  return result
}

/**
 * @customfunction
 * @param contractId Contract id
 * @param product Hash type
 * @param quantity Hash rate amount
 * @param total Total cost
 * @param timestamp Transaction date
 * @param guid Command guid
 */
export async function addContract(contractId : number, product : string, quantity : number, total : number, timestamp : any, guid : string) : Promise<any>
{
  timestamp = ExcelDateToJSDate(timestamp).getTime();
  const mutation = `mutation {
        buyHashrate ( txId : "${contractId}", type : "${product}", quantity : ${quantity}, total : ${total}, timestamp : ${timestamp}, guid : "${guid}" )
      }`;

  let result = await SingleQuery(mutation, getIdOrError(contractId.toString(), data => data.buyHashrate.toString()) )
  return result;
}

/**
 * @customfunction
 * @param contractId Contract id
 * @param product Hash type
 * @param quantity Hash rate amount
 * @param total Total cost
 * @param timestamp Transaction date
 * @param guid Command guid
 */
export async function addContracts(contractId : number[][], product : string[][], quantity : number[][], total : number[][], timestamp : any[][], guid : string[][]) : Promise<any>
{
  let start = new Date(Date.now()).getTime();
  let mutation = `mutation { createContracts( txId : [ ${contractId.map(x => `"${x}"`)} ], type: [ ${product.map(x => `"${x}"`)} ], quantity : [ ${quantity} ], total : [ ${total} ], timestamp : [ ${timestamp.map(x => ExcelDateToJSDate(x[0]).getTime())} ], guid : [ ${guid.map(x => `"${x}"`)} ] ) }`

  let result = await Mutation(mutation)
  let end = new Date(Date.now()).getTime();
  return end - start;
}

/**
 * @customfunction
 * @param contractId Contract id
 * @param product Hash type
 * @param quantity Hash rate amount
 * @param total Total cost
 * @param timestamp Transaction date
 */
export async function getOrAddContract(contractId : number, product : string, quantity : number, total : number, timestamp : any) : Promise<any>
{
  let stats = await contractStats(contractId)
  if (stats != "")
    return stats
  
  timestamp = ExcelDateToJSDate(timestamp).getTime();
  const mutation = `mutation {
        buyHashrate ( txId : "${contractId}", type : "${product}", quantity : ${quantity}, total : ${total}, timestamp : ${timestamp} )
      }`;
  
  let result = await Mutation(mutation);
  if(result == true)
    return await contractStats(contractId)
  return result;
}

/**
 * @customfunction
 * @param username Hashflare username
 * @param timestamp Registration timestamp
 * @param guid Command guid
 */
export async function registerHashflare(username: string, timestamp: any, guid : string) : Promise<any>
{
  timestamp = ExcelDateToJSDate(timestamp).getTime()

  const mutation = `mutation {
        registerHashflare( username : "${username}", timestamp : ${timestamp}, guid : "${guid}" )
    }`;

  let result = await SingleQuery(mutation, getIdOrError(username, data => data.registerHashflare.toString()) )
  return result
}

/**
 * @customfunction
 * @param username Hashflare username
 * @param timestamp Registration timestamp
 */
export async function getOrRegisterHashflare(username: string, timestamp: any) : Promise<any>
{
  let stats = await hashflareStats()
  if(stats != "")
    return stats
  
  timestamp = ExcelDateToJSDate(timestamp).getTime()

  const mutation = `mutation {
        registerHashflare( username : "${username}", timestamp : ${timestamp} )
    }`;
  
  let result = await Mutation(mutation);
  if(result == true)
    return await hashflareStats()
  return result
}

/**
 * @customfunction
 * @param type Hash type
 * @param amount Mine amount
 * @param timestamp Mining timestamp
 * @param guid Command guid
 */
export async function addSingleMinedAmount(type : string, amount : number, timestamp : any, guid : string) : Promise<any>
{
  let t = ExcelDateToJSDate(timestamp).getTime()

  const mutation = `mutation {
      addMinedAmount( type : "${type}", quantity : ${amount}, timestamp : ${t}, guid : "${guid}")
    }`

  let result = await SingleQuery(mutation, getIdOrError(type, data => data.addMinedAmount.toString()) )
  return result

}

/**
 * @customfunction
 * @param guid Command guid
 * @param type Hash type
 * @param amount Mine amount
 * @param timestamp Mining timestamp
 */
export async function addMinedAmounts(guid: string[][], type : string[][], amount : number[][], timestamp : any[][]) : Promise<any>
{
  let start = new Date(Date.now()).getTime();
  let mutation = `mutation { addMinedAmounts( type: [ ${type.map(x => `"${x}"`)} ], quantity : [ ${amount} ], timestamp : [ ${timestamp.map(x => ExcelDateToJSDate(x[0]).getTime())} ], guid : [ ${guid.map(x => `"${x}"`)} ] ) }`

  await Mutation(mutation)
  let end = new Date(Date.now()).getTime();

  return end - start;
}

/**
 * @customfunction
 * @param type Hash type
 * @param amount Mine amount
 * @param timestamp Mining timestamp
 */
export async function addMinedAmount(type : string[][], amount : number[][], timestamp : any[][]) : Promise<any>
{
  for(let i = 0; i < timestamp.length; i++)
  {
    let t = ExcelDateToJSDate(timestamp[i][0]).getTime()

    const mutation = `mutation {
      addMinedAmount( type : "${type[i][0]}", quantity : ${amount[i][0]}, timestamp : ${t})
    }`
    
    let result = await Mutation(mutation)
    if (result != true)
      return result
  }
  
  return true
}

/**
 * @customfunction
 * @param name Account name
 * @param type Account type
 * @param date Date to create
 * @param guid Command guid
 */
export async function createAccount(name : string, type : string, date : number, guid : string) : Promise<any>
{
  const mutation = `mutation {
    createAccount( name : "${name}", type : "${type}", date: ${ExcelDateToISO(date)}, guid : "${guid}" )
  }`
  
  let result = await MutationWithId(name, mutation)
  return result
}

/**
 * Fetches the asset quote based on the provided input parameters.
 * @customfunction
 * @param {string} forAssetId - The ID of the foreign asset.
 * @param {string} domAssetId - The ID of the domestic asset.
 * @param {number} date - The date (in numeric format) for which the asset quote is requested.
 */
export async function assetQuote(forAssetId : string, domAssetId : string, date: number) : Promise<any>
{
  let query = `query { assetQuote( forAssetId : "${forAssetId}", domAssetId : "${domAssetId}", date : ${ExcelDateToISO(date)} ) { quantity { amount } } }`

  let result = await SingleQuery(query, data => data.assetQuote.quantity.amount)
  window.console.log(result)

  return result
}

/**
 * @customfunction
 * @param contractId Contract Id
 */
export async function contractStats(contractId : number) : Promise<any>
{
  let query = `query {contractStats(txId: "${contractId}") {
      contractId
      quantity
      type
      mined
      date
      cost
    }}  
  `
  
  let result = await SingleQuery(query, data => data.contractStats)
  window.console.log(result)
  if (!result.contractId)
    return ""
  
  let myEntity : Excel.EntityCellValue = {
    type : Excel.CellValueType.entity,
    text : `Contract@${result.contractId}`,
    properties : {
      "Date" : {
        type : Excel.CellValueType.formattedNumber,
        basicValue : JSDateToExcelDate(new Date(Number.parseInt(result.date)/10000)),
        numberFormat: "dd/mm/yyyy hh:mm"
      },
      "Type" : {
        type : Excel.CellValueType.string,
        basicValue : result.type
      },
      "Quantity" : {
        type : Excel.CellValueType.double,
        basicValue : result.quantity
      },
      "Cost" : {
        type : Excel.CellValueType.double,
        basicValue : result.cost
      },
      "Total mined" : {
        type : Excel.CellValueType.double,
        basicValue : result.mined,
      }
    }
  }
  return myEntity
}

/**
 * @customfunction
 */
export async function hashflareStats() : Promise<any>
{
  let query = `query { hashflareStats {
      username
      bitcoinHashRate
      scryptHashRate
    }}  
  `

  let result = await SingleQuery(query, data => data.hashflareStats)
  if(!result.username)
    return ""
  
  const myEntity : Excel.EntityCellValue = {
    type : Excel.CellValueType.entity,
    text : "Hashflare",
    properties : {
      "Username" : {
        type : Excel.CellValueType.string,
        basicValue : result.username
      },
      "Bitcoin Hash Rate" : {
        type : Excel.CellValueType.double,
        basicValue : result.bitcoinHashRate
      },
      "Scrypt Hash Rate" : {
        type : Excel.CellValueType.double,
        basicValue : result.scryptHashRate
      }
    }
  }
  return myEntity
}

/**
 * @customfunction
 * @param {string[][]} accounts account names
 * @param {number} asOfDate as of date
 * @param {number} [startDate] start date
 * @param {string} assetId denominator asset
 */
export async function blendedIrr(accounts: string[][], asOfDate : number, startDate? : number, assetId? : string) : Promise<any> {
  if(assetId == undefined || assetId == "")
    assetId = "GBP"

  let query = `{
    blendedIrr( accounts:[${accounts.map(a => `"${a}"`).join(', ')}], denominator : { assetId : "${assetId}", assetType : CURRENCY }, date : ${ExcelDateToISO(asOfDate)}, startDate : ${ExcelDateToISO(startDate)} ) { irr }
  }`
  
  window.console.log(query)
  
  let result = await SingleQuery(query, data => data.blendedIrr.irr)
  return result
}

function FormatAssetQuoteOverrides(quoteOverrides?: string[][]): string {
  if(quoteOverrides === undefined || quoteOverrides == null)
    return null

  let assetQuoteOverrides = quoteOverrides.filter(a =>
          a !== undefined &&
          a !== null &&
          a[0] !== undefined && a[0] !== null && a[0] !== "" &&
          a[1] !== undefined && a[1] !== null && a[1] !== "" &&
          a[2] !== undefined && a[2] !== null && a[2] !== ""
      ).map(a => `{
      sourceOperationId: "${a[0]}",
      fordom: "${a[1]}",
      price: ${a[2]}
    }`).join(', ')
  return `[${assetQuoteOverrides}]`
}

/**
 * @customfunction
 * @param {string[][]} accounts account name
 * @param {number} asOfDate as of date
 * @param {string} assetId denominator asset
 * @param quoteOverrides
 */
export async function accountStats(accounts : string[][], asOfDate : number, assetId? : string, quoteOverrides?: string[][]) : Promise<any> {
  if(assetId == undefined || assetId == "")
    assetId = "GBP"

  let assetQuoteOverrides = FormatAssetQuoteOverrides(quoteOverrides)
  
  let query = ''
  if(accounts.length == 1)
  {
    query = `{ stats: accountStats(  
      accountName : "${accounts[0][0]}",
      date : "${ExcelDateToJSDate(asOfDate).toISOString()}",
      denominator : { assetId : "${assetId}", assetType : CURRENCY },
      assetQuoteOverrides : ${assetQuoteOverrides})
      {
        balance { amount denominator { assetId } }
        income { amount denominator { assetId } } 
        cashBalance { amount denominator { assetId } } 
        totalDividend { amount denominator { assetId } } 
        positions { amount denominator { assetId } } 
        values { amount denominator { assetId } } 
        dividends { amount denominator { assetId } } 
        costBasis { amount denominator { assetId } } 
        realisedGains { amount denominator { assetId } } 
        irr 
      }
    }`;
  }
  else 
  {
    query = `{ stats: combinedAccountStats(
      accounts:[${accounts.map(a => `"${a}"`).join(', ')}],
      date : "${ExcelDateToJSDate(asOfDate).toISOString()}",
      denominator : { assetId : "${assetId}", assetType : CURRENCY },
      assetQuoteOverrides : ${assetQuoteOverrides} ) 
      { 
        balance { amount denominator { assetId } } 
        income { amount denominator { assetId } } 
        cashBalance { amount denominator { assetId } } 
        totalDividend { amount denominator { assetId } } 
        positions { amount denominator { assetId } } 
        values { amount denominator { assetId } } 
        dividends { amount denominator { assetId } } 
        costBasis { amount denominator { assetId } } 
        realisedGains { amount denominator { assetId } } 
        irr
      }
    }`;
  }
  
  window.console.log(query)

  // amount = Number(await SingleQuery(query, data => data.accountStats.balance.amount.toString()))
  let result = await SingleQuery(query, data => ({ 
    amount: data.stats.balance.amount, asset: data.stats.balance.denominator,
    cashAmount: data.stats.cashBalance.amount, cashAsset: data.stats.cashBalance.denominator,
    totalDividend: data.stats.totalDividend.amount, income : data.stats.income.amount, 
    positions: data.stats.positions, values: data.stats.values, costBasis: data.stats.costBasis, realisedGains: data.stats.realisedGains, dividends: data.stats.dividends,
    irr: data.stats.irr }
  ))
  if(typeof(result)  === "string")
    return result
  
  const myEntity : Excel.EntityCellValue = {
    type : Excel.CellValueType.entity, 
    text : `[${assetId}] ` + accounts.map(a => `${a}`).join(', '),
    properties : {
      "IRR" : {
        type : Excel.CellValueType.double,
        basicValue : result.irr,
      },
      "Balance" : {
        type : Excel.CellValueType.double,
        basicValue: result.amount, 
      },
      "CashBalance" : {
        type: Excel.CellValueType.double,
        basicValue: result.cashAmount,
      },
      "Income" : {
        type: Excel.CellValueType.double,
        basicValue: result.income,
      },
      "Dividend" : {
        type: Excel.CellValueType.double,
        basicValue: result.totalDividend,
      },
      "Positions" : {
        type : Excel.CellValueType.array,
        elements: (result.positions && result.positions.length > 0)
          ? result.positions.map((pos, idx) => ({
              type: Excel.CellValueType.entity,
              text: pos.denominator?.assetId ?? "",
              properties: {
                "Asset": {
                  type: Excel.CellValueType.string,
                  basicValue: pos.denominator?.assetId ?? ""
                },
                "Amount": {
                  type: Excel.CellValueType.double,
                  basicValue: pos.amount ?? 0
                },
                "Value": {
                  type: Excel.CellValueType.double,
                  basicValue: result.values?.[idx]?.amount ?? 0
                },
                "Dividend": {
                  type: Excel.CellValueType.double,
                  basicValue: result.dividends?.[idx]?.amount ?? 0
                },
                "CostBasis" : {
                  type: Excel.CellValueType.double,
                  basicValue: result.costBasis?.[idx]?.amount ?? 0
                },
                "RealisedGain": {
                  type: Excel.CellValueType.double,
                  basicValue: result.realisedGains?.[idx]?.amount ?? 0
                }
              }
            }))
          : [{
              type: Excel.CellValueType.entity,
              text: "No positions",
              properties: {
                "Asset": {
                  type: Excel.CellValueType.string,
                  basicValue: ""
                },
                "Amount": {
                  type: Excel.CellValueType.double,
                  basicValue: 0
                },
                "Value": {
                  type: Excel.CellValueType.double,
                  basicValue: 0
                },
                "Dividend" : {
                  type: Excel.CellValueType.double,
                  basicValue: 0
                },
                "CostBasis" : {
                  type: Excel.CellValueType.double,
                  basicValue: 0 
                },
                "RealisedGain": {
                  type: Excel.CellValueType.double,
                  basicValue: 0
                }
              }
            }]
      },
    }
  }
  return myEntity
}

/**
 * @customfunction
 * @param {string[][]} accounts account names
 * @param {number} asOfDate as of date
 * @param {string} assetId disposed asset id
 * @param {string} [denominatorAssetId] denominator asset
 * @param {string[][]} [quoteOverrides] Quote overrides
 * @param {boolean} [trackDisposalLots] Aggregate disposal gains
 */
export async function disposalGainItems(accounts : string[][], asOfDate : number, assetId : string, denominatorAssetId? : string, quoteOverrides?: string[][], trackDisposalLots? : boolean) : Promise<any> {
  if(denominatorAssetId == undefined || denominatorAssetId == "")
    denominatorAssetId = "GBP"

  if(trackDisposalLots === undefined || trackDisposalLots == null)
    trackDisposalLots = false
  
  let assetQuoteOverrides = FormatAssetQuoteOverrides(quoteOverrides)
  let query = `{ disposalGains: accountDisposalGainItems(
      accounts:[${accounts.map(a => `"${a}"`).join(', ')}],
      assetId : "${assetId}",
      denominatorAssetId : "${denominatorAssetId}",
      date : "${ExcelDateToJSDate(asOfDate).toISOString()}",
      assetQuoteOverrides : ${assetQuoteOverrides},
      trackDisposalLots : ${trackDisposalLots})
      {
        items {
          date
          acquisitionDate
          quantity
          proceeds
          costBasis
          gain
          taxYear
          matchType
        }
      }
    }`;

  window.console.log(query)

  let result = await SingleQuery(query, data => data.disposalGains.items)
  if(typeof(result)  === "string")
    return result

  const emptyDisposalGainItem = {
    type: Excel.CellValueType.entity,
    text: "N/A",
    properties: {
      "Disposal Date": {
        type: Excel.CellValueType.error,
        errorType: Excel.ErrorCellValueType.notAvailable
      },
      "Acquisition Date": {
        type: Excel.CellValueType.error,
        errorType: Excel.ErrorCellValueType.notAvailable
      },
      "Quantity": {
        type: Excel.CellValueType.error,
        errorType: Excel.ErrorCellValueType.notAvailable
      },
      "Proceeds": {
        type: Excel.CellValueType.error,
        errorType: Excel.ErrorCellValueType.notAvailable
      },
      "Cost Basis": {
        type: Excel.CellValueType.error,
        errorType: Excel.ErrorCellValueType.notAvailable
      },
      "Gain": {
        type: Excel.CellValueType.error,
        errorType: Excel.ErrorCellValueType.notAvailable
      },
      "Tax Year": {
        type: Excel.CellValueType.error,
        errorType: Excel.ErrorCellValueType.notAvailable
      },
      "Match Type": {
        type: Excel.CellValueType.error,
        errorType: Excel.ErrorCellValueType.notAvailable
      }
    }
  }

  const disposalGainRows = result && result.length > 0
    ? result
        .sort((a, b) => new Date(a.date).getTime() - new Date(b.date).getTime())
        .map(item => [({
          type: Excel.CellValueType.entity,
          text: `${item.date ?? ""}`,
          properties: {
            "Disposal Date": {
              type: Excel.CellValueType.double,
              basicValue: DateTimeToExcelDate(item.date),
              numberFormat: "yyyy-mm-dd"
            },
            "Acquisition Date": item.acquisitionDate
              ? {
                type: Excel.CellValueType.double,
                basicValue: DateTimeToExcelDate(item.acquisitionDate),
                numberFormat: "yyyy-mm-dd"
              }
              : {
                type: Excel.CellValueType.string,
                basicValue: ""
              },
            "Quantity": {
              type: Excel.CellValueType.double,
              basicValue: CleanNumber(item.quantity, 12)
            },
            "Proceeds": {
              type: Excel.CellValueType.double,
              basicValue: CleanMoney(item.proceeds)
            },
            "Cost Basis": {
              type: Excel.CellValueType.double,
              basicValue: CleanMoney(item.costBasis)
            },
            "Gain": {
              type: Excel.CellValueType.double,
              basicValue: CleanMoney(item.gain)
            },
            "Tax Year": {
              type: Excel.CellValueType.double,
              basicValue: item.taxYear ?? 0
            },
            "Match Type": {
              type: Excel.CellValueType.string,
              basicValue: item.matchType ?? ""
            }
          }
        })])
    : [[emptyDisposalGainItem]]

  if (disposalGainRows.length === 1)
    disposalGainRows.push([emptyDisposalGainItem])

  const myEntity : Excel.EntityCellValue = {
    type : Excel.CellValueType.entity,
    text : `${accounts.map(a => `${a}`).join(', ')} ${assetId} disposals`,
    properties : {
      "Items" : {
        type : Excel.CellValueType.array,
        elements: disposalGainRows
      }
    }
  }
  return myEntity
}

/**
 * @customfunction
 * @param {string} account account name
 * @param {number} asOfDate as of date
 * @param invocation Custom function handler
 */
export function accountStatsDynamic(account : string, asOfDate : number, invocation : CustomFunctions.StreamingInvocation<string>) : void {
  const query = `{
      accountStats(  accountName : "${account}", date : "${ExcelDateToJSDate(asOfDate).toISOString()}" ) { balance { amount } } 
  }`;

  Query(query, data => data.accountStats.balance.amount.toString(), invocation);
}

/**
 * @customfunction
 * @param invocation Custom function handler
 */
export function activeBranch(invocation : CustomFunctions.StreamingInvocation<string>) : void {
  const query = `query { activeBranch }`;

  Query(query, data => data.activeBranch.toString(), invocation);
}

