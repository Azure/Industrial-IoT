// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Serializers.NewtonSoft {
    using Newtonsoft.Json.Converters;
    using Newtonsoft.Json;
    using System.Collections.Generic;
    using System.Runtime.Serialization;

    /// <summary>
    /// Json convert helpers
    /// </summary>
    public class NewtonSoftJsonConverters : IJsonSerializerConverterProvider {

        /// <inheritdoc/>
        public virtual StreamingContext Context => default;

        /// <summary>
        /// Create provider
        /// </summary>
        public NewtonSoftJsonConverters() : this (false) {
        }

        /// <summary>
        /// Create provider
        /// </summary>
        /// <param name="permissive"></param>
        protected NewtonSoftJsonConverters(bool permissive = false) {
            _permissive = permissive;
        }

        /// <inheritdoc/>
        public virtual IEnumerable<JsonConverter> GetConverters() {
            var converters = new List<JsonConverter> {
                new ExceptionConverter(_permissive),
                new PhysicalAddressConverter(),
                new IPAddressConverter(),
                new StringEnumConverter()
            };
            return converters;
        }

        private readonly bool _permissive;
    }
}
