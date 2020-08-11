using Neo.IO.Json;

namespace Neo.Plugins
{
    public class ConstructionParseRequest
    {
        public NetworkIdentifier NetworkIdentifier { get; set; }
        public bool Signed { get; set; }
        public string Transaction { get; set; }

        public ConstructionParseRequest(NetworkIdentifier networkIdentifier, bool signed, string transaction)
        {
            NetworkIdentifier = networkIdentifier;
            Signed = signed;
            Transaction = transaction;
        }

        public JObject ToJson()
        {
            JObject json = new JObject();
            json["network_identifier"] = NetworkIdentifier.ToJson();
            json["signed"] = Signed.ToString().ToLower();
            json["transaction"] = Transaction;
            return json;
        }
    }
}
