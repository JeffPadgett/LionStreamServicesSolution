using StreamServices.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace StreamServices.Test
{
    public class Subscribe
    {
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
                    
                }

            }

            //act

            //assert
        }
    }
}
