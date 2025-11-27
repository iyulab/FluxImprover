using FluxImprover;
using FluxImprover.Abstractions.Models;
using FluxImprover.Abstractions.Options;
using FluxImprover.ConsoleDemo;
using FluxImprover.QAGeneration;
using FluxImprover.QuestionSuggestion;
using Microsoft.Extensions.Configuration;

Console.OutputEncoding = System.Text.Encoding.UTF8;
Console.WriteLine("╔══════════════════════════════════════════════════════════════════╗");
Console.WriteLine("║           FluxImprover - Real API Integration Test               ║");
Console.WriteLine("╚══════════════════════════════════════════════════════════════════╝");
Console.WriteLine();

// Find project root by looking for .env.local file
string? FindProjectRoot(string startDir)
{
    var dir = new DirectoryInfo(startDir);
    while (dir != null)
    {
        if (File.Exists(Path.Combine(dir.FullName, ".env.local")))
            return dir.FullName;
        dir = dir.Parent;
    }
    return null;
}

var projectRoot = FindProjectRoot(Directory.GetCurrentDirectory()) ?? Directory.GetCurrentDirectory();

// Load configuration from .env.local
var config = new ConfigurationBuilder()
    .SetBasePath(projectRoot)
    .AddJsonFile("appsettings.json", optional: true)
    .AddEnvironmentVariables()
    .Build();

// Try to load API key from environment or .env.local file
var apiKey = config["OPENAI_API_KEY"];
// Use gpt-4o-mini as default (ignore invalid model names like gpt-5-nano)
var configModel = config["OPENAI_MODEL"];
var model = configModel?.StartsWith("gpt-4") == true || configModel?.StartsWith("gpt-3") == true
    ? configModel
    : "gpt-4o-mini";

// Fallback: read from .env.local file directly if env var not set
if (string.IsNullOrEmpty(apiKey))
{
    var envLocalPath = Path.Combine(projectRoot, ".env.local");
    if (File.Exists(envLocalPath))
    {
        var lines = await File.ReadAllLinesAsync(envLocalPath);
        foreach (var line in lines)
        {
            var parts = line.Split('=', 2);
            if (parts.Length == 2)
            {
                var key = parts[0].Trim();
                var value = parts[1].Trim();
                if (key == "OPENAI_API_KEY") apiKey = value;
                // Only use valid model names
                if (key == "OPENAI_MODEL" && (value.StartsWith("gpt-4") || value.StartsWith("gpt-3")))
                    model = value;
            }
        }
    }
}

if (string.IsNullOrEmpty(apiKey))
{
    Console.WriteLine("❌ OPENAI_API_KEY not found. Please set it in environment or .env.local file.");
    return 1;
}

Console.WriteLine($"📌 Using model: {model}");
Console.WriteLine();

// Create OpenAI completion service
using var completionService = new OpenAICompletionService(apiKey, model);

// Build FluxImprover services
var services = new FluxImproverBuilder()
    .WithCompletionService(completionService)
    .Build();

var testsPassed = 0;
var testsFailed = 0;

// ═══════════════════════════════════════════════════════════════════
// Test 1: Summarization Service
// ═══════════════════════════════════════════════════════════════════
Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
Console.WriteLine("📝 Test 1: Summarization Service");
Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
try
{
    var content = """
        Machine learning is a subset of artificial intelligence (AI) that enables systems
        to learn and improve from experience without being explicitly programmed. It focuses
        on developing computer programs that can access data and use it to learn for themselves.
        The process begins with observations or data, such as examples, direct experience,
        or instruction, to look for patterns in data and make better decisions in the future.
        """;

    Console.WriteLine("Input text:");
    Console.WriteLine($"  {content[..100]}...");
    Console.WriteLine();

    var summary = await services.Summarization.SummarizeAsync(content);

    Console.WriteLine("Generated summary:");
    Console.WriteLine($"  {summary}");
    Console.WriteLine();
    Console.WriteLine("✅ Summarization test PASSED");
    testsPassed++;
}
catch (Exception ex)
{
    Console.WriteLine($"❌ Summarization test FAILED: {ex.Message}");
    testsFailed++;
}
Console.WriteLine();

