using Chronos.Hashflare.Events;
using ZES.Infrastructure.Domain;
using ZES.Interfaces.Domain;

namespace Chronos.Hashflare
{
    /// <summary>
    /// Hashflare contract
    /// </summary>
    public class Contract : EventSourced, IAggregate
    {
        private string _type;

        /// <inheritdoc />
        public Contract()
        {
            Register<ContractCreated>(ApplyEvent);
        }

        /// <inheritdoc />
        public Contract(string contractId, string type, int quantity, int total)
        {
            Id = contractId;
            When(new ContractCreated(contractId, type, quantity, total));
        }

        /// <summary>
        /// Add mined coin to contract
        /// </summary>
        /// <param name="quantity">Mined amount</param>
        public void AddAmountMined(double quantity)
        {
           When(new CoinMinedByContract(Id, _type, quantity));
        }

        /// <summary>
        /// Expire the contract
        /// </summary>
        public void Expire()
        {
            When(new ContractExpired(Id));
        }

        private void ApplyEvent(ContractCreated e)
        {
            _type = e.Type;
        }
    }
}