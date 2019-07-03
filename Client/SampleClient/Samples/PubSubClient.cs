﻿/* ========================================================================
 * Copyright © 2011-2019 Softing Industrial Automation GmbH. 
 * All rights reserved.
 * 
 * The Software is subject to the Softing Industrial Automation GmbH’s 
 * license agreement, which can be found here:
 * http://www.softing.com/LicenseSIA.pdf
 *  
 * ======================================================================*/

using System;
using System.Collections.Generic;
using System.Text;
using Opc.Ua;
using Softing.Opc.Ua.Client;
using SampleClient.Helpers;
using System.IO;
using Opc.Ua.Client;
using Softing.Opc.Ua.PubSub.Configuration;
using Softing.Opc.Ua.Client.Nodes;

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
        public void Initialize()
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
                    m_session.Connect(false, true);
                    Console.WriteLine("Session is connected.");

                }
                catch (Exception ex)
                {
                    Console.WriteLine("CreateSession Error: {0}", ex.Message);
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
        public void Disconnect()
        {
            try
            {
                if (m_session != null)
                {
                    m_session.Disconnect(true);
                    m_session.Dispose();
                    m_session = null;

                    Console.WriteLine("Session is disconnected.");
                }
                else
                {
                    Console.WriteLine("Session already disconnected.");
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("DisconnectSession error: {0}", e.Message);
            }
        }


        /// <summary>
        /// Read the configuration of the PubSubConfig.
        /// </summary>
        public void PubSubReadCfg()
        {
            if (m_session == null)
            {
                Console.WriteLine("The session is not initialized!");
                return;
            }

            try
            {
                UaPubSubConfigurator uaPubSubConfigurator = new UaPubSubConfigurator();
                PubSubStateCfgReader.PubSubConfigurationRead(uaPubSubConfigurator, m_session);
            }
            catch (Exception e)
            {
                Console.WriteLine("Download file error: {0}", e.Message);
            }
        }
    }
    #endregion
}