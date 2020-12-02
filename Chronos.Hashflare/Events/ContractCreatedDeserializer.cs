using Newtonsoft.Json;
using ZES.Infrastructure.Serialization;

namespace Chronos.Hashflare.Events
{
    /// <inheritdoc />
    public class ContractCreatedDeserializer : EventDeserializerBase<ContractCreated>
    {
        /// <inheritdoc />
        public override string EventType => nameof(ContractCreated);

        /// <inheritdoc />
        public override void Switch(JsonTextReader reader, string currentProperty, ContractCreated e)
        {
            switch (reader.TokenType)
            {
                case JsonToken.String when currentProperty == nameof(ContractCreated.ContractId):
                    e.ContractId = (string)reader.Value;
                    break;
                case JsonToken.String when currentProperty == nameof(ContractCreated.Type):
                    e.Type = (string)reader.Value;
                    break;
                case JsonToken.Integer when currentProperty == nameof(ContractCreated.Quantity):
                    e.Quantity = (int)(long)reader.Value;
                    break;
                case JsonToken.Float when currentProperty == nameof(ContractCreated.Total):
                    e.Total = (double)reader.Value;
                    break;
            }
        }
    }
}