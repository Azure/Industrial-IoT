﻿// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Registry.Models {
    using Microsoft.Azure.IIoT.OpcUa.Registry.Events.v2.Models;
    using System;

    /// <summary>
    /// Supervisor event extensions
    /// </summary>
    public static class SupervisorEventEx {

        /// <summary>
        /// Convert to api model
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public static SupervisorEventApiModel ToApiModel(
            this SupervisorEventModel model) {
            return new SupervisorEventApiModel {
                EventType = (SupervisorEventType)model.EventType,
                Id = model.Id,
                IsPatch = model.IsPatch,
                Supervisor = model.Supervisor.Map<SupervisorApiModel>()
            };
        }
    }
}