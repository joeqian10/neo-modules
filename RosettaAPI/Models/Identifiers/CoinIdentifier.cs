using Neo.IO.Json;

namespace Neo.Plugins
{
    public class CoinIdentifier
    {
        // tx hash + ":" + output index
        public string Identifier { get; set; }

        public CoinIdentifier(string identifier)
        {
            Identifier = identifier;
        }

        public CoinIdentifier(UInt256 txHash, int index)
        {
            Identifier = txHash.ToString() + ":" + index.ToString();
        }

        public UInt256 GetTxHash()
        {
            int index = Identifier.IndexOf(":");
            return UInt256.Parse(Identifier.Substring(0, index));
        }

        public int GetIndex()
        {
            int index = Identifier.IndexOf(":");
            return int.Parse(Identifier.Substring(index + 1));
        }

        public JObject ToJson()
        {
            JObject json = new JObject();
            json["identifier"] = Identifier;
            return json;
        }
    }
}
