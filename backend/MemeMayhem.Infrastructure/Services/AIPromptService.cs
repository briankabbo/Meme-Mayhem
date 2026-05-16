using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using MemeMayhem.Core.Interfaces;
using Microsoft.Extensions.Configuration;

namespace MemeMayhem.Infrastructure.Services;

public class AIPromptService : IAIPromptService
{
    private readonly HttpClient _http;
    private readonly string _apiKey;
    private const string GROQ_URL =
        "https://api.groq.com/openai/v1/chat/completions";

    // Fallback prompts
    private static readonly Dictionary<string, List<string>> _fallbacks = new()
    {
        ["dark"]      = new() {
            "When you realize adulthood is just paying bills until you die",
            "When your therapist needs a therapist after hearing your problems",
            "When Monday hits like it has a personal vendetta against you",
            "When you smile at work but your soul left the building at 9am",
            "When you google your symptoms and WebMD says you have 3 days",
            "When your life plan was a vibe but the universe said no",
            "When you realize sleep is just a dress rehearsal for death",
            "When your existential crisis hits during a Teams meeting",
            "When you laugh so you don't have to deal with your feelings",
            "When the void stares back and honestly it gets you",
            "When you relate more to the villain than the hero",
            "When you've been fine for so long you forgot what it felt like",
            "When rock bottom has a basement and you found the stairs",
            "When your coping mechanism has a coping mechanism",
            "When you're the problem but at least you're self aware about it",
        },
        ["office"]    = new() {
            "When someone schedules a meeting that could have been an email",
            "When Karen from accounting replies all to a company wide email",
            "When your manager asks if you can hop on a quick call for 2 hours",
            "When your Zoom background is just your messy room",
            "When you mute yourself but the whole team hears you singing",
            "When someone says 'let's take this offline' like it's a bad thing",
            "When you're the only one who didn't understand the inside joke",
            "When you have to pretend to work during your lunch break",
            "When HR sends another email about 'team building activities'",
            "When you use 'per my last email' unironically",
            "When your webcam decides to focus on your forehead today",
            "When you close 27 tabs and immediately open 27 more",
            "When you realize you've been talking to a wall for 10 minutes",
            "When you have to pretend to care about the office potluck",
            "When you hit 'reply all' by accident and pray",
            "When your only coworker is the office plant you're slowly killing",
            "When you're running on 3 cups of coffee and pure spite",
        },
        ["genz"]      = new() {
            "When your Roman Empire is just that embarrassing thing from 2019",
            "When you're the main character but the plot isn't it",
            "When the delulu becomes the solulu and it actually works",
            "When you say 'it's giving' unironically",
            "When your ADHD manifests as collecting hobbies instead of skills",
            "When you're chronically online and it shows",
            "When you use 'low key' and 'high key' in the same sentence",
            "When you need to touch grass but you're comfy on the floor",
            "When your love language is sending each other TikToks",
            "When you're healing but also want to set everything on fire",
            "When you unironically say 'it's not that deep' after overthinking",
            "When you're the chaos goblin of your friend group",
            "When your brain has too many tabs open",
            "When you're trying to romanticize your life but it's mid",
            "When you're simping for a fictional character again",
            "When you've seen so many fandom edits you forget what's canon",
            "When you're processing your trauma through niche internet humor",
        },
        ["chaos"]     = new() {
            "When the ceiling fan starts judging your life choices at 3am",
            "When you put something in a safe place and it enters another dimension",
            "When the USB goes in wrong three times before the fourth try works",
            "When you wake up at 3am and your brain decides to write a novel",
            "When you walk into a room and forget why you went in",
            "When you try to be normal but something is just... off",
            "When your paranoia has its own paranoia",
            "When you're pretty sure aliens are real and they're laughing at us",
            "When you're convinced your pet is plotting world domination",
            "When you're 90% sure you're living in a simulation",
            "When you have a conversation with yourself in the mirror",
            "When you've seen so much internet chaos you're numb to it",
            "When you realize you're the main character of a horror movie",
            "When your life choices could be used as a warning label",
            "When you're one bad day away from becoming a conspiracy theorist",
            "When you're pretty sure your toaster is sentient and plotting against you",
            "When you start questioning reality at 2am",
            "When you're the chaos goblin of your friend group",
        },
        ["wholesome"] = new() {
            "When your pet runs to the door because they heard you coming home",
            "When someone saves you a seat without being asked",
            "When grandma sneaks extra food into your bag before you leave",
            "When your dog tilts their head like they actually understand you",
            "When someone compliments your outfit out of nowhere",
            "When you find money in a jacket you haven't worn in a year",
            "When your barista remembers your name and your order",
            "When your friend sends you a meme they know you'll love",
            "When you get a hug that feels like it fixes everything",
            "When someone listens to your rambling without judgment",
            "When your pet falls asleep on you and it's the cutest thing ever",
            "When you achieve a small goal and feel like a champion",
            "When someone offers you food and you're genuinely starving",
            "When you watch a thunderstorm from a safe cozy spot",
            "When you have a long conversation with your pet and feel understood",
            "When someone says 'I'm proud of you' and you actually believe them",
            "When you realize you're surrounded by people who love you",
            "When you see a stranger being kind to someone else",
        },
    };

