using StreamServices.Core;
using System;
using System.Net.Http;
using System.Net.Http.Json;
using Xunit;
using Xunit.Abstractions;

namespace StreamServices.Test
{
    public class Subscribe
    {
        private readonly ITestOutputHelper _output;

        public Subscribe(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public void SubscribeToChannelForOnlineNotifications()
        {
            //arrange
            CreateSubscriptionPostJson postRequest = new CreateSubscriptionPostJson();

            postRequest.Type = "stream.online";
            postRequest.Condition.Broadcaster_user_id = "48646924";

            postRequest.Transport.Method = "webhook";
            postRequest.Transport.Callback = "https://lionstream.azurewebsites.net/api/StreamStartNotification?";
            postRequest.Transport.Secret = "kw37f9enu68mipwpsok2b8htuz16k3";

            using (var client = new HttpClient())
            {
                client.BaseAddress = new Uri("https://api.twitch.tv/");
                var response = client.PostAsJsonAsync("helix/eventsub/subscriptions", postRequest).Result;
                if (response.IsSuccessStatusCode)
                {
                    _output.WriteLine($"Headers: {response.Headers} ,Body: {response.Content} ");
                }
                else
                {
                    _output.WriteLine($"ERROR : {response.StatusCode}");
                }

            }

            //Now it is going to send a request to the actual azure function hosted in azure
            //It sends a challenge and wants to recieve it back .If it recieves it back then it is subscribed. 

            //act

            //assert
        }
    }
}
