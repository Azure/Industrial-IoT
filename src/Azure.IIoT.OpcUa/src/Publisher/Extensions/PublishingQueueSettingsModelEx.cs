// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Models
{
    using System.Diagnostics.CodeAnalysis;

    /// <summary>
    /// Queue settings extensions
    /// </summary>
    public static class PublishingQueueSettingsModelEx
    {
        /// <summary>
        /// Check if these are the same publishing settings.
        /// </summary>
        /// <param name="model"></param>
        /// <param name="other"></param>
        /// <returns></returns>
        public static bool IsSameAs(this PublishingQueueSettingsModel? model,
            PublishingQueueSettingsModel? other)
        {
            if (ReferenceEquals(model, other))
            {
                return true;
            }
            if (model is null || other is null)
            {
                return false;
            }
            if (model.RequestedDeliveryGuarantee != other.RequestedDeliveryGuarantee)
            {
                return false;
            }
            if (model.QueueName != other.QueueName)
            {
                return false;
            }
            return true;
        }

        /// <summary>
        /// Clone
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [return: NotNullIfNotNull(nameof(model))]
        public static PublishingQueueSettingsModel? Clone(this PublishingQueueSettingsModel? model)
        {
            return model == null ? null : (model with
            {
            });
        }
    }
}
