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
        public async Task Subscribe([QueueTrigger("twitch-channel-subscription")] HttpRequest msg, ILogger logger)
        {
            logger.LogInformation("Subscribe function initiated...");

            //var channelId = await GetChannelIdForUserName(msg);


        }
    }

}
