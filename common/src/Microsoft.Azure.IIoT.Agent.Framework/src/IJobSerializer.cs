// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Agent.Framework {
    using Microsoft.Azure.IIoT.Serializers;

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
        object DeserializeJobConfiguration(VariantValue model, string jobConfigurationType);

        /// <summary>
        /// Serialize job
        /// </summary>
        /// <param name="jobConfig"></param>
        /// <param name="jobConfigurationType"></param>
        /// <returns></returns>
        VariantValue SerializeJobConfiguration<T>(T jobConfig, out string jobConfigurationType);
    }
}