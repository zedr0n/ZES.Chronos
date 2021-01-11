using Newtonsoft.Json;
using ZES.Infrastructure.Serialization;

namespace Chronos.Hashflare.Events
{
    /// <summary>
    /// CoinMined serialize
    /// </summary>
    public class CoinMinedByContractSerializer : EventSerializerBase<CoinMinedByContract>
    {
        /// <inheritdoc/>
        public override void Write(JsonTextWriter writer, CoinMinedByContract e)
        {
           writer.WritePropertyName(nameof(CoinMinedByContract.Type));
           writer.WriteValue(e.Type);
           
           writer.WritePropertyName(nameof(CoinMinedByContract.Quantity));
           writer.WriteValue(e.Quantity);
        }
    }
}