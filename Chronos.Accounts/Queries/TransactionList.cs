using System.Collections.Generic;
using Chronos.Core;
using Chronos.Core.Queries;

namespace Chronos.Accounts.Queries;

public class TransactionList()
{
    public List<string> TxId { get; set; } = new();
    public List<TransactionInfo> Infos { get; set; } = new();
}