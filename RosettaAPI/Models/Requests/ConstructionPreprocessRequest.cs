using Neo.IO.Json;
using System.Linq;

namespace Neo.Plugins
{
    public class ConstructionPreprocessRequest
    {
        public NetworkIdentifier NetworkIdentifier { get; set; }
        public Operation[] Operations { get; set; }
        public Metadata Metadata { get; set; }

        public ConstructionPreprocessRequest(NetworkIdentifier networkIdentifier, Operation[] operations, Metadata metadata = null)
        {
            NetworkIdentifier = networkIdentifier;
            Operations = operations;
            Metadata = metadata;
        }

        public JObject ToJson()
        {
            JObject json = new JObject();
            json["network_identifier"] = NetworkIdentifier.ToJson();
            json["operations"] = Operations.Select(p => p.ToJson()).ToArray();
            if (Metadata != null && Metadata.ToJson() != null)
                json["metadata"] = Metadata.ToJson();
            return json;
        }
    }
}
