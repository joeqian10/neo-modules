using System;
using System.Collections.Generic;
using System.Text;

namespace Neo.Plugins
{
    public class AccountBalanceRequest
    {
        public NetworkIdentifier NetworkIdentifier { get; set; }
        public AccountIdentifier AccountIdentifier { get; set; }
        public PartialBlockIdentifier BlockIdentifier { get; set; }
    }
}
