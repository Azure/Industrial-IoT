/* ========================================================================
 * Copyright (c) 2005-2016 The OPC Foundation, Inc. All rights reserved.
 *
 * OPC Foundation MIT License 1.00
 *
 * Permission is hereby granted, free of charge, to any person
 * obtaining a copy of this software and associated documentation
 * files (the "Software"), to deal in the Software without
 * restriction, including without limitation the rights to use,
 * copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the
 * Software is furnished to do so, subject to the following
 * conditions:
 *
 * The above copyright notice and this permission notice shall be
 * included in all copies or substantial portions of the Software.
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
 * EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES
 * OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
 * NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT
 * HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
 * WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
 * FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR
 * OTHER DEALINGS IN THE SOFTWARE.
 *
 * The complete license agreement can be found here:
 * http://opcfoundation.org/License/MIT/1.00/
 * ======================================================================*/

namespace Opc.Ua.Design {
    using Opc.Ua.Design.Resolver;
    using Opc.Ua.Design.Schema;
    using Opc.Ua.Types;
    using System;
    using System.Collections.Generic;
    using System.IO;

    /// <summary>
    /// Model loader
    /// </summary>
    public static class Model {

        /// <summary>
        /// Default built in model
        /// </summary>
        public static IModelDesign Standard { get; } = LoadStandardModel();

        /// <summary>
        /// Load design from input stream
        /// </summary>
        /// <param name="design"></param>
        /// <param name="assigner"></param>
        /// <param name="resolver"></param>
        /// <returns></returns>
        public static IModelDesign Load(Stream design, INodeIdAssigner assigner = null,
            INodeResolver resolver = null) {
            try {
                var model = design.DeserializeFromXml<ModelDesign>();
                return new ModelDesignFile(model, assigner, resolver ?? new CompositeModelResolver());
            }
            catch (Exception ex) {
                // Try to load as type dictionary
                if (!design.CanSeek) {
                    // Stream is already partitially read - need to reset - if we cannot throw
                    throw;
                }
                try {
                    design.Seek(0, SeekOrigin.Begin);
                    return LoadTypeDictionary(design, assigner, resolver);
                }
                catch {
#pragma warning disable CA2200 // Rethrow to preserve stack details
                    throw ex;
#pragma warning restore CA2200 // Rethrow to preserve stack details
                }
            }
        }

        /// <summary>
        /// Load design from file and optional identifier file
        /// </summary>
        /// <param name="designFile"></param>
        /// <param name="resolver"></param>
        /// <returns></returns>
        public static IModelDesign Load(string designFile, INodeResolver resolver = null) {
            using (var stream = File.OpenRead(designFile)) {
                var model = stream.DeserializeFromXml<ModelDesign>();
                var identifierFile = Path.ChangeExtension(designFile, "csv");
                INodeIdAssigner assigner = null;
                if (File.Exists(identifierFile)) {
                    using (var stream2 = File.OpenRead(identifierFile)) {
                        assigner = new IdentifierFile(stream2, model.TargetNamespace, model.TargetVersion);
                    }
                }
                return new ModelDesignFile(model, assigner, resolver ?? new CompositeModelResolver());
            }
        }

        /// <summary>
        /// Load design from file
        /// </summary>
        /// <param name="designFile"></param>
        /// <param name="assigner"></param>
        /// <param name="resolver"></param>
        /// <returns></returns>
        public static IModelDesign LoadTypeDictionary(string designFile,
            INodeIdAssigner assigner = null, INodeResolver resolver = null) {
            using (var stream = File.OpenRead(designFile)) {
                return LoadTypeDictionary(stream, assigner, resolver);
            }
        }

        /// <summary>
        /// Load design from input stream
        /// </summary>
        /// <param name="design"></param>
        /// <param name="assigner"></param>
        /// <param name="resolver"></param>
        /// <returns></returns>
        public static IModelDesign LoadTypeDictionary(Stream design,
            INodeIdAssigner assigner = null, INodeResolver resolver = null) {
            var types = Types.Load(design, resolver as ITypeResolver);
            return new ModelDesignFile(types.ToModelDesign(), assigner,
                resolver ?? new CompositeModelResolver());
        }

        /// <summary>
        /// Load the standard types from the included resources
        /// </summary>
        /// <returns></returns>
        internal static IModelDesign LoadStandardModel() {
            var assembly = typeof(ModelDesignFile).Assembly;
            var prefix = assembly.GetName().Name;

            // Load built in types
            var builtInTypes = assembly
                .DeserializeFromXmlManifestResource<ModelDesign>(
                    prefix + ".Stack.Design.BuiltIn.BuiltInTypes.xml");

            // Load default type dictionaries
            var typeDictionary = Types.Standard.ToModelDesign();

            // Load standard types
            var standardTypes = assembly
                .DeserializeFromXmlManifestResource<ModelDesign>(
                    prefix + ".Stack.Design.BuiltIn.StandardTypes.xml");

            // Add all to the standard types (i.e. all in opcfoundation.org/UA)
            var nodes = new List<NodeDesign>();
            nodes.AddRange(builtInTypes.Items);
            nodes.AddRange(typeDictionary.Items);
            nodes.AddRange(standardTypes.Items);
            standardTypes.Items = nodes.ToArray();
            standardTypes.TargetVersion = null; // Allow matching to any version

            var ids = assembly.GetManifestResourceStream(
                prefix + ".Stack.Design.BuiltIn.StandardTypes.csv");
            var identifiers = new IdentifierFile(ids,
                standardTypes.TargetNamespace, standardTypes.TargetVersion);
            // Return model
            return new ModelDesignFile(standardTypes, identifiers, null);
        }
    }
}
