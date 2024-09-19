namespace EasyCaption
{
    using System.Net.Http.Json;
    using System.Text.Json;
    using Microsoft.Extensions.Configuration;

    public class Program
    {
        public static async Task Main(string[] args)
        {
            // Build configuration
            IConfiguration config = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: false)
                .Build();

            // Read API URLs from configuration
            string captionApiUrl = config["ApiEndpoints:CaptionApiUrl"];
            string chatApiUrl = config["ApiEndpoints:ChatApiUrl"];
            string systemPrompt = config["Prompts:SystemPrompt"];
            bool skipRecaption = bool.Parse(config["AppSettings:SkipRecaption"]);

            // Check if the image file path is provided
            if (args.Length == 0)
            {
                Console.WriteLine("Please provide the image file path as an argument.");
                return;
            }

            string imagePath = args[0];

            // Validate if the file exists
            if (!File.Exists(imagePath))
            {
                Console.WriteLine($"File not found: {imagePath}");
                return;
            }

            try
            {
                // Step 1: Base64 encode the image
                byte[] imageBytes = await File.ReadAllBytesAsync(imagePath);
                string base64Image = Convert.ToBase64String(imageBytes);

                // Step 2: Send the image to the first API endpoint
                using HttpClient httpClient = new HttpClient();

                var captionResponse = await httpClient.PostAsJsonAsync(captionApiUrl, new
                {
                    image = base64Image
                });

                captionResponse.EnsureSuccessStatusCode();

                var captionResult = await captionResponse.Content.ReadFromJsonAsync<CaptionResponse>();

                if (captionResult == null || string.IsNullOrWhiteSpace(captionResult.caption))
                {
                    Console.WriteLine("Failed to get caption from the API.");
                    return;
                }

                string caption = captionResult.caption;

                if (!skipRecaption)
                {
                    // Step 3: Send the caption to the second API endpoint
                    var chatRequest = new
                    {
                        messages = new[]
                        {
                        new
                        {
                            role = "system",
                            content = systemPrompt
                        },
                        new
                        {
                            role = "user",
                            content = caption
                        }
                    }
                    };

                    var chatResponse = await httpClient.PostAsJsonAsync(chatApiUrl, chatRequest);

                    chatResponse.EnsureSuccessStatusCode();

                    var chatResultJson = await chatResponse.Content.ReadAsStringAsync();

                    var chatResult = JsonSerializer.Deserialize<ChatResponse>(chatResultJson);

                    if (chatResult == null || chatResult.choices == null || chatResult.choices.Length == 0)
                    {
                        Console.WriteLine("Failed to get modified caption from the API.");
                        return;
                    }

                    caption = chatResult.choices[0].message.content;
                }

                // Step 4: Save the modified caption to a text file
                string imageDirectory = Path.GetDirectoryName(imagePath);
                string imageFileNameWithoutExtension = Path.GetFileNameWithoutExtension(imagePath);
                string textFilePath = Path.Combine(imageDirectory, $"{imageFileNameWithoutExtension}.txt");

                await File.WriteAllTextAsync(textFilePath, caption);

                Console.WriteLine($"Modified caption saved to {textFilePath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");
            }
        }
    }

    public class CaptionResponse
    {
        public string caption { get; set; }
    }

    public class ChatResponse
    {
        public string id { get; set; }
        public string @object { get; set; }
        public int created { get; set; }
        public string model { get; set; }
        public Choice[] choices { get; set; }
        public Usage usage { get; set; }
        public string system_fingerprint { get; set; }
    }

    public class Choice
    {
        public int index { get; set; }
        public Message message { get; set; }
        public object logprobs { get; set; }
        public string finish_reason { get; set; }
    }

    public class Message
    {
        public string role { get; set; }
        public string content { get; set; }
    }

    public class Usage
    {
        public int prompt_tokens { get; set; }
        public int completion_tokens { get; set; }
        public int total_tokens { get; set; }
    }
}