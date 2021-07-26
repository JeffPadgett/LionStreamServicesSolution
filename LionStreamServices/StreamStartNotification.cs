using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Net.Http;
using System.Net;
using System.Text;
using LionStreamServices.Models;

namespace LionStreamServices
{
    public sealed class StreamStartNotification 
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public StreamStartNotification(IHttpClientFactory clientFactory)
        {
            _httpClientFactory = clientFactory;
        }


        [FunctionName("StreamStartNotification")]
        public async Task<HttpResponseMessage> StreamStartedNotificationAsync(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get","post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("StreamStartNotification function initiated...");
            return await SubscribeToStreamAsync(req, log);
            

            //Authoriztion
            var challenge = req.Query["hub.challenge"].ToString();
            log.LogInformation($"challenge: {challenge}");
            if (!string.IsNullOrEmpty(challenge))
            {
                //log.LogInformation($"Successfully subscribed to channel {channelId}");
                if (challenge == Environment.GetEnvironmentVariable("TwitchAPISecret"))
                {
                    log.LogInformation("Verification successful");
                    return new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent(challenge)
                    };
                }

            }

            return new HttpResponseMessage(HttpStatusCode.NoContent);
        }

        public async Task<HttpResponseMessage> SubscribeToStreamAsync(HttpRequest req, ILogger log)
        {
            //Send the git reqeust to them, they send a HTTP GET request back to us. From the get requst that we get back, we need to botain 
            //we need to send hub.mode: subscribe , which makes the origioanl request. We also send hub.topic, hub.challenge, 
            var request = new HttpRequestMessage(HttpMethod.Get, "https://api.twitch.tv/helix/webhooks/hub");
            request.Headers.Add("Authorization", "Bearer b24qlp98n8nmy03n21z9hf7l38c3ja");
            request.Headers.Add("Client-Id", "za4li2la2mf36yspac8n6uruf5hoi1");
            var channelId = GetChannelIdForUserName("test");

            var requestBody = new TwitchWebhookSubscriptionBody()
            {
                Callback = new Uri(Environment.GetEnvironmentVariable("StreamStartFunctionUri"), $"?channelId={channelId}").ToString(),
                Mode = "subscribe",
                Topic = $"https://api.twitch.tv/helix/streams?user_id={channelId}",
                Lease_seconds = leaseInSeconds,
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
    }
}