// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IoTSolutions.OpcTwin.Services.Cloud {
    using Microsoft.Azure.IoTSolutions.OpcTwin.Services.External;
    using Microsoft.Azure.IoTSolutions.OpcTwin.Services.External.Models;
    using Microsoft.Azure.IoTSolutions.OpcTwin.Services.Models;
    using Newtonsoft.Json.Linq;
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Twin (application) registration persisted and comparable
    /// </summary>
    public sealed class OpcUaApplicationRegistration : OpcUaTwinRegistration {

        /// <summary>
        /// Device id for registration
        /// </summary>
        public override string DeviceId {
            get {
                if (_deviceId == null) {
                    _deviceId = ApplicationId;
                }
                return _deviceId;
            }
            set => _deviceId = value;
        }

        public override string DeviceType => "Application";

        #region Twin Tags

        /// <summary>
        /// Application id (hash of supervisor and lowercase application uri)
        /// </summary>
        public override string ApplicationId {
            get {
                if (_applicationId == null) {
                    if (SupervisorId == null && ApplicationUri == null) {
                        return null;
                    }
                    _applicationId = ApplicationModelEx.CreateApplicationId(
                        SupervisorId, ApplicationUri);
                }
                return _applicationId;
            }
            set => _applicationId = value;
        }

        /// <summary>
        /// Application uri
        /// </summary>
        public string ApplicationUri { get; set; }

        /// <summary>
        /// Lower case application url
        /// </summary>
        public string ApplicationUriLC { get; set; }

        /// <summary>
        /// Application name
        /// </summary>
        public string ApplicationName { get; set; }

        /// <summary>
        /// Product uri
        /// </summary>
        public string ProductUri { get; set; }

        /// <summary>
        /// Application type
        /// </summary>
        public ApplicationType? ApplicationType { get; set; }

        /// <summary>
        /// Returns the public certificate presented by the application
        /// </summary>
        public Dictionary<string, string> Certificate { get; set; }

        /// <summary>
        /// Returns discovery urls of the application
        /// </summary>
        public Dictionary<string, string> DiscoveryUrls { get; set; }

        /// <summary>
        /// Certificate hash
        /// </summary>
        public string Thumbprint { get; set; }

        /// <summary>
        /// Capabilities
        /// </summary>
        public string Capabilities { get; set; }

        #endregion Twin Tags

        /// <summary>
        /// Patch this registration and create patch twin model to upload
        /// </summary>
        /// <param name="application"></param>
        public DeviceTwinModel Patch(ApplicationInfoModel application) {
            if (application == null) {
                throw new ArgumentNullException(nameof(application));
            }

            var twin = new DeviceTwinModel {
                Etag = Etag,
                Tags = new Dictionary<string, JToken>(),
                Properties = new TwinPropertiesModel() {
                    Desired = new Dictionary<string, JToken>()
                }
            };

            // Tags + Endpoint Property

            if (IsEnabled != (application.Certificate != null)) {
                IsEnabled = (application.Certificate != null);
                twin.Tags.Add(nameof(IsEnabled), IsEnabled);
            }

            // Tags

            var updateApplicationId = false;

            if (ApplicationId != application.ApplicationId) {
                ApplicationId = application.ApplicationId;
                updateApplicationId = true;
            }

            if (ApplicationType != application.ApplicationType) {
                ApplicationType = application.ApplicationType;
                twin.Tags.Add(nameof(ApplicationType), JToken.FromObject(ApplicationType));

                twin.Tags.Add(nameof(Models.ApplicationType.Server), 
                    ApplicationType == Models.ApplicationType.Server ||
                    ApplicationType == Models.ApplicationType.ClientAndServer);
                twin.Tags.Add(nameof(Models.ApplicationType.Client),
                    ApplicationType == Models.ApplicationType.Client ||
                    ApplicationType == Models.ApplicationType.ClientAndServer);
            }

            if (SupervisorId != application.SupervisorId) {
                SupervisorId = application.SupervisorId;
                twin.Tags.Add(nameof(SupervisorId), SupervisorId);
                updateApplicationId = true;
            }

            if (ApplicationUri != application.ApplicationUri) {
                ApplicationUri = application.ApplicationUri;
                twin.Tags.Add(nameof(ApplicationUri), ApplicationUri);
                ApplicationUriLC = application.ApplicationUri?.ToLowerInvariant();
                twin.Tags.Add(nameof(ApplicationUriLC), ApplicationUriLC);
                updateApplicationId = true;
            }

            if (ApplicationName != application.ApplicationName) {
                ApplicationName = application.ApplicationName;
                twin.Tags.Add(nameof(ApplicationName), ApplicationName);
            }

            if (ProductUri != application.ProductUri) {
                ProductUri = application.ProductUri;
                twin.Tags.Add(nameof(ProductUri), ProductUri);
            }

            if (!Certificate.DecodeAsByteArray().SequenceEqualsSafe(application.Certificate)) {
                Certificate = application.Certificate.EncodeAsDictionary();
                twin.Tags.Add(nameof(Certificate), JToken.FromObject(Certificate));
                Thumbprint = application.Certificate?.ToSha1Hash();
                twin.Tags.Add(nameof(Thumbprint), Thumbprint);
            }

            if (!DiscoveryUrls.DecodeAsList().SequenceEqualsSafe(application.DiscoveryUrls)) {
                DiscoveryUrls = application.DiscoveryUrls.EncodeAsDictionary();
                twin.Tags.Add(nameof(DiscoveryUrls), JToken.FromObject(DiscoveryUrls));
            }

            if (Capabilities != application.Capabilities.EncodeAsString()) {
                Capabilities = application.Capabilities.EncodeAsString();
                twin.Tags.Add(nameof(Capabilities), Capabilities);
            }

            if (updateApplicationId) {
                twin.Tags.Add(nameof(ApplicationId), ApplicationId);
            }

            twin.Tags.Add(nameof(DeviceType), DeviceType);
            twin.Id = DeviceId;
            return twin;
        }

        /// <summary>
        /// Decode tags and property into registration object
        /// </summary>
        /// <param name="id"></param>
        /// <param name="etag"></param>
        /// <param name="tags"></param>
        /// <returns></returns>
        public static OpcUaApplicationRegistration FromTwin(string id, string etag,
            Dictionary<string, JToken> tags) {
            return new OpcUaApplicationRegistration {
                // Device

                DeviceId = id,
                Etag = etag,

                // Tags

                IsEnabled =
                    tags.Get(nameof(IsEnabled), false),
                ApplicationName =
                    tags.Get<string>(nameof(ApplicationName), null),
                ApplicationUri =
                    tags.Get<string>(nameof(ApplicationUri), null),
                ProductUri =
                    tags.Get<string>(nameof(ProductUri), null),
                Thumbprint =
                    tags.Get<string>(nameof(Thumbprint), null),
                SupervisorId =
                    tags.Get<string>(nameof(SupervisorId), null),
                ApplicationUriLC =
                    tags.Get<string>(nameof(ApplicationUriLC), null),
                ApplicationId =
                    tags.Get<string>(nameof(ApplicationId), null),
                ApplicationType =
                    tags.Get<ApplicationType>(nameof(ApplicationType), null),
                Capabilities =
                    tags.Get<string>(nameof(Capabilities), null),
                DiscoveryUrls =
                    tags.Get<Dictionary<string, string>>(nameof(DiscoveryUrls), null),
                Certificate =
                    tags.Get<Dictionary<string, string>>(nameof(Certificate), null),
            };
        }

        /// <summary>
        /// Make sure to get the registration information from the right place.
        /// Reported (truth) properties take precedence over desired.
        /// </summary>
        /// <param name="twin"></param>
        /// <returns></returns>
        public static OpcUaApplicationRegistration FromTwin(DeviceTwinModel twin) {
            if (twin == null) {
                return null;
            }
            if (twin.Tags == null) {
                twin.Tags = new Dictionary<string, JToken>();
            }
            return FromTwin(twin.Id, twin.Etag, twin.Tags);
        }

        /// <summary>
        /// Convert to service model
        /// </summary>
        /// <returns></returns>
        public ApplicationInfoModel ToServiceModel() {
            return new ApplicationInfoModel {
                ApplicationId = ApplicationId,
                ApplicationName = ApplicationName,
                ApplicationType = ApplicationType ?? Models.ApplicationType.Server,
                ApplicationUri = string.IsNullOrEmpty(ApplicationUri) ?
                    ApplicationUriLC : ApplicationUri,
                ProductUri = ProductUri,
                Certificate = Certificate?.DecodeAsByteArray(),
                DiscoveryUrls = DiscoveryUrls?.DecodeAsList(),
                SupervisorId = string.IsNullOrEmpty(SupervisorId) ?
                    null : SupervisorId,
                Capabilities = Capabilities?.DecodeAsList()
            };
        }

        /// <summary>
        /// Decode tags and property into registration object
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public static OpcUaApplicationRegistration FromServiceModel(ApplicationInfoModel model) {
            return new OpcUaApplicationRegistration {
                ApplicationName = model.ApplicationName,
                ApplicationType = model.ApplicationType,
                ApplicationUri = model.ApplicationUri,
                ProductUri = model.ProductUri,
                ApplicationUriLC = model.ApplicationUri?.ToLowerInvariant(),
                Capabilities = model.Capabilities?.EncodeAsString(),
                Certificate = model.Certificate?.EncodeAsDictionary(),
                DiscoveryUrls = model.DiscoveryUrls?.EncodeAsDictionary(),
                Thumbprint = model.Certificate?.ToSha1Hash(),
                SupervisorId = model.SupervisorId,
                ApplicationId = model.ApplicationId,
            };
        }

        /// <summary>
        /// Returns true if this registration matches the server
        /// model provided.
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public bool Matches(ApplicationInfoModel model) {
            return model != null &&
                ApplicationId == model.ApplicationId &&
                ApplicationType == model.ApplicationType &&
                ApplicationUri == model.ApplicationUri &&
                SupervisorId == model.SupervisorId &&
                Capabilities == model.Capabilities?.EncodeAsString() &&
                DiscoveryUrls.DecodeAsList().SequenceEqualsSafe(
                    model.DiscoveryUrls) &&
                Certificate.DecodeAsByteArray().SequenceEqualsSafe(
                    model.Certificate);
        }

        /// <summary>
        /// Stringify
        /// </summary>
        /// <returns></returns>
        public override string ToString() {
            return $"{ApplicationUriLC}-{ApplicationType}-{SupervisorId}";
        }

        /// <summary>
        /// Pure equality
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public override bool Equals(object obj) {
            var registration = obj as OpcUaApplicationRegistration;
            return registration != null &&
                ApplicationId == registration.ApplicationId &&
                IsEnabled == registration.IsEnabled &&
                ApplicationType == registration.ApplicationType &&
                ApplicationUriLC == registration.ApplicationUriLC &&
                ProductUri == registration.ProductUri &&
                ApplicationName == registration.ApplicationName &&
                SupervisorId == registration.SupervisorId &&
                Capabilities == registration.Capabilities &&
                DiscoveryUrls.DecodeAsList().SequenceEqualsSafe(
                    registration.DiscoveryUrls.DecodeAsList()) &&
                Certificate.DecodeAsByteArray().SequenceEqualsSafe(
                    registration.Certificate.DecodeAsByteArray());
        }

        public static bool operator ==(OpcUaApplicationRegistration r1,
            OpcUaApplicationRegistration r2) =>
            EqualityComparer<OpcUaApplicationRegistration>.Default.Equals(r1, r2);
        public static bool operator !=(OpcUaApplicationRegistration r1,
            OpcUaApplicationRegistration r2) =>
            !(r1 == r2);

        /// <summary>
        /// Hash code
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode() {
            var hashCode = 1200389859;
            hashCode = hashCode * -1521134295 +
                IsEnabled.GetHashCode();
            hashCode = hashCode * -1521134295 +
                EqualityComparer<ApplicationType?>.Default.GetHashCode(ApplicationType);
            hashCode = hashCode * -1521134295 +
                EqualityComparer<string>.Default.GetHashCode(ProductUri);
            hashCode = hashCode * -1521134295 +
                EqualityComparer<string>.Default.GetHashCode(ApplicationName);
            hashCode = hashCode * -1521134295 +
                EqualityComparer<string>.Default.GetHashCode(ApplicationId);
            hashCode = hashCode * -1521134295 +
                EqualityComparer<string>.Default.GetHashCode(Capabilities);
            hashCode = hashCode * -1521134295 +
                EqualityComparer<string>.Default.GetHashCode(Thumbprint);
            return hashCode;
        }

        private string _applicationId;
    }
}
