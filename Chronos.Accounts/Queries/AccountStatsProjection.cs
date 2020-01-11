using Chronos.Accounts.Events;
using ZES.Infrastructure.Projections;
using ZES.Interfaces;
using ZES.Interfaces.Domain;
using ZES.Interfaces.EventStore;
using ZES.Interfaces.Pipes;

namespace Chronos.Accounts.Queries
{
    /// <inheritdoc />
    public class AccountStatsProjection : SingleProjection<AccountStats>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AccountStatsProjection"/> class.
        /// </summary>
        /// <param name="eventStore">Event store service</param>
        /// <param name="log">Log service</param>
        /// <param name="timeline">Timeline service</param>
        /// <param name="messageQueue">Message queue service</param>
        public AccountStatsProjection(IEventStore<IAggregate> eventStore, ILog log, ITimeline timeline, IMessageQueue messageQueue) 
            : base(eventStore, log, timeline, messageQueue)
        {
            Register<AccountCreated>(When);
        }

        private static AccountStats When(AccountCreated e, AccountStats state)
        {
            state.Increment();

            return state;
        }
    }
}