
using System;
using System.Text;
using System.Threading.Tasks;
using Amqp;
using Amqp.Framing;
using Amqp.Sasl;
using System.IO;
using System.Net;
using System.Threading;
using System.Diagnostics;
using System.Collections.Generic;
using IoTHubCredentialTools;

namespace Opc.Ua.IoTHub
{
     public class AmqpConnection
    {
        #region Serialized Configuration Properties

        public string Host { get; set; }

        public int Port { get; set; }

        public string Endpoint { get; set; }

        public string WebSocketEndpoint { get; set; }

        public string KeyName { get; set; }

        public string KeyValue { get; set; }

        public string KeyEncoding { get; set; }

        public bool UseCbs { get; set; }

        public string TokenType { get; set; }

        public string TokenScope { get; set; }

        public int TokenLifetime { get; set; }

        #endregion

        #region Private members

        private Connection m_connection;
        private Session m_session;
        private SenderLink m_link;
        private LinkedList<ArraySegment<byte>> messages;

        private DateTime m_currentExpiryTime;
        private Timer m_tokenRenewalTimer;
        private bool m_closed;
        private int m_sendCounter;
        private int m_sendAcceptedCounter;
        private int m_sendRejectedCounter;
        private object m_sending;
        private int m_sendallthreads;

        #endregion

        #region Constructor

        /// <summary>
        /// Default Constructor 
        /// </summary>
        public AmqpConnection()
        {
            Initialize();
        }

        /// <summary>
        /// Initialize the connection class
        /// </summary>
        private void Initialize()
        {
            m_connection = null;
            m_session = null;
            m_link = null;
            m_closed = true;
            m_sending = new object();
            m_sendallthreads = 0;
            messages = new LinkedList<ArraySegment<byte>>();
            m_sendCounter = 0;
            m_sendAcceptedCounter = 0;
            m_sendRejectedCounter = 0;
        }

        #endregion

        /// <summary>
        /// Open the connection
        /// </summary>
        public async Task OpenAsync()
        {
            // make sure only one SendAll or OpenAsync task is active
            if (Interlocked.Increment(ref m_sendallthreads) != 1)
            {
                Interlocked.Decrement(ref m_sendallthreads);
                return;
            }

            try
            {
                Close();

                ConnectionFactory factory = new ConnectionFactory();
                factory.AMQP.ContainerId = Guid.NewGuid().ToString();
                if (UseCbs)
                {
                    factory.SASL.Profile = SaslProfile.External;
                }

                m_connection = await factory.CreateAsync(GetAddress());
                m_connection.Closed += new ClosedCallback(OnConnectionClosed);

                if (UseCbs && KeyName != null && KeyValue != null)
                {
                    await StartCbs();
                }
                else
                {
                    await ResetLinkAsync();
                }

                Module.Trace("AMQP Connection opened, connected to '{0}'...", Endpoint);

                m_closed = false;
            }
            catch (Exception e)
            {
                Module.Trace("AMQP Connection failed to open, exception: {0}...", e.Message);
            }
            finally
            {
                Interlocked.Decrement(ref m_sendallthreads);
            }

            // Push out the messages we have so far
            SendAll();
        }

        /// <summary>
        /// Publish a JSON message
        /// </summary>
        /// <param name="body"></param>
        public void Publish(ArraySegment<byte> body)
        {
            lock (messages)
            {
                messages.AddLast(body);
            }

            if (IsClosed())
            {
                Task.Run(OpenAsync);
            }
            else
            {
                // Push out the messages we have so far
                Task.Run(new Action(SendAll));
            }
        }

        /// <summary>
        /// Work until all messages have been send...
        /// </summary>
        protected void SendAll()
        {
            // make sure only one send all task is active
            if (Interlocked.Increment(ref m_sendallthreads) != 1)
            {
                Interlocked.Decrement(ref m_sendallthreads);
                return;
            }

            try
            {

                while (!IsClosed())
                {
                    ArraySegment<byte> onemessage;
                    lock (messages)
                    {
                        if (messages.Count == 0)
                        {
                            break;
                        }
                        onemessage = messages.First.Value;
                        messages.RemoveFirst();
                    }

                    bool sent;
                    lock (m_sending)
                    {
                        sent = SendOneAsync(onemessage, SendAllCallback);
                    }

                    if (!sent)
                    {
                        lock (messages)
                        {
                            messages.AddFirst(onemessage);
                        }
                    }
                }
            }
            catch (Exception)
            {
                Close();
            }
            finally
            {
                Interlocked.Decrement(ref m_sendallthreads);
            }
        }

