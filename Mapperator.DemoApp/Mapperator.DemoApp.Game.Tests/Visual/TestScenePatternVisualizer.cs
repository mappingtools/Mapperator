using Mapperator.DemoApp.Game.Drawables;
using Mapping_Tools_Core.BeatmapHelper;
using NUnit.Framework;
using osu.Framework.Allocation;

namespace Mapperator.DemoApp.Game.Tests.Visual
{
    [TestFixture]
    public partial class TestScenePatternVisualizer : DemoAppTestScene
    {
        private int start;
        private int end;

        [BackgroundDependencyLoader]
        private void load(BeatmapStore beatmapStore)
        {
            var beatmap = beatmapStore.Get(@"input.osu");
            beatmap.UpdateStacking();
            PatternVisualizer visualizer = new PatternVisualizer();

            Add(visualizer);
            AddStep("add hitobject", () => visualizer.HitObjects.Add(beatmap.HitObjects[end++]));
            AddStep("remove hitobject", () => visualizer.HitObjects.Remove(beatmap.HitObjects[start++]));
            AddStep("move ahead", () =>
            {
                visualizer.HitObjects.Add(beatmap.HitObjects[end++]);
                visualizer.HitObjects.Remove(beatmap.HitObjects[start++]);
            });
        }
    }
}
