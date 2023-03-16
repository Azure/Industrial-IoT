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
        [DataMember(Name = "operationLimits", Order = 0,
            EmitDefaultValue = false)]
        public OperationLimitsModel OperationLimits { get; set; } = null!;

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
    }
}
