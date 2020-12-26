import { request } from 'graphql-request';

function setIntervalImmediately(func, interval) {
    func();
    return setInterval(func, interval);
}

export async function SingleQuery(query : string, 
                                  parseFn : (data : any) => string) : Promise<string>
{
    const value = await graphQlQuerySingle(window.server, query, parseFn);
    return String(value);
}

export function Query(query : string,
                      parseFn: ( data : any ) => string,
                      invocation : CustomFunctions.StreamingInvocation<string>,
                      continueFn? : ( result : string ) => boolean) : void {
    graphQlQuery(window.server, query, parseFn, window.period, invocation, continueFn);
}

export async function Mutation(mutation : string)
{
    await graphQlMutation(window.server, mutation); 
}

async function graphQlQuerySingle(server : string,
                      query : string,
                      parseFn: ( data : any ) => string,
                      retry? : boolean) : Promise<string>
{
    let result : string = "";
    try {
        let reqResult = await request(server, query)
        // console.log(reqResult)
        result = parseFn(reqResult);
    }
    catch(error) {
        /*if (retry == undefined) {
            console.log("Retrying...")
            await new Promise(r => setTimeout(r, 100));
            result = await graphQlQuerySingle(server, query, parseFn, false)
        }
        else*/
            result = error.message;
    }
    return result;
}

async function graphQlMutation(server : string, mutation : string)
{
    try {
        await request(server, mutation);
    }
    catch(error) {
        return error.message;
    }
}

function graphQlQuery(server : string,
                      query : string, 
                                     parseFn: ( data : any ) => string,
                                     period : number,
                                     invocation : CustomFunctions.StreamingInvocation<string>,
                                     continueFn? : ( result : string ) => boolean) : void {
   if (continueFn == undefined)
       continueFn = _ => true;
    
    const timer = setIntervalImmediately(() => {
        // invocation.setResult("Querying...");
        try {
            request(server, query).then(data => {
                let result = parseFn(data);
                invocation.setResult(result);
                if (!continueFn(result))
                    clearInterval(timer);
                })
                .catch(r => invocation.setResult(r.message))
        }
        catch(error) {
            invocation.setResult(error.message)
        }
    }, period);

    invocation.onCanceled = () => {
        clearInterval(timer);
    };
}
