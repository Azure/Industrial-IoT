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

namespace HistoricalAccess {
    using System;

    /// <summary>
    /// This class stores the state of a tag known to the system.
    /// </summary>
    /// <remarks>
    /// This class only stores the information about an tag that a system has. The
    /// system has no concept of the UA information model and the NodeManager must
    /// convert the information stored in this class into the UA equivalent.
    /// </remarks>
    public class UnderlyingSystemTag {

        /// <summary>
        /// The block that the tag belongs to
        /// </summary>
        /// <value>The block.</value>
        public UnderlyingSystemBlock Block { get; set; }

        /// <summary>
        /// Gets or sets the name of the tag.
        /// </summary>
        /// <value>The name of the tag.</value>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the description of the tag.
        /// </summary>
        /// <value>The description of the tag.</value>
        public string Description { get; set; }

        /// <summary>
        /// Gets or sets the engineering units for the tag.
        /// </summary>
        /// <value>The engineering units for the tag.</value>
        public string EngineeringUnits { get; set; }

        /// <summary>
        /// Gets or sets the data type for the tag.
        /// </summary>
        /// <value>The data type for the tag.</value>
        public UnderlyingSystemDataType DataType { get; set; }

        /// <summary>
        /// Gets or sets the type of the tag.
        /// </summary>
        /// <value>The type of the tag.</value>
        public UnderlyingSystemTagType TagType { get; set; }

        /// <summary>
        /// Gets or sets the value of the tag.
        /// </summary>
        /// <value>The tag value.</value>
        public object Value { get; set; }

        /// <summary>
        /// Gets or sets the timestamp for the value.
        /// </summary>
        /// <value>The timestamp for the value.</value>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the value is writeable.
        /// </summary>
        /// <value>
        /// 	<c>true</c> if the vaklue is writeable; otherwise, <c>false</c>.
        /// </value>
        public bool IsWriteable { get; set; }

        /// <summary>
        /// Gets or sets the EU ranges for the tag.
        /// </summary>
        /// <value>The EU ranges for the tag.</value>
        /// <remarks>
        /// 2 values: HighEU, LowEU
        /// 4 values: HighEU, LowEU, HighIR, LowIR
        /// </remarks>
        public double[] EuRange { get; set; }

        /// <summary>
        /// Gets or sets the labels for the tag values.
        /// </summary>
        /// <value>The labels for the tag values.</value>
        /// <remarks>
        /// Digital Tags: TrueState, FalseState
        /// Enumerated Tags: Lookup table for Value.
        /// </remarks>
        public string[] Labels { get; set; }

        /// <summary>
        /// Creates a snapshot of the tag.
        /// </summary>
        /// <returns>The snapshot.</returns>
        public UnderlyingSystemTag CreateSnapshot() {
            return (UnderlyingSystemTag)MemberwiseClone();
        }
    }
}
