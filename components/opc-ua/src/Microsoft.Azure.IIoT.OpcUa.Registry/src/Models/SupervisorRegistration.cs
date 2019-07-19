// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Registry.Models {
    using Microsoft.Azure.IIoT.Hub;
    using Newtonsoft.Json;
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Endpoint persisted in twin and comparable
    /// </summary>
    [Serializable]
    public sealed class SupervisorRegistration : BaseRegistration {

        /// <inheritdoc/>
        public override string DeviceType => "Supervisor";

        /// <inheritdoc/>
        public override string ApplicationId => null;

        /// <summary>
        /// Supervisor that owns the twin.
        /// </summary>
        [JsonProperty(PropertyName = "id")]
        public override string SupervisorId =>
            SupervisorModelEx.CreateSupervisorId(DeviceId, ModuleId);

        /// <summary>
        /// Discovery state callback uris
        /// </summary>
        public Dictionary<string, CallbackModel> DiscoveryCallbacks { get; set; }

        /// <summary>
        /// Activation filter security mode
        /// </summary>
        public SecurityMode? SecurityModeFilter { get; set; }

        /// <summary>
        /// Activation filter security policies
        /// </summary>
        public Dictionary<string, string> SecurityPoliciesFilter { get; set; }

        /// <summary>
        /// Activation filter trust lists
        /// </summary>
        public Dictionary<string, string> TrustListsFilter { get; set; }

        /// <summary>
        /// Discovery state
        /// </summary>
        public DiscoveryMode Discovery { get; set; }

        /// <summary>
        /// Current log level
        /// </summary>
        public SupervisorLogLevel? LogLevel { get; set; }

        /// <summary>
        /// Address ranges to scan (null == all wired)
        /// </summary>
        public string AddressRangesToScan { get; set; }

        /// <summary>
        /// Network probe timeout.
        /// </summary>
        public TimeSpan? NetworkProbeTimeout { get; set; }

        /// <summary>
        /// Max network probes that should ever run.
        /// </summary>
        public int? MaxNetworkProbes { get; set; }

        /// <summary>
        /// Port ranges to scan (null == all unassigned)
        /// </summary>
        public string PortRangesToScan { get; set; }

        /// <summary>
        /// Port probe timeout
        /// </summary>
        public TimeSpan? PortProbeTimeout { get; set; }

        /// <summary>
        /// Max port probes that should ever run.
        /// </summary>
        public int? MaxPortProbes { get; set; }

        /// <summary>
        /// Probes that must always be there as percent of max.
        /// </summary>
        public int? MinPortProbesPercent { get; set; }

        /// <summary>
        /// Delay time between discovery sweeps
        /// </summary>
        public TimeSpan? IdleTimeBetweenScans { get; set; }

        /// <summary>
        /// predefined discovery urls for supervisor
        /// </summary>
        public Dictionary<string, string> DiscoveryUrls { get; set; }

        /// <summary>
        /// Locales to filter discoveries against
        /// </summary>
        public Dictionary<string, string> Locales { get; set; }

        /// <summary>
        /// Create registration - for testing purposes
        /// </summary>
        /// <param name="deviceId"></param>
        /// <param name="moduleId"></param>
        public SupervisorRegistration(string deviceId = null,
            string moduleId = null) {
            DeviceId = deviceId;
            ModuleId = moduleId;
        }

        /// <inheritdoc/>
        public override bool Equals(object obj) {
            var registration = obj as SupervisorRegistration;
            return base.Equals(registration) &&
                ModuleId == registration.ModuleId &&
                Discovery == registration.Discovery &&
                LogLevel == registration.LogLevel &&
                AddressRangesToScan == registration.AddressRangesToScan &&
                EqualityComparer<TimeSpan?>.Default.Equals(
                    NetworkProbeTimeout, registration.NetworkProbeTimeout) &&
                EqualityComparer<int?>.Default.Equals(
                    MaxNetworkProbes, registration.MaxNetworkProbes) &&
                PortRangesToScan == registration.PortRangesToScan &&
                EqualityComparer<TimeSpan?>.Default.Equals(
                    PortProbeTimeout, registration.PortProbeTimeout) &&
                EqualityComparer<int?>.Default.Equals(
                    MaxPortProbes, registration.MaxPortProbes) &&
                EqualityComparer<int?>.Default.Equals(
                    MinPortProbesPercent, registration.MinPortProbesPercent) &&
                EqualityComparer<TimeSpan?>.Default.Equals(
                    IdleTimeBetweenScans, registration.IdleTimeBetweenScans) &&
                EqualityComparer<SecurityMode?>.Default.Equals(
                    SecurityModeFilter, registration.SecurityModeFilter) &&
                TrustListsFilter.DecodeAsList().SequenceEqualsSafe(
                    registration.TrustListsFilter.DecodeAsList()) &&
                SecurityPoliciesFilter.DecodeAsList().SequenceEqualsSafe(
                    registration.SecurityPoliciesFilter.DecodeAsList()) &&
                DiscoveryUrls.DecodeAsList().SequenceEqualsSafe(
                    registration.DiscoveryUrls.DecodeAsList()) &&
                Locales.DecodeAsList().SequenceEqualsSafe(
                    registration.Locales.DecodeAsList()) &&
                DiscoveryCallbacks.DecodeAsList().SetEqualsSafe(
                    registration.DiscoveryCallbacks.DecodeAsList(),
                        (callback1, callback2) => callback1.IsSameAs(callback2));
        }

        /// <inheritdoc/>
        public static bool operator ==(SupervisorRegistration r1,
            SupervisorRegistration r2) => EqualityComparer<SupervisorRegistration>.Default.Equals(r1, r2);

        /// <inheritdoc/>
        public static bool operator !=(SupervisorRegistration r1,
            SupervisorRegistration r2) => !(r1 == r2);

        /// <inheritdoc/>
        public override int GetHashCode() {
            var hashCode = base.GetHashCode();
            hashCode = (hashCode * -1521134295) +
                EqualityComparer<string>.Default.GetHashCode(ModuleId);
            hashCode = (hashCode * -1521134295) +
                Discovery.GetHashCode();
            hashCode = (hashCode * -1521134295) +
                EqualityComparer<SupervisorLogLevel?>.Default.GetHashCode(LogLevel);
            hashCode = (hashCode * -1521134295) +
                EqualityComparer<string>.Default.GetHashCode(AddressRangesToScan);
            hashCode = (hashCode * -1521134295) +
                EqualityComparer<TimeSpan?>.Default.GetHashCode(NetworkProbeTimeout);
            hashCode = (hashCode * -1521134295) +
                EqualityComparer<int?>.Default.GetHashCode(MaxNetworkProbes);
            hashCode = (hashCode * -1521134295) +
                EqualityComparer<string>.Default.GetHashCode(PortRangesToScan);
            hashCode = (hashCode * -1521134295) +
                EqualityComparer<TimeSpan?>.Default.GetHashCode(PortProbeTimeout);
            hashCode = (hashCode * -1521134295) +
                EqualityComparer<int?>.Default.GetHashCode(MaxPortProbes);
            hashCode = (hashCode * -1521134295) +
                EqualityComparer<int?>.Default.GetHashCode(MinPortProbesPercent);
            hashCode = (hashCode * -1521134295) +
                EqualityComparer<SecurityMode?>.Default.GetHashCode(SecurityModeFilter);
            hashCode = (hashCode * -1521134295) +
                EqualityComparer<TimeSpan?>.Default.GetHashCode(IdleTimeBetweenScans);
            return hashCode;
        }

        internal bool IsInSync() {
            return _isInSync;
        }

        internal bool IsConnected() {
            return Connected;
        }

        internal bool _isInSync;
    }
}
