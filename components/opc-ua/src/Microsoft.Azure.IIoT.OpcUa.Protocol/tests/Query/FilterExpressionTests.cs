// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Protocol.Parser {
    public class FilterExpressionTests {
#if FALSE
        [Fact]
        public void ParseWhereStatement1() {
            var parser = new FilterExpression(
                "Select 's=node1'.Value, 's=node2'.Value where " +
                    "('s=node1'.Value < 5 and 's=node2'.Value = 'test') or " +
                    "('s=node1' >= 6 OR 's=node1' in [1, 2, 3])");

            Assert.NotNull(parser.WhereClause);
            Assert.NotNull(parser.SelectClause);
        }

        [Fact]
        public void ParseWhereStatement2() {
            var parser = new FilterExpression(
                "SELECT ('s=node1', '.boiler', '.Temp').Value, ('s=node2', '.Temp').Value WHERE " +
                    "('s=node1'.Value < 5 and 's=node2'.Value = 'test') OR " +
                    "('s=node1' >= 6 OR 's=node1' in [1, 2, 3]) AND 's=node3'.NodeClass = 'Object'");

            Assert.NotNull(parser.WhereClause);
            Assert.NotNull(parser.SelectClause);
        }

        [Fact]
        public void ParseWhereStatement3() {
            var parser = new FilterExpression(
                "SELECT ('s=node1', '.boiler', '.Temp').13, ('s=node2', '.Temp').2 WHERE " +
                    "('s=node1'.Value < 5 and 's=node2'.Value = 'test') OR " +
                    "('s=node1' >= 6 OR 's=node1' in [1, 2, 3]) AND 's=node3'.NodeClass = 'Object'");

            Assert.NotNull(parser.WhereClause);
            Assert.NotNull(parser.SelectClause);
        }
#endif
    }
}
