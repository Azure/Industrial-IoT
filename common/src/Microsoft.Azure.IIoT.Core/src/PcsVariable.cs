// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT
{
    /// <summary>
    /// Common runtime environment variables
    /// </summary>
    public static class PcsVariable
    {
#pragma warning disable CA1707 // Identifiers should not contain underscores
        /// <summary> Service name </summary>
        public const string PCS_SERVICE_NAME =
            "PCS_SERVICE_NAME";
        /// <summary> Iot hub connection string </summary>
        public const string PCS_IOTHUB_CONNSTRING =
            "PCS_IOTHUB_CONNSTRING";
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
        /// <summary> Auth service endpoint url </summary>
        public const string PCS_AUTH_SERVICE_URL =
            "PCS_AUTH_SERVICE_URL";
        /// <summary> OPC publisher service endpoint url </summary>
        public const string PCS_PUBLISHER_SERVICE_URL =
            "PCS_PUBLISHER_SERVICE_URL";
        /// <summary> URL path base for TSI query </summary>
        public const string PCS_TSI_URL =
            "PCS_TSI_URL";
        /// <summary> Log Analytics workbook id </summary>
        public const string PCS_WORKBOOK_ID =
            "PCS_WORKBOOK_ID";
        /// <summary> Subscription id </summary>
        public const string PCS_SUBSCRIPTION_ID =
            "PCS_SUBSCRIPTION_ID";
        /// <summary> Resource group </summary>
        public const string PCS_RESOURCE_GROUP =
            "PCS_RESOURCE_GROUP";
#pragma warning restore CA1707 // Identifiers should not contain underscores
    }
}
