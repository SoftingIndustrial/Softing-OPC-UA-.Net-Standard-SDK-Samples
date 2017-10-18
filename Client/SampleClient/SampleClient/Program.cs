using Opc.Ua;
/* ========================================================================
 * Copyright © 2011-2018 Softing Industrial Automation GmbH. 
 * All rights reserved.
 * 
 * The Software is subject to the Softing Industrial Automation GmbH’s 
 * license agreement, which can be found here:
 * http://www.softing.com/LicenseSIA.pdf
 *  
 * ======================================================================*/

using System;
using System.Threading.Tasks;
using Softing.Opc.Ua;

namespace SampleClient
{
    class Program
    {
        private const string m_configFileName = "SampleClient.config";
        #region Commands
        private const string m_exitCommand = "x";
        private const string m_createCommand = "c";
        private const string m_disconnectCommand = "d";
        private const string m_createSubscripotionCommand = "cs";
        private const string m_deleteSubscripotionCommand = "ds";
        private const string m_createMonitoredItem = "m";
        private const string m_deleteMonitoredItem = "dm";
        private const string m_createEventMonitoredItem = "e";
        private const string m_filterEventMonitoredItem = "f";
        private const string m_deleteEventMonitoredItem = "de";
        private const string m_setEventFilter = "f";
        private const string m_createAlarmsMonitoredItem = "ca";
        private const string m_deleteAlarmsMonitoredItem = "da";
        private const string m_callConditionRefresh = "ra";
        private const string m_acknowledgeAlarm = "aa";
        private const string m_readMonitoredItem = "r";
        private const string m_writeMonitoredItem = "w";
        private const string m_callMethodCommand = "met";
        private const string m_asyncCallMethodCommand = "amet";
        private const string m_historyReadRaw = "hr";
        private const string m_historyReadAtTime = "ht";
        private const string m_historyReadProcessed = "hp";
        private const string m_browseCommand = "b";
        private const string m_browseOptionsCommand = "o";
        private const string m_translateBrowsePathCommand = "t";
        private const string m_translateMultipleBrowsePathCommand = "tm";
        
        #endregion

        static void Main(string[] args)
        {
            UaApplication application = UaApplication.CreateConfiguredApplication(configFileName: m_configFileName).Result;
            if (application.Configuration.SecurityConfiguration.AutoAcceptUntrustedCertificates)
            {
                application.Configuration.CertificateValidator.CertificateValidation += new CertificateValidationEventHandler(CertificateValidator_CertificateValidation);
            }

            string commandList = "List of commands: \r\n" +
                                "c  - Create and Connect the session \r\n" +
                                "d  - Disconnect session \r\n" +
                                "cs - Create a subscription \r\n" +
                                "ds - Delete subscription \r\n" +
                                "m  - Create monitored item \r\n" +
                                "dm - Delete monitored item \r\n" +
                                "e  - Create event monitored item \r\n" +
                                "f  - Apply filter for event monitored item \r\n" +
                                "de - Delete event monitored item \r\n" +
                                "ca - Create alarms monitored item\r\n" +
                                "da - Delete alarms monitored item\r\n" +
                                "ra - Refresh active alarms (Call ConditionRefresh method)\r\n" +
                                "aa - Acknowledge alarm\r\n" +
                                "r  - Read Command \r\n" +
                                "w  - Write Command \r\n" +
                                "met  - Call the method \r\n" +
                                "amet - Asynchronous call method \r\n" +
                                "hr   - History read raw \r\n" +
                                "ht   - History read at time \r\n" +
                                "hp   - History read processed \r\n" +
                                "b  - Browse server \r\n" +
                                "o  - Browse server with options \r\n" +
                                "t  - TranslateBrowsePathToNodeIds \r\n" +
                                "tm - Translate multiple Browse Paths \r\n" +
                                "x  - Exit \r\n";

            Console.WriteLine(commandList);
            Console.WriteLine("Enter Commands:\n");

            Client client = new Client(application);
            bool finished = false;
            try
            {
                do
                {
                    // Read commands
                    string inputCommand = Console.ReadLine();

                    switch (inputCommand.ToLower())
                    {
                        case m_createCommand:
                            client.CreateSession();
                            break;
                        case m_disconnectCommand:
                            client.DisconnectSession();
                            break;
                        case m_exitCommand:
                            finished = true;
                            client.DisconnectSession();
                            break;
                        case m_createSubscripotionCommand:
                            client.CreateSubscription();
                            break;
                        case m_deleteSubscripotionCommand:
                            client.DeleteSubscription();
                            break;
                        case m_createMonitoredItem:
                            client.CreateMonitoredItem();
                            break;
                        case m_deleteMonitoredItem:
                            client.DeleteMonitoredItem();
                            break;
                        case m_createEventMonitoredItem:
                            client.CreateEventMonitoredItem();
                            break;
                        case m_filterEventMonitoredItem:
                            client.ApplyEventMonitoredItemFilter();
                            break;
                        case m_deleteEventMonitoredItem:
                            client.DeleteEventMonitoredItem();
                            break;
                        case m_createAlarmsMonitoredItem:
                            client.CreateAlarmsMonitoredItem();
                            break;
                        case m_deleteAlarmsMonitoredItem:
                            client.DeleteAlarmsMonitoredItem();
                            break;
                        case m_callConditionRefresh:
                            client.ConditionRefresh();
                            break;
                        case m_acknowledgeAlarm:
                            client.AcknowledgeAlarm();
                            break;
                        case m_readMonitoredItem:
                            Console.WriteLine("Read done with DataValue: {0}", client.ReadMonitoredItem());
                            break;                        
                        case m_writeMonitoredItem:
                            Console.WriteLine("Write done with StatusCode: {0}", client.WriteMonitoredItem());
                            break;
                        case m_callMethodCommand:
                            client.CallMethod();
                            break;
                        case m_asyncCallMethodCommand:
                            client.AsyncCallMethod();
                            break;
                        case m_historyReadRaw:
                            client.HistoryReadRaw();
                            break;
                        case m_historyReadAtTime:
                            client.HistoryReadAtTime();
                            break;
                        case m_historyReadProcessed:
                            client.HistoryReadProcessed();
                            break;
                        case m_browseCommand:
                            client.BrowseTheServer();
                            break;
                        case m_browseOptionsCommand:
                            Console.WriteLine("\nBrowse with options.\nMaxReferencesReturned is set to 3 before browsing the Server node. \nAfter 3 references returned a continuation point event cancels further browse.\n");
                            client.BrowseWithOptions();
                            break;
                        case m_translateBrowsePathCommand:
                            client.TranslateBrowsePathToNodeIds();
                            break;
                        case m_translateMultipleBrowsePathCommand:
                            client.TranslateBrowsePathsToNodeIds();
                            break;
                        default:
                            Console.WriteLine("Invalid Command!");
                            Console.WriteLine(commandList);
                            break;
                    }
                }
                while (!finished);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }


            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
        }



        private static void CertificateValidator_CertificateValidation(CertificateValidator validator, CertificateValidationEventArgs e)
        {
            Console.WriteLine("Accepted Certificate: {0}", e.Certificate.Subject);
            e.Accept = (e.Error.StatusCode == StatusCodes.BadCertificateUntrusted);
        }
    }
}