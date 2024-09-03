// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Models
{
    using System.Collections.Generic;
    using System.Runtime.Serialization;

    /// <summary>
    /// Server capabilities
    /// </summary>
    [DataContract]
    public sealed record class ServerCapabilitiesModel
    {
        /// <summary>
        /// Operation limits
        /// </summary>
        [DataMember(Name = "operationLimits", Order = 0)]
        public required OperationLimitsModel OperationLimits { get; set; }

        /// <summary>
        /// Supported locales
        /// </summary>
        [DataMember(Name = "supportedLocales", Order = 1)]
        public IReadOnlyList<string>? SupportedLocales { get; set; }

        /// <summary>
        /// Server profiles
        /// </summary>
        [DataMember(Name = "serverProfileArray", Order = 2)]
        public IReadOnlyList<string>? ServerProfiles { get; set; }

        /// <summary>
        /// Supported modelling rules
        /// </summary>
        [DataMember(Name = "modellingRules", Order = 3,
            EmitDefaultValue = false)]
        public IReadOnlyDictionary<string, string>? ModellingRules { get; set; }

        /// <summary>
        /// Supported aggregate functions
        /// </summary>
        [DataMember(Name = "aggregateFunctions", Order = 4,
            EmitDefaultValue = false)]
        public IReadOnlyDictionary<string, string>? AggregateFunctions { get; set; }

        /// <summary>
        /// Supported aggregate functions
        /// </summary>
        [DataMember(Name = "MaxSessions", Order = 5,
            EmitDefaultValue = false)]
        public uint? MaxSessions { get; set; }

        /// <summary>
        /// Supported aggregate functions
        /// </summary>
        [DataMember(Name = "MaxSubscriptions", Order = 6,
            EmitDefaultValue = false)]
        public uint? MaxSubscriptions { get; set; }

        /// <summary>
        /// Supported aggregate functions
        /// </summary>
        [DataMember(Name = "MaxMonitoredItems", Order = 7,
            EmitDefaultValue = false)]
        public uint? MaxMonitoredItems { get; set; }

        /// <summary>
        /// Supported aggregate functions
        /// </summary>
        [DataMember(Name = "MaxSubscriptionsPerSession", Order = 8,
            EmitDefaultValue = false)]
        public uint? MaxSubscriptionsPerSession { get; set; }

        /// <summary>
        /// Supported aggregate functions
        /// </summary>
        [DataMember(Name = "MaxMonitoredItemsPerSubscription", Order = 9,
            EmitDefaultValue = false)]
        public uint? MaxMonitoredItemsPerSubscription { get; set; }

        /// <summary>
        /// Supported aggregate functions
        /// </summary>
        [DataMember(Name = "MaxSelectClauseParameters", Order = 10,
            EmitDefaultValue = false)]
        public uint? MaxSelectClauseParameters { get; set; }

        /// <summary>
        /// Supported aggregate functions
        /// </summary>
        [DataMember(Name = "MaxWhereClauseParameters", Order = 11,
            EmitDefaultValue = false)]
        public uint? MaxWhereClauseParameters { get; set; }

        /// <summary>
        /// Supported aggregate functions
        /// </summary>
        [DataMember(Name = "MaxMonitoredItemsQueueSize", Order = 12,
            EmitDefaultValue = false)]
        public uint? MaxMonitoredItemsQueueSize { get; set; }

        /// <summary>
        /// Supported aggregate functions
        /// </summary>
        [DataMember(Name = "conformanceUnits", Order = 13,
            EmitDefaultValue = false)]
        public IReadOnlyList<string>? ConformanceUnits { get; set; }
    }
}
