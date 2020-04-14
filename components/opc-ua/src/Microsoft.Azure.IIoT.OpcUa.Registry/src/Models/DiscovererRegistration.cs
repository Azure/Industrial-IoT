// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Registry.Models {
    using Microsoft.Azure.IIoT.OpcUa.Core.Models;
    using Microsoft.Azure.IIoT.Hub;
    using System;
    using System.Collections.Generic;
    using System.Runtime.Serialization;

    /// <summary>
    /// Discoverer module registration
    /// </summary>
    [DataContract]
    public sealed class DiscovererRegistration : EntityRegistration {

        /// <inheritdoc/>
        [DataMember]
        public override string DeviceType => IdentityType.Discoverer;

        /// <summary>
        /// Device id for registration
        /// </summary>
        [DataMember]
        public string ModuleId { get; set; }

        /// <summary>
        /// Activation filter security mode
        /// </summary>
        [DataMember]
        public SecurityMode? SecurityModeFilter { get; set; }

        /// <summary>
        /// Activation filter security policies
        /// </summary>
        [DataMember]
        public Dictionary<string, string> SecurityPoliciesFilter { get; set; }

        /// <summary>
        /// Activation filter trust lists
        /// </summary>
        [DataMember]
        public Dictionary<string, string> TrustListsFilter { get; set; }

        /// <summary>
        /// Discovery state
        /// </summary>
        [DataMember]
        public DiscoveryMode Discovery { get; set; }

        /// <summary>
        /// Current log level
        /// </summary>
        [DataMember]
        public TraceLogLevel? LogLevel { get; set; }

        /// <summary>
        /// Address ranges to scan (null == all wired)
        /// </summary>
        [DataMember]
        public string AddressRangesToScan { get; set; }

        /// <summary>
        /// Network probe timeout.
        /// </summary>
        [DataMember]
        public TimeSpan? NetworkProbeTimeout { get; set; }

        /// <summary>
        /// Max network probes that should ever run.
        /// </summary>
        [DataMember]
        public int? MaxNetworkProbes { get; set; }

        /// <summary>
        /// Port ranges to scan (null == all unassigned)
        /// </summary>
        [DataMember]
        public string PortRangesToScan { get; set; }

        /// <summary>
        /// Port probe timeout
        /// </summary>
        [DataMember]
        public TimeSpan? PortProbeTimeout { get; set; }

        /// <summary>
        /// Max port probes that should ever run.
        /// </summary>
        [DataMember]
        public int? MaxPortProbes { get; set; }

        /// <summary>
        /// Probes that must always be there as percent of max.
        /// </summary>
        [DataMember]
        public int? MinPortProbesPercent { get; set; }

        /// <summary>
        /// Delay time between discovery sweeps
        /// </summary>
        [DataMember]
        public TimeSpan? IdleTimeBetweenScans { get; set; }

        /// <summary>
        /// predefined discovery urls for discoverer
        /// </summary>
        [DataMember]
        public Dictionary<string, string> DiscoveryUrls { get; set; }

        /// <summary>
        /// Locales to filter discoveries against
        /// </summary>
        [DataMember]
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
            if (!(obj is DiscovererRegistration registration)) {
                return false;
            }
            if (!base.Equals(registration)) {
                return false;
            }
            if (ModuleId != registration.ModuleId) {
                return false;
            }
            if (LogLevel != registration.LogLevel) {
                return false;
            }
            if (Discovery != registration.Discovery) {
                return false;
            }
            if (AddressRangesToScan != registration.AddressRangesToScan) {
                return false;
            }
            if (!EqualityComparer<TimeSpan?>.Default.Equals(
                    NetworkProbeTimeout, registration.NetworkProbeTimeout)) {
                return false;
            }
            if (!EqualityComparer<int?>.Default.Equals(
                    MaxNetworkProbes, registration.MaxNetworkProbes)) {
                return false;
            }
            if (PortRangesToScan != registration.PortRangesToScan) {
                return false;
            }
            if (!EqualityComparer<TimeSpan?>.Default.Equals(
                    PortProbeTimeout, registration.PortProbeTimeout)) {
                return false;
            }
            if (!EqualityComparer<int?>.Default.Equals(
                    MaxPortProbes, registration.MaxPortProbes)) {
                return false;
            }
            if (!EqualityComparer<int?>.Default.Equals(
                    MinPortProbesPercent, registration.MinPortProbesPercent)) {
                return false;
            }
            if (!EqualityComparer<TimeSpan?>.Default.Equals(
                    IdleTimeBetweenScans, registration.IdleTimeBetweenScans)) {
                return false;
            }
            if (!EqualityComparer<SecurityMode?>.Default.Equals(
                    SecurityModeFilter, registration.SecurityModeFilter)) {
                return false;
            }
            if (!TrustListsFilter.DecodeAsList().SequenceEqualsSafe(
                    registration.TrustListsFilter.DecodeAsList())) {
                return false;
            }
            if (!SecurityPoliciesFilter.DecodeAsList().SequenceEqualsSafe(
                    registration.SecurityPoliciesFilter.DecodeAsList())) {
                return false;
            }
            if (!DiscoveryUrls.DecodeAsList().SequenceEqualsSafe(
                    registration.DiscoveryUrls.DecodeAsList())) {
                return false;
            }
            if (!Locales.DecodeAsList().SequenceEqualsSafe(
                    registration.Locales.DecodeAsList())) {
                return false;
            }
            return true;
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
        internal DiscovererRegistration _desired;
    }
}
