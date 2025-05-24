using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Mapperator.Model;
using osu.Framework.IO.Stores;

namespace Mapperator.DemoApp.Game;

public class MapDataStore : IResourceStore<IEnumerable<IEnumerable<MapDataPoint>>>
{
    private readonly IResourceStore<byte[]> store;

    public MapDataStore(IResourceStore<byte[]> resourceStore)
    {
        store = resourceStore;
    }

    public void Dispose()
    {
        store.Dispose();
    }

    private static IEnumerable<string> iterateLines(StreamReader reader)
    {
        while (!reader.EndOfStream)
        {
            yield return reader.ReadLine();
        }
    }

    public IEnumerable<IEnumerable<MapDataPoint>> Get(string name)
    {
        using Stream stream = store.GetStream(name);
        if (stream is null) return null;
        using StreamReader reader = new StreamReader(stream);
        var (version, data) = DataSerializer.DeserializeBeatmapData(iterateLines(reader).ToArray());
        if (version != 1)
            throw new NotImplementedException($"Data version {version} is not currently supported in MapDataStore");
        return data;
    }

    public Task<IEnumerable<IEnumerable<MapDataPoint>>> GetAsync(string name, CancellationToken cancellationToken = new())
    {
        using Stream stream = store.GetStream(name);
        if (stream is null) return null;
        using StreamReader reader = new StreamReader(stream);
        var (version, data) = DataSerializer.DeserializeBeatmapData(iterateLines(reader));
        if (version != 1)
            throw new NotImplementedException($"Data version {version} is not currently supported in MapDataStore");
        return Task.FromResult(data);
    }

    public Stream GetStream(string name)
    {
        return store.GetStream(name);
    }

    public IEnumerable<string> GetAvailableResources()
    {
        return store.GetAvailableResources();
    }
}
