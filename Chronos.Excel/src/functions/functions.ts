import { request } from 'graphql-request';
import { graphQlQuery } from './queries';

/**
 * @customfunction
 * @param url Server url
 * @param period Update period
 * @param invocation Stats query custom function handler
 */
function statsQuery(url : string, period: number,invocation: CustomFunctions.StreamingInvocation<string>): void {
  const query = `{
      stats(query : {}) { numberOfCoins }
  }`;

  graphQlQuery(url, query, data => data.stats.numberOfCoins.toString(), period, invocation);
}

/**
 * @customfunction
 * @param url Server url
 * @param period Update period
 * @param invocation Stats query custom function handler
 */
function accountStatsQuery(url : string, period: number,invocation: CustomFunctions.StreamingInvocation<string>): void {
  const query = `{
      accountStats() { numberOfAccounts }
  }`;

  graphQlQuery(url, query, data => data.accountStats.numberOfAccounts.toString(), period, invocation);
}

/**
 * @customfunction
 * @param url Server url
 * @param period Update period
 * @param invocation Custom function handler
 */
function activeBranch(url : string, period : number, invocation : CustomFunctions.StreamingInvocation<string>) : void {
  const query = `{
    activeBranch
  }`;

  graphQlQuery(url, query, data => data.activeBranch.toString(), period, invocation);
}

