// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.App.Runtime
{
    /// <summary>
    /// Environment variables
    /// </summary>
    internal static class EnvVars
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
        /// <summary> Client application id </summary>
        public const string PCS_AAD_CONFIDENTIAL_CLIENT_APPID =
            "PCS_AUTH_CLIENT_APPID";
        /// <summary> Client application secret </summary>
        public const string PCS_AAD_CONFIDENTIAL_CLIENT_SECRET =
            "PCS_AUTH_CLIENT_SECRET";
#pragma warning restore CA1707 // Identifiers should not contain underscores
    }
}
