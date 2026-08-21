using CycloneGames.Persistence;
using CycloneGames.Persistence.SystemIO;
using Newtonsoft.Json;

namespace CycloneGames.Persistence.NewtonsoftJson
{
    public static class NewtonsoftJsonPersistenceStoreFactory
    {
        public static PersistenceStore<T> CreateStore<T>(
            string path,
            PersistenceLimits limits = null)
        {
            var storage = new SystemFilePersistenceStorage(path);
            var codec = new NewtonsoftJsonPersistenceCodec<T>();
            var profile = new PersistenceProfile<T>(codec, limits);
            return new PersistenceStore<T>(storage, profile);
        }

        public static PersistenceStore<T> CreateStore<T>(
            string path,
            JsonSerializerSettings settings,
            PersistenceLimits limits = null)
        {
            var storage = new SystemFilePersistenceStorage(path);
            var codec = new NewtonsoftJsonPersistenceCodec<T>(settings);
            var profile = new PersistenceProfile<T>(codec, limits);
            return new PersistenceStore<T>(storage, profile);
        }
    }
}
