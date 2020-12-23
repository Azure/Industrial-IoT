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

namespace DataAccess {
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using Opc.Ua;

    /// <summary>
    /// An object that provides access to the underlying system.
    /// </summary>
    public class UnderlyingSystem : IDisposable {

        /// <summary>
        /// Initializes a new instance of the <see cref="UnderlyingSystem"/> class.
        /// </summary>
        public UnderlyingSystem() {
            _blocks = new Dictionary<string, UnderlyingSystemBlock>();
        }

        /// <summary>
        /// The finializer implementation.
        /// </summary>
        ~UnderlyingSystem() {
            Dispose(false);
        }

        /// <summary>
        /// Frees any unmanaged reblocks.
        /// </summary>
        public void Dispose() {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// An overrideable version of the Dispose.
        /// </summary>
        protected virtual void Dispose(bool disposing) {
            if (disposing) {
                if (_simulationTimer != null) {
                    _simulationTimer.Dispose();
                    _simulationTimer = null;
                }
            }
        }

        /// <summary>
        /// A database which stores all known block paths.
        /// </summary>
        /// <remarks>
        /// These are hardcoded for an example but the real data could come from a DB,
        /// a file or any other system accessed with a non-UA API.
        /// The name of the block is the final path element.
        /// The same block can have many paths.
        /// Each preceding element is a segment.
        /// </remarks>
        private readonly string[] s_BlockPathDatabase = {
            "Factory/East/Boiler1/Pipe1001",
            "Factory/East/Boiler1/Drum1002",
            "Factory/East/Boiler1/Pipe1002",
            "Factory/East/Boiler1/FC1001",
            "Factory/East/Boiler1/LC1001",
            "Factory/East/Boiler1/CC1001",
            "Factory/West/Boiler2/Pipe2001",
            "Factory/West/Boiler2/Drum2002",
            "Factory/West/Boiler2/Pipe2002",
            "Factory/West/Boiler2/FC2001",
            "Factory/West/Boiler2/LC2001",
            "Factory/West/Boiler2/CC2001",
            "Assets/Sensors/Flow/Pipe1001",
            "Assets/Sensors/Level/Drum1002",
            "Assets/Sensors/Flow/Pipe1002",
            "Assets/Controllers/Flow/FC1001",
            "Assets/Controllers/Level/LC1001",
            "Assets/Controllers/Custom/CC1001",
            "Assets/Sensors/Flow/Pipe2001",
            "Assets/Sensors/Level/Drum2002",
            "Assets/Sensors/Flow/Pipe2002",
            "Assets/Controllers/Flow/FC2001",
            "Assets/Controllers/Level/LC2001",
            "Assets/Controllers/Custom/CC2001",
            "TestData/Static/FC1001",
            "TestData/Static/LC1001",
            "TestData/Static/CC1001",
            "TestData/Static/FC2001",
            "TestData/Static/LC2001",
            "TestData/Static/CC2001"
        };

        /// <summary>
        /// A database which stores all known blocks.
        /// </summary>
        /// <remarks>
        /// These are hardcoded for an example but the real data could come from a DB,
        /// a file or any other system accessed with a non-UA API.
        ///
        /// The name of the block is the first element.
        /// The type of block is the second element.
        /// </remarks>
        private readonly string[] s_BlockDatabase = {
            "Pipe1001/FlowSensor",
            "Drum1002/LevelSensor",
            "Pipe1002/FlowSensor",
            "Pipe2001/FlowSensor",
            "Drum2002/LevelSensor",
            "Pipe2002/FlowSensor",
            "FC1001/Controller",
            "LC1001/Controller",
            "CC1001/CustomController",
            "FC2001/Controller",
            "LC2001/Controller",
            "CC2001/CustomController"
        };

        /// <summary>
        /// Returns the segment
        /// </summary>
        /// <param name="segmentPath">The path to the segment.</param>
        /// <returns>The segment. Null if the segment path does not exist.</returns>
        public UnderlyingSystemSegment FindSegment(string segmentPath) {
            lock (_lock) {
                // check for invalid path.
                if (string.IsNullOrEmpty(segmentPath)) {
                    return null;
                }

                // extract the seqment name from the path.
                var segmentName = segmentPath;

                var index = segmentPath.LastIndexOf('/');

                if (index != -1) {
                    segmentName = segmentName.Substring(index + 1);
                }

                if (string.IsNullOrEmpty(segmentName)) {
                    return null;
                }

                // see if there is a block path that includes the segment.
                index = segmentPath.Length;

                for (var ii = 0; ii < s_BlockPathDatabase.Length; ii++) {
                    var blockPath = s_BlockPathDatabase[ii];

                    if (index >= blockPath.Length || blockPath[index] != '/') {
                        continue;
                    }

                    // segment found - return the info.
                    if (blockPath.StartsWith(segmentPath, StringComparison.Ordinal)) {
                        var segment = new UnderlyingSystemSegment {
                            Id = segmentPath,
                            Name = segmentName,
                            SegmentType = null
                        };

                        return segment;
                    }
                }

                return null;
            }
        }

        /// <summary>
        /// Finds the segments belonging to the specified segment.
        /// </summary>
        /// <param name="segmentPath">The path to the segment to search.</param>
        /// <returns>The list of segments found. Null if the segment path does not exist.</returns>
        public IList<UnderlyingSystemSegment> FindSegments(string segmentPath) {
            lock (_lock) {
                // check for invalid path.
                if (string.IsNullOrEmpty(segmentPath)) {
                    segmentPath = string.Empty;
                }

                var segments = new Dictionary<string, UnderlyingSystemSegment>();

                // find all block paths that start with the specified segment.
                var length = segmentPath.Length;

                for (var ii = 0; ii < s_BlockPathDatabase.Length; ii++) {
                    var blockPath = s_BlockPathDatabase[ii];

                    // check for segment path prefix in block path.
                    if (length > 0) {
                        if (length >= blockPath.Length || blockPath[length] != '/') {
                            continue;
                        }

                        if (!blockPath.StartsWith(segmentPath, StringComparison.Ordinal)) {
                            continue;
                        }

                        blockPath = blockPath.Substring(length + 1);
                    }

                    // extract segment name.
                    var index = blockPath.IndexOf('/');

                    if (index != -1) {
                        var segmentName = blockPath.Substring(0, index);

                        if (!segments.ContainsKey(segmentName)) {
                            var segmentId = segmentName;

                            if (!string.IsNullOrEmpty(segmentPath)) {
                                segmentId = segmentPath;
                                segmentId += "/";
                                segmentId += segmentName;
                            }

                            var segment = new UnderlyingSystemSegment {
                                Id = segmentId,
                                Name = segmentName,
                                SegmentType = null
                            };

                            segments.Add(segmentName, segment);
                        }
                    }
                }

                // return list.
                return new List<UnderlyingSystemSegment>(segments.Values);
            }
        }

        /// <summary>
        /// Finds the blocks belonging to the specified segment.
        /// </summary>
        /// <param name="segmentPath">The path to the segment to search.</param>
        /// <returns>The list of blocks found. Null if the segment path does not exist.</returns>
        public IList<string> FindBlocks(string segmentPath) {
            lock (_lock) {
                // check for invalid path.
                if (string.IsNullOrEmpty(segmentPath)) {
                    segmentPath = string.Empty;
                }

                var blocks = new List<string>();

                // look up the segment in the "DB".
                var length = segmentPath.Length;

                for (var ii = 0; ii < s_BlockPathDatabase.Length; ii++) {
                    var blockPath = s_BlockPathDatabase[ii];

                    // check for segment path prefix in block path.
                    if (length > 0) {
                        if (length >= blockPath.Length || blockPath[length] != '/') {
                            continue;
                        }

                        if (!blockPath.StartsWith(segmentPath, StringComparison.Ordinal)) {
                            continue;
                        }

                        blockPath = blockPath.Substring(length + 1);
                    }

                    // check if there are no more segments in the path.
                    var index = blockPath.IndexOf('/');

                    if (index == -1) {
                        blockPath = blockPath.Substring(index + 1);

                        if (!blocks.Contains(blockPath)) {
                            blocks.Add(blockPath);
                        }
                    }
                }

                return blocks;
            }
        }

        /// <summary>
        /// Finds the parent segment for the specified segment.
        /// </summary>
        /// <param name="segmentPath">The segment path.</param>
        /// <returns>The segment path for the the parent.</returns>
        public UnderlyingSystemSegment FindParentForSegment(string segmentPath) {
            lock (_lock) {
                // check for invalid segment.
                var segment = FindSegment(segmentPath);

                if (segment == null) {
                    return null;
                }

                // check for root segment.
                var index = segmentPath.LastIndexOf('/');

                if (index == -1) {
                    return null;
                }

                // construct the parent.
                var parent = new UnderlyingSystemSegment {
                    Id = segmentPath.Substring(0, index),
                    SegmentType = null
                };

                index = parent.Id.LastIndexOf('/');

                if (index == -1) {
                    parent.Name = parent.Id;
                }
                else {
                    parent.Name = parent.Id.Substring(0, index);
                }


                return parent;
            }
        }

        /// <summary>
        /// Finds a block.
        /// </summary>
        /// <param name="blockId">The block identifier.</param>
        /// <returns>The block.</returns>
        public UnderlyingSystemBlock FindBlock(string blockId) {
            UnderlyingSystemBlock block = null;

            lock (_lock) {
                // check for invalid name.
                if (string.IsNullOrEmpty(blockId)) {
                    return null;
                }

                // look for cached block.
                if (_blocks.TryGetValue(blockId, out block)) {
                    return block;
                }

                // lookup block in database.
                string blockType = null;
                var length = blockId.Length;

                for (var ii = 0; ii < s_BlockDatabase.Length; ii++) {
                    blockType = s_BlockDatabase[ii];

                    if (length >= blockType.Length || blockType[length] != '/') {
                        continue;
                    }

                    if (blockType.StartsWith(blockId, StringComparison.Ordinal)) {
                        blockType = blockType.Substring(length + 1);
                        break;
                    }

                    blockType = null;
                }

                // block not found.
                if (blockType == null) {
                    return null;
                }

                // create a new block.
                block = new UnderlyingSystemBlock {

                    // create the block.
                    Id = blockId,
                    Name = blockId,
                    BlockType = blockType
                };

                _blocks.Add(blockId, block);

                // add the tags based on the block type.
                // note that the block and tag types used here are types defined by the underlying system.
                // the node manager will need to map these types to UA defined types.
                switch (block.BlockType) {
                    case "FlowSensor": {
                            block.CreateTag("Measurement", UnderlyingSystemDataType.Real4, UnderlyingSystemTagType.Analog, "liters/sec", false);
                            block.CreateTag("Online", UnderlyingSystemDataType.Integer1, UnderlyingSystemTagType.Digital, null, false);
                            break;
                        }

                    case "LevelSensor": {
                            block.CreateTag("Measurement", UnderlyingSystemDataType.Real4, UnderlyingSystemTagType.Analog, "liters", false);
                            block.CreateTag("Online", UnderlyingSystemDataType.Integer1, UnderlyingSystemTagType.Digital, null, false);
                            break;
                        }

                    case "Controller": {
                            block.CreateTag("SetPoint", UnderlyingSystemDataType.Real4, UnderlyingSystemTagType.Normal, null, true);
                            block.CreateTag("Measurement", UnderlyingSystemDataType.Real4, UnderlyingSystemTagType.Normal, null, false);
                            block.CreateTag("Output", UnderlyingSystemDataType.Real4, UnderlyingSystemTagType.Normal, null, false);
                            block.CreateTag("Status", UnderlyingSystemDataType.Integer4, UnderlyingSystemTagType.Enumerated, null, false);
                            break;
                        }

                    case "CustomController": {
                            block.CreateTag("Input1", UnderlyingSystemDataType.Real4, UnderlyingSystemTagType.Normal, null, true);
                            block.CreateTag("Input2", UnderlyingSystemDataType.Real4, UnderlyingSystemTagType.Normal, null, true);
                            block.CreateTag("Input3", UnderlyingSystemDataType.Real4, UnderlyingSystemTagType.Normal, null, true);
                            block.CreateTag("Output", UnderlyingSystemDataType.Real4, UnderlyingSystemTagType.Normal, null, false);
                            break;
                        }
                }
            }

            // return the new block.
            return block;
        }

        /// <summary>
        /// Finds the segments for block.
        /// </summary>
        /// <param name="blockId">The block id.</param>
        /// <returns>The list of segments.</returns>
        public IList<UnderlyingSystemSegment> FindSegmentsForBlock(string blockId) {
            lock (_lock) {
                // check for invalid path.
                if (string.IsNullOrEmpty(blockId)) {
                    return null;
                }

                var segments = new List<UnderlyingSystemSegment>();

                // look up the segment in the block path database.
                var length = blockId.Length;

                for (var ii = 0; ii < s_BlockPathDatabase.Length; ii++) {
                    var blockPath = s_BlockPathDatabase[ii];

                    if (length >= blockPath.Length || blockPath[blockPath.Length - length] != '/') {
                        continue;
                    }

                    if (!blockPath.EndsWith(blockId, StringComparison.Ordinal)) {
                        continue;
                    }

                    var segmentPath = blockPath.Substring(0, blockPath.Length - length - 1);

                    // construct segment.
                    var segment = new UnderlyingSystemSegment {
                        Id = segmentPath,
                        SegmentType = null
                    };

                    var index = segmentPath.LastIndexOf('/');

                    if (index == -1) {
                        segment.Name = segmentPath;
                    }
                    else {
                        segment.Name = segmentPath.Substring(0, index);
                    }

                    segments.Add(segment);
                }

                return segments;
            }
        }

        /// <summary>
        /// Starts a simulation which causes the tag states to change.
        /// </summary>
        /// <remarks>
        /// This simulation randomly activates the tags that belong to the blocks.
        /// Once an tag is active it has to be acknowledged and confirmed.
        /// Once an tag is confirmed it go to the inactive state.
        /// If the tag stays active the severity will be gradually increased.
        /// </remarks>
        public void StartSimulation() {
            lock (_lock) {
                if (_simulationTimer != null) {
                    _simulationTimer.Dispose();
                    _simulationTimer = null;
                }

                _generator = new Opc.Ua.Test.TestDataGenerator();
                _simulationTimer = new Timer(DoSimulation, null, 1000, 1000);
            }
        }

        /// <summary>
        /// Stops the simulation.
        /// </summary>
        public void StopSimulation() {
            lock (_lock) {
                if (_simulationTimer != null) {
                    _simulationTimer.Dispose();
                    _simulationTimer = null;
                }
            }
        }



        /// <summary>
        /// Simulates a block by updating the state of the tags belonging to the condition.
        /// </summary>
        private void DoSimulation(object state) {
            try {
                // get the list of blocks.
                List<UnderlyingSystemBlock> blocks = null;

                lock (_lock) {
                    _simulationCounter++;
                    blocks = new List<UnderlyingSystemBlock>(_blocks.Values);
                }

                // run simulation for each block.
                for (var ii = 0; ii < blocks.Count; ii++) {
                    blocks[ii].DoSimulation(_simulationCounter, ii, _generator);
                }
            }
            catch (Exception e) {
                Utils.Trace(e, "Unexpected error running simulation for system");
            }
        }

        private readonly object _lock = new object();
        private readonly Dictionary<string, UnderlyingSystemBlock> _blocks;
        private Timer _simulationTimer;
        private long _simulationCounter;
        private Opc.Ua.Test.TestDataGenerator _generator;
    }

    /// <summary>
    /// USed to received notifications when a tag value changes.
    /// </summary>
    internal delegate void TagValueChangedEventHandler(string tagName, Variant value, DateTime timestamp);

    /// <summary>
    /// USed to received notifications when the tag metadata changes.
    /// </summary>
    internal delegate void TagMetadataChangedEventHandler(UnderlyingSystemTag tag);
}
