// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace OpcPublisherAEE2ETests.TestModels
{
    using Newtonsoft.Json;
    using System;

    /// <summary>Base class for system events types.</summary>
    public class SystemEventTypePayload : BaseEventTypePayload
    {
        /// <summary>Gets or sets the time.</summary>
        [JsonProperty(nameof(Time))]
        public DataValueObject<DateTime?> Time { get; set; }
    }
}