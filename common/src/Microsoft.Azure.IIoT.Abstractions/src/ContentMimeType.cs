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
        /// Bson encoding
        /// </summary>
        public const string Bson =
            "application/bson";
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
        /// OPC UA bson encoding
        /// </summary>
        public const string UaBson =
            "application/ua+bson";
        /// <summary>
        /// OPC UA json encoding but non reversible
        /// </summary>
        public const string UaNonReversibleJson =
            "application/ua+json+nr";

        /// <summary>
        /// OPC UA binary encoding
        /// </summary>
        public const string UaBinary =
            "application/ua+binary";
        /// <summary>
        /// OPC UA UADP binary encoding
        /// </summary>
        public const string Uadp =
            "application/opcua+uadp";
        /// <summary>
        /// OPC UA xml encoding
        /// </summary>
        public const string UaXml =
            "application/ua+xml";

        /// <summary>
        /// (For testing) Reference encoder
        /// </summary>
        public const string UaJsonReference =
            "application/ua+json+ref";
        /// <summary>
        /// (For testing) Reference encoder but non reversible
        /// </summary>
        public const string UaNonReversibleJsonReference =
            "application/ua+json+ref+nr";

        /// <summary>
        /// For backwards compatibility with legacy publisher
        /// </summary>
        public const string UaLegacyPublisher =
            "application/opcua+uajson";

        /// <summary>
        /// Certificate content
        /// </summary>
        public const string Cert =
            "application/pkix-cert";
        /// <summary>
        /// Crl content
        /// </summary>
        public const string Crl =
            "application/pkix-crl";
        /// <summary>
        /// see CertificateContentType.Pfx
        /// </summary>
        public const string PfxCert =
            "application/x-pkcs12";
        /// <summary>
        /// see CertificateContentType.Pem
        /// </summary>
        public const string PemCert =
            "application/x-pem-file";
    }
}
