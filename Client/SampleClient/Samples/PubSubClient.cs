/* ========================================================================
 * Copyright © 2011-2022 Softing Industrial Automation GmbH. 
 * All rights reserved.
 * 
 * The Software is subject to the Softing Industrial Automation GmbH’s 
 * license agreement, which can be found here:
 * https://industrial.softing.com/LA-SDK-en
 *  
 * ======================================================================*/

using System;
using Opc.Ua;
using Softing.Opc.Ua.Client;
using Opc.Ua.Client;
using System.Threading.Tasks;

namespace SampleClient.Samples
{
    internal class PubSubClient
    {
        #region Private Fields

        private ClientSession m_session;
        private readonly UaApplication m_application;

        private const string SessionName = "PubSubClient Session";
        private ServerState m_currentServerState = ServerState.Unknown;
        #endregion

        #region Constructor
        public PubSubClient(UaApplication application)
        {
            m_application = application;
        }
        #endregion

        #region Private Methods
        private void Session_KeepAlive(object sender, KeepAliveEventArgs e)
        {
            if (e.CurrentState != m_currentServerState)
            {
                m_currentServerState = e.CurrentState;
                Console.WriteLine("Session KeepAlive Server state changed to: {0}", m_currentServerState);
            }
        }
        #endregion

        #region Public Methods

        /// <summary>
        /// Initialize session
        /// </summary>
        public async Task Initialize()
        {
            if (m_session == null)
            {
                try
                {
                    // create the session object with no security and anonymous login    
                    m_session = m_application.CreateSession(Program.ServerUrl);
                    m_session.SessionName = SessionName;
                    m_session.KeepAlive += Session_KeepAlive;

                    // connect session
                    await m_session.ConnectAsync(false, true).ConfigureAwait(false);
                    Console.WriteLine("Session is connected.");

                }
                catch (Exception ex)
                {
                    Program.PrintException("CreateSession", ex);

                    if (m_session != null)
                    {
                        m_session.Dispose();
                        m_session = null;
                    }

                    return;
                }
            }
        }      

        /// <summary>
        /// Disconnect the current session
        /// </summary>
        public async Task Disconnect()
        {
            try
            {
                if (m_session != null)
                {
                    await m_session.DisconnectAsync(true).ConfigureAwait(false);
                    m_session.Dispose();
                    m_session = null;

                    Console.WriteLine("Session is disconnected.");
                }
                else
                {
                    Console.WriteLine("Session already disconnected.");
                }
            }
            catch (Exception ex)
            {
                Program.PrintException("DisconnectSession", ex);
            }
        }

        /// <summary>
        /// Read the configuration of the PubSub.
        /// </summary>
        public void ReadPubSubConfiguration()
        {
            if (m_session == null)
            {
                Console.WriteLine("The session is not initialized!");
                return;
            }

            try
            {
                PubSubConfigurationDataType pubSubConfigurationData = 
                    PubSubStateConfigurationReader.PubSubConfigurationRead(m_session);
            }
            catch (Exception ex)
            {
                Program.PrintException("ReadPubSubConfiguration", ex);
            }
        }
    }
    #endregion
}
