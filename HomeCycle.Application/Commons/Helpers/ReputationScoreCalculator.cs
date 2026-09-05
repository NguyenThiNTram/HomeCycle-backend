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
        // Điểm gốc mặc định khi chưa có vi phạm vận hành.
        public const double DefaultBaseScore = 100.0;

        // Điểm sao trung bình kỳ vọng của toàn sàn (giá trị mặc định khi chưa có đánh giá).
        public const double PlatformTarget = 4.8;

        // Số lượng đánh giá tối thiểu để điểm thực tế có trọng số mạnh.
        public const double ConfidenceWeight = 5.0;

        /// <summary>
        /// Tính Điểm Uy Tín Vận Hành (thang 100) theo cơ chế Hệ Thống Điểm Phạt:
        /// Score = Clamp(baseScore - penaltyPoints, 0, 100).
        /// Đánh giá KHÔNG trừ trực tiếp điểm vận hành; chỉ vi phạm vận hành
        /// (huỷ đơn, giao trễ, hàng giả...) mới sinh penaltyPoints.
        /// Thang phạt tham khảo theo sao (dùng khi nối dây hệ thống phạt):
        ///   5 sao: +0 | 4 sao: -1~3 | 3 sao: -5~10 | 2 sao: -15~25 | 1 sao: -30~50
        /// </summary>
        public static double CalculateReputationScore(double baseScore, double penaltyPoints = 0)
        {
            double score = Math.Clamp(baseScore - penaltyPoints, 0.0, 100.0);
            return Math.Round(score, 1, MidpointRounding.AwayFromZero);
        }

        /// <summary>
        /// Tính Điểm Sao Hiển Thị (thang 5) theo Bayesian Average:
        /// Rating = (ConfidenceWeight * PlatformTarget + tổng sao thực) / (ConfidenceWeight + số review).
        /// Ngăn việc 1 đánh giá thấp làm sụp đổ điểm sao hiển thị của Shop.
        /// </summary>
        public static double CalculateDisplayStarRating(IReadOnlyCollection<review> validReviews)
        {
            int totalReviews = validReviews.Count;
            if (totalReviews == 0)
                return PlatformTarget;

            double totalStarPoints = validReviews.Sum(r => r.Rating ?? 0);
            double rating = (ConfidenceWeight * PlatformTarget + totalStarPoints) / (ConfidenceWeight + totalReviews);

            rating = Math.Clamp(rating, 1.0, 5.0);
            return Math.Round(rating, 2, MidpointRounding.AwayFromZero);
        }

        public static int ApplyDelta(int currentScore, int pointDelta)
        {
            return Math.Clamp(currentScore + pointDelta, MinScore, MaxScore);
        }
    }
}