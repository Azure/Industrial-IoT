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
    using System.Collections.Generic;

    /// <summary>
    /// The delegate used to receive publish notifications.
    /// </summary>
    /// <param name="session"></param>
    /// <param name="e"></param>
    public delegate void NotificationEventHandler(ISession session,
        NotificationEventArgs e);

    /// <summary>
    /// Represents the event arguments provided when a new notification message arrives.
    /// </summary>
    public class NotificationEventArgs : EventArgs
    {
        /// <summary>
        /// Creates a new instance.
        /// </summary>
        /// <param name="subscription"></param>
        /// <param name="notificationMessage"></param>
        /// <param name="stringTable"></param>
        public NotificationEventArgs(
            Subscription subscription,
            NotificationMessage notificationMessage,
            IList<string> stringTable)
        {
            Subscription = subscription;
            NotificationMessage = notificationMessage;
            StringTable = stringTable;
        }

        /// <summary>
        /// Gets the subscription that the notification applies to.
        /// </summary>
        public Subscription Subscription { get; }

        /// <summary>
        /// Gets the notification message.
        /// </summary>
        public NotificationMessage NotificationMessage { get; }

        /// <summary>
        /// Gets the string table returned with the notification message.
        /// </summary>
        public IList<string> StringTable { get; }
    }
}
