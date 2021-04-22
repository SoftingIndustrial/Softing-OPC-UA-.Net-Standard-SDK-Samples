/* ========================================================================
 * Copyright © 2011-2021 Softing Industrial Automation GmbH. 
 * All rights reserved.
 * 
 * The Software is subject to the Softing Industrial Automation GmbH’s 
 * license agreement, which can be found here:
 * https://data-intelligence.softing.com/LA-SDK-en/
 * 
 * ======================================================================*/

using System;
using System.Text;
using System.IO;
using System.Xml;
using System.Data;
using Opc.Ua;

namespace SampleServer.HistoricalDataAccess
{
    /// <summary>
    /// Reads an item history from a file.
    /// </summary>
    public class DataFileReader
    {
        enum HistoryDataValueType
        {
            Raw,
            Modified,
            Annotation
        }

        #region Public Methods

        /// <summary>
        ///  Loads the item configuration
        /// </summary>
        /// <param name="context"></param>
        /// <param name="item"></param>
        /// <returns></returns>
        public bool LoadConfiguration(ISystemContext context, ArchiveItem item)
        {
            using (StreamReader reader = item.OpenArchive())
            {
                while (!reader.EndOfStream)
                {
                    string line = reader.ReadLine();

                    // check for end or error.
                    if (line == null)
                    {
                        break;
                    }

                    // ignore blank lines.
                    line = line.Trim();

                    if (String.IsNullOrEmpty(line))
                    {
                        continue;
                    }

                    // ignore commented out lines.
                    if (line.StartsWith("//"))
                    {
                        continue;
                    }

                    int valueRank = ValueRanks.Scalar;
                    int samplingInterval = 0;
                    int simulationType = 0;
                    int amplitude = 0;
                    int period = 0;
                    int archiving = 0;
                    int stepped = 0;
                    int useSlopedExtrapolation = 0;
                    int treatUncertainAsBad = 0;
                    int percentDataBad = 0;
                    int percentDataGood = 0;
                    string nodeIdName;
                    BuiltInType dataType;

                    // get data type.
                    if (!ExtractField(1, ref line, out dataType))
                    {
                        return false;
                    }

                    // get value rank.
                    if (!ExtractField(1, ref line, out valueRank))
                    {
                        return false;
                    }

                    // get sampling interval.
                    if (!ExtractField(1, ref line, out samplingInterval))
                    {
                        return false;
                    }

                    // get simulation type.
                    if (!ExtractField(1, ref line, out simulationType))
                    {
                        return false;
                    }

                    // get simulation amplitude.
                    if (!ExtractField(1, ref line, out amplitude))
                    {
                        return false;
                    }

                    // get simulation period.
                    if (!ExtractField(1, ref line, out period))
                    {
                        return false;
                    }

                    // get flag indicating whether new data is generated.
                    if (!ExtractField(1, ref line, out archiving))
                    {
                        return false;
                    }

                    // get flag indicating whether stepped interpolation is used.
                    if (!ExtractField(1, ref line, out stepped))
                    {
                        return false;
                    }

                    // get flag indicating whether sloped interpolation should be used.
                    if (!ExtractField(1, ref line, out useSlopedExtrapolation))
                    {
                        return false;
                    }

                    // get flag indicating whether sloped interpolation should be used.
                    if (!ExtractField(1, ref line, out treatUncertainAsBad))
                    {
                        return false;
                    }

                    // get the maximum permitted of bad data in an interval.
                    if (!ExtractField(1, ref line, out percentDataBad))
                    {
                        return false;
                    }

                    // get the minimum amount of good data in an interval.
                    if (!ExtractField(1, ref line, out percentDataGood))
                    {
                        return false;
                    }

                    // get the nodeId identifier.
                    if (!ExtractField(1, ref line, out nodeIdName))
                    {
                        return false;
                    }

                    // update the item.
                    item.DataType = dataType;
                    item.ValueRank = valueRank;
                    item.SimulationType = simulationType;
                    item.Amplitude = amplitude;
                    item.Period = period;
                    item.SamplingInterval = samplingInterval;
                    item.Archiving = archiving != 0;
                    item.Stepped = stepped != 0;
                    item.AggregateConfiguration = new AggregateConfiguration();
                    item.AggregateConfiguration.UseServerCapabilitiesDefaults = false;
                    item.AggregateConfiguration.UseSlopedExtrapolation = useSlopedExtrapolation != 0;
                    item.AggregateConfiguration.TreatUncertainAsBad = treatUncertainAsBad != 0;
                    item.AggregateConfiguration.PercentDataBad = (byte) percentDataBad;
                    item.AggregateConfiguration.PercentDataGood = (byte) percentDataGood;
                    item.NodeIdName = nodeIdName;
                    break;
                }
            }

            return true;
        }

