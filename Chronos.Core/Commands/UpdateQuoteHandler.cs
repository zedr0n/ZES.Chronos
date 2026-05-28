using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using ZES.Infrastructure.Utils;
using ZES.Interfaces.Clocks;
using ZES.Interfaces.Domain;
using ZES.Interfaces.EventStore;
using IClock = ZES.Interfaces.Clocks.IClock;
namespace Chronos.Core.Commands;

  /// <inheritdoc />
[SuppressMessage("StyleCop.CSharp.SpacingRules", "SA1010:Opening square brackets should be spaced correctly", Justification = "[] issue")]
public class UpdateQuoteHandler : ZES.Infrastructure.Domain.CommandHandlerBase<UpdateQuote, AssetPair>
{
    private readonly IEsRepository<IAggregate> _repository;
    private readonly IQuoteDataResolver _quoteDataResolver;
    private readonly ICommandHandler<AddQuote> _handler;
    private readonly IClock _clock;

    /// <summary>
    /// Initializes a new instance of the <see cref="UpdateQuoteHandler"/> class.
    /// Handles the `UpdateQuote` command by updating quotes of a specific asset pair
    /// and invoking additional sub-commands and handlers as required.
    /// </summary>
    /// <param name="repository">The aggregate repository used for retrieving and managing asset pair aggregates.</param>
    /// <param name="quoteDataResolver">Resolves quote data required for the quote update process.</param>
    /// <param name="handler">Handles the `AddQuote` command to add new quotes as part of the update operation.</param>
    /// <param name="clock">Provides time-related operations necessary for processing quote updates.</param>
    /// <remarks>
    /// Ensures the proper processing and validation of quote updates, preventing duplicate updates for the same date
    /// and distinguishing between intraday and non-intraday updates.
    /// </remarks>
    public UpdateQuoteHandler(
    IEsRepository<IAggregate> repository,
    IQuoteDataResolver quoteDataResolver,
    ICommandHandler<AddQuote> handler,
    IClock clock)
    : base(repository)
    {
      _repository = repository;
      _quoteDataResolver = quoteDataResolver;
      _handler = handler;
      _clock = clock;
    }

    /// <inheritdoc/>
    public override async Task Handle(UpdateQuote command)
    {
        var assetPair = await _repository.Find<AssetPair>(command.Target);
        if (assetPair == null)
          throw new ArgumentNullException(nameof(AssetPair));

        var quoteDates = assetPair?.QuoteDates?.ToHashSet() ?? [];
        var holidayCalendar = assetPair.HolidayCalendar;

        var timestamp = command.Timestamp;
        if (timestamp == null)
          throw new InvalidOperationException("Timestamp cannot be null");
        
        var now = _clock.GetCurrentInstant();
        var intraday = timestamp.Date() == now.Date();
        var supportsIntraday = assetPair.SupportsIntraday;
        
        // handle intraday and holidays
        if ((intraday && !assetPair.SupportsIntraday) || !timestamp.IsWorkingDay(holidayCalendar))
        {
          intraday = false;
          timestamp = timestamp.PreviousWorkingDay(holidayCalendar).CloseOfDay(); 
        }
        
        var effectiveQuoteDates = quoteDates?
          .GroupBy(d => d.RollToWorkingDay(holidayCalendar).Date())
          .ToDictionary(g => g.Key, g => g.ToList()) ?? [];
        
        if (QuoteExtensions.HasExistingQuote(timestamp.ToInstant(), quoteDates, effectiveQuoteDates, intraday, supportsIntraday, holidayCalendar))
          throw new InvalidOperationException($"Valid quote already exists for {timestamp.Date()}");
        
        // filter out additional timestamps that are already present in the quote dates
        var additionalTimestamps = command.AdditionalTimestamps?
          .Where(t => !QuoteExtensions.HasExistingQuote(t.ToInstant(), quoteDates, effectiveQuoteDates, false, supportsIntraday, holidayCalendar)).ToList();
        
        if (additionalTimestamps != null && holidayCalendar != null)
        {
          additionalTimestamps = additionalTimestamps
            .Select(t => !t.IsWorkingDay(holidayCalendar) ? t.PreviousWorkingDay(holidayCalendar).CloseOfDay() : t)
            .Distinct().Where(t => t != timestamp)
            .ToList();
        }

        if (!command.Ephemeral && additionalTimestamps is { Count: > 0 })
          throw new InvalidOperationException("Additional timestamps can only be specified for ephemeral quote updates");
        
        if (intraday && additionalTimestamps is { Count: > 0 })
        {
          await Handle(command, assetPair, timestamp, null);
          
          timestamp = additionalTimestamps.Max();
          additionalTimestamps = additionalTimestamps.Where(t => t != timestamp).OrderBy(t => t).ToList();
          intraday = false;
        }

        await Handle(command, assetPair, timestamp, additionalTimestamps); 
    }

    /// <inheritdoc/>
    protected override void Act(AssetPair root, UpdateQuote command)
    {
        throw new NotImplementedException();
    }

    private async Task Handle(UpdateQuote command, AssetPair assetPair, Time timestamp, IEnumerable<Time> additionalTimestamps)
    {
        var ticker = assetPair.Ticker;
        if (ticker == null)
          throw new InvalidOperationException($"Ticker is not set for {assetPair.Id}");

        var timestamps = new List<Time>();
        if (additionalTimestamps != null)
          timestamps.AddRange(additionalTimestamps);
        timestamps.Add(timestamp);

        var request = new QuoteDataRequest(assetPair.ForAsset, assetPair.DomAsset, timestamps, ticker, assetPair.Url, command.EnforceCache, command.UseBatching);
        var values = await _quoteDataResolver.ResolveValues(request);
        foreach (var (t, value) in values.OrderBy(x => x.Key))
          await AddQuote(command, t, value);
    }
    
    private async Task AddQuote(UpdateQuote command, Time timestamp, double value)
    {
        var addQuoteCommand = new AddQuote(command.Target, timestamp.ToInstant(), value)
        {
          StoreInLog = false,
          CorrelationId = command.CorrelationId,
          AncestorId = command.AncestorId ?? command.MessageId,
        };
        
        if (command.Ephemeral)
        {
          addQuoteCommand.Ephemeral = true;
          addQuoteCommand.Timestamp = timestamp;
        }

        await _handler.Handle(addQuoteCommand);
    }
}
