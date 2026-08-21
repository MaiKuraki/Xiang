using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using CycloneGames.Persistence;
using CycloneGames.Persistence.NewtonsoftJson;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using NUnit.Framework;

namespace CycloneGames.Persistence.Tests.NewtonsoftJson
{
    public sealed class NewtonsoftJsonPersistenceCodecTests
    {
        [Test]
        public void CodecId_IsJsonVersion1()
        {
            var codec = new NewtonsoftJsonPersistenceCodec<SaveData>();

            Assert.That(codec.CodecId, Is.EqualTo(new PersistenceCodecId("json/1")));
            Assert.That(codec.CodecId.Value, Is.EqualTo("json/1"));
        }

        [Test]
        public void CodecId_RejectsInvalidIdentifiers()
        {
            Assert.Throws<ArgumentException>(() => new PersistenceCodecId("Json/1"));
            Assert.Throws<ArgumentException>(() => new PersistenceCodecId("json"));
            Assert.Throws<ArgumentException>(() => new PersistenceCodecId("json//1"));
        }

        [Test]
        public void DefaultSettings_DisableTypeNameHandling_AndUseInvariantCulture()
        {
            var codec = new NewtonsoftJsonPersistenceCodec<SaveData>();

            Assert.That(codec.Settings.TypeNameHandling, Is.EqualTo(TypeNameHandling.None));
            Assert.That(codec.Settings.SerializationBinder, Is.Null);
            Assert.That(codec.Settings.Culture, Is.EqualTo(CultureInfo.InvariantCulture));
        }

        [Test]
        public void Constructor_RejectsTypeNameHandlingWithoutAllowlist()
        {
            var settings = new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.Objects
            };

            Assert.Throws<ArgumentException>(
                () => new NewtonsoftJsonPersistenceCodec<SaveData>(settings));
        }

        [Test]
        public void Constructor_AllowsTypeNameHandlingWithAllowlistBinder()
        {
            var settings = new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.All,
                SerializationBinder = new DenyAllBinder()
            };

