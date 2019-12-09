// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Registry.Models {
    using Microsoft.Azure.IIoT.OpcUa.Core.Models;
    using Microsoft.Azure.IIoT.Hub;
    using Microsoft.Azure.IIoT.Hub.Models;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using System;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Twin (endpoint) registration extensions
    /// </summary>
    public static class EndpointRegistrationEx {

        /// <summary>
        /// Logical comparison of endpoint registrations
        /// </summary>
        public static IEqualityComparer<EndpointRegistration> Logical =>
            new LogicalEquality();

        /// <summary>
        /// Create device twin
        /// </summary>
        /// <param name="registration"></param>
        /// <returns></returns>
        public static DeviceTwinModel ToDeviceTwin(this EndpointRegistration registration) {
            return Patch(null, registration);
        }

        /// <summary>
        /// Create patch twin model to upload
        /// </summary>
        /// <param name="existing"></param>
        /// <param name="update"></param>
        public static DeviceTwinModel Patch(this EndpointRegistration existing,
            EndpointRegistration update) {

            var twin = BaseRegistrationEx.PatchBase(existing, update);

            // Tags

            if (update?.EndpointRegistrationUrl != null &&
                update.EndpointRegistrationUrl != existing?.EndpointRegistrationUrl) {
                twin.Tags.Add(nameof(EndpointRegistration.EndpointUrlLC),
                    update.EndpointUrlLC);
                twin.Tags.Add(nameof(EndpointRegistration.EndpointRegistrationUrl),
                    update.EndpointRegistrationUrl);
            }

            if (update?.SecurityLevel != existing?.SecurityLevel) {
                twin.Tags.Add(nameof(EndpointRegistration.SecurityLevel), update?.SecurityLevel == null ?
                    null : JToken.FromObject(update?.SecurityLevel));
            }

            if (update?.Activated != null &&
                update.Activated != existing?.Activated) {
                twin.Tags.Add(nameof(EndpointRegistration.Activated), update?.Activated);
            }

            var methodEqual = update?.AuthenticationMethods.DecodeAsList().SetEqualsSafe(
                existing?.AuthenticationMethods?.DecodeAsList(), JToken.DeepEquals);
            if (!(methodEqual ?? true)) {
                twin.Tags.Add(nameof(EndpointRegistration.AuthenticationMethods),
                    update?.AuthenticationMethods == null ?
                    null : JToken.FromObject(update.AuthenticationMethods,
                        new JsonSerializer { NullValueHandling = NullValueHandling.Ignore }));
            }

            // Endpoint Property

            if (update?.EndpointUrl != null &&
                update.EndpointUrl != existing?.EndpointUrl) {
                twin.Properties.Desired.Add(nameof(EndpointRegistration.EndpointUrl),
                    update.EndpointUrl);
            }

            var urlsEqual = update?.AlternativeUrls.DecodeAsList().ToHashSetSafe().SetEqualsSafe(
                existing?.AlternativeUrls?.DecodeAsList());
            if (!(urlsEqual ?? true)) {
                twin.Properties.Desired.Add(nameof(EndpointRegistration.AlternativeUrls),
                    update?.AlternativeUrls == null ?
                    null : JToken.FromObject(update.AlternativeUrls));
            }

            if (update?.SecurityMode != null &&
                update.SecurityMode != existing?.SecurityMode) {
                twin.Properties.Desired.Add(nameof(EndpointRegistration.SecurityMode),
                    JToken.FromObject(update.SecurityMode));
            }

            if (update?.SecurityPolicy != null &&
                update?.SecurityPolicy != existing?.SecurityPolicy) {
                twin.Properties.Desired.Add(nameof(EndpointRegistration.SecurityPolicy),
                    update.SecurityPolicy);
            }

            var certEqual = update?.Certificate.DecodeAsByteArray().SequenceEqualsSafe(
                existing?.Certificate.DecodeAsByteArray());
            if (update?.Certificate != null && !(certEqual ?? true)) {
                twin.Properties.Desired.Add(nameof(EndpointRegistration.Certificate),
                    update?.Certificate == null ?
                    null : JToken.FromObject(update.Certificate));
            }

            // Recalculate identity

            var reportedEndpointUrl = existing?.EndpointRegistrationUrl;
            if (update?.EndpointRegistrationUrl != null) {
                reportedEndpointUrl = update.EndpointRegistrationUrl;
            }
            if (reportedEndpointUrl == null) {
                throw new ArgumentException(nameof(EndpointRegistration.EndpointUrl));
            }
            var applicationId = existing?.ApplicationId;
            if (update?.ApplicationId != null) {
                applicationId = update.ApplicationId;
            }
            if (applicationId == null) {
                throw new ArgumentException(nameof(EndpointRegistration.ApplicationId));
            }
            var securityMode = existing?.SecurityMode;
            if (update?.SecurityMode != null) {
                securityMode = update.SecurityMode;
            }
            var securityPolicy = existing?.SecurityPolicy;
            if (update?.SecurityPolicy != null) {
                securityPolicy = update.SecurityPolicy;
            }

            twin.Id = EndpointInfoModelEx.CreateEndpointId(
                applicationId, reportedEndpointUrl, securityMode, securityPolicy);

            if (existing?.DeviceId != twin.Id) {
                twin.Etag = null; // Force creation of new identity
            }
            return twin;
        }

        /// <summary>
        /// Decode tags and property into registration object
        /// </summary>
        /// <param name="twin"></param>
        /// <param name="properties"></param>
        /// <returns></returns>
        public static EndpointRegistration ToEndpointRegistration(this DeviceTwinModel twin,
            Dictionary<string, JToken> properties) {
            if (twin == null) {
                return null;
            }

            var tags = twin.Tags ?? new Dictionary<string, JToken>();
            var connected = twin.IsConnected();

            var registration = new EndpointRegistration {
                // Device

                DeviceId = twin.Id,
                Etag = twin.Etag,

                // Tags
                IsDisabled =
                    tags.GetValueOrDefault(nameof(EndpointRegistration.IsDisabled), twin.IsDisabled()),
                NotSeenSince =
                    tags.GetValueOrDefault<DateTime>(nameof(EndpointRegistration.NotSeenSince), null),

                Thumbprint =
                    tags.GetValueOrDefault<string>(nameof(EndpointRegistration.Thumbprint), null),
                SupervisorId =
                    tags.GetValueOrDefault<string>(nameof(EndpointRegistration.SupervisorId), null),
                Activated =
                    tags.GetValueOrDefault<bool>(nameof(EndpointRegistration.Activated), null),
                ApplicationId =
                    tags.GetValueOrDefault<string>(nameof(EndpointRegistration.ApplicationId), null),
                SecurityLevel =
                    tags.GetValueOrDefault<int>(nameof(EndpointRegistration.SecurityLevel), null),
                AuthenticationMethods =
                    tags.GetValueOrDefault<Dictionary<string, JToken>>(nameof(EndpointRegistration.AuthenticationMethods), null),
                EndpointRegistrationUrl =
                    tags.GetValueOrDefault<string>(nameof(EndpointRegistration.EndpointRegistrationUrl), null),

                // Properties

                Connected = connected ??
                    properties.GetValueOrDefault(TwinProperty.Connected, false),
                Type =
                    properties.GetValueOrDefault<string>(TwinProperty.Type, null),
                State =
                    properties.GetValueOrDefault(nameof(EndpointRegistration.State), EndpointConnectivityState.Connecting),
                SiteId =
                    properties.GetValueOrDefault(TwinProperty.SiteId,
                        tags.GetValueOrDefault<string>(nameof(EndpointRegistration.SiteId), null)),
                EndpointUrl =
                    properties.GetValueOrDefault<string>(nameof(EndpointRegistration.EndpointUrl), null),
                AlternativeUrls =
                    properties.GetValueOrDefault<Dictionary<string, string>>(nameof(EndpointRegistration.AlternativeUrls), null),
                SecurityMode =
                    properties.GetValueOrDefault<SecurityMode>(nameof(EndpointRegistration.SecurityMode), null),
                SecurityPolicy =
                    properties.GetValueOrDefault<string>(nameof(EndpointRegistration.SecurityPolicy), null),
                Certificate =
                    properties.GetValueOrDefault<Dictionary<string, string>>(nameof(EndpointRegistration.Certificate), null),
            };
            return registration;
        }

        /// <summary>
        /// Make sure to get the registration information from the right place.
        /// Reported (truth) properties take precedence over desired. However,
        /// if there is nothing reported, it means the endpoint is not currently
        /// serviced, thus we use desired as if they are attributes of the
        /// endpoint.
        /// </summary>
        /// <param name="twin"></param>
        /// <param name="onlyServerState">Only desired endpoint should be returned
        /// this means that you will look at stale information.</param>
        /// <returns></returns>
        public static EndpointRegistration ToEndpointRegistration(this DeviceTwinModel twin,
            bool onlyServerState) {

            if (twin == null) {
                return null;
            }
            if (twin.Tags == null) {
                twin.Tags = new Dictionary<string, JToken>();
            }

            var consolidated =
                ToEndpointRegistration(twin, twin.GetConsolidatedProperties());
            var desired = (twin.Properties?.Desired == null) ? null :
                ToEndpointRegistration(twin, twin.Properties.Desired);

            if (!onlyServerState) {
                consolidated._isInSync = consolidated.IsInSyncWith(desired);
                return consolidated;
            }
            if (desired != null) {
                desired._isInSync = desired.IsInSyncWith(consolidated);
            }
            return desired;
        }

        /// <summary>
        /// Convert to service model
        /// </summary>
        /// <param name="registration"></param>
        /// <returns></returns>
        public static EndpointInfoModel ToServiceModel(this EndpointRegistration registration) {
            return new EndpointInfoModel {
                ApplicationId = registration.ApplicationId,
                Registration = new EndpointRegistrationModel {
                    Id = registration.DeviceId,
                    SiteId = string.IsNullOrEmpty(registration.SiteId) ?
                        null : registration.SiteId,
                    SupervisorId = string.IsNullOrEmpty(registration.SupervisorId) ?
                        null : registration.SupervisorId,
                    AuthenticationMethods = registration.AuthenticationMethods?.DecodeAsList(j =>
                        j.ToObject<AuthenticationMethodModel>()),
                    SecurityLevel = registration.SecurityLevel,
                    EndpointUrl = string.IsNullOrEmpty(registration.EndpointRegistrationUrl) ?
                        (string.IsNullOrEmpty(registration.EndpointUrl) ?
                            registration.EndpointUrlLC : registration.EndpointUrl) : registration.EndpointRegistrationUrl,
                    Endpoint = new EndpointModel {
                        Url = string.IsNullOrEmpty(registration.EndpointUrl) ?
                            registration.EndpointUrlLC : registration.EndpointUrl,
                        AlternativeUrls = registration.AlternativeUrls?.DecodeAsList().ToHashSetSafe(),
                        SecurityMode = registration.SecurityMode == SecurityMode.Best ?
                            null : registration.SecurityMode,
                        SecurityPolicy = string.IsNullOrEmpty(registration.SecurityPolicy) ?
                            null : registration.SecurityPolicy,
                        Certificate = registration.Certificate.DecodeAsByteArray()
                    }
                },
                ActivationState = registration.ActivationState,
                NotSeenSince = registration.NotSeenSince,
                EndpointState = registration.ActivationState == EndpointActivationState.ActivatedAndConnected ?
                    registration.State : (EndpointConnectivityState?)null,
                OutOfSync = registration.Connected && !registration._isInSync ? true : (bool?)null
            };
        }

        /// <summary>
        /// Returns true if this registration matches the server endpoint
        /// model provided.
        /// </summary>
        /// <param name="model"></param>
        /// <param name="registration"></param>
        /// <returns></returns>
        public static bool Matches(this EndpointRegistration registration, EndpointInfoModel model) {
            return model != null &&
                registration.Matches(model.Registration?.Endpoint) &&
                registration.NotSeenSince == model.NotSeenSince &&
                registration.ApplicationId == model.ApplicationId &&
                (registration.ActivationState ?? EndpointActivationState.Deactivated) ==
                    (model.ActivationState ?? EndpointActivationState.Deactivated);
        }

        /// <summary>
        /// Returns true if this registration matches the endpoint
        /// model provided.
        /// </summary>
        /// <param name="endpoint"></param>
        /// <param name="registration"></param>
        /// <returns></returns>
        public static bool Matches(this EndpointRegistration registration, EndpointModel endpoint) {
            return endpoint != null &&
                registration.EndpointUrl == endpoint.Url &&
                registration.AlternativeUrls.DecodeAsList().ToHashSetSafe().SetEqualsSafe(
                    endpoint.AlternativeUrls) &&
                registration.SecurityMode == (endpoint.SecurityMode ?? SecurityMode.Best) &&
                registration.SecurityPolicy == endpoint.SecurityPolicy &&
                endpoint.Certificate.SequenceEqualsSafe(
                    registration.Certificate.DecodeAsByteArray());
        }


        /// <summary>
        /// Decode tags and property into registration object
        /// </summary>
        /// <param name="model"></param>
        /// <param name="disabled"></param>
        /// <returns></returns>
        public static EndpointRegistration ToEndpointRegistration(this EndpointInfoModel model,
            bool? disabled = null) {
            if (model == null) {
                throw new ArgumentNullException(nameof(model));
            }
            return new EndpointRegistration {
                IsDisabled = disabled,
                NotSeenSince = model.NotSeenSince,
                ApplicationId = model.ApplicationId,
                SiteId = model.Registration?.SiteId,
                SupervisorId = model.Registration?.SupervisorId,
                SecurityLevel = model.Registration?.SecurityLevel,
                EndpointRegistrationUrl = model.Registration?.EndpointUrl ??
                    model.Registration?.Endpoint.Url,
                EndpointUrl = model.Registration?.Endpoint.Url,
                AlternativeUrls = model.Registration?.Endpoint.AlternativeUrls?.ToList()?
                    .EncodeAsDictionary(),
                AuthenticationMethods = model.Registration?.AuthenticationMethods?
                    .EncodeAsDictionary(JToken.FromObject),
                SecurityMode = model.Registration?.Endpoint.SecurityMode ??
                    SecurityMode.Best,
                SecurityPolicy = model.Registration?.Endpoint.SecurityPolicy,
                Certificate = model.Registration?.Endpoint?
                    .Certificate.EncodeAsDictionary(),
                Thumbprint = model.Registration?.Endpoint?
                    .Certificate?.ToSha1Hash(),
                ActivationState = model.ActivationState
            };
        }

        /// <summary>
        /// Flag endpoint as synchronized - i.e. it matches the other.
        /// </summary>
        /// <param name="registration"></param>
        /// <param name="other"></param>
        internal static bool IsInSyncWith(this EndpointRegistration registration,
            EndpointRegistration other) {
            return
                other != null &&
                registration.EndpointUrl == other.EndpointUrl &&
                registration.AlternativeUrls.DecodeAsList().ToHashSetSafe().SetEqualsSafe(
                    other.AlternativeUrls.DecodeAsList()) &&
                registration.SecurityPolicy == other.SecurityPolicy &&
                registration.SecurityMode == other.SecurityMode &&
                registration.Certificate.DecodeAsByteArray().SequenceEqualsSafe(
                    other.Certificate.DecodeAsByteArray());
        }

        /// <summary>
        /// Compares for logical equality
        /// </summary>
        private class LogicalEquality : IEqualityComparer<EndpointRegistration> {

            /// <inheritdoc />
            public bool Equals(EndpointRegistration x, EndpointRegistration y) {
                return
                    x.EndpointUrlLC == y.EndpointUrlLC &&
                    x.ApplicationId == y.ApplicationId &&
                    x.SecurityPolicy == y.SecurityPolicy &&
                    x.SecurityMode == y.SecurityMode;
            }

            /// <inheritdoc />
            public int GetHashCode(EndpointRegistration obj) {
                var hashCode = 1200389859;
                hashCode = (hashCode * -1521134295) +
                    EqualityComparer<string>.Default.GetHashCode(obj.EndpointUrlLC);
                hashCode = (hashCode * -1521134295) +
                    EqualityComparer<string>.Default.GetHashCode(obj.ApplicationId);
                hashCode = (hashCode * -1521134295) +
                    EqualityComparer<SecurityMode?>.Default.GetHashCode(obj.SecurityMode);
                hashCode = (hashCode * -1521134295) +
                    EqualityComparer<string>.Default.GetHashCode(obj.SecurityPolicy);
                return hashCode;
            }
        }
    }
}
