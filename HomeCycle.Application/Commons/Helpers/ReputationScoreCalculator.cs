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
        // Điểm trung lập mặc định: 3 sao.
        public const double NeutralStars = 3.0;

        // Trọng số khởi tạo: tương đương 5 lượt đánh giá.
        public const int InitialWeight = 5;

        public const int MinScore = 1;
        public const int MaxScore = 100;

        /// <summary>
        /// Tính điểm uy tín (1-100) của người dùng dựa trên các đánh giá hợp lệ.
        /// AdjustedStars = (S + 5*3) / (n + 5)
        /// ReputationScore = Clamp(Round((AdjustedStars - 1) * 25), 1, 100)
        /// </summary>
        public static int Calculate(IReadOnlyCollection<review> validReviews)
        {
            int n = validReviews.Count;
            int sumOfStars = validReviews.Sum(r => r.Rating ?? 0);

            double adjustedStars = (sumOfStars + (InitialWeight * NeutralStars)) / (n + InitialWeight);
            double rawScore = (adjustedStars - 1.0) * 25.0;

            return (int)Math.Clamp(Math.Round(rawScore, MidpointRounding.AwayFromZero), MinScore, MaxScore);
        }

        public static int ApplyDelta(int currentScore, int pointDelta)
        {
            return Math.Clamp(currentScore + pointDelta, MinScore, MaxScore);
        }
    }
}
