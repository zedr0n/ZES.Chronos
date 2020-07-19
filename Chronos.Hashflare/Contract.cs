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

        /// <summary>
        /// Initializes a new instance of the <see cref="Contract"/> class.
        /// </summary>
        public Contract()
        {
            Register<ContractCreated>(ApplyEvent);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Contract"/> class.
        /// </summary>
        /// <param name="contractId">Contract identifier</param>
        /// <param name="type">Algo type</param>
        /// <param name="quantity">Hashrate amount</param>
        /// <param name="total">Total price (in USD ) </param>
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