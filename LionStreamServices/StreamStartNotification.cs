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
            await SubscribeToStreamAsync(req, log);          

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


//        public static async Task<bool> VerifyPayLoadSecret(HttpRequest req, ILogger log)
//        {
//#if DEBUG
//            return true;
//#endif
//            string signature = req.Headers["Twitch-Eventsub-Message-Signature"].ToString();

//            if (string.IsNullOrEmpty(signature))
//            {
//                log.LogError("Twitch signature header not found");
//                return false;
//            }

//            // TODO: Compare against the the signature. 
//            return true;
//        }

        public async Task<HttpResponseMessage> SubscribeToStreamAsync(HttpRequest req, ILogger log)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, "https://api.twitch.tv/helix/eventsub/subscriptions");
            request.Headers.Add("Authorization", Environment.GetEnvironmentVariable("OAuthToken"));
            string JsonBodyForSubscribe = JsonConvert.SerializeObject(new SubscribeBodyJson());
            request.Content = new StringContent(JsonBodyForSubscribe, Encoding.UTF8, "application/json");
            var client = _httpClientFactory.CreateClient("SubClient");

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
            return new HttpResponseMessage(HttpStatusCode.PaymentRequired);
        }
    }
}