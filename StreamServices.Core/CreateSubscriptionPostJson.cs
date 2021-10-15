using Newtonsoft.Json;
using System;
using System.Linq;

namespace StreamServices.Core
{

    public class CreateSubscriptionPostJson
    {

        public CreateSubscriptionPostJson(string userId, string eventType = "stream.online")
        {
            Condition = new Condition(userId);
            Transport = new Transport();
            Type = eventType;
        }

        public string Type { get; set; }
        public string Version { get; set; } = "1";
        public Condition Condition { get; set; }

        [JsonProperty("transport")]
        public Transport Transport { get; set; }
    }

    public class Condition
    {
        public Condition(string userId)
        {
            Broadcaster_user_id = userId;
        }

        public string Broadcaster_user_id { get; set; }
    }

    public class Transport
    {
        public Transport()
        {
            Secret = Environment.GetEnvironmentVariable("EventSubSecret");
            Callback = Environment.GetEnvironmentVariable("StreamStartFunctionUri");
            Method = "webhook";
        }

        public string Method { get; set; }
        public string Callback { get; set; }
        public string Secret { get; set; }
    }

}
