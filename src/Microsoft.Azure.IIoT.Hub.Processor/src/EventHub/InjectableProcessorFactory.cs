// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Hub.Processor.EventHub {
    using Autofac;
    using Microsoft.Azure.EventHubs.Processor;

    /// <summary>
    /// Factory implementation
    /// </summary>
    public sealed class InjectableProcessorFactory : IEventProcessorFactory {

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="factory"></param>
        public InjectableProcessorFactory(IComponentContext factory) {
            _factory = factory;
        }

        /// <summary>
        /// Factory
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public IEventProcessor CreateEventProcessor(PartitionContext context) {
            return _factory.Resolve<IEventProcessor>();
        }

        private readonly IComponentContext _factory;
    }
}
