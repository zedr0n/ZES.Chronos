using Newtonsoft.Json;
using ZES.Infrastructure.Serialization;

namespace Chronos.Hashflare.Events
{
    /// <inheritdoc />
    public class CoinMinedByContractDeserializer : EventDeserializerBase<CoinMinedByContract>
    {
        /// <inheritdoc />
        public override string EventType => nameof(CoinMinedByContract);

        /// <inheritdoc />
        public override void Switch(JsonTextReader reader, string currentProperty, CoinMinedByContract e)
        {
            switch (reader.TokenType)
            {
                case JsonToken.String when currentProperty == nameof(CoinMinedByContract.ContractId):
                    e.ContractId = (string)reader.Value;
                    break;
                case JsonToken.String when currentProperty == nameof(CoinMinedByContract.Type):
                    e.Type = (string)reader.Value;
                    break;
                case JsonToken.Float when currentProperty == nameof(CoinMinedByContract.Quantity):
                    e.Quantity = (double)reader.Value;
                    break;
            }
        }
    }
}