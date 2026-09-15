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
        public string MarketSearchModel { get; set; } = "gemini-3.6-flash";
        public string PriceSuggestionModel { get; set; } = "gemini-3.5-flash-lite";
        public string BuyMatchingModel { get; set; } = "gemini-3.5-flash-lite";
        public string BusinessHomeModel { get; set; } = "gemini-3.5-flash-lite";
        public int TimeoutSeconds { get; set; } = 15;
        public int MaxOutputTokens { get; set; } = 1200;
        public bool SearchGroundingEnabled { get; set; } = false;
        public int RequestsPerMinute { get; set; } = 15;
        public int ResponseCacheMinutes { get; set; } = 10;
    }
}
