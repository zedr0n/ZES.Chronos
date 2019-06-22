using ZES.Infrastructure.Domain;

namespace Chronos.Accounts.Commands
{
    public class DepositAsset : Command
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DepositAsset"/> class.
        /// </summary>
        public DepositAsset() { }

        /// <summary>
        /// Initializes a new instance of the <see cref="DepositAsset"/> class.
        /// </summary>
        /// <param name="accountName">Account name</param>
        /// <param name="assetId">Asset identifier</param>
        /// <param name="quantity">Asset quantity</param>
        public DepositAsset(string accountName, string assetId, double quantity)
        {
            AssetId = assetId;
            Quantity = quantity;
            Target = accountName;
        }
        
        /// <summary>
        /// Gets asset identifier 
        /// </summary>
        /// <value>
        /// <placeholder>Asset identifier</placeholder>
        /// </value>
        public string AssetId { get; private set; }

        /// <summary>
        /// Gets quantity of asset deposited
        /// </summary>
        /// <value>
        /// <placeholder>Quantity of asset deposited</placeholder>
        /// </value>
        public double Quantity { get; private set; }
    }
}