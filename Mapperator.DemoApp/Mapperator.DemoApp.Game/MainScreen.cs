using System;
using System.Collections.Generic;
using System.Linq;
using Mapperator.Construction;
using Mapperator.DemoApp.Game.Drawables;
using Mapperator.Matching;
using Mapperator.Matching.DataStructures;
using Mapperator.Matching.Filters;
using Mapperator.Matching.Judges;
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
using osu.Framework.Input;
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
        private int patternIndex;
        private IEnumerator<Match> matchIterator;
        private BasicButton variantButton;
        private OnScreenFilter filter;
        private BestScoreOrderFilter sorter;

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
                        },
                        new BasicSliderBar<int>
                        {
                            Size = new Vector2(400, 50),
                            Margin = new MarginPadding(10),
                            Current = pos.GetBoundCopy(),
                        },
                        variantButton = new BasicButton
                        {
                            Size = new Vector2(100, 50),
                            Margin = new MarginPadding(10),
                            Text = @"variant",
                            BackgroundColour = Color4.Purple,
                            HoverColour = Color4.Pink,
                            FlashColour = Color4.HotPink,
                            Action = generateVariant,
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

            var data = mapDataStore.Get(@"SotarksData.txt");
            dataStruct = new RhythmDistanceTrieStructure();
            foreach (var map in data)
            {
                dataStruct.Add(map.ToArray());
            }

            beatmap.Value = beatmapStore.Get(@"input.osu");
            beatmap.BindValueChanged(OnBeatmapChange, true);
            pos.BindValueChanged(OnPosChange, true);
        }

        private void generateVariant()
        {
            if (matchIterator is not null && matchIterator.MoveNext())
            {
                showMatch(matchIterator.Current);
                matchIterator.MoveNext();
            }
            else
            {
                variantButton.Enabled.Value = false;
            }
        }

        private void OnPosChange(ValueChangedEvent<int> obj)
        {
            Scheduler.AddOnce(newPos);
        }

        private void newPos()
        {
            beatmap.Value.HitObjects.ForEach(o => o.IsSelected = false);

            // Find the current index for pattern because the indices of hitobject and pattern do not always match
            patternIndex = pos.Value + length + beatmap.Value.HitObjects.Take(pos.Value + length).Count(o => o is Slider or Spinner);

            // Get matches for current index
            var (endPos, angle, _) = BeatmapConstructor2.GetContinuation(beatmap.Value.HitObjects.GetRange(0, pos.Value + length));
            filter.Pos = endPos;
            filter.Angle = angle;
            sorter.PatternIndex = patternIndex;
            matcher.MinLength = 1;
            var matches = sorter.FilterMatches(filter.FilterMatches(matcher.FindMatches(patternIndex)));

            // For now just get the first match
            matchIterator?.Dispose();
            matchIterator = matches.GetEnumerator();

            var hasMatch = matchIterator.MoveNext();
            variantButton.Enabled.Value = hasMatch;

            if (hasMatch)
                showMatch(matchIterator.Current);
        }

        private void showMatch(Match match)
        {
            originalVisualizer.HitObjects.Clear();
            originalVisualizer.HitObjects.AddRange(beatmap.Value.HitObjects.GetRange(pos.Value, length));

            if (match.Length == 0) return;

            // Show the matched objects in the right visualizer
            var newHitObjects = beatmap.Value.HitObjects.GetRange(pos.Value, length);
            var constructor = new BeatmapConstructor2 { SelectNewObjects = true };
            constructor.Construct(newHitObjects, match, pattern.AsSpan()[patternIndex..]);

            newHitObjects.GiveObjectsTimingContext(beatmap.Value.BeatmapTiming);
            newHitObjects.CalculateEndPositions();
            newHitObjects.UpdateStacking(beatmap.Value.Difficulty.StackOffset, beatmap.Value.General.StackLeniency, beatmap.Value.Difficulty.ApproachTime);
            newHitObjects.CalculateHitObjectComboStuff(beatmap.Value.ComboColoursList.Count == 0 ? null : beatmap.Value.ComboColoursList.ToArray(), beatmap.Value.Storyboard.BreakPeriods);

            newVisualizer.HitObjects.Clear();
            newVisualizer.HitObjects.AddRange(newHitObjects);

            // Show the original objects in the left visualizer
            var i = 0;
            for (int j = 0; j < match.Length; j++)
            {
                if (match.Sequence.Span[j].DataType != DataType.Hit) continue;

                var ho = beatmap.Value.HitObjects[pos.Value + length + i++];
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
            filter = new OnScreenFilter();
            sorter = new BestScoreOrderFilter(new SuperJudge(), pattern, matcher);
        }
    }
}
