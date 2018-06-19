// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace I40.Common.Models {
    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;

    /// <summary>
    /// Kind of identification.
    /// TODO: Should rename Identificator to Kind to a) be consistent, and b) not english.
    /// </summary>
    [JsonConverter(typeof(StringEnumConverter))]
    public enum IdentificationKind {

        /// <summary>
        /// ISO29002-5 as an identifier scheme for properties
        /// and classifications. All globally unique properties in
        /// ISO, IEC, eCl@ss and others are identified in accordance
        /// with this identification scheme
        /// </summary>
        Irdi,

        /// <summary>
        /// URIs and URLs can easily be formed by developers themselves.
        /// All that is needed for this is to make sure that the way the
        /// domain (e.g. www.festo.com) is organised ensures that the path
        /// behind the domain name is reserved in a semantically unique
        /// way for these identifiers.
        /// </summary>
        Uri,

        /// <summary>
        /// Internal identifiers can also be easily formed by developers
        /// themselves. All that is necessary is for a corresponding
        /// programmatic functionality to be retrieved. It is necessary
        /// to ensure that internal identifiers can be clearly
        /// distinguished from IRDI and URI.
        /// </summary>
        Internal
    }
}