using Mapperator.DemoApp.Resources;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.IO.Stores;
using osuTK;

namespace Mapperator.DemoApp.Game
{
    public class DemoAppGameBase : osu.Framework.Game
    {
        // Anything in this class is shared between the test browser and the game implementation.
        // It allows for caching global dependencies that should be accessible to tests, or changing
        // the screen scaling for all components including the test browser and framework overlays.

        protected override Container<Drawable> Content { get; }

        protected DemoAppGameBase()
        {
            // Ensure game and tests scale with window size and screen DPI.
            base.Content.Add(Content = new DrawSizePreservingFillContainer
            {
                // You may want to change TargetDrawSize to your "default" resolution, which will decide how things scale and position when using absolute coordinates.
                TargetDrawSize = new Vector2(1366, 768)
            });
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            Resources.AddStore(new DllResourceStore(typeof(DemoAppResources).Assembly));
            dependencies.Cache(BeatmapStore = new BeatmapStore(new NamespacedResourceStore<byte[]>(Resources, @"Beatmaps")));
            dependencies.Cache(MapDataStore = new MapDataStore(new NamespacedResourceStore<byte[]>(Resources, @"MapData")));
        }

        private DependencyContainer dependencies;

        protected override IReadOnlyDependencyContainer CreateChildDependencies(IReadOnlyDependencyContainer parent) =>
            dependencies = new DependencyContainer(base.CreateChildDependencies(parent));

        public BeatmapStore BeatmapStore { get; private set; }

        public MapDataStore MapDataStore { get; private set; }
    }
}
