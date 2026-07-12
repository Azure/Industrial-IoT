// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Opc.Ua.Extensions
{
    using Xunit;

    public sealed class NodeStateExTests
    {
        [Fact]
        public void GetValueOrDefaultExReturnsDefaultForMissingValueTypeState()
        {
            PropertyState<uint>? state = null;

            var value = state.GetValueOrDefaultEx(42);

            Assert.Equal(42u, value);
        }

        [Fact]
        public void GetValueOrDefaultExPassesNullToReferenceTypeConverterForMissingState()
        {
            PropertyState<ArrayOf<string>>? state = null;

            var value = state.GetValueOrDefaultEx(values => values?.Count);

            Assert.Null(value);
        }
    }
}
