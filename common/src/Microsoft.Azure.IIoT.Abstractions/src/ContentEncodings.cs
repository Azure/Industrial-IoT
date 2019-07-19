// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT {

    /// <summary>
    /// Content encoding constants
    /// </summary>
    public static class ContentEncodings {

        /// <summary>
        /// Binary blob
        /// </summary>
        public const string MimeTypeBinary =
             "application/octet-stream";
        /// <summary>
        /// Json encoding
        /// </summary>
        public const string MimeTypeJson =
            "application/json";
        /// <summary>
        /// Bson encoding
        /// </summary>
        public const string MimeTypeBson =
            "application/bson";
        /// <summary>
        /// Message pack encoding
        /// </summary>
        public const string MimeTypeMsgPack =
            "application/binary-msgpack";

        /// <summary>
        /// OPC UA json encoding as per OPC UA part 6
        /// </summary>
        public const string MimeTypeUaJson =
            "application/ua+json";
        /// <summary>
        /// OPC UA bson encoding
        /// </summary>
        public const string MimeTypeUaBson =
            "application/ua+bson";
        /// <summary>
        /// OPC UA json encoding but non reversible
        /// </summary>
        public const string MimeTypeUaNonReversibleJson =
            "application/ua+json+nr";

        /// <summary>
        /// OPC UA binary encoding
        /// </summary>
        public const string MimeTypeUaBinary =
            "application/ua+binary";
        /// <summary>
        /// OPC UA xml encoding
        /// </summary>
        public const string MimeTypeUaXml =
            "application/ua+xml";
        /// <summary>
        /// OPC UA nodset xml encoding
        /// </summary>
        public const string MimeTypeUaNodesetXml =
            "application/ua-nodeset+xml";

        /// <summary>
        /// (For testing) Reference encoder
        /// </summary>
        public const string MimeTypeUaJsonReference =
            "application/ua+json+ref";
        /// <summary>
        /// (For testing) Reference encoder but non reversible
        /// </summary>
        public const string MimeTypeUaNonReversibleJsonReference =
            "application/ua+json+ref+nr";

        /// <summary>
        /// Certificate content
        /// </summary>
        public const string MimeTypeCert =
            "application/pkix-cert";
        /// <summary>
        /// Crl content
        /// </summary>
        public const string MimeTypeCrl =
            "application/pkix-crl";
        /// <summary>
        /// see CertificateContentType.Pfx
        /// </summary>
        public const string MimeTypePfxCert =
            "application/x-pkcs12";
        /// <summary>
        /// see CertificateContentType.Pem
        /// </summary>
        public const string MimeTypePemCert =
            "application/x-pem-file";
    }
}
