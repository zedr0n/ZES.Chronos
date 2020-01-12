using ZES.Infrastructure.Domain;
using ZES.Interfaces.Domain;

namespace Chronos.Hashflare.Commands
{
    public class AdjustRatioForContract : Command
    {
        public AdjustRatioForContract() { }
        public AdjustRatioForContract(string txId, double ratio, long timestamp) 
            : base(txId) 
        {
            Ratio = ratio;
            Timestamp = timestamp; 
        }

        public double Ratio { get; }
    }
}
