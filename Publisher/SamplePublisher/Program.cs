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
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Xml;
using System.Xml.Serialization;
using System.Xml.XPath;
using Opc.Ua;
using Opc.Ua.Test;
using Softing.Opc.Ua.PubSub;

namespace SamplePublisher
{
    public class Program
    {        
        private static DataGenerator m_generator;
        private static object m_lock = new object();
        private static FieldMetaDataCollection m_dynamicFields = new FieldMetaDataCollection();
        private static UaPubSubApplication m_pubSubApplication;

        /// <summary>
        /// Entry point for application
        /// </summary>
        static void Main()
        {
            try
            {
                string configurationFileName = "PubSubConfiguration.xml";
                PubSubConfigurationDataType pubSubConfiguration =   UaPubSubConfigurationHelper.LoadConfiguration(configurationFileName);
                
                foreach (var publishedDataSet in pubSubConfiguration.PublishedDataSets)
                {
                    //remember fields to be updated 
                    m_dynamicFields.AddRange(publishedDataSet.DataSetMetaData.Fields);
                }             
                
                // Create the PubSub application
                m_pubSubApplication = new UaPubSubApplication();
                m_pubSubApplication.LoadConfiguration(pubSubConfiguration);

                Console.WriteLine("Publisher started");
                PrintCommandParameters();

                //start data generator timer 
                Timer simulationTimer = new Timer(DoSimulation, null, 1000, 1000);
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
                simulationTimer.Dispose();
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

        #region Data Changes Simulation
        /// <summary>
        /// Simulate value changes in dynamic nodes
        /// </summary>
        /// <param name="state"></param>
        private static void DoSimulation(object state)
        {
            try
            {
                lock (m_lock)
                {
                    foreach (FieldMetaData variable in m_dynamicFields)
                    {
                        DataValue newDataValue = new DataValue(new Variant(GetNewValue(variable)), StatusCodes.Good, DateTime.UtcNow);
                        m_pubSubApplication.DataStore.WritePublishedDataItem(new NodeId(variable.Name, ConfigurationHelper.NamespaceIndex), Attributes.Value, newDataValue);                       
                    }
                }
            }
            catch (Exception e)
            {
                Utils.Trace(e, "Unexpected error doing simulation.");
            }
        }


        /// <summary>
        /// Generate new value for variable
        /// </summary>
        /// <param name="variable"></param>
        /// <returns></returns>
        private static object GetNewValue(FieldMetaData fieldMetadata)
        {
            if (m_generator == null)
            {
                m_generator = new Opc.Ua.Test.DataGenerator(null);
                m_generator.BoundaryValueFrequency = 0;
            }

            object value = null;

            while (value == null)
            {
                value = m_generator.GetRandom(fieldMetadata.DataType, fieldMetadata.ValueRank, new uint[] { 10 }, null);
            }

            return value;
        }
        #endregion

        private static void PrintCommandParameters()
        {
            Console.WriteLine("Press:\n\ts: connections status");
            Console.WriteLine("\tx,q: shutdown the server\n\n");
        }       
    }
}
