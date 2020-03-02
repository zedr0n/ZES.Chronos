using ZES.Infrastructure.Domain;
using ZES.Interfaces.Domain;

namespace Chronos.Hashflare.Commands
{
    /// <inheritdoc />
    public class ExpireContract : Command
    {
        /// <inheritdoc />
        public ExpireContract() { }

        /// <inheritdoc />
        public ExpireContract(string contractId) 
            : base(contractId) 
        {
        }
    }
}
