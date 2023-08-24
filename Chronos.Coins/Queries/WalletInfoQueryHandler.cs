using System.Threading.Tasks;
using Chronos.Core;
using ZES.Infrastructure.Domain;
using ZES.Interfaces;
using ZES.Interfaces.Domain;

namespace Chronos.Coins.Queries
{
    /// <inheritdoc />
    public class WalletInfoQueryHandler : DefaultSingleQueryHandler<WalletInfoQuery, WalletInfo, WalletInfo>
    {
        private readonly IQueryHandler<CoinInfoQuery, CoinInfo> _handler;

        /// <summary>
        /// Initializes a new instance of the <see cref="WalletInfoQueryHandler"/> class.
        /// </summary>
        /// <param name="manager">Projection manager</param>
        /// <param name="activeTimeline">Active timeline</param>
        /// <param name="handler">Coin info query handler</param>
        public WalletInfoQueryHandler(IProjectionManager manager, ITimeline activeTimeline, IQueryHandler<CoinInfoQuery, CoinInfo> handler) 
            : base(manager, activeTimeline)
        {
            _handler = handler;
        }

        /// <inheritdoc/>
        protected override async Task<WalletInfo> Handle(IProjection<WalletInfo> projection, WalletInfoQuery query)
        {
            var state = projection.State;

            var coinInfo = await _handler.Handle(new CoinInfoQuery(state.Asset.AssetId));
            return new WalletInfo(state.Address, new Asset(coinInfo.Name, coinInfo.Ticker, AssetType.Coin), state.Balance, state.MineQuantity);
        }
    }
}