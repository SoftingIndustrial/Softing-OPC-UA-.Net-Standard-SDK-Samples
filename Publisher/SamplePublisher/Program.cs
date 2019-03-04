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
                pubSubConnection.Address = new ExtensionObject("opc.udp://239.0.0.1:4840");

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
		
		private static void StartPublisher()
        {
            /*
            // configure connection
            UadpNetworkMessage msg = new UadpNetworkMessage();
            msg.PublisherId = (byte)1;
            msg.DataSetClassId = new Guid();
            msg.Timestamp = DateTime.Now;
            msg.PicoSeconds = 10;

            // Add writer group
            msg.GroupVersion = 1;
            msg.WriterGroupId = 1;
            msg.NetworkMessageNumber = 1;
            msg.SequenceNumber = 1;

            // Add writer for group 1
            // PubSubConnectionDataType has a datasetconnection and a connection config (workgroup ...)
            UInt16 size = 1;
            UInt16 dataSetWriterId = 1;
            UadpDataSetMessage dataSetMessage = new UadpDataSetMessage();
            dataSetMessage.DataSetMessageSequenceNumber = 1;

            DataSetGroup dataSetGroup = new DataSetGroup(size, dataSetWriterId, dataSetMessage);
            UadpDataSet dataSet = new UadpDataSet("Test");
            dataSet.AddDataSetGroup(dataSetGroup);

            List<UadpDataSet> dataSets = new List<UadpDataSet>();
            msg.AddDataSetMessages(dataSets);

            ServiceMessageContext messageContext = new ServiceMessageContext();
            BinaryEncoder encoder = new BinaryEncoder(messageContext);
            msg.Encode(encoder);

            dataSetMessage.DataSetMessageSequenceNumber = 2;
            msg.UpdateDataSetMessages(encoder);
            */

        }
    }
}
