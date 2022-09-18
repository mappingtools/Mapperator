using Mapperator.DemoApp.Game.Drawables;
using osu.Framework.Graphics;
using NUnit.Framework;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osuTK;
using osuTK.Graphics;

namespace Mapperator.DemoApp.Game.Tests.Visual
{
    [TestFixture]
    public class TestSceneSliderPath : DemoAppTestScene
    {
        private ManualSliderBody sliderBody;
        private MainCirclePiece circlePiece;

        public TestSceneSliderPath()
        {
            Add(sliderBody = new ManualSliderBody
            {
                Anchor = Anchor.Centre,
                PathRadius = 30,
                AccentColour = Color4.Black
            });
            Add(new MainCirclePiece { Anchor = Anchor.Centre });
            Add(circlePiece = new MainCirclePiece { Anchor = Anchor.Centre, Position = new Vector2(-100, 0)});

            sliderBody.SetVertices(new[]
                { new Vector2(0, 0), new Vector2(100, 0), new Vector2(100, 100), new Vector2(200, 100) });
            sliderBody.Position -= sliderBody.PathOffset;

            AddStep("increase combo", () => circlePiece.IndexInCurrentCombo.Value += 1);
        }
    }
}
