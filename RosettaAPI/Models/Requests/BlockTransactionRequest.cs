using Neo.IO.Json;

namespace Neo.Plugins
{
    public class BlockTransactionRequest
    {
        public NetworkIdentifier NetworkIdentifier { get; set; }
        public BlockIdentifier BlockIdentifier { get; set; }
        public TransactionIdentifier TransactionIdentifier { get; set; }

        public JObject ToJson()
        {
            JObject json = new JObject();
            json["network_identifier"] = NetworkIdentifier.ToJson();
            json["block_identifier"] = BlockIdentifier.ToJson();
            json["transaction_identifier"] = TransactionIdentifier.ToJson();
            return json;
        }
    }
}
