using Neo.IO.Json;

namespace Neo.Plugins
{
    public class ConstructionSubmitRequest
    {
        public NetworkIdentifier NetworkIdentifier { get; set; }
        public string SignedTransaction { get; set; }

        public ConstructionSubmitRequest(NetworkIdentifier networkIdentifier, string signedTransaction)
        {
            NetworkIdentifier = networkIdentifier;
            SignedTransaction = signedTransaction;
        }

        public JObject ToJson()
        {
            JObject json = new JObject();
            json["network_identifier"] = NetworkIdentifier.ToJson();
            json["signed_transaction"] = SignedTransaction;
            return json;
        }
    }
}
