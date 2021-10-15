//Create an event sub of stream.online and stream.offline
//To sub to a twitch webhook from the API the first thing we do is send a post request with a subscribe webhook object. The callback can be the same function, or can be a entirely differnt one. 
//The secret that we store in the body is something we create ourselves. 

//Create an object that represents the JSON of our event. 


using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using StreamServices.Core;
using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;


namespace StreamServices
{
    public sealed class StreamServices : BaseFunction
    {
        public StreamServices(IHttpClientFactory httpClientFactory, IConfiguration configuration) : base(httpClientFactory, configuration)
        {
        }

        //Function is meant to pass the userId in. 
        [FunctionName("Subscribe")]
        public async Task<IActionResult> Subscribe([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req, ILogger log)
        {
            var twitchEndpoint = Environment.GetEnvironmentVariable("EventSubUrl");
            string userName = req.Query["userName"].ToString();

            if (String.IsNullOrWhiteSpace(userName))
            {
                return new BadRequestObjectResult("Please pass a user name into the query string paramter. Like ?userName = \"coolStreamer\" ");
            }

            log.LogInformation($"Subscribeing {userName}");
            var userToSubscribeToo = await GetChannelIdForUserName(userName);
            CreateSubscriptionPostJson subObject = new CreateSubscriptionPostJson(userToSubscribeToo);
            var subPayLoad = JsonConvert.SerializeObject(subObject);
            var postRequestContent = new StringContent(subPayLoad, Encoding.UTF8, "application/json");
            //setup header that has oAuth token

            string responseBody;
            using (var client = GetHttpClient(twitchEndpoint))
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Environment.GetEnvironmentVariable("AppAccessToken"));
                var responseMessage = await client.PostAsync("", postRequestContent);

                if (!responseMessage.IsSuccessStatusCode)
                {
                    responseBody = await responseMessage.Content.ReadAsStringAsync();
                    log.Log(LogLevel.Error, $"Error response body {responseBody}");
                }
                else
                {
                    log.LogInformation($"Subscribed to {userName}'s stream");
                    return new OkObjectResult($"Subscribed to {userName}'s stream");
                }
            }

            return new BadRequestObjectResult(responseBody);
        }

        [FunctionName("StreamOnline")]
        public async Task<IActionResult> StreamOnline([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req, ILogger log)
        {
            //setup callback object
            //verify the signature
            //respond to the callback request by returnign the value of the challenge. Return a raw string. 
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            
            var reqTypeHeader = req.Headers["Twitch-Eventsub-Message-Type"];
            if (reqTypeHeader == "webhook_callback_verification")
            {
                
            var hmacMessage = req.Headers["Twitch-Eventsub-Message-Id"] + req.Headers["Twitch-Eventsub-Message-Timestamp"] + requestBody;
                var isAuthenticated = await VerifySignature(req);
                if (isAuthenticated)
                {
                    return new OkObjectResult();//return challenge
                }
                else
                {
                    return new BadRequestResult();
                }

            }
        }

        private async Task<bool> VerifySignature(HttpRequest req)
        {
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var callbackJson = JsonConvert.DeserializeObject<CallbackChallengeJson>(requestBody);
            var hmacMessage = req.Headers["Twitch-Eventsub-Message-Id"] + req.Headers["Twitch-Eventsub-Message-Timestamp"] + requestBody;

            var expectedSignature = "sha256=" + CreateHmacHash(hmacMessage, Environment.GetEnvironmentVariable("EventSubSecret"));

            //Send back challenge as a response. 
            var messageSignatureHeader = req.Headers["Twitch-Eventsub-Message-Signature"];
            if (expectedSignature == messageSignatureHeader)
            {
                return true;
            }
            else
                return false;
        }
    }


}
