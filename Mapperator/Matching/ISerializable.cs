using Mapperator.Model;
using System.Collections.Generic;
using System.IO;

namespace Mapperator.Matching {
    internal interface ISerializable {
        string DefaultExtension { get; }

        void Save(Stream stream);

        void Load(IEnumerable<MapDataPoint> data, Stream stream);
    }
}
