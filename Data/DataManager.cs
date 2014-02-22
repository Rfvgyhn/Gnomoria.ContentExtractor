using Microsoft.Xna.Framework.Content;
using Newtonsoft.Json;
using NLog;
using System;
using System.Collections.Generic;
using System.IO;

namespace Gnomoria.ContentExtractor.Data
{
    public class DataManager : IDataTypeManager
    {
        private static Logger logger = LogManager.GetCurrentClassLogger();
        private ContentManager content;
        private Dictionary<string, string> typeOverrides;

        public DataManager(ContentManager content)
        {
            this.content = content;
            typeOverrides = new Dictionary<string, string>
            {
                { "audioevents", "SFXEvent" }
            };
        }

        public void Pack(string sourcePath, string destinationPath)
        {
            throw new NotImplementedException();
        }

        public void Unpack(string sourcePath, string destinationPath)
        {
            content.RootDirectory = Path.GetDirectoryName(sourcePath);
            var files = new DirectoryInfo(content.RootDirectory).EnumerateFiles("*.xnb", SearchOption.AllDirectories);
            var load = content.GetType().GetMethod("Load");
            
            foreach (var file in files)
            {
                var fileName = Path.GetFileNameWithoutExtension(file.Name);
                logger.Debug("Unpacking '{0}'", fileName);

                var path = file.FullName.Substring(content.RootDirectory.Length + 1).Split('.')[0];

                try
                {
                    var data = content.Load<object>(path);
                    logger.Info("Serializing {0}", fileName);
                    var dir = Path.GetDirectoryName(Path.Combine(destinationPath, path));
                    Directory.CreateDirectory(dir);

                    using (var stream = File.CreateText(Path.Combine(dir, fileName + ".json")))
                        stream.Write(JsonConvert.SerializeObject(data, Newtonsoft.Json.Formatting.Indented));
                }
                catch
                {
                    logger.Error("Error loading {0}", fileName);
                }
            }
        }
    }
}
