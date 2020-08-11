using Neo.IO.Json;

namespace Neo.Plugins
{
    public class SubNetworkIdentifier
    {
        public string Network { get; set; }
        public Metadata Metadata { get; set; }

        public SubNetworkIdentifier(string network, Metadata metadata = null)
        {
            Network = network;
            Metadata = metadata;
        }

        public JObject ToJson()
        {
            JObject json = new JObject();
            json["network"] = Network;
            if (Metadata != null && Metadata.ToJson() != null)
                json["metadata"] = Metadata.ToJson();
            return json;
        }
    }
}
