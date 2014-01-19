using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Gnomoria.ContentExtractor.Extensions
{
    public static class LoggerExtensions
    {
        public static void Debug(this Logger logger, string message, params object[] args)
        {
            logger.Debug(message.Format(args));
        }

        public static void Info(this Logger logger, string message, params object[] args)
        {
            logger.Info(message.Format(args));
        }

        public static void Error(this Logger logger, string message, params object[] args)
        {
            logger.Error(message.Format(args));
        }
    }
}
