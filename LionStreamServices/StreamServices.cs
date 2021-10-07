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

namespace StreamServices
{
    public sealed class StreamServices
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public StreamServices(IHttpClientFactory clientFactory)
        {
            _httpClientFactory = clientFactory;
        }

        [FunctionName("StreamStartNotification")]
        public HttpResponseMessage StreamStartedNotificationAsync(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("StreamStartNotification function initiated...");

            var challenge = req.Query["hub.challenge"].ToString();

            if (!string.IsNullOrEmpty(challenge))
            {
                log.LogInformation($"Successfully subscribed to channel");

                return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(challenge) };  
            }
            else
            {
                return new HttpResponseMessage(HttpStatusCode.NotFound);
            }


        }
    }

}
