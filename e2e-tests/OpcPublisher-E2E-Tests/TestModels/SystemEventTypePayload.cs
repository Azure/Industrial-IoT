﻿// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace OpcPublisher_AE_E2E_Tests.TestModels {
    using System;

    /// <summary>Base class for system events types.</summary>
    public class SystemEventTypePayload : BaseEventTypePayload {

        /// <summary>Gets or sets the time.</summary>
        public DateTime? Time { get; set; }
    }
}