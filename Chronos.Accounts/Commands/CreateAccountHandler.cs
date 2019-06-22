using ZES.Infrastructure.Domain;
using ZES.Interfaces.Domain;

namespace Chronos.Accounts.Commands
{
    /// <inheritdoc />
    public class CreateAccountHandler : CreateCommandHandlerBase<CreateAccount, Account>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CreateAccountHandler"/> class.
        /// </summary>
        /// <param name="repository">Aggregate root repository</param>
        public CreateAccountHandler(IEsRepository<IAggregate> repository)
            : base(repository)
        {
        }

        protected override Account Create(CreateAccount command) => new Account(command.Name, command.Type);
    }
}