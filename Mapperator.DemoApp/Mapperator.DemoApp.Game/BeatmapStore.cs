using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Mapping_Tools_Core.BeatmapHelper;
using Mapping_Tools_Core.BeatmapHelper.IO.Decoding;
using osu.Framework.IO.Stores;

namespace Mapperator.DemoApp.Game;

public class BeatmapStore : IResourceStore<Beatmap>
{
    private readonly IResourceStore<byte[]> store;
    private readonly OsuBeatmapDecoder decoder = new();

    public BeatmapStore(IResourceStore<byte[]> resourceStore)
    {
        store = resourceStore;
    }

    public void Dispose()
    {
        store.Dispose();
    }

    public Beatmap Get(string name)
    {
        using Stream stream = store.GetStream(name);
        if (stream is null) return null;
        using StreamReader reader = new StreamReader(stream);
        string result = reader.ReadToEnd();
        return decoder.Decode(result);
    }

    public async Task<Beatmap> GetAsync(string name, CancellationToken cancellationToken = new())
    {
        await using Stream stream = store.GetStream(name);
        if (stream is null) return null;
        using StreamReader reader = new StreamReader(stream);
        string result = await reader.ReadToEndAsync();
        return decoder.Decode(result);
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
