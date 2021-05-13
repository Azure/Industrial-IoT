// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Opc.Ua.Extensions {
    using Microsoft.Azure.IIoT.OpcUa.Core.Models;
    using Microsoft.Azure.IIoT.OpcUa.Protocol;

    /// <summary>
    /// Endpoint description extensions
    /// </summary>
    public static class EndpointDescriptionEx {

        /// <summary>
        /// Matches model
        /// </summary>
        /// <param name="endpoint"></param>
        /// <param name="model"></param>
        /// <returns></returns>
        public static bool IsSameAs(this EndpointDescription endpoint,
            EndpointModel model) {

            return

            endpoint.SecurityMode == (model.SecurityMode ?? SecurityMode.SignAndEncrypt)
           .ToStackType() &&
       endpoint.SecurityPolicyUri == model.SecurityPolicy;

        }
    }
}
