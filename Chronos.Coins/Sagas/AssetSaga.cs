using Chronos.Coins.Events;
using Chronos.Core;
using Chronos.Core.Commands;
using NodaTime;
using ZES.Infrastructure.Domain;
using ZES.Infrastructure.Utils;

namespace Chronos.Coins.Sagas
{
    /// <summary>
    /// Asset registration saga
    /// </summary>
    public class AssetSaga : StatelessSaga<AssetSaga.State, AssetSaga.Trigger>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AssetSaga"/> class.
        /// </summary>
        public AssetSaga()
        {
            RegisterWithParameters<CoinCreated>(e => e.Name, Trigger.CoinRegistered);
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

            var trigger = GetTrigger<CoinCreated>();
            
            StateMachine.Configure(State.Open)
                .Permit(Trigger.CoinRegistered, State.Complete);
            StateMachine.Configure(State.Complete)
                .OnEntryFrom(trigger, Handle);
        }

        private void Handle(CoinCreated e)
        {
            var forAsset = new Asset(e.Name, e.Ticker, Asset.Type.Coin);
            var domAsset = new Currency("USD");
            var command = new RegisterAssetPair(AssetPair.Fordom(forAsset, domAsset), forAsset, domAsset)
            {
                Timestamp = new LocalDate(2000, 1, 1).AtMidnight().InUtc().ToInstant().ToTime(), 
                UseTimestamp = true,
            };
            SendCommand(command);
        }
    }
}