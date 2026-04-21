import {Mutation, Query, SingleQuery, getIdOrError, MutationWithId} from './queries';
import {v4 as uuidv4} from 'uuid';
import * as webpack from "webpack";

/**
 * @customfunction
 */
export async function guid() : Promise<any> {

  let guid = uuidv4();
  return guid;
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
    registerCurrencyPair( forCcy : "${forCcy}", domCcy : "${domCcy}" )
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
 */
export async function registerAssetPair(forAssetId : string, forAssetType : string, domAssetId : string, domAssetType : string, guid: string) : Promise<any>
{
  const mutation = `mutation {
    registerAssetPair( forAsset : {assetId : "${forAssetId}", assetType : ${forAssetType.toUpperCase()}}, domAsset : {assetId : "${domAssetId}", assetType : ${domAssetType.toUpperCase()}}, guid : "${guid}" )
  }`
 
  const fordom = forAssetId + domAssetId;
  
  let result = await SingleQuery(mutation, getIdOrError(fordom, data => data.registerAssetPair.toString()))
  return result;
}

/**
 * @customfunction
 * @param {string} forAssetId - The unique identifier of the asset for which the stock split is being added.
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
 * @param {string} account - The account identifier associated with the transaction.
 * @param {string} assetId - The unique identifier of the asset being transacted.
 * @param {number} amount - The quantity of the asset being transacted.
 * @param {string} costAssetId - The unique identifier of the asset used for costing the transaction.
 * @param {number} cost - The quantity of the cost asset for this transaction.
 * @param {number} date - The transaction date, represented as an Excel serial date number.
 * @param {string} guid - A unique identifier for the transaction.
 * @param {number} fee - The fee associated with the transaction.
 */
export async function transactAsset(account: string, assetId : string, amount : number, costAssetId? : string, cost?: number, date? : number, guid? : string, fee? : number)
{
  let mutation = `mutation {
      transactAsset( account : "${account}", assetId: "${assetId}", amount : ${amount}, ${costAssetId != undefined ? `costAssetId : "${costAssetId}", ` : ``} ${cost != undefined ? `cost : ${cost},` : ""} date : ${ExcelDateToISO(date)}, guid : "${guid}", fee : ${fee} )
    }`

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
 * @return {Promise<any>} A promise that resolves with the result of the transaction creation.
 */
export async function createTransaction(txId : string, transactionType : string, assetId : string, amount : number, date : number, comment? : string, guid? : string, relatedAssetId? : string) : Promise<any>
{
  const mutation = `mutation {
    createTransaction( txId : "${txId}", assetId : "${assetId}", amount : ${amount}, date : ${ExcelDateToISO(date)}, transactionType : "${transactionType}", comment : "${comment}", guid : "${guid}" ${relatedAssetId != undefined ? `, relatedAssetId : "${relatedAssetId}"` : ``} )
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
 * @param {string} forAssetType - The type of the foreign asset.
 * @param {string} domAssetId - The ID of the domestic asset.
 * @param {string} domAssetType - The type of the domestic asset.
 * @param {number} date - The date (in numeric format) for which the asset quote is requested.
 */
export async function assetQuote(forAssetId : string, forAssetType : string, domAssetId : string, domAssetType: string, date: number) : Promise<any>
{
  let query = `query { assetQuote( forAsset : {assetId : "${forAssetId}", assetType : ${forAssetType.toUpperCase()}}, domAsset : {assetId : "${domAssetId}", assetType : ${domAssetType.toUpperCase()}}, date : ${ExcelDateToISO(date)} ) { quantity { amount } } }`

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
 * @param {string} account account name
 * @param {number} asOfDate as of date
 * @param {string} assetId denominator asset
 * @param {boolean} immediate convert to asset at tx date
 * @param {boolean} [withPositions] include positions
 */
export async function accountStats(account : string, asOfDate : number, assetId? : string, immediate? : boolean, withPositions?: boolean) : Promise<any> {
  if(assetId == undefined || assetId == "")
    assetId = "GBP"

  let query = `{
      accountStats(  accountName : "${account}", date : "${ExcelDateToJSDate(asOfDate).toISOString()}", denominator : { assetId : "${assetId}", assetType : CURRENCY }, immediate : ${immediate}, withPositions : ${withPositions} ) { balance { amount denominator { assetId } } cashBalance { amount denominator { assetId } } totalDividend { amount denominator { assetId } } positions { amount denominator { assetId } } values { amount denominator { assetId } } dividends { amount denominator { assetId } } costBasis { amount denominator { assetId } } }
    }`;
  
  window.console.log(query)

  // amount = Number(await SingleQuery(query, data => data.accountStats.balance.amount.toString()))
  let result = await SingleQuery(query, data => ({ 
    amount: data.accountStats.balance.amount, asset: data.accountStats.balance.denominator,
    cashAmount: data.accountStats.cashBalance.amount, cashAsset: data.accountStats.cashBalance.denominator,
    totalDividend: data.accountStats.totalDividend.amount,    
    positions: data.accountStats.positions, values: data.accountStats.values, costBasis: data.accountStats.costBasis, dividends: data.accountStats.dividends}
  ))
  if(typeof(result)  === "string")
    return result
  
  const myEntity : Excel.EntityCellValue = {
    type : Excel.CellValueType.entity, 
    text : account,
    properties : {
      "Balance" : {
        type : Excel.CellValueType.entity,
        text : "Balance",
        properties : {
          "Amount" : {
            type : Excel.CellValueType.double,
            basicValue : result.amount,
          },
          "Asset" : {
            type : Excel.CellValueType.string,
            basicValue : result.asset ? result.asset.assetId ?? "" : ""
          }  
        },
      },
      "CashBalance" : {
        type: Excel.CellValueType.entity,
        text: "Cash Balance",
        properties : {
          "Amount" : {
            type : Excel.CellValueType.double,
            basicValue : result.cashAmount,
          },
          "Asset" : {
            type : Excel.CellValueType.string,
            basicValue : result.cashAsset ? result.cashAsset.assetId ?? "" : ""
          }  
        },
      },
      "Dividend" : {
        type: Excel.CellValueType.entity,
        text: "Total dividend",
        properties : {
          "Amount" : {
            type : Excel.CellValueType.double,
            basicValue : result.totalDividend,
          },
          "Asset" : {
            type : Excel.CellValueType.string,
            basicValue : result.cashAsset ? result.cashAsset.assetId ?? "" : ""
          }
        },
      },
      /*"Info" : {
        type : Excel.CellValueType.array,
        elements: [[ {
            type : Excel.CellValueType.entity,
            text : "Balance",
            properties : {
              "Type" : {
                type: Excel.CellValueType.string,
                basicValue: "Balance",
              },
              "Amount" : {
                type : Excel.CellValueType.double,
                basicValue : result.amount,
              },
              "Asset" : {
                type : Excel.CellValueType.string,
                basicValue : result.asset ? result.asset.assetId ?? "" : ""
              }
            }            
          },
          {
            type : Excel.CellValueType.entity,
            text : "Cash Balance",
            properties : {
              "Type" : {
                type: Excel.CellValueType.string,
                basicValue: "Cash Balance",
              },
              "Amount" : {
                type : Excel.CellValueType.double,
                basicValue : result.cashAmount,
              },
              "Asset" : {
                type : Excel.CellValueType.string,
                basicValue : result.cashAsset ? result.cashAsset.assetId ?? "" : ""
              }
            }
          },
          {
              type: Excel.CellValueType.entity,
              text: "Total dividend",
              properties : {
                "Type" : {
                  type: Excel.CellValueType.string,
                  basicValue: "Dividend",
                },
                "Amount" : {
                  type : Excel.CellValueType.double,
                  basicValue : result.totalDividend,
                },
                "Asset" : {
                  type : Excel.CellValueType.string,
                  basicValue : result.cashAsset ? result.cashAsset.assetId ?? "" : ""
                }
              },
            },            
        ]],
      },*/
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

