using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using StreamServices.Core;
using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
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

        public string TwitchClientID { get { return Environment.GetEnvironmentVariable("ClientId"); } }


        public HttpClient GetHttpClient(string baseAddress, string clientId = "", bool includeJson = true, bool discordPost = false)
        {

            if (clientId == "") clientId = TwitchClientID;

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

        protected async Task<AppAccessToken> GetAccessToken()
        {

            var clientId = Environment.GetEnvironmentVariable("ClientId");
            var clientSecret = Environment.GetEnvironmentVariable("ClientSecret");

            using (var client = GetHttpClient("https://id.twitch.tv"))
            {

                var result = await client.PostAsync($"/oauth2/token?client_id={clientId}&client_secret={clientSecret}&grant_type=client_credentials&scope=", new StringContent(""));

                result.EnsureSuccessStatusCode();

                return JsonConvert.DeserializeObject<AppAccessToken>(await result.Content.ReadAsStringAsync());
            }

        }

        protected async Task<string> IdentifyUser(string user)
        {
            if (char.IsDigit(user[0]))
                return user;
            else
               return await GetUserNameForChannelId(user);
        }

        protected async Task<string> GetUserNameForChannelId(string channelId)
        {
            var client = GetHttpClient("https://api.twitch.tv/helix/");
            var token = await GetAccessToken();
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {token.AccessToken}");

            var body = await client.GetAsync($"users?id={channelId}")
              .ContinueWith(msg => msg.Result.Content.ReadAsStringAsync()).Result;
            var obj = JObject.Parse(body);

            return obj["data"][0]["login"].ToString();
        }

        internal async Task<string> GetChannelIdForUserName(string userName)
        {
            var client = GetHttpClient("https://api.twitch.tv/helix/");
            var token = await GetAccessToken();
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {token.AccessToken}");

            string body = string.Empty;
            try
            {
                var msg = await client.GetAsync($"users?login={userName}");
                msg.EnsureSuccessStatusCode();
                body = await msg.Content.ReadAsStringAsync();
            }
            catch (HttpRequestException e)
            {
                throw;
            }
            catch
            {
                if (await GetUserNameForChannelId(userName) != string.Empty)
                    return userName;
                else
                    return string.Empty;
            }

            var obj = JObject.Parse(body);
            return obj["data"][0]["id"].ToString();

        }

        protected static string CreateHmacHash(string data, string key)
        {

            var keybytes = UTF8Encoding.UTF8.GetBytes(key);
            var dataBytes = UTF8Encoding.UTF8.GetBytes(data);

            var hmac = new HMACSHA256(keybytes);
            var hmacBytes = hmac.ComputeHash(dataBytes);

            //return Convert.ToBase64String(hmacBytes);
            return BitConverter.ToString(hmacBytes).Replace("-", "").ToLower();

        }

        protected async Task<bool> VerifyPayloadSecret(HttpRequest req, TwitchSubscriptionInitalPost subPostJson)
        {
            var signature = req.Headers["X-Hub-Signature"].ToString();
            var ourHashCalculation = string.Empty;
            if (req.Body.CanSeek)
            {
                using (var reader = new StreamReader(req.Body, Encoding.UTF8))
                {
                    req.Body.Position = 0;
                    var bodyContent = await reader.ReadToEndAsync();
                    ourHashCalculation = CreateHmacHash(bodyContent, subPostJson.Transport.Secret);
                }
            }
            return ourHashCalculation == signature;
        }
    }
}
