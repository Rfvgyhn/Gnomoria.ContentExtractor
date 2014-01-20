using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Gnomoria.ContentExtractor.Data
{
    public interface IDataTypeManager
    {
        void Pack(string sourcePath, string destinationPath);
        void Unpack(string sourcePath, string destinationPath);
    }
}
