// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Registry.Models {
    using Microsoft.Azure.IIoT.Hub;
    using Microsoft.Azure.IIoT.Hub.Models;
    using Newtonsoft.Json.Linq;
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Base twin registration
    /// </summary>
    public static class BaseRegistrationEx {

        /// <summary>
        /// Create patch twin model to upload
        /// </summary>
        /// <param name="existing"></param>
        /// <param name="update"></param>
        public static DeviceTwinModel PatchBase(this BaseRegistration existing,
            BaseRegistration update) {

            var twin = new DeviceTwinModel {
                Etag = existing?.Etag,
                Tags = new Dictionary<string, JToken>(),
                Properties = new TwinPropertiesModel {
                    Desired = new Dictionary<string, JToken>()
                }
            };

            // Tags

            if (update?.ApplicationId != null &&
                update.ApplicationId != existing?.ApplicationId) {
                twin.Tags.Add(nameof(ApplicationId), update.ApplicationId);
            }

            if (update?.IsDisabled != null &&
                update.IsDisabled != existing?.IsDisabled) {
                twin.Tags.Add(nameof(BaseRegistration.IsDisabled), (update?.IsDisabled ?? false) ?
                    true : (bool?)null);
                twin.Tags.Add(nameof(BaseRegistration.NotSeenSince), (update?.IsDisabled ?? false) ?
                    DateTime.UtcNow : (DateTime?)null);
            }

            if (update?.SiteOrSupervisorId != existing?.SiteOrSupervisorId) {
                twin.Tags.Add(nameof(BaseRegistration.SiteOrSupervisorId), update?.SiteOrSupervisorId);
            }

            if (update?.SupervisorId != existing?.SupervisorId) {
                twin.Tags.Add(nameof(BaseRegistration.SupervisorId), update?.SupervisorId);
            }

            if (update?.SiteId != existing?.SiteId) {
                twin.Tags.Add(nameof(BaseRegistration.SiteId), update?.SiteId);
            }

            var certUpdate = update?.Certificate.DecodeAsByteArray().SequenceEqualsSafe(
                existing?.Certificate.DecodeAsByteArray());
            if (!(certUpdate ?? true)) {
                twin.Tags.Add(nameof(BaseRegistration.Certificate), update?.Certificate == null ?
                    null : JToken.FromObject(update.Certificate));
                twin.Tags.Add(nameof(BaseRegistration.Thumbprint),
                    update?.Certificate?.DecodeAsByteArray()?.ToSha1Hash());
            }

            twin.Tags.Add(nameof(BaseRegistration.DeviceType), update?.DeviceType);
            return twin;
        }

        /// <summary>
        /// Convert twin to registration information.
        /// </summary>
        /// <param name="twin"></param>
        /// <param name="onlyServerState"></param>
        /// <returns></returns>
        public static BaseRegistration ToRegistration(this DeviceTwinModel twin,
            bool onlyServerState = false) {
            if (twin == null) {
                return null;
            }
            var type = twin.Tags.GetValueOrDefault<string>(nameof(BaseRegistration.DeviceType), null);
            if (string.IsNullOrEmpty(type) && twin.Properties.Reported != null) {
                type = twin.Properties.Reported.GetValueOrDefault<string>(TwinProperty.kType, null);
            }
            switch (type?.ToLowerInvariant() ?? "") {
                case "endpoint":
                    return twin.ToEndpointRegistration(onlyServerState);
                case "application":
                    return twin.ToApplicationRegistration();
                case "supervisor":
                    return twin.ToSupervisorRegistration(onlyServerState);
            }
            return null;
        }
    }
}
