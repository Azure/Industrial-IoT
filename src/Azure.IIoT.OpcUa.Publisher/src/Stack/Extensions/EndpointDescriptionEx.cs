// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Opc.Ua
{
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Azure.IIoT.OpcUa.Publisher.Stack;

    /// <summary>
    /// Endpoint description extensions
    /// </summary>
    public static class EndpointDescriptionEx
    {
        /// <summary>
        /// Matches model
        /// </summary>
        /// <param name="endpoint"></param>
        /// <param name="model"></param>
        /// <returns></returns>
        public static bool IsSameAs(this EndpointDescription endpoint,
            EndpointModel model)
        {
            if (endpoint.SecurityMode !=
                (model.SecurityMode ?? SecurityMode.SignAndEncrypt).ToStackType())
            {
                return false;
            }
            if (string.IsNullOrEmpty(model.SecurityPolicy))
            {
                return true;
            }
            return endpoint.SecurityPolicyUri == model.SecurityPolicy;
        }
    }
}
