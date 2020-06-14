using Chronos.Accounts.Events;
using ZES.Infrastructure.Projections;

namespace Chronos.Accounts.Queries
{
    public class AccountStatsAccountCreatedHandler : ProjectionHandlerBase<AccountStats, AccountCreated>
    {
        public override AccountStats Handle(AccountCreated e, AccountStats state)
        {
            state.Increment();
            return state;
        }
    }
}