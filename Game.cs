using Gnomoria.ContentExtractor.Data;
using Microsoft.Xna.Framework;
using NLog;
using System;

namespace Gnomoria.ContentExtractor
{
    /// <summary>
    /// This is the main type for your game
    /// </summary>
    public class Game : Microsoft.Xna.Framework.Game
    {
        private static Logger logger = LogManager.GetCurrentClassLogger();
        private GraphicsDeviceManager graphics;
        private Options options;

        public Game(Options options)
        {
            this.options = options;
            graphics = new GraphicsDeviceManager(this);            
        }

        protected override void LoadContent()
        {
            var manager = new DataTypeManagerFactory().Get(options.DataType, Content);

            try
            {
                if (options.Action == DataAction.Pack)
                    manager.Pack(options.Source, options.Destination);
                else
                    manager.Unpack(options.Source, options.Destination);

                logger.Info("{0} {1} complete", options.Action, options.DataType);
            }
            catch (Exception e)
            {
                logger.Error("Error while trying to {0} {1} {2}{3}{4}", options.Action, options.DataType, options.Source, Environment.NewLine, e);
            }

            this.Exit();
        }
    }
}
