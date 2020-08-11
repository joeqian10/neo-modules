using Neo.IO.Json;
using System.Collections.Generic;

namespace Neo.Plugins
{
    public class Metadata
    {
        public Dictionary<string, JObject> Pairs { get; set; }

        public JObject this[string s]
        {
            get { return Pairs[s]; }
            set { Pairs[s] = value; }
        }

        public Metadata(Dictionary<string, JObject> pairs)
        {
            Pairs = pairs;
        }

        public bool TryGetValue(string key, out JObject value)
        {
            return Pairs.TryGetValue(key, out value);
        }

        public JObject ToJson()
        {
            if (Pairs != null && Pairs.Count > 0)
            {
                JObject meta = new JObject();
                foreach (var item in Pairs)
                    meta[item.Key] = item.Value;
                return meta;
            }
            return null;
        }
    }
}
