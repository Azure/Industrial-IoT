// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa
{
    /// <summary>
    /// Common twin properties
    /// </summary>
    public static class Constants
    {
        /// <summary>
        /// Type property name constant
        /// </summary>
        public const string TwinPropertyTypeKey = "__type__";

        /// <summary>
        /// Site id property name constant
        /// </summary>
        public const string TwinPropertySiteKey = "__siteid__";

        /// <summary>
        /// Semver Version property name constant
        /// </summary>
        public const string TwinPropertyVersionKey = "__version__";

        /// <summary>
        /// Full Version property name constant
        /// </summary>
        public const string TwinPropertyFullVersionKey = "__fullversion__";

        /// <summary>
        /// Scheme property constant
        /// </summary>
        public const string TwinPropertySchemeKey = "__scheme__";

        /// <summary>
        /// Hostname property constant
        /// </summary>
        public const string TwinPropertyHostnameKey = "__hostname__";

        /// <summary>
        /// Adresseses property constant
        /// </summary>
        public const string TwinPropertyIpAddressesKey = "__ip__";

        /// <summary>
        /// Port key constant
        /// </summary>
        public const string TwinPropertyPortKey = "__port__";

        /// <summary>
        /// Spi key property name constant
        /// </summary>
        public const string TwinPropertyApiKeyKey = "__apikey__";

        /// <summary>
        /// Certificate property name constant
        /// </summary>
        public const string TwinPropertyCertificateKey = "__certificate__";

        /// <summary>
        /// Routing info message property
        /// </summary>
        public const string MessagePropertyRoutingKey = "$$RoutingInfo";

        /// <summary>
        /// Routing info message property
        /// </summary>
        public const string MessagePropertySchemaKey = "$$MessageSchema";

        /// <summary>
        /// Gateway identity
        /// </summary>
        public const string EntityTypeGateway = "iiotedge";

        /// <summary>
        /// Publisher module identity
        /// </summary>
        public const string EntityTypePublisher = "OpcPublisher";

        /// <summary>
        /// Endpoint identity
        /// </summary>
        public const string EntityTypeEndpoint = "Endpoint";

        /// <summary>
        /// Application identity
        /// </summary>
        public const string EntityTypeApplication = "Application";
    }
}
