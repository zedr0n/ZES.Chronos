using Chronos.Core;
using Newtonsoft.Json;
using ZES.Infrastructure.Domain;
using ZES.Interfaces.Domain;

namespace Chronos.Accounts.Commands;

[method: JsonConstructor]
public class StartTransfer(string txId, string fromAccount, string toAccount, Quantity amount) : Command, ICreateCommand
{
    public string TxId => txId;
    public string FromAccount => fromAccount;
    public string ToAccount => toAccount;
    public Quantity Amount => amount;
    public Quantity Fee { get; set; }

    public override string Target => TxId;
}