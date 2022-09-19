using System.Collections.Generic;
using System.Collections.Specialized;
using Mapping_Tools_Core.BeatmapHelper.HitObjects;
using osu.Framework.Bindables;
using osu.Framework.Graphics.Containers;
using osuTK;

namespace Mapperator.DemoApp.Game.Drawables;

public class PatternVisualizer : CompositeDrawable
{
    public BindableList<HitObject> HitObjects { get; }

    private readonly Dictionary<HitObject, DrawableHitObject> drawableHitObjects = new();

    public PatternVisualizer()
    {
        Size = new Vector2(512, 384);
        Masking = true;

        HitObjects = new BindableList<HitObject>();
        HitObjects.BindCollectionChanged(OnChange);
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
                drawableHitObjects[ho] = drawableHitObject;
                AddInternal(drawableHitObject);
                ChangeInternalChildDepth(drawableHitObject, (float)ho.StartTime);
            }
    }
}
