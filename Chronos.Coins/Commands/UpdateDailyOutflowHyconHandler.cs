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
using ZES.Interfaces;
using ZES.Interfaces.Domain;
using ZES.Interfaces.Pipes;

namespace Chronos.Coins.Commands
{
    /// <summary>
    /// Daily transaction hycon handler
    /// </summary>
    public class UpdateDailyOutflowHyconHandler : UpdateDailyOutflowHandler
    {
        private readonly ICommandHandler<RequestJson<TxResults>> _handler;
        private readonly ICommandHandler<RequestJson<AddressInfo>> _addressHandler;
        private readonly ICommandHandler<RequestJson<BlockListV2>> _blockListV2Handler;
        private readonly ICommandHandler<RequestJson<BlockInfoV2>> _blockInfoV2Handler;

        private readonly ILog _log;
        private readonly IMessageQueue _messageQueue;

        private string _firstTx;
        private string _remoteFirstTx;

        private string _server;
        
        /// <summary>
        /// Initializes a new instance of the <see cref="UpdateDailyOutflowHyconHandler"/> class.
        /// </summary>
        /// <param name="repository">Aggregate repository</param>
        /// <param name="handler">Tx list JSON handler</param>
        /// <param name="messageQueue">Messaging service</param>
        /// <param name="balanceHandler">Wallet balance change handler</param>
        /// <param name="addressHandler">Address JSON handler</param>
        /// <param name="blockListV2Handler">Block list JSON handler</param>
        /// <param name="blockInfoV2Handler">Block info JSON handler</param>
        /// <param name="log">Logging service</param>
        public UpdateDailyOutflowHyconHandler(IEsRepository<IAggregate> repository, ICommandHandler<RequestJson<TxResults>> handler, IMessageQueue messageQueue, ICommandHandler<RetroactiveCommand<ChangeWalletBalance>> balanceHandler, ICommandHandler<RequestJson<AddressInfo>> addressHandler, ICommandHandler<RequestJson<BlockListV2>> blockListV2Handler, ICommandHandler<RequestJson<BlockInfoV2>> blockInfoV2Handler, ILog log) 
          : base(repository, balanceHandler)
        {
          _handler = handler;
          _messageQueue = messageQueue;
          _addressHandler = addressHandler;
          _blockListV2Handler = blockListV2Handler;
          _blockInfoV2Handler = blockInfoV2Handler;
          _log = log;
        }

        /// <inheritdoc/>
        protected override async Task<IEnumerable<Tx>> GetTransactions(UpdateDailyOutflow command)
        {
          if (!Api.TryGetServer(command.UseRemote, out _server))
            return null;
          
          List<Tx> txResults = null;
          if (command.UseV2)
          {
            txResults = new List<Tx>();
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

              foreach (var blockv2 in res.Data.Where(b => b.TxCount > 0))
              {
                var blockInfoUrl = GetBlockInfoV2Url(blockv2.Height); 
                var obsInfo = _messageQueue.Alerts.OfType<JsonRequestCompleted<BlockInfoV2>>().Replay();
                obsInfo.Connect();

                await _blockInfoV2Handler.Handle(new RequestJson<BlockInfoV2>(command.Target, blockInfoUrl));

                var blockInfo = await obsInfo.FirstOrDefaultAsync(r => r.RequestorId == command.Target).Timeout(TimeSpan.FromMinutes(1));
                if (blockInfo.Data == null)
                  throw new InvalidOperationException();

                if (blockInfo.Data.Txs.All(t => t.From != command.Target && t.To != command.Target))
                  continue;
              
                txResults.AddRange(blockInfo.Data.Txs.Where(t => t.From == command.Target || t.To == command.Target).Select(t => new Tx
                {
                  Amount = t.Amount / 1000000000,
                  From = t.From,
                  To = t.To,
                  Hash = t.TxHash,
                  ReceiveTime = t.BlockTimeStamp,
                }));
              }
            }
          }
          else
          {
            var url = await GetTxUrl(command.Address, command.Index, command.UseRemote);
            if (url == null)
            {
              _log.Warn($"No transactions for {command.Address}");
              return null;
            }
            
            var obs = _messageQueue.Alerts.OfType<JsonRequestCompleted<TxResults>>().Replay();
            obs.Connect();

            await _handler.Handle(new RequestJson<TxResults>(command.Target, url));
            var res = await obs.FirstOrDefaultAsync(r => r.RequestorId == command.Target).Timeout(TimeSpan.FromMinutes(1));
            if (res.Data == null)
              throw new InvalidOperationException();

            txResults = res.Data.AsList();
          }

          return txResults;
        }
        
        private async Task<string> GetTxUrl(string address, int index, bool useRemote)
        {
          if (!useRemote && _firstTx == null)
            _firstTx = await GetFirstTx(address);
          if (useRemote && _remoteFirstTx == null)
            _remoteFirstTx = await GetFirstTx(address);

          var firstTx = useRemote ? _remoteFirstTx : _firstTx;
          if (string.IsNullOrEmpty(firstTx))
            return null;
      
          var url = $"http://{_server}/api/v1/nextTxs/{address}/{firstTx}/{index}";
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
    
        private string GetAddressInfoUrl(string address)
        {
          var url = $"http://{_server}/api/v1/address/{address}";
          return url;
        }
    
        private async Task<string> GetFirstTx(string address)
        {
          var url = GetAddressInfoUrl(address);
          var obs = _messageQueue.Alerts.OfType<JsonRequestCompleted<AddressInfo>>().Replay();
          obs.Connect();

          await _addressHandler.Handle(new RequestJson<AddressInfo>(address, url));
      
          var res = await obs.FirstOrDefaultAsync(r => r.RequestorId == address).Timeout(TimeSpan.FromMinutes(1));
          if (res.Data == null)
            throw new InvalidOperationException();

          return res.Data.Txs.First().Hash;
        }
    }
}