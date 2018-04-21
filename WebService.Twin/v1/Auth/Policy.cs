// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------


namespace Microsoft.Azure.IIoT.OpcTwin.WebService.v1.Auth {
    using System.Collections.Generic;

    /// <summary>
    /// Defines various policies for user claims for the rest api.
    /// Policies are associated with roles, and users are assigned 
    /// to roles.
    /// </summary>
    public static class Policy {

        /// <summary>
        /// Allowed to add new endpoint
        /// </summary>
        public const string RegisterTwins = 
            nameof(RegisterTwins);

        /// <summary>
        /// Allowed to browse
        /// </summary>
        public const string BrowseTwins =
            nameof(BrowseTwins);

        /// <summary>
        /// Allowed to write or execute
        /// </summary>
        public const string ControlTwins = 
            nameof(ControlTwins);

        /// <summary>
        /// Allowed to request publish
        /// </summary>
        public const string PublishNodes = 
            nameof(PublishNodes);

        /// <summary>
        /// Allowed to download certificate
        /// </summary>
        public const string DownloadCertificate = 
            nameof(DownloadCertificate);

        /// <summary>
        /// Return all policies
        /// </summary>
        /// <returns></returns>
        public static IEnumerable<string> All() {
            yield return RegisterTwins;
            yield return BrowseTwins;
            yield return ControlTwins;
            yield return PublishNodes;
            yield return DownloadCertificate;
        }
    }
}
