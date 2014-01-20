using Gnomoria.ContentExtractor.Extensions;
using Microsoft.Build.Evaluation;
using Microsoft.Build.Framework;
using Microsoft.Build.Logging;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;
using NLog;
using SevenZip;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml;

namespace Gnomoria.ContentExtractor
{
    /// <summary>
    /// This is the main type for your game
    /// </summary>
    public class Game : Microsoft.Xna.Framework.Game
    {
        static Logger logger = LogManager.GetCurrentClassLogger();
        GraphicsDeviceManager graphics;
        string contentRoot;
        Options options;
        Dictionary<string, string> typeOverrides;
        const string SkinHeader = "58 4E 42 77 05 00 D0 AE 00 00 01 26 47 61 6D 65 2E 47 55 49 2E 43 6F 6E 74 72 6F 6C 73 2E 53 6B 69 6E 52 65 61 64 65 72 2C 20 47 6E 6F 6D 6F 72 69 61 00 00 00 00 00 01 95 DD 02";

        public Game(Options options)
        {
            this.options = options;
            Init();
        }

        private void Init()
        {
            graphics = new GraphicsDeviceManager(this);
            typeOverrides = ((Hashtable)ConfigurationManager.GetSection("typeOverrides"))
                             .Cast<DictionaryEntry>()
                             .ToDictionary(n => n.Key.ToString(), n => n.Value.ToString());
            options.DataType = GetDataType();
            contentRoot = GetContentRoot(options.DataType);
            Content.RootDirectory = contentRoot;
            logger.Info("Content Root: {0}", contentRoot);

            if (options.DataType == DataType.Unknown)
            {
                logger.Error("Unknown data type");
                this.Exit();
            }

            logger.Info("Data Type: {0}", options.DataType);
            var sevenZipPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), @"lib\7z.dll");
            SevenZipExtractor.SetLibraryPath(sevenZipPath);
            SevenZipCompressor.SetLibraryPath(sevenZipPath);
        }

        protected override void LoadContent()
        {
            var actions = new Dictionary<DataType, Dictionary<DataAction, Action>>
            {
                { 
                    DataType.Data, new Dictionary<DataAction, Action>
                        { { DataAction.Unpack, UnpackData }, { DataAction.Pack, PackData } }
                },
                { 
                    DataType.Skin, new Dictionary<DataAction, Action>
                        { { DataAction.Unpack, UnpackSkin }, { DataAction.Pack, PackSkin } }
                },
            };

            try
            {
                actions[options.DataType][options.Action]();
            }
            catch (Exception e)
            {
                logger.Error("Error while trying to {0} {1} {2}{3}{4}", options.Action, options.DataType, options.Source, Environment.NewLine, e);
            }

            logger.Info("{0} {1} complete", options.Action, options.DataType);

            this.Exit();
        }

        private string GetContentRoot(DataType dataType)
        {
            if (options.Source == null)
            {
                var pathMap = new Dictionary<DataType, string>
                {
                    { DataType.Data, @"Data\" },
                    { DataType.Skin, @"UI\" }
                };
                options.Source = Path.Combine(ConfigurationManager.AppSettings["ContentRoot"], pathMap[dataType]);
                logger.Info("Source not specified. Using default '{0}'", options.Source);
            }

            options.Source = Environment.ExpandEnvironmentVariables(options.Source);
            if (!Directory.Exists(options.Source) && !File.Exists(options.Source))
            {
                logger.Error("Invalid source specified '{0}'", options.Source);
                this.Exit();
                return "";
            }

            return Path.GetDirectoryName(options.Source);
        }

        private DataType GetDataType()
        {
            if (options.DataType != DataType.Unknown)
                return options.DataType;

            // try to guess it from source path
            if (options.Source.Contains(@"Gnomoria\Content\Data"))
                return DataType.Data;
            if (options.Source.Contains(@"Gnomoria\Content\UI"))
                return DataType.Skin;

            return DataType.Unknown;
        }

        private void UnpackData()
        {
            var typesToLoad = new List<string>
            {
                "GameLibrary.{0}def[], gnomorialib",
                "GameLibrary.{0}property[], gnomorialib",
                "GameLibrary.{0}[], gnomorialib",
                "GameLibrary.{0}topic[], gnomorialib"
            };

            var files = new DirectoryInfo(contentRoot).EnumerateFiles("*.xnb", SearchOption.AllDirectories);
            var load = Content.GetType().GetMethod("Load");

            logger.Info("OutputPath: {0}", options.Destination);

            foreach (var file in files)
            {
                var fileName = Path.GetFileNameWithoutExtension(file.Name);
                Type type = null;

                logger.Debug("Unpacking '{0}'", fileName);
                foreach (var typeName in typesToLoad)
                {
                    var overridenType = typeOverrides.ContainsKey(fileName) ? typeOverrides[fileName] : fileName;
                    var t = string.Format(typeName, overridenType);
                    type = Type.GetType(t, false, true);

                    if (type != null)
                        break;
                }

                if (type == null)
                {
                    logger.Warn("Can't resolve {0}", fileName);
                    continue;
                }

                var path = file.FullName.Substring(contentRoot.Length + 1).Split('.')[0];

                try
                {
                    var data = load.MakeGenericMethod(new Type[] { type }).Invoke(Content, new object[] { path });
                    logger.Info("Serializing {0}", fileName);
                    var dir = Path.GetDirectoryName(Path.Combine(options.Destination, path));
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

        private void PackData()
        {
            logger.Warn("Packing data not implemented.");
        }

        private void UnpackSkin()
        {
            var skins = File.Exists(options.Source) ? new FileInfo[] { new FileInfo(options.Source) } : new DirectoryInfo(contentRoot).EnumerateFiles("*.skin", SearchOption.AllDirectories);
            var tempDir = Content.RootDirectory = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "temp");

            foreach (var skin in skins)
            {
                var skinName = Path.GetFileNameWithoutExtension(skin.Name);
                var skinRoot  = Path.Combine(tempDir, skinName);
                var rootDestination = Path.Combine(options.Destination, skinName);

                logger.Info("Unpacking skin '{0}'", skinName);

                logger.Debug("Unzipping skin");
                using (var file = File.OpenRead(skin.FullName))
                using (var extractor = new SevenZipExtractor(file))
                {
                    extractor.ExtractArchive(skinRoot);
                }

                Directory.CreateDirectory(rootDestination);

                logger.Debug("Unpacking Skin.xnb");
                var destination = rootDestination;
                using (var file = File.OpenRead(Path.Combine(skinRoot, "Skin.xnb")))
                using (var xml = new FileStream(Path.Combine(destination, "Skin.xml"), FileMode.Create, FileAccess.Write))
                {
                    file.Position = 0x3B;
                    file.CopyTo(xml);
                }

                logger.Debug("Unpacking images");
                destination = Path.Combine(rootDestination, "Images");
                var images = new DirectoryInfo(Path.Combine(skinRoot, "Images")).EnumerateFiles("*.xnb", SearchOption.AllDirectories);
                Directory.CreateDirectory(destination);

                foreach (var image in images)
                {
                    var name = Path.GetFileNameWithoutExtension(image.Name);
                    var texture = Content.Load<Texture2D>(skinName + "/Images/" + name);

                    using (var png = File.Create(Path.Combine(destination, name + ".png")))
                    {
                        texture.SaveAsPng(png, texture.Width, texture.Height);
                    }
                }

                logger.Debug("Copying fonts");
                // not implemented. just copy them instead
                destination = Path.Combine(rootDestination, "Fonts");
                var fonts = new DirectoryInfo(Path.Combine(skinRoot, "Fonts")).EnumerateFiles("*.xnb", SearchOption.AllDirectories);
                Directory.CreateDirectory(destination);

                foreach (var font in fonts)
                {
                    var name = Path.GetFileNameWithoutExtension(font.Name);

                    File.Copy(font.FullName, Path.Combine(destination, name + ".xnb"), true);
                }

                logger.Debug("Copying cursors");
                // not implemented. just copy them instead
                destination = Path.Combine(rootDestination, "Cursors");
                var cursors = new DirectoryInfo(Path.Combine(skinRoot, "Cursors")).EnumerateFiles("*.xnb", SearchOption.AllDirectories);
                Directory.CreateDirectory(destination);

                foreach (var cursor in cursors)
                {
                    var name = Path.GetFileNameWithoutExtension(cursor.Name);

                    File.Copy(cursor.FullName, Path.Combine(destination, name + ".xnb"), true);
                }
            }

            logger.Debug("Cleaning up temp files");
            Directory.Delete(tempDir, true);
        }

        private void PackSkin()
        {
            var skin = Path.Combine(options.Source, "Skin.xml");
            var cursorsDir = Path.Combine(options.Source, "Cursors");
            var fontsDir = Path.Combine(options.Source, "Fonts");
            var imagesDir = Path.Combine(options.Source, "Images");

            if (!File.Exists(skin) || !Directory.Exists(cursorsDir) || !Directory.Exists(fontsDir) || !Directory.Exists(imagesDir))
            {
                logger.Error("Cursors, Fonts, Images and Skin.xml are required");
                return;
            }

            logger.Info("Packing skin '{0}'", options.Source);

            var workingDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            var skinRoot = Path.Combine(workingDir, "temp");
            Directory.CreateDirectory(skinRoot);

            logger.Debug("Packing Skin.xml");
            using (var file = File.Create(Path.Combine(skinRoot, "Skin.xnb")))
            using (var xml = File.OpenRead(skin))
            {
                var header = SkinHeader.Split(' ')
                                       .Select(s => Convert.ToByte(s, 16))
                                       .ToArray();
                file.Write(header, 0, header.Length);
                xml.CopyTo(file);
            }

            logger.Debug("Packing images");
            var images = Directory.GetFiles(imagesDir, "*.png", SearchOption.AllDirectories);
            var destination = Path.Combine(skinRoot, "Images");
            PackSkinImages(images, imagesDir, destination);

            logger.Debug("Copying fonts");
            // since fonts arent unpacked by this tool, just copy the XNBs
            destination = Path.Combine(skinRoot, "Fonts");
            var fonts = Directory.GetFiles(fontsDir, "*.xnb", SearchOption.AllDirectories);
            Directory.CreateDirectory(destination);

            foreach (var font in fonts)
            {
                var name = Path.GetFileNameWithoutExtension(font);

                File.Copy(font, Path.Combine(destination, name + ".xnb"), true);
            }

            logger.Debug("Copying cursors");
            // since fonts arent unpacked by this tool, just copy the XNBs
            destination = Path.Combine(skinRoot, "Cursors");
            var cursors = Directory.GetFiles(cursorsDir, "*.xnb", SearchOption.AllDirectories);
            Directory.CreateDirectory(destination);

            foreach (var cursor in cursors)
            {
                var name = Path.GetFileNameWithoutExtension(cursor);

                File.Copy(cursor, Path.Combine(destination, name + ".xnb"), true);
            }

            logger.Debug("Zipping skin contents");
            var zipper = new SevenZipCompressor
            {
                PreserveDirectoryRoot = false,
                ArchiveFormat = OutArchiveFormat.Zip,
                CompressionLevel = CompressionLevel.Ultra,                
            };

            zipper.CompressDirectory(skinRoot, options.Destination, true);

            logger.Debug("Cleaning up temp files");
            Directory.Delete(skinRoot, true);
            Directory.Delete(Path.Combine(workingDir, "obj"), true);

            foreach (var file in Directory.GetFiles(workingDir, "cachefile-*-targetpath.txt"))
                File.Delete(file);
        }

        private void PackSkinImages(string[] images, string imagesDir, string destination)
        {
            Directory.CreateDirectory(destination);
            var props = new Dictionary<string, string>
            {
                { "Configuration", "Release" },
                { "ProjectDir", imagesDir },
                { "OutputPath", destination }
            };

            var nlogLogger = new ConfigurableForwardingLogger
            {
                BuildEventRedirector = new NlogEventRedirector()
            };
            var collection = new ProjectCollection(props, new List<ILogger> { nlogLogger }, ToolsetDefinitionLocations.Registry);
            var compileFormat = @"
    <Compile Include=""{0}"">
      <Name>{1}</Name>
      <Importer>TextureImporter</Importer>
      <Processor>TextureProcessor</Processor>
    </Compile>";

            var resources = images.Aggregate(new StringBuilder(), (sb, i) => sb.AppendFormat(compileFormat, i, Path.GetFileNameWithoutExtension(i)))
                                  .ToString();            

            var project = string.Format(Resource.ContentProject, resources);
            using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(project)))
            using (var xml = XmlReader.Create(stream))
            {
                if (!collection.LoadProject(xml).Build())
                    logger.Error("Error while packing images. See log for details");
            }
        }
    }
}