        /// <summary>
        /// Send outcome callback
        /// </summary>
        /// <param name="body"></param>
        /// <returns>Whether message was sent</returns>
        void SendAllCallback(Message message, Outcome outcome, object state)
        {
            if (outcome.Descriptor.Code == 36)
            {
                // accepted
                m_sendAcceptedCounter++;
            }
            else 
            {
                // rejected or other fail reason
                m_sendRejectedCounter++;
            }
            if (((m_sendRejectedCounter + m_sendAcceptedCounter) % 100) == 0)
            {
                Module.Trace("Send Statistics: {0} sent {1} accepted {2} rejected", 
                    m_sendCounter, m_sendAcceptedCounter, m_sendRejectedCounter);
            }
        }

        /// <summary>
        /// Send message
        /// </summary>
        /// <param name="body"></param>
        /// <returns>Whether message was sent</returns>
        protected bool SendOneAsync(ArraySegment<byte> body, OutcomeCallback SendCallback)
        {
            if (IsClosed())
            {
                return false;
            }

            using (var istrm = new MemoryStream(body.Array, body.Offset, body.Count, false))
            {
                Message message = new Message()
                {
                    BodySection = new Data() { Binary = istrm.ToArray() }
                };

                message.Properties = new Properties()
                {
                    MessageId = Guid.NewGuid().ToString(),
                    ContentType = "application/opcua+json"
                };

                if (m_link != null)
                {
                    m_sendCounter++;
                    m_link.Send(message, SendCallback, null);
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Close and therefore dispose of all resources
        /// </summary>
        public void Close()
        {
            m_closed = true;
            if (m_tokenRenewalTimer != null)
            {
                m_tokenRenewalTimer.Dispose();
                m_tokenRenewalTimer = null;
            }
            Dispose(true);
        }

        /// <summary>
        /// is the connection closed?
        /// </summary>
        /// <returns>true or false</returns>
        public bool IsClosed()
        {
            return m_closed;
        }

        /// <summary>
        /// Destructor
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
        }

        /// <summary>
        /// Close all resources
        /// </summary>
        /// <param name="disposing"></param>
        public virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (m_tokenRenewalTimer != null)
                {
                    m_tokenRenewalTimer.Dispose();
                    m_tokenRenewalTimer = null;
                }

                if (m_link != null)
                {
                    try
                    {
                        m_link.Close(3000);
                    }
                    catch(Exception)
                    {
                    }
                    m_link = null;
                }

                if (m_session != null)
                {
                    try
                    {
                        m_session.Close(3000);
                    }
                    catch (Exception)
                    {
                    }
                    m_session = null;
                }

                if (m_connection != null)
                {
                    try
                    {
                        m_connection.Close(3000);
                    }
                    catch (Exception)
                    {
                    }
                    m_connection = null;
                }
            }
        }

        /// <summary>
        /// Returns the amqp.net lite broker address to connect to.
        /// </summary>
        /// <returns>Address to connect to</returns>
        protected Address GetAddress()
        {
            if (Port == 0)
            {
                // Set default port
                if (WebSocketEndpoint != null)
                    Port = 443;
                else
                    Port = 5671;
            }

            if (WebSocketEndpoint != null)
            {
                return new Address(Host, Port, null, null, WebSocketEndpoint, "wss");
            }
            else if (UseCbs)
            {
                return new Address(Host, Port);
            }
            else
            {
                return new Address(Host, Port, KeyName.Trim(), KeyValue.Trim());
            }
        }

        /// <summary>
        /// Start cbs protocol on the underlying connection
        /// </summary>
        /// <returns>Task to wait on</returns>
        protected async Task StartCbs()
        {
            if (m_connection == null)
            {
                throw new Exception("No connection to run cbs renewal on!");
            }

            if (TokenType == null || TokenScope == null)
            {
                throw new Exception("Must specifiy token scope and type");
            }

            if (TokenLifetime == 0)
            {
                TokenLifetime = 60000;
            }

            // Ensure we have a token
            await RenewTokenAsync(GenerateSharedAccessToken());

            // then start the periodic renewal
            int interval = (int)(TokenLifetime * 0.8);
            m_tokenRenewalTimer = new Timer(OnTokenRenewal, null, interval, interval);
        }

        /// <summary>
        /// Return decoded key from configured key value
        /// </summary>
        /// <returns>decoded key</returns>
        protected byte[] DecodeKey()
        {
            if (!KeyEncoding.Equals("base64", StringComparison.CurrentCultureIgnoreCase))
            {
                return Encoding.UTF8.GetBytes(KeyValue.Trim());
            }
            else
            {
                return Convert.FromBase64String(KeyValue.Trim());
            }
        }

