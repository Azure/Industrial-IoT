// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Identifiers {
    using System;

    /// <summary>
    /// Code space identifier values according to ISO 22745
    /// </summary>
    public enum Csi {

        /// <summary>
        /// Undefined
        /// </summary>
        Undefined,

        /// <summary>
        /// Concept type
        /// </summary>
        ConceptType,

        /// <summary>
        /// Term
        /// </summary>
        Term,

        /// <summary>
        /// Definition
        /// </summary>
        Definition,

        /// <summary>
        /// Image
        /// </summary>
        Image,

        /// <summary>
        /// Abbreviation
        /// </summary>
        Abbreviation,

        /// <summary>
        /// Graphical
        /// </summary>
        GraphicalSymbol,

        /// <summary>
        /// Textual
        /// </summary>
        TextualSymbol,

        /// <summary>
        /// Language
        /// </summary>
        Language,

        /// <summary>
        /// Org
        /// </summary>
        Organization,

        /// <summary>
        /// Class
        /// </summary>
        Class,

        /// <summary>
        /// Property
        /// </summary>
        Property,

        /// <summary>
        /// Uom
        /// </summary>
        UnitOfMeasure,

        /// <summary>
        /// Property value
        /// </summary>
        PropertyValue,

        /// <summary>
        /// Currency
        /// </summary>
        Currency,

        /// <summary>
        /// Data type
        /// </summary>
        DataType,

        /// <summary>
        /// Ontology
        /// </summary>
        Ontology,

        /// <summary>
        /// Aspect of Conversion
        /// </summary>
        AspectOfConversion,

        /// <summary>
        /// Template
        /// </summary>
        Template,

        /// <summary>
        /// Quantity
        /// </summary>
        Quantity
    }

    /// <summary>
    /// Csi type extensions
    /// </summary>
    public static class CsiEx {

        /// <summary>
        /// Helpoer to parse Csi
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static Csi ToCsi(this string value) {
            switch (value) {
                case "CT":
                    return Csi.ConceptType;
                case "TM":
                    return Csi.Term;
                case "DF":
                    return Csi.Definition;
                case "IM":
                    return Csi.Image;
                case "AB":
                    return Csi.Abbreviation;
                case "GS":
                    return Csi.GraphicalSymbol;
                case "TS":
                    return Csi.TextualSymbol;
                case "LG":
                    return Csi.Language;
                case "OG":
                    return Csi.Organization;
                case "01":
                    return Csi.Class;
                case "02":
                    return Csi.Property;
                case "05":
                    return Csi.UnitOfMeasure;
                case "07":
                    return Csi.PropertyValue;
                case "08":
                    return Csi.Currency;
                case "09":
                    return Csi.DataType;
                case "11":
                    return Csi.Ontology;
                case "Z2":
                    return Csi.AspectOfConversion;
                case "Z3":
                    return Csi.Template;
                case "Z4":
                    return Csi.Quantity;
                default:
                    throw new FormatException(
                        $"Bad CSI '{value}'");
            }
        }

        /// <summary>
        /// Helper to stringify Csi
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string ToCode(this Csi value) {
            switch (value) {
                case Csi.ConceptType:
                    return "CT";
                case Csi.Term:
                    return "TM";
                case Csi.Definition:
                    return "DF";
                case Csi.Image:
                    return "IM";
                case Csi.Abbreviation:
                    return "AB";
                case Csi.GraphicalSymbol:
                    return "GS";
                case Csi.TextualSymbol:
                    return "TS";
                case Csi.Language:
                    return "LG";
                case Csi.Organization:
                    return "OG";
                case Csi.Class:
                    return "01";
                case Csi.Property:
                    return "02";
                case Csi.UnitOfMeasure:
                    return "05";
                case Csi.PropertyValue:
                    return "07";
                case Csi.Currency:
                    return "08";
                case Csi.DataType:
                    return "09";
                case Csi.Ontology:
                    return "11";
                case Csi.AspectOfConversion:
                    return "Z2";
                case Csi.Template:
                    return "Z3";
                case Csi.Quantity:
                    return "Z4";
                default:
                    throw new FormatException(
                        $"Unexpected CSI '{value}'");
            }
        }
    }
}
