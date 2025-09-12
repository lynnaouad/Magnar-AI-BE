// <copyright file="TextChunker.cs" company="Magnar Systems">
// Copyright (c) Magnar Systems. All rights reserved.
// </copyright>

using System.Text;
using System.Text.RegularExpressions;

/// <summary>
/// The TextChunker class provides intelligent text splitting functionality for preparing documents for embedding and Retrieval-Augmented Generation (RAG) systems.
/// It breaks large text documents into smaller, semantically meaningful chunks while preserving context and structure.
/// </summary>
public static class TextChunker
{
    private const double CHARSPERTOKEN = 4.0; // Average characters per token for OpenAI models
    private const int MINCHUNKSIZE = 50; // Minimum meaningful chunk size in tokens (50)
    private const double OVERLAPRATIO = 0.2; // Percentage of overlap between chunks when enabled (20%)

    private static readonly Regex BlockSplitRegex = new Regex(@"(\n{2,}|(?<=:)\n)", RegexOptions.Compiled); // Compiled regex for splitting text into semantic blocks (paragraphs and sections)

    private static readonly Regex SentenceSplitRegex = new Regex(@"(?<=[\.!\?])\s+", RegexOptions.Compiled); // Compiled regex for splitting text into sentences

    private static readonly Regex WordSplitRegex = new Regex(@"\s+", RegexOptions.Compiled); // Compiled regex for splitting text into individual words

    /// <summary>
    /// Splits text into semantic chunks with specified maximum token count.
    /// 1. Validates input parameters.
    /// 2. Normalizes the input text.
    /// 3. Splits text into semantic blocks (paragraphs/sections).
    /// 4. Processes each block to fit within token limits.
    /// 5. Handles oversized blocks by further subdivision.
    /// 6. Optionally adds overlap between chunks.
    /// 7. Returns clean, non-empty chunks.
    /// </summary>
    /// <param name="text">Input text to chunk.</param>
    /// <param name="maxTokens">Maximum tokens per chunk.</param>
    /// <param name="preserveStructure">Whether to preserve document structure (paragraphs, sections).</param>
    /// <param name="addOverlap">Whether to add overlap between chunks for better context.</param>
    /// <returns>List of text chunks.</returns>
    public static List<string> ChunkText(string text, int maxTokens = 512, bool preserveStructure = true, bool addOverlap = false)
    {
        // Input validation
        if (string.IsNullOrWhiteSpace(text))
        {
            return [];
        }

        if (maxTokens <= MINCHUNKSIZE)
        {
            throw new ArgumentException($"maxTokens must be greater than {MINCHUNKSIZE}", nameof(maxTokens));
        }

        var chunks = new List<string>();
        var currentChunk = new StringBuilder();
        int currentTokenEstimate = 0;
        int overlapTokens = addOverlap ? (int)(maxTokens * OVERLAPRATIO) : 0;
        string previousChunkEnd = string.Empty;

        // Normalize text - remove excessive whitespace but preserve structure
        text = NormalizeText(text, preserveStructure);

        // Split by semantic blocks (paragraphs, sections)
        var blocks = preserveStructure
            ? BlockSplitRegex.Split(text).Where(b => !string.IsNullOrWhiteSpace(b)).ToArray()
            : [text];

        foreach (var block in blocks)
        {
            var trimmedBlock = block.Trim();
            if (string.IsNullOrEmpty(trimmedBlock))
            {
                continue;
            }

            int blockTokenEstimate = EstimateTokenCount(trimmedBlock);

            // Handle oversized blocks by splitting into smaller units
            if (blockTokenEstimate > maxTokens)
            {
                var subChunks = SplitOversizedBlock(trimmedBlock, maxTokens);
                foreach (var subChunk in subChunks)
                {
                    ProcessChunk(subChunk, chunks, currentChunk, ref currentTokenEstimate, maxTokens, addOverlap, overlapTokens, ref previousChunkEnd);
                }

                continue;
            }

            // Check if block fits in current chunk
            if (currentTokenEstimate + blockTokenEstimate > maxTokens && currentChunk.Length > 0)
            {
                FinishCurrentChunk(chunks, currentChunk, ref currentTokenEstimate, addOverlap, ref previousChunkEnd);
            }

            // Add overlap from previous chunk if needed
            if (currentChunk.Length == 0 && addOverlap && !string.IsNullOrEmpty(previousChunkEnd))
            {
                currentChunk.Append(previousChunkEnd);
                currentTokenEstimate += EstimateTokenCount(previousChunkEnd);
            }

            // Add block to current chunk
            if (currentChunk.Length > 0 && preserveStructure)
            {
                currentChunk.AppendLine();
            }

            currentChunk.Append(trimmedBlock);
            currentTokenEstimate += blockTokenEstimate;
        }

        // Add final chunk if it exists
        if (currentChunk.Length > 0)
        {
            var finalChunk = currentChunk.ToString().Trim();
            if (!string.IsNullOrEmpty(finalChunk))
            {
                chunks.Add(finalChunk);
            }
        }

        return chunks.Where(c => !string.IsNullOrWhiteSpace(c)).ToList();
    }