    private readonly Random _random = new();

    public AIPromptService(IConfiguration config, HttpClient http)
    {
        _http = http;
        _apiKey = config["Groq:ApiKey"]
            ?? throw new InvalidOperationException("Groq API key not found");
    }

    public async Task<string> GeneratePromptAsync(
        string theme, List<string>? usedPrompts = null)
    {
        try
        {
            var systemPrompt = $"""
                You are a prompt generator for a meme reaction card game called Meme Mayhem.
                Generate ONE funny, relatable sentence players will react to with meme cards.

                STRICT Rules:
                - Maximum 15 words
                - Single sentence or phrase only
                - Must be funny and relatable
                - No hashtags, no emojis, no quotes, no punctuation at end
                - Return ONLY the sentence, absolutely nothing else
                - No explanations, no intro, no extra text

                Theme: {GetThemeInstruction(theme)}
                {BuildAvoidSection(usedPrompts)}
                """;

            var requestBody = new
            {
                model = "llama3-8b-8192",
                max_tokens = 60,
                temperature = 0.9,
                messages = new[]
                {
                    new { role = "system", content = systemPrompt },
                    new { role = "user",   content = "Generate one meme prompt now." }
                }
            };

            var json = JsonSerializer.Serialize(requestBody);
            var request = new HttpRequestMessage(HttpMethod.Post, GROQ_URL)
            {
                Content = new StringContent(
                    json, Encoding.UTF8, "application/json")
            };

            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", _apiKey);

            var response = await _http.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var responseJson = await response.Content.ReadAsStringAsync();
            var parsed = JsonDocument.Parse(responseJson);

            var result = parsed
                .RootElement
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString();

            if (string.IsNullOrWhiteSpace(result))
                return GetFallbackPrompt(theme);

            // Enforce 15 word limit just in case
            var words = result.Trim().Split(' ');
            if (words.Length > 15)
                result = string.Join(' ', words.Take(15));

            return result.Trim();
        }
        catch (Exception)
        {
            // API failed → use fallback silently
            return GetFallbackPrompt(theme);
        }
    }

    // Theme Instructions

    private string GetThemeInstruction(string theme) => theme.ToLower() switch
    {
        "dark" => """
            Dark humor. Morbid observations, existential dread made funny.
            Edgy but never hateful or offensive.
            Example: 'When you realize Monday motivation is just caffeine and denial'
            """,

        "office" => """
            Workplace humor. Meetings, emails, deadlines, coworkers, corporate speak.
            Example: 'When someone schedules a meeting that could have been an email'
            """,

        "genz" => """
            Gen Z internet culture. Brainrot, chronically online, modern slang used naturally.
            Example: 'When your Roman Empire is just that embarrassing thing from 2019'
            """,

        "chaos" => """
            Completely unhinged, absurdist, chaotic. No rules, no logic, pure mayhem energy.
            Example: 'When the ceiling fan judges your life choices at 3am'
            """,

        "wholesome" => """
            Wholesome, heartwarming, feel good. Cute moments, small victories, kindness.
            Example: 'When your pet runs to the door because they heard you coming home'
            """,

        _ => """
            Universally relatable, funny everyday situations everyone experiences.
            Example: 'When you open the fridge for the fifth time hoping something changed'
            """
    };

    // Avoid Repeats

    private string BuildAvoidSection(List<string>? usedPrompts)
    {
        if (usedPrompts == null || usedPrompts.Count == 0)
            return string.Empty;

        var list = string.Join("\n- ", usedPrompts);
        return $"""

            Already used this game — do NOT repeat or use anything similar to:
            - {list}
            """;
    }

    // Fallback

    private string GetFallbackPrompt(string theme)
    {
        var key = theme.ToLower();

        if (!_fallbacks.TryGetValue(key, out var list))
            list = _fallbacks["chaos"];

        return list[_random.Next(list.Count)];
    }
}