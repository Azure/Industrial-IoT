// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Publisher.Clients {
    using Microsoft.Azure.IIoT.OpcUa.Api.Publisher.Models;
    using Microsoft.Azure.IIoT.OpcUa.Publisher.Models;
    using Microsoft.Azure.IIoT.Agent.Framework;
    using Microsoft.Azure.IIoT.Agent.Framework.Exceptions;
    using Microsoft.Azure.IIoT.Serializers;

    /// <summary>
    /// Publisher job serializer
    /// </summary>
    public sealed class PublisherJobSerializer : IJobSerializer {

        /// <summary>
        /// Cerate job serializer
        /// </summary>
        /// <param name="serializer"></param>
        public PublisherJobSerializer(IJsonSerializer serializer) {
            _serializer = serializer;
        }

        /// <inheritdoc/>
        public object DeserializeJobConfiguration(VariantValue model, string jobConfigurationType) {
            switch (jobConfigurationType) {
                case kDataSetWriterJobV2:
                    return model.ConvertTo<WriterGroupJobApiModel>().ToServiceModel();
                    // ... Add more if needed
            }
            throw new UnknownJobTypeException(jobConfigurationType);
        }

        /// <inheritdoc/>
        public VariantValue SerializeJobConfiguration<T>(T jobConfig, out string jobConfigurationType) {
            switch (jobConfig) {
                case WriterGroupJobModel pj:
                    jobConfigurationType = kDataSetWriterJobV2;
                    return _serializer.FromObject(pj.ToApiModel());
                    // ... Add more if needed
            }
            throw new UnknownJobTypeException(typeof(T).Name);
        }

        private const string kDataSetWriterJobV2 = "DataSetWriterV2";
        private readonly IJsonSerializer _serializer;
    }
}
