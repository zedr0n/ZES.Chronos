using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Chronos.Core;
using Chronos.Core.Json;
using ZES.Infrastructure.Alerts;
using ZES.Infrastructure.Domain;
using ZES.Infrastructure.Net;
using ZES.Interfaces.Domain;
using ZES.Interfaces.EventStore;
using ZES.Interfaces.Infrastructure;

namespace Chronos.Coins.Commands
{
    /// <summary>
    /// Daily mining Hycon parser
    /// </summary>
    public class UpdateDailyMiningHyconHandler : UpdateDailyMiningHandler
    {
        private readonly IMessageQueue _messageQueue;
        private readonly ILog _log;
        
        private readonly ICommandHandler<RequestJson<AddressInfo>> _addressHandler;
        private readonly ICommandHandler<RequestJson<BlockListV2>> _blockListV2Handler;
        private readonly ICommandHandler<RequestJson<BlockInfoV2>> _blockInfoV2Handler;
        private readonly ICommandHandler<RequestJson<MinedResults>> _handler;

        private string _firstBlock;
        private string _remoteFirstBlock;
        private string _server;

        /// <summary>
        /// Handles the process of updating daily mining data for Hycon.
        /// </summary>
        /// <param name="repository">Aggregate repository</param>
        /// <param name="addressHandler">Handler for processing JSON requests related to address information</param>
        /// <param name="messageQueue">Service for managing message queues</param>
        /// <param name="handler">Handler for processing mined results as JSON</param>
        /// <param name="mineHandler">Handler for executing commands to modify balance based on mining</param>
        /// <param name="blockListV2Handler">Handler for processing JSON requests related to block lists</param>
        /// <param name="blockInfoV2Handler">Handler for processing JSON requests related to block information</param>
        /// <param name="log">Logging service</param>
        /// <param name="flowCompletionService">Service to manage flow completions</param>
        public UpdateDailyMiningHyconHandler(IEsRepository<IAggregate> repository,
            ICommandHandler<RequestJson<AddressInfo>> addressHandler, IMessageQueue messageQueue,
            ICommandHandler<RequestJson<MinedResults>> handler,
            ICommandHandler<RetroactiveCommand<MineCoin>> mineHandler,
            ICommandHandler<RequestJson<BlockListV2>> blockListV2Handler,
            ICommandHandler<RequestJson<BlockInfoV2>> blockInfoV2Handler, ILog log,
            IFlowCompletionService flowCompletionService)
            : base(repository, mineHandler, flowCompletionService)
        {
            _addressHandler = addressHandler;
            _messageQueue = messageQueue;
            _handler = handler;
            _blockListV2Handler = blockListV2Handler;
            _blockInfoV2Handler = blockInfoV2Handler;
            _log = log;
        }

