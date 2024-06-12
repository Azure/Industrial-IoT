// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Models
{
    using System.Diagnostics.CodeAnalysis;

    /// <summary>
    /// Events extensions
    /// </summary>
    public static class PublishedDataSetVariableModelEx
    {
        /// <summary>
        /// Clone
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [return: NotNullIfNotNull(nameof(model))]
        public static PublishedDataSetVariableModel? Clone(this PublishedDataSetVariableModel? model)
        {
            return model == null ? null : (model with
            {
                Triggering = model.Triggering.Clone(),
                SubstituteValue = model.SubstituteValue?.Copy()
            });
        }
    }
}
