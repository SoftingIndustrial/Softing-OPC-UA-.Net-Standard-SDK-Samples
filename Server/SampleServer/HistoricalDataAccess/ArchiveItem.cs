/* ========================================================================
 * Copyright © 2011-2021 Softing Industrial Automation GmbH. 
 * All rights reserved.
 * 
 * The Software is subject to the Softing Industrial Automation GmbH’s 
 * license agreement, which can be found here:
 * https://industrial.softing.com/LA_SIA_EN
 * 
 * ======================================================================*/

using System;
using System.Text;
using System.IO;
using System.Data;
using Opc.Ua;

namespace SampleServer.HistoricalDataAccess
{
    /// <summary>
    /// Stores the metadata for a node representing a folder on a file system
    /// </summary>
    public class ArchiveItem
    {
        #region Constructor

        /// <summary>
        /// Creates a new instance.
        /// </summary>
        public ArchiveItem(string nodeIdName, FileInfo file)
        {
            NodeIdName = nodeIdName;
            FileInfo = file;
            Name = string.Empty;

            if (FileInfo != null)
            {
                Name = FileInfo.Name;

                int index = Name.LastIndexOf('.');

                if (index > 0)
                {
                    Name = Name.Substring(0, index);
                }
            }
        }

        #endregion

        #region Properties

        /// <summary>
        /// A name for the item
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// The unique path to the item in the archive
        /// </summary>
        public string NodeIdName { get; set; }

        /// <summary>
        /// The data type for the item
        /// </summary>
        public BuiltInType DataType;

        /// <summary>
        /// The value rank for the item
        /// </summary>
        public int ValueRank { get; set; }

        /// <summary>
        /// The type of simulated data
        /// </summary>
        public int SimulationType { get; set; }

        /// <summary>
        /// The amplitude of the simulated data
        /// </summary>
        public double Amplitude { get; set; }

        /// <summary>
        /// The period of the simulated data
        /// </summary>
        public double Period { get; set; }

        /// <summary>
        /// Whether the simulation is running
        /// </summary>
        public bool Archiving { get; set; }

        /// <summary>
        /// Whether the data requires stepped interpolation
        /// </summary>
        public bool Stepped { get; set; }

        /// <summary>
        /// The sampling interval for the simulation
        /// </summary>
        public double SamplingInterval { get; set; }

        /// <summary>
        /// The history for the item
        /// </summary>
        public DataSet DataSet { get; set; }

        /// <summary>
        /// The last the dataset was loaded from its source
        /// </summary>
        public DateTime LastLoadTime { get; set; }

        /// <summary>
        /// Whether the source is persistent and needs to be reloaded
        /// </summary>
        public bool Persistent { get; set; }

        /// <summary>
        /// The aggregate configuration for the item
        /// </summary>
        public AggregateConfiguration AggregateConfiguration { get; set; }

        /// <summary>
        /// The physical file containing the item history
        /// </summary>
        private FileInfo FileInfo { get; }

        #endregion

        #region Public Methods

        /// <summary>
        /// Returns a stream that can be used to read the archive
        /// </summary>
        public StreamReader OpenArchive()
        {
            if (FileInfo != null)
            {
                return new StreamReader(FileInfo.FullName, Encoding.UTF8);
            }

            return null;
        }

        #endregion
    }
}