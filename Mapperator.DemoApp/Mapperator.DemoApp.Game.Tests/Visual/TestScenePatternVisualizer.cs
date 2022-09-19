using System;
using System.IO;
using System.Reflection;
using Mapperator.DemoApp.Game.Drawables;
using Mapping_Tools_Core.BeatmapHelper;
using Mapping_Tools_Core.BeatmapHelper.IO.Decoding;
using NUnit.Framework;

namespace Mapperator.DemoApp.Game.Tests.Visual
{
    [TestFixture]
    public class TestScenePatternVisualizer : DemoAppTestScene
    {
        private int start;
        private int end;

        public TestScenePatternVisualizer()
        {
            var assembly = Assembly.GetExecutingAssembly();
            var resourceName = @"Mapperator.DemoApp.Game.Tests.Resources.input.osu";

            foreach (var name in assembly.GetManifestResourceNames())
            {
                Console.WriteLine(name);
            }
            using Stream stream = assembly.GetManifestResourceStream(resourceName);
            using StreamReader reader = new StreamReader(stream);
            string result = reader.ReadToEnd();

            var beatmap = new OsuBeatmapDecoder().Decode(result);
            beatmap.UpdateStacking();
            PatternVisualizer visualizer = new PatternVisualizer();

            Add(visualizer);
            AddStep("add hitobject", () => visualizer.HitObjects.Add(beatmap.HitObjects[end++]));
            AddStep("remove hitobject", () => visualizer.HitObjects.Remove(beatmap.HitObjects[start++]));
        }
    }
}
