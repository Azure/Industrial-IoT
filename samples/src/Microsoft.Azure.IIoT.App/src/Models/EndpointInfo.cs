using Microsoft.Azure.IIoT.OpcUa.Api.Registry.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.Azure.IIoT.App.Models {
    public class EndpointInfo {
        public EndpointInfoApiModel endpointModel { get; set; }
        public bool endpointState { get; set; }
    }
}
