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

namespace LionStreamServices
{
    public static class StreamStartNotification
    {
        [FunctionName("StreamStartNotification")]
        public static async Task<HttpResponseMessage> StreamStartedNotification(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("StreamStartNotification function initiated...");
            var channelId = req.Query["channelId"].ToString();
            log.LogInformation($"channelId: {channelId}");

            //Authoriztion
            var challenge = req.Query["hub.challenge"].ToString();
            if(!string.IsNullOrEmpty(challenge))
            {
                log.LogInformation($"Successfully subscribed to channel {channelId}");

                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(challenge)
                };               
            }

            if(!(await VerifyPayLoadSecret(req, log)))
            {
                log.LogError($"Invalid signature on request for ChannelId {channelId}");
                return null;
            }
            else
            {
                log.LogTrace($"Valid signature for ChallenId {channelId}");
            }

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic data = JsonConvert.DeserializeObject(requestBody);

            return new HttpResponseMessage(HttpStatusCode.OK);
        }


        public static async Task<bool> VerifyPayLoadSecret(HttpRequest req, ILogger log)
        {
#if DEBUG
            return true;
#endif
            string signature = req.Headers["Twitch-Eventsub-Message-Signature"].ToString();

            if(string.IsNullOrEmpty(signature))
            {
                log.LogError("Twitch signature header not found");
                return false;
            }

            // TODO: Compare against the the signature. 
            return true;
        }

    }
}
