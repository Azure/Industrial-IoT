/* ========================================================================
 * Copyright (c) 2005-2016 The OPC Foundation, Inc. All rights reserved.
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

using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using System.Xml;
using System.Runtime.Serialization;
using Opc.Ua;

namespace Publisher
{
    #region Method Identifiers
    /// <summary>
    /// A class that declares constants for all Methods in the Model Design.
    /// </summary>
    /// <exclude />
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Opc.Ua.ModelCompiler", "1.0.0.0")]
    public static partial class Methods
    {
        /// <summary>
        /// The identifier for the PublisherType_PublishNode Method.
        /// </summary>
        public const uint PublisherType_PublishNode = 8;

        /// <summary>
        /// The identifier for the PublisherType_UnPublishNode Method.
        /// </summary>
        public const uint PublisherType_UnPublishNode = 10;

        /// <summary>
        /// The identifier for the PublisherType_GetListOfPublishedNodes Method.
        /// </summary>
        public const uint PublisherType_GetListOfPublishedNodes = 12;

        /// <summary>
        /// The identifier for the PublisherInstance_PublishNode Method.
        /// </summary>
        public const uint PublisherInstance_PublishNode = 16;

        /// <summary>
        /// The identifier for the PublisherInstance_UnPublishNode Method.
        /// </summary>
        public const uint PublisherInstance_UnPublishNode = 18;

        /// <summary>
        /// The identifier for the PublisherInstance_GetListOfPublishedNodes Method.
        /// </summary>
        public const uint PublisherInstance_GetListOfPublishedNodes = 20;
    }
    #endregion

    #region Object Identifiers
    /// <summary>
    /// A class that declares constants for all Objects in the Model Design.
    /// </summary>
    /// <exclude />
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Opc.Ua.ModelCompiler", "1.0.0.0")]
    public static partial class Objects
    {
        /// <summary>
        /// The identifier for the PublisherInstance Object.
        /// </summary>
        public const uint PublisherInstance = 15;
    }
    #endregion

    #region ObjectType Identifiers
    /// <summary>
    /// A class that declares constants for all ObjectTypes in the Model Design.
    /// </summary>
    /// <exclude />
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Opc.Ua.ModelCompiler", "1.0.0.0")]
    public static partial class ObjectTypes
    {
        /// <summary>
        /// The identifier for the PublisherType ObjectType.
        /// </summary>
        public const uint PublisherType = 7;
    }
    #endregion

    #region Variable Identifiers
    /// <summary>
    /// A class that declares constants for all Variables in the Model Design.
    /// </summary>
    /// <exclude />
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Opc.Ua.ModelCompiler", "1.0.0.0")]
    public static partial class Variables
    {
        /// <summary>
        /// The identifier for the PublisherType_PublishNode_InputArguments Variable.
        /// </summary>
        public const uint PublisherType_PublishNode_InputArguments = 9;

        /// <summary>
        /// The identifier for the PublisherType_UnPublishNode_InputArguments Variable.
        /// </summary>
        public const uint PublisherType_UnPublishNode_InputArguments = 11;

        /// <summary>
        /// The identifier for the PublisherType_GetListOfPublishedNodes_OutputArguments Variable.
        /// </summary>
        public const uint PublisherType_GetListOfPublishedNodes_OutputArguments = 13;

        /// <summary>
        /// The identifier for the PublisherType_ConnectionString Variable.
        /// </summary>
        public const uint PublisherType_ConnectionString = 14;

        /// <summary>
        /// The identifier for the PublisherInstance_PublishNode_InputArguments Variable.
        /// </summary>
        public const uint PublisherInstance_PublishNode_InputArguments = 17;

        /// <summary>
        /// The identifier for the PublisherInstance_UnPublishNode_InputArguments Variable.
        /// </summary>
        public const uint PublisherInstance_UnPublishNode_InputArguments = 19;

        /// <summary>
        /// The identifier for the PublisherInstance_GetListOfPublishedNodes_OutputArguments Variable.
        /// </summary>
        public const uint PublisherInstance_GetListOfPublishedNodes_OutputArguments = 21;

        /// <summary>
        /// The identifier for the PublisherInstance_ConnectionString Variable.
        /// </summary>
        public const uint PublisherInstance_ConnectionString = 22;
    }
    #endregion

    #region Method Node Identifiers
    /// <summary>
    /// A class that declares constants for all Methods in the Model Design.
    /// </summary>
    /// <exclude />
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Opc.Ua.ModelCompiler", "1.0.0.0")]
    public static partial class MethodIds
    {
        /// <summary>
        /// The identifier for the PublisherType_PublishNode Method.
        /// </summary>
        public static readonly ExpandedNodeId PublisherType_PublishNode = new ExpandedNodeId(Publisher.Methods.PublisherType_PublishNode, Publisher.Namespaces.Publisher);

        /// <summary>
        /// The identifier for the PublisherType_UnPublishNode Method.
        /// </summary>
        public static readonly ExpandedNodeId PublisherType_UnPublishNode = new ExpandedNodeId(Publisher.Methods.PublisherType_UnPublishNode, Publisher.Namespaces.Publisher);

        /// <summary>
        /// The identifier for the PublisherType_GetListOfPublishedNodes Method.
        /// </summary>
        public static readonly ExpandedNodeId PublisherType_GetListOfPublishedNodes = new ExpandedNodeId(Publisher.Methods.PublisherType_GetListOfPublishedNodes, Publisher.Namespaces.Publisher);

        /// <summary>
        /// The identifier for the PublisherInstance_PublishNode Method.
        /// </summary>
        public static readonly ExpandedNodeId PublisherInstance_PublishNode = new ExpandedNodeId(Publisher.Methods.PublisherInstance_PublishNode, Publisher.Namespaces.Publisher);

        /// <summary>
        /// The identifier for the PublisherInstance_UnPublishNode Method.
        /// </summary>
        public static readonly ExpandedNodeId PublisherInstance_UnPublishNode = new ExpandedNodeId(Publisher.Methods.PublisherInstance_UnPublishNode, Publisher.Namespaces.Publisher);

        /// <summary>
        /// The identifier for the PublisherInstance_GetListOfPublishedNodes Method.
        /// </summary>
        public static readonly ExpandedNodeId PublisherInstance_GetListOfPublishedNodes = new ExpandedNodeId(Publisher.Methods.PublisherInstance_GetListOfPublishedNodes, Publisher.Namespaces.Publisher);
    }
    #endregion

    #region Object Node Identifiers
    /// <summary>
    /// A class that declares constants for all Objects in the Model Design.
    /// </summary>
    /// <exclude />
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Opc.Ua.ModelCompiler", "1.0.0.0")]
    public static partial class ObjectIds
    {
        /// <summary>
        /// The identifier for the PublisherInstance Object.
        /// </summary>
        public static readonly ExpandedNodeId PublisherInstance = new ExpandedNodeId(Publisher.Objects.PublisherInstance, Publisher.Namespaces.Publisher);
    }
    #endregion

    #region ObjectType Node Identifiers
    /// <summary>
    /// A class that declares constants for all ObjectTypes in the Model Design.
    /// </summary>
    /// <exclude />
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Opc.Ua.ModelCompiler", "1.0.0.0")]
    public static partial class ObjectTypeIds
    {
        /// <summary>
        /// The identifier for the PublisherType ObjectType.
        /// </summary>
        public static readonly ExpandedNodeId PublisherType = new ExpandedNodeId(Publisher.ObjectTypes.PublisherType, Publisher.Namespaces.Publisher);
    }
    #endregion

    #region Variable Node Identifiers
    /// <summary>
    /// A class that declares constants for all Variables in the Model Design.
    /// </summary>
    /// <exclude />
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Opc.Ua.ModelCompiler", "1.0.0.0")]
    public static partial class VariableIds
    {
        /// <summary>
        /// The identifier for the PublisherType_PublishNode_InputArguments Variable.
        /// </summary>
        public static readonly ExpandedNodeId PublisherType_PublishNode_InputArguments = new ExpandedNodeId(Publisher.Variables.PublisherType_PublishNode_InputArguments, Publisher.Namespaces.Publisher);

        /// <summary>
        /// The identifier for the PublisherType_UnPublishNode_InputArguments Variable.
        /// </summary>
        public static readonly ExpandedNodeId PublisherType_UnPublishNode_InputArguments = new ExpandedNodeId(Publisher.Variables.PublisherType_UnPublishNode_InputArguments, Publisher.Namespaces.Publisher);

        /// <summary>
        /// The identifier for the PublisherType_GetListOfPublishedNodes_OutputArguments Variable.
        /// </summary>
        public static readonly ExpandedNodeId PublisherType_GetListOfPublishedNodes_OutputArguments = new ExpandedNodeId(Publisher.Variables.PublisherType_GetListOfPublishedNodes_OutputArguments, Publisher.Namespaces.Publisher);

        /// <summary>
        /// The identifier for the PublisherType_ConnectionString Variable.
        /// </summary>
        public static readonly ExpandedNodeId PublisherType_ConnectionString = new ExpandedNodeId(Publisher.Variables.PublisherType_ConnectionString, Publisher.Namespaces.Publisher);

        /// <summary>
        /// The identifier for the PublisherInstance_PublishNode_InputArguments Variable.
        /// </summary>
        public static readonly ExpandedNodeId PublisherInstance_PublishNode_InputArguments = new ExpandedNodeId(Publisher.Variables.PublisherInstance_PublishNode_InputArguments, Publisher.Namespaces.Publisher);

        /// <summary>
        /// The identifier for the PublisherInstance_UnPublishNode_InputArguments Variable.
        /// </summary>
        public static readonly ExpandedNodeId PublisherInstance_UnPublishNode_InputArguments = new ExpandedNodeId(Publisher.Variables.PublisherInstance_UnPublishNode_InputArguments, Publisher.Namespaces.Publisher);

        /// <summary>
        /// The identifier for the PublisherInstance_GetListOfPublishedNodes_OutputArguments Variable.
        /// </summary>
        public static readonly ExpandedNodeId PublisherInstance_GetListOfPublishedNodes_OutputArguments = new ExpandedNodeId(Publisher.Variables.PublisherInstance_GetListOfPublishedNodes_OutputArguments, Publisher.Namespaces.Publisher);

        /// <summary>
        /// The identifier for the PublisherInstance_ConnectionString Variable.
        /// </summary>
        public static readonly ExpandedNodeId PublisherInstance_ConnectionString = new ExpandedNodeId(Publisher.Variables.PublisherInstance_ConnectionString, Publisher.Namespaces.Publisher);
    }
    #endregion

    #region BrowseName Declarations
    /// <summary>
    /// Declares all of the BrowseNames used in the Model Design.
    /// </summary>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Opc.Ua.ModelCompiler", "1.0.0.0")]
    public static partial class BrowseNames
    {
        /// <summary>
        /// The BrowseName for the ConnectionString component.
        /// </summary>
        public const string ConnectionString = "ConnectionString";

        /// <summary>
        /// The BrowseName for the GetListOfPublishedNodes component.
        /// </summary>
        public const string GetListOfPublishedNodes = "GetListOfPublishedNodes";

        /// <summary>
        /// The BrowseName for the PublisherInstance component.
        /// </summary>
        public const string PublisherInstance = "PublisherInstance";

        /// <summary>
        /// The BrowseName for the PublisherType component.
        /// </summary>
        public const string PublisherType = "PublisherType";

        /// <summary>
        /// The BrowseName for the PublishNode component.
        /// </summary>
        public const string PublishNode = "PublishNode";

        /// <summary>
        /// The BrowseName for the UnPublishNode component.
        /// </summary>
        public const string UnPublishNode = "UnPublishNode";
    }
    #endregion

    #region Namespace Declarations
    /// <summary>
    /// Defines constants for all namespaces referenced by the model design.
    /// </summary>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Opc.Ua.ModelCompiler", "1.0.0.0")]
    public static partial class Namespaces
    {
        /// <summary>
        /// The URI for the OpcUa namespace (.NET code namespace is 'Opc.Ua').
        /// </summary>
        public const string OpcUa = "http://opcfoundation.org/UA/";

        /// <summary>
        /// The URI for the OpcUaXsd namespace (.NET code namespace is 'Opc.Ua').
        /// </summary>
        public const string OpcUaXsd = "http://opcfoundation.org/UA/2008/02/Types.xsd";

        /// <summary>
        /// The URI for the Publisher namespace (.NET code namespace is 'Publisher').
        /// </summary>
        public const string Publisher = "http://opcfoundation.org/UA/Publisher/";
    }
    #endregion
}