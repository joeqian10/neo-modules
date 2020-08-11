﻿using Neo.IO.Json;

namespace Neo.Plugins
{
    public class CoinChange
    {
        public CoinIdentifier CoinIdentifier { get; set; }
        public CoinAction CoinAction { get; set; }

        public CoinChange(CoinIdentifier coinIdentifier, CoinAction coinAction)
        {
            CoinIdentifier = coinIdentifier;
            CoinAction = coinAction;
        }

        public JObject ToJson()
        {
            JObject json = new JObject();
            json["coin_identifier"] = CoinIdentifier.ToJson();
            json["coin_action"] = CoinAction.AsString();
            return json;
        }
    }

    public enum CoinAction
    {
        CoinCreated,
        CoinSpent
    }
}
