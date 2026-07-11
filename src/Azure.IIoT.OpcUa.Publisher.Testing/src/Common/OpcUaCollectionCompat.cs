// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Opc.Ua
{
    using System.Collections.Generic;

    // TODO(Phase 4b/5): The UA-.NETStandard 2.0 stack removed the generated
    // typed "XxxCollection" classes in favour of ArrayOf<Xxx>. The IIoT test
    // servers (and their checked-in generated model code) still construct and
    // mutate these collection types. To keep them compiling without a sweeping
    // rewrite we resurrect the classic List<Xxx>-based collection types here.
    // List<T> converts implicitly to ArrayOf<T>, so instances still flow into
    // the 2.0 server/service APIs.

    /// <summary> Compat collection. </summary>
    public class StringCollection : List<string>
    {
        /// <summary> Create empty. </summary>
        public StringCollection() { }
        /// <summary> Create with capacity. </summary>
        public StringCollection(int capacity) : base(capacity) { }
        /// <summary> Create from collection. </summary>
        public StringCollection(IEnumerable<string> collection) : base(collection) { }
    }

    /// <summary> Compat collection. </summary>
    public class LocalizedTextCollection : List<LocalizedText>
    {
        /// <summary> Create empty. </summary>
        public LocalizedTextCollection() { }
        /// <summary> Create with capacity. </summary>
        public LocalizedTextCollection(int capacity) : base(capacity) { }
        /// <summary> Create from collection. </summary>
        public LocalizedTextCollection(IEnumerable<LocalizedText> collection) : base(collection) { }
    }
}
