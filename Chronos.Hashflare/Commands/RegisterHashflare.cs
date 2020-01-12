using ZES.Infrastructure.Domain;
using ZES.Interfaces.Domain;

namespace Chronos.Hashflare.Commands
{
    public class RegisterHashflare : Command, ICreateCommand   
    {
        public RegisterHashflare() { }
        public RegisterHashflare(string username, long timestamp)
            : base("Hashflare")
        {
            Username = username;
            Timestamp = timestamp;
        }
        
        public string Username { get; }
        public long Timestamp { get; }
    }
}
