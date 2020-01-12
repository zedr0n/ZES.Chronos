using System.Collections.Generic;
using Chronos.Hashflare.Commands;
using Chronos.Hashflare.Events;
using Stateless;
using ZES.Infrastructure.Sagas;

namespace Chronos.Hashflare.Sagas
{
    public class RatioSaga : StatelessSaga<RatioSaga.State, RatioSaga.Trigger>
    {
        private readonly Dictionary<string, Contract> _contracts = new Dictionary<string, Contract>();

        private int _bitcoinHashRate;
        private int _scryptHashRate;
        private long _timestamp;
        
        public RatioSaga()
        {
            Register<HashrateBought>(e => "RatioSaga", Trigger.ContractCreated, e =>
            {
                if (e.Type == "SHA-256")
                    _bitcoinHashRate += e.Quantity;
                else
                    _scryptHashRate -= e.Quantity;

                _contracts[e.TxId] = new Contract(e.Type, e.Quantity);
                _timestamp = e.Timestamp;
            });
            Register<ContractExpired>(e => "RatioSaga", Trigger.ContractExpired, e =>
            {
                if (e.Type == "SHA-256")
                    _bitcoinHashRate -= e.Quantity;
                else
                    _scryptHashRate -= e.Quantity;

                _contracts[e.TxId] = new Contract(e.Type, 0);
                _timestamp = e.Timestamp;
            });
            Register<ContractRatioAdjusted>(e => "RatioSaga", Trigger.AdjustmentComplete);
        }
        
        public enum Trigger
        {
            ContractCreated,
            ContractExpired,
            AdjustmentComplete
        }
        
        public enum State 
        {
            Open,
            Adjusting
        }

        protected override void ConfigureStateMachine()
        {
            StateMachine = new StateMachine<State, Trigger>(State.Open);

            StateMachine.Configure(State.Open)
                .Permit(Trigger.ContractCreated, State.Adjusting)
                .Permit(Trigger.ContractExpired, State.Adjusting);

            StateMachine.Configure(State.Adjusting)
                .Permit(Trigger.AdjustmentComplete, State.Open)
                .PermitReentry(Trigger.ContractCreated)
                .PermitReentry(Trigger.ContractExpired);

            StateMachine.Configure(State.Adjusting)
                .OnEntry(() =>
                {
                    foreach (var c in _contracts)
                    {
                        double ratio = c.Value.Quantity;
                        
                        var totalHash = c.Value.Type == "SHA-256" ? _bitcoinHashRate : _scryptHashRate;
                        if (totalHash > 0)
                            ratio /= totalHash;
                        else
                            ratio = 0.0;
                        SendCommand(new AdjustRatioForContract(c.Key, ratio, _timestamp));
                    }
                });
            
            base.ConfigureStateMachine();
        }

        private class Contract
        {
            public Contract(string type, int quantity)
            {
                Type = type;
                Quantity = quantity;
            }

            public string Type { get; }
            public int Quantity { get; }
        }
    }
}