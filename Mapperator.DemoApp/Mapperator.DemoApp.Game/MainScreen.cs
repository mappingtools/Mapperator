using System;
using System.Linq;
using Mapperator.Construction;
using Mapperator.DemoApp.Game.Drawables;
using Mapperator.Matching.DataStructures;
using Mapperator.Matching.Filters;
using Mapperator.Matching.Matchers;
using Mapperator.Model;
using Mapping_Tools_Core.BeatmapHelper;
using Mapping_Tools_Core.BeatmapHelper.HitObjects.Objects;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.UserInterface;
using osu.Framework.Screens;
using osuTK;
using osuTK.Graphics;

namespace Mapperator.DemoApp.Game
{
    public class MainScreen : Screen
    {
        private readonly Bindable<Beatmap> beatmap = new();
        private readonly BindableInt pos = new() { MinValue = 0 };
        private readonly int length = 5;
        private PatternVisualizer originalVisualizer;
        private PatternVisualizer newVisualizer;
        private RhythmDistanceTrieStructure dataStruct;
        private MapDataPoint[] pattern;
        private TrieDataMatcher2 matcher;

        [BackgroundDependencyLoader]
        private void load(BeatmapStore beatmapStore, MapDataStore mapDataStore)
        {
            InternalChildren = new Drawable[]
            {
                new Box
                {
                    Colour = Color4.LightGray,
                    RelativeSizeAxes = Axes.Both,
                },
                new SpriteText
                {
                    Y = 20,
                    Text = "Main Screen",
                    Anchor = Anchor.TopCentre,
                    Origin = Anchor.TopCentre,
                    Font = FontUsage.Default.With(size: 40)
                },
                new FillFlowContainer
                {
                    Anchor = Anchor.TopLeft,
                    RelativeSizeAxes = Axes.X,
                    FillMode = FillMode.Stretch,
                    Position = new Vector2(0, 80),
                    Direction = FillDirection.Horizontal,
                    Children = new Drawable[]
                    {
                        new BasicButton
                        {
                            Size = new Vector2(100, 50),
                            Margin = new MarginPadding(10),
                            Text = @"prev",
                            BackgroundColour = Color4.Blue,
                            HoverColour = Color4.CornflowerBlue,
                            FlashColour = Color4.MediumPurple,
                            Action = () => pos.Value--,
                        },
                        new BasicButton
                        {
                            Size = new Vector2(100, 50),
                            Margin = new MarginPadding(10),
                            Text = @"next",
                            BackgroundColour = Color4.Blue,
                            HoverColour = Color4.CornflowerBlue,
                            FlashColour = Color4.MediumPurple,
                            Action = () => pos.Value++,
                        }
                    }
                },
                new DrawSizePreservingFillContainer
                {
                    TargetDrawSize = new Vector2(1184, 464),
                    Anchor = Anchor.TopLeft,
                    Position = new Vector2(0, 150),
                    Child = new FillFlowContainer
                    {
                        Direction = FillDirection.Horizontal,
                        FillMode = FillMode.Stretch,
                        Children = new Drawable[]
                        {
                            originalVisualizer = new PatternVisualizer { Margin = new MarginPadding(10) },
                            newVisualizer = new PatternVisualizer { Margin = new MarginPadding(10) }
                        }
                    }
                }
            };

            var data = mapDataStore.Get(@"test.txt");
            dataStruct = new RhythmDistanceTrieStructure();
            foreach (var map in data)
            {
                dataStruct.Add(map.ToArray());
            }

            pos.BindValueChanged(OnPosChange);
            beatmap.Value = beatmapStore.Get(@"input.osu");
            beatmap.BindValueChanged(OnBeatmapChange, true);
        }

        private void OnPosChange(ValueChangedEvent<int> obj)
        {
            beatmap.Value.HitObjects.ForEach(o => o.IsSelected = false);

            originalVisualizer.HitObjects.Clear();
            newVisualizer.HitObjects.Clear();
            originalVisualizer.HitObjects.AddRange(beatmap.Value.HitObjects.GetRange(obj.NewValue, length));
            newVisualizer.HitObjects.AddRange(beatmap.Value.HitObjects.GetRange(obj.NewValue, length));

            // Find the current index for pattern because the indices of hitobject and pattern do not always match
            var index = obj.NewValue + length + beatmap.Value.HitObjects.Take(obj.NewValue + length).Count(o => o is Slider or Spinner);

            // Get matches for current index
            var (endPos, angle, _) = BeatmapConstructor2.GetContinuation(newVisualizer.HitObjects);
            var filter = new OnScreenFilter { Pos = endPos, Angle = angle };
            var matches = filter.FilterMatches(matcher.FindMatches(index));

            // For now just get the first match
            var match = matches.FirstOrDefault();

            var constructor = new BeatmapConstructor2();
            constructor.Construct(newVisualizer.HitObjects, match, pattern.AsSpan()[index..], true, null, out _);

            // Show the original objects next to the generated objects
            var i = 0;
            for (int j = 0; j < match.Length; j++)
            {
                if (match.WholeSequence.Span[j + match.Lookback].DataType != DataType.Hit) continue;

                var ho = beatmap.Value.HitObjects[obj.NewValue + length + i++];
                ho.IsSelected = true;
                originalVisualizer.HitObjects.Add(ho);
            }
        }

        private void OnBeatmapChange(ValueChangedEvent<Beatmap> obj)
        {
            obj.NewValue.CalculateEndPositions();
            originalVisualizer.HitObjects.Clear();
            newVisualizer.HitObjects.Clear();
            originalVisualizer.HitObjects.AddRange(obj.NewValue.HitObjects.GetRange(pos.Value, length));
            newVisualizer.HitObjects.AddRange(obj.NewValue.HitObjects.GetRange(pos.Value, length));

            pos.MaxValue = obj.NewValue.HitObjects.Count - length - 1;

            pattern = new DataExtractor().ExtractBeatmapData(obj.NewValue).ToArray();
            matcher = new TrieDataMatcher2(dataStruct, pattern);
        }
    }
}
