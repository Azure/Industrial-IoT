// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("Azure.IIoT.OpcUa.Tests")]

//
// The Publisher's native PubSub bridge converts the resolved dataset metadata
// to its stack form. The converter lives beside the custom encoder that phase 8
// removes, at which point it is re-homed and this line goes away with it.
//
[assembly: InternalsVisibleTo("Azure.IIoT.OpcUa.Publisher")]
