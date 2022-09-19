using System.Collections.Generic;
using System.Linq;
using Mapping_Tools_Core.BeatmapHelper.Contexts;
using Mapping_Tools_Core.BeatmapHelper.HitObjects;
using Mapping_Tools_Core.BeatmapHelper.HitObjects.Objects;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osuTK;
using osuTK.Graphics;

namespace Mapperator.DemoApp.Game.Drawables
{
    public class DrawableHitObject : CompositeDrawable
    {
        private readonly HitObject hitObject;

        public DrawableHitObject(HitObject hitObject) {
            this.hitObject = hitObject;

            Anchor = Anchor.TopLeft;
            Position = new Vector2((float)hitObject.Pos.X + 100, (float)hitObject.Pos.Y + 100);

            MainCirclePiece mainCirclePiece;
            if (hitObject is Slider slider)
            {
                ManualSliderBody sliderBody = new ManualSliderBody { AccentColour = Color4.DarkGray };
                var path = slider.GetSliderPath();
                var vertices = new List<Mapping_Tools_Core.MathUtil.Vector2>();
                path.GetPathToProgress(vertices, 0, 1);
                sliderBody.SetVertices(vertices.Select(o => new Vector2((float)o.X, (float)o.Y)).ToList());
                sliderBody.Position -= sliderBody.PathOffset;
                sliderBody.PathRadius = 30;
                sliderBody.Anchor = Anchor.Centre;

                InternalChildren = new Drawable[]
                {
                    //new SpinningBox(),
                    sliderBody,
                    mainCirclePiece = new MainCirclePiece {Anchor = Anchor.Centre},
                };
            }
            else
            {
                InternalChild = mainCirclePiece = new MainCirclePiece();
            }

            if (hitObject.HasContext<ComboContext>())
            {
                var comboContext = hitObject.GetContext<ComboContext>();
                mainCirclePiece.IndexInCurrentCombo.Value = comboContext.ComboIndex;
            }
        }
    }
}
