using System.Collections.Generic;
using System.Collections.Specialized;
using Mapping_Tools_Core.BeatmapHelper.HitObjects;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osuTK;
using osuTK.Graphics;

namespace Mapperator.DemoApp.Game.Drawables;

public class PatternVisualizer : CompositeDrawable
{
    public BindableList<HitObject> HitObjects { get; }

    private readonly Dictionary<HitObject, DrawableHitObject> drawableHitObjects = new();
    private readonly float margin;

    /// <summary>
    /// Used to colour the selected hit objects.
    /// </summary>
    public Color4 AccentColour { get; set; } = Color4.Red;

    public PatternVisualizer(float margin = 30)
    {
        this.margin = margin;
        Size = new Vector2(512 + margin * 2, 384 + margin * 2);
        Masking = true;

        HitObjects = new BindableList<HitObject>();
        HitObjects.BindCollectionChanged(OnChange);

        AddInternal(new Box { RelativeSizeAxes = Axes.Both, Colour = Color4.Black, Depth = float.MaxValue });
    }

    private void OnChange(object sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.OldItems != null)
            foreach (var item in e.OldItems)
            {
                var ho = (HitObject)item;
                RemoveInternal(drawableHitObjects[ho], true);
                drawableHitObjects.Remove(ho);
            }

        if (e.NewItems != null)
            foreach (var item in e.NewItems)
            {
                var ho = (HitObject)item;
                var drawableHitObject = new DrawableHitObject(ho);
                drawableHitObject.Position += new Vector2(margin);
                drawableHitObjects[ho] = drawableHitObject;
                AddInternal(drawableHitObject);
                ChangeInternalChildDepth(drawableHitObject, (float)ho.StartTime);
            }
    }
}
