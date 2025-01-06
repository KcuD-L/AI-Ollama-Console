using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace AIOllamaInConsole
{
    internal class Program
    {
        static HttpClient client = new HttpClient();
        static string model = "";
        private const string OllamaApiUrl = "http://localhost:11434/api/chat";
        private static List<object> chatHistory = new List<object>();
        private const int maxHistorySize = 10;
        static async Task Main(string[] args)
        {
            Console.WriteLine("Before using it, make sure that you have ollama and the models installed and that ollama is currently running.");
            Console.WriteLine();
            Console.WriteLine("Enter the full correct name of the model you want to use (for example, mistral or llama3:3 or llama3:2:1b). Please note that it is necessary to specify exactly the FULL CORRECT NAME, as indicated on the ollama website.");
            Console.WriteLine();
            Console.Write("So which model should we use?  ");
            model = Console.ReadLine() ?? "";

            if (string.IsNullOrWhiteSpace(model))
            {
                Console.WriteLine("Model name cannot be empty. Exiting.");
                return;
            }
            await MainCycle();
        }
        static async Task MainCycle()
        {
            while (true)
            {
                Console.Write("User> ");
                string userQuestion = Console.ReadLine();
                if (string.IsNullOrWhiteSpace(userQuestion) || userQuestion.ToLower() == "exit")
                {
                    break;
                }
                AddMessageToHistory(new { role = "user", content = userQuestion });
                try
                {
                    string response = await SendRequestAsync(chatHistory);
                    Console.WriteLine("AI> " + response);
                    AddMessageToHistory(new { role = "assistant", content = response });
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error: {ex.Message}");
                }
            }
        }

        private static async Task<string> SendRequestAsync(List<object> chatHistory)
        {
            var payload = new
            {
                model,
                messages = chatHistory,
                stream = false
            };

            try
            {
                HttpResponseMessage response = await client.PostAsJsonAsync(OllamaApiUrl, payload);
                response.EnsureSuccessStatusCode();
                string jsonResponse = await response.Content.ReadAsStringAsync();
                using JsonDocument doc = JsonDocument.Parse(jsonResponse);
                if (doc.RootElement.TryGetProperty("message", out JsonElement message) &&
                   message.TryGetProperty("content", out JsonElement content))
                {
                    return content.GetString() ?? "Error: Could not retrieve content.";
                }
                else
                {
                    return "Error: Unexpected response format from the API.";
                }

            }
            catch (HttpRequestException ex)
            {
                return $"Request Error: {ex.Message}";
            }
            catch (JsonException ex)
            {
                return $"JSON Error: {ex.Message}";
            }

        }

        private static void AddMessageToHistory(object message)
        {
            if (chatHistory.Count < maxHistorySize * 2)
            {
                chatHistory.Add(message);
            }
            else
            {
                chatHistory.RemoveAt(0);
                chatHistory.Add(message);
            }
        }
    }
}