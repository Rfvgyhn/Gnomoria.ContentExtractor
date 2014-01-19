using Microsoft.Build.Framework;
using NLog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace Gnomoria.ContentExtractor
{
    public class NlogEventRedirector : IEventRedirector
    {
        private static Logger logger = LogManager.GetLogger("MSBuild");
        
        public void ForwardEvent(BuildEventArgs buildEvent)
        {
            logger.Trace(buildEvent.Message);
        }
    }
}
