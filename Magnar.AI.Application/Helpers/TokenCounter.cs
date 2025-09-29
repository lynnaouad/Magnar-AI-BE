using SharpToken;
using static Magnar.AI.Static.Constants;

namespace Magnar.AI.Application.Helpers
{
    public static class TokenCounter
    {
        /// <summary>
        /// Count tokens for text given model.
        /// </summary>
        public static int CountTokens(string text, string modelId = AiModels.Gpt4omini)
        {
            if (string.IsNullOrEmpty(text))
            {
                return 0;
            }

            var encoding = GetEncodingForModel(modelId);
            return encoding.Encode(text).Count;
        }

        /// <summary>
        /// Count tokens for multiple messages combined.
        /// </summary>
        public static int CountTokens(IEnumerable<string> texts, string modelId = AiModels.Gpt4omini)
        {
            var encoding = GetEncodingForModel(modelId);

            return texts.Where(t => !string.IsNullOrEmpty(t)).Sum(t => encoding.Encode(t).Count);
        }

        #region Private Methods
        /// <summary>
        /// Get correct tokenizer based on model name.
        /// </summary>
        private static GptEncoding GetEncodingForModel(string modelId)
        {
            if (string.IsNullOrWhiteSpace(modelId))
            {
                return GptEncoding.GetEncoding("cl100k_base");
            }            

            modelId = modelId.ToLowerInvariant();

            // GPT-4o, GPT-4.1, GPT-4-turbo, GPT-3.5-turbo
            if (modelId.Contains(AiModels.Gpt4o) ||
                modelId.Contains(AiModels.Gpt41) ||
                modelId.Contains(AiModels.Gpt4) ||
                modelId.Contains(AiModels.Gpt35))
            {
                return GptEncoding.GetEncoding("cl100k_base");
            }

            // GPT-3 (davinci, curie, babbage, ada) → p50k_base
            if (modelId.Contains(AiModels.Davinci) ||
                modelId.Contains(AiModels.Curie) ||
                modelId.Contains(AiModels.Babbage) ||
                modelId.Contains(AiModels.Ada))
            {
                return GptEncoding.GetEncoding("p50k_base");
            }

            // Newer OpenAI models with 200k+ context (o200k_base) 
            if (modelId.Contains(AiModels.Gpt41) && modelId.Contains("128k"))
            {
                return GptEncoding.GetEncoding("o200k_base");
            }

            // Default fallback → cl100k_base (safe for chat models)
            return GptEncoding.GetEncoding("cl100k_base");
        }
        #endregion
    }
}
