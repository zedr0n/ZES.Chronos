using Newtonsoft.Json;
using ZES.Infrastructure.Serialization;

namespace Chronos.Hashflare.Events
{
    /// <inheritdoc />
    public class CoinMinedDeserializer : EventDeserializerBase<CoinMined>
    {
        /// <inheritdoc />
        public override string EventType => nameof(CoinMined);

        /// <inheritdoc />
        public override void Switch(JsonTextReader reader, string currentProperty, CoinMined e)
        {
            switch (reader.TokenType)
            {
                case JsonToken.String when currentProperty == nameof(CoinMined.Type):
                    e.Type = (string)reader.Value;
                    break;
                case JsonToken.Float when currentProperty == nameof(CoinMined.Quantity):
                    e.Quantity = (double)reader.Value;
                    break;
            }            
        }
    }
}