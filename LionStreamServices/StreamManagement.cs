using LionStreamServices.Models;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace LionStreamServices
{
    public class StreamManagement : BaseFunction
    {
        public StreamManagement(IHttpClientFactory httpClientFactory, IConfiguration configuration) : base(httpClientFactory, configuration) { }

        protected async Task<AppAccessToken> GetAccessToken()
        {
            var clientId = Environment.GetEnvironmentVariable("OAuthToken");
            var clientSecret = Environment.GetEnvironmentVariable("ClientSecret");

            using (var client = GetHttpClient("https://id.twitch.tv"))
            {
                var result = await client.PostAsync($"/oauth2/token?client_id={clientId}&client_secret={clientSecret}&grant_type=client_credentials&scope=", new StringContent(""));

                result.EnsureSuccessStatusCode();

                return JsonConvert.DeserializeObject<AppAccessToken>(await result.Content.ReadAsStringAsync());
            }
        }
    }

}
