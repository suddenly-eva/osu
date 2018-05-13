// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu/master/LICENCE

using System;
using osu.Game.Rulesets.Osu.OsuDifficulty.Preprocessing;

namespace osu.Game.Rulesets.Osu.OsuDifficulty.Skills
{
    /// <summary>
    /// Represents the skill required to correctly aim at every object in the map with a uniform CircleSize and normalized distances.
    /// </summary>
    public class Jump : Skill
    {
        protected override double SkillMultiplier => 28;
        protected override double StrainDecayBase => 0.15;

        protected override double StrainValueOf(OsuDifficultyHitObject current) => Math.Pow(current.JumpDistance, 0.99) / current.DeltaTime;
    }
}
