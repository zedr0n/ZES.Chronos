using System.Threading.Tasks;
using Chronos.Core;
using ZES.Infrastructure.Domain;
using ZES.Interfaces.Domain;

namespace Chronos.Coins.Queries
{
    public class WalletInfoQueryHandler : DefaultSingleQueryHandler<WalletInfoQuery, WalletInfo, WalletInfo>
    {
        private readonly IQueryHandler<CoinInfoQuery, CoinInfo> _handler;
        
        public WalletInfoQueryHandler(IProjectionManager manager, IQueryHandler<CoinInfoQuery, CoinInfo> handler) 
            : base(manager)
        {
            _handler = handler;
        }

        protected override async Task<WalletInfo> Handle(IProjection<WalletInfo> projection, WalletInfoQuery query)
        {
            var state = projection.State;

            var coinInfo = await _handler.Handle(new CoinInfoQuery(state.Asset.AssetId));
            return new WalletInfo(state.Address, new Asset(coinInfo.Name, coinInfo.Ticker, Asset.Type.Coin), state.Balance, state.MineQuantity);
        }
    }
}