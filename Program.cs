using CommandLine;
using NLog;
using System;

namespace Gnomoria.ContentExtractor
{
#if WINDOWS || XBOX
    static class Program
    {
        static Logger logger = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main(string[] args)
        {
            var options = new Options();

            if (!Parser.Default.ParseArguments(args, options))
            {
                logger.Error("Invalid arguments");
                return;
            }

            using (Game game = new Game(options))
            {
                game.Run();
            }
        }
    }
#endif
}

