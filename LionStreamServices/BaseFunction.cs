using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;

namespace LionStreamServices
{
    public abstract class BaseFunction
    {

        protected IHttpClientFactory HttpClientFactory { get; }

        protected IConfiguration Configuration { get; }

        protected BaseFunction(IHttpClientFactory httpClientFactory, IConfiguration configuration)
        {
            HttpClientFactory = httpClientFactory;
            Configuration = configuration;
        }

        public string TwitchClientID { get { return Environment.GetEnvironmentVariable("ClientId"); } }


        protected HttpClient GetHttpClient(string baseAddress, string clientId = "", bool includeJson = true)
        {

            if (clientId == "") clientId = TwitchClientID;

            var client = HttpClientFactory.CreateClient();
            client.BaseAddress = new Uri(baseAddress);

            if (includeJson)
            {
                client.DefaultRequestHeaders.Add("Accept", @"application/json");
            }
            client.DefaultRequestHeaders.Add("Accept", @"application/vnd.twitchtv.v5+json");
            client.DefaultRequestHeaders.Add("Client-ID", clientId);

            return client;

        }
    }
}
