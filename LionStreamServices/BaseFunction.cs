using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using StreamServices.Core;
using System;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

[assembly: InternalsVisibleTo("StreamServices.Test")]
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


        public HttpClient GetHttpClient(string baseAddress, string clientId = "", bool includeJson = true)
        {

            if (clientId == "") clientId = TwitchClientID;

            var client = HttpClientFactory.CreateClient();
            client.BaseAddress = new Uri(baseAddress);

            if (includeJson)
            {
                client.DefaultRequestHeaders.Add("Accept", @"application/json");
            }
            client.DefaultRequestHeaders.Add("Accept", @"application/vnd.twitchtv.v5+json");
            client.DefaultRequestHeaders.Add("Client-ID", clientId);

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
    }
}
