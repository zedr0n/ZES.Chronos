using System.Threading.Tasks;
using ZES.Interfaces;
using ZES.Interfaces.Domain;

namespace Chronos.Accounts.Commands
{
    /// <inheritdoc />
    public class CreateAccountHandler : ICommandHandler<CreateAccount>
    {
        private readonly IEsRepository<IAggregate> _repository;

        /// <summary>
        /// Initializes a new instance of the <see cref="CreateAccountHandler"/> class.
        /// </summary>
        /// <param name="repository">Aggregate root repository</param>
        public CreateAccountHandler(IEsRepository<IAggregate> repository)
        {
            _repository = repository;
        }

        /// <inheritdoc />
        public async Task Handle(CreateAccount command)
        {
            var account = new Account(command.Name, command.Currency, command.Type);
            await _repository.Save(account);
        }
    }
}