namespace Chronos.Coins.Queries
{
    public class Stats
    {
        public Stats(int numberOfCoins)
        {
            NumberOfCoins = numberOfCoins;
        }

        public int NumberOfCoins { get; set; }    
    }
}