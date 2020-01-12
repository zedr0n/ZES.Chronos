using ZES.Infrastructure.Domain;
using ZES.Interfaces.Domain;

namespace Chronos.Hashflare.Commands
{
    public class CreatePurchaseHandler : CreateCommandHandlerBase<CreatePurchase, Purchase>
    {
        public CreatePurchaseHandler(IEsRepository<IAggregate> repository)
            : base(repository) { }

        protected override Purchase Create(CreatePurchase command) => new Purchase(
            command.Target, command.Type, command.Quantity, command.Total, command.Timestamp);
    }
}