            Assert.DoesNotThrow(() => new NewtonsoftJsonPersistenceCodec<SaveData>(settings));
        }

        [Test]
        public void DefaultSettings_DoNotResolveTypeNameFromUntrustedPayload()
        {
            var codec = new NewtonsoftJsonPersistenceCodec<GadgetProbe>();
            const string malicious =
                "{ \"$type\": \"System.Diagnostics.Process, System\", \"Score\": 7 }";

            GadgetProbe result = JsonConvert.DeserializeObject<GadgetProbe>(
                malicious,
                codec.Settings);

            Assert.That(result, Is.Not.Null);
            Assert.That(result.GetType(), Is.EqualTo(typeof(GadgetProbe)));
            Assert.That(result.Score, Is.EqualTo(7));
        }

        [Test]
        public async Task SaveAndLoad_RoundTripsNestedValue()
        {
            using (var directory = new TemporaryDirectory())
            {
                var store = NewtonsoftJsonPersistenceStoreFactory.CreateStore<SaveData>(
                    directory.GetPath("save.json"));
                SaveData value = CreateRepresentativeValue();

                PersistenceOperationResult save = await store.SaveAsync(in value, contentVersion: 3);
                PersistenceLoadResult<SaveData> load = await store.LoadAsync(
                    maximumSupportedContentVersion: 3);

                Assert.That(save.IsSuccess, Is.True);
                Assert.That(load.IsSuccess, Is.True);
                Assert.That(load.ContentVersion, Is.EqualTo(3));
                Assert.That(load.Value.Score, Is.EqualTo(value.Score));
                Assert.That(load.Value.Speed, Is.EqualTo(value.Speed));
                Assert.That(load.Value.IsPremium, Is.EqualTo(value.IsPremium));
                Assert.That(load.Value.PlayerName, Is.EqualTo(value.PlayerName));
                Assert.That(load.Value.Items.Count, Is.EqualTo(value.Items.Count));
                Assert.That(load.Value.Items[1].Weight, Is.EqualTo(value.Items[1].Weight));
            }
        }

        [Test]
        public async Task Save_WritesRecordWithCodecIdAndVersionHeader()
        {
            using (var directory = new TemporaryDirectory())
            {
                string path = directory.GetPath("save.json");
                var store = NewtonsoftJsonPersistenceStoreFactory.CreateStore<SaveData>(path);
                SaveData value = CreateRepresentativeValue();

                Assert.That((await store.SaveAsync(in value, 2)).IsSuccess, Is.True);

                string record = Encoding.UTF8.GetString(File.ReadAllBytes(path));
                StringAssert.StartsWith("# cgp-record: 1\n", record);
                StringAssert.Contains("# codec-id: json/1\n", record);
                StringAssert.Contains("# content-version: 2\n", record);
            }
        }

        [Test]
        public async Task Corruption_IsRejectedWithIntegrityFailure_BeforeDeserialize()
        {
            using (var directory = new TemporaryDirectory())
            {
                string path = directory.GetPath("save.json");
                var store = NewtonsoftJsonPersistenceStoreFactory.CreateStore<SaveData>(path);
                SaveData value = CreateRepresentativeValue();
                Assert.That((await store.SaveAsync(in value, 1)).IsSuccess, Is.True);

                byte[] record = File.ReadAllBytes(path);
                record[record.Length - 1] ^= 0x01;
                File.WriteAllBytes(path, record);

                PersistenceLoadResult<SaveData> load = await store.LoadAsync(1);

                Assert.That(load.ErrorCode, Is.EqualTo(PersistenceErrorCode.IntegrityCheckFailed));
            }
        }

        [Test]
        public async Task MissingFile_IsMissingNotFailure()
        {
            using (var directory = new TemporaryDirectory())
            {
                var store = NewtonsoftJsonPersistenceStoreFactory.CreateStore<SaveData>(
                    directory.GetPath("missing.json"));

                PersistenceLoadResult<SaveData> load = await store.LoadAsync(0);

                Assert.That(load.IsMissing, Is.True);
                Assert.That(load.ErrorCode, Is.EqualTo(PersistenceErrorCode.None));
            }
        }

        [Test]
        public async Task Delete_RemovesRecordAndIsIdempotent()
        {
            using (var directory = new TemporaryDirectory())
            {
                string path = directory.GetPath("save.json");
                var store = NewtonsoftJsonPersistenceStoreFactory.CreateStore<SaveData>(path);
                SaveData value = CreateRepresentativeValue();
                Assert.That((await store.SaveAsync(in value, 1)).IsSuccess, Is.True);

                Assert.That((await store.DeleteAsync()).IsSuccess, Is.True);
                Assert.That(File.Exists(path), Is.False);
                Assert.That((await store.DeleteAsync()).IsSuccess, Is.True);
            }
        }

        private static SaveData CreateRepresentativeValue()
        {
            return new SaveData
            {
                Score = 123456,
                Speed = 0.1f,
                IsPremium = true,
                PlayerName = "Mai",
                Items =
                {
                    new SaveItem { Id = 1, Weight = 1.25f },
                    new SaveItem { Id = 2, Weight = 3.5f },
                }
            };
        }

        private sealed class TemporaryDirectory : IDisposable
        {
            internal TemporaryDirectory()
            {
                Path = System.IO.Path.Combine(
                    System.IO.Path.GetTempPath(),
                    "CycloneGames.Persistence.Tests.NewtonsoftJson",
                    Guid.NewGuid().ToString("N"));
                Directory.CreateDirectory(Path);
            }

            internal string Path { get; }

            internal string GetPath(string relativePath)
            {
                return System.IO.Path.Combine(Path, relativePath);
            }

            public void Dispose()
            {
                if (Directory.Exists(Path))
                {
                    Directory.Delete(Path, true);
                }
            }
        }
    }

    public sealed class SaveData
    {
        public int Score;
        public float Speed;
        public bool IsPremium;
        public string PlayerName;
        public List<SaveItem> Items = new List<SaveItem>();
    }

    public sealed class SaveItem
    {
        public int Id;
        public float Weight;
    }

    public sealed class GadgetProbe
    {
        public int Score;
    }

    internal sealed class DenyAllBinder : ISerializationBinder
    {
        public Type BindToType(string assemblyName, string typeName)
        {
            return null;
        }

        public void BindToName(Type serializedType, out string assemblyName, out string typeName)
        {
            assemblyName = null;
            typeName = null;
        }
    }
}
