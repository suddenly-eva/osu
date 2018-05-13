// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu/master/LICENCE

using System;
using System.Linq;
using OpenTK;
using osu.Game.Rulesets.Osu.Objects;

namespace osu.Game.Rulesets.Osu.OsuDifficulty.Preprocessing
{
    /// <summary>
    /// A wrapper around <see cref="OsuHitObject"/> extending it with additional data required for difficulty calculation.
    /// </summary>
    public class OsuDifficultyHitObject
    {
        /// <summary>
        /// The <see cref="OsuHitObject"/> this <see cref="OsuDifficultyHitObject"/> refers to.
        /// </summary>
        public OsuHitObject BaseObject { get; }

        /// <summary>
        /// Normalized distance from the <see cref="OsuHitObject.StackedPosition"/> of the previous <see cref="OsuDifficultyHitObject"/>.
        /// </summary>
        public double JumpDistance { get; private set; }

        /// <summary>
        /// Normalized distance from the <see cref="OsuHitObject.StackedPosition"/> of the previous <see cref="OsuDifficultyHitObject"/>.
        /// </summary>
        public double FlowDistance { get; private set; }

        /// <summary>
        /// Milliseconds elapsed since the StartTime of the previous <see cref="OsuDifficultyHitObject"/>.
        /// </summary>
        public double DeltaTime { get; private set; }

        /// <summary>
        /// Milliseconds elapsed since the StartTime of the last two <see cref="OsuDifficultyHitObject"/>.
        /// </summary>
        public double DeltaTime2 { get; private set; }

        /// <summary>
        /// Number of milliseconds until the <see cref="OsuDifficultyHitObject"/> has to be hit.
        /// </summary>
        public double TimeUntilHit { get; set; }

        private const int normalized_radius = 52;

        private readonly double timeRate;

        private readonly OsuHitObject[] t;

        /// <summary>
        /// Initializes the object calculating extra data required for difficulty calculation.
        /// </summary>
        public OsuDifficultyHitObject(OsuHitObject[] triangle, double timeRate)
        {
            this.timeRate = timeRate;

            t = triangle;
            BaseObject = t[0];
            setDistances();
            setTimingValues();
            // Calculate angle here
        }

        private void setDistances()
        {
            // We will scale distances by this factor, so we can assume a uniform CircleSize among beatmaps.
            double scalingFactor = normalized_radius / BaseObject.Radius;
            if (BaseObject.Radius < 40)
            {
                double smallCircleBonus = (40 - BaseObject.Radius) / 50;
                scalingFactor *= 1 + smallCircleBonus;
            }

            var lastSlider = t[1] as Slider;

            double lastTravelDistance = 0;

            if (lastSlider != null)
            {
                computeSliderCursorPosition(lastSlider);
                lastTravelDistance = lastSlider.LazyTravelDistance;
            }

            FlowDistance = (lastTravelDistance + (BaseObject.StackedPosition - t[2].StackedPosition).Length) * scalingFactor;
            JumpDistance = Math.Max(0.000001, (BaseObject.StackedPosition - t[1].StackedPosition).Length * scalingFactor -  FlowDistance / 2);
        }

        private void setTimingValues()
        {
            // Every timing inverval is hard capped at the equivalent of 375 BPM streaming speed as a safety measure.
            DeltaTime = Math.Max(40, (t[0].StartTime - t[1].StartTime) / timeRate);
            var lastSlider = t[1] as Slider;
            var lastFlowpoint = lastSlider ?? t[2];
            DeltaTime2 = Math.Max(40, (t[0].StartTime - lastFlowpoint.StartTime) / timeRate);
            TimeUntilHit = 450; // BaseObject.PreEmpt;
        }

        private void computeSliderCursorPosition(Slider slider)
        {
            if (slider.LazyEndPosition != null)
                return;
            slider.LazyEndPosition = slider.StackedPosition;

            float approxFollowCircleRadius = (float)(slider.Radius * 3);
            var computeVertex = new Action<double>(t =>
            {
                // ReSharper disable once PossibleInvalidOperationException (bugged in current r# version)
                var diff = slider.StackedPositionAt(t) - slider.LazyEndPosition.Value;
                float dist = diff.Length;

                if (dist > approxFollowCircleRadius)
                {
                    // The cursor would be outside the follow circle, we need to move it
                    diff.Normalize(); // Obtain direction of diff
                    dist -= approxFollowCircleRadius;
                    slider.LazyEndPosition += diff * dist;
                    slider.LazyTravelDistance += dist;
                }
            });

            var scoringTimes = slider.NestedHitObjects.Select(t => t.StartTime);
            foreach (var time in scoringTimes)
                computeVertex(time);
            computeVertex(slider.EndTime);
        }
    }
}
