// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace I40.Common.Models {
    using Newtonsoft.Json;
    using System;

    /// <inheritdoc/>
    public abstract class BaseTimeStamped : ITimeStamped {

        /// <inheritdoc/>
        [JsonProperty(PropertyName = "timeStamp",
            NullValueHandling = NullValueHandling.Ignore)]
        public DateTime TimeStamp { get; set; }
    }
}