// ═══════════════════════════════════════════════════════════════════
// Test 2: Keyword Extraction Service
// ═══════════════════════════════════════════════════════════════════
Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
Console.WriteLine("🔑 Test 2: Keyword Extraction Service");
Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
try
{
    var content = """
        Paris is the capital and most populous city of France. The city has an estimated
        population of 2,161,000 residents. Located along the Seine River, Paris has been
        nicknamed the "City of Light" and is a global center for art, fashion, gastronomy,
        and culture. The Eiffel Tower and Louvre Museum are among its most famous landmarks.
        """;

    Console.WriteLine("Input text:");
    Console.WriteLine($"  {content[..80]}...");
    Console.WriteLine();

    var keywords = await services.KeywordExtraction.ExtractKeywordsAsync(content);

    Console.WriteLine("Extracted keywords:");
    foreach (var kw in keywords.Take(5))
    {
        Console.WriteLine($"  • {kw}");
    }
    Console.WriteLine();
    Console.WriteLine("✅ Keyword Extraction test PASSED");
    testsPassed++;
}
catch (Exception ex)
{
    Console.WriteLine($"❌ Keyword Extraction test FAILED: {ex.Message}");
    testsFailed++;
}
Console.WriteLine();

// ═══════════════════════════════════════════════════════════════════
// Test 3: Chunk Enrichment Service
// ═══════════════════════════════════════════════════════════════════
Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
Console.WriteLine("📦 Test 3: Chunk Enrichment Service");
Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
try
{
    var chunk = new Chunk
    {
        Id = "chunk-001",
        Content = """
            React is a free and open-source front-end JavaScript library for building
            user interfaces based on components. It was developed by Meta (formerly Facebook)
            and is maintained by a community of individual developers and companies.
            React uses a virtual DOM to efficiently update and render components.
            """
    };

    Console.WriteLine($"Input chunk ID: {chunk.Id}");
    Console.WriteLine($"Input content: {chunk.Content[..60]}...");
    Console.WriteLine();

    var enriched = await services.ChunkEnrichment.EnrichAsync(chunk);

    Console.WriteLine("Enriched chunk:");
    Console.WriteLine($"  Summary: {enriched.Summary}");
    Console.WriteLine($"  Keywords: {string.Join(", ", enriched.Keywords?.Take(5) ?? [])}");
    Console.WriteLine();
    Console.WriteLine("✅ Chunk Enrichment test PASSED");
    testsPassed++;
}
catch (Exception ex)
{
    Console.WriteLine($"❌ Chunk Enrichment test FAILED: {ex.Message}");
    testsFailed++;
}
Console.WriteLine();

// ═══════════════════════════════════════════════════════════════════
// Test 4: QA Generation Service
// ═══════════════════════════════════════════════════════════════════
Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
Console.WriteLine("❓ Test 4: QA Generation Service");
Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
try
{
    var context = """
        The solar system consists of the Sun and the celestial objects bound to it by
        gravity. Earth is the third planet from the Sun and the only astronomical object
        known to harbor life. The Moon is Earth's only natural satellite. Mars, the fourth
        planet, is often called the "Red Planet" due to its reddish appearance.
        """;

    Console.WriteLine("Input context:");
    Console.WriteLine($"  {context[..80]}...");
    Console.WriteLine();

    var options = new QAGenerationOptions
    {
        PairsPerChunk = 2,
        QuestionTypes = [QuestionType.Factual]
    };

    var qaPairs = await services.QAGenerator.GenerateAsync(context, options);

    Console.WriteLine("Generated QA pairs:");
    foreach (var qa in qaPairs)
    {
        Console.WriteLine($"  Q: {qa.Question}");
        Console.WriteLine($"  A: {qa.Answer}");
        Console.WriteLine();
    }
    Console.WriteLine("✅ QA Generation test PASSED");
    testsPassed++;
}
catch (Exception ex)
{
    Console.WriteLine($"❌ QA Generation test FAILED: {ex.Message}");
    testsFailed++;
}
Console.WriteLine();

