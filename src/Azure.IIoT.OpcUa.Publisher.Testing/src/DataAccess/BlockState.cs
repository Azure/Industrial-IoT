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

namespace DataAccess
{
    using Opc.Ua;
    using Opc.Ua.Server;
    using System.Collections.Generic;

    /// <summary>
    /// A object which maps a block to a UA object.
    /// </summary>
    public class BlockState : BaseObjectState
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="BlockState"/> class.
        /// </summary>
        /// <param name="nodeManager">The context.</param>
        /// <param name="nodeId">The node id.</param>
        /// <param name="block">The block.</param>
        public BlockState(
            DataAccessNodeManager nodeManager,
            NodeId nodeId,
            UnderlyingSystemBlock block) : base(null)
        {
            _blockId = block.Id;
            _nodeManager = nodeManager;

            SymbolicName = block.Name;
            NodeId = nodeId;
            BrowseName = new QualifiedName(block.Name, nodeId.NamespaceIndex);
            DisplayName = new LocalizedText(block.Name);
            Description = null;
            WriteMask = 0;
            UserWriteMask = 0;
            EventNotifier = EventNotifiers.None;

            if (nodeManager.SystemContext.SystemHandle is UnderlyingSystem system)
            {
                var tags = block.GetTags();

                for (var ii = 0; ii < tags.Count; ii++)
                {
                    var variable = CreateVariable(nodeManager.SystemContext, tags[ii]);
                    AddChild(variable);
                    variable.OnSimpleWriteValue = OnWriteTagValue;
                }
            }
        }

        /// <summary>
        /// Starts the monitoring the block.
        /// </summary>
        /// <param name="context">The context.</param>
        public void StartMonitoring(ServerSystemContext context)
        {
            if (_monitoringCount == 0 && context.SystemHandle is UnderlyingSystem system)
            {
                var block = system.FindBlock(_blockId);

                block?.StartMonitoring(OnTagsChanged);
            }

            _monitoringCount++;
        }

        /// <summary>
        /// Stop the monitoring the block.
        /// </summary>
        /// <param name="context">The context.</param>
        public bool StopMonitoring(ServerSystemContext context)
        {
            _monitoringCount--;

            if (_monitoringCount == 0 && context.SystemHandle is UnderlyingSystem system)
            {
                var block = system.FindBlock(_blockId);

                block?.StopMonitoring();
            }

            return _monitoringCount != 0;
        }

        /// <summary>
        /// Used to receive notifications when the value attribute is read or written.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="node"></param>
        /// <param name="value"></param>
        public ServiceResult OnWriteTagValue(
            ISystemContext context,
            NodeState node,
            ref object value)
        {
            if (context.SystemHandle is not UnderlyingSystem system)
            {
                return StatusCodes.BadCommunicationError;
            }

            var block = system.FindBlock(_blockId);

            if (block == null)
            {
                return StatusCodes.BadNodeIdUnknown;
            }

            var error = block.WriteTagValue(node.SymbolicName, value);

            if (error != 0)
            {
                // the simulator uses UA status codes so there is no need for a mapping table.
                return error;
            }

            return ServiceResult.Good;
        }

        /// <summary>
        /// Called when one or more tags changes.
        /// </summary>
        /// <param name="tags">The tags.</param>
        private void OnTagsChanged(IList<UnderlyingSystemTag> tags)
        {
            lock (_nodeManager.Lock)
            {
                for (var ii = 0; ii < tags.Count; ii++)
                {
                    if (FindChildBySymbolicName(_nodeManager.SystemContext, tags[ii].Name) is BaseVariableState variable)
                    {
                        UpdateVariable(_nodeManager.SystemContext, tags[ii], variable);
                    }
                }

                ClearChangeMasks(_nodeManager.SystemContext, true);
            }
        }

        /// <summary>
        /// Populates the browser with references that meet the criteria.
        /// </summary>
        /// <param name="context">The context for the system being accessed.</param>
        /// <param name="browser">The browser to populate.</param>
        protected override void PopulateBrowser(ISystemContext context, NodeBrowser browser)
        {
            base.PopulateBrowser(context, browser);

            // check if the parent segments need to be returned.
            if (browser.IsRequired(ReferenceTypeIds.Organizes, true))
            {
                if (context.SystemHandle is not UnderlyingSystem system)
                {
                    return;
                }

                // add reference for each segment.
                var segments = system.FindSegmentsForBlock(_blockId);

                for (var ii = 0; ii < segments.Count; ii++)
                {
                    browser.Add(ReferenceTypeIds.Organizes, true, ModelUtils.ConstructIdForSegment(segments[ii].Id, NodeId.NamespaceIndex));
                }
            }
        }

