﻿using Neo.IO.Json;

namespace Neo.Plugins
{
    // Amount is some Value of a Currency. It is considered invalid to specify a Value without a
    // Currency.
    public class Amount
    {
        // Value of the transaction in atomic units represented as an arbitrary-sized signed integer.
        // For example, 1 BTC would be represented by a value of 100000000.
        public string Value { get; set; }
        public Currency Currency { get; set; }
        public Metadata Metadata { get; set; }

        public Amount(string value, Currency currency, Metadata metadata = null)
        {
            Value = value;
            Currency = currency;
            Metadata = metadata;
        }

        public JObject ToJson()
        {
            JObject json = new JObject();
            json["value"] = Value;
            json["currency"] = Currency.ToJson();
            if (Metadata != null && Metadata.ToJson() != null)
                json["metadata"] = Metadata.ToJson();
            return json;
        }
    }
}