// ═══════════════════════════════════════════════════════════════════
// Test 5: Faithfulness Evaluator
// ═══════════════════════════════════════════════════════════════════
Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
Console.WriteLine("🎯 Test 5: Faithfulness Evaluator");
Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
try
{
    var context = "France is a country in Western Europe. Paris is the capital city of France.";
    var answer = "Paris is the capital of France.";

    Console.WriteLine($"Context: {context}");
    Console.WriteLine($"Answer: {answer}");
    Console.WriteLine();

    var result = await services.Faithfulness.EvaluateAsync(context, answer);

    Console.WriteLine($"Faithfulness Score: {result.Score:P0}");
    Console.WriteLine($"Details: {string.Join(", ", result.Details.Select(kv => $"{kv.Key}={kv.Value}"))}");
    Console.WriteLine();
    Console.WriteLine("✅ Faithfulness Evaluator test PASSED");
    testsPassed++;
}
catch (Exception ex)
{
    Console.WriteLine($"❌ Faithfulness Evaluator test FAILED: {ex.Message}");
    testsFailed++;
}
Console.WriteLine();

// ═══════════════════════════════════════════════════════════════════
// Test 6: Relevancy Evaluator
// ═══════════════════════════════════════════════════════════════════
Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
Console.WriteLine("🔗 Test 6: Relevancy Evaluator");
Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
try
{
    var question = "What is the capital of France?";
    var answer = "Paris is the capital of France.";
    var context = "France is a country in Western Europe. Paris is the capital city of France.";

    Console.WriteLine($"Question: {question}");
    Console.WriteLine($"Answer: {answer}");
    Console.WriteLine();

    var result = await services.Relevancy.EvaluateAsync(question, answer, context: context);

    Console.WriteLine($"Relevancy Score: {result.Score:P0}");
    Console.WriteLine($"Details: {string.Join(", ", result.Details.Select(kv => $"{kv.Key}={kv.Value}"))}");
    Console.WriteLine();
    Console.WriteLine("✅ Relevancy Evaluator test PASSED");
    testsPassed++;
}
catch (Exception ex)
{
    Console.WriteLine($"❌ Relevancy Evaluator test FAILED: {ex.Message}");
    testsFailed++;
}
Console.WriteLine();

// ═══════════════════════════════════════════════════════════════════
// Test 7: Answerability Evaluator
// ═══════════════════════════════════════════════════════════════════
Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
Console.WriteLine("✔️ Test 7: Answerability Evaluator");
Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
try
{
    var context = "Python is a high-level programming language. It was created by Guido van Rossum in 1991.";
    var question = "Who created Python?";

    Console.WriteLine($"Context: {context}");
    Console.WriteLine($"Question: {question}");
    Console.WriteLine();

    var result = await services.Answerability.EvaluateAsync(context, question);

    Console.WriteLine($"Answerability Score: {result.Score:P0}");
    Console.WriteLine($"Details: {string.Join(", ", result.Details.Select(kv => $"{kv.Key}={kv.Value}"))}");
    Console.WriteLine();
    Console.WriteLine("✅ Answerability Evaluator test PASSED");
    testsPassed++;
}
catch (Exception ex)
{
    Console.WriteLine($"❌ Answerability Evaluator test FAILED: {ex.Message}");
    testsFailed++;
}
Console.WriteLine();

