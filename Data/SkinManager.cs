using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using NLog;
using SevenZip;
using System;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Gnomoria.ContentExtractor.Data
{
    public class SkinManager : IDataTypeManager
    {
        private static Logger logger = LogManager.GetCurrentClassLogger();
        private ContentManager content;
        private const string SkinHeader = "58 4E 42 77 05 00 D0 AE 00 00 01 26 47 61 6D 65 2E 47 55 49 2E 43 6F 6E 74 72 6F 6C 73 2E 53 6B 69 6E 52 65 61 64 65 72 2C 20 47 6E 6F 6D 6F 72 69 61 00 00 00 00 00 01 95 DD 02";

        public SkinManager(ContentManager content)
        {
            this.content = content;
        }

        public void Pack(string sourcePath, string destinationPath)
        {
            var skin = Path.Combine(sourcePath, "Skin.xml");
            var cursorsDir = Path.Combine(sourcePath, "Cursors");
            var fontsDir = Path.Combine(sourcePath, "Fonts");
            var imagesDir = Path.Combine(sourcePath, "Images");

            if (!File.Exists(skin) || !Directory.Exists(cursorsDir) || !Directory.Exists(fontsDir) || !Directory.Exists(imagesDir))
            {
                logger.Error("Cursors, Fonts, Images and Skin.xml are required");
                return;
            }

            logger.Info("Packing skin '{0}'", sourcePath);

            var tempDir = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), "Gnomoria.ContentExtractor", Guid.NewGuid().ToString())).FullName;
            var skinRoot = Path.Combine(tempDir, "skinRoot");
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
            PackSkinImages(images, imagesDir, destination, tempDir);

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

            zipper.CompressDirectory(skinRoot, destinationPath, true);

            logger.Debug("Cleaning up temp files");
            Directory.Delete(tempDir, true);
        }

        public void Unpack(string sourcePath, string destinationPath)
        {
            var contentRoot = Path.GetDirectoryName(sourcePath);
            var skins = File.Exists(sourcePath) ? new FileInfo[] { new FileInfo(sourcePath) } : new DirectoryInfo(contentRoot).EnumerateFiles("*.skin", SearchOption.AllDirectories);
            var tempDir = content.RootDirectory = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "temp");

            foreach (var skin in skins)
            {
                var skinName = Path.GetFileNameWithoutExtension(skin.Name);
                var skinRoot = Path.Combine(tempDir, skinName);
                var rootDestination = Path.Combine(destinationPath, skinName);

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
                    var texture = content.Load<Texture2D>(skinName + "/Images/" + name);

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

        private void PackSkinImages(string[] images, string imagesDir, string destination, string tempDir)
        {
            var project = new ContentProject(destination, tempDir);
            project.AddReference("Microsoft.Xna.Framework.Content.Pipeline.TextureImporter, Version=4.0.0.0, Culture=neutral, PublicKeyToken=842cf8be1de50553, processorArchitecture=MSIL");

            foreach (var file in images)
                project.AddItem(file, "TextureImporter", Path.GetFileName(file), Path.GetFileNameWithoutExtension(file), "TextureProcessor");

            if (!project.Build())
                logger.Error("Error while packing images. See log for details");
        }
    }
}
