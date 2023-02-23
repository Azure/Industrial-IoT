// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT {
    /// <summary>
    /// Content mime types
    /// </summary>
    public static class ContentMimeType {
        /// <summary>
        /// Binary blob
        /// </summary>
        public const string Binary =
             "application/octet-stream";
        /// <summary>
        /// Json encoding
        /// </summary>
        public const string Json =
            "application/json";
        /// <summary>
        /// Json+Gzip encoding
        /// </summary>
        public const string JsonGzip =
            "application/json+gzip";

        /// <summary>
        /// Message pack encoding
        /// </summary>
        public const string MsgPack =
            "application/x-msgpack";

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
