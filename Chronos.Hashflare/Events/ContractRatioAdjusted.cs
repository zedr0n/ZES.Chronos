using ZES.Infrastructure.Domain;

namespace Chronos.Hashflare.Events
{
    public class ContractRatioAdjusted : Event
    {
        public ContractRatioAdjusted(string txId, double ratio, long timestamp)
        {
            TxId = txId;
            Ratio = ratio;
            Timestamp = timestamp;
        }

        public string TxId { get; }
        public double Ratio { get; }
    }
}