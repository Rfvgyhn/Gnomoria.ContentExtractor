using Gnomoria.ContentExtractor.Extensions;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Content.Pipeline.Serialization.Intermediate;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using NLog;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace Gnomoria.ContentExtractor.Data
{
    public class DataManager : IDataTypeManager
    {
        private static Logger logger = LogManager.GetCurrentClassLogger();
        private ContentManager content;

        public DataManager(ContentManager content)
        {
            this.content = content;
        }

        public void Pack(string sourcePath, string destinationPath)
        {
            var isFile = File.Exists(sourcePath);
            var unpackedFiles = isFile ? new[] { sourcePath } : Directory.GetFiles(sourcePath, "*.xnb.js", SearchOption.AllDirectories);
            var tempDir = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), "Gnomoria.ContentExtractor", Guid.NewGuid().ToString())).FullName;

            foreach (var file in unpackedFiles)
            {
                logger.Info("Creating intermediate file for {0}", Path.GetFileNameWithoutExtension(file));
                var doc = JsonConvert.DeserializeXmlNode(File.ReadAllText(file));
                var assetPath = (isFile ? Path.GetFileName(file) : file.Substring(sourcePath.Length)).Split('.')[0] + ".xml";
                Directory.CreateDirectory(Path.GetDirectoryName(Path.Combine(tempDir, assetPath)));
                doc.Save(Path.Combine(tempDir, assetPath));
            }

            logger.Info("Packing files");
            var filesToBePacked = Directory.GetFiles(tempDir, "*.xml", SearchOption.AllDirectories);
            var libPath = Path.Combine(tempDir, "gnomorialib.dll");
            var preppedLibrary = false;
            
            try
            {
                var startInfo = new ProcessStartInfo()
                {
                    FileName = "lib\\de4dot",
                    Arguments = "gnomorialib.dll -o {0} --keep-names ef".FormatWith(libPath),
                    RedirectStandardOutput = true,
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    WorkingDirectory = "lib"
                };

                using (var process = new Process { StartInfo = startInfo })
                {
                    process.Start();
                    process.StandardOutput.ReadToEnd();

                    preppedLibrary = process.ExitCode == 0;
                }
            }
            catch (Exception e)
            {
                logger.Debug(e);
            }

            if (!preppedLibrary)
            {
                logger.Error("Couldn't prep library");
                Cleanup(tempDir);
                return;
            }

            var project = new ContentProject(destinationPath, tempDir);
            project.AddReference("gnomorialib", libPath);

            foreach (var file in filesToBePacked)
                project.AddItem(file, "XmlImporter", Path.GetFileName(file), Path.GetFileNameWithoutExtension(file));

            if (!project.Build())
                logger.Error("Error while packing data. See log for details");

            Cleanup(tempDir);
        }

        public void Unpack(string sourcePath, string destinationPath)
        {
            var files = new DirectoryInfo(Path.GetDirectoryName(sourcePath)).EnumerateFiles("*.xnb", SearchOption.AllDirectories);
            var load = content.GetType().GetMethod("Load");
            content.RootDirectory = Path.GetDirectoryName(sourcePath);

            foreach (var file in files)
            {
                var fileName = Path.GetFileNameWithoutExtension(file.Name);
                logger.Debug("Unpacking '{0}'", fileName);

                var assetPath = file.FullName.Substring(sourcePath.Length).Split('.')[0];

                try
                {
                    var data = content.Load<object>(assetPath);
                    logger.Info("Serializing {0}", fileName);
                    var dir = Path.GetDirectoryName(Path.Combine(destinationPath, assetPath));
                    Directory.CreateDirectory(dir);                    

                    using (var stream = new MemoryStream())
                    using (var writer = new XmlTextWriter(stream, Encoding.UTF8))
                    using (var result = File.CreateText(Path.Combine(dir, fileName + ".xnb.js")))
                    {
                        IntermediateSerializer.Serialize(writer, data, null);
                        writer.Flush();
                        stream.Position = 0;

                        var xDoc = XDocument.Load(stream);
                        var asset = xDoc.Root.Descendants("Asset").Single();
                        var type = Type.GetType(asset.Attribute("Type").Value);
                        var doc = new XmlDocument();

                        using (var xmlReader = xDoc.CreateReader())
                            doc.Load(xmlReader);

                        result.Write(JsonConvert.SerializeXmlNode(doc, Newtonsoft.Json.Formatting.Indented));
                    }

                    using (var stream = File.CreateText(Path.Combine(dir, fileName + ".friendly.js")))
                        stream.Write(JsonConvert.SerializeObject(data, Newtonsoft.Json.Formatting.Indented, new StringEnumConverter()));
                }
                catch (Exception e)
                {
                    logger.Error("Error loading {0}", fileName);
                    logger.Debug(e);
                }
            }
        }

        private void Cleanup(string tempDir = null)
        {
            logger.Info("Cleaning up temp files");

            if (!tempDir.IsNullOrEmpty())
                Directory.Delete(tempDir, true);
        }
    }
}
