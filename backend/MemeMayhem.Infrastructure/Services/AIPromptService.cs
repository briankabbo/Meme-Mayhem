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
        ["dark"] = new() {
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
    "When you realize your dreams had an expiry date",
    "When your 5 year plan is just surviving the next 5 days",
    "When you accidentally become your parents",
    "When your idea of self care is not crying in public",
    "When you realize fun is just expensive suffering",
    "When adulting hits and you realize nobody actually knows what they're doing",
    "When your back goes out more than you do",
    "When you cancel plans and feel nothing but relief",
    "When your biggest achievement today was getting out of bed",
    "When you realize your childhood dreams are now just tax categories",
    "When you look in the mirror and see your dad staring back",
    "When you're too tired to be sad but too sad to sleep",
    "When you call your doctor and they say it's just stress again",
    "When you realize retirement is just a myth they tell workers",
    "When your idea of a wild night is being in bed by 9pm",
    "When you realize the light at the end of the tunnel is a train",
    "When karma takes too long so you just stew in it",
    "When you've accepted that things won't get better but adapted anyway",
    "When you're not pessimistic you're just statistically realistic",
    "When your horoscope says good things are coming but it's been 30 years",
    "When you find out your comfort show ends on a cliffhanger forever",
    "When you realize your pet will outlive your will to clean",
    "When you make peace with disappointment because it's reliable",
    "When the universe gives you a sign and it's a stop sign",
    "When you've already planned 12 escape routes from a party you haven't attended",
    "When your inner monologue is just a disappointed narrator",
    "When you realize hope is just delayed disappointment",
    "When the autopilot takes over and you don't remember the last hour",
    "When your productivity is just anxiety in disguise",
    "When you realize procrastination is just time blindness with consequences",
    "When you prepare for a conversation three days in advance",
    "When you replay that awkward moment from 2009 at 3am",
    "When you cancel on friends and spend the night doing nothing anyway",
    "When you say you're fine and your face says absolutely not",
    "When you avoid a phone call for so long it becomes a personal principle",
    "When you rehearse an argument in the shower that never happens",
    "When you overshare with a stranger and immediately regret your existence",
    "When you laugh at the wrong moment and ruin the entire vibe",
    "When you try to be relatable and accidentally reveal too much",
    "When you ghost someone and then see them in real life immediately",
    "When your small talk is just trauma bonding in disguise",
    "When you realize you've been performing normal for too long",
    "When your resting face makes people ask if you're okay",
    "When you say yes to plans you knew you'd cancel three weeks ago",
    "When you're the funny one because crying in public isn't socially acceptable",
    "When you realize every birthday is just a countdown wearing a hat",
    "When you do the math on how many Mondays you have left",
    "When your body starts making sounds you didn't authorize",
    "When the warranty on your knees runs out at 28",
    "When you realize you've spent 40 percent of your life asleep",
    "When you find a grey hair and it brings you unexpected peace",
    "When you stop fearing death and start fearing mediocrity",
    "When you realize your legacy is just your browser history",
    "When you calculate how much time you've spent in traffic and weep",
    "When you realize your metabolism left without a goodbye note",
    "When you wake up at 3am contemplating every choice that led here",
    "When you realize your body has been slowly betraying you for years",
    "When your joints sound like a bowl of breakfast cereal",
    "When you find out your favorite band is now considered classic rock",
    "When you realize the years are going faster and you can't find the brakes",
    "When your job description and your actual job are two different jobs",
    "When you write a resignation letter just to feel something",
    "When your passion project becomes your day job and kills the passion",
    "When you realize your boss has never done your job",
    "When performance review season makes you question your entire identity",
    "When you work overtime and get a pizza party as compensation",
    "When you smile through a meeting that should have been a document",
    "When you realize LinkedIn is just suffering with a professional filter",
    "When you give 110 percent and they ask for 120 next quarter",
    "When the promotion goes to someone who sends more emails",
    "When you realize hustle culture was invented by people who don't work",
    "When your annual bonus is a gift card to a place you never go",
    "When you stay late and nobody notices but you leave early and everyone does",
    "When you're asked to do more with less and less and less",
    "When you realize your coworker thrives on chaos and you're the chaos",
    "When 3am hits and every unresolved issue wants to talk",
    "When your brain schedules a board meeting right as you fall asleep",
    "When you stare at the ceiling and the ceiling stares back",
    "When you have a brilliant idea at 2am and forget it by morning",
    "When insomnia and anxiety decide to collaborate on a project",
    "When you stay up late doing nothing but can't afford to stay up late",
    "When the silence gets loud enough to hear your own thoughts",
    "When you're too tired to sleep and too awake to rest",
    "When you doom scroll until the algorithm knows your deepest fears",
    "When 4am feels oddly peaceful because everyone else is also spiraling alone",
        },
        ["office"] = new() {
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
        ["genz"] = new() {
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
        ["chaos"] = new() {
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