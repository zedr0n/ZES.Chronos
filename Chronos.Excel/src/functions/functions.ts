import {Mutation, Query, SingleQuery} from './queries';

function ExcelDateToJSDate (serial : number) : Date {
  var utc_days  = Math.floor(serial - 25569);
  var utc_value = utc_days * 86400;
  var date_info = new Date(utc_value * 1000);

  var fractional_day = serial - Math.floor(serial) + 0.0000001;

  var total_seconds = Math.floor(86400 * fractional_day);

  var seconds = total_seconds % 60;

  total_seconds -= seconds;

  var hours = Math.floor(total_seconds / (60 * 60));
  var minutes = Math.floor(total_seconds / 60) % 60;

  return new Date(date_info.getFullYear(), date_info.getMonth(), date_info.getDate(), hours, minutes, seconds);
}

function JSDateToExcelDate(date : Date) : number {
  let converted = 25569.0 + ((date.getTime() - (date.getTimezoneOffset() * 60 * 1000)) / (1000 * 60 * 60 * 24));
  return converted
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
  if (!result.contractId)
    return ""
  
  let myEntity : Excel.EntityCellValue = {
    type : Excel.CellValueType.entity,
    text : `Contract@${result.contractId}`,
    properties : {
      "Date" : {
        type : Excel.CellValueType.formattedNumber,
        basicValue : JSDateToExcelDate(new Date(result.date)),
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
 */
export async function accountStats(account : string, asOfDate : number, assetId? : string, immediate? : boolean) : Promise<any> {
  let query = `{
      accountStats(  accountName : "${account}", date : "${ExcelDateToJSDate(asOfDate).toISOString()}" ) { balance { amount } } 
  }`;
  if (assetId != undefined && assetId != "")
  {
    if (immediate != undefined && immediate) {
      query = `{
        accountStats(  accountName : "${account}", date : "${ExcelDateToJSDate(asOfDate).toISOString()}", assetId : "${assetId}", immediate : true ) { balance { amount } } 
      }`;
    }
    else {
        query = `{
        accountStats(  accountName : "${account}", date : "${ExcelDateToJSDate(asOfDate).toISOString()}", assetId : "${assetId}" ) { balance { amount } } 
      }`;
    }
  }
  
  window.console.log(query)

  let amount = 0
  // amount = Number(await SingleQuery(query, data => data.accountStats.balance.amount.toString()))
  let result = await SingleQuery(query, data => data.accountStats.balance.amount.toString())
  amount = Number(result)
  if (isNaN(amount))
    return result
  return amount
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

