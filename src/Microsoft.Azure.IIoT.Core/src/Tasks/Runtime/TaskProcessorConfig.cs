// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Tasks.Runtime {
    using Microsoft.Azure.IIoT.Tasks;
    using Microsoft.Azure.IIoT.Utils;
    using Microsoft.Extensions.Configuration;

    /// <summary>
    /// Task processor configuration for runtime
    /// </summary>
    public class TaskProcessorConfig : ConfigBase, ITaskProcessorConfig {

        private const string MaxInstancesKey = "MaxInstances";
        private const string MaxQueueSizeKey = "MaxQueueSize";
        /// <summary> Max task instances - best between 1-5 </summary>
        public int MaxInstances => GetIntOrDefault(MaxInstancesKey, 1);
        /// <summary> Max queue size to use for in memory queue </summary>
        public int MaxQueueSize => GetIntOrDefault(MaxQueueSizeKey, 1000);

        /// <summary>
        /// Configuration constructor
        /// </summary>
        /// <param name="configuration"></param>
        public TaskProcessorConfig(IConfigurationRoot configuration) :
            base(configuration) {
        }
    }
}
