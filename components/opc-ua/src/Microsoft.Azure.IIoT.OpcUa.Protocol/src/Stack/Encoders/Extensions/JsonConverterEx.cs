// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Newtonsoft.Json.Converters {
    using Newtonsoft.Json;
    using Newtonsoft.Json.Serialization;
    using System.Collections.Generic;
    using Opc.Ua;

    /// <summary>
    /// Json converter extensions
    /// </summary>
    public static class JsonConverterEx {

        /// <summary>
        /// Add converters
        /// </summary>
        /// <param name="converters"></param>
        /// <returns></returns>
        public static IList<JsonConverter> AddUaConverters(this IList<JsonConverter> converters) {
            if (converters == null) {
                converters = new List<JsonConverter>();
            }
            converters.Add(new StringEnumConverter(new CamelCaseNamingStrategy(), true));
            converters.Add(new DataValueConverter());
            converters.Add(new Opc.Ua.DataSetConverter());
            converters.Add(new DiagnosticInfoConverter());
            converters.Add(new ExpandedNodeIdConverter());
            converters.Add(new NodeIdConverter());
            converters.Add(new EncodeableConverter());
            converters.Add(new ExtensionObjectConverter());
            converters.Add(new LocalizedTextConverter());
            converters.Add(new QualifiedNameConverter());
            converters.Add(new StatusCodeConverter());
            converters.Add(new UuidConverter());
            converters.Add(new VariantConverter());
            return converters;
        }
    }
}
