// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Opc.Ua.Design {
    using Newtonsoft.Json;
    using Opc.Ua.Extensions;
    using Opc.Ua.Nodeset;
    using Opc.Ua.Nodeset.Schema;
    using System;
    using System.IO;

    /// <summary>
    /// Model design extensions
    /// </summary>
    public static class ModelDesignEx {

        /// <summary>
        /// Generates a single file containing all of the classes.
        /// </summary>
        public static void Save(this IModelDesign _model, string filePath) {
            var context = new SystemContext {
                NamespaceUris = new NamespaceTable()
            };
            var nodes = _model.GetNodes(context);
            var collection = nodes.ToNodeStateCollection(context);

            // open the output file.
            var outputFile = string.Format(@"{0}\{1}.PredefinedNodes.xml", filePath, _model.Name);
            using (var ostrm = File.Open(outputFile, FileMode.Create)) {
                collection.SaveAsXml(context, ostrm);
            }

            // save as nodeset.
            var outputFile2 = string.Format(@"{0}\{1}.NodeSet.xml", filePath, _model.Name);
            using (var ostrm = File.Open(outputFile2, FileMode.Create)) {
                collection.SaveAsNodeSet(ostrm, context);
            }

            // save as nodeset2.
            var outputFile3 = string.Format(@"{0}\{1}.NodeSet2.xml", filePath, _model.Name);
            using (Stream ostrm = File.Open(outputFile3, FileMode.Create)) {
                var model = new ModelTableEntry {
                    ModelUri = _model.Namespace,
                    Version = _model.Version,
                    PublicationDate = _model.PublicationDate ?? DateTime.MinValue,
                    PublicationDateSpecified = _model.PublicationDate != null
                };
                // if (_model.Dependencies != null) {
                //     model.RequiredModel = new List<ModelTableEntry>(_model.Dependencies.Values).ToArray();
                // }
                NodeSet2.Create(nodes, model, _model.PublicationDate, context).Save(ostrm);
            }

            // save as json.
            var outputFile4 = string.Format(@"{0}\{1}.NodeSet.json", filePath, _model.Name);
            using (var ostrm = File.Open(outputFile4, FileMode.Create)) {
                collection.SaveAsJson(ostrm, Formatting.Indented, context);
            }
        }
    }
}
