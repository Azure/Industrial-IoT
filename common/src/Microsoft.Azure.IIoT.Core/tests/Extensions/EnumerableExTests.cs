// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace System.Collections.Generic {
    using Xunit;
    using System.Text;

    public class EnumerableExTests {

        [Fact]
        public void SequenceEqualsReturnsFalseWhenListSubjectNull() {
            List<string> test1 = null;
            var test2 = new List<string> { "serf", "sated" };

            var result = test1.SequenceEqualsSafe(test2);
            Assert.False(result);
        }


        [Fact]
        public void SequenceEqualsReturnsFalseWhenListObjectNull() {
            var test1 = new List<string> { "serf", "sated" };
            List<string> test2 = null;

            var result = test1.SequenceEqualsSafe(test2);
            Assert.False(result);
        }

        [Fact]
        public void SequenceEqualsWReturnsTrueWhenBothListNull() {
            List<string> test1 = null;
            List<string> test2 = null;

            var result = test1.SequenceEqualsSafe(test2);
            Assert.True(result);
        }

        [Fact]
        public void SequenceEqualsReturnsFalseWhenEnumerableSubjectNull() {
            IEnumerable<string> test1 = null;
            IEnumerable<string> test2 = new List<string> { "serf", "sated" };

            var result = test1.SequenceEqualsSafe(test2);
            Assert.False(result);
        }

        [Fact]
        public void SequenceEqualsReturnsFalseWhenEnumerableObjectNull() {
            IEnumerable<string> test1 = new List<string> { "serf", "sated" };
            IEnumerable<string> test2 = null;

            var result = test1.SequenceEqualsSafe(test2);
            Assert.False(result);
        }

        [Fact]
        public void SequenceEqualsWReturnsTrueWhenBothEnumerableNull() {
            IEnumerable<string> test1 = null;
            IEnumerable<string> test2 = null;

            var result = test1.SequenceEqualsSafe(test2);
            Assert.True(result);
        }

        [Fact]
        public void SequenceEqualsReturnsFalseWhenBufferSubjectNull() {
            byte[] test1 = null;
            var test2 = Encoding.UTF8.GetBytes("testtesttesttest");

            var result = test1.SequenceEqualsSafe(test2);
            Assert.False(result);
        }

        [Fact]
        public void SequenceEqualsReturnsFalseWhenBufferObjectNull() {
            var test1 = Encoding.UTF8.GetBytes("testtesttesttest");
            byte[] test2 = null;

            var result = test1.SequenceEqualsSafe(test2);
            Assert.False(result);
        }

        [Fact]
        public void SequenceEqualsReturnsTrueWhenSequenceSame() {
            var test1 = new List<string> { "serf", "sated" };
            var test2 = new List<string> { "serf", "sated" };

            var result = test1.SequenceEqualsSafe(test2);
            Assert.True(result);
        }

        [Fact]
        public void SequenceEqualsReturnsFalseWhenSequenceNotSame1() {
            var test1 = new List<string> { "serf", "sated" };
            var test2 = new List<string> { "serf", "sated", "data" };

            var result = test1.SequenceEqualsSafe(test2);
            Assert.False(result);
        }

        [Fact]
        public void SequenceEqualsReturnsFalseWhenSequenceNotSame2() {
            var test1 = new List<string> { "serf", "sated" };
            var test2 = new List<string> { "sated", "serf" };

            var result = test1.SequenceEqualsSafe(test2);
            Assert.False(result);
        }

        [Fact]
        public void SequenceEqualsReturnsTrueWhenBufferSame() {
            var test1 = Encoding.UTF8.GetBytes("testtesttesttest");
            var test2 = Encoding.UTF8.GetBytes("testtesttesttest");

            var result = test1.SequenceEqualsSafe(test2);
            Assert.True(result);
        }

        [Fact]
        public void SequenceEqualsReturnsFalseWhenBufferNotSame() {
            var test1 = Encoding.UTF8.GetBytes("testtesttesttest");
            var test2 = Encoding.UTF8.GetBytes("testtesttesttesx");

            var result = test1.SequenceEqualsSafe(test2);
            Assert.False(result);
        }

    }
}
