using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;
using GameLibrary;
using System.Runtime.Serialization.Json;
using System.ComponentModel;
using Newtonsoft.Json;
using System.IO;
using System.Configuration;
using NLog;

namespace Gnomoria.ContentExtractor
{
    /// <summary>
    /// This is the main type for your game
    /// </summary>
    public class Game1 : Microsoft.Xna.Framework.Game
    {
        static Logger logger = LogManager.GetCurrentClassLogger();
        GraphicsDeviceManager graphics;
        private readonly string contentRoot;     

        public Game1()
        {
            graphics = new GraphicsDeviceManager(this);
            contentRoot = ConfigurationManager.AppSettings["ContentRoot"];
            Content.RootDirectory = contentRoot;
            logger.Info("Content Root: " + contentRoot);
        }

        protected override void Initialize()
        {
            base.Initialize();
        }

        protected override void LoadContent()
        {
            var typesToLoad = new List<string>
            {
                "GameLibrary.{0}def[], gnomorialib",
                "GameLibrary.{0}property[], gnomorialib",
                "GameLibrary.{0}[], gnomorialib",
                "GameLibrary.{0}topic[], gnomorialib"
            };
            
            var files = new DirectoryInfo(contentRoot).EnumerateFiles("*.xnb", SearchOption.AllDirectories);
            var saveDir = ConfigurationManager.AppSettings["OutputPath"];
            var load = Content.GetType().GetMethod("Load");

            logger.Info("OutputPath: " + saveDir);

            foreach (var file in files)
            {
                var fileName = file.Name.Split('.')[0];
                Type type = null;

                foreach (var typeName in typesToLoad)
                {
                    var t = string.Format(typeName, fileName);
                    type = Type.GetType(t, false, true);

                    if (type != null)
                        break;
                }

                if (type == null)
                {
                    logger.Warn("Can't resolve " + fileName);
                    continue;
                }

                var path = file.FullName.Substring(contentRoot.Length).Split('.')[0];

                try
                {
                    var data = load.MakeGenericMethod(new Type[] { type }).Invoke(Content, new object[] { path });
                    logger.Info("Serializing " + fileName);
                    var dir = Path.GetDirectoryName(Path.Combine(saveDir, path));
                    Directory.CreateDirectory(dir);

                    using (var stream = File.CreateText(Path.Combine(dir, fileName + ".json")))
                        stream.Write(JsonConvert.SerializeObject(data, Formatting.Indented));
                }
                catch
                {
                    logger.Error("Error loading " + fileName);
                }                
            }

            this.Exit();
        }
    }
}
