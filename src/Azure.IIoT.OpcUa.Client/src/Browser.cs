/* ========================================================================
 * Copyright (c) 2005-2020 The OPC Foundation, Inc. All rights reserved.
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
    using System.Runtime.Serialization;

    /// <summary>
    /// Stores the options to use for a browse operation.
    /// </summary>
    [DataContract(Namespace = Namespaces.OpcUaXsd)]
    public class Browser
    {
        /// <summary>
        /// Creates an unattached instance of a browser.
        /// </summary>
        public Browser()
        {
            Initialize();
        }

        /// <summary>
        /// Creates new instance of a browser and attaches it to a session.
        /// </summary>
        /// <param name="session"></param>
        public Browser(ISession session)
        {
            Initialize();
            m_session = session;
        }

        /// <summary>
        /// Creates a copy of a browser.
        /// </summary>
        /// <param name="template"></param>
        public Browser(Browser template)
        {
            Initialize();

            if (template != null)
            {
                m_session = template.m_session;
                m_view = template.m_view;
                m_maxReferencesReturned = template.m_maxReferencesReturned;
                m_browseDirection = template.m_browseDirection;
                m_referenceTypeId = template.m_referenceTypeId;
                m_includeSubtypes = template.m_includeSubtypes;
                m_nodeClassMask = template.m_nodeClassMask;
                m_resultMask = template.m_resultMask;
                m_continueUntilDone = template.m_continueUntilDone;
            }
        }

        /// <summary>
        /// Sets all private fields to default values.
        /// </summary>
        private void Initialize()
        {
            m_session = null;
            m_view = null;
            m_maxReferencesReturned = 0;
            m_browseDirection = Opc.Ua.BrowseDirection.Forward;
            m_referenceTypeId = null;
            m_includeSubtypes = true;
            m_nodeClassMask = 0;
            m_resultMask = (uint)BrowseResultMask.All;
            m_continueUntilDone = false;
            m_browseInProgress = false;
        }

        /// <summary>
        /// The session that the browse is attached to.
        /// </summary>
        public ISession Session
        {
            get { return m_session; }

            set
            {
                CheckBrowserState();
                m_session = value;
            }
        }

        /// <summary>
        /// The view to use for the browse operation.
        /// </summary>
        [DataMember(Order = 1)]
        public ViewDescription View
        {
            get { return m_view; }

            set
            {
                CheckBrowserState();
                m_view = value;
            }
        }

        /// <summary>
        /// The maximum number of references to return in a single browse operation.
        /// </summary>
        [DataMember(Order = 2)]
        public uint MaxReferencesReturned
        {
            get { return m_maxReferencesReturned; }

            set
            {
                CheckBrowserState();
                m_maxReferencesReturned = value;
            }
        }

        /// <summary>
        /// The direction to browse.
        /// </summary>
        [DataMember(Order = 3)]
        public BrowseDirection BrowseDirection
        {
            get { return m_browseDirection; }

            set
            {
                CheckBrowserState();
                m_browseDirection = value;
            }
        }

        /// <summary>
        /// The reference type to follow.
        /// </summary>
        [DataMember(Order = 4)]
        public NodeId ReferenceTypeId
        {
            get { return m_referenceTypeId; }

            set
            {
                CheckBrowserState();
                m_referenceTypeId = value;
            }
        }

        /// <summary>
        /// Whether subtypes of the reference type should be included.
        /// </summary>
        [DataMember(Order = 5)]
        public bool IncludeSubtypes
        {
            get { return m_includeSubtypes; }

            set
            {
                CheckBrowserState();
                m_includeSubtypes = value;
            }
        }

        /// <summary>
        /// The classes of the target nodes.
        /// </summary>
        [DataMember(Order = 6)]
        public int NodeClassMask
        {
            get { return Utils.ToInt32(m_nodeClassMask); }

            set
            {
                CheckBrowserState();
                m_nodeClassMask = Utils.ToUInt32(value);
            }
        }

        /// <summary>
        /// The results to return.
        /// </summary>
        [DataMember(Order = 6)]
        public uint ResultMask
        {
            get { return m_resultMask; }

            set
            {
                CheckBrowserState();
                m_resultMask = value;
            }
        }

        /// <summary>
        /// Whether subsequent continuation points should be processed automatically.
        /// </summary>
        public bool ContinueUntilDone
        {
            get { return m_continueUntilDone; }

            set
            {
                CheckBrowserState();
                m_continueUntilDone = value;
            }
        }

        /// <summary>
        /// Checks the state of the browser.
        /// </summary>
        /// <exception cref="ServiceResultException"></exception>
        private void CheckBrowserState()
        {
            if (m_browseInProgress)
            {
                throw new ServiceResultException(StatusCodes.BadInvalidState, "Cannot change browse parameters while a browse operation is in progress.");
            }
        }

        private ISession m_session;
        private ViewDescription m_view;
        private uint m_maxReferencesReturned;
        private BrowseDirection m_browseDirection;
        private NodeId m_referenceTypeId;
        private bool m_includeSubtypes;
        private uint m_nodeClassMask;
        private uint m_resultMask;
        private bool m_continueUntilDone;
        private bool m_browseInProgress;
    }
}
