// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Deployment.Configuration {

    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Microsoft.Graph;

    class ApplicationSettings {

        public Guid Id { get; set; }
        public string DisplayName { get; set; }
        public List<string> IdentifierUris { get; set; }
        public Guid AppId { get; set; }

        public ApplicationSettings() {
            IdentifierUris = new List<string>();
        }

        public ApplicationSettings(
            Guid id,
            string displayName,
            IEnumerable<string> identifierUris,
            Guid appId
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
            Id = new Guid(application.Id);
            DisplayName = application.DisplayName;
            AppId = new Guid(application.AppId);

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
                Id = Id.ToString(),
                DisplayName = DisplayName,
                AppId = AppId.ToString(),
                IdentifierUris = IdentifierUris
            };

            return application;
        }

        public void Validate(string parentProperty) {
            if (default == Id) {
                throw new Exception($"{parentProperty}.Id" +
                    $" configuration property is missing.");
            }

            if (string.IsNullOrEmpty(DisplayName)) {
                throw new Exception($"{parentProperty}.DisplayName" +
                    $" configuration property is missing or is empty.");
            }

            if (null == IdentifierUris || IdentifierUris.Count == 0) {
                throw new Exception($"{parentProperty}.IdentifierUris" +
                    $" configuration property is missing or is empty.");
            }

            if (default == AppId) {
                throw new Exception($"{parentProperty}.AppId" +
                    $" configuration property is missing.");
            }
        }
    }
}
