﻿using DSharpPlus;
using EmojiButler.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace EmojiButler
{
    public class DiscordEmojiClient
    {
        public static HttpClient client = new HttpClient();

        private List<Emoji> emoji;
        private Dictionary<int, string> categories;

        public List<Emoji> Emoji { get => emoji; }
        public Dictionary<int, string> Categories { get => categories; }

        public const string BASE = "https://discordemoji.com/api";
        public const string BASE_ASSETS = "https://discordemoji.com/assets/emoji/";

        public DiscordEmojiClient()
        {
            client = new HttpClient();
            emoji = GetEmojis().GetAwaiter().GetResult();
            categories = GetCategories().GetAwaiter().GetResult();
        }

        public async Task<List<Emoji>> GetEmojis()
        {
            using (HttpResponseMessage resp = await client.GetAsync(new Uri(BASE)))
            {
                string content = await resp.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<List<Emoji>>(content);
            }
        }

        public async Task<Dictionary<int, string>> GetCategories()
        {
            using (HttpResponseMessage resp = await client.GetAsync(new Uri(BASE + "?request=categories")))
            {
                string content = await resp.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<Dictionary<int, string>>(content);
            }
        }

        public async Task<Statistics> GetStatistics()
        {
            using (HttpResponseMessage resp = await client.GetAsync(new Uri(BASE + "?request=stats")))
            {
                string content = await resp.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<Statistics>(content);
            }
        }

        public async Task<List<Emoji>> SearchEmojis(string query)
        {
            using (HttpResponseMessage resp = await client.GetAsync(new Uri(BASE + "?request=search&q=" + query)))
            {
                string content = await resp.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<List<Emoji>>(content);
            }
        }

        public async Task RefreshEmoji()
        {
            while (true)
            {
                emoji = await GetEmojis();
                EmojiButler.client.DebugLogger.LogMessage(LogLevel.Info, "EmojiButler", "Cached emoji list updated.", DateTime.Now);
                Thread.Sleep(TimeSpan.FromMinutes(5));
            }
        }
    }
}