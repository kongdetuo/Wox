﻿using System;
using System.Globalization;
using System.IO;
using Newtonsoft.Json;
using NLog;
using Wox.Infrastructure.Logger;
using Wox.Plugin;

namespace Wox.Infrastructure.Storage
{
    /// <summary>
    /// Serialize object using json format.
    /// </summary>
    public class JsonStrorage<T>
    {
        private readonly JsonSerializerSettings _serializerSettings;
        private T _data;
        // need a new directory name
        public const string DirectoryName = "Settings";
        public const string FileSuffix = ".json";
        public string FilePath { get; set; }
        public string DirectoryPath { get; set; }

        private static readonly NLog.Logger Logger = LogManager.GetCurrentClassLogger();


        internal JsonStrorage()
        {
            // use property initialization instead of DefaultValueAttribute
            // easier and flexible for default value of object
            _serializerSettings = new JsonSerializerSettings
            {
                ObjectCreationHandling = ObjectCreationHandling.Replace,
                NullValueHandling = NullValueHandling.Ignore,
                
            };
            _serializerSettings.Converters.Add(new KeywordConvertor());
        }

        public T Load()
        {
            if (File.Exists(FilePath))
            {
                var searlized = File.ReadAllText(FilePath);
                if (!string.IsNullOrWhiteSpace(searlized))
                {
                    Deserialize(searlized);
                }
                else
                {
                    LoadDefault();
                }
            }
            else
            {
                LoadDefault();
            }
            return _data.NonNull();
        }

        private void Deserialize(string searlized)
        {
            try
            {
                _data = JsonConvert.DeserializeObject<T>(searlized, _serializerSettings);
            }
            catch (JsonException e)
            {
                LoadDefault();
                Logger.WoxError($"Deserialize error for json <{FilePath}>", e);
            }

            if (_data == null)
            {
                LoadDefault();
            }
        }

        private void LoadDefault()
        {
            if (File.Exists(FilePath))
            {
                BackupOriginFile();
            }

            _data = JsonConvert.DeserializeObject<T>("{}", _serializerSettings);
            Save();
        }

        private void BackupOriginFile()
        {
            var timestamp = DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss-fffffff", CultureInfo.CurrentUICulture);
            var directory = Path.GetDirectoryName(FilePath).NonNull();
            var originName = Path.GetFileNameWithoutExtension(FilePath);
            var backupName = $"{originName}-{timestamp}{FileSuffix}";
            var backupPath = Path.Combine(directory, backupName);
            File.Copy(FilePath, backupPath, true);
            // todo give user notification for the backup process
        }

        public void Save()
        {
            string serialized = JsonConvert.SerializeObject(_data, Formatting.Indented, new KeywordConvertor());
            File.WriteAllText(FilePath, serialized);
        }
    }

    public class KeywordConvertor : JsonConverter<Keyword>
    {
        public override Keyword ReadJson(JsonReader reader, Type objectType, Keyword existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            return new Keyword(reader.Value.ToString());
        }

        public override void WriteJson(JsonWriter writer, Keyword value, JsonSerializer serializer)
        {
            writer.WriteValue(value.ToString());
        }
    }

}
