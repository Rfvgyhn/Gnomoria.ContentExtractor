using CommandLine;
using CommandLine.Text;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Gnomoria.ContentExtractor
{
    public enum DataType
    {
        Unknown,
        Skin,
        Data
    }

    public enum DataAction
    {
        Pack,
        Unpack
    }

    public class Options
    {
        [Option('a', "action", HelpText = "Pack or unpack XNB", Required = true)]
        public DataAction Action { get; set; }

        [Option('t', "type", HelpText = "Type to pack/unpack", DefaultValue = DataType.Unknown)]
        public DataType DataType { get; set; }

        [Option('i', "input", HelpText = "Input file/folder")]
        public string Source { get; set; }

        [Option('o', "output", HelpText = "Output destination", Required = true)]
        public string Destination { get; set; }

        [HelpOption]
        public string GetUsage()
        {
            return HelpText.AutoBuild(this, (HelpText current) => HelpText.DefaultParsingErrorsHandler(this, current));
        }
    }
}
