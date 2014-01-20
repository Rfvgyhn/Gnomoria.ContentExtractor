using CommandLine;
using Gnomoria.ContentExtractor.Extensions;
using NLog;
using SevenZip;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Reflection;

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

            if (!EnsureOptions(options))
                return;

            var sevenZipPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), @"lib\7z.dll");
            SevenZipExtractor.SetLibraryPath(sevenZipPath);
            SevenZipCompressor.SetLibraryPath(sevenZipPath);

            using (Game game = new Game(options))
            {
                game.Run();
            }
        }

        private static bool EnsureOptions(Options options)
        {
            if (!EnsureDataType(options))
                return false;

            if (options.Source == null)
            {
                var pathMap = new Dictionary<DataType, string>
                {
                    { DataType.Data, @"Data" },
                    { DataType.Skin, @"UI" }
                };
                options.Source = Path.Combine(ConfigurationManager.AppSettings["ContentRoot"], pathMap[options.DataType]);
                logger.Info("Source not specified. Using default '{0}'", options.Source);
            }

            options.Source = Environment.ExpandEnvironmentVariables(options.Source);
            if (!Directory.Exists(options.Source) && !File.Exists(options.Source))
            {
                logger.Error("Invalid source specified '{0}'", options.Source);
                return false;
            }

            if (Directory.Exists(options.Source))
                options.Source = options.Source.EnsureEndsWith(Path.DirectorySeparatorChar);

            logger.Debug("Options: {0}", options.Dump());
            return true;
        }

        private static bool EnsureDataType(Options options)
        {
            if (options.DataType != DataType.Unknown)
                return true;

            if (options.Source != null)
            {
                // try to guess it from source path
                if (options.Source.Contains(@"Gnomoria\Content\Data"))
                    options.DataType = DataType.Data;
                if (options.Source.Contains(@"Gnomoria\Content\UI"))
                    options.DataType = DataType.Skin;
            }

            if (options.DataType != DataType.Unknown)
                return true;


            logger.Error("Unknown data type");
            return false;
        }
    }
#endif
}

