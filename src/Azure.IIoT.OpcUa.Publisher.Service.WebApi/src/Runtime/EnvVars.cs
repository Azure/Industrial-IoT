// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Service.WebApi
{
    /// <summary>
    /// Common runtime environment variables
    /// </summary>
    public static class EnvVars
    {
#pragma warning disable CA1707 // Identifiers should not contain underscores
        /// <summary> Aad Auth tenant </summary>
        public const string PCS_AUTH_TENANT =
            "PCS_AUTH_TENANT";
        /// <summary> Aad Instance </summary>
        public const string PCS_AAD_INSTANCE =
            "PCS_AUTH_INSTANCE";
        /// <summary> Aad valid audience or null if disabled </summary>
        public const string PCS_AAD_AUDIENCE =
            "PCS_AUTH_AUDIENCE";
        /// <summary> Service application id </summary>
        public const string PCS_AAD_SERVICE_APPID =
            "PCS_AUTH_SERVICE_APPID";
        /// <summary> Service secret </summary>
        public const string PCS_AAD_SERVICE_SECRET =
            "PCS_AUTH_SERVICE_SECRET";
#pragma warning restore CA1707 // Identifiers should not contain underscores
    }
}
