using System.Threading;
using ZES.Interfaces.Domain;

namespace Chronos.Coins.Queries
{
    public class Stats : IState
    {
        private int _numberOfCoins;
        public Stats() { }

        public int NumberOfCoins => _numberOfCoins;

        public void Increment()
        {
            Interlocked.Increment(ref _numberOfCoins);
        }
    }
}