// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Agent.Framework {
    using Newtonsoft.Json.Linq;

    /// <summary>
    /// Serialize and deserialize jobs
    /// </summary>
    public interface IJobSerializer {

        /// <summary>
        /// Deserialize job
        /// </summary>
        /// <param name="model"></param>
        /// <param name="jobConfigurationType"></param>
        /// <returns></returns>
        object DeserializeJobConfiguration(JToken model, string jobConfigurationType);

        /// <summary>
        /// Serialize job
        /// </summary>
        /// <param name="jobConfig"></param>
        /// <param name="jobConfigurationType"></param>
        /// <returns></returns>
        JToken SerializeJobConfiguration<T>(T jobConfig, out string jobConfigurationType);
    }
}