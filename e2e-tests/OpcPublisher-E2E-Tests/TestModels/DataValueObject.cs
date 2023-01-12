// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace OpcPublisher_AE_E2E_Tests.TestModels {
    using Microsoft.Azure.Management.Monitor.Fluent.Models;
    using Newtonsoft.Json;
    using System;

    public class DataValueObject<T> {

        /// <summary>
        /// Value
        /// </summary>
        [JsonProperty]
        public T Value { get; set; }

        [JsonProperty]
        public DateTime? SourceTimestamp { get; set; }

        [JsonProperty]
        public DateTime? ServerTimestamp { get; set; }
    }
}