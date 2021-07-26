using LionStreamServices.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace LionStreamServices
{
    public class StreamManagement : BaseFunction
    {
        public StreamManagement(IHttpClientFactory httpClientFactory, IConfiguration configuration) : base(httpClientFactory, configuration) { }

        [FunctionName("StreamStartNotification")]
        public async Task<HttpResponseMessage> StreamStartedNotificationAsync([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req, ILogger log)
        {
            log.LogInformation("StreamStartNotification function initiated...");

            return await SubscribeToStreamAsync(req, log);

            //return new HttpResponseMessage(HttpStatusCode.NoContent);
        }

        public async Task<HttpResponseMessage> SubscribeToStreamAsync(HttpRequest req, ILogger log)
        {
            //Send the git reqeust to them, they send a HTTP GET request back to us. From the get requst that we get back, we need to botain 
            //we need to send hub.mode: subscribe , which makes the origioanl request. We also send hub.topic, hub.challenge, 
            var request = new HttpRequestMessage(HttpMethod.Get, "https://api.twitch.tv/helix/webhooks/hub");
            request.Headers.Add("Authorization", "Bearer b24qlp98n8nmy03n21z9hf7l38c3ja");
            request.Headers.Add("Client-Id", "za4li2la2mf36yspac8n6uruf5hoi1");
            var channelId = GetChannelIdForUserName("brokenswordx");

            var requestBody = new TwitchWebhookSubscriptionBody()
            {
                Callback = new Uri($"{Environment.GetEnvironmentVariable("StreamStartFunctionUri")}?channelId={channelId}").ToString(),
                Mode = "subscribe",
                Topic = $"https://api.twitch.tv/helix/streams?user_id={channelId}",
                //Lease_seconds = leaseInSeconds,     //Optional?
                Secret = TWITCH_SECRET
            }

            request.Content = new StringContent(TwitchWebhookSubscriptionBody, Encoding.UTF8, "application/json");
            var client = _httpClientFactory.CreateClient("SubClient");
            client.DefaultRequestHeaders.Add("Authorization", Environment.GetEnvironmentVariable("OAuthToken"));

            HttpResponseMessage response = await client.SendAsync(request);

            if (response.IsSuccessStatusCode)
            {
                log.LogInformation("status code OK posting response...");
                var channelId = req.Query["channelId"].ToString();
                var challenge = req.Query["hub.challenge"].ToString();

                if (!string.IsNullOrEmpty(challenge))
                {
                    log.LogInformation($"Successfully subscribed to channel {channelId}");

                    return new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent(challenge)
                    };
                }
            }
            return response;
        }

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
