// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IoTSolutions.OpcTwin.Services.Cloud {
    using Microsoft.Azure.IoTSolutions.OpcTwin.Services.External.Models;
    using Microsoft.Azure.IoTSolutions.OpcTwin.Services.Models;
    using Newtonsoft.Json.Linq;
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Endpoint persisted in twin and comparable
    /// </summary>
    public class OpcUaSupervisorRegistration {

        /// <summary>
        /// Device id for registration
        /// </summary>
        public string DeviceId { get; set; }

        #region Twin Tags

        /// <summary>
        /// Domain
        /// </summary>
        public string Domain { get; set; }

        #endregion Twin Tags

        #region Twin Properties

        /// <summary>
        /// Discovering state
        /// </summary>
        public bool Discovering { get; set; }

        /// <summary>
        /// Type
        /// </summary>
        public string Type { get; set; }

        #endregion Twin Properties

        /// <summary>
        /// Patch this registration and create patch twin model to upload
        /// </summary>
        /// <param name="model"></param>
        public TwinModel Patch(SupervisorModel model) {
            if (model == null) {
                throw new ArgumentNullException(nameof(model));
            }

            var twin = new TwinModel {
                Id = model.Id,
                Tags = new Dictionary<string, JToken>(),
                Properties = new TwinPropertiesModel {
                    Desired = new Dictionary<string, JToken> ()
                }
            };

            // Tags

            if (Domain != model.Domain?.ToLowerInvariant()) {
                Domain = model.Domain?.ToLowerInvariant();
                twin.Tags.Add(nameof(Domain), Domain);
            }

            // Settings

            if (Discovering != (model.Discovering ?? false)) {
                Discovering = (model.Discovering ?? false);
                twin.Properties.Desired.Add(nameof(Discovering), Discovering);
            }

            return twin;
        }

        /// <summary>
        /// Decode tags and property into registration object
        /// </summary>
        /// <param name="tags"></param>
        /// <param name="properties"></param>
        /// <returns></returns>
        public static OpcUaSupervisorRegistration FromTwin(string id,
            Dictionary<string, JToken> tags, Dictionary<string, JToken> properties) {
            return new OpcUaSupervisorRegistration {
                // Device

                DeviceId = id,

                // Tags

                Domain =
                    tags.Get<string>(nameof(Domain), null),

                // Properties

                Discovering =
                    properties.Get(nameof(Discovering), false),
                Type =
                    properties.Get<string>("type", null)
            };
        }

        /// <summary>
        /// Get supervisor registration from twin
        /// </summary>
        /// <param name="twin"></param>
        /// <returns></returns>
        public static OpcUaSupervisorRegistration FromTwin(TwinModel twin) =>
            FromTwin(twin, false, out var tmp);

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
        public static OpcUaSupervisorRegistration FromTwin(TwinModel twin,
            bool onlyServerState, out bool connected) {

            connected = false;
            if (twin == null) {
                return null;
            }
            if (twin.Tags == null) {
                twin.Tags = new Dictionary<string, JToken>();
            }

            OpcUaSupervisorRegistration reported = null, desired = null;
            if (twin.Properties?.Reported != null) {
                reported = FromTwin(twin.Id, twin.Tags, twin.Properties.Reported);
                if (reported != null) {
                    connected = reported.Type == "supervisor";
                }
            }

            if (twin.Properties?.Desired != null) {
                desired = FromTwin(twin.Id, twin.Tags, twin.Properties.Desired);
            }

            if (!onlyServerState && reported != null) {
                reported.MarkAsInSyncWith(desired);
                return reported;
            }

            if (desired != null) {
                desired.MarkAsInSyncWith(reported);
                return desired;
            }
            return null;
        }

        /// <summary>
        /// Convert to service model
        /// </summary>
        /// <returns></returns>
        public SupervisorModel ToServiceModel() {
            return new SupervisorModel {
                Discovering = Discovering ? true : (bool?)null,
                Domain = string.IsNullOrEmpty(Domain) ? null : Domain,
                Connected = IsConnected() ? true : (bool?)null,
                OutOfSync = IsConnected() && !_isInSync ? true : (bool?)null,
                Id = DeviceId
            };
        }

        /// <summary>
        /// Returns true if this registration matches the server endpoint
        /// model provided.
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public bool Matches(SupervisorModel model) {
            return model != null &&
                Discovering == (model.Discovering ?? false) &&
                model.Domain == (string.IsNullOrEmpty(Domain) ? null : Domain) &&
                DeviceId == model.Id;
        }

        /// <summary>
        /// Decode tags and property into registration object
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public static OpcUaSupervisorRegistration FromServiceModel(
            SupervisorModel model) {
            return new OpcUaSupervisorRegistration {
                Discovering = model.Discovering ?? false,
                Domain = model.Domain,
                DeviceId = model.Id
            };
        }

        /// <summary>
        /// Flag endpoint as synchronized - i.e. it matches the other.
        /// These test only the items that are exchanged between edge
        /// and server, i.e. reported and desired.  Other properties are
        /// reported out of band, or only apply to the server side, etc.
        /// </summary>
        /// <param name="other"></param>
        internal void MarkAsInSyncWith(OpcUaSupervisorRegistration other) {
            _isInSync =
                other != null &&
                Discovering == other.Discovering;
        }

        internal bool IsConnected() => Type == "supervisor";
        internal bool IsInSync() => _isInSync;
        private bool _isInSync;
    }
}
