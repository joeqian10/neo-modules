using Neo.IO.Json;

namespace Neo.Plugins
{
    public class ConstructionDeriveRequest
    {
        public NetworkIdentifier NetworkIdentifier { get; set; }
        public PublicKey PublicKey { get; set; }
        public Metadata Metadata { get; set; }

        public ConstructionDeriveRequest(NetworkIdentifier networkIdentifier, PublicKey publicKey, Metadata metadata = null)
        {
            NetworkIdentifier = networkIdentifier;
            PublicKey = publicKey;
            Metadata = metadata;
        }

        public JObject ToJson()
        {
            JObject json = new JObject();
            json["network_identifier"] = NetworkIdentifier.ToJson();
            json["public_key"] = PublicKey.ToJson();
            if (Metadata != null && Metadata.ToJson() != null)
                json["metadata"] = Metadata.ToJson();
            return json;
        }
    }
}
