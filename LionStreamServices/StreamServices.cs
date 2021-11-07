using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Cosmos.Table;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using StreamServices.Core;
using StreamServices.Core.DTOs;
using StreamServices.Core.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;


namespace StreamServices
{
    public sealed class StreamServices : BaseFunction
    {
        private readonly IMapper _mapper;
        public StreamServices(IHttpClientFactory httpClientFactory, IConfiguration configuration, IMapper mapper) : base(httpClientFactory, configuration)
        {
            _mapper = mapper;
        }

        //http://localhost:7071/api/Subscribe?userName=brokenswordx
        //Function is meant to pass the userId in and subtype
        [FunctionName("Subscribe")]
        public async Task<IActionResult> Subscribe([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req,
            [Table("Tokens", Connection = "AzureWebJobsStorage")] CloudTable cloudTable,
            [Table("Tokens", "Twitch", "1", Connection = "AzureWebJobsStorage")] AppAccessToken appAccessToken,
            ILogger log)
        {
            var baseTwitchEndpoint = Environment.GetEnvironmentVariable("BaseTwitchUrl");
            string user = req.Query["userName"].ToString();
            string subType = req.Query["subType"].ToString();

            if (String.IsNullOrWhiteSpace(user))
            {
                return new BadRequestObjectResult("Please pass a user name into the query string paramter. Like ?userName = coolStreamer or ?subType=channel.follow. ");
            }

            appAccessToken = await VerifyAccessToken(cloudTable, appAccessToken, log);

            log.LogInformation($"Subscribeing {user}");
            var channelToSubscribeTo = await IdentifyUser(user, appAccessToken);
            TwitchSubscriptionInitalPost subObject = new TwitchSubscriptionInitalPost(await GetChannelIdForUserName(channelToSubscribeTo, appAccessToken), subType);
            string subPayLoad = JsonConvert.SerializeObject(subObject);
            var postRequestContent = new StringContent(subPayLoad, Encoding.UTF8, "application/json");

            string responseBody;
            string namedUser = char.IsDigit(user[0]) ? await GetUserNameForChannelId(user, appAccessToken) : user;
            using (var client = GetHttpClient(baseTwitchEndpoint))
            {
                client.DefaultRequestHeaders.Add("Authorization", "Bearer " + appAccessToken.AccessToken);
                var responseMessage = await client.PostAsync("eventsub/subscriptions", postRequestContent);

                if (!responseMessage.IsSuccessStatusCode)
                {
                    responseBody = await responseMessage.Content.ReadAsStringAsync();
                    log.Log(LogLevel.Error, $"Error response body {responseBody}");
                }
                else
                {
                    log.LogInformation($"Subscribed to {namedUser}'s stream");
                    return new OkObjectResult($"Notifications will now be sent when {subType} on stream {namedUser}");
                }
            }
            return new BadRequestObjectResult(responseBody + $" When attempting to subscribe {namedUser}");
        }

        [FunctionName("DiscordNotificationProcessor")]
        public async Task<IActionResult> DiscordNotificationProcessor([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req, ILogger log)
        {
            if (req.Headers["Twitch-Eventsub-Message-Type"] == "webhook_callback_verification")
            {
                return await CallbackVerification(req, log);
            }

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            StreamStatusJson streamData = JsonConvert.DeserializeObject<StreamStatusJson>(requestBody);

            string discordMessage = SetDiscordMessage(streamData);
            string discordWebhook = SetChatRoom(streamData);

            var discordPayload = JsonConvert.SerializeObject(new DiscordChannelNotification(discordMessage));
            var discordPost = new StringContent(discordPayload, Encoding.UTF8, "application/json");
            using (var client = new HttpClient())
            {
                var response = await client.PostAsync(discordWebhook, discordPost);
            }

            return default;
        }

        private async Task<IActionResult> CallbackVerification(HttpRequest req, ILogger log)
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

        private static string SetDiscordMessage(StreamStatusJson streamData)
        {
            if (streamData.Subscription.Type == "stream.offline")
            {
                return $"{streamData.Event.BroadcasterUserName} ended their stream :( " + "https://www.twitch.tv/" + streamData.Event.BroadcasterUserName;
            }
            else if (streamData.Subscription.Type == "stream.online")
            {
                return $"{streamData.Event.BroadcasterUserName} is now live! " + "https://www.twitch.tv/" + streamData.Event.BroadcasterUserName;
            }
            else if (streamData.Subscription.Type == "channel.follow")
            {
                return $"{streamData.Event.UserName} just followed {streamData.Event.BroadcasterUserName}s stream!";
            }
            else
            {
                throw new NullReferenceException();
            }
        }

        private static string SetChatRoom(StreamStatusJson streamData)
        {
            if (streamData.Subscription.Type == "stream.offline" || streamData.Subscription.Type == "stream.online")
            {
                return Environment.GetEnvironmentVariable("AnnouncementDiscordWebhook");
            }

            return Environment.GetEnvironmentVariable("LiveStreamDiscordWebhook");
        }

        [FunctionName("GetSubscriptions")]
        public async Task<IActionResult> GetSubscriptions(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req,
            [Table("Tokens", Connection = "AzureWebJobsStorage")] CloudTable cloudTable,
            [Table("Tokens", "Twitch", "1", Connection = "AzureWebJobsStorage")] AppAccessToken appAccessToken,
            ILogger log)
        {
            appAccessToken = await VerifyAccessToken(cloudTable, appAccessToken, log);

            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("Authorization", "Bearer " + appAccessToken.AccessToken);
                client.DefaultRequestHeaders.Add("Client-ID", Environment.GetEnvironmentVariable("ClientId"));
                var response = await client.GetAsync("https://api.twitch.tv/helix/eventsub/subscriptions");
                response.EnsureSuccessStatusCode();
                var resp = await response.Content.ReadAsStringAsync();

                return await GetEventSubscriptions(appAccessToken, resp);
            }
        }

        private async Task<IActionResult> GetEventSubscriptions(AppAccessToken appAccessToken, string resp)
        {
            SubscriptionList twitchList = JsonConvert.DeserializeObject<SubscriptionList>(resp);
            var formatedList = _mapper.Map<List<SubscriptionDTO>>(twitchList.Subscriptions);
            formatedList.RemoveAll(x => x.Status != "enabled");
            foreach (var sub in formatedList)
            {
                sub.Name = (await GetUserNameForChannelId(sub.BroadcasterUserId, appAccessToken));
            }

            return new OkObjectResult(JsonConvert.SerializeObject(formatedList));
        }
    }


}
