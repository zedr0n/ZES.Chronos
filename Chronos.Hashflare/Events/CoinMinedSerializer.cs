using Newtonsoft.Json;
using ZES.Infrastructure.Serialization;

namespace Chronos.Hashflare.Events
{
    public class CoinMinedSerializer : EventSerializerBase<CoinMined>
    {
        public override void Write(JsonTextWriter writer, CoinMined e)
        {
            writer.WritePropertyName(nameof(e.Type));
            writer.WriteValue(e.Type);
            
            writer.WritePropertyName(nameof(e.Quantity));
            writer.WriteValue(e.Quantity);
        }
    }
}