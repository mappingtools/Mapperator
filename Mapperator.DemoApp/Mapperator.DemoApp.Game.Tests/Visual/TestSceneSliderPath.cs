using System.Linq;
using Mapperator.DemoApp.Game.Drawables;
using Mapping_Tools_Core.BeatmapHelper.IO.Decoding.HitObjects.Objects;
using osu.Framework.Graphics;
using NUnit.Framework;
using osuTK;
using osuTK.Graphics;

namespace Mapperator.DemoApp.Game.Tests.Visual
{
    [TestFixture]
    public partial class TestSceneSliderPath : DemoAppTestScene
    {
        private ManualSliderBody sliderBody;
        private MainCirclePiece circlePiece;

        public TestSceneSliderPath()
        {
            Add(sliderBody = new ManualSliderBody
            {
                PathRadius = 30,
                AccentColour = Color4.Black
            });
            Add(new MainCirclePiece { Anchor = Anchor.Centre });
            Add(circlePiece = new MainCirclePiece { Anchor = Anchor.Centre, Position = new Vector2(-100, 0)});
            var decoder = new SliderDecoder();
            var slider = decoder.Decode(@"64,71,40059,6,0,B|114:12|114:12|106:118|106:118|96:142|96:142|90:277,1,284.999992389679,6|0,2:2|0:0,0:0:0:0:");
            var path = slider.GetSliderPath();
            var vertices = new System.Collections.Generic.List<Mapping_Tools_Core.MathUtil.Vector2>();
            path.GetPathToProgress(vertices, 0, 1);
            sliderBody.SetVertices(vertices.Select(o => new Vector2((float)o.X, (float)o.Y)).ToList());

            AddStep("increase combo", () => circlePiece.IndexInCurrentCombo.Value += 1);
        }
    }
}
