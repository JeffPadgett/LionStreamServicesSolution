using System;
using System.Collections.Generic;
using System.Text;

namespace LionStreamServices.Models
{

    public class SubscribeBodyJson
    {
        public string Type { get; set; } = "stream.online";
        public string Version { get; set; } = "1";
        public Condition Condition { get; set; } = new Condition { Broadcaster_user_id = "za4li2la2mf36yspac8n6uruf5hoi1" };
        public Transport Transport { get; set; } = new Transport { Method = "webhook", Callback = Environment.GetEnvironmentVariable("StreamStartFunctionUri"), Secret = Environment.GetEnvironmentVariable("ClientSecret") };
    }

    public class Condition
    {
        public string Broadcaster_user_id { get; set; }
    }

    public class Transport
    {
        public string Method { get; set; }
        public string Callback { get; set; }
        public string Secret { get; set; }
    }


}

