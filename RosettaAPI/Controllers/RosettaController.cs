using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Neo.IO.Json;

namespace Neo.Plugins
{
    public partial class RosettaController: ControllerBase
    {
        private readonly NeoSystem system;

        public RosettaController(NeoSystem system)
        {
            this.system = system;
        }

        private ContentResult FormatJson(JObject jObject)
        {
            return Content(jObject.ToString(), "application/json");
        }
    }
}
