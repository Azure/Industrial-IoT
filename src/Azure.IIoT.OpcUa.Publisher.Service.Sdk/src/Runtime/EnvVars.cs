// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Service.Sdk.Runtime
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
        /// <summary> Built in Auth server disabled </summary>
        public const string PCS_AUTH_SERVICE_DISABLED =
            "PCS_AUTH_SERVICE_DISABLED";
        /// <summary> Built in Auth server service application id </summary>
        public const string PCS_AUTH_SERVICE_SERVICE_APPID =
            "PCS_AUTH_SERVICE_SERVICE_APPID";
        /// <summary> Built in Auth server service secret </summary>
        public const string PCS_AUTH_SERVICE_SERVICE_SECRET =
            "PCS_AUTH_SERVICE_SERVICE_SECRET";
        /// <summary> Client application id </summary>
        public const string PCS_AAD_CONFIDENTIAL_CLIENT_APPID =
            "PCS_AUTH_CLIENT_APPID";
        /// <summary> Client application secret </summary>
        public const string PCS_AAD_CONFIDENTIAL_CLIENT_SECRET =
            "PCS_AUTH_CLIENT_SECRET";
        /// <summary> Client application id </summary>
        public const string PCS_AAD_PUBLIC_CLIENT_APPID =
            "PCS_AUTH_PUBLIC_CLIENT_APPID";
        /// <summary> Service url </summary>
        public const string PCS_SERVICE_URL =
            "PCS_SERVICE_URL";
        /// <summary> OPC publisher service endpoint url </summary>
        public const string PCS_PUBLISHER_SERVICE_URL =
            "PCS_PUBLISHER_SERVICE_URL";
#pragma warning restore CA1707 // Identifiers should not contain underscores
    }
}
