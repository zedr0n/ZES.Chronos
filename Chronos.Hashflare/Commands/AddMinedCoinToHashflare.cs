using ZES.Infrastructure.Domain;
using ZES.Interfaces.Domain;

namespace Chronos.Hashflare.Commands
{
    /// <inheritdoc />
    public class AddMinedCoinToHashflare : Command
    {
        private string _type;

        /// <summary>
        /// Initializes a new instance of the <see cref="AddMinedCoinToHashflare"/> class.
        /// </summary>
        public AddMinedCoinToHashflare() { }

        /// <summary>
        /// Initializes a new instance of the <see cref="AddMinedCoinToHashflare"/> class.
        /// </summary>
        /// <param name="type">Hash type</param>
        /// <param name="quantity">Mined amount</param>
        public AddMinedCoinToHashflare(string type, double quantity) 
            : base("Hashflare") 
        {
            Type = type;
            Quantity = quantity;
        }

        /// <summary>
        /// Gets or sets coin type
        /// </summary>
        public string Type
        {
            get => _type;
            set => _type = value;
        }

        /// <summary>
        /// Gets or sets mined amount 
        /// </summary>
        public double Quantity { get; set; }
    }
}
