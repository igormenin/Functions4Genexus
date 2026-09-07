using System;
using System.Collections.Generic;
using System.IO;
using Artech.Architecture.UI.Framework.Services;
using Newtonsoft.Json;

namespace Func4Genexus.Store
{
    public class SdtTrackingStore
    {
        private const string StoreFileName = ".sdt_generator_map.json";

        private string GetStoreFilePath()
        {
            var kbPath = UIServices.KB.CurrentKB.Location;
            return Path.Combine(kbPath, StoreFileName);
        }

        public Dictionary<Guid, List<Guid>> LoadTrackingData()
        {
            var path = GetStoreFilePath();
            if (!File.Exists(path))
            {
                return new Dictionary<Guid, List<Guid>>();
            }

            try
            {
                var json = File.ReadAllText(path);
                var data = JsonConvert.DeserializeObject<Dictionary<Guid, List<Guid>>>(json);
                return data ?? new Dictionary<Guid, List<Guid>>();
            }
            catch (Exception ex)
            {
                // In production, we'd log this properly
                Console.WriteLine($"Error loading SDT tracking data: {ex.Message}");
                return new Dictionary<Guid, List<Guid>>();
            }
        }

        public void SaveTrackingData(Dictionary<Guid, List<Guid>> data)
        {
            var path = GetStoreFilePath();
            try
            {
                var json = JsonConvert.SerializeObject(data, Formatting.Indented);
                File.WriteAllText(path, json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving SDT tracking data: {ex.Message}");
            }
        }

        public List<Guid> GetSdtsForTransaction(Guid transactionGuid)
        {
            var data = LoadTrackingData();
            if (data.TryGetValue(transactionGuid, out var sdtGuids))
            {
                return sdtGuids;
            }
            return new List<Guid>();
        }

        public void LinkSdtToTransaction(Guid transactionGuid, Guid sdtGuid)
        {
            var data = LoadTrackingData();
            
            if (!data.ContainsKey(transactionGuid))
            {
                data[transactionGuid] = new List<Guid>();
            }

            if (!data[transactionGuid].Contains(sdtGuid))
            {
                data[transactionGuid].Add(sdtGuid);
                SaveTrackingData(data);
            }
        }
    }
}
