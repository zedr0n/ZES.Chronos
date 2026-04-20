using System;
using System.Linq;
using System.Threading.Tasks;
using Chronos.Core.Queries;
using ZES.Infrastructure.Domain;
using ZES.Interfaces.Branching;
using ZES.Interfaces.Domain;

namespace Chronos.Accounts.Queries;

public class TransactionListQueryHandler(IProjectionManager manager, ITimeline activeTimeline, IQueryHandler<TransactionInfoQuery, TransactionInfo> transactionInfoQueryHandler)
    : DefaultSingleQueryHandler<TransactionListQuery, TransactionList, TransactionListState>(manager, activeTimeline)
{
    protected override async Task<TransactionList> Handle(IProjection<TransactionListState> projection, TransactionListQuery query)
    {
        if (projection == null)
            throw new ArgumentNullException(nameof(projection), $"{typeof(IProjection<TransactionListState>).Name}");        

        var state = projection?.State;
        var result = new TransactionList
        {
            TxId = state.TxId.ToList()
        };

        if (!query.IncludeInfo)
            return result;
        
        foreach (var txId in state.TxId)
        {
            var txInfo = await transactionInfoQueryHandler.Handle(new TransactionInfoQuery(txId));
            result.Infos.Add(txInfo);
        }

        return result;
    }
}