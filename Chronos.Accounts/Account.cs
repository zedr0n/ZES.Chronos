using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Chronos.Core;

namespace Chronos.Accounts
{
    public sealed class Account : ZES.Infrastructure.Domain.AggregateRoot
    {
        private readonly ConcurrentDictionary<Core.Asset, double> _assets = new();

        public Account()
        {
            Register<Events.AccountCreated>(ApplyEvent);
            Register<Events.AssetDeposited>(ApplyEvent);
            Register<Events.TransactionAdded>();
            Register<Events.AssetTransactionStarted>();
        }

        public Account(string name, AccountType type) : this()
        {
            When(new Events.AccountCreated(name, type));
        }

        public string Name 
        { 
            get;
            set; 
        }

        public AccountType Type
        {
            get;
            set;
        }

        public List<string> Assets => _assets.Keys.Select(k => k.AssetId).ToList();

        public void DepositAsset (Core.Quantity quantity)
        {
            When(new Events.AssetDeposited(quantity));
        }  
        public void AddTransaction (string txId)
        {
            When(new Events.TransactionAdded(txId));
        }
        public void TransactAsset(Quantity asset, Quantity cost, bool queryQuote)
        {
            When(new Events.AssetTransactionStarted(asset, cost, queryQuote));
        }
        private void ApplyEvent(Events.AccountCreated e)
        {
            Id = e.Name;
            Type = e.Type;
        }

        private void ApplyEvent(Events.AssetDeposited e)
        {
            _assets.AddOrUpdate(e.Quantity.Denominator, e.Quantity.Amount, (a, d) => d + e.Quantity.Amount);
        }
    }
}