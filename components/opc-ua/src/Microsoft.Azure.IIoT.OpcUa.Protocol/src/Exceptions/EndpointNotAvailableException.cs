// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------


namespace Microsoft.Azure.IIoT.OpcUa.Protocol.Exceptions {
    using Microsoft.Azure.IIoT.OpcUa.Core.Models;
    using System;

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
