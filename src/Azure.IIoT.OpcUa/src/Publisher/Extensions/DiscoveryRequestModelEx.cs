// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Models
{
    using System;
    using System.Diagnostics.CodeAnalysis;

    /// <summary>
    /// Discovery request model extensions
    /// </summary>
    public static class DiscoveryRequestModelEx
    {
        /// <summary>
        /// Clone
        /// </summary>
        /// <param name="model"></param>
        /// <param name="timeProvider"></param>
        /// <returns></returns>
        [return: NotNullIfNotNull(nameof(model))]
        public static DiscoveryRequestModel? Clone(this DiscoveryRequestModel? model,
            TimeProvider timeProvider)
        {
            return model == null ? null : (model with
            {
                Configuration = model.Configuration.Clone(),
                Context = model.Context?.Clone(timeProvider)
            });
        }
    }
}
