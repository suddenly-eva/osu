// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu/master/LICENCE

using System;
using System.Collections.Generic;
using osu.Game.Beatmaps;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Osu.Beatmaps;
using osu.Game.Rulesets.Osu.Objects;
using osu.Game.Rulesets.Osu.OsuDifficulty.Preprocessing;
using osu.Game.Rulesets.Osu.OsuDifficulty.Skills;

namespace osu.Game.Rulesets.Osu.OsuDifficulty
{
    public class OsuDifficultyCalculator : DifficultyCalculator<OsuHitObject>
    {
        private const int section_length = 400;
        private const double difficulty_multiplier = 0.0675;

        public OsuDifficultyCalculator(Beatmap beatmap)
            : base(beatmap)
        {
        }

        public OsuDifficultyCalculator(Beatmap beatmap, Mod[] mods)
            : base(beatmap, mods)
        {
        }

        protected override void PreprocessHitObjects()
        {
            new OsuBeatmapProcessor().PostProcess(Beatmap);
        }

        public override double Calculate(Dictionary<string, double> categoryDifficulty = null)
        {
            OsuDifficultyBeatmap beatmap = new OsuDifficultyBeatmap(Beatmap.HitObjects, TimeRate);
            Skill[] skills =
            {
                new Jump(),
                new Speed(),
                new Flow(),
            };

            double sectionEnd = section_length / TimeRate;
            foreach (OsuDifficultyHitObject h in beatmap)
            {
                while (h.BaseObject.StartTime > sectionEnd)
                {
                    foreach (Skill s in skills)
                    {
                        s.SaveCurrentPeak();
                        s.StartNewSectionFrom(sectionEnd);
                    }

                    sectionEnd += section_length;
                }

                foreach (Skill s in skills)
                    s.Process(h);
            }

            double jumpRating  = Math.Sqrt(skills[0].DifficultyValue()) * difficulty_multiplier;
            double speedRating = Math.Sqrt(skills[1].DifficultyValue()) * difficulty_multiplier;
            double flowRating  = Math.Sqrt(skills[2].DifficultyValue()) * difficulty_multiplier;

            double[] list = { jumpRating, speedRating, flowRating };
            double max = System.Linq.Enumerable.Max<double>(list);
            double min = System.Linq.Enumerable.Min<double>(list);

            double starRating = (jumpRating + speedRating + flowRating) * 2 / 3 + Math.Abs(max - min) / 2;

            if (categoryDifficulty != null)
            {
                categoryDifficulty.Add("Jump",  jumpRating);
                categoryDifficulty.Add("Speed", speedRating);
                categoryDifficulty.Add("Flow",  flowRating);
            }

            return starRating;
        }

        protected override BeatmapConverter<OsuHitObject> CreateBeatmapConverter(Beatmap beatmap) => new OsuBeatmapConverter();
    }
}
