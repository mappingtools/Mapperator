using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Mapperator.ConsoleApp.Resources;

namespace Mapperator.ConsoleApp.Utils;

public sealed class OsuApiV2Client : IDisposable {
    public const string DefaultOnlineCachePath = "online_cache.jsonl";
    private const string TokenUrl = "https://osu.ppy.sh/oauth/token";
    private const string ApiUrl = "https://osu.ppy.sh/api/v2/beatmapsets";
    private const int MaxRetries = 10;

    private readonly HttpClient client;
    private readonly string onlineCachePath;
    private readonly Dictionary<int, JsonElement> onlineCache = new();
    private readonly int rateLimitInterval;
    private DateTime lastApiCallTime = DateTime.MinValue;

    public OsuApiV2Client(string clientId, string clientSecret, string? onlineCachePath = null, int rateLimitInterval = 120) {
        string token = GetAccessToken(clientId, clientSecret).GetAwaiter().GetResult();

        client = new HttpClient();
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        this.onlineCachePath = string.IsNullOrWhiteSpace(onlineCachePath) ? DefaultOnlineCachePath : onlineCachePath;
        this.rateLimitInterval = rateLimitInterval;

        string? cacheDirectory = Path.GetDirectoryName(this.onlineCachePath);
        if (!string.IsNullOrWhiteSpace(cacheDirectory)) {
            Directory.CreateDirectory(cacheDirectory);
        }

        LoadOnlineCache();
    }

    public JsonElement GetBeatmapSetInfo(int setId, bool bypassCache = false) {
        if (!bypassCache && onlineCache.TryGetValue(setId, out var beatmapsetInfo)) {
            return beatmapsetInfo;
        }

        EnsureRateLimit();
        beatmapsetInfo = GetBeatmapSetInfoFromApi(setId).GetAwaiter().GetResult();
        SaveBeatmapSetInfoToCache(beatmapsetInfo);
        return beatmapsetInfo;
    }

    private void SaveBeatmapSetInfoToCache(JsonElement beatmapsetInfo) {
        int setId = beatmapsetInfo.GetProperty("id").GetInt32();
        onlineCache[setId] = beatmapsetInfo;
        File.AppendAllLines(onlineCachePath, [beatmapsetInfo.GetRawText()]);
    }

    private void LoadOnlineCache() {
        if (File.Exists(onlineCachePath)) {
            Console.WriteLine(Strings.Dataset2_DoDataExtraction2_Loading_online_cache___);
            foreach (string line in File.ReadLines(onlineCachePath)) {
                if (string.IsNullOrWhiteSpace(line)) {
                    continue;
                }

                var json = JsonSerializer.Deserialize<JsonElement>(line);
                int id = json.GetProperty("id").GetInt32();
                onlineCache[id] = json;
            }

            return;
        }

        File.Create(onlineCachePath).Dispose();
    }

    private void EnsureRateLimit() {
        double timeSinceLastCall = (DateTime.Now - lastApiCallTime).TotalMilliseconds;
        if (timeSinceLastCall < rateLimitInterval) {
            Thread.Sleep(rateLimitInterval - (int)timeSinceLastCall);
        }

        lastApiCallTime = DateTime.Now;
    }

    private async Task<JsonElement> GetBeatmapSetInfoFromApi(int setId) {
        HttpResponseMessage? response = null;
        var retries = 0;

        while (retries++ < MaxRetries) {
            response = await client.GetAsync($"{ApiUrl}/{setId}");
            if (response.IsSuccessStatusCode) {
                string jsonResponse = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<JsonElement>(jsonResponse);
            }

            if (retries >= MaxRetries) {
                break;
            }

            switch (response.StatusCode) {
                case HttpStatusCode.TooManyRequests:
                    Console.WriteLine(Strings.Dataset2_GetBeatmapsetInfo_Rate_limited__waiting_10_seconds___);
                    Thread.Sleep(10000);
                    break;
                case HttpStatusCode.GatewayTimeout:
                case HttpStatusCode.RequestTimeout:
                    Console.WriteLine(Strings.Dataset2_GetBeatmapsetInfo_Timed_out__waiting_10_seconds___);
                    Thread.Sleep(10000);
                    break;
                default:
                    throw new Exception($"Failed to fetch beatmapset info: {response.StatusCode}");
            }
        }

        throw new Exception($"Failed to fetch beatmapset info: {response?.StatusCode}");
    }

    private static async Task<string> GetAccessToken(string clientId, string clientSecret) {
        using var tokenClient = new HttpClient();
        tokenClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        var requestBody = new FormUrlEncodedContent([
            new KeyValuePair<string, string>("client_id", clientId),
            new KeyValuePair<string, string>("client_secret", clientSecret),
            new KeyValuePair<string, string>("grant_type", "client_credentials"),
            new KeyValuePair<string, string>("scope", "public")
        ]);

        var response = await tokenClient.PostAsync(TokenUrl, requestBody);
        if (response.IsSuccessStatusCode) {
            string jsonResponse = await response.Content.ReadAsStringAsync();
            var tokenResponse = JsonSerializer.Deserialize<TokenResponse>(jsonResponse);
            return tokenResponse!.AccessToken;
        }

        Console.WriteLine(Strings.Dataset2_GetAccessToken_Failed_to_get_osu__API_access_token__Make_sure_your_ClientId_and_ClientSecret_are_correctly_set_in_config_json_);
        throw new Exception($"Error: {response.StatusCode}");
    }

    public void Dispose() {
        client.Dispose();
    }

    public class TokenResponse {
        [JsonPropertyName("token_type")] public string TokenType { get; set; } = null!;
        [JsonPropertyName("expires_in")] public int ExpiresIn { get; set; }
        [JsonPropertyName("access_token")] public string AccessToken { get; set; } = null!;
    }
}
