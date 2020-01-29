// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Registry.Models {
    using Microsoft.Azure.IIoT.OpcUa.Registry.Events.v2.Models;
    using System;

    /// <summary>
    /// Application event extensions
    /// </summary>
    public static partial class ApplicationEventEx {

        /// <summary>
        /// Convert to api model
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public static ApplicationEventApiModel ToApiModel(
            this ApplicationEventModel model) {
            return new ApplicationEventApiModel {
                EventType = (ApplicationEventType)model.EventType,
                Id = model.Id,
                IsPatch = model.IsPatch,
                Application = model.Application.Map<ApplicationInfoApiModel>()
            };
        }
    }
}