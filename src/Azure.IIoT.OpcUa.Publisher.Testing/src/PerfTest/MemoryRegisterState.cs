/* ========================================================================
 * Copyright (c) 2005-2017 The OPC Foundation, Inc. All rights reserved.
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

namespace PerfTest
{
    using Opc.Ua;
    using System;
    using System.Collections.Generic;

    public static class ModelUtils
    {
        public static NodeId GetRegisterId(MemoryRegister register, ushort namespaceIndex)
        {
            return new NodeId((uint)register.Id, namespaceIndex);
        }

        public static NodeId GetRegisterVariableId(MemoryRegister register, int index, ushort namespaceIndex)
        {
            var id = (uint)(register.Id << 24) + (uint)index;
            return new NodeId(id, namespaceIndex);
        }

        public static MemoryRegisterState GetRegister(MemoryRegister register, ushort namespaceIndex)
        {
            return new MemoryRegisterState(register, namespaceIndex);
        }

        public static BaseDataVariableState GetRegisterVariable(MemoryRegister register, int index, ushort namespaceIndex)
        {
            if (index < 0 || index >= register.Size)
            {
                return null;
            }

            var variable = new BaseDataVariableState<int>(null)
            {
                NodeId = GetRegisterVariableId(register, index, namespaceIndex),
                BrowseName = new QualifiedName(Utils.Format("{0:000000}", index), namespaceIndex)
            };
            variable.DisplayName = variable.BrowseName.Name;
            variable.Value = register.Read(index);
            variable.DataType = DataTypeIds.Int32;
            variable.ValueRank = ValueRanks.Scalar;
            variable.MinimumSamplingInterval = 100;
            variable.AccessLevel = AccessLevels.CurrentReadOrWrite;
            variable.UserAccessLevel = AccessLevels.CurrentReadOrWrite;
            variable.Handle = register;
            variable.NumericId = (uint)index;

            return variable;
        }
    }

    public class MemoryRegisterState : FolderState
    {
        public MemoryRegisterState(MemoryRegister register, ushort namespaceIndex) : base(null)
        {
            Register = register;

            NodeId = new NodeId((uint)register.Id, namespaceIndex);
            BrowseName = new QualifiedName(register.Name, namespaceIndex);
            DisplayName = BrowseName.Name;

            AddReference(ReferenceTypeIds.Organizes, true, ObjectIds.ObjectsFolder);
        }

        public MemoryRegister Register { get; }

        public override INodeBrowser CreateBrowser(
            ISystemContext context,
            ViewDescription view,
            NodeId referenceType,
            bool includeSubtypes,
            BrowseDirection browseDirection,
            QualifiedName browseName,
            IEnumerable<IReference> additionalReferences,
            bool internalOnly)
        {
            return new MemoryRegisterBrowser(
                context,
                view,
                referenceType,
                includeSubtypes,
                browseDirection,
                browseName,
                additionalReferences,
                internalOnly,
                this);
        }
    }

    /// <summary>
    /// Browses the children of a segment.
    /// </summary>
    public class MemoryRegisterBrowser : NodeBrowser
    {
        /// <summary>
        /// Creates a new browser object with a set of filters.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="view"></param>
        /// <param name="referenceType"></param>
        /// <param name="includeSubtypes"></param>
        /// <param name="browseDirection"></param>
        /// <param name="browseName"></param>
        /// <param name="additionalReferences"></param>
        /// <param name="internalOnly"></param>
        /// <param name="parent"></param>
        public MemoryRegisterBrowser(
            ISystemContext context,
            ViewDescription view,
            NodeId referenceType,
            bool includeSubtypes,
            BrowseDirection browseDirection,
            QualifiedName browseName,
            IEnumerable<IReference> additionalReferences,
            bool internalOnly,
            MemoryRegisterState parent)
        :
            base(
                context,
                view,
                referenceType,
                includeSubtypes,
                browseDirection,
                browseName,
                additionalReferences,
                internalOnly)
        {
            _parent = parent;
            _stage = Stage.Begin;
        }

        /// <summary>
        /// Returns the next reference.
        /// </summary>
        /// <returns>The next reference that meets the browse criteria.</returns>
        public override IReference Next()
        {
            _ = (UnderlyingSystem)SystemContext.SystemHandle;

            lock (DataLock)
            {
                // enumerate pre-defined references.
                // always call first to ensure any pushed-back references are returned first.
                var reference = base.Next();

                if (reference != null)
                {
                    return reference;
                }

                if (_stage == Stage.Begin)
                {
                    _stage = Stage.Tags;
                    _position = 0;
                }

                // don't start browsing huge number of references when only internal references are requested.
                if (InternalOnly)
                {
                    return null;
                }

                // enumerate tags.
                if (_stage == Stage.Tags && IsRequired(ReferenceTypeIds.Organizes, false))
                {
                    reference = NextChild();

                    if (reference != null)
                    {
                        return reference;
                    }
                }

                // all done.
                return null;
            }
        }

        /// <summary>
        /// Returns the next child.
        /// </summary>
        private NodeStateReference NextChild()
        {
            _ = (UnderlyingSystem)SystemContext.SystemHandle;

            NodeId targetId;

            // check if a specific browse name is requested.
            if (!QualifiedName.IsNull(BrowseName))
            {
                // browse name must be qualified by the correct namespace.
                if (_parent.BrowseName.NamespaceIndex != BrowseName.NamespaceIndex)
                {
                    return null;
                }

                // parse the browse name.
                var index = 0;

                for (var ii = 0; ii < BrowseName.Name.Length; ii++)
                {
                    var ch = BrowseName.Name[ii];

                    if (!char.IsDigit(ch))
                    {
                        return null;
                    }

                    index *= 10;
                    index += Convert.ToInt32(ch - '0');
                }

                // check for valid browse name.
                if (index < 0 || index > _parent.Register.Size)
                {
                    return null;
                }

                // return target.
                targetId = ModelUtils.GetRegisterVariableId(_parent.Register, index, _parent.NodeId.NamespaceIndex);
            }

            // return the child at the next position.
            else
            {
                // look for next segment.
                if (_position >= _parent.Register.Size)
                {
                    return null;
                }

                // return target.
                targetId = ModelUtils.GetRegisterVariableId(_parent.Register, _position, _parent.NodeId.NamespaceIndex);
                _position++;
            }

            // create reference.
            if (targetId != null)
            {
                return new NodeStateReference(ReferenceTypeIds.Organizes, false, targetId);
            }

            return null;
        }

        /// <summary>
        /// The stages available in a browse operation.
        /// </summary>
        private enum Stage
        {
            Begin,
            Tags,
            Done
        }

        private Stage _stage;
        private int _position;
        private readonly MemoryRegisterState _parent;
    }
}
