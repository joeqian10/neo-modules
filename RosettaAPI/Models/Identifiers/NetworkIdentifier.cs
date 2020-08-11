using Neo.IO.Json;

namespace Neo.Plugins
{
    public class NetworkIdentifier
    {
        public string Blockchain { get; set; }
        public string Network { get; set; }
        public SubNetworkIdentifier SubNetworkIdentifier { get; set; }

        public NetworkIdentifier(string blockChain, string network, SubNetworkIdentifier subNetworkIdentifier = null)
        {
            Blockchain = blockChain;
            Network = network;
            SubNetworkIdentifier = subNetworkIdentifier;
        }

        public JObject ToJson()
        {
            JObject json = new JObject();
            json["blockchain"] = Blockchain;
            json["network"] = Network;
            if (SubNetworkIdentifier != null)
                json["sub_network_identifier"] = SubNetworkIdentifier.ToJson();
            return json;
        }
    }
}