        /// <summary>
        /// Generate token for member values
        /// </summary>
        /// <returns>Token string</returns>
        protected string GenerateSharedAccessToken()
        {
            m_currentExpiryTime = DateTime.UtcNow + TimeSpan.FromMilliseconds(TokenLifetime);
            return "SharedAccessSignature " + IoTHubRegistration.GenerateSharedAccessToken(KeyName.Trim(), DecodeKey(), TokenScope.Trim(), TokenLifetime);
        }


        /// <summary>
        /// Callback for connection close events
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="error"></param>
        protected virtual void OnConnectionClosed(AmqpObject sender, Error error)
        {
            if (error != null)
            {
                Debug.WriteLine("Connection Closed {0} {1}", error.Condition, error.Description);
            }
            m_closed = true;
        }

        /// <summary>
        /// Callback for session close event
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="error"></param>
        protected virtual void OnSessionClosed(AmqpObject sender, Error error)
        {
            if (error != null)
            {
                Debug.WriteLine("Session Closed {0} {1}", error.Condition, error.Description);
            }
        }

        /// <summary>
        /// Callback for link close events
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="error"></param>
        protected virtual void OnLinkClosed(AmqpObject sender, Error error)
        {
            if (error != null)
            {
                Debug.WriteLine("Link Closed {0} {1}", error.Condition, error.Description);
            }
        }

        /// <summary>
        /// Timer callback for token renewal
        /// </summary>
        /// <param name="state"></param>
        private void OnTokenRenewal(object state)
        {
            try
            {
                lock (m_sending)
                {
                    bool result = RenewTokenAsync(GenerateSharedAccessToken()).Wait(TokenLifetime);
                    if (!result)
                    {
                        Module.Trace("Unexpected timeout error renewing token.");
                    }
                }
            }
            catch (Exception e)
            {
                Module.Trace(e, "Unexpected error renewing token.");
                
                if (e is AggregateException ae)
                {
                    foreach (var ie in ae.InnerExceptions)
                    {
                        Module.Trace("[{0}] {1}", ie.GetType().Name, ie.Message);
                    }
                }
            }
        }

        /// <summary>
        /// Reset the link and session
        /// </summary>
        /// <returns>Task to wait on</returns>
        private async Task ResetLinkAsync()
        {
            SenderLink link;
            Session session;

            session = new Session(m_connection);
            session.Closed += new ClosedCallback(OnSessionClosed);

            link = new SenderLink(session, Guid.NewGuid().ToString(), Endpoint);
            link.Closed += new ClosedCallback(OnLinkClosed);

            if (m_link != null)
            {
                await m_link.CloseAsync();
            }

            if (m_session != null)
            {
                await m_session.CloseAsync();
            }

            m_session = session;
            m_link = link;
        }

        /// <summary>
        /// renews the cbs token
        /// </summary>
        /// <param name="sharedAccessToken">token to renew</param>
        /// <returns>Task to wait on</returns>
        private async Task RenewTokenAsync(string sharedAccessToken)
        {
            var session = new Session(m_connection);
            string cbsClientAddress = "cbs-client-reply-to";
            var cbsSender = new SenderLink(session, "cbs-sender", "$cbs");
            var receiverAttach = new Attach()
            {
                Source = new Source() { Address = "$cbs" },
                Target = new Target() { Address = cbsClientAddress }
            };
            var cbsReceiver = new ReceiverLink(session, "cbs-receiver", receiverAttach, null);

            // construct the put-token message
            var request = new Message(sharedAccessToken);
            request.Properties = new Properties();
            request.Properties.MessageId = "1";
            request.Properties.ReplyTo = cbsClientAddress;
            request.ApplicationProperties = new ApplicationProperties();

            request.ApplicationProperties["operation"] = "put-token";
            request.ApplicationProperties["type"] = TokenType;
            request.ApplicationProperties["name"] = TokenScope.Trim();

            await cbsSender.SendAsync(request);

            // receive the response
            var response = await cbsReceiver.ReceiveAsync();
            if (response == null || response.Properties == null || response.ApplicationProperties == null)
            {
                throw new Exception("invalid response received");
            }

            int statusCode = (int)response.ApplicationProperties["status-code"];

            await cbsSender.CloseAsync();
            await cbsReceiver.CloseAsync();
            await session.CloseAsync();

            if (statusCode != (int)HttpStatusCode.Accepted && statusCode != (int)HttpStatusCode.OK)
            {
                throw new Exception("put-token message was not accepted. Error code: " + statusCode);
            }

            // Now create new link
            await ResetLinkAsync();
        }
    }
}
