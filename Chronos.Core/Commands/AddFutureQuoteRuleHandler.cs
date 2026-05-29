using ZES.Infrastructure.Domain;
using ZES.Interfaces.Domain;
using ZES.Interfaces.EventStore;

namespace Chronos.Core.Commands;

/// <inheritdoc />
public class AddFutureQuoteRuleHandler(IEsRepository<IAggregate> repository) : CommandHandlerBase<AddFutureQuoteRule, AssetPair>(repository)
{
    /// <inheritdoc/>
    protected override void Act(AssetPair root, AddFutureQuoteRule command)
    {
        root.AddFutureQuoteRule(command.RuleType, command.Parameters);
    }
}