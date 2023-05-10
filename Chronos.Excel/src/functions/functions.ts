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
  return result
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
      details {
        key    
        value {
          type
          quantity
        }
      }
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

