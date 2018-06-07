// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace I40.IEC61360.Models {
    using I40.Common.Models;
    using Newtonsoft.Json;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;

    /// <summary>
    /// Template for defining property descriptions conformant to IEC 61360
    /// </summary>
    public class Template_PropertyDefinition_IEC61360 : ITemplateContent {

        /// <summary>
        /// short name
        /// </summary>
        [JsonProperty(PropertyName = "shortName")]
        [Required]
        public string ShortName { get; set; }

        /// <summary>
        /// Information which uniquely the significance of a data
        /// element type and permits it to be distinguished from
        /// all other data element types.
        /// </summary>
        [JsonProperty(PropertyName = "definition")]
        [Required]
        public LanguageString Definition { get; set; }

        /// <summary>
        /// Designation consisting of a word or several words which
        /// is assigned to a data element type (property).
        /// </summary>
        [JsonProperty(PropertyName = "preferredName")]
        [Required]
        public LanguageString PreferredName { get; set; }

        /// <summary>
        /// Data type
        /// </summary>
        [JsonProperty(PropertyName = "dataType")]
        [Required]
        public IDataType DataType { get; set; }

        /// <summary>
        /// [0..1] Symbol
        /// </summary>
        [JsonProperty(PropertyName = "symbol",
            NullValueHandling = NullValueHandling.Ignore)]
        public string Symbol { get; set; }

        /// <summary>
        /// [0..1] Source of definition
        /// </summary>
        [JsonProperty(PropertyName = "sourceOfDefinition",
            NullValueHandling = NullValueHandling.Ignore)]
        public LanguageString SourceOfDefinition { get; set; }

        /// <summary>
        /// Specification of the type and length of the presentation
        /// of the value of a data element type.
        /// </summary>
        [JsonProperty(PropertyName = "valueFormat",
            NullValueHandling = NullValueHandling.Ignore)]
        public string ValueFormat { get; set; }

        /// <summary>
        /// [0..*] List of values
        /// </summary>
        [JsonProperty(PropertyName = "valueList",
            NullValueHandling = NullValueHandling.Ignore)]
        public List<object> ValueList { get; set; }

        /// <summary>
        /// [0..1] Unit of measure
        /// </summary>
        [JsonProperty(PropertyName = "unitOfMeasure",
            NullValueHandling = NullValueHandling.Ignore)]
        public string UnitOfMeasure { get; set; }

    //    /// <summary>
    //    /// Property definition
    //    /// </summary>
    //    [JsonProperty(PropertyName = "id_propertyDefinition",
    //        NullValueHandling = NullValueHandling.Ignore)]
    //    public Identification PropertyDefinition { get; set; }
    }
}