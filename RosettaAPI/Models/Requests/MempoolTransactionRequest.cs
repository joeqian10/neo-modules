using Neo.IO.Json;

namespace Neo.Plugins
{
    public class MempoolTransactionRequest
    {
        public NetworkIdentifier NetworkIdentifier { get; set; }
        public TransactionIdentifier TransactionIdentifier { get; set; }

        public MempoolTransactionRequest(NetworkIdentifier networkIdentifier, TransactionIdentifier transactionIdentifier)
        {
            NetworkIdentifier = networkIdentifier;
            TransactionIdentifier = transactionIdentifier;
        }

        public JObject ToJson()
        {
            JObject json = new JObject();
            json["network_identifier"] = NetworkIdentifier.ToJson();
            json["transaction_identifier"] = TransactionIdentifier.ToJson();
            return json;
        }
    }
}
