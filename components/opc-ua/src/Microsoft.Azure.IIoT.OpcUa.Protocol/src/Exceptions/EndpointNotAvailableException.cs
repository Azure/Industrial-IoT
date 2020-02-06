using Microsoft.Azure.IIoT.OpcUa.Core.Models;
using Opc.Ua;
using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Azure.IIoT.OpcUa.Protocol.Exceptions {
    /// <summary>
    /// Exception when an endpoint with requested security settings is not available.
    /// </summary>
    public class EndpointNotAvailableException : Exception {
        /// <summary>
        /// Creates a new instance of EndpointNotAvailableException.
        /// </summary>
        /// <param name="url"></param>
        /// <param name="securityMode"></param>
        /// <param name="securityPolicyUrl"></param>
        public EndpointNotAvailableException(string url, SecurityMode? securityMode, string securityPolicyUrl) : base($"There is not endpoint with requested security settings (SecurityMode: {securityMode}, SecurityPolicyUrl: {securityPolicyUrl}) available at url '{url}'.") { }
    }
}
