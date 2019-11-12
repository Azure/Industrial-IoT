// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Agent.Framework.Serializer {
    using Microsoft.Azure.IIoT.Agent.Framework.Exceptions;
    using System;
    using System.Linq;
    using Newtonsoft.Json.Linq;

    /// <summary>
    /// Default job configuration serializer
    /// </summary>
    public class DefaultJobSerializer : IJobSerializer {

        /// <summary>
        /// Create serializer
        /// </summary>
        /// <param name="knownJobConfigProvider"></param>
        public DefaultJobSerializer(IKnownJobConfigProvider knownJobConfigProvider) {
            _knownJobConfigProvider = knownJobConfigProvider;
        }

        /// <inheritdoc/>
        public JToken SerializeJobConfiguration<T>(T jobConfig, out string jobConfigurationType) {
            jobConfigurationType = typeof(T).Name;
            return JObject.FromObject(jobConfig);
        }

        /// <inheritdoc/>
        public object DeserializeJobConfiguration(JToken model, string jobConfigurationType) {
            var type = _knownJobConfigProvider.KnownJobTypes
                .SingleOrDefault(t => t.Name.Equals(jobConfigurationType, StringComparison.OrdinalIgnoreCase));
            if (type == null) {
                throw new UnknownJobTypeException(jobConfigurationType);
            }
            return model.ToObject(type);
        }

        private readonly IKnownJobConfigProvider _knownJobConfigProvider;
    }
}