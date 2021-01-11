using Chronos.Coins.Events;
using Chronos.Core;
using Chronos.Core.Commands;
using NodaTime;
using ZES.Infrastructure.Domain;

namespace Chronos.Coins.Sagas
{
    /// <summary>
    /// Asset registration saga
    /// </summary>
    public class AssetSaga : StatelessSaga<AssetSaga.State, AssetSaga.Trigger>
    {
        private string _coinName;
        private string _coinTicker;

        /// <summary>
        /// Initializes a new instance of the <see cref="AssetSaga"/> class.
        /// </summary>
        public AssetSaga()
        {
            Register<CoinCreated>(e => e.Name, Trigger.CoinRegistered, e =>
            {
                _coinName = e.Name;
                _coinTicker = e.Ticker;
            });
        }
        
        /// <summary>
        /// Triggers
        /// </summary>
        public enum Trigger
        {
            CoinRegistered,
        }

        /// <summary>
        /// States
        /// </summary>
        public enum State
        {
            Open,
            Complete,
        }

        /// <inheritdoc/>
        protected override void ConfigureStateMachine()
        {
            base.ConfigureStateMachine();
            StateMachine.Configure(State.Open)
                .Permit(Trigger.CoinRegistered, State.Complete);
            StateMachine.Configure(State.Complete)
                .OnEntry(() =>
                {
                    var forAsset = new Asset(_coinName, _coinTicker, Asset.Type.Coin);
                    var domAsset = new Currency("USD");
                    var command = new RegisterAssetPair(AssetPair.Fordom(forAsset, domAsset), forAsset, domAsset)
                    {
                        Timestamp = new LocalDate(2000, 1, 1).AtMidnight().InUtc().ToInstant(), 
                        UseTimestamp = true,
                    };
                    SendCommand(command);
                });
        }
    }
}