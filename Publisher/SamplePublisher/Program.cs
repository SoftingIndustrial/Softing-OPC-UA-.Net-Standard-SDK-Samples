/* ========================================================================
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
using Softing.Opc.Ua.PubSub;
using Softing.Opc.Ua.PubSub.Common;

namespace SamplePublisher
{
    static class Program
    {
        /// <summary>
        /// Entry point for application
        /// </summary>
        static void Main()
        {
            try
            {	
                // Create the PubSub application
                UaPubSubApplication pubSubApplication = new UaPubSubApplication();

                // Define a PubSub connection
                PubSubConnectionDataType pubSubConnection = new PubSubConnectionDataType();
                pubSubConnection.Name = "UDPConection1";
                pubSubConnection.Enabled = true;
                pubSubConnection.PublisherId = (UInt16)10;
                pubSubConnection.TransportProfileUri = "http://opcfoundation.org/UA-Profile/Transport/pubsub-udp-uadp";
                NetworkAddressUrlDataType address = new NetworkAddressUrlDataType();
                address.Url = "opc.udp://239.0.0.1:4840";
                pubSubConnection.Address = new ExtensionObject(address);

                // Define a WriterGroup
                WriterGroupDataType writerGroup = new WriterGroupDataType();
                writerGroup.Enabled = true;
                writerGroup.WriterGroupId = 1;
                writerGroup.PublishingInterval = 5000;
                writerGroup.KeepAliveTime = 1500;
                writerGroup.HeaderLayoutUri = "UADP-Cyclic-Fixed";

                // Define a DataSetWriter
                DataSetWriterDataType dataSetWriter = new DataSetWriterDataType();
                dataSetWriter.Enabled = true;
                dataSetWriter.DataSetName = "DataSetWriterSimple";
                dataSetWriter.DataSetWriterId = 1;
                dataSetWriter.KeyFrameCount = 1;

                writerGroup.DataSetWriters.Add(dataSetWriter);
                pubSubConnection.WriterGroups.Add(writerGroup);

                // todo: Add also the dataset (FieldMetadata into UadpDataMessage) 

                // Add the connection to the application
                pubSubApplication.AddConnection(pubSubConnection);


                Console.WriteLine("Publisher started");
                PrintCommandParameters();

                do
                {
                    ConsoleKeyInfo key = Console.ReadKey();
                    if (key.KeyChar == 'q' || key.KeyChar == 'x')
                    {
                        Console.WriteLine("\nShutting down...");
                        break;
                    }
                    else if (key.KeyChar == 's')
                    {
                        // list connection status
                    }
                    else
                    {
                        PrintCommandParameters();
                    }
                }
                while (true);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                Console.ReadKey();
                Environment.Exit(-1);
            }
            finally
            {
                //pubSubApplication.Stop();
            }
        }

        private static void PrintCommandParameters()
        {
            Console.WriteLine("Press:\n\ts: connections status");
            Console.WriteLine("\tx,q: shutdown the server\n\n");
        }	
    }
}
