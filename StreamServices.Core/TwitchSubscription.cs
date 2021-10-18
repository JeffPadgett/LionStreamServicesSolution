using Newtonsoft.Json;
using System;
using System.Linq;

namespace StreamServices.Core
{

    public class TwitchSubscription
    {

        public TwitchSubscription(string userId, string eventType = "stream.online")
        {
            Condition = new Condition(userId);
            Transport = new Transport();
            Type = eventType;
        }
        [JsonProperty("type")]
        public string Type { get; set; }
        [JsonProperty("version")]
        public string Version { get; set; } = "1";
        [JsonProperty("condition")]
        public Condition Condition { get; set; }

        [JsonProperty("transport")]
        public Transport Transport { get; set; }
    }

    public class Condition
    {
        public Condition(string userId)
        {

            BroadcasterUserId = userId;
        }
        [JsonProperty("broadcaster_user_id")]
        public string BroadcasterUserId { get; set; }
    }

    public class Transport
    {
        public Transport()
        {
            Secret = Environment.GetEnvironmentVariable("EventSubSecret");
            Callback = Environment.GetEnvironmentVariable("StreamStartFunctionUri");
            Method = "webhook";
        }
        [JsonProperty("method")]
        public string Method { get; set; }
        [JsonProperty("callback")]
        public string Callback { get; set; }
        [JsonProperty("secret")]
        public string Secret { get; set; }
    }

}
