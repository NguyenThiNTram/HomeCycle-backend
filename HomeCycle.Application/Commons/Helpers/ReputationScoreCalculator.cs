using HomeCycle.Application.DTOs.Configs;
using HomeCycle.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Application.Commons.Helpers
{
    public static class ReputationScoreCalculator
    {
        public static double CalculateReputationScore(double baseScore, double penaltyPoints = 0)
        {
            double score = Math.Clamp(baseScore - penaltyPoints, 0.0, 100.0);
            return Math.Round(score, 1, MidpointRounding.AwayFromZero);
        }

        public static double CalculateDisplayStarRating(
            IReadOnlyCollection<review> validReviews,
            RatingPolicyConfigDto policy)
        {
            int totalReviews = validReviews.Count;
            if (totalReviews == 0)
                return policy.PriorMean;

            double totalStarPoints = validReviews.Sum(r => r.Rating ?? 0);
            double rating = (policy.PriorWeight * policy.PriorMean + totalStarPoints) /
                (policy.PriorWeight + totalReviews);

            rating = Math.Clamp(rating, 1.0, 5.0);
            return Math.Round(rating, 2, MidpointRounding.AwayFromZero);
        }

        public static int ApplyDelta(int currentScore, int pointDelta)
        {
            return Math.Clamp(currentScore + pointDelta, 0, 100);
        }

        public static int GetRatingPoints(int rating, RatingPolicyConfigDto policy)
        {
            return rating switch
            {
                5 => policy.FiveStarPoints,
                4 => policy.FourStarPoints,
                3 => policy.ThreeStarPoints,
                2 => policy.TwoStarPoints,
                1 => policy.OneStarPoints,
                _ => 0
            };
        }

        public static (int Score, int AppliedDelta) ApplyRatingDelta(
            int currentScore,
            int requestedDelta,
            RatingPolicyConfigDto policy)
        {
            var score = Math.Clamp(
                currentScore + requestedDelta,
                policy.MinimumReputationScore,
                policy.MaximumReputationScore);

            return (score, score - currentScore);
        }
    }
}
