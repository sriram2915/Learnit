using Learnit.Server.Services;
using Microsoft.Extensions.Configuration;

namespace Learnit.Server
{
    /// <summary>
    /// Simple test class to verify AI provider configuration
    /// Run this with: dotnet run --project Learnit.Server -- test-ai
    /// </summary>
    public class TestAiProvider
    {
        public static async Task TestAsync(IConfiguration config)
        {
            Console.WriteLine("=== Testing AI Provider Configuration ===\n");

            // Check configuration
            var apiKey = config["Groq:ApiKey"]
                        ?? config["GROQ_API_KEY"]
                        ?? config["OpenAi:ApiKey"]
                        ?? config["OPENAI_API_KEY"];

            if (string.IsNullOrWhiteSpace(apiKey))
            {
                Console.WriteLine("❌ ERROR: No API key found!");
                Console.WriteLine("Checked for:");
                Console.WriteLine("  - Groq:ApiKey");
                Console.WriteLine("  - GROQ_API_KEY");
                Console.WriteLine("  - OpenAi:ApiKey");
                Console.WriteLine("  - OPENAI_API_KEY");
                Console.WriteLine("\nPlease set the API key using:");
                Console.WriteLine("  dotnet user-secrets set \"Groq:ApiKey\" \"YOUR_KEY\"");
                return;
            }

            Console.WriteLine("✅ API Key found!");
            Console.WriteLine($"   Key preview: {apiKey.Substring(0, Math.Min(10, apiKey.Length))}...");
            Console.WriteLine();

            // Test AI provider
            var httpClient = new HttpClient();
            var provider = new OpenAiProvider(httpClient, config);

            Console.WriteLine("Testing AI provider with a simple prompt...");
            Console.WriteLine("Prompt: 'What is 2+2? Answer in one word.'\n");

            try
            {
                var response = await provider.GenerateAsync(
                    "You are a helpful assistant. Answer concisely.",
                    "What is 2+2? Answer in one word.",
                    null,
                    CancellationToken.None
                );

                if (response.Contains("Stubbed AI response") || response.Contains("Key not set"))
                {
                    Console.WriteLine("❌ ERROR: AI provider returned stub response!");
                    Console.WriteLine($"Response: {response}");
                    Console.WriteLine("\nThis means the API key is not being used correctly.");
                }
                else
                {
                    Console.WriteLine("✅ AI Provider is working!");
                    Console.WriteLine($"Response: {response}");
                    Console.WriteLine("\nThe AI provider is configured correctly and can communicate with the API.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ ERROR: Failed to call AI provider");
                Console.WriteLine($"Exception: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }
        }
    }
}


