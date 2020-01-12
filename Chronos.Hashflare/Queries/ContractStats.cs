namespace Chronos.Hashflare.Queries
{
    public class ContractStats
    {
        public ContractStats(string txId, double ratio)
            : this(txId, "SHA-256", ratio) { }
        public ContractStats(string txId, string type, double ratio)
        {
            TxId = txId;
            Type = type;
            Ratio = ratio;
        }

        public ContractStats()
        {
        }

        public string TxId { get; set; }
        public string Type { get; set; }
        public double Ratio { get; set; }
    }
}