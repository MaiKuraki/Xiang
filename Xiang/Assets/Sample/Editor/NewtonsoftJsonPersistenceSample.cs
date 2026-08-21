using System;
using System.IO;
using System.Threading.Tasks;
using CycloneGames.Logging;
using CycloneGames.Persistence;
using CycloneGames.Persistence.NewtonsoftJson;
using UnityEditor;
using UnityEngine;

namespace Xiang.Sample.Editor
{
    public static class NewtonsoftJsonPersistenceSample
    {
        private const int ContentVersion = 1;

        private static readonly LogChannel Log = XiangSampleEditorLog.Channel;

        /// <summary>
        /// Menu: Xiang > Samples > NewtonsoftJson Save-Load.
        /// </summary>
        [MenuItem("Xiang/Samples/NewtonsoftJson Save-Load")]
        private static async void Run()
        {
            try
            {
                await RunAsync();
            }
            catch (Exception exception)
            {
                Log.Error(exception);
            }
        }

        private static async Task RunAsync()
        {
            string path = Path.Combine(
                Application.persistentDataPath,
                "PersistenceSample",
                "sample.save");

            // The factory wires the JSON codec to the file storage provider in one line.
            PersistenceStore<GameSaveData> store =
                NewtonsoftJsonPersistenceStoreFactory.CreateStore<GameSaveData>(path);

            Log.Info($"[NewtonsoftJsonSample] CodecId = {store.Profile.CodecId.Value}");
            Log.Info($"[NewtonsoftJsonSample] Save path = {path}");

            GameSaveData value = new GameSaveData
            {
                PlayerName = "Mai",
                DayNumber = 3,
                TotalScore = 123456,
                Trees =
                {
                    new PlacedTreeState { GridX = 0, GridY = 0, Size = 1, GrowthStage = 2 },
                    new PlacedTreeState { GridX = 2, GridY = 3, Size = 2, GrowthStage = 3 },
                }
            };

            PersistenceOperationResult save = await store.SaveAsync(in value, ContentVersion);
            Log.Info($"[NewtonsoftJsonSample] Save result = {save.Status} ({save.ErrorCode})");

            string raw = File.ReadAllText(path);
            Log.Info("[NewtonsoftJsonSample] ----- raw record content -----\n"
                + raw
                + "\n[NewtonsoftJsonSample] ---------------------------------");

            PersistenceLoadResult<GameSaveData> load = await store.LoadAsync(ContentVersion);
            if (load.IsSuccess)
            {
                Log.Info($"[NewtonsoftJsonSample] Load result = Loaded, version = {load.ContentVersion}");
                Log.Info($"[NewtonsoftJsonSample] Loaded value -> name='{load.Value.PlayerName}', "
                    + $"day={load.Value.DayNumber}, score={load.Value.TotalScore}, "
                    + $"trees={load.Value.Trees.Count}");
            }
            else
            {
                Log.Warning($"[NewtonsoftJsonSample] Load result = {load.Status} ({load.ErrorCode})");
            }
        }
    }
}
