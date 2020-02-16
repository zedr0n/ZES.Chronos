namespace Chronos.Hashflare.Queries
{
    public class ContractStats
    {
        public ContractStats(string txId, double ratio)
            : this(txId, "SHA-256", ratio, 0) { }
        public ContractStats(string txId, string type, double ratio, double mined)
        {
            TxId = txId;
            Type = type;
            Ratio = ratio;
            Mined = mined;
        }

        public ContractStats()
        {
        }

        public string TxId { get; set; }
        public string Type { get; set; }
        public double Ratio { get; set; }
        public double Mined { get; set; }
    }
}