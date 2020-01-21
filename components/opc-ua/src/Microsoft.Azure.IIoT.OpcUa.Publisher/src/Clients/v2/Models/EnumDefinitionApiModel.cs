// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Publisher.Clients.v2.Models {
    using Microsoft.Azure.IIoT.OpcUa.Core.Models;
    using Newtonsoft.Json;
    using System;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Enum definition
    /// </summary>
    public class EnumDefinitionApiModel {
        /// <summary>
        /// Default constructor
        /// </summary>
        public EnumDefinitionApiModel() { }

        /// <summary>
        /// Create api model from service model
        /// </summary>
        /// <param name="model"></param>
        public EnumDefinitionApiModel(EnumDefinitionModel model) {
            if (model == null) {
                throw new ArgumentNullException(nameof(model));
            }
            Fields = model.Fields?
                .Select(f => new EnumFieldApiModel(f))
                .ToList();
        }

        /// <summary>
        /// Create service model from api model
        /// </summary>
        public EnumDefinitionModel ToServiceModel() {
            return new EnumDefinitionModel {
                Fields = Fields?
                    .Select(f => f.ToServiceModel())
                    .ToList()
            };
        }

        /// <summary>
        /// The fields of the enum
        /// </summary>
        [JsonProperty(PropertyName = "fields")]
        public List<EnumFieldApiModel> Fields { get; set; }
    }
}
