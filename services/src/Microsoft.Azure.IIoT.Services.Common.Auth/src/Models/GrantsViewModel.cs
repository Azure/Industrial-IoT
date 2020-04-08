// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.


namespace Microsoft.Azure.IIoT.Services.Common.Auth {
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Grants
    /// </summary>
    public class GrantsViewModel {

        /// <summary>
        /// List of grants
        /// </summary>
        public IEnumerable<GrantViewModel> Grants { get; set; }
    }

    /// <summary>
    /// Grant
    /// </summary>
    public class GrantViewModel {

        /// <summary>
        /// Client id
        /// </summary>
        public string ClientId { get; set; }

        /// <summary>
        /// Client name
        /// </summary>
        public string ClientName { get; set; }

        /// <summary>
        /// Client url
        /// </summary>
        public string ClientUrl { get; set; }

        /// <summary>
        /// Client logo
        /// </summary>
        public string ClientLogoUrl { get; set; }

        /// <summary>
        /// Created
        /// </summary>
        public DateTime Created { get; set; }

        /// <summary>
        /// Expiration
        /// </summary>
        public DateTime? Expires { get; set; }

        /// <summary>
        /// Grant names for identity
        /// </summary>
        public IEnumerable<string> IdentityGrantNames { get; set; }

        /// <summary>
        /// Api grants
        /// </summary>
        public IEnumerable<string> ApiGrantNames { get; set; }
    }
}