namespace Chronos.Hashflare.Queries
{
    public class ContractStats
    {
        public ContractStats(string txId, string type = "SHA-256", double mined = 0)
        {
            TxId = txId;
            Type = type;
            Mined = mined;
        }

        public ContractStats()
        {
        }

        public string TxId { get; set; }
        public string Type { get; set; }
        public double Mined { get; set; }
    }
}