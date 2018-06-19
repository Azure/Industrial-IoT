// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace I40.DINPVSxx.Models {
    using Newtonsoft.Json;
    using System.ComponentModel.DataAnnotations;

    /// <summary>
    /// A file property is a path reference to a file.
    /// Constraint AAS-010: For properties of category =
    /// File a qualifier of type “mimetype” shall be added.
    /// </summary>
    public class File : AtomicDataProperty {

        /// <summary>
        /// Name of the referenced file (without file extension)
        /// Note: The file extension is defined by using a
        /// qualifier of type "Mimetype".
        /// </summary>
        [JsonProperty(PropertyName = "fileName")]
        [Required]
        public string FileName { get; set; }
        // TODO: FileName is 0..1 in UML which makes not sense.

        /// <summary>
        /// Path to referenced file (not including the file name
        /// itself). The path can be absolute or relative to the
        /// package root. An absolute path is used Absolute in the
        /// case that the file exists independently of the AAS. A
        /// relative path, rRelative to the package root should be
        /// used, if the file is part of the serialized package of
        /// the AAS
        /// </summary>
        [JsonProperty(PropertyName = "path",
            NullValueHandling = NullValueHandling.Ignore)]
        public string Path { get; set; }
    }
}