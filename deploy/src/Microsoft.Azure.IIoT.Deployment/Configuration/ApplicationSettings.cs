// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Deployment.Configuration {

    using System.Collections.Generic;
    using System.Linq;

    using Microsoft.Graph;

    class ApplicationSettings {

        public string Id { get; set; }
        public string DisplayName { get; set; }
        public List<string> IdentifierUris { get; set; }
        public string AppId { get; set; }

        public ApplicationSettings() {
            IdentifierUris = new List<string>();
        }

        public ApplicationSettings(
            string id,
            string displayName,
            IEnumerable<string> identifierUris,
            string appId
        ) {
            Id = id;
            DisplayName = displayName;
            AppId = appId;

            if (null != identifierUris) {
                IdentifierUris = identifierUris.ToList();
            }
            else {
                IdentifierUris = new List<string>();
            }
        }

        /// <summary>
        /// Create ApplicationSettings instance from Microsoft.Graph.Application object.
        /// </summary>
        /// <param name="application"></param>
        public ApplicationSettings(
            Application application
        ) {
            Id = application.Id;
            DisplayName = application.DisplayName;
            AppId = application.AppId;

            if (null != application.IdentifierUris) {
                IdentifierUris = application.IdentifierUris.ToList();
            }
            else {
                IdentifierUris = new List<string>();
            }
        }

        /// <summary>
        /// Create Microsoft.Graph.Application instance representing current ApplicationSettings.
        /// </summary>
        /// <returns></returns>
        public Application ToApplication() {
            var application = new Application() {
                Id = Id,
                DisplayName = DisplayName,
                AppId = AppId,
                IdentifierUris = IdentifierUris
            };

            return application;
        }
    }
}
