using System.Collections.Concurrent;
using Chronos.Accounts.Events;
using Chronos.Core;
using ZES.Infrastructure.Domain;
using ZES.Interfaces.Domain;

namespace Chronos.Accounts
{
    /// <inheritdoc cref="EventSourced" />
    public class Account : EventSourced, IAggregate
    {
        private readonly ConcurrentDictionary<Asset, double> _assets = new ConcurrentDictionary<Asset, double>();
        
        private Type _type;
        
        /// <summary>
        /// Initializes a new instance of the <see cref="Account"/> class.
        /// </summary>
        public Account()
        {
            Register<AccountCreated>(ApplyEvent);
            Register<AssetDeposited>(ApplyEvent);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Account"/> class.
        /// </summary>
        /// <param name="name">Account identifier</param>
        /// <param name="type">Account type</param>
        public Account(string name, Type type)
            : this()
        {
            base.When(new AccountCreated(name, type));    
        }
        
        /// <summary>
        /// Account type enum
        /// </summary>
        public enum Type
        {
            /// <summary>
            /// Savings account ( bank, cash, etc... )
            /// </summary>
            Saving,
            
            /// <summary>
            /// Trading account ( stocks, crypto, etc... ) 
            /// </summary>
            Trading
        }

        /// <summary>
        /// Deposit an asset to account 
        /// </summary>
        /// <param name="assetId">Asset identifier</param>
        /// <param name="quantity">Quantity to deposit</param>
        public void DepositAsset(string assetId, double quantity)
        {
            base.When(new AssetDeposited(assetId, quantity));
        }

        private void ApplyEvent(AccountCreated e)
        {
            Id = e.Name;
            _type = e.Type;
        }

        private void ApplyEvent(AssetDeposited e)
        {
            var asset = new Asset(e.AssetId, Asset.Type.Coin);
            _assets.TryAdd(asset, 0.0);
            
            _assets[asset] += e.Quantity;
        }
    }
}