namespace Chronos.Core
{
    /// <summary>
    /// Currency asset
    /// </summary>
    public class Currency : Asset
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="assetId">Currency asset id</param>
        public Currency(string assetId) 
            : base(assetId, assetId, Type.Currency)
        {
        }
    }
}