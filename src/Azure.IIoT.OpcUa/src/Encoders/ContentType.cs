// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Encoders
{
    /// <summary>
    /// Content types
    /// </summary>
    public static class ContentType
    {
        /// <summary>
        /// Json+Gzip encoding
        /// </summary>
        public const string JsonGzip =
            "application/json+gzip";

        /// <summary>
        /// OPC UA json encoding as per OPC UA part 6
        /// </summary>
        public const string UaJson =
            "application/ua+json";

        /// <summary>
        /// OPC UA json encoding but non reversible
        /// </summary>
        public const string UaNonReversibleJson =
            "application/ua+json+nr";

        /// <summary>
        /// OPC UA UADP binary encoding
        /// </summary>
        public const string Uadp =
            "application/opcua+uadp";

        /// <summary>
        /// For backwards compatibility with legacy publisher
        /// </summary>
        public const string UaLegacyPublisher =
            "application/opcua+uajson";
    }
}
