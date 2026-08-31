// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Core.Messaging.Clients.Dapr
{
    /// <summary>
    /// Dapr runtime environment variables.
    /// </summary>
    public static class EnvironmentVariable
    {
        /// <summary>Dapr api token.</summary>
        public const string DAPRAPITOKEN = "DAPR_API_TOKEN";

        /// <summary>Dapr http endpoint.</summary>
        public const string DAPRHTTPENDPOINT = "DAPR_HTTP_ENDPOINT";

        /// <summary>Dapr grpc endpoint.</summary>
        public const string DAPRGRPCENDPOINT = "DAPR_GRPC_ENDPOINT";
    }
}
