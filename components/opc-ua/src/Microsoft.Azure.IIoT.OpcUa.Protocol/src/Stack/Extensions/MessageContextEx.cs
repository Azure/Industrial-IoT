// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Opc.Ua.Extensions {
    using Opc.Ua.Client;

    /// <summary>
    /// Extensions for message context
    /// </summary>
    public static class MessageContextEx {

        /// <summary>
        /// Update message context from session
        /// </summary>
        /// <param name="context"></param>
        /// <param name="session"></param>
        public static void UpdateFromSession(this ServiceMessageContext context,
            Session session) {
            if (session.TransportChannel?.MessageContext != null) {
                context.UpdateFromContext(session.TransportChannel.MessageContext);
            }
            context.Factory = session.Factory;
            context.NamespaceUris = session.NamespaceUris;
            context.ServerUris = session.ServerUris;
        }

        /// <summary>
        /// Update message context from context
        /// </summary>
        /// <param name="context"></param>
        /// <param name="update"></param>
        public static void UpdateFromContext(this ServiceMessageContext context,
            ServiceMessageContext update) {
            context.Factory = update.Factory;
            context.NamespaceUris = update.NamespaceUris;
            context.ServerUris = update.ServerUris;
            context.MaxStringLength = update.MaxStringLength;
            context.MaxArrayLength = update.MaxArrayLength;
            context.MaxByteStringLength = update.MaxByteStringLength;
            context.MaxMessageSize = update.MaxMessageSize;
        }

        /// <summary>
        /// Update message context from context
        /// </summary>
        /// <param name="context"></param>
        /// <param name="update"></param>
        public static void UpdateFromContext(this ServiceMessageContext context,
            ISystemContext update) {
            context.Factory = update.EncodeableFactory;
            context.NamespaceUris = update.NamespaceUris;
            context.ServerUris = update.ServerUris;
        }

        /// <summary>
        /// Convert to system context
        /// </summary>
        /// <param name="context"></param>
        public static ISystemContext ToSystemContext(this ServiceMessageContext context) {
            if (context == null) {
                return null;
            }
            return new SystemContext {
                EncodeableFactory = context.Factory,
                NamespaceUris = context.NamespaceUris,
                ServerUris = context.ServerUris
            };
        }

        /// <summary>
        /// Convert to message context
        /// </summary>
        /// <param name="context"></param>
        public static ServiceMessageContext ToMessageContext(this ISystemContext context) {
            if (context == null) {
                return null;
            }
            return new ServiceMessageContext {
                Factory = context.EncodeableFactory,
                NamespaceUris = context.NamespaceUris,
                ServerUris = context.ServerUris
            };
        }

    }
}
