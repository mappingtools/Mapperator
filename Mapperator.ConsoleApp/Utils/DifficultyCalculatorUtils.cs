using System.Diagnostics.CodeAnalysis;
using System.IO;
using osu.Game.Beatmaps;
using osu.Game.Beatmaps.Formats;
using osu.Game.IO;
using osu.Game.Rulesets;
using osu.Game.Tests.Beatmaps;
using osu.Game.Rulesets.Catch.Difficulty;
using osu.Game.Rulesets.Catch.Mods;
using osu.Game.Rulesets.Difficulty;
using osu.Game.Rulesets.Mania.Difficulty;
using osu.Game.Rulesets.Mania.Mods;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Osu;
using osu.Game.Rulesets.Mania;
using osu.Game.Rulesets.Taiko;
using osu.Game.Rulesets.Catch;
using osu.Game.Rulesets.Osu.Difficulty;
using osu.Game.Rulesets.Osu.Mods;
using osu.Game.Rulesets.Taiko.Difficulty;
using osu.Game.Rulesets.Taiko.Mods;

namespace Mapperator.ConsoleApp.Utils;

public static class DifficultyCalculatorUtils
{
    private static bool initialized;

    [DynamicDependency(DynamicallyAccessedMemberTypes.PublicConstructors, typeof(OsuRuleset))]
    private static void InitRulesetStore()
    {
        if (initialized) return;

        // Do a dumb hack to force load all the ruleset assemblies
        // This is necessary because the ruleset store relies on the assemblies being loaded in trimmed publications
        var rulesetTypes = new[] {
            typeof(OsuRuleset),
            typeof(TaikoRuleset),
            typeof(CatchRuleset),
            typeof(ManiaRuleset),
        };
        foreach (var type in rulesetTypes)
        {
            // Force the type to be loaded
            _ = type.Assembly;
        }

        var rulesets = new AssemblyRulesetStore();
        Decoder.RegisterDependencies(rulesets);
        initialized = true;
    }

    public static IWorkingBeatmap GetBeatmap(string path) {
        InitRulesetStore();
        using var resStream = File.OpenRead(path);
        using var stream = new LineBufferedReader(resStream);
        var decoder = Decoder.GetDecoder<Beatmap>(stream);

        ((LegacyBeatmapDecoder)decoder).ApplyOffsets = false;

        return new TestWorkingBeatmap(decoder.Decode(stream));
    }

    public static DifficultyCalculator CreateDifficultyCalculator(IWorkingBeatmap beatmap) {
        switch (beatmap.BeatmapInfo.Ruleset.ShortName) {
            case "osu":
                return new OsuDifficultyCalculator(beatmap.BeatmapInfo.Ruleset, beatmap);
            case "taiko":
                return new TaikoDifficultyCalculator(beatmap.BeatmapInfo.Ruleset, beatmap);
            case "fruits":
                return new CatchDifficultyCalculator(beatmap.BeatmapInfo.Ruleset, beatmap);
            case "mania":
                return new ManiaDifficultyCalculator(beatmap.BeatmapInfo.Ruleset, beatmap);
            default:
                throw new System.NotSupportedException("This ruleset is not supported.");
        }
    }

    public static ModRateAdjust GetRateAdjust(IWorkingBeatmap beatmap, float rate) {
        ModRateAdjust mod;
        switch (beatmap.BeatmapInfo.Ruleset.ShortName) {
            case "osu":
                mod = rate > 1 ? new OsuModDoubleTime() : new OsuModHalfTime();
                break;
            case "taiko":
                mod = rate > 1 ? new TaikoModDoubleTime() : new TaikoModHalfTime();
                break;
            case "fruits":
                mod = rate > 1 ? new CatchModDoubleTime() : new CatchModHalfTime();
                break;
            case "mania":
                mod = rate > 1 ? new ManiaModDoubleTime() : new ManiaModHalfTime();
                break;
            default:
                throw new System.NotSupportedException("This ruleset is not supported.");
        }
        mod.SpeedChange.Value = rate;
        return mod;
    }
}