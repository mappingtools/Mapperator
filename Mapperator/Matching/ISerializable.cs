using Mapperator.Model;

namespace Mapperator.Matching {
    public interface ISerializable {
        string DefaultExtension { get; }

        void Save(Stream stream);

        void Load(IEnumerable<IEnumerable<MapDataPoint>> data, Stream stream);
    }
}
