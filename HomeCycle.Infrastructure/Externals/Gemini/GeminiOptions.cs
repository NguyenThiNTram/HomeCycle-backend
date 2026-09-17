using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HomeCycle.Infrastructure.Externals.Gemini
{
    public sealed class GeminiOptions
    {
        public string ApiKey { get; set; } = "";
        public string MarketSearchModel { get; set; } = "gemini-3.5-flash-lite";
        public string PriceSuggestionModel { get; set; } = "gemini-3.5-flash-lite";
        public string BuyMatchingModel { get; set; } = "gemini-3.5-flash-lite";
        public string BusinessHomeModel { get; set; } = "gemini-3.5-flash-lite";
        public int TimeoutSeconds { get; set; } = 15;
        public int MaxOutputTokens { get; set; } = 1200;
        public bool SearchGroundingEnabled { get; set; } = false;
        public bool ExternalUsedPriceSearchEnabled { get; set; } = true;
        public int ExternalUsedPriceSearchCacheHours { get; set; } = 24;
        public int ExternalUsedPriceNegativeCacheMinutes { get; set; } = 30;
        public int ExternalUsedPriceSearchMaxResults { get; set; } = 5;
        public int ExternalUsedPriceSearchTimeoutSeconds { get; set; } = 25;
        public int ExternalUsedPriceSearchMaxOutputTokens { get; set; } = 600;
        public int ExternalUsedPriceExtractionTimeoutSeconds { get; set; } = 15;
        public int ExternalUsedPriceExtractionMaxOutputTokens { get; set; } = 600;
        public int RequestsPerMinute { get; set; } = 15;
        public int ResponseCacheMinutes { get; set; } = 10;
    }
}
