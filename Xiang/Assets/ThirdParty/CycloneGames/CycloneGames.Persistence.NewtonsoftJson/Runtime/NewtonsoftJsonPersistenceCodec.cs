using System;
using System.Buffers;
using System.Globalization;
using System.Text;
using CycloneGames.Persistence;
using Newtonsoft.Json;

namespace CycloneGames.Persistence.NewtonsoftJson
{
    public sealed class NewtonsoftJsonPersistenceCodec<T> : IPersistenceCodec<T>
    {
        private static readonly PersistenceCodecId StableCodecId =
            new PersistenceCodecId("json/1");

        private static readonly Encoding Utf8WithoutBom =
            new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);

        private readonly JsonSerializerSettings _settings;

        public NewtonsoftJsonPersistenceCodec(JsonSerializerSettings settings = null)
        {
            _settings = settings ?? CreateSafeDefaultSettings();
            ValidateSettings(_settings);
        }

        public PersistenceCodecId CodecId => StableCodecId;
        public JsonSerializerSettings Settings => _settings;

        public void Serialize(
            in T value,
            IBufferWriter<byte> destination,
            in PersistenceWriteContext context)
        {
            if (destination == null)
            {
                throw new ArgumentNullException(nameof(destination));
            }

            context.CancellationToken.ThrowIfCancellationRequested();

            // SerializeObject materializes the whole string. This is acceptable on the save cold path
            // (user-triggered, app pause, or focus loss); a single save is far below the 1 MiB budget.
            string json = JsonConvert.SerializeObject(value, _settings);
            if (string.IsNullOrEmpty(json))
            {
                return;
            }

            byte[] bytes = Utf8WithoutBom.GetBytes(json);
            int offset = 0;
            while (offset < bytes.Length)
            {
                context.CancellationToken.ThrowIfCancellationRequested();
                int remaining = bytes.Length - offset;
                Span<byte> span = destination.GetSpan(remaining);
                int count = Math.Min(span.Length, remaining);
                bytes.AsSpan(offset, count).CopyTo(span);
                destination.Advance(count);
                offset += count;
            }
        }

        public T Deserialize(
            ReadOnlyMemory<byte> payload,
            in PersistenceReadContext context)
        {
            context.CancellationToken.ThrowIfCancellationRequested();

            if (payload.IsEmpty)
            {
                return default;
            }

            string json = Utf8WithoutBom.GetString(payload.Span);
            return JsonConvert.DeserializeObject<T>(json, _settings);
        }

        public static JsonSerializerSettings CreateSafeDefaultSettings()
        {
            return new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.None,
                Culture = CultureInfo.InvariantCulture,
                FloatParseHandling = FloatParseHandling.Double,
                FloatFormatHandling = FloatFormatHandling.String,
                MaxDepth = 64,
            };
        }

        private static void ValidateSettings(JsonSerializerSettings settings)
        {
            if (settings.TypeNameHandling != TypeNameHandling.None
                && settings.SerializationBinder == null)
            {
                throw new ArgumentException(
                    "Enabling Newtonsoft TypeNameHandling requires an ISerializationBinder allowlist. " +
                    "Type-name handling is a deserialization RCE risk and must be limited to internally " +
                    "trusted archives with an explicit type allowlist.",
                    nameof(settings));
            }
        }
    }
}
