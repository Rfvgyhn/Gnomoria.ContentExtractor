using Microsoft.Xna.Framework.Content;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Gnomoria.ContentExtractor.Data
{
    public class DataTypeManagerFactory
    {
        public IDataTypeManager Get(DataType type, ContentManager content)
        {
            switch (type)
            {
                case DataType.Data:
                    return new DataManager(content);
                case DataType.Skin:
                    return new SkinManager(content);
                default:
                    throw new InvalidOperationException("Must specify a known data type");
            }
        }
    }
}
