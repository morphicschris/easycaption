namespace EasyCaption
{
    using System.Net.Http.Json;
    using System.Text.Json;
    using Microsoft.Extensions.Configuration;
    using System.Drawing;

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

            // Check if the image file paths are provided
            if (args.Length == 0)
            {
                Console.WriteLine("Please provide the image file paths as arguments.");
                return;
            }

            // Ask for trigger word
            Console.Write("Enter a trigger word (optional): ");
            string triggerWord = Console.ReadLine();

            // Ask for general tone
            Console.Write("Enter a general theme for the images (optional): ");
            string generalTone = Console.ReadLine();

            // Prepare image paths
            string[] imagePaths = args;

            for (int i = 0; i < imagePaths.Length; i++)
            {
                string imagePath = imagePaths[i];

                // Validate if the file exists
                if (!File.Exists(imagePath))
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"File not found: {imagePath}");
                    Console.ResetColor();
                    continue;
                }

                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"Processing image {i + 1} of {imagePaths.Length}...");
                Console.ResetColor();

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
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("Failed to get caption from the API.");
                        Console.ResetColor();
                        continue;
                    }

                    string caption = captionResult.caption;

                    if (!skipRecaption)
                    {
                        // Modify system prompt with general tone if provided
                        string modifiedSystemPrompt = systemPrompt;
                        if (!string.IsNullOrWhiteSpace(generalTone))
                        {
                            modifiedSystemPrompt += $" Please update the caption accordingly bearing in mind that the general theme of the image is {generalTone}.";
                        }

                        // Step 3: Send the caption to the second API endpoint
                        var chatRequest = new
                        {
                            messages = new[]
                            {
                                new
                                {
                                    role = "system",
                                    content = modifiedSystemPrompt
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
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine("Failed to get modified caption from the API.");
                            Console.ResetColor();
                            continue;
                        }

                        caption = chatResult.choices[0].message.content;
                    }

                    // Prepend trigger word if specified
                    if (!string.IsNullOrWhiteSpace(triggerWord))
                    {
                        caption = $"{triggerWord}, {caption}";
                    }

                    // Step 4: Save the modified caption to a text file
                    string imageDirectory = Path.GetDirectoryName(imagePath);
                    string imageFileNameWithoutExtension = Path.GetFileNameWithoutExtension(imagePath);
                    string textFilePath = Path.Combine(imageDirectory, $"{imageFileNameWithoutExtension}.txt");

                    await File.WriteAllTextAsync(textFilePath, caption);

                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"Modified caption saved to {textFilePath}");
                    Console.ResetColor();
                }
                catch (Exception ex)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"An error occurred: {ex.Message}");
                    Console.ResetColor();
                }
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
