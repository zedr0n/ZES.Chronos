using Chronos.Core;
using Newtonsoft.Json;
using ZES.Infrastructure.Domain;

namespace Chronos.Accounts.Events;

[method: JsonConstructor]
public class TransferStarted(string txId, string fromAccount, string toAccount, Quantity amount, Quantity fee): Event
{
    public TransferStarted()
        : this(null, null, null, null, null) { }
    public string TxId => txId;
    public string FromAccount => fromAccount;
    public string ToAccount => toAccount;
    public Quantity Amount => amount;
    public Quantity Fee => fee;
}