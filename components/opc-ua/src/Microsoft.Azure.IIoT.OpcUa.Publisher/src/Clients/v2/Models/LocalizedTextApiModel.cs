// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Publisher.Clients.v2.Models {
    using Microsoft.Azure.IIoT.OpcUa.Core.Models;
    using Newtonsoft.Json;
    using System;

    /// <summary>
    /// Localized text.
    /// </summary>
    public class LocalizedTextApiModel {
        /// <summary>
        /// Default constructor
        /// </summary>
        public LocalizedTextApiModel() { }

        /// <summary>
        /// Create api model from service model
        /// </summary>
        /// <param name="model"></param>
        public LocalizedTextApiModel(LocalizedTextModel model) {
            if (model == null) {
                throw new ArgumentNullException(nameof(model));
            }
            Locale = model.Locale;
            Text = model.Text;
        }

        /// <summary>
        /// Create service model from api model
        /// </summary>
        public LocalizedTextModel ToServiceModel() {
            return new LocalizedTextModel {
                Locale = Locale,
                Text = Text
            };
        }

        /// <summary>
        /// Locale or null for default locale
        /// </summary>
        [JsonProperty(PropertyName = "locale",
            NullValueHandling = NullValueHandling.Ignore)]
        public string Locale { get; set; }

        /// <summary>
        /// Text
        /// </summary>
        [JsonProperty(PropertyName = "text")]
        public string Text { get; set; }
    }
}
