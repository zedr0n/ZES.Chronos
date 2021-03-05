using Newtonsoft.Json;
using ZES.Infrastructure.Serialization;

namespace Chronos.Hashflare.Events
{
    /// <inheritdoc />
    public class HashflareRegisteredDeserializer : EventDeserializerBase<HashflareRegistered>
    {
        /// <inheritdoc />
        public override void Switch(JsonTextReader reader, string currentProperty, HashflareRegistered e)
        {
            switch (reader.TokenType)
            {
                case JsonToken.String when currentProperty == nameof(HashflareRegistered.Username):
                    e.Username = (string)reader.Value;
                    break;
            }
        }
    }
}