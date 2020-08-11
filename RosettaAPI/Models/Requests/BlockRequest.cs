using Neo.IO.Json;

namespace Neo.Plugins
{
    public class BlockRequest
    {
        public NetworkIdentifier NetworkIdentifier { get; set; }
        public PartialBlockIdentifier BlockIdentifier { get; set; }

        public JObject ToJson()
        {
            JObject json = new JObject();
            json["network_identifier"] = NetworkIdentifier.ToJson();
            json["block_identifier"] = BlockIdentifier.ToJson();
            return json;
        }
    }
}
