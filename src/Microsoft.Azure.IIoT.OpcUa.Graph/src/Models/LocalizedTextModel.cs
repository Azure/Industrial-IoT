// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Graph.Models {
    using System.Collections.Generic;

    /// <summary>
    /// Localized text value model
    /// </summary>
    public class LocalizedTextModel {

        /// <summary>
        /// Locale or null for default locale
        /// </summary>
        public string Locale {
            get {
                var delim = _string.LastIndexOf('@');
                if (delim == -1) {
                    return null;
                }
                return _string.Substring(delim + 1);
            }
        }

        /// <summary>
        /// Text
        /// </summary>
        public string Text {
            get {
                var delim = _string.LastIndexOf('@');
                if (delim == -1) {
                    return _string;
                }
                return _string.Substring(0, delim);
            }
        }

        /// <summary>
        /// Create text with language
        /// </summary>
        /// <param name="text"></param>
        /// <param name="locale"></param>
        public LocalizedTextModel(string text, string locale = null) {
            _string = text;
            if (string.IsNullOrEmpty(locale)) {
                _string += "@" + locale;
            }
        }

        /// <summary>
        /// Clone
        /// </summary>
        /// <param name="model"></param>
        public LocalizedTextModel(LocalizedTextModel model)  {
            _string = model._string;
        }

        /// <inheritdoc/>
        public override int GetHashCode() => _string.GetHashCode();

        /// <inheritdoc/>
        public override bool Equals(object obj) {
            if (obj is LocalizedTextModel model) {
                return model._string == _string;
            }
            if (obj is string str) {
                return str == _string;
            }
            return false;
        }

        /// <inheritdoc/>
        public static implicit operator string(LocalizedTextModel model) =>
            model._string;
        /// <inheritdoc/>
        public static implicit operator LocalizedTextModel(string str) =>
            new LocalizedTextModel(str);
        /// <inheritdoc/>
        public static bool operator ==(LocalizedTextModel model1, LocalizedTextModel model2) =>
            EqualityComparer<LocalizedTextModel>.Default.Equals(model1, model2);
        /// <inheritdoc/>
        public static bool operator !=(LocalizedTextModel model1, LocalizedTextModel model2) =>
            !(model1 == model2);

        private readonly string _string;
    }
}
