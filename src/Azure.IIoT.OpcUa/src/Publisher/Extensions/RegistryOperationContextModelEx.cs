// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Models
{
    using System;

    /// <summary>
    /// Operation extensions
    /// </summary>
    public static class RegistryOperationContextModelEx
    {
        /// <summary>
        /// Clone
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public static OperationContextModel? Clone(
            this OperationContextModel? model)
        {
            model = model.Validate();
            return new OperationContextModel
            {
                AuthorityId = model.AuthorityId,
                Time = model.Time
            };
        }

        /// <summary>
        /// Clone
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public static OperationContextModel Validate(
            this OperationContextModel? context)
        {
            if (context == null)
            {
                context = new OperationContextModel
                {
                    AuthorityId = null, // Should throw if configured
                    Time = DateTime.UtcNow
                };
            }
            return context;
        }
    }
}
