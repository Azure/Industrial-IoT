// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Service.Sdk.Runtime
{
    /// <summary>
    /// Common runtime environment variables
    /// </summary>
    internal static class EnvVars
    {
        /// <summary> Aad Auth tenant </summary>
        public const string PCS_AUTH_TENANT =
            "PCS_AUTH_TENANT";
        /// <summary> Aad Instance </summary>
        public const string PCS_AAD_INSTANCE =
            "PCS_AUTH_INSTANCE";
        /// <summary> Service application id </summary>
        public const string PCS_AAD_SERVICE_APPID =
            "PCS_AUTH_SERVICE_APPID";
        /// <summary> Client application id </summary>
        public const string PCS_AAD_PUBLIC_CLIENT_APPID =
            "PCS_AUTH_PUBLIC_CLIENT_APPID";
    }
}
