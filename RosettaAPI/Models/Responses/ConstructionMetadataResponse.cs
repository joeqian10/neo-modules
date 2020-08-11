using Neo.IO.Json;

namespace Neo.Plugins
{
    public class ConstructionMetadataResponse
    {
        public Metadata Metadata { get; set; }

        public ConstructionMetadataResponse(Metadata metadata = null)
        {
            Metadata = metadata;
        }

        public JObject ToJson()
        {
            JObject json = new JObject();
            if (Metadata != null && Metadata.ToJson() != null)
                json["metadata"] = Metadata.ToJson();
            return json;
        }
    }
}
