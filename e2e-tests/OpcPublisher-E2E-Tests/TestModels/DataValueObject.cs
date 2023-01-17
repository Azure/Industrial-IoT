// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace OpcPublisher_AE_E2E_Tests.TestModels {
    using Newtonsoft.Json;
    using System;

    public static class DataValueObject {
        public static DataValueObject<T> Create<T>(T value) => new DataValueObject<T> { Value = value };
    }

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