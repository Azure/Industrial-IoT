// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Encoders {
    using Azure.IIoT.OpcUa.Encoders.Converters;
    using Microsoft.Azure.IIoT.Serializers.NewtonSoft;
    using Newtonsoft.Json;
    using Opc.Ua;
    using System.Collections.Generic;
    using System.Linq;
    using System.Runtime.Serialization;

    /// <summary>
    /// Adds Ua converters to default json converters
    /// </summary>
    public class JsonConverters : NewtonSoftJsonConverters {

        /// <inheritdoc/>
        public override StreamingContext Context => _context != null ?
            new StreamingContext(StreamingContextStates.File, _context) : default;

        /// <summary>
        /// Create configuration
        /// </summary>
        /// <param name="context"></param>
        /// <param name="permissive"></param>
        public JsonConverters(IServiceMessageContext context = null,
            bool permissive = false) : base(permissive) {
            _context = context;
        }

        /// <inheritdoc/>
        public override IEnumerable<JsonConverter> GetConverters() {
            var converters = base.GetConverters().ToList();
            converters.AddUaConverters();
            return converters;
        }

        private readonly IServiceMessageContext _context;
    }
}