// ═══════════════════════════════════════════════════════════════════
// Test 8: Question Suggestion Service
// ═══════════════════════════════════════════════════════════════════
Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
Console.WriteLine("💡 Test 8: Question Suggestion Service");
Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
try
{
    var history = new[]
    {
        new ConversationMessage { Role = "user", Content = "What is Docker?" },
        new ConversationMessage { Role = "assistant", Content = "Docker is a platform for developing, shipping, and running applications in containers. Containers allow you to package an application with all its dependencies." }
    };

    Console.WriteLine("Conversation history:");
    foreach (var msg in history)
    {
        Console.WriteLine($"  [{msg.Role}]: {msg.Content[..Math.Min(60, msg.Content.Length)]}...");
    }
    Console.WriteLine();

    var options = new QuestionSuggestionOptions
    {
        MaxSuggestions = 3,
        Categories = [QuestionCategory.DeepDive, QuestionCategory.Related]
    };

    var suggestions = await services.QuestionSuggestion.SuggestFromConversationAsync(history, options);

    Console.WriteLine("Suggested follow-up questions:");
    foreach (var suggestion in suggestions)
    {
        Console.WriteLine($"  • [{suggestion.Category}] {suggestion.Text}");
        Console.WriteLine($"    Relevance: {suggestion.Relevance:P0}");
    }
    Console.WriteLine();
    Console.WriteLine("✅ Question Suggestion test PASSED");
    testsPassed++;
}
catch (Exception ex)
{
    Console.WriteLine($"❌ Question Suggestion test FAILED: {ex.Message}");
    testsFailed++;
}
Console.WriteLine();

// ═══════════════════════════════════════════════════════════════════
// Test 9: QA Pipeline (End-to-End)
// ═══════════════════════════════════════════════════════════════════
Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
Console.WriteLine("🔄 Test 9: QA Pipeline (End-to-End)");
Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
try
{
    var chunks = new[]
    {
        new Chunk
        {
            Id = "pipeline-1",
            Content = "Kubernetes is an open-source container orchestration platform. It automates deployment, scaling, and management of containerized applications."
        }
    };

    Console.WriteLine($"Processing {chunks.Length} chunk(s) through QA Pipeline...");
    Console.WriteLine();

    var pipelineOptions = new QAPipelineOptions
    {
        GenerationOptions = new QAGenerationOptions { PairsPerChunk = 2 },
        FilterOptions = new QAFilterOptions
        {
            MinFaithfulness = 0.5,
            MinRelevancy = 0.5,
            MinAnswerability = 0.5
        }
    };

    var results = await services.QAPipeline.ExecuteFromChunksBatchAsync(chunks, pipelineOptions);

    var totalGenerated = results.Sum(r => r.GeneratedCount);
    var totalFiltered = results.Sum(r => r.FilteredCount);

    Console.WriteLine($"Pipeline Results:");
    Console.WriteLine($"  Total Generated: {totalGenerated}");
    Console.WriteLine($"  Total Passed: {totalFiltered}");
    Console.WriteLine($"  Pass Rate: {(totalGenerated > 0 ? (double)totalFiltered / totalGenerated : 0):P0}");
    Console.WriteLine();

    var allQAPairs = results.SelectMany(r => r.QAPairs).ToList();
    if (allQAPairs.Count > 0)
    {
        Console.WriteLine("Passed QA Pairs:");
        foreach (var qa in allQAPairs.Take(3))
        {
            Console.WriteLine($"  Q: {qa.Question}");
            Console.WriteLine($"  A: {qa.Answer}");
            Console.WriteLine();
        }
    }

    Console.WriteLine("✅ QA Pipeline test PASSED");
    testsPassed++;
}
catch (Exception ex)
{
    Console.WriteLine($"❌ QA Pipeline test FAILED: {ex.Message}");
    testsFailed++;
}
Console.WriteLine();

// ═══════════════════════════════════════════════════════════════════
// Summary
// ═══════════════════════════════════════════════════════════════════
Console.WriteLine("╔══════════════════════════════════════════════════════════════════╗");
Console.WriteLine("║                         TEST SUMMARY                             ║");
Console.WriteLine("╚══════════════════════════════════════════════════════════════════╝");
Console.WriteLine();
Console.WriteLine($"  ✅ Passed: {testsPassed}");
Console.WriteLine($"  ❌ Failed: {testsFailed}");
Console.WriteLine($"  📊 Total:  {testsPassed + testsFailed}");
Console.WriteLine();

if (testsFailed == 0)
{
    Console.WriteLine("🎉 All tests passed successfully!");
    return 0;
}
else
{
    Console.WriteLine($"⚠️ {testsFailed} test(s) failed. Please check the output above.");
    return 1;
}
