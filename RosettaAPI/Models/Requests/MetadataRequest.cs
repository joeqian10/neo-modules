using Neo.IO.Json;

namespace Neo.Plugins
{
    public class MetadataRequest
    {
        public Metadata Metadata { get; set; }

        public JObject ToJson()
        {
            JObject json = new JObject();
            if (Metadata != null && Metadata.ToJson() != null)
                json["metadata"] = Metadata.ToJson();
            return json;
        }
    }
}
