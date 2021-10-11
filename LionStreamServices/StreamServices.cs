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
using Microsoft.Extensions.Configuration;

namespace StreamServices
{
    public sealed class StreamServices : BaseFunction
    {
        public StreamServices(IHttpClientFactory httpClientFactory, IConfiguration configuration) : base(httpClientFactory, configuration)
        {
        }

        [FunctionName("Subscribe")]
        public async Task<HttpResponseMessage> Subscribe([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)] HttpRequest req, ILogger logger)
        {
            logger.LogInformation("Subscribe function initiated...");
            string userName = req.Query["userId"];
            //var callbackUrl = new Uri(Configuration[""]);

            var channelId = await GetChannelIdForUserName(userName);
            logger.LogInformation(channelId);
            if (channelId != null)
                return new HttpResponseMessage(HttpStatusCode.OK);
            else
                return new HttpResponseMessage(HttpStatusCode.NotFound);
        }

        [FunctionName("SendDiscordStreamStartNotification")]
        public async Task SendDiscordStreamStartNotification([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)] HttpRequest req, ILogger logger)
        {
            logger.LogInformation("Subscribe function initiated...");
            string userName = req.Query["userId"];
            var callbackUrl = new Uri(Configuration[""]);

            var channelId = await GetChannelIdForUserName(userName);
            logger.LogInformation(channelId);
        }
    }

}
