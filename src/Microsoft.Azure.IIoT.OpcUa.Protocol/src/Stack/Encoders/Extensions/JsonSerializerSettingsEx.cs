// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Newtonsoft.Json {
    using Newtonsoft.Json.Converters;
    using Newtonsoft.Json.Serialization;
    using Opc.Ua;
    using System.Collections.Generic;
    using System.Runtime.Serialization;

    /// <summary>
    /// Json serializer settings extensions
    /// </summary>
    public static class JsonSerializerSettingsEx {

        /// <summary>
        /// Add converters
        /// </summary>
        /// <param name="settings"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        public static JsonSerializerSettings AddUaConverters(this JsonSerializerSettings settings,
            ServiceMessageContext context = null) {
            if (settings == null) {
                return null;
            }
            if (settings.Converters == null) {
                settings.Converters = new List<JsonConverter>();
            }
            settings.ContractResolver = new CamelCasePropertyNamesContractResolver();
            if (context != null) {
                settings.Context = new StreamingContext(StreamingContextStates.File, context);
            }
            settings.Converters.Add(new StringEnumConverter(new CamelCaseNamingStrategy(), true));
            settings.Converters.Add(new DataValueConverter());
            settings.Converters.Add(new DiagnosticInfoConverter());
            settings.Converters.Add(new ExpandedNodeIdConverter());
            settings.Converters.Add(new NodeIdConverter());
            settings.Converters.Add(new EncodeableConverter());
            settings.Converters.Add(new ExtensionObjectConverter());
            settings.Converters.Add(new LocalizedTextConverter());
            settings.Converters.Add(new QualifiedNameConverter());
            settings.Converters.Add(new StatusCodeConverter());
            settings.Converters.Add(new UuidConverter());
            settings.Converters.Add(new VariantConverter());
            return settings;
        }
    }
}