        /// <summary>
        /// Loads the history for the item.
        /// </summary>
        public void LoadHistoryData(ISystemContext context, ArchiveItem item)
        {
            using (StreamReader reader = item.OpenArchive())
            {
                // skip configuration line.
                reader.ReadLine();
                item.DataSet = LoadData(context, reader, item.Archiving, item.SamplingInterval);
            }

            // update the timestamp.
            item.LastLoadTime = DateTime.UtcNow;
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Creates a new data set
        /// </summary>
        private DataSet CreateDataSet()
        {
            DataSet dataset = new DataSet();

            dataset.Tables.Add("CurrentData");

            dataset.Tables[0].Columns.Add("SourceTimestamp", typeof(DateTime));
            dataset.Tables[0].Columns.Add("ServerTimestamp", typeof(DateTime));
            dataset.Tables[0].Columns.Add("Value", typeof(DataValue));
            dataset.Tables[0].Columns.Add("DataType", typeof(BuiltInType));
            dataset.Tables[0].Columns.Add("ValueRank", typeof(int));

            dataset.Tables[0].DefaultView.Sort = "SourceTimestamp";

            dataset.Tables.Add("ModifiedData");

            dataset.Tables[1].Columns.Add("SourceTimestamp", typeof(DateTime));
            dataset.Tables[1].Columns.Add("ServerTimestamp", typeof(DateTime));
            dataset.Tables[1].Columns.Add("Value", typeof(DataValue));
            dataset.Tables[1].Columns.Add("DataType", typeof(BuiltInType));
            dataset.Tables[1].Columns.Add("ValueRank", typeof(int));
            dataset.Tables[1].Columns.Add("UpdateType", typeof(int));
            dataset.Tables[1].Columns.Add("ModificationInfo", typeof(ModificationInfo));

            dataset.Tables[1].DefaultView.Sort = "SourceTimestamp, UpdateType";

            dataset.Tables.Add("AnnotationData");

            dataset.Tables[2].Columns.Add("SourceTimestamp", typeof(DateTime));
            dataset.Tables[2].Columns.Add("ServerTimestamp", typeof(DateTime));
            dataset.Tables[2].Columns.Add("Value", typeof(DataValue));
            dataset.Tables[2].Columns.Add("DataType", typeof(BuiltInType));
            dataset.Tables[2].Columns.Add("ValueRank", typeof(int));
            dataset.Tables[2].Columns.Add("Annotation", typeof(Annotation));

            dataset.Tables[2].DefaultView.Sort = "SourceTimestamp";

            return dataset;
        }

        /// <summary>
        /// Loads the history data from a stream.
        /// </summary>
        private DataSet LoadData(ISystemContext context, StreamReader reader, bool archiving, double samplingRate)
        {
            DataSet dataset = CreateDataSet();
            ServiceMessageContext messageContext = new ServiceMessageContext();

            if (context != null)
            {
                messageContext.NamespaceUris = context.NamespaceUris;
                messageContext.ServerUris = context.ServerUris;
                messageContext.Factory = context.EncodeableFactory;
            }
            else
            {
                messageContext.NamespaceUris = ServiceMessageContext.GlobalContext.NamespaceUris;
                messageContext.ServerUris = ServiceMessageContext.GlobalContext.ServerUris;
                messageContext.Factory = ServiceMessageContext.GlobalContext.Factory;
            }

            string sourceDateTime = string.Empty;
            string serverDateTime = string.Empty;
            HistoryDataValueType recordType;
            int modificationTimeOffet = 0;
            string modificationUser = String.Empty;
            BuiltInType valueType = BuiltInType.String;
            Variant value = Variant.Null;
            string annotationDateTime = String.Empty;
            string annotationUser = String.Empty;
            string annotationMessage = String.Empty;
            int lineCount = 0;
            DateTime nextValueTime = DateTime.UtcNow; //used for generating values

            while (!reader.EndOfStream)
            {
                string line = reader.ReadLine();

                // check for end or error.
                if (line == null)
                {
                    break;
                }

                // ignore blank lines.
                line = line.Trim();
                lineCount++;

                if (String.IsNullOrEmpty(line))
                {
                    continue;
                }

                // ignore commented out lines.
                if (line.StartsWith("//"))
                {
                    continue;
                }

                //if the item is archiving we generate the values at each sample rate
                if (!archiving)
                {
                    // get source time.
                    if (!ExtractField(lineCount, ref line, out sourceDateTime))
                    {
                        continue;
                    }

                    // get server time.
                    if (!ExtractField(lineCount, ref line, out serverDateTime))
                    {
                        continue;
                    }
                }

                // get status code.
                StatusCode status = StatusCodes.Good;
                if (!ExtractField(lineCount, ref line, out status))
                {
                    continue;
                }

                // get modification type.
                if (!ExtractField(lineCount, ref line, out recordType))
                {
                    continue;
                }

                if (recordType == HistoryDataValueType.Modified)
                {
                    // get modification time.
                    if (!ExtractField(lineCount, ref line, out modificationTimeOffet))
                    {
                        continue;
                    }

                    // get modification user.
                    if (!ExtractField(lineCount, ref line, out modificationUser))
                    {
                        continue;
                    }
                }

                if (recordType == HistoryDataValueType.Raw || recordType == HistoryDataValueType.Modified)
                {
                    // get value type.
                    if (!ExtractField(lineCount, ref line, out valueType))
                    {
                        continue;
                    }

                    // get value.
                    if (!ExtractField(lineCount, ref line, messageContext, valueType, out value))
                    {
                        continue;
                    }
                }
                else if (recordType == HistoryDataValueType.Annotation)
                {
                    // get annotation time.
                    if (!ExtractField(lineCount, ref line, out annotationDateTime))
                    {
                        continue;
                    }

                    // get annotation user.
                    if (!ExtractField(lineCount, ref line, out annotationUser))
                    {
                        continue;
                    }

                    // get annotation message.
                    if (!ExtractField(lineCount, ref line, out annotationMessage))
                    {
                        continue;
                    }
                }

                // add values to data table.
                DataValue dataValue = new DataValue();
                dataValue.WrappedValue = value;

                if (!archiving)
                {
                    dataValue.SourceTimestamp = DateTime.Parse(sourceDateTime).ToUniversalTime();
                    dataValue.ServerTimestamp = DateTime.Parse(serverDateTime).ToUniversalTime();
                }
                else
                {
                    dataValue.SourceTimestamp = nextValueTime;
                    dataValue.ServerTimestamp = nextValueTime;

                    nextValueTime = nextValueTime.AddMilliseconds(samplingRate);
                }

                dataValue.StatusCode = status;

                DataRow row = null;

                if (recordType == HistoryDataValueType.Raw)
                {
                    row = dataset.Tables[0].NewRow();

                    row[0] = dataValue.SourceTimestamp;
                    row[1] = dataValue.ServerTimestamp;
                    row[2] = dataValue;
                    row[3] = valueType;
                    row[4] = (value.TypeInfo != null) ? value.TypeInfo.ValueRank : ValueRanks.Any;

                    dataset.Tables[0].Rows.Add(row);
                }
                else if (recordType == HistoryDataValueType.Modified)
                {
                    row = dataset.Tables[1].NewRow();

                    row[0] = dataValue.SourceTimestamp;
                    row[1] = dataValue.ServerTimestamp;
                    row[2] = dataValue;
                    row[3] = valueType;
                    row[4] = (value.TypeInfo != null) ? value.TypeInfo.ValueRank : ValueRanks.Any;
                    row[5] = recordType;

                    ModificationInfo info = new ModificationInfo();
                    info.UpdateType = (HistoryUpdateType) recordType;
                    //info.ModificationTime = baseline.AddMilliseconds(modificationTimeOffet);
                    info.UserName = modificationUser;
                    row[6] = info;

                    dataset.Tables[1].Rows.Add(row);
                }

                else if (recordType == HistoryDataValueType.Annotation)
                {
                    row = dataset.Tables[2].NewRow();

                    Annotation annotation = new Annotation();
                    annotation.AnnotationTime = DateTime.Parse(annotationDateTime).ToUniversalTime();
                    annotation.UserName = annotationUser;
                    annotation.Message = annotationMessage;
                    dataValue.WrappedValue = new ExtensionObject(annotation);

                    row[0] = dataValue.SourceTimestamp;
                    row[1] = dataValue.ServerTimestamp;
                    row[2] = dataValue;
                    row[3] = valueType;
                    row[4] = (value.TypeInfo != null) ? value.TypeInfo.ValueRank : ValueRanks.Any;
                    row[5] = annotation;

                    dataset.Tables[2].Rows.Add(row);
                }

                dataset.AcceptChanges();
            }

            return dataset;
        }

        #endregion

        #region Parsing Functions

        /// <summary>
        /// Extracts the next comma separated field from the line.
        /// </summary>
        private string ExtractField(ref string line)
        {
            string field = line;
            int index = field.IndexOf(',');

            if (index >= 0)
            {
                field = field.Substring(0, index);
                line = line.Substring(index + 1);
            }

            field = field.Trim();

            if (String.IsNullOrEmpty(field))
            {
                return null;
            }

            return field;
        }

        /// <summary>
        ///  Extracts an integer value from the line.
        /// </summary>
        /// <param name="lineCount"></param>
        /// <param name="line"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        private bool ExtractField(int lineCount, ref string line, out string value)
        {
            value = string.Empty;
            string field = ExtractField(ref line);

            if (field == null)
            {
                return true;
            }

            value = field;
            return true;
        }

        /// <summary>
        /// Extracts an integer value from the line.
        /// </summary>
        /// <param name="lineCount"></param>
        /// <param name="line"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        private bool ExtractField(int lineCount, ref string line, out int value)
        {
            value = 0;
            string field = ExtractField(ref line);

            if (field == null)
            {
                return true;
            }

            try
            {
                value = Convert.ToInt32(field);
            }
            catch (Exception e)
            {
                Utils.Trace(Utils.TraceMasks.Error, 
                    "HistoricalAccess.HistoricalDataAccess.DataFileReader.ExtractField: PARSE ERROR [Line:{0}] - '{1}': {2}", 
                    lineCount, field, e.Message);
                return false;
            }

            return true;
        }

        /// <summary>
        /// Extracts a StatusCode value from the line.
        /// </summary>
        private bool ExtractField(int lineCount, ref string line, out StatusCode value)
        {
            value = 0;
            string field = ExtractField(ref line);

            if (field == null)
            {
                return true;
            }

            if (field.StartsWith("0x"))
            {
                field = field.Substring(2);
                uint code = Convert.ToUInt32(field, 16);
                value = new StatusCode(code);
            }
            else
            {
                uint code = StatusCodes.GetIdentifier(field);

                if (code == 0 && field != "Good")
                    throw new ApplicationException("Error parsing StatusCode");

                value = code;
            }
            return true;
        }

        /// <summary>
        /// Extracts a HistoryDataValueType value from the line.
        /// </summary>
        private bool ExtractField(int lineCount, ref string line, out HistoryDataValueType value)
        {
            value = 0;
            string field = ExtractField(ref line);

            if (field == null)
            {
                return true;
            }

            value = (HistoryDataValueType) Enum.Parse(typeof(HistoryDataValueType), field);
            return true;
        }

        /// <summary>
        /// Extracts a BuiltInType value from the line.
        /// </summary>
        private bool ExtractField(int lineCount, ref string line, out BuiltInType value)
        {
            value = BuiltInType.String;
            string field = ExtractField(ref line);

            if (field == null)
            {
                return true;
            }

            try
            {
                value = (BuiltInType) Enum.Parse(typeof(BuiltInType), field);
            }
            catch (Exception e)
            {
                Utils.Trace(Utils.TraceMasks.Error,
                    "HistoricalAccess.HistoricalDataAccess.DataFileReader.ExtractField: PARSE ERROR [Line:{0}] - '{1}': {2}", 
                    lineCount, field, e.Message);
                return false;
            }

            return true;
        }

        /// <summary>
        /// Extracts a BuiltInType value from the line.
        /// </summary>
        private bool ExtractField(int lineCount, ref string line, ServiceMessageContext context, BuiltInType valueType, out Variant value)
        {
            value = Variant.Null;
            string field = line;

            if (field == null)
            {
                return true;
            }

            if (valueType == BuiltInType.Null)
            {
                return true;
            }

            StringBuilder builder = new StringBuilder();
            builder.AppendFormat("<Value xmlns=\"{0}\">", Opc.Ua.Namespaces.OpcUaXsd);
            builder.AppendFormat("<{0}>", valueType);
            builder.Append(line);
            builder.AppendFormat("</{0}>", valueType);
            builder.Append("</Value>");

            XmlDocument document = new XmlDocument();
            document.InnerXml = builder.ToString();

            try
            {
                XmlDecoder decoder = new XmlDecoder(document.DocumentElement, context);
                value = decoder.ReadVariant(null);
            }
            catch (Exception e)
            {
                Utils.Trace(Utils.TraceMasks.Error, 
                    "HistoricalAccess.HistoricalDataAccess.DataFileReader.ExtractField: PARSE ERROR [Line:{0}] - '{1}': {2}", 
                    lineCount, field, e.Message);
                return false;
            }

            return true;
        }

        #endregion
    }
}