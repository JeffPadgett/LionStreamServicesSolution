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

        //http://localhost:7071/api/Subscribe?userName=brokenswordx
        //Function is meant to pass the userId in and subtype
        [FunctionName("Subscribe")]
        public async Task<IActionResult> Subscribe([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req, ILogger log)
        {
            
            var baseTwitchEndpoint = Environment.GetEnvironmentVariable("BaseTwitchUrl");
            string user = req.Query["userName"].ToString();
            string subType = req.Query["subType"].ToString();

            if (String.IsNullOrWhiteSpace(user))
            {
                return new BadRequestObjectResult("Please pass a user name into the query string paramter. Like ?userName = coolStreamer or ?subType=channel.follow. ");
            }

            log.LogInformation($"Subscribeing {user}");
            var channelToSubscribeTo = await IdentifyUser(user);
            TwitchSubscriptionInitalPost subObject = new TwitchSubscriptionInitalPost(channelToSubscribeTo, subType);
            string subPayLoad = JsonConvert.SerializeObject(subObject);
            var postRequestContent = new StringContent(subPayLoad, Encoding.UTF8, "application/json");

            string responseBody;
            string namedUser = char.IsDigit(user[0]) ? await GetUserNameForChannelId(user) : user;
            using (var client = GetHttpClient(baseTwitchEndpoint))
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Environment.GetEnvironmentVariable("AppAccessToken"));
                var responseMessage = await client.PostAsync("eventsub/subscriptions", postRequestContent);

                if (!responseMessage.IsSuccessStatusCode)
                {
                    responseBody = await responseMessage.Content.ReadAsStringAsync();
                    log.Log(LogLevel.Error, $"Error response body {responseBody}");
                }
                else
                {
                    log.LogInformation($"Subscribed to {namedUser}'s stream");
                    return new OkObjectResult($"Notifications will now be sent to {namedUser}'s stream when {subType}");
                }
            }

            log.LogInformation($"{namedUser} may already be subscribed...");
            return new BadRequestObjectResult(responseBody + $" When attempting to subscribe {namedUser}");
        }

        [FunctionName("StreamOnline")]
        public async Task<IActionResult> StreamOnline([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req, ILogger log)
        {           
            var reqTypeHeader = req.Headers["Twitch-Eventsub-Message-Type"];
            if (reqTypeHeader == "webhook_callback_verification")
            {                
                var isAuthenticated = await VerifySignature(req);
                if (!string.IsNullOrEmpty(isAuthenticated))
                {
                    log.LogInformation("User authenticated");
                    return new OkObjectResult(isAuthenticated);
                }
                else
                {
                    return new BadRequestResult();
                }
            }

            //Parse incoming webhook to grab username and stream URL and store them in variables. 

            //Post stuff to discord now. 
            log.LogInformation("Ready to post stuff to discord channels");
            var discordWebHook = Environment.GetEnvironmentVariable("DiscordWebhook");
            //Define payload, which is the message
            return default;
        }

        [FunctionName("GetSubscriptions")]
        public async Task<IActionResult> GetSubscriptions([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req, ILogger log)
        {
            log.LogInformation($"Getting Subscriptions...");
            var baseTwitchEndpoint = Environment.GetEnvironmentVariable("BaseTwitchUrl");
            return default;

        }

        private async Task<string> VerifySignature(HttpRequest req)
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
    }


}
