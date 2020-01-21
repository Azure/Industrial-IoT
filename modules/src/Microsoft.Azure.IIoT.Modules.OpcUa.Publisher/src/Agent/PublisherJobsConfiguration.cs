// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Modules.OpcUa.Publisher.Agent {
    using Autofac;
    using Microsoft.Azure.IIoT.Agent.Framework;
    using Microsoft.Azure.IIoT.Agent.Framework.Exceptions;
    using Microsoft.Azure.IIoT.Modules.OpcUa.Publisher.v2.Models;
    using Microsoft.Azure.IIoT.OpcUa.Edge.Publisher.Runtime;
    using Microsoft.Azure.IIoT.OpcUa.Publisher.Models;
    using Newtonsoft.Json.Linq;

    /// <summary>
    /// Publish jobs configuration module
    /// </summary>
    public class PublisherJobsConfiguration : Module {

        /// <summary>
        /// Publish job serializer
        /// </summary>
        public sealed class PublisherJobSerializer : IJobSerializer {

            /// <inheritdoc/>
            public object DeserializeJobConfiguration(JToken model, string jobConfigurationType) {
                switch (jobConfigurationType) {
                    case kDataSetWriterJobV2:
                        return model.ToObject<WriterGroupJobApiModel>().ToServiceModel();
                        // ... Add more if needed
                }
                throw new UnknownJobTypeException(jobConfigurationType);
            }

            /// <inheritdoc/>
            public JToken SerializeJobConfiguration<T>(T jobConfig, out string jobConfigurationType) {
                switch (jobConfig) {
                    case WriterGroupJobModel pj:
                        jobConfigurationType = kDataSetWriterJobV2;
                        return JObject.FromObject(new WriterGroupJobApiModel(pj));
                        // ... Add more if needed
                }
                throw new UnknownJobTypeException(typeof(T).Name);
            }
        }

        /// <inheritdoc/>
        protected override void Load(ContainerBuilder builder) {
            builder.RegisterType<PublisherJobSerializer>()
                .AsImplementedInterfaces().InstancePerDependency();
            builder.RegisterType<WriterGroupJobContainerFactory>()
                .Named<IProcessingEngineContainerFactory>(kDataSetWriterJobV2)
                .AsImplementedInterfaces().InstancePerDependency();
            base.Load(builder);
        }

        private const string kDataSetWriterJobV2 = "DataSetWriterV2";
    }
}