        /// <summary>
        /// Creates a variable from a tag.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="tag">The tag.</param>
        /// <returns>The variable that represents the tag.</returns>
        private BaseDataVariableState CreateVariable(ISystemContext context, UnderlyingSystemTag tag)
        {
            // create the variable type based on the tag type.
            BaseDataVariableState variable;
            switch (tag.TagType)
            {
                case UnderlyingSystemTagType.Analog:
                    {
                        var node = new AnalogItemState(this);

                        if (tag.EngineeringUnits != null)
                        {
                            node.EngineeringUnits = new PropertyState<EUInformation>(node);
                        }

                        if (tag.EuRange.Length >= 4)
                        {
                            node.InstrumentRange = new PropertyState<Range>(node);
                        }

                        variable = node;
                        break;
                    }

                case UnderlyingSystemTagType.Digital:
                    {
                        variable = new TwoStateDiscreteState(this);
                        break;
                    }

                case UnderlyingSystemTagType.Enumerated:
                    {
                        var node = new MultiStateDiscreteState(this);

                        if (tag.Labels != null)
                        {
                            node.EnumStrings = new PropertyState<LocalizedText[]>(node);
                        }

                        variable = node;
                        break;
                    }

                default:
                    {
                        variable = new DataItemState(this);
                        break;
                    }
            }

            // set the symbolic name and reference types.
            variable.SymbolicName = tag.Name;
            variable.ReferenceTypeId = ReferenceTypeIds.HasComponent;

            // initialize the variable from the type model.
            variable.Create(
                context,
                null,
                new QualifiedName(tag.Name, BrowseName.NamespaceIndex),
                null,
                true);

            // update the variable values.
            UpdateVariable(context, tag, variable);

            return variable;
        }

        /// <summary>
        /// Updates a variable from a tag.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="tag">The tag.</param>
        /// <param name="variable">The variable to update.</param>
        private void UpdateVariable(ISystemContext context, UnderlyingSystemTag tag, BaseVariableState variable)
        {
            System.Diagnostics.Contracts.Contract.Assume(context != null);
            variable.Description = tag.Description;
            variable.Value = tag.Value;
            variable.Timestamp = tag.Timestamp;

            switch (tag.DataType)
            {
                case UnderlyingSystemDataType.Integer1: { variable.DataType = DataTypes.SByte; break; }
                case UnderlyingSystemDataType.Integer2: { variable.DataType = DataTypes.Int16; break; }
                case UnderlyingSystemDataType.Integer4: { variable.DataType = DataTypes.Int32; break; }
                case UnderlyingSystemDataType.Real4: { variable.DataType = DataTypes.Float; break; }
                case UnderlyingSystemDataType.String: { variable.DataType = DataTypes.String; break; }
            }

            variable.ValueRank = ValueRanks.Scalar;
            variable.ArrayDimensions = null;

            if (tag.IsWriteable)
            {
                variable.AccessLevel = AccessLevels.CurrentReadOrWrite;
                variable.UserAccessLevel = AccessLevels.CurrentReadOrWrite;
            }
            else
            {
                variable.AccessLevel = AccessLevels.CurrentRead;
                variable.UserAccessLevel = AccessLevels.CurrentRead;
            }

            variable.MinimumSamplingInterval = MinimumSamplingIntervals.Continuous;
            variable.Historizing = false;

            switch (tag.TagType)
            {
                case UnderlyingSystemTagType.Analog:
                    {
                        var node = variable as AnalogItemState;

                        if (tag.EuRange != null)
                        {
                            if (tag.EuRange.Length >= 2 && node.EURange != null)
                            {
                                node.EURange.Value = new Range(tag.EuRange[0], tag.EuRange[1]);
                                node.EURange.Timestamp = tag.Block.Timestamp;
                            }

                            if (tag.EuRange.Length >= 4 && node.InstrumentRange != null)
                            {
                                node.InstrumentRange.Value = new Range(tag.EuRange[2], tag.EuRange[3]);
                                node.InstrumentRange.Timestamp = tag.Block.Timestamp;
                            }
                        }

                        if (!string.IsNullOrEmpty(tag.EngineeringUnits) && node.EngineeringUnits != null)
                        {
                            node.EngineeringUnits.Value = new EUInformation
                            {
                                DisplayName = tag.EngineeringUnits,
                                NamespaceUri = Namespaces.DataAccess
                            };
                            node.EngineeringUnits.Timestamp = tag.Block.Timestamp;
                        }

                        break;
                    }

                case UnderlyingSystemTagType.Digital:
                    {
                        var node = variable as TwoStateDiscreteState;

                        if (tag.Labels != null && node.TrueState != null && node.FalseState != null && tag.Labels.Length >= 2)
                        {
                            node.TrueState.Value = new LocalizedText(tag.Labels[0]);
                            node.TrueState.Timestamp = tag.Block.Timestamp;
                            node.FalseState.Value = new LocalizedText(tag.Labels[1]);
                            node.FalseState.Timestamp = tag.Block.Timestamp;
                        }

                        break;
                    }

                case UnderlyingSystemTagType.Enumerated:
                    {
                        var node = variable as MultiStateDiscreteState;

                        if (tag.Labels != null)
                        {
                            var strings = new LocalizedText[tag.Labels.Length];

                            for (var ii = 0; ii < tag.Labels.Length; ii++)
                            {
                                strings[ii] = new LocalizedText(tag.Labels[ii]);
                            }

                            node.EnumStrings.Value = strings;
                            node.EnumStrings.Timestamp = tag.Block.Timestamp;
                        }

                        break;
                    }
            }
        }

        private readonly string _blockId;
        private readonly CustomNodeManager2 _nodeManager;
        private int _monitoringCount;
    }
}