    #region Private Methods

    /// <summary>
    /// Processes individual chunks and manages the chunking state.
    /// 1. Checks if adding a chunk would exceed token limits.
    /// 2. Finishes current chunk if limit would be exceeded.
    /// 3. Adds overlap from previous chunk if enabled.
    /// 4. Appends new content to current chunk.
    /// 5. Updates token count estimates.
    /// </summary>
    private static void ProcessChunk(string chunk, List<string> chunks, StringBuilder currentChunk,  ref int currentTokenEstimate, int maxTokens, bool addOverlap, int overlapTokens, ref string previousChunkEnd)
    {
        int chunkTokens = EstimateTokenCount(chunk);

        if (currentTokenEstimate + chunkTokens > maxTokens && currentChunk.Length > 0)
        {
            FinishCurrentChunk(chunks, currentChunk, ref currentTokenEstimate, addOverlap, ref previousChunkEnd);
        }

        // Add overlap if starting new chunk
        if (currentChunk.Length == 0 && addOverlap && !string.IsNullOrEmpty(previousChunkEnd))
        {
            currentChunk.Append(previousChunkEnd + " ");
            currentTokenEstimate += EstimateTokenCount(previousChunkEnd);
        }

        currentChunk.Append(chunk + " ");
        currentTokenEstimate += chunkTokens;
    }

    /// <summary>
    /// Completes and stores the current chunk being built.
    /// 1. Converts StringBuilder content to final chunk string.
    /// 2. Adds completed chunk to the results list.
    /// 3. Extracts end portion for overlap with next chunk (if enabled).
    /// 4. Resets the current chunk builder and token counter.
    /// </summary>
    private static void FinishCurrentChunk(List<string> chunks, StringBuilder currentChunk, ref int currentTokenEstimate, bool addOverlap, ref string previousChunkEnd)
    {
        var chunkText = currentChunk.ToString().Trim();
        if (!string.IsNullOrEmpty(chunkText))
        {
            chunks.Add(chunkText);

            // Store end portion for overlap
            if (addOverlap)
            {
                previousChunkEnd = GetChunkEnd(chunkText, (int)(EstimateTokenCount(chunkText) * OVERLAPRATIO));
            }
        }

        currentChunk.Clear();
        currentTokenEstimate = 0;
    }

    /// <summary>
    /// Handles text blocks that are too large for a single chunk.
    /// 1. First attempts to split by sentences.
    /// 2. Groups sentences together until token limit is approached.
    /// 3. For sentences that are still too large, calls SplitByWords().
    /// 4. Returns a list of appropriately sized sub-chunks.
    /// 5. Maintains semantic meaning as much as possible.
    /// </summary>
    private static List<string> SplitOversizedBlock(string block, int maxTokens)
    {
        var result = new List<string>();

        // Try splitting by sentences first
        var sentences = SentenceSplitRegex.Split(block)
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Select(s => s.Trim())
            .ToArray();

        var currentGroup = new StringBuilder();
        int currentTokens = 0;

        foreach (var sentence in sentences)
        {
            int sentenceTokens = EstimateTokenCount(sentence);

            // If single sentence is too large, split by words
            if (sentenceTokens > maxTokens)
            {
                if (currentGroup.Length > 0)
                {
                    result.Add(currentGroup.ToString().Trim());
                    currentGroup.Clear();
                    currentTokens = 0;
                }

                result.AddRange(SplitByWords(sentence, maxTokens));
                continue;
            }

            // If adding sentence would exceed limit, finish current group
            if (currentTokens + sentenceTokens > maxTokens && currentGroup.Length > 0)
            {
                result.Add(currentGroup.ToString().Trim());
                currentGroup.Clear();
                currentTokens = 0;
            }

            if (currentGroup.Length > 0)
            {
                currentGroup.Append(' ');
            }

            currentGroup.Append(sentence);
            currentTokens += sentenceTokens;
        }

        // Add final group
        if (currentGroup.Length > 0)
        {
            result.Add(currentGroup.ToString().Trim());
        }

        return result;
    }

