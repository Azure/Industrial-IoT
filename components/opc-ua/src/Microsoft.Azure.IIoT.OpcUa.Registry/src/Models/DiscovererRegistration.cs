// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Registry.Models {
    using Microsoft.Azure.IIoT.OpcUa.Core.Models;
    using Microsoft.Azure.IIoT.Hub;
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Discoverer module registration
    /// </summary>
    [Serializable]
    public sealed class DiscovererRegistration : EntityRegistration {

        /// <inheritdoc/>
        public override string DeviceType => IdentityType.Discoverer;

        /// <summary>
        /// Device id for registration
        /// </summary>
        public string ModuleId { get; set; }

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
        public TraceLogLevel? LogLevel { get; set; }

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
        /// predefined discovery urls for discoverer
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
        public DiscovererRegistration(string deviceId = null,
            string moduleId = null) {
            DeviceId = deviceId;
            ModuleId = moduleId;
        }

        /// <inheritdoc/>
        public override bool Equals(object obj) {
            var registration = obj as DiscovererRegistration;
            return base.Equals(registration) &&
                ModuleId == registration.ModuleId &&
                LogLevel == registration.LogLevel &&
                Discovery == registration.Discovery &&
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
                    registration.Locales.DecodeAsList());
        }

        /// <inheritdoc/>
        public static bool operator ==(DiscovererRegistration r1,
            DiscovererRegistration r2) => EqualityComparer<DiscovererRegistration>.Default.Equals(r1, r2);

        /// <inheritdoc/>
        public static bool operator !=(DiscovererRegistration r1,
            DiscovererRegistration r2) => !(r1 == r2);

        /// <inheritdoc/>
        public override int GetHashCode() {
            var hashCode = base.GetHashCode();
            hashCode = (hashCode * -1521134295) +
                EqualityComparer<string>.Default.GetHashCode(ModuleId);
            hashCode = (hashCode * -1521134295) +
                EqualityComparer<TraceLogLevel?>.Default.GetHashCode(LogLevel);
            hashCode = (hashCode * -1521134295) +
                Discovery.GetHashCode();
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
