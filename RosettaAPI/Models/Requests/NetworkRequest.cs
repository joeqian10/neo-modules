using System.Collections.Generic;

namespace Neo.Plugins
{
    public class NetworkRequest
    {
        public NetworkIdentifier NetworkIdentifier { get; set; }
        public Dictionary<string, object> Metadata { get; set; }
    }
}
