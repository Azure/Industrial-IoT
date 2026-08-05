// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Module.Tests
{
    /// <summary>
    /// xUnit trait names and values used to select or exclude tests.
    /// </summary>
    public static class TestCategories
    {
        /// <summary>
        /// Name of the category trait.
        /// </summary>
        public const string Name = "Category";

        /// <summary>
        /// <para>
        /// Tests that run for minutes to hours. They are excluded from the
        /// pull request build with <c>--filter "Category!=LongRunning"</c>
        /// and run by the scheduled soak workflow with
        /// <c>--filter "Category=LongRunning"</c>.
        /// </para>
        /// </summary>
        public const string LongRunning = "LongRunning";
    }
}
