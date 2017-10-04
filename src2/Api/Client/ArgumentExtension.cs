using Opc.Ua.Toolkit.Trace;
using System;
using System.Collections.Generic;
using System.Text;

namespace Opc.Ua.Toolkit.Client
{
    class ArgumentExtension
    {
        private const string NullXml = "<Null/>";
        /// <summary>
        /// Gets the default value for data type.
        /// </summary>
        /// <param name="dataType">The NodeId of the data type</param>
        /// <param name="valueRank">The value rank.</param>
        /// <param name="session">The session that contains the Address Space where Node is registered.</param>
        /// <returns>The default value returned as an object.  </returns>
        public static object GetDefaultValueForDatatype(NodeId dataType, ValueRanks valueRank, Client.Session session)
        {
            if (dataType == null)
            {
                throw new ArgumentNullException("dataType");
            }

            if (session == null)
            {
                throw new ArgumentNullException("session");
            }

            if (valueRank < 0)
            {
                BuiltInType builtInType = BuiltInType.Null;

                if (dataType != null && dataType.IdType == IdType.Numeric && dataType.NamespaceIndex == 0)
                {
                    uint id = (uint)dataType.Identifier;

                    // if we want an array then go into a loop
                    if (id <= DataTypes.DiagnosticInfo)
                    {
                        if (id == DataTypes.String)
                        {
                            return string.Empty;
                        }
                        else if (id == DataTypes.XmlElement)
                        {
                            System.Xml.XmlDocument document = new System.Xml.XmlDocument();
                            document.InnerXml = NullXml;
                            return document.DocumentElement;
                        }
                        else if (id == DataTypes.ByteString)
                        {
                            return new byte[0];
                        }
                        else if (id == DataTypes.DiagnosticInfo)
                        {
                            return new DiagnosticInfo();
                        }

                        return TypeInfo.GetDefaultValue((BuiltInType)(int)id);
                    }

                    switch (id)
                    {
                        case DataTypes.Duration:
                        case DataTypes.Number:
                            {
                                return (double)0;
                            }
                        case DataTypes.Date:
                        case DataTypes.UtcTime:
                            {
                                return DateTime.MinValue;
                            }
                        case DataTypes.Counter:
                        case DataTypes.IntegerId:
                            {
                                return (uint)0;
                            }
                        case DataTypes.UInteger:
                            {
                                return (ulong)0;
                            }
                        case DataTypes.Integer:
                            {
                                return (long)0;
                            }
                        case DataTypes.IdType:
                            {
                                return (int)IdType.Numeric;
                            }
                        case DataTypes.NodeClass:
                            {
                                return (int)NodeClass.Unspecified;
                            }
                        case DataTypes.Enumeration:
                            {
                                return (int)0;
                            }
                        default:
                            break;
                    }
                }

                if (session != null && session.CoreSession != null)
                {
                    builtInType = TypeInfo.GetBuiltInType(dataType, session.CoreSession.TypeTree);

                    if (builtInType != BuiltInType.Null)
                    {
                        if (builtInType == BuiltInType.String)
                        {
                            return string.Empty;
                        }
                        else if (builtInType == BuiltInType.XmlElement)
                        {
                            System.Xml.XmlDocument document = new System.Xml.XmlDocument();
                            document.InnerXml = NullXml;
                            return document.DocumentElement;
                        }
                        else if (builtInType == BuiltInType.ByteString)
                        {
                            return new byte[0];
                        }
                        //else if (builtInType == BuiltInType.Enumeration)
                        //{
                        //    if (session.CoreSession.Factory.GetEnumeratedType(new ExpandedNodeId(dataType)) != null)
                        //    {
                        //        return new EnumValue(new ExpandedNodeId(dataType), session.Factory);
                        //    }

                        //    Type type = Argument.GetSystemType(dataType, session.Factory);

                        //    if (type != null && type.IsEnum)
                        //    {
                        //        return new EnumValue(type);
                        //    }

                        //    return null;
                        //}
                        //else if (builtInType == BuiltInType.ExtensionObject)
                        //{
                        //    Type type = Argument.GetSystemType(dataType, session.Factory);

                        //    if (type != null)
                        //    {
                        //        return Activator.CreateInstance(type);
                        //    }

                        //    StructuredValue structuredValue = new StructuredValue(Softing.Opc.Ua.Toolkit.NodeId.ToExpandedNodeId(dataType, session.NamespaceUris), session.Factory);
                        //    return structuredValue;
                        //}

                        return TypeInfo.GetDefaultValue(builtInType);
                    }
                }
            }
            else
            {
                if (session.CoreSession != null)
                {
                    Type type = TypeInfo.GetSystemType(dataType, session.Factory);

                    //if (type == null)
                    //{
                    //    ExpandedNodeId typeId = NodeId.ToExpandedNodeId(dataType, session.CoreSession.NamespaceUris);
                    //    if (session.CoreSession.Factory.GetSystemType(typeId) != null)
                    //    {
                    //        type = typeof(StructuredValue);
                    //    }
                    //    else if (session.CoreSession.Factory.GetEnumeratedType(new ExpandedNodeId(dataType).Wrapped) != null)
                    //    {
                    //        type = typeof(EnumValue);
                    //    }
                    //    else
                    //    {
                    //        return null;
                    //    }
                    //}

                    try
                    {
                        if (valueRank == 0)
                        {
                            valueRank = ValueRanks.OneDimension;
                        }

                        return Array.CreateInstance(type, new int[(int)valueRank]);
                    }
                    catch (Exception ex)
                    {
                        TraceService.Log(TraceMasks.Error, TraceSources.ClientAPI, "Argument.GetDefaultValueForDatatype", ex);
                        return null;
                    }
                }
            }

            return null;
        }
    }
}