    /// <summary>
    /// Last resort splitting method for extremely large sentences.
    /// 1. Splits text into individual words.
    /// 2. Groups words together until token limit is reached.
    /// 3. Creates new chunks when limit would be exceeded.
    /// 4. Ensures no words are lost in the process.
    /// 5. Used when sentence-level splitting isn't sufficient.
    /// </summary>
    private static List<string> SplitByWords(string text, int maxTokens)
    {
        var result = new List<string>();
        var words = WordSplitRegex.Split(text).Where(w => !string.IsNullOrWhiteSpace(w)).ToArray();

        var currentChunk = new StringBuilder();
        int currentTokens = 0;

        foreach (var word in words)
        {
            int wordTokens = EstimateTokenCount(word);

            if (currentTokens + wordTokens > maxTokens && currentChunk.Length > 0)
            {
                result.Add(currentChunk.ToString().Trim());
                currentChunk.Clear();
                currentTokens = 0;
            }

            if (currentChunk.Length > 0)
            {
                currentChunk.Append(" ");
            }

            currentChunk.Append(word);
            currentTokens += wordTokens;
        }

        if (currentChunk.Length > 0)
        {
            result.Add(currentChunk.ToString().Trim());
        }

        return result;
    }

    /// <summary>
    /// Extracts the ending portion of a chunk for overlap purposes.
    /// 1. Works backwards from the end of a chunk.
    /// 2. Collects complete sentences up to the specified token limit.
    /// 3. Ensures overlap content maintains sentence boundaries.
    /// 4.Returns the extracted text for use in the next chunk's beginning.
    /// </summary>
    private static string GetChunkEnd(string chunk, int maxTokens)
    {
        if (string.IsNullOrEmpty(chunk))
        {
            return string.Empty;
        }

        var sentences = SentenceSplitRegex.Split(chunk).Reverse().ToArray();
        var result = new StringBuilder();
        int tokens = 0;

        foreach (var sentence in sentences)
        {
            var trimmed = sentence.Trim();
            if (string.IsNullOrEmpty(trimmed))
            {
                continue;
            }

            int sentenceTokens = EstimateTokenCount(trimmed);
            if (tokens + sentenceTokens > maxTokens)
            {
                break;
            }

            if (result.Length > 0)
            {
                result.Insert(0, " ");
            }

            result.Insert(0, trimmed);
            tokens += sentenceTokens;
        }

        return result.ToString();
    }

    /// <summary>
    /// Standardizes text formatting for consistent processing.
    /// 1. Removes excessive whitespace (multiple spaces become single spaces).
    /// 2. Standardizes line endings (converts \r\n and \r to \n).
    /// 3. When preserveStructure is true: preserves paragraph breaks and structure.
    /// 4. When preserveStructure is false: more aggressive whitespace removal.
    /// 5. Trims leading and trailing whitespace.
    /// </summary>
    private static string NormalizeText(string text, bool preserveStructure)
    {
        if (preserveStructure)
        {
            // Remove excessive whitespace but preserve paragraph breaks
            return Regex.Replace(text, @"[ \t]+", " ") // Multiple spaces/tabs to single space
                       .Replace("\r\n", "\n") // Normalize line endings
                       .Replace("\r", "\n")
                       .Trim();
        }
        else
        {
            // More aggressive normalization
            return Regex.Replace(text, @"\s+", " ").Trim();
        }
    }

    /// <summary>
    /// Provides approximate token count for text segments.
    /// 1. Uses the average characters-per-token ratio (4.0 for OpenAI models).
    /// 2. Calculates estimated tokens by dividing character count by this ratio.
    /// 3. Ensures minimum token count of 1 for non-empty text.
    /// 4. Rounds up to ensure conservative estimates (prevents token limit overruns).
    /// </summary>
    private static int EstimateTokenCount(string text)
    {
        return string.IsNullOrEmpty(text) ? 0 : Math.Max(1, (int)Math.Ceiling(text.Length / CHARSPERTOKEN));
    }

    #endregion
}