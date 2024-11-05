/* ========================================================================
 * Copyright (c) 2005-2022 The OPC Foundation, Inc. All rights reserved.
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

namespace Opc.Ua.Client
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Used to handle renews of user identity tokens before reconnect.
    /// </summary>
    /// <param name="session"></param>
    /// <param name="identity"></param>
    public delegate IUserIdentity RenewUserIdentityEventHandler(ISession session, IUserIdentity identity);

    /// <summary>
    /// The delegate used to receive keep alive notifications.
    /// </summary>
    /// <param name="session"></param>
    /// <param name="e"></param>
    public delegate void KeepAliveEventHandler(ISession session, KeepAliveEventArgs e);

    /// <summary>
    /// The delegate used to receive publish notifications.
    /// </summary>
    /// <param name="session"></param>
    /// <param name="e"></param>
    public delegate void NotificationEventHandler(ISession session, NotificationEventArgs e);

    /// <summary>
    /// The delegate used to receive pubish error notifications.
    /// </summary>
    /// <param name="session"></param>
    /// <param name="e"></param>
    public delegate void PublishErrorEventHandler(ISession session, PublishErrorEventArgs e);

    /// <summary>
    /// The delegate used to modify publish response sequence numbers to acknowledge.
    /// </summary>
    /// <param name="session"></param>
    /// <param name="e"></param>
    public delegate void PublishSequenceNumbersToAcknowledgeEventHandler(ISession session, PublishSequenceNumbersToAcknowledgeEventArgs e);

    /// <summary>
    /// Manages a session with a server.
    /// </summary>
    public interface ISession : ISessionClient
    {
        /// <summary>
        /// Raised when a keep alive arrives from the server or an error is detected.
        /// </summary>
        /// <remarks>
        /// Once a session is created a timer will periodically read the server state and current time.
        /// If this read operation succeeds this event will be raised each time the keep alive period elapses.
        /// If an error is detected (KeepAliveStopped == true) then this event will be raised as well.
        /// </remarks>
        event KeepAliveEventHandler KeepAlive;

        /// <summary>
        /// Raised when an exception occurs while processing a publish response.
        /// </summary>
        /// <remarks>
        /// Exceptions in a publish response are not necessarily fatal and the Session will
        /// attempt to recover by issuing Republish requests if missing messages are detected.
        /// That said, timeout errors may be a symptom of a OperationTimeout that is too short
        /// when compared to the shortest PublishingInterval/KeepAliveCount amount the current
        /// Subscriptions. The OperationTimeout should be twice the minimum value for
        /// PublishingInterval*KeepAliveCount.
        /// </remarks>
        event PublishErrorEventHandler PublishError;

        /// <summary>
        /// Raised when a publish request is about to acknowledge sequence numbers.
        /// </summary>
        /// <remarks>
        /// If the client chose to defer acknowledge of sequence numbers, it is responsible
        /// to transfer these <see cref="SubscriptionAcknowledgement"/> to the deferred list.
        /// </remarks>
        event PublishSequenceNumbersToAcknowledgeEventHandler PublishSequenceNumbersToAcknowledge;
        /// <summary>
        /// Raised to indicate the session configuration changed.
        /// </summary>
        /// <remarks>
        /// An example for a session configuration change is a new user identity,
        /// a new server nonce, a new locale etc.
        /// </remarks>
        event EventHandler SessionConfigurationChanged;

        /// <summary>
        /// The factory which was used to create the session.
        /// </summary>
        ISessionFactory SessionFactory { get; }

        /// <summary>
        /// Gets the endpoint used to connect to the server.
        /// </summary>
        ConfiguredEndpoint ConfiguredEndpoint { get; }

        /// <summary>
        /// Gets the name assigned to the session.
        /// </summary>
        string SessionName { get; }

        /// <summary>
        /// Gets the period for wich the server will maintain the session if there is no communication from the client.
        /// </summary>
        double SessionTimeout { get; }

        /// <summary>
        /// Gets the local handle assigned to the session.
        /// </summary>
        object Handle { get; }

        /// <summary>
        /// Gets the user identity currently used for the session.
        /// </summary>
        IUserIdentity Identity { get; }

        /// <summary>
        /// Gets the table of namespace uris known to the server.
        /// </summary>
        NamespaceTable NamespaceUris { get; }

        /// <summary>
        /// Gest the table of remote server uris known to the server.
        /// </summary>
        StringTable ServerUris { get; }

        /// <summary>
        /// Gets the system context for use with the session.
        /// </summary>
        ISystemContext SystemContext { get; }

        /// <summary>
        /// Gets the factory used to create encodeable objects that the server understands.
        /// </summary>
        IEncodeableFactory Factory { get; }

        /// <summary>
        /// Gets the cache of the server's type tree.
        /// </summary>
        ITypeTable TypeTree { get; }

        /// <summary>
        /// Gets the cache of nodes fetched from the server.
        /// </summary>
        INodeCache NodeCache { get; }

        /// <summary>
        /// Gets the locales that the server should use when returning localized text.
        /// </summary>
        StringCollection PreferredLocales { get; }

        /// <summary>
        /// Gets the subscriptions owned by the session.
        /// </summary>
        IEnumerable<Subscription> Subscriptions { get; }

        /// <summary>
        /// Gets the number of subscriptions owned by the session.
        /// </summary>
        int SubscriptionCount { get; }

        /// <summary>
        /// If the subscriptions are deleted when a session is closed.
        /// </summary>
        bool DeleteSubscriptionsOnClose { get; set; }

        /// <summary>
        /// Gets or Sets the default subscription for the session.
        /// </summary>
        Subscription DefaultSubscription { get; set; }

        /// <summary>
        /// Gets or Sets how frequently the server is pinged to see if communication is still working.
        /// </summary>
        /// <remarks>
        /// This interval controls how much time elaspes before a communication error is detected.
        /// If everything is ok the KeepAlive event will be raised each time this period elapses.
        /// </remarks>
        int KeepAliveInterval { get; set; }

        /// <summary>
        /// Returns true if the session is not receiving keep alives.
        /// </summary>
        /// <remarks>
        /// Set to true if the server does not respond for 2 times the KeepAliveInterval.
        /// Set to false is communication recovers.
        /// </remarks>
        bool KeepAliveStopped { get; }

        /// <summary>
        /// Gets the TickCount in ms of the last keep alive based on <see cref="HiResClock.TickCount"/>.
        /// </summary>
        int LastKeepAliveTickCount { get; }

        /// <summary>
        /// Gets the number of outstanding publish or keep alive requests.
        /// </summary>
        int OutstandingRequestCount { get; }

        /// <summary>
        /// Gets the number of outstanding publish or keep alive requests which appear to be missing.
        /// </summary>
        int DefunctRequestCount { get; }

        /// <summary>
        /// Gets the number of good outstanding publish requests.
        /// </summary>
        int GoodPublishRequestCount { get; }

        /// <summary>
        /// Gets and sets the minimum number of publish requests to be used in the session.
        /// </summary>
        int MinPublishRequestCount { get; set; }

        /// <summary>
        /// Gets and sets the maximum number of publish requests to be used in the session.
        /// </summary>
        int MaxPublishRequestCount { get; set; }

        /// <summary>
        /// Stores the operation limits of a OPC UA Server.
        /// </summary>
        OperationLimits OperationLimits { get; }

        /// <summary>
        /// If the subscriptions are transferred when a session is reconnected.
        /// </summary>
        /// <remarks>
        /// Default <c>false</c>, set to <c>true</c> if subscriptions should
        /// be transferred after reconnect. Service must be supported by server.
        /// </remarks>
        bool TransferSubscriptionsOnReconnect { get; set; }

        /// <summary>
        /// Whether the endpoint Url domain is checked in the certificate.
        /// </summary>
        bool CheckDomain { get; }

        /// <summary>
        /// gets or set the policy which is used to prevent the allocation of too many
        /// Continuation Points in the ManagedBrowse(Async) methods
        /// </summary>
        ContinuationPointPolicy ContinuationPointPolicy { get; set; }

        /// <summary>
        /// Raised before a reconnect operation completes.
        /// </summary>
        event RenewUserIdentityEventHandler RenewUserIdentity;

        /// <summary>
        /// Reconnects to the server after a network failure.
        /// </summary>
        /// <param name="ct"></param>
        Task ReconnectAsync(CancellationToken ct = default);

        /// <summary>
        /// Reconnects to the server after a network failure using a waiting connection.
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="ct"></param>
        Task ReconnectAsync(ITransportWaitingConnection connection, CancellationToken ct = default);

        /// <summary>
        /// Updates the cache with the types and its subtypes.
        /// </summary>
        /// <param name="typeIds"></param>
        /// <remarks>
        /// This method can be used to ensure the TypeTree is populated.
        /// </remarks>
        void FetchTypeTree(ExpandedNodeIdCollection typeIds);

        /// <summary>
        /// Updates the local copy of the server's namespace uri and server uri tables.
        /// </summary>
        /// <param name="ct">The cancellation token.</param>
        Task FetchNamespaceTablesAsync(CancellationToken ct = default);

        /// <summary>
        /// Updates the cache with the types and its subtypes.
        /// </summary>
        /// <param name="typeIds"></param>
        /// <param name="ct"></param>
        /// <remarks>
        /// This method can be used to ensure the TypeTree is populated.
        /// </remarks>
        Task FetchTypeTreeAsync(ExpandedNodeIdCollection typeIds, CancellationToken ct = default);

        /// <summary>
        /// Loads all dictionaries of the OPC binary or Xml schema type system.
        /// </summary>
        /// <param name="dataTypeSystem">The type system.</param>
        /// <param name="ct"></param>
        Task<Dictionary<NodeId, DataDictionary>> LoadDataTypeSystem(
            NodeId dataTypeSystem = null,
            CancellationToken ct = default);

        /// <summary>
        /// Reads the values for the node attributes and returns a node object.
        /// </summary>
        /// <param name="nodeId">The nodeId.</param>
        Node ReadNode(NodeId nodeId);

        /// <summary>
        /// Reads the values for the node attributes and returns a node object.
        /// </summary>
        /// <remarks>
        /// If the nodeclass is known, only the supported attribute values are read.
        /// </remarks>
        /// <param name="nodeId">The nodeId.</param>
        /// <param name="nodeClass">The nodeclass of the node to read.</param>
        /// <param name="optionalAttributes">Read optional attributes.</param>
        Node ReadNode(NodeId nodeId, NodeClass nodeClass, bool optionalAttributes = true);

        /// <summary>
        /// Reads the values for the node attributes and returns a node object.
        /// Reads the nodeclass of the nodeIds, then reads
        /// the values for the node attributes and returns a node object collection.
        /// </summary>
        /// <param name="nodeIds">The nodeId collection.</param>
        /// <param name="nodeCollection">The node collection read from the server.</param>
        /// <param name="errors">The errors occured reading the nodes.</param>
        /// <param name="optionalAttributes">Set to <c>true</c> if optional attributes should not be omitted.</param>
        void ReadNodes(IList<NodeId> nodeIds, out IList<Node> nodeCollection, out IList<ServiceResult> errors, bool optionalAttributes = false);

        /// <summary>
        /// Reads the values for a node collection. Returns diagnostic errors.
        /// </summary>
        /// <param name="nodeIds">The node Id.</param>
        /// <param name="values">The data values read from the server.</param>
        /// <param name="errors">The errors reported by the server.</param>
        void ReadValues(IList<NodeId> nodeIds, out DataValueCollection values, out IList<ServiceResult> errors);

        /// <summary>
        /// Fetches all references for the specified node.
        /// </summary>
        /// <param name="nodeId">The node id.</param>
        ReferenceDescriptionCollection FetchReferences(NodeId nodeId);

        /// <summary>
        /// Fetches all references for the specified nodes.
        /// </summary>
        /// <param name="nodeIds">The node id collection.</param>
        /// <param name="referenceDescriptions">A list of reference collections.</param>
        /// <param name="errors">The errors reported by the server.</param>
        void FetchReferences(IList<NodeId> nodeIds, out IList<ReferenceDescriptionCollection> referenceDescriptions, out IList<ServiceResult> errors);

        /// <summary>
        /// Fetches all references for the specified node.
        /// </summary>
        /// <param name="nodeId">The node id.</param>
        /// <param name="ct"></param>
        Task<ReferenceDescriptionCollection> FetchReferencesAsync(NodeId nodeId, CancellationToken ct);

        /// <summary>
        /// Fetches all references for the specified nodes.
        /// </summary>
        /// <param name="nodeIds">The node id collection.</param>
        /// <param name="ct"></param>
        /// <returns>A list of reference collections and the errors reported by the server.</returns>
        Task<(IList<ReferenceDescriptionCollection>, IList<ServiceResult>)> FetchReferencesAsync(IList<NodeId> nodeIds, CancellationToken ct);

        /// <summary>
        /// Establishes a session with the server.
        /// </summary>
        /// <param name="sessionName">The name to assign to the session.</param>
        /// <param name="sessionTimeout">The session timeout.</param>
        /// <param name="identity">The user identity.</param>
        /// <param name="preferredLocales">The list of preferred locales.</param>
        /// <param name="checkDomain">If set to <c>true</c> then the domain in the certificate must match the endpoint used.</param>
        /// <param name="ct">The cancellation token.</param>
        Task OpenAsync(string sessionName, uint sessionTimeout, IUserIdentity identity, IList<string> preferredLocales, bool checkDomain, CancellationToken ct);

        /// <summary>
        /// Reads the values for the node attributes and returns a node object collection.
        /// </summary>
        /// <remarks>
        /// If the nodeclass for the nodes in nodeIdCollection is already known
        /// and passed as nodeClass, reads only values of required attributes.
        /// Otherwise NodeClass.Unspecified should be used.
        /// </remarks>
        /// <param name="nodeIds">The nodeId collection to read.</param>
        /// <param name="nodeClass">The nodeClass of all nodes in the collection. Set to <c>NodeClass.Unspecified</c> if the nodeclass is unknown.</param>
        /// <param name="optionalAttributes">Set to <c>true</c> if optional attributes should not be omitted.</param>
        /// <param name="ct">The cancellation token.</param>
        /// <returns>The node collection and associated errors.</returns>
        Task<(IList<Node>, IList<ServiceResult>)> ReadNodesAsync(IList<NodeId> nodeIds, NodeClass nodeClass, bool optionalAttributes = false, CancellationToken ct = default);

        /// <summary>
        /// Reads the value for a node.
        /// </summary>
        /// <param name="nodeId">The node Id.</param>
        /// <param name="ct">The cancellation token for the request.</param>
        Task<DataValue> ReadValueAsync(NodeId nodeId, CancellationToken ct = default);

        /// <summary>
        /// Reads the values for the node attributes and returns a node object.
        /// </summary>
        /// <param name="nodeId">The nodeId.</param>
        /// <param name="ct">The cancellation token for the request.</param>
        Task<Node> ReadNodeAsync(NodeId nodeId, CancellationToken ct = default);

        /// <summary>
        /// Reads the values for the node attributes and returns a node object.
        /// </summary>
        /// <remarks>
        /// If the nodeclass is known, only the supported attribute values are read.
        /// </remarks>
        /// <param name="nodeId">The nodeId.</param>
        /// <param name="nodeClass">The nodeclass of the node to read.</param>
        /// <param name="optionalAttributes">Read optional attributes.</param>
        /// <param name="ct">The cancellation token for the request.</param>
        Task<Node> ReadNodeAsync(NodeId nodeId, NodeClass nodeClass, bool optionalAttributes = true, CancellationToken ct = default);

        /// <summary>
        /// Reads the values for the node attributes and returns a node object collection.
        /// Reads the nodeclass of the nodeIds, then reads
        /// the values for the node attributes and returns a node collection.
        /// </summary>
        /// <param name="nodeIds">The nodeId collection.</param>
        /// <param name="optionalAttributes">If optional attributes to read.</param>
        /// <param name="ct">The cancellation token.</param>
        Task<(IList<Node>, IList<ServiceResult>)> ReadNodesAsync(IList<NodeId> nodeIds, bool optionalAttributes = false, CancellationToken ct = default);

        /// <summary>
        /// Reads the values for a node collection. Returns diagnostic errors.
        /// </summary>
        /// <param name="nodeIds">The node Id.</param>
        /// <param name="ct">The cancellation token for the request.</param>
        Task<(DataValueCollection, IList<ServiceResult>)> ReadValuesAsync(IList<NodeId> nodeIds, CancellationToken ct = default);

        /// <summary>
        /// Disconnects from the server and frees any network resources with the specified timeout.
        /// </summary>
        /// <param name="timeout"></param>
        /// <param name="closeChannel"></param>
        StatusCode Close(int timeout, bool closeChannel);

        /// <summary>
        /// Close the session with the server and optionally closes the channel.
        /// </summary>
        /// <param name="closeChannel"></param>
        /// <param name="ct"></param>
        Task<StatusCode> CloseAsync(bool closeChannel, CancellationToken ct = default);

        /// <summary>
        /// Disconnects from the server and frees any network resources with the specified timeout.
        /// </summary>
        /// <param name="timeout"></param>
        /// <param name="closeChannel"></param>
        /// <param name="ct"></param>
        Task<StatusCode> CloseAsync(int timeout, bool closeChannel, CancellationToken ct = default);

        /// <summary>
        /// Adds a subscription to the session.
        /// </summary>
        /// <param name="subscription">The subscription to add.</param>
        bool AddSubscription(Subscription subscription);

        /// <summary>
        /// Removes a transferred subscription from the session.
        /// Called by the session to which the subscription
        /// is transferred to obtain ownership. Internal.
        /// </summary>
        /// <param name="subscription">The subscription to remove.</param>
        bool RemoveTransferredSubscription(Subscription subscription);

        /// <summary>
        /// Removes a subscription from the session.
        /// </summary>
        /// <param name="subscription">The subscription to remove.</param>
        /// <param name="ct">The cancellation token for the request.</param>
        Task<bool> RemoveSubscriptionAsync(Subscription subscription, CancellationToken ct = default);

        /// <summary>
        /// Removes a list of subscriptions from the session.
        /// </summary>
        /// <param name="subscriptions">The list of subscriptions to remove.</param>
        /// <param name="ct">The cancellation token for the request.</param>
        Task<bool> RemoveSubscriptionsAsync(IEnumerable<Subscription> subscriptions, CancellationToken ct = default);

        /// <summary>
        /// Transfers a list of subscriptions from another session.
        /// </summary>
        /// <param name="subscriptions">The list of subscriptions to transfer.</param>
        /// <param name="sendInitialValues">Send the last value of each monitored item in the subscriptions.</param>
        /// <param name="ct">The cancellation token for the request.</param>
        Task<bool> TransferSubscriptionsAsync(SubscriptionCollection subscriptions, bool sendInitialValues, CancellationToken ct = default);

        /// <summary>
        /// Execute browse and, if necessary, browse next in one service call.
        /// Takes care of BadNoContinuationPoint and BadInvalidContnuationPoint status codes.
        /// </summary>
        /// <param name="requestHeader"></param>
        /// <param name="view"></param>
        /// <param name="nodesToBrowse"></param>
        /// <param name="maxResultsToReturn"></param>
        /// <param name="browseDirection"></param>
        /// <param name="referenceTypeId"></param>
        /// <param name="includeSubtypes"></param>
        /// <param name="nodeClassMask"></param>
        /// <param name="result"></param>
        /// <param name="errors"></param>
        void ManagedBrowse(
            RequestHeader requestHeader,
            ViewDescription view,
            IList<NodeId> nodesToBrowse,
            uint maxResultsToReturn,
            BrowseDirection browseDirection,
            NodeId referenceTypeId,
            bool includeSubtypes,
            uint nodeClassMask,
            out IList<ReferenceDescriptionCollection> result,
            out IList<ServiceResult> errors
            );

        /// <summary>
        /// Execute BrowseAsync and, if necessary, BrowseNextAsync, in one service call.
        /// Takes care of BadNoContinuationPoint and BadInvalidContinuationPoint status codes.
        /// </summary>
        /// <param name="requestHeader"></param>
        /// <param name="view"></param>
        /// <param name="nodesToBrowse"></param>
        /// <param name="maxResultsToReturn"></param>
        /// <param name="browseDirection"></param>
        /// <param name="referenceTypeId"></param>
        /// <param name="includeSubtypes"></param>
        /// <param name="nodeClassMask"></param>
        /// <param name="ct"></param>
        Task<(
            IList<ReferenceDescriptionCollection>,
            IList<ServiceResult>
            )>
                ManagedBrowseAsync(
                RequestHeader requestHeader,
                ViewDescription view,
                IList<NodeId> nodesToBrowse,
                uint maxResultsToReturn,
                BrowseDirection browseDirection,
                NodeId referenceTypeId,
                bool includeSubtypes,
                uint nodeClassMask,
                CancellationToken ct = default
            );


        /// <summary>
        /// Calls the specified method and returns the output arguments.
        /// </summary>
        /// <param name="objectId">The NodeId of the object that provides the method.</param>
        /// <param name="methodId">The NodeId of the method to call.</param>
        /// <param name="args">The input arguments.</param>
        /// <returns>The list of output argument values.</returns>
        IList<object> Call(NodeId objectId, NodeId methodId, params object[] args);

        /// <summary>
        /// Calls the specified method and returns the output arguments.
        /// </summary>
        /// <param name="objectId">The NodeId of the object that provides the method.</param>
        /// <param name="methodId">The NodeId of the method to call.</param>
        /// <param name="ct">The cancellation token for the request.</param>
        /// <param name="args">The input arguments.</param>
        /// <returns>The list of output argument values.</returns>
        Task<IList<object>> CallAsync(NodeId objectId, NodeId methodId, CancellationToken ct = default, params object[] args);

        /// <summary>
        /// Sends an additional publish request.
        /// </summary>
        /// <param name="timeout"></param>
        IAsyncResult BeginPublish(int timeout);

        /// <summary>
        /// Create the publish requests for the active subscriptions.
        /// </summary>
        /// <param name="timeout"></param>
        /// <param name="fullQueue"></param>
        void StartPublishing(int timeout, bool fullQueue);

        /// <summary>
        /// Sends a republish request.
        /// </summary>
        /// <param name="subscriptionId"></param>
        /// <param name="sequenceNumber"></param>
        /// <param name="ct"></param>
        Task<(bool, ServiceResult)> RepublishAsync(uint subscriptionId, uint sequenceNumber, CancellationToken ct = default);
    }

    /// <summary>
    /// controls how the client treats continuation points
    /// if the server has restrictions on their number
    /// As of now only used for browse/browse next in the
    /// ManagedBrowse method.
    /// </summary>
    public enum ContinuationPointPolicy
    {
        /// <summary>
        /// Ignore how many Continuation Points are in use already.
        /// Rebrowse nodes for which BadNoContinuationPoint or
        /// BadInvalidContinuationPoint was raised. Can be used
        /// whenever the server has no restrictions no the maximum
        /// number of continuation points
        /// </summary>
        Default,

        /// <summary>
        /// Restrict the number of nodes which are browsed in a
        /// single service call to the maximum number of
        /// continuation points the server can allocae
        /// (if set to a value different from 0)
        /// </summary>
        Balanced

    }
}
