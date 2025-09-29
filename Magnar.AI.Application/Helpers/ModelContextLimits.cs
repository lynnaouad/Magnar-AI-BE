namespace Magnar.AI.Application.Helpers
{
    public static class ModelContextLimits
    {
        private static readonly Dictionary<string, int> Limits = new()
        {
            // OpenAI models
            { "gpt-4", 8_000 },
            { "gpt-4o", 128_000 },
            { "gpt-4o-mini", 128_000 },
            { "gpt-4.1", 128_000 },
            { "gpt-4.1-mini", 128_000 },
            { "gpt-3.5-turbo", 16_000 },

            { "claude-3-opus", 200_000 },
            { "claude-3-sonnet", 200_000 },
            { "claude-3-haiku", 200_000 },
        };

        public static int GetLimit(string modelId)
        {
            if (Limits.TryGetValue(modelId, out var limit))
            {
                return limit;
            }
                
            // Default fallback
            return 16_000;
        }
    }

}
