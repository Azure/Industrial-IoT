// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------


namespace Microsoft.Azure.IoTSolutions.OpcUaExplorer.WebService.v1.Auth {
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
        public const string AddOpcServer = 
            nameof(AddOpcServer);

        /// <summary>
        /// Allowed to browse
        /// </summary>
        public const string BrowseOpcServer =
            nameof(BrowseOpcServer);

        /// <summary>
        /// Allowed to write or execute
        /// </summary>
        public const string ControlOpcServer = 
            nameof(ControlOpcServer);

        /// <summary>
        /// Allowed to request publish
        /// </summary>
        public const string PublishOpcNode = 
            nameof(PublishOpcNode);

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
            yield return AddOpcServer;
            yield return BrowseOpcServer;
            yield return ControlOpcServer;
            yield return PublishOpcNode;
            yield return DownloadCertificate;
        }
    }
}
