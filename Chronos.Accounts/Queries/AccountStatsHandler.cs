using Chronos.Accounts.Events;
using ZES.Infrastructure.Projections;

namespace Chronos.Accounts.Queries
{
    /// <inheritdoc />
    public class AccountStatsHandler : ProjectionHandlerBase<AccountStats, AccountCreated>
    {
        /// <inheritdoc />
        public override AccountStats Handle(AccountCreated e, AccountStats state)
        {
            state.Increment();
            return state;
        }
    }
}