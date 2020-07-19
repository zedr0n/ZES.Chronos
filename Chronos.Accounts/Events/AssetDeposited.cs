using ZES.Infrastructure.Domain;

namespace Chronos.Accounts.Events
{
    /// <summary>
    /// Event fired when asset is deposited to account
    /// </summary>
    public class AssetDeposited : Event
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AssetDeposited"/> class.
        /// </summary>
        /// <param name="assetId">Asset identifier</param>
        /// <param name="quantity">Quantity deposited</param>
        public AssetDeposited(string assetId, double quantity)
        {
            AssetId = assetId;
            Quantity = quantity;
        }

        /// <summary>
        /// Gets asset identifier 
        /// </summary>
        /// <value>
        /// Asset identifier
        /// </value>
        public string AssetId { get; private set; }

        /// <summary>
        /// Gets quantity of asset deposited
        /// </summary>
        /// <value>
        /// Quantity of asset deposited
        /// </value>
        public double Quantity { get; private set; }
    }
}