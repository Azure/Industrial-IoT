// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace IIoTPlatform_E2E_Tests.TestExtensions {
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Xunit.Abstractions;
    using Xunit.Sdk;

    /// <summary>
    /// Orderer to order use test cases based on <see cref="PriorityOrderAttribute"/>
    /// </summary>
    public class TestCaseOrderer : ITestCaseOrderer {

        /// <summary>
        /// Fullname of the TestCollectionOrderer, as constant to be used in assembly info
        /// </summary>
        public const string FullName = "IIoTPlatform_E2E_Tests.TestExtensions.TestCaseOrderer";

        /// <inheritdoc />
        public IEnumerable<TTestCase> OrderTestCases<TTestCase>(IEnumerable<TTestCase> testCases) where TTestCase : ITestCase {
            var sortedMethods = new SortedDictionary<uint, List<TTestCase>>();

            foreach (var testCase in testCases) {
                uint order = 0;

                foreach (var attr in testCase.TestMethod.Method.GetCustomAttributes((typeof(PriorityOrderAttribute).AssemblyQualifiedName))) {
                    order = attr.GetNamedArgument<uint>("Order");
                }


                GetOrCreate(sortedMethods, order).Add(testCase);
            }

            foreach (var list in sortedMethods.Keys.Select(priority => sortedMethods[priority])) {
                list.Sort((x, y) => StringComparer.OrdinalIgnoreCase.Compare(x.TestMethod.Method.Name, y.TestMethod.Method.Name));
                foreach (var testCase in list) {
                    yield return testCase;
                }
            }
        }

        private TValue GetOrCreate<TKey, TValue>(IDictionary<TKey, TValue> dictionary, TKey key) where TValue : new() {

            if (dictionary.TryGetValue(key, out var result)) {
                return result;
            }

            result = new TValue();
            dictionary[key] = result;

            return result;
        }
    }
}
