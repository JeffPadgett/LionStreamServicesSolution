using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Cosmos.Table;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using StreamServices.Core;
using StreamServices.Core.Models;
using System;
using System.IO;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace StreamServices
{
    public abstract class BaseFunction
    {
        protected IHttpClientFactory HttpClientFactory { get; }
        protected IConfiguration Configuration { get; }
        protected BaseFunction(IHttpClientFactory httpClientFactory, IConfiguration configuration)
        {
            HttpClientFactory = httpClientFactory;
            Configuration = configuration;
        }

        public HttpClient GetHttpClient(string baseAddress, string clientId = "", bool includeJson = true, bool discordPost = false)
        {
            if (clientId == "") clientId = Environment.GetEnvironmentVariable("ClientId");

            var client = HttpClientFactory.CreateClient();
            client.BaseAddress = new Uri(baseAddress);

            if (includeJson)
            {
                client.DefaultRequestHeaders.Add("Accept", @"application/json");
            }
            if (!discordPost)
            {
                client.DefaultRequestHeaders.Add("Client-ID", clientId);
            }

            return client;
        }

        protected async Task GetAccessToken(AppAccessToken accessToken)
        {
            var clientId = Environment.GetEnvironmentVariable("ClientId");
            var clientSecret = Environment.GetEnvironmentVariable("ClientSecret");

            using var client = GetHttpClient("https://id.twitch.tv");
            var result = await client.PostAsync($"/oauth2/token?client_id={clientId}&client_secret={clientSecret}&grant_type=client_credentials&scope=", new StringContent(""));

            result.EnsureSuccessStatusCode();

            var refreshedToken = JsonConvert.DeserializeObject<AppAccessToken>(await result.Content.ReadAsStringAsync());
            accessToken.AccessToken = refreshedToken.AccessToken;
            accessToken.ExpiresAtUTC = DateTime.UtcNow.AddSeconds(refreshedToken.ExpiresInSeconds);
        }

        protected async Task<string> GetUserNameForChannelId(string channelId, AppAccessToken accessToken)
        {
            var client = GetHttpClient("https://api.twitch.tv/helix/");
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {accessToken.AccessToken}");

            var body = await client.GetAsync($"users?id={channelId}")
              .ContinueWith(msg => msg.Result.Content.ReadAsStringAsync()).Result;
            var obj = JObject.Parse(body);

            return obj["data"][0]["login"].ToString();
        }

        internal async Task<string> GetChannelIdForUserName(string userName, AppAccessToken accessToken)
        {
            var client = GetHttpClient("https://api.twitch.tv/helix/");
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {accessToken.AccessToken}");

            string body;
            try
            {
                var msg = await client.GetAsync($"users?login={userName}");
                msg.EnsureSuccessStatusCode();
                body = await msg.Content.ReadAsStringAsync();
            }
            catch (HttpRequestException e)
            {
                throw e;
            }
            catch
            {
                if (await GetUserNameForChannelId(userName, accessToken) != string.Empty)
                    return userName;
                else
                    return string.Empty;
            }

            var obj = JObject.Parse(body);
            return obj["data"][0]["id"].ToString();
        }

        protected async Task<string> IdentifyUser(string user, AppAccessToken accessToken)
        {
            if (char.IsDigit(user[0]))
                return await GetUserNameForChannelId(user, accessToken);
            else
                return user;
        }

        protected async Task<string> VerifySignature(HttpRequest req)
        {
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var callbackJson = JsonConvert.DeserializeObject<ChallengeJson>(requestBody);
            var hmacMessage = req.Headers["Twitch-Eventsub-Message-Id"] + req.Headers["Twitch-Eventsub-Message-Timestamp"] + requestBody;

            var expectedSignature = "sha256=" + CreateHmacHash(hmacMessage, Environment.GetEnvironmentVariable("EventSubSecret"));

            var messageSignatureHeader = req.Headers["Twitch-Eventsub-Message-Signature"];
            if (expectedSignature == messageSignatureHeader)
            {
                return callbackJson.Challenge;
            }
            else
                return "";
        }

        protected static string CreateHmacHash(string data, string key)
        {
            var keybytes = UTF8Encoding.UTF8.GetBytes(key);
            var dataBytes = UTF8Encoding.UTF8.GetBytes(data);

            var hmac = new HMACSHA256(keybytes);
            var hmacBytes = hmac.ComputeHash(dataBytes);

            return BitConverter.ToString(hmacBytes).Replace("-", "").ToLower();
        }

        protected async Task<AppAccessToken> VerifyAccessToken(CloudTable cloudTable, AppAccessToken appAccessToken, ILogger log)
        {
            log.LogInformation("VerifyingAccessToken");
            await cloudTable.CreateIfNotExistsAsync();

            if (appAccessToken is null)
            {
                appAccessToken = new AppAccessToken()
                {
                    AccessToken = "123",
                    ExpiresAtUTC = DateTime.UtcNow.AddMinutes(-15),
                    PartitionKey = "Twitch",
                    RowKey = "1"
                };
                TableOperation tableOperation = TableOperation.InsertOrReplace(appAccessToken);
                await cloudTable.ExecuteAsync(tableOperation);
            }

            if (appAccessToken.ExpiresAtUTC < DateTime.UtcNow.AddSeconds(-30))
            {
                await GetAccessToken(appAccessToken);
                TableOperation tableOperation = TableOperation.InsertOrReplace(appAccessToken);
                await cloudTable.ExecuteAsync(tableOperation);
            }

            return appAccessToken;
        }
    }
}
