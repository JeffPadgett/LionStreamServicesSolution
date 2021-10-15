using System;
using System.Collections.Generic;
using System.Text;

namespace StreamServices.Core
{

    public class CallbackChallengeJson
    {
        public string Challenge { get; set; }
        public Subscription Subscription { get; set; }
    }

    public class Subscription
    {
        public string Id { get; set; }
        public string Status { get; set; }
        public string Type { get; set; }
        public string Version { get; set; }
        public int Cost { get; set; }
        public Condition Condition { get; set; }
        public Transport Transport { get; set; }
        public DateTime Created_at { get; set; }
    }
}
