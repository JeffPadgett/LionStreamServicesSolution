using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace LionStreamServices.Models
{
    class TwitchWebhookSubscriptionBody
    {

		[JsonProperty("hub.callback")]
		public string Callback { get; set; }

		[JsonProperty("hub.mode")]
		public string Mode { get; set; }

		[JsonProperty("hub.topic")]
		public string Topic { get; set; }

		[JsonProperty("hub.lease_seconds")]
		public int Lease_seconds { get; set; }

		[JsonProperty("hub.secret")]
		public string Secret { get; set; }
	}
}
