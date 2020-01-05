﻿// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Opc.Ua.Extensions {
    using Opc.Ua.Models;
    using System;
    using System.Linq;

    /// <summary>
    /// Relative path extensions
    /// </summary>
    public static class RelativePathEx {

        /// <summary>
        /// Convert to path object
        /// </summary>
        /// <param name="path"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        public static RelativePath ToRelativePath(this string[] path,
            ServiceMessageContext context) {
            if (path == null) {
                return new RelativePath();
            }
            return new RelativePath {
                Elements = new RelativePathElementCollection(path
                    .Where(p => !string.IsNullOrEmpty(p))
                    .Select(p => ParsePathElement(p, context)))
            };
        }

        /// <summary>
        /// Convert a relative path to path strings
        /// </summary>
        /// <param name="path"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        public static string[] AsString(this RelativePath path,
            ServiceMessageContext context) {
            if (path == null) {
                return null;
            }
            return path.Elements
                .Select(p => FormatRelativePathElement(p, context))
                .ToArray();
        }

        /// <summary>
        /// Convert to path element object
        /// </summary>
        /// <param name="element"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        private static RelativePathElement ParsePathElement(string element,
            ServiceMessageContext context) {
            if (string.IsNullOrEmpty(element)) {
                throw new ArgumentNullException(nameof(element));
            }

            var pathElement = new RelativePathElement {
                IncludeSubtypes = true,
                IsInverse = false
            };
            //
            // Parse relative path reference information
            // This should allow
            // - "targeturi" == "/targeturi"
            // - ".targeturi"
            // - "!.parenturi"
            // - "!/parenturi"
            // - "<!#uri>parenturi"
            //
            var index = 0;
            var exit = false;
            var parseReference = false;
            while (index < element.Length && !exit) {
                switch (element[index]) {
                    case '<':
                        if (pathElement.ReferenceTypeId == null) {
                            parseReference = true;
                            break;
                        }
                        throw new FormatException("Reference type set.");
                    case '!':
                        pathElement.IsInverse = true;
                        break;
                    case '#':
                        pathElement.IncludeSubtypes = false;
                        break;
                    case '/':
                        if (pathElement.ReferenceTypeId == null &&
                            !parseReference) {
                            pathElement.ReferenceTypeId =
                                ReferenceTypeIds.HierarchicalReferences;
                            break;
                        }
                        throw new FormatException("Reference type set.");
                    case '.':
                        if (pathElement.ReferenceTypeId == null &&
                            !parseReference) {
                            pathElement.ReferenceTypeId =
                                ReferenceTypeIds.Aggregates;
                            break;
                        }
                        throw new FormatException("Reference type set.");
                    default:
                        if (element[index] == '&') {
                            index++;
                        }
                        if (pathElement.ReferenceTypeId == null &&
                            !parseReference) {
                            // Set to all references
                            pathElement.ReferenceTypeId =
                                ReferenceTypeIds.References;
                        }
                        exit = true;
                        break;
                }
                index++;
            }
            index--;
            if (parseReference) {
                var to = index;
                while (to < element.Length) {
                    if (element[to] == '>' && element[to - 1] != '&') {
                        break;
                    }
                    to++;
                    if (to == element.Length) {
                        throw new FormatException(
                            "Reference path starts in < but does not end in >");
                    }
                }
                var reference = element.Substring(index, to - index);
                // TODO: Deescape &<, &>, &/, &., &:, &&
                index = to + 1;
                pathElement.ReferenceTypeId = reference.ToNodeId(context);
                if (NodeId.IsNull(pathElement.ReferenceTypeId)) {
                    if (TypeMaps.ReferenceTypes.Value.TryGetIdentifier(reference,
                        out var id)) {
                        pathElement.ReferenceTypeId = id;
                    }
                }
            }
            var target = element.Substring(index);
            // TODO: Deescape &<, &>, &/, &., &:, &&
            if (string.IsNullOrEmpty(target)) {
                throw new FormatException("Bad target name is empty");
            }
            pathElement.TargetName = target.ToQualifiedName(context);
            return pathElement;
        }

        /// <summary>
        /// Format relative path element information
        /// </summary>
        /// <param name="element"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        private static string FormatRelativePathElement(RelativePathElement element,
            ServiceMessageContext context) {
            var value = "";
            var writeReference = false;
            if (element.ReferenceTypeId == ReferenceTypeIds.HierarchicalReferences) {
                value += "/";
            }
            else if (element.ReferenceTypeId == ReferenceTypeIds.Aggregates) {
                value += ".";
            }
            else if (element.ReferenceTypeId != ReferenceTypeIds.References) {
                value += "<";
                writeReference = true;
            }
            if (element.IsInverse) {
                value += "!";
            }
            if (!element.IncludeSubtypes) {
                value += "#";
            }
            if (writeReference) {
                string reference = null;
                if (element.ReferenceTypeId.NamespaceIndex == 0 &&
                    element.ReferenceTypeId.Identifier is uint id) {
                    TypeMaps.ReferenceTypes.Value.TryGetBrowseName(id, out reference);
                }
                if (string.IsNullOrEmpty(reference)) {
                    reference = element.ReferenceTypeId.AsString(context);
                }
                // TODO: Escape <,>,/,:,&,.
                value += reference + ">";
            }
            var target = element.TargetName.AsString(context);
            // TODO: Escape <,>,/,:,&,.
            value += target;
            return value;
        }
    }
}
