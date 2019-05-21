using System.Threading.Tasks;
using ZES.Interfaces;
using ZES.Interfaces.Domain;

namespace Chronos.Accounts.Commands
{
    public class CreateAccountHandler : ICommandHandler<CreateAccount>
    {
        private readonly IEsRepository<IAggregate> _repository;

        public CreateAccountHandler(IEsRepository<IAggregate> repository)
        {
            _repository = repository;
        }

        public async Task Handle(CreateAccount command)
        {
            var account = new Account(command.Name, command.Currency, command.Type);
            await _repository.Save(account);
        }
    }
}