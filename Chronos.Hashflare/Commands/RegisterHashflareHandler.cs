using ZES.Infrastructure.Domain;
using ZES.Interfaces.Domain;

namespace Chronos.Hashflare.Commands
{
    public class RegisterHashflareHandler : CreateCommandHandlerBase<RegisterHashflare, Hashflare>
    {
        public RegisterHashflareHandler(IEsRepository<IAggregate> repository)
            : base(repository) { }

        protected override Hashflare Create(RegisterHashflare command) => new Hashflare(command.Target, command.Username, command.Timestamp);
    }
}