        /// <inheritdoc/>
        protected override async Task<IEnumerable<MinedBlock>> GetMinedBlocks(UpdateDailyMining command)
        {
            if (!Api.TryGetServer(command.UseRemote, out _server))
                return null;

            List<MinedBlock> minedBlocks = null;
            if (command.UseV2)
            {
                minedBlocks = new List<MinedBlock>();
                var count = command.Count / 20;
                for (var i = 0; i < count; ++i)
                {
                    var url = GetBlockListV2Url(command.Index - (20 * i));
                    var obsList = _messageQueue.Alerts.OfType<JsonRequestCompleted<BlockListV2>>().Replay();
                    obsList.Connect();
                    await _blockListV2Handler.Handle(new RequestJson<BlockListV2>(command.Target, url));

                    var res = await obsList.FirstOrDefaultAsync(r => r.RequestorId == command.Target).Timeout(TimeSpan.FromMinutes(1));
                    if (res.Data == null)
                        throw new InvalidOperationException();

                    if (res.Data.AsList().All(b => b.Miner != command.Target))
                        continue;

                    minedBlocks.AddRange(res.Data.AsList().Where(b => b.Miner == command.Target && b.IsMain).Select(b => new MinedBlock
                    {
                        Blockhash = b.Blockhash,
                        Feereward = 120,
                        Miner = b.Miner,
                        Timestamp = b.Blocktimestamp,
                    }));

                    foreach (var blockv2 in res.Data.AsList().Where(b => b.UncleCount > 0))
                    {
                        var blockInfoUrl = GetBlockInfoV2Url(blockv2.Height); 
                        
                        var obsInfo = _messageQueue.Alerts.OfType<JsonRequestCompleted<BlockInfoV2>>().Replay();
                        obsInfo.Connect();
 
                        await _blockInfoV2Handler.Handle(new RequestJson<BlockInfoV2>(command.Target, blockInfoUrl));

                        var blockInfo = await obsInfo.FirstOrDefaultAsync(r => r.RequestorId == command.Target).Timeout(TimeSpan.FromMinutes(1));
                        if (blockInfo.Data == null)
                            throw new InvalidOperationException();

                        if (blockInfo.Data.Uncles.All(u => u.Miner != command.Address))
                            continue;
                        
                        minedBlocks.AddRange(blockInfo.Data.Uncles.Where(u => u.Miner == command.Address).Select(u => new MinedBlock
                        {
                            Blockhash = u.UncleHash,
                            Feereward = 120 * 1.2 * Math.Pow(0.75, u.Depth),
                            Miner = u.Miner,
                            Timestamp = u.UncleTimeStamp,
                        }));
                    }
                }
            }
            else
            {
                var url = await GetMinedUrl(command.Address, command.Index, command.UseRemote);
                if (url == null)
                    return null;

                var obs = _messageQueue.Alerts.OfType<JsonRequestCompleted<MinedResults>>().Replay();
                obs.Connect();
                await _handler.Handle(new RequestJson<MinedResults>(command.Target, url));
                // var res = await _messageQueue.Alerts.OfType<JsonRequestCompleted<MinedResults>>().FirstOrDefaultAsync(r => r.RequestorId == command.Target).Timeout(TimeSpan.FromMinutes(1));
                var res = await obs.FirstOrDefaultAsync(r => r.RequestorId == command.Target).Timeout(TimeSpan.FromMinutes(1));
                if (res.Data == null)
                    throw new InvalidOperationException();
                minedBlocks = res.Data.AsList();
            }

            return minedBlocks;
        }

        private async Task<string> GetMinedUrl(string address, int index, bool useRemote)
        {
            if (!useRemote && _firstBlock == null)
                _firstBlock = await GetFirstBlock(address);
            if (useRemote && _remoteFirstBlock == null)
                _remoteFirstBlock = await GetFirstBlock(address);
        
            var firstBlock = useRemote ? _remoteFirstBlock : _firstBlock;
            if (string.IsNullOrEmpty(firstBlock))
            {
                _log.Warn("No mined blocks received");
                return null;
            }

            var url = $"http://{_server}/api/v1/getMinedInfo/{address}/{firstBlock ?? string.Empty}/{index}";
            return url;
        }

        private string GetAddressInfoUrl(string address)
        {
            var url = $"http://{_server}/api/v1/address/{address}";
            return url;
        }

        private string GetBlockInfoV2Url(int blockHeight)
        {
            var url = $"http://{_server}/api/v2/block/height/{blockHeight}";
            return url;
        }

        private string GetBlockListV2Url(int blockHeight)
        {
            var url = $"http://{_server}/api/v2/blockList/{blockHeight}";
            return url;
        }

        private async Task<string> GetFirstBlock(string address)
        {
            var url = GetAddressInfoUrl(address);
            
            var obs = _messageQueue.Alerts.OfType<JsonRequestCompleted<AddressInfo>>().Replay();
            obs.Connect();
            
            await _addressHandler.Handle(new RequestJson<AddressInfo>(address, url));
      
            var res = await obs.FirstOrDefaultAsync(r => r.RequestorId == address).Timeout(TimeSpan.FromMinutes(1));
            if (res.Data == null)
                throw new InvalidOperationException();

            return res.Data.MinedBlocks.FirstOrDefault()?.Blockhash;
        }
    }
}