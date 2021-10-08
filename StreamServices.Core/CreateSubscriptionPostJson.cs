using System;

namespace StreamServices.Core
{

    public class CreateSubscriptionPostJson
    {
        public string Type { get; set; }
        public string Version { get; set; }
        public Condition Condition { get; set; } = new Condition();
        public Transport Transport { get; set; } = new Transport();
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
