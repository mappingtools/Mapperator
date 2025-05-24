using System.Collections.Generic;
using System.Linq;
using Mapping_Tools_Core.BeatmapHelper.Contexts;
using Mapping_Tools_Core.BeatmapHelper.HitObjects;
using Mapping_Tools_Core.BeatmapHelper.HitObjects.Objects;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osuTK;
using osuTK.Graphics;

namespace Mapperator.DemoApp.Game.Drawables
{
    public partial class DrawableHitObject : CompositeDrawable
    {
        private readonly HitObject hitObject;
        private readonly Container box;

        public DrawableHitObject(HitObject hitObject) {
            this.hitObject = hitObject;

            Anchor = Anchor.TopLeft;
            AutoSizeAxes = Axes.Both;
            Origin = Anchor.Centre;

            if (hitObject.HasContext<StackingContext>())
            {
                var stackingContext = hitObject.GetContext<StackingContext>();
                var pos = stackingContext.Stacked(hitObject.Pos);
                Position = new Vector2((float)pos.X, (float)pos.Y);
            }
            else
            {
                Position = new Vector2((float)hitObject.Pos.X, (float)hitObject.Pos.Y);
            }

            InternalChild = box = new Container
            {
                AutoSizeAxes = Axes.Both,
                Anchor = Anchor.Centre,
                Origin = Anchor.Centre
            };

            MainCirclePiece mainCirclePiece;
            if (hitObject is Slider slider)
            {
                ManualSliderBody sliderBody = new ManualSliderBody { AccentColour = Color4.Black };
                var path = slider.GetSliderPath();
                var vertices = new List<Mapping_Tools_Core.MathUtil.Vector2>();
                path.GetPathToProgress(vertices, 0, 1);
                var v1 = vertices[0];
                sliderBody.SetVertices(vertices.Select(o => new Vector2((float)(o.X - v1.X), (float)(o.Y - v1.Y))).ToList());
                sliderBody.PathRadius = 30;

                box.Add(sliderBody);
            }

            box.Add(mainCirclePiece = new MainCirclePiece());

            if (hitObject.HasContext<ComboContext>())
            {
                var comboContext = hitObject.GetContext<ComboContext>();
                mainCirclePiece.IndexInCurrentCombo.Value = comboContext.ComboIndex - 1;
            }
        }
    }
}
