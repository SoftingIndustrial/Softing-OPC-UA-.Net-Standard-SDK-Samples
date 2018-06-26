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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Xml;
using Opc.Ua;
using Opc.Ua.Server;
using Softing.Opc.Ua.Server;

namespace SampleServer.ReferenceServer
{
    public class ReferenceNodeManager : NodeManager
    {
        #region Private Members
        
        private Opc.Ua.Test.DataGenerator m_generator;
        private Timer m_simulationTimer;
        private UInt16 m_simulationInterval = 1000;
        private bool m_simulationEnabled = true;
        private readonly List<BaseDataVariableState> m_dynamicNodes;
        private Dictionary<string, int> m_usedIdentifiers;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes the node manager.
        /// </summary>
        public ReferenceNodeManager(IServerInternal server, ApplicationConfiguration configuration) : base(server, configuration, Namespaces.ReferenceApplications)
        {
            m_usedIdentifiers = new Dictionary<string, int>();
            m_dynamicNodes = new List<BaseDataVariableState>();
        }

        #endregion

        #region IDisposable Members

        /// <summary>
        /// An overrideable version of the Dispose.
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                // TBD
            }
        }

        #endregion

        #region INodeIdFactory Members

        /// <summary>
        /// Creates the NodeId for the specified node.
        /// </summary>
        public override NodeId New(ISystemContext context, NodeState node)
        {
            BaseInstanceState instance = node as BaseInstanceState;

            if (instance != null && instance.Parent != null && instance.Parent.NodeId != null)
            {
                string id = instance.Parent.NodeId.Identifier as string;

                if (id != null)
                {
                    id += "_" + instance.SymbolicName;
                    //ensure id uniqueness
                    if (!m_usedIdentifiers.ContainsKey(id))
                    {
                        m_usedIdentifiers.Add(id, 1);
                        return new NodeId(id, NamespaceIndex);
                    }
                    else
                    {
                        m_usedIdentifiers[id] = m_usedIdentifiers[id] + 1;
                        id += m_usedIdentifiers[id];
                        return new NodeId(id, NamespaceIndex);
                    }
                }
            }
            else if (node != null)
            {
                if (node.BrowseName != null && !m_usedIdentifiers.ContainsKey(node.BrowseName.Name))
                {
                    m_usedIdentifiers.Add(node.BrowseName.Name, 1);
                    return new NodeId(node.BrowseName.Name, NamespaceIndex);
                }
            }

            return base.New(SystemContext, node);
        }

        #endregion

        #region Private Helper Functions

        private static bool IsUnsignedAnalogType(BuiltInType builtInType)
        {
            if (builtInType == BuiltInType.Byte ||
                builtInType == BuiltInType.UInt16 ||
                builtInType == BuiltInType.UInt32 ||
                builtInType == BuiltInType.UInt64)
            {
                return true;
            }
            return false;
        }

        private static bool IsAnalogType(BuiltInType builtInType)
        {
            switch (builtInType)
            {
                case BuiltInType.Byte:
                case BuiltInType.UInt16:
                case BuiltInType.UInt32:
                case BuiltInType.UInt64:
                case BuiltInType.SByte:
                case BuiltInType.Int16:
                case BuiltInType.Int32:
                case BuiltInType.Int64:
                case BuiltInType.Float:
                case BuiltInType.Double:
                    return true;
            }
            return false;
        }

        private static Opc.Ua.Range GetAnalogRange(BuiltInType builtInType)
        {
            switch (builtInType)
            {
                case BuiltInType.UInt16:
                    return new Range(System.UInt16.MaxValue, System.UInt16.MinValue);
                case BuiltInType.UInt32:
                    return new Range(System.UInt32.MaxValue, System.UInt32.MinValue);
                case BuiltInType.UInt64:
                    return new Range(System.UInt64.MaxValue, System.UInt64.MinValue);
                case BuiltInType.SByte:
                    return new Range(System.SByte.MaxValue, System.SByte.MinValue);
                case BuiltInType.Int16:
                    return new Range(System.Int16.MaxValue, System.Int16.MinValue);
                case BuiltInType.Int32:
                    return new Range(System.Int32.MaxValue, System.Int32.MinValue);
                case BuiltInType.Int64:
                    return new Range(System.Int64.MaxValue, System.Int64.MinValue);
                case BuiltInType.Float:
                    return new Range(System.Single.MaxValue, System.Single.MinValue);
                case BuiltInType.Double:
                    return new Range(System.Double.MaxValue, System.Double.MinValue);
                case BuiltInType.Byte:
                    return new Range(System.Byte.MaxValue, System.Byte.MinValue);
                default:
                    return new Range(System.SByte.MaxValue, System.SByte.MinValue);
            }
        }

        #endregion

        #region INodeManager Members

        /// <summary>
        /// Does any initialization required before the address space can be used.
        /// </summary>
        /// <remarks>
        /// The externalReferences is an out parameter that allows the node manager to link to nodes
        /// in other node managers. For example, the 'Objects' node is managed by the CoreNodeManager and
        /// should have a reference to the root folder node(s) exposed by this node manager.  
        /// </remarks>
        public override void CreateAddressSpace(IDictionary<NodeId, IList<IReference>> externalReferences)
        {
            lock (Lock)
            {
                base.CreateAddressSpace(externalReferences);

                FolderState root = CreateFolder(null, "CTT");
                root.EventNotifier = EventNotifiers.SubscribeToEvents;
                AddReference(root, ReferenceTypeIds.Organizes, true, ObjectIds.ObjectsFolder, true);

                // Add Support for Event Notifiers
                // Creating notifier ensures events propagate up the hierarchy when they are produced
                AddRootNotifier(root);

                List<BaseDataVariableState> variables = new List<BaseDataVariableState>();

                try
                {
                    #region Scalar_Static

                    FolderState scalarFolder = CreateFolder(root, "Scalar");
                    base.CreateVariable(scalarFolder, "Scalar_Instructions", DataTypeIds.String);
                    BaseDataVariableState scalarInstructions = base.CreateVariable(scalarFolder, "Scalar_Instructions", DataTypeIds.String);
                    scalarInstructions.Value = "A library of Read/Write Variables of all supported data-types.";
                    variables.Add(scalarInstructions);

                    FolderState staticFolder = CreateFolder(scalarFolder, "Scalar_Static");
                    variables.Add(CreateVariable(staticFolder, "Boolean", DataTypeIds.Boolean, ValueRanks.Scalar));
                    variables.Add(CreateVariable(staticFolder, "Byte", DataTypeIds.Byte, ValueRanks.Scalar));
                    variables.Add(CreateVariable(staticFolder, "ByteString", DataTypeIds.ByteString, ValueRanks.Scalar));
                    variables.Add(CreateVariable(staticFolder, "DateTime", DataTypeIds.DateTime, ValueRanks.Scalar));
                    variables.Add(CreateVariable(staticFolder, "Double", DataTypeIds.Double, ValueRanks.Scalar));
                    variables.Add(CreateVariable(staticFolder, "Duration", DataTypeIds.Duration, ValueRanks.Scalar));
                    variables.Add(CreateVariable(staticFolder, "Float", DataTypeIds.Float, ValueRanks.Scalar));
                    variables.Add(CreateVariable(staticFolder, "Guid", DataTypeIds.Guid, ValueRanks.Scalar));
                    variables.Add(CreateVariable(staticFolder, "Int16", DataTypeIds.Int16, ValueRanks.Scalar));
                    variables.Add(CreateVariable(staticFolder, "Int32", DataTypeIds.Int32, ValueRanks.Scalar));
                    variables.Add(CreateVariable(staticFolder, "Int64", DataTypeIds.Int64, ValueRanks.Scalar));
                    variables.Add(CreateVariable(staticFolder, "Integer", DataTypeIds.Integer, ValueRanks.Scalar));
                    variables.Add(CreateVariable(staticFolder, "LocaleId", DataTypeIds.LocaleId, ValueRanks.Scalar));
                    variables.Add(CreateVariable(staticFolder, "LocalizedText", DataTypeIds.LocalizedText, ValueRanks.Scalar));
                    variables.Add(CreateVariable(staticFolder, "NodeId", DataTypeIds.NodeId, ValueRanks.Scalar));
                    variables.Add(CreateVariable(staticFolder, "Number", DataTypeIds.Number, ValueRanks.Scalar));
                    variables.Add(CreateVariable(staticFolder, "QualifiedName", DataTypeIds.QualifiedName, ValueRanks.Scalar));
                    variables.Add(CreateVariable(staticFolder, "SByte", DataTypeIds.SByte, ValueRanks.Scalar));
                    variables.Add(CreateVariable(staticFolder, "String", DataTypeIds.String, ValueRanks.Scalar));
                    variables.Add(CreateVariable(staticFolder, "Time", DataTypeIds.Time, ValueRanks.Scalar));
                    variables.Add(CreateVariable(staticFolder, "UInt16", DataTypeIds.UInt16, ValueRanks.Scalar));
                    variables.Add(CreateVariable(staticFolder, "UInt32", DataTypeIds.UInt32, ValueRanks.Scalar));
                    variables.Add(CreateVariable(staticFolder, "UInt64", DataTypeIds.UInt64, ValueRanks.Scalar));
                    variables.Add(CreateVariable(staticFolder, "UInteger", DataTypeIds.UInteger, ValueRanks.Scalar));
                    variables.Add(CreateVariable(staticFolder, "UtcTime", DataTypeIds.UtcTime, ValueRanks.Scalar));
                    variables.Add(CreateVariable(staticFolder,"Variant", (uint)BuiltInType.Variant, ValueRanks.Scalar));
                    variables.Add(CreateVariable(staticFolder, "XmlElement", DataTypeIds.XmlElement, ValueRanks.Scalar));

                    #endregion

                    #region Scalar_Static_Arrays

                    FolderState arraysFolder = CreateFolder(staticFolder, "Arrays");
                    variables.Add(CreateVariable(arraysFolder, "Boolean", DataTypeIds.Boolean, ValueRanks.OneDimension));
                    variables.Add(CreateVariable(arraysFolder, "Byte", DataTypeIds.Byte, ValueRanks.OneDimension));
                    variables.Add(CreateVariable(arraysFolder, "ByteString", DataTypeIds.ByteString, ValueRanks.OneDimension));
                    variables.Add(CreateVariable(arraysFolder, "DateTime", DataTypeIds.DateTime, ValueRanks.OneDimension));

                    BaseDataVariableState doubleArrayVar = CreateVariable(arraysFolder, "Double", DataTypeIds.Double, ValueRanks.OneDimension);
                    // Set the first elements of the array to a smaller value.
                    double[] doubleArrayVal = doubleArrayVar.Value as double[];
                    doubleArrayVal[0] %= 10E+10;
                    doubleArrayVal[1] %= 10E+10;
                    doubleArrayVal[2] %= 10E+10;
                    doubleArrayVal[3] %= 10E+10;
                    variables.Add(doubleArrayVar);

                    variables.Add(CreateVariable(arraysFolder, "Duration", DataTypeIds.Duration, ValueRanks.OneDimension));

                    BaseDataVariableState floatArrayVar = CreateVariable(arraysFolder, "Float", DataTypeIds.Float, ValueRanks.OneDimension);
                    // Set the first elements of the array to a smaller value.
                    float[] floatArrayVal = floatArrayVar.Value as float[];
                    floatArrayVal[0] %= 0xf10E + 4;
                    floatArrayVal[1] %= 0xf10E + 4;
                    floatArrayVal[2] %= 0xf10E + 4;
                    floatArrayVal[3] %= 0xf10E + 4;
                    variables.Add(floatArrayVar);

                    variables.Add(CreateVariable(arraysFolder, "Guid", DataTypeIds.Guid, ValueRanks.OneDimension));
                    variables.Add(CreateVariable(arraysFolder, "Int16", DataTypeIds.Int16, ValueRanks.OneDimension));
                    variables.Add(CreateVariable(arraysFolder, "Int32", DataTypeIds.Int32, ValueRanks.OneDimension));
                    variables.Add(CreateVariable(arraysFolder, "Int64", DataTypeIds.Int64, ValueRanks.OneDimension));
                    variables.Add(CreateVariable(arraysFolder, "Integer", DataTypeIds.Integer, ValueRanks.OneDimension));
                    variables.Add(CreateVariable(arraysFolder, "LocaleId", DataTypeIds.LocaleId, ValueRanks.OneDimension));
                    variables.Add(CreateVariable(arraysFolder, "LocalizedText", DataTypeIds.LocalizedText, ValueRanks.OneDimension));
                    variables.Add(CreateVariable(arraysFolder, "NodeId", DataTypeIds.NodeId, ValueRanks.OneDimension));
                    variables.Add(CreateVariable(arraysFolder, "Number", DataTypeIds.Number, ValueRanks.OneDimension));
                    variables.Add(CreateVariable(arraysFolder, "QualifiedName", DataTypeIds.QualifiedName, ValueRanks.OneDimension));
                    variables.Add(CreateVariable(arraysFolder, "SByte", DataTypeIds.SByte, ValueRanks.OneDimension));

                    BaseDataVariableState stringArrayVar = CreateVariable(arraysFolder,  "String", DataTypeIds.String, ValueRanks.OneDimension);
                    stringArrayVar.Value = new string[]
                    {
                        "Лошадь_ Пурпурово( Змейка( Слон",
                        "猪 绿色 绵羊 大象~ 狗 菠萝 猪鼠",
                        "Лошадь Овцы Голубика Овцы Змейка",
                        "Чернота` Дракон Бело Дракон",
                        "Horse# Black Lemon Lemon Grape",
                        "猫< パイナップル; ドラゴン 犬 モモ",
                        "레몬} 빨간% 자주색 쥐 백색; 들",
                        "Yellow Sheep Peach Elephant Cow",
                        "Крыса Корова Свинья Собака Кот",
                        "龙_ 绵羊 大象 芒果; 猫'"
                    };
                    variables.Add(stringArrayVar);

                    variables.Add(CreateVariable(arraysFolder, "Time", DataTypeIds.Time, ValueRanks.OneDimension));
                    variables.Add(CreateVariable(arraysFolder, "UInt16", DataTypeIds.UInt16, ValueRanks.OneDimension));
                    variables.Add(CreateVariable(arraysFolder,  "UInt32", DataTypeIds.UInt32, ValueRanks.OneDimension));
                    variables.Add(CreateVariable(arraysFolder,"UInt64", DataTypeIds.UInt64, ValueRanks.OneDimension));
                    variables.Add(CreateVariable(arraysFolder, "UInteger", DataTypeIds.UInteger, ValueRanks.OneDimension));
                    variables.Add(CreateVariable(arraysFolder, "UtcTime", DataTypeIds.UtcTime, ValueRanks.OneDimension));
                    variables.Add(CreateVariable(arraysFolder,"Variant", BuiltInType.Variant, ValueRanks.OneDimension));
                    variables.Add(CreateVariable(arraysFolder, "XmlElement", DataTypeIds.XmlElement, ValueRanks.OneDimension));

                    #endregion

                    #region Scalar_Static_Arrays2D

                    FolderState arrays2DFolder = CreateFolder(staticFolder, "Arrays2D");
                    variables.Add(CreateVariable(arrays2DFolder, "Boolean", DataTypeIds.Boolean, ValueRanks.TwoDimensions));
                    variables.Add(CreateVariable(arrays2DFolder, "Byte", DataTypeIds.Byte, ValueRanks.TwoDimensions));
                    variables.Add(CreateVariable(arrays2DFolder, "ByteString", DataTypeIds.ByteString, ValueRanks.TwoDimensions));
                    variables.Add(CreateVariable(arrays2DFolder, "DateTime", DataTypeIds.DateTime, ValueRanks.TwoDimensions));
                    variables.Add(CreateVariable(arrays2DFolder, "Double", DataTypeIds.Double, ValueRanks.TwoDimensions));
                    variables.Add(CreateVariable(arrays2DFolder, "Duration", DataTypeIds.Duration, ValueRanks.TwoDimensions));
                    variables.Add(CreateVariable(arrays2DFolder, "Float", DataTypeIds.Float, ValueRanks.TwoDimensions));
                    variables.Add(CreateVariable(arrays2DFolder, "Guid", DataTypeIds.Guid, ValueRanks.TwoDimensions));
                    variables.Add(CreateVariable(arrays2DFolder, "Int16", DataTypeIds.Int16, ValueRanks.TwoDimensions));
                    variables.Add(CreateVariable(arrays2DFolder, "Int32", DataTypeIds.Int32, ValueRanks.TwoDimensions));
                    variables.Add(CreateVariable(arrays2DFolder, "Int64", DataTypeIds.Int64, ValueRanks.TwoDimensions));
                    variables.Add(CreateVariable(arrays2DFolder, "Integer", DataTypeIds.Integer, ValueRanks.TwoDimensions));
                    variables.Add(CreateVariable(arrays2DFolder, "LocaleId", DataTypeIds.LocaleId, ValueRanks.TwoDimensions));
                    variables.Add(CreateVariable(arrays2DFolder, "LocalizedText", DataTypeIds.LocalizedText, ValueRanks.TwoDimensions));
                    variables.Add(CreateVariable(arrays2DFolder, "NodeId", DataTypeIds.NodeId, ValueRanks.TwoDimensions));
                    variables.Add(CreateVariable(arrays2DFolder, "Number", DataTypeIds.Number, ValueRanks.TwoDimensions));
                    variables.Add(CreateVariable(arrays2DFolder, "QualifiedName", DataTypeIds.QualifiedName, ValueRanks.TwoDimensions));
                    variables.Add(CreateVariable(arrays2DFolder, "SByte", DataTypeIds.SByte, ValueRanks.TwoDimensions));
                    variables.Add(CreateVariable(arrays2DFolder, "String", DataTypeIds.String, ValueRanks.TwoDimensions));
                    variables.Add(CreateVariable(arrays2DFolder, "Time", DataTypeIds.Time, ValueRanks.TwoDimensions));
                    variables.Add(CreateVariable(arrays2DFolder, "UInt16", DataTypeIds.UInt16, ValueRanks.TwoDimensions));
                    variables.Add(CreateVariable(arrays2DFolder, "UInt32", DataTypeIds.UInt32, ValueRanks.TwoDimensions));
                    variables.Add(CreateVariable(arrays2DFolder, "UInt64", DataTypeIds.UInt64, ValueRanks.TwoDimensions));
                    variables.Add(CreateVariable(arrays2DFolder, "UInteger", DataTypeIds.UInteger, ValueRanks.TwoDimensions));
                    variables.Add(CreateVariable(arrays2DFolder, "UtcTime", DataTypeIds.UtcTime, ValueRanks.TwoDimensions));
                    variables.Add(CreateVariable(arrays2DFolder, "Variant", BuiltInType.Variant, ValueRanks.TwoDimensions));
                    variables.Add(CreateVariable(arrays2DFolder, "XmlElement", DataTypeIds.XmlElement, ValueRanks.TwoDimensions));

                    #endregion

                    #region Scalar_Static_ArrayDynamic

                    FolderState arrayDymnamicFolder = CreateFolder(staticFolder, "ArrayDymamic");
                    variables.Add(CreateVariable(arrayDymnamicFolder, "Boolean", DataTypeIds.Boolean, ValueRanks.OneOrMoreDimensions));
                    variables.Add(CreateVariable(arrayDymnamicFolder, "Byte", DataTypeIds.Byte, ValueRanks.OneOrMoreDimensions));
                    variables.Add(CreateVariable(arrayDymnamicFolder, "ByteString", DataTypeIds.ByteString, ValueRanks.OneOrMoreDimensions));
                    variables.Add(CreateVariable(arrayDymnamicFolder, "DateTime", DataTypeIds.DateTime, ValueRanks.OneOrMoreDimensions));
                    variables.Add(CreateVariable(arrayDymnamicFolder, "Double", DataTypeIds.Double, ValueRanks.OneOrMoreDimensions));
                    variables.Add(CreateVariable(arrayDymnamicFolder, "Duration", DataTypeIds.Duration, ValueRanks.OneOrMoreDimensions));
                    variables.Add(CreateVariable(arrayDymnamicFolder, "Float", DataTypeIds.Float, ValueRanks.OneOrMoreDimensions));
                    variables.Add(CreateVariable(arrayDymnamicFolder, "Guid", DataTypeIds.Guid, ValueRanks.OneOrMoreDimensions));
                    variables.Add(CreateVariable(arrayDymnamicFolder, "Int16", DataTypeIds.Int16, ValueRanks.OneOrMoreDimensions));
                    variables.Add(CreateVariable(arrayDymnamicFolder, "Int32", DataTypeIds.Int32, ValueRanks.OneOrMoreDimensions));
                    variables.Add(CreateVariable(arrayDymnamicFolder, "Int64", DataTypeIds.Int64, ValueRanks.OneOrMoreDimensions));
                    variables.Add(CreateVariable(arrayDymnamicFolder, "Integer", DataTypeIds.Integer, ValueRanks.OneOrMoreDimensions));
                    variables.Add(CreateVariable(arrayDymnamicFolder, "LocaleId", DataTypeIds.LocaleId, ValueRanks.OneOrMoreDimensions));
                    variables.Add(CreateVariable(arrayDymnamicFolder, "LocalizedText", DataTypeIds.LocalizedText, ValueRanks.OneOrMoreDimensions));
                    variables.Add(CreateVariable(arrayDymnamicFolder, "NodeId", DataTypeIds.NodeId, ValueRanks.OneOrMoreDimensions));
                    variables.Add(CreateVariable(arrayDymnamicFolder, "Number", DataTypeIds.Number, ValueRanks.OneOrMoreDimensions));
                    variables.Add(CreateVariable(arrayDymnamicFolder, "QualifiedName", DataTypeIds.QualifiedName, ValueRanks.OneOrMoreDimensions));
                    variables.Add(CreateVariable(arrayDymnamicFolder, "SByte", DataTypeIds.SByte, ValueRanks.OneOrMoreDimensions));
                    variables.Add(CreateVariable(arrayDymnamicFolder, "String", DataTypeIds.String, ValueRanks.OneOrMoreDimensions));
                    variables.Add(CreateVariable(arrayDymnamicFolder, "Time", DataTypeIds.Time, ValueRanks.OneOrMoreDimensions));
                    variables.Add(CreateVariable(arrayDymnamicFolder, "UInt16", DataTypeIds.UInt16, ValueRanks.OneOrMoreDimensions));
                    variables.Add(CreateVariable(arrayDymnamicFolder, "UInt32", DataTypeIds.UInt32, ValueRanks.OneOrMoreDimensions));
                    variables.Add(CreateVariable(arrayDymnamicFolder, "UInt64", DataTypeIds.UInt64, ValueRanks.OneOrMoreDimensions));
                    variables.Add(CreateVariable(arrayDymnamicFolder, "UInteger", DataTypeIds.UInteger, ValueRanks.OneOrMoreDimensions));
                    variables.Add(CreateVariable(arrayDymnamicFolder, "UtcTime", DataTypeIds.UtcTime, ValueRanks.OneOrMoreDimensions));
                    variables.Add(CreateVariable(arrayDymnamicFolder, "Variant", BuiltInType.Variant, ValueRanks.OneOrMoreDimensions));
                    variables.Add(CreateVariable(arrayDymnamicFolder, "XmlElement", DataTypeIds.XmlElement, ValueRanks.OneOrMoreDimensions));

                    #endregion

                    #region Scalar_Static_Mass

                    // create 100 instances of each static scalar type
                    FolderState massFolder = CreateFolder(staticFolder, "Mass");
                    variables.AddRange(CreateVariables(massFolder, "Boolean", "Boolean", DataTypeIds.Boolean, ValueRanks.Scalar, 100));
                    variables.AddRange(CreateVariables(massFolder, "Byte", "Byte", DataTypeIds.Byte, ValueRanks.Scalar, 100));
                    variables.AddRange(CreateVariables(massFolder, "ByteString", "ByteString", DataTypeIds.ByteString, ValueRanks.Scalar, 100));
                    variables.AddRange(CreateVariables(massFolder, "DateTime", "DateTime", DataTypeIds.DateTime, ValueRanks.Scalar, 100));
                    variables.AddRange(CreateVariables(massFolder, "Double", "Double", DataTypeIds.Double, ValueRanks.Scalar, 100));
                    variables.AddRange(CreateVariables(massFolder, "Duration", "Duration", DataTypeIds.Duration, ValueRanks.Scalar, 100));
                    variables.AddRange(CreateVariables(massFolder, "Float", "Float", DataTypeIds.Float, ValueRanks.Scalar, 100));
                    variables.AddRange(CreateVariables(massFolder, "Guid", "Guid", DataTypeIds.Guid, ValueRanks.Scalar, 100));
                    variables.AddRange(CreateVariables(massFolder, "Int16", "Int16", DataTypeIds.Int16, ValueRanks.Scalar, 100));
                    variables.AddRange(CreateVariables(massFolder, "Int32", "Int32", DataTypeIds.Int32, ValueRanks.Scalar, 100));
                    variables.AddRange(CreateVariables(massFolder, "Int64", "Int64", DataTypeIds.Int64, ValueRanks.Scalar, 100));
                    variables.AddRange(CreateVariables(massFolder, "Integer", "Integer", DataTypeIds.Integer, ValueRanks.Scalar, 100));
                    variables.AddRange(CreateVariables(massFolder, "LocalizedText", "LocalizedText", DataTypeIds.LocalizedText, ValueRanks.Scalar, 100));
                    variables.AddRange(CreateVariables(massFolder, "NodeId", "NodeId", DataTypeIds.NodeId, ValueRanks.Scalar, 100));
                    variables.AddRange(CreateVariables(massFolder, "Number", "Number", DataTypeIds.Number, ValueRanks.Scalar, 100));
                    variables.AddRange(CreateVariables(massFolder, "SByte", "SByte", DataTypeIds.SByte, ValueRanks.Scalar, 100));
                    variables.AddRange(CreateVariables(massFolder, "String", "String", DataTypeIds.String, ValueRanks.Scalar, 100));
                    variables.AddRange(CreateVariables(massFolder, "Time", "Time", DataTypeIds.Time, ValueRanks.Scalar, 100));
                    variables.AddRange(CreateVariables(massFolder, "UInt16", "UInt16", DataTypeIds.UInt16, ValueRanks.Scalar, 100));
                    variables.AddRange(CreateVariables(massFolder, "UInt32", "UInt32", DataTypeIds.UInt32, ValueRanks.Scalar, 100));
                    variables.AddRange(CreateVariables(massFolder, "UInt64", "UInt64", DataTypeIds.UInt64, ValueRanks.Scalar, 100));
                    variables.AddRange(CreateVariables(massFolder, "UInteger", "UInteger", DataTypeIds.UInteger, ValueRanks.Scalar, 100));
                    variables.AddRange(CreateVariables(massFolder, "UtcTime", "UtcTime", DataTypeIds.UtcTime, ValueRanks.Scalar, 100));
                    variables.AddRange(CreateVariables(massFolder, "Variant", "Variant", BuiltInType.Variant, ValueRanks.Scalar, 100));
                    variables.AddRange(CreateVariables(massFolder, "XmlElement", "XmlElement", DataTypeIds.XmlElement, ValueRanks.Scalar, 100));

                    #endregion

                    #region Scalar_Simulation

                    FolderState simulationFolder = CreateFolder(scalarFolder, "Scalar_Simulation", "Simulation");
                    CreateDynamicVariable(simulationFolder, "Boolean", DataTypeIds.Boolean, ValueRanks.Scalar);
                    CreateDynamicVariable(simulationFolder, "Byte", DataTypeIds.Byte, ValueRanks.Scalar);
                    CreateDynamicVariable(simulationFolder, "ByteString", DataTypeIds.ByteString, ValueRanks.Scalar);
                    CreateDynamicVariable(simulationFolder, "DateTime", DataTypeIds.DateTime, ValueRanks.Scalar);
                    CreateDynamicVariable(simulationFolder, "Double", DataTypeIds.Double, ValueRanks.Scalar);
                    CreateDynamicVariable(simulationFolder, "Duration", DataTypeIds.Duration, ValueRanks.Scalar);
                    CreateDynamicVariable(simulationFolder, "Float", DataTypeIds.Float, ValueRanks.Scalar);
                    CreateDynamicVariable(simulationFolder, "Guid", DataTypeIds.Guid, ValueRanks.Scalar);
                    CreateDynamicVariable(simulationFolder, "Int16", DataTypeIds.Int16, ValueRanks.Scalar);
                    CreateDynamicVariable(simulationFolder, "Int32", DataTypeIds.Int32, ValueRanks.Scalar);
                    CreateDynamicVariable(simulationFolder, "Int64", DataTypeIds.Int64, ValueRanks.Scalar);
                    CreateDynamicVariable(simulationFolder, "Integer", DataTypeIds.Integer, ValueRanks.Scalar);
                    CreateDynamicVariable(simulationFolder, "LocaleId", DataTypeIds.LocaleId, ValueRanks.Scalar);
                    CreateDynamicVariable(simulationFolder, "LocalizedText", DataTypeIds.LocalizedText, ValueRanks.Scalar);
                    CreateDynamicVariable(simulationFolder, "NodeId", DataTypeIds.NodeId, ValueRanks.Scalar);
                    CreateDynamicVariable(simulationFolder, "Number", DataTypeIds.Number, ValueRanks.Scalar);
                    CreateDynamicVariable(simulationFolder, "QualifiedName", DataTypeIds.QualifiedName, ValueRanks.Scalar);
                    CreateDynamicVariable(simulationFolder, "SByte", DataTypeIds.SByte, ValueRanks.Scalar);
                    CreateDynamicVariable(simulationFolder, "String", DataTypeIds.String, ValueRanks.Scalar);
                    CreateDynamicVariable(simulationFolder, "Time", DataTypeIds.Time, ValueRanks.Scalar);
                    CreateDynamicVariable(simulationFolder, "UInt16", DataTypeIds.UInt16, ValueRanks.Scalar);
                    CreateDynamicVariable(simulationFolder, "UInt32", DataTypeIds.UInt32, ValueRanks.Scalar);
                    CreateDynamicVariable(simulationFolder, "UInt64", DataTypeIds.UInt64, ValueRanks.Scalar);
                    CreateDynamicVariable(simulationFolder, "UInteger", DataTypeIds.UInteger, ValueRanks.Scalar);
                    CreateDynamicVariable(simulationFolder, "UtcTime", DataTypeIds.UtcTime, ValueRanks.Scalar);
                    CreateDynamicVariable(simulationFolder, "Variant", (uint)BuiltInType.Variant, ValueRanks.Scalar);
                    CreateDynamicVariable(simulationFolder, "XmlElement", DataTypeIds.XmlElement, ValueRanks.Scalar);

                    BaseDataVariableState intervalVariable = CreateVariable(simulationFolder, "Interval", DataTypeIds.UInt16, ValueRanks.Scalar);
                    intervalVariable.Value = m_simulationInterval;
                    intervalVariable.OnSimpleWriteValue = OnWriteInterval;

                    BaseDataVariableState enabledVariable = CreateVariable(simulationFolder, "Enabled", DataTypeIds.Boolean, ValueRanks.Scalar);
                    enabledVariable.Value = m_simulationEnabled;
                    enabledVariable.OnSimpleWriteValue = OnWriteEnabled;

                    #endregion

                    #region Scalar_Simulation_Arrays

                    FolderState arraysSimulationFolder = CreateFolder(simulationFolder, "Arrays");
                    CreateDynamicVariable(arraysSimulationFolder, "Boolean", DataTypeIds.Boolean, ValueRanks.OneDimension);
                    CreateDynamicVariable(arraysSimulationFolder, "Byte", DataTypeIds.Byte, ValueRanks.OneDimension);
                    CreateDynamicVariable(arraysSimulationFolder, "ByteString", DataTypeIds.ByteString, ValueRanks.OneDimension);
                    CreateDynamicVariable(arraysSimulationFolder, "DateTime", DataTypeIds.DateTime, ValueRanks.OneDimension);
                    CreateDynamicVariable(arraysSimulationFolder, "Double", DataTypeIds.Double, ValueRanks.OneDimension);
                    CreateDynamicVariable(arraysSimulationFolder, "Duration", DataTypeIds.Duration, ValueRanks.OneDimension);
                    CreateDynamicVariable(arraysSimulationFolder, "Float", DataTypeIds.Float, ValueRanks.OneDimension);
                    CreateDynamicVariable(arraysSimulationFolder, "Guid", DataTypeIds.Guid, ValueRanks.OneDimension);
                    CreateDynamicVariable(arraysSimulationFolder, "Int16", DataTypeIds.Int16, ValueRanks.OneDimension);
                    CreateDynamicVariable(arraysSimulationFolder, "Int32", DataTypeIds.Int32, ValueRanks.OneDimension);
                    CreateDynamicVariable(arraysSimulationFolder, "Int64", DataTypeIds.Int64, ValueRanks.OneDimension);
                    CreateDynamicVariable(arraysSimulationFolder, "Integer", DataTypeIds.Integer, ValueRanks.OneDimension);
                    CreateDynamicVariable(arraysSimulationFolder, "LocaleId", DataTypeIds.LocaleId, ValueRanks.OneDimension);
                    CreateDynamicVariable(arraysSimulationFolder, "LocalizedText", DataTypeIds.LocalizedText, ValueRanks.OneDimension);
                    CreateDynamicVariable(arraysSimulationFolder, "NodeId", DataTypeIds.NodeId, ValueRanks.OneDimension);
                    CreateDynamicVariable(arraysSimulationFolder, "Number", DataTypeIds.Number, ValueRanks.OneDimension);
                    CreateDynamicVariable(arraysSimulationFolder, "QualifiedName", DataTypeIds.QualifiedName, ValueRanks.OneDimension);
                    CreateDynamicVariable(arraysSimulationFolder, "SByte", DataTypeIds.SByte, ValueRanks.OneDimension);
                    CreateDynamicVariable(arraysSimulationFolder, "String", DataTypeIds.String, ValueRanks.OneDimension);
                    CreateDynamicVariable(arraysSimulationFolder, "Time", DataTypeIds.Time, ValueRanks.OneDimension);
                    CreateDynamicVariable(arraysSimulationFolder, "UInt16", DataTypeIds.UInt16, ValueRanks.OneDimension);
                    CreateDynamicVariable(arraysSimulationFolder, "UInt32", DataTypeIds.UInt32, ValueRanks.OneDimension);
                    CreateDynamicVariable(arraysSimulationFolder, "UInt64", DataTypeIds.UInt64, ValueRanks.OneDimension);
                    CreateDynamicVariable(arraysSimulationFolder, "UInteger", DataTypeIds.UInteger, ValueRanks.OneDimension);
                    CreateDynamicVariable(arraysSimulationFolder, "UtcTime", DataTypeIds.UtcTime, ValueRanks.OneDimension);
                    CreateDynamicVariable(arraysSimulationFolder, "Variant", (uint)BuiltInType.Variant, ValueRanks.OneDimension);
                    CreateDynamicVariable(arraysSimulationFolder, "XmlElement", DataTypeIds.XmlElement, ValueRanks.OneDimension);

                    #endregion

                    #region Scalar_Simulation_Mass

                    FolderState massSimulationFolder = CreateFolder(simulationFolder, "Scalar_Simulation_Mass", "Mass");
                    CreateDynamicVariables(massSimulationFolder, "Boolean", DataTypeIds.Boolean, ValueRanks.Scalar, 100);
                    CreateDynamicVariables(massSimulationFolder, "Byte", DataTypeIds.Byte, ValueRanks.Scalar, 100);
                    CreateDynamicVariables(massSimulationFolder, "ByteString", DataTypeIds.ByteString, ValueRanks.Scalar, 100);
                    CreateDynamicVariables(massSimulationFolder, "DateTime", DataTypeIds.DateTime, ValueRanks.Scalar, 100);
                    CreateDynamicVariables(massSimulationFolder, "Double", DataTypeIds.Double, ValueRanks.Scalar, 100);
                    CreateDynamicVariables(massSimulationFolder, "Duration", DataTypeIds.Duration, ValueRanks.Scalar, 100);
                    CreateDynamicVariables(massSimulationFolder, "Float", DataTypeIds.Float, ValueRanks.Scalar, 100);
                    CreateDynamicVariables(massSimulationFolder, "Guid", DataTypeIds.Guid, ValueRanks.Scalar, 100);
                    CreateDynamicVariables(massSimulationFolder, "Int16", DataTypeIds.Int16, ValueRanks.Scalar, 100);
                    CreateDynamicVariables(massSimulationFolder, "Int32", DataTypeIds.Int32, ValueRanks.Scalar, 100);
                    CreateDynamicVariables(massSimulationFolder, "Int64", DataTypeIds.Int64, ValueRanks.Scalar, 100);
                    CreateDynamicVariables(massSimulationFolder, "Integer", DataTypeIds.Integer, ValueRanks.Scalar, 100);
                    CreateDynamicVariables(massSimulationFolder, "LocaleId", DataTypeIds.LocaleId, ValueRanks.Scalar, 100);
                    CreateDynamicVariables(massSimulationFolder, "LocalizedText", DataTypeIds.LocalizedText, ValueRanks.Scalar, 100);
                    CreateDynamicVariables(massSimulationFolder, "NodeId", DataTypeIds.NodeId, ValueRanks.Scalar, 100);
                    CreateDynamicVariables(massSimulationFolder, "Number", DataTypeIds.Number, ValueRanks.Scalar, 100);
                    CreateDynamicVariables(massSimulationFolder, "QualifiedName", DataTypeIds.QualifiedName, ValueRanks.Scalar, 100);
                    CreateDynamicVariables(massSimulationFolder, "SByte", DataTypeIds.SByte, ValueRanks.Scalar, 100);
                    CreateDynamicVariables(massSimulationFolder, "String", DataTypeIds.String, ValueRanks.Scalar, 100);
                    CreateDynamicVariables(massSimulationFolder, "Time", DataTypeIds.Time, ValueRanks.Scalar, 100);
                    CreateDynamicVariables(massSimulationFolder, "UInt16", DataTypeIds.UInt16, ValueRanks.Scalar, 100);
                    CreateDynamicVariables(massSimulationFolder, "UInt32", DataTypeIds.UInt32, ValueRanks.Scalar, 100);
                    CreateDynamicVariables(massSimulationFolder, "UInt64", DataTypeIds.UInt64, ValueRanks.Scalar, 100);
                    CreateDynamicVariables(massSimulationFolder, "UInteger", DataTypeIds.UInteger, ValueRanks.Scalar, 100);
                    CreateDynamicVariables(massSimulationFolder, "UtcTime", DataTypeIds.UtcTime, ValueRanks.Scalar, 100);
                    CreateDynamicVariables(massSimulationFolder, "Variant", (uint)BuiltInType.Variant, ValueRanks.Scalar, 100);
                    CreateDynamicVariables(massSimulationFolder, "XmlElement", DataTypeIds.XmlElement, ValueRanks.Scalar, 100);

                    #endregion

                    #region DataAccess_DataItem

                    FolderState daFolder = CreateFolder(root, "DataAccess");
                    BaseDataVariableState daInstructions = CreateVariable(daFolder, "Instructions", DataTypeIds.String, ValueRanks.Scalar);
                    daInstructions.Value = "A library of Read/Write Variables of all supported data-types.";
                    variables.Add(daInstructions);

                    FolderState dataItemFolder = CreateFolder(daFolder, "DataItem");
                    
                    foreach (string name in Enum.GetNames(typeof(BuiltInType)))
                    {
                        DataItemState item = CreateDataItemVariable(dataItemFolder, name, (BuiltInType) Enum.Parse(typeof(BuiltInType), name), ValueRanks.Scalar);
                        
                        // set initial value to String.Empty for String node.
                        if (name == BuiltInType.String.ToString())
                        {
                            item.Value = String.Empty;
                        }
                    }

                    #endregion

                    #region DataAccess_AnalogType

                    FolderState analogItemFolder = CreateFolder(daFolder, "AnalogType");
                    
                    foreach (string name in Enum.GetNames(typeof(BuiltInType)))
                    {
                        BuiltInType builtInType = (BuiltInType) Enum.Parse(typeof(BuiltInType), name);
                        if (IsAnalogType(builtInType))
                        {
                            AnalogItemState item = CreateAnalogItemVariable(analogItemFolder, name, name, builtInType, ValueRanks.Scalar);

                            if (builtInType == BuiltInType.Int64 ||
                                builtInType == BuiltInType.UInt64)
                            {
                                // make test case without optional ranges
                                item.EngineeringUnits = null;
                                item.InstrumentRange = null;
                            }
                            else if (builtInType == BuiltInType.Float)
                            {
                                item.EURange.Value.High = 0;
                                item.EURange.Value.Low = 0;
                            }
                        }
                    }

                    #endregion

                    #region DataAccess_AnalogType_Array

                    FolderState analogArrayFolder = CreateFolder(analogItemFolder, "AnalogType_Array", "Array");
                    
                    CreateAnalogItemVariable(analogArrayFolder, "Boolean", "Boolean", BuiltInType.Boolean, ValueRanks.OneDimension, new Boolean[] {true, false, true, false, true, false, true, false, true});
                    CreateAnalogItemVariable(analogArrayFolder, "Byte", "Byte", BuiltInType.Byte, ValueRanks.OneDimension, new Byte[] {0, 1, 2, 3, 4, 5, 6, 7, 8, 9});
                    CreateAnalogItemVariable(analogArrayFolder, "ByteString", "ByteString", BuiltInType.ByteString, ValueRanks.OneDimension,
                        new Byte[][]
                        {
                            new Byte[] {0, 1, 2, 3, 4, 5, 6, 7, 8, 9}, new Byte[] {0, 1, 2, 3, 4, 5, 6, 7, 8, 9}, new Byte[] {0, 1, 2, 3, 4, 5, 6, 7, 8, 9}, new Byte[] {0, 1, 2, 3, 4, 5, 6, 7, 8, 9}, new Byte[] {0, 1, 2, 3, 4, 5, 6, 7, 8, 9},
                            new Byte[] {0, 1, 2, 3, 4, 5, 6, 7, 8, 9}, new Byte[] {0, 1, 2, 3, 4, 5, 6, 7, 8, 9}, new Byte[] {0, 1, 2, 3, 4, 5, 6, 7, 8, 9}, new Byte[] {0, 1, 2, 3, 4, 5, 6, 7, 8, 9}, new Byte[] {0, 1, 2, 3, 4, 5, 6, 7, 8, 9}
                        });
                    CreateAnalogItemVariable(analogArrayFolder, "DateTime", "DateTime", BuiltInType.DateTime, ValueRanks.OneDimension,
                        new DateTime[] {DateTime.MinValue, DateTime.MaxValue, DateTime.MinValue, DateTime.MaxValue, DateTime.MinValue, DateTime.MaxValue, DateTime.MinValue, DateTime.MaxValue, DateTime.MinValue});
                    CreateAnalogItemVariable(analogArrayFolder, "Double", "Double", BuiltInType.Double, ValueRanks.OneDimension, new double[] {9.00001d, 9.0002d, 9.003d, 9.04d, 9.5d, 9.06d, 9.007d, 9.008d, 9.0009d});
                    CreateAnalogItemVariable(analogArrayFolder, "Duration", "Duration", DataTypeIds.Duration, ValueRanks.OneDimension, new double[] {9.00001d, 9.0002d, 9.003d, 9.04d, 9.5d, 9.06d, 9.007d, 9.008d, 9.0009d}, null);
                    CreateAnalogItemVariable(analogArrayFolder, "Float", "Float", BuiltInType.Float, ValueRanks.OneDimension, new float[] {0.1f, 0.2f, 0.3f, 0.4f, 0.5f, 1.1f, 2.2f, 3.3f, 4.4f, 5.5f});
                    CreateAnalogItemVariable(analogArrayFolder, "Guid", "Guid", BuiltInType.Guid, ValueRanks.OneDimension,
                        new Guid[] {Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid()});
                    CreateAnalogItemVariable(analogArrayFolder, "Int16", "Int16", BuiltInType.Int16, ValueRanks.OneDimension, new Int16[] {0, 1, 2, 3, 4, 5, 6, 7, 8, 9});
                    CreateAnalogItemVariable(analogArrayFolder, "Int32", "Int32", BuiltInType.Int32, ValueRanks.OneDimension, new Int32[] {10, 11, 12, 13, 14, 15, 16, 17, 18, 19});
                    CreateAnalogItemVariable(analogArrayFolder, "Int64", "Int64", BuiltInType.Int64, ValueRanks.OneDimension, new Int64[] {10, 11, 12, 13, 14, 15, 16, 17, 18, 19});
                    CreateAnalogItemVariable(analogArrayFolder, "Integer", "Integer", BuiltInType.Integer, ValueRanks.OneDimension, new Int64[] {10, 11, 12, 13, 14, 15, 16, 17, 18, 19});
                    CreateAnalogItemVariable(analogArrayFolder, "LocaleId", "LocaleId", DataTypeIds.LocaleId, ValueRanks.OneDimension, new String[] {"en", "fr", "de", "en", "fr", "de", "en", "fr", "de", "en"}, null);
                    CreateAnalogItemVariable(analogArrayFolder, "LocalizedText", "LocalizedText", BuiltInType.LocalizedText, ValueRanks.OneDimension,
                        new LocalizedText[]
                        {
                            new LocalizedText("en", "Hello World1"), new LocalizedText("en", "Hello World2"), new LocalizedText("en", "Hello World3"), new LocalizedText("en", "Hello World4"), new LocalizedText("en", "Hello World5"),
                            new LocalizedText("en", "Hello World6"), new LocalizedText("en", "Hello World7"), new LocalizedText("en", "Hello World8"), new LocalizedText("en", "Hello World9"), new LocalizedText("en", "Hello World10")
                        });
                    CreateAnalogItemVariable(analogArrayFolder, "NodeId", "NodeId", BuiltInType.NodeId, ValueRanks.OneDimension,
                        new NodeId[]
                        {
                            new NodeId(Guid.NewGuid()), new NodeId(Guid.NewGuid()), new NodeId(Guid.NewGuid()), new NodeId(Guid.NewGuid()), new NodeId(Guid.NewGuid()), new NodeId(Guid.NewGuid()), new NodeId(Guid.NewGuid()),
                            new NodeId(Guid.NewGuid()), new NodeId(Guid.NewGuid()), new NodeId(Guid.NewGuid())
                        });
                    CreateAnalogItemVariable(analogArrayFolder, "Number", "Number", BuiltInType.Number, ValueRanks.OneDimension, new Int16[] {1, 2, 3, 4, 5, 6, 7, 8, 9, 10});
                    CreateAnalogItemVariable(analogArrayFolder, "QualifiedName", "QualifiedName", BuiltInType.QualifiedName, ValueRanks.OneDimension, new Int16[] {1, 2, 3, 4, 5, 6, 7, 8, 9, 10});
                    CreateAnalogItemVariable(analogArrayFolder, "SByte", "SByte", BuiltInType.SByte, ValueRanks.OneDimension, new SByte[] {10, 20, 30, 40, 50, 60, 70, 80, 90});
                    CreateAnalogItemVariable(analogArrayFolder, "String", "String", BuiltInType.String, ValueRanks.OneDimension, new String[] {"a00", "b10", "c20", "d30", "e40", "f50", "g60", "h70", "i80", "j90"});
                    CreateAnalogItemVariable(analogArrayFolder, "Time", "Time", DataTypeIds.Time, ValueRanks.OneDimension,
                        new String[]
                        {
                            DateTime.MinValue.ToString(), DateTime.MaxValue.ToString(), DateTime.MinValue.ToString(), DateTime.MaxValue.ToString(), DateTime.MinValue.ToString(), DateTime.MaxValue.ToString(), DateTime.MinValue.ToString(),
                            DateTime.MaxValue.ToString(), DateTime.MinValue.ToString(), DateTime.MaxValue.ToString()
                        }, null);
                    CreateAnalogItemVariable(analogArrayFolder, "UInt16", "UInt16", BuiltInType.UInt16, ValueRanks.OneDimension, new UInt16[] {20, 21, 22, 23, 24, 25, 26, 27, 28, 29});
                    CreateAnalogItemVariable(analogArrayFolder, "UInt32", "UInt32", BuiltInType.UInt32, ValueRanks.OneDimension, new UInt32[] {30, 31, 32, 33, 34, 35, 36, 37, 38, 39});
                    CreateAnalogItemVariable(analogArrayFolder, "UInt64", "UInt64", BuiltInType.UInt64, ValueRanks.OneDimension, new UInt64[] {10, 11, 12, 13, 14, 15, 16, 17, 18, 19});
                    CreateAnalogItemVariable(analogArrayFolder, "UInteger", "UInteger", BuiltInType.UInteger, ValueRanks.OneDimension, new UInt64[] {10, 11, 12, 13, 14, 15, 16, 17, 18, 19});
                    CreateAnalogItemVariable(analogArrayFolder, "UtcTime", "UtcTime", DataTypeIds.UtcTime, ValueRanks.OneDimension,
                        new DateTime[]
                        {
                            DateTime.MinValue.ToUniversalTime(), DateTime.MaxValue.ToUniversalTime(), DateTime.MinValue.ToUniversalTime(), DateTime.MaxValue.ToUniversalTime(), DateTime.MinValue.ToUniversalTime(), DateTime.MaxValue.ToUniversalTime(),
                            DateTime.MinValue.ToUniversalTime(), DateTime.MaxValue.ToUniversalTime(), DateTime.MinValue.ToUniversalTime()
                        }, null);
                    CreateAnalogItemVariable(analogArrayFolder, "Variant", "Variant", BuiltInType.Variant, ValueRanks.OneDimension, new Variant[] {10, 11, 12, 13, 14, 15, 16, 17, 18, 19});
                    XmlDocument doc1 = new XmlDocument();
                    CreateAnalogItemVariable(analogArrayFolder, "XmlElement", "XmlElement", BuiltInType.XmlElement, ValueRanks.OneDimension,
                        new XmlElement[]
                        {
                            doc1.CreateElement("tag1"), doc1.CreateElement("tag2"), doc1.CreateElement("tag3"), doc1.CreateElement("tag4"), doc1.CreateElement("tag5"), doc1.CreateElement("tag6"), doc1.CreateElement("tag7"),
                            doc1.CreateElement("tag8"), doc1.CreateElement("tag9"), doc1.CreateElement("tag10")
                        });

                    #endregion

                    #region DataAccess_DiscreteType

                    FolderState discreteTypeFolder = CreateFolder(daFolder, "DiscreteType");
                    FolderState twoStateDiscreteFolder = CreateFolder(discreteTypeFolder, "TwoStateDiscreteType");
                    
                    // Add our Nodes to the folder, and specify their customized discrete enumerations
                    CreateTwoStateDiscreteItemVariable(twoStateDiscreteFolder, "001", "001", "red", "blue");
                    CreateTwoStateDiscreteItemVariable(twoStateDiscreteFolder, "002", "002", "open", "close");
                    CreateTwoStateDiscreteItemVariable(twoStateDiscreteFolder, "003", "003", "up", "down");
                    CreateTwoStateDiscreteItemVariable(twoStateDiscreteFolder, "004", "004", "left", "right");
                    CreateTwoStateDiscreteItemVariable(twoStateDiscreteFolder, "005", "005", "circle", "cross");

                    FolderState multiStateDiscreteFolder = CreateFolder(discreteTypeFolder, "MultiStateDiscreteType");
                    
                    // Add our Nodes to the folder, and specify their customized discrete enumerations
                    CreateMultiStateDiscreteItemVariable(multiStateDiscreteFolder, "001", "001", "open", "closed", "jammed");
                    CreateMultiStateDiscreteItemVariable(multiStateDiscreteFolder, "002", "002", "red", "green", "blue", "cyan");
                    CreateMultiStateDiscreteItemVariable(multiStateDiscreteFolder, "003", "003", "lolo", "lo", "normal", "hi", "hihi");
                    CreateMultiStateDiscreteItemVariable(multiStateDiscreteFolder, "004", "004", "left", "right", "center");
                    CreateMultiStateDiscreteItemVariable(multiStateDiscreteFolder, "005", "005", "circle", "cross", "triangle");

                    #endregion

                    #region DataAccess_MultiStateValueDiscreteType

                    FolderState multiStateValueDiscreteFolder = CreateFolder(discreteTypeFolder, "MultiStateValueDiscreteType");
                    
                    // Add our Nodes to the folder, and specify their customized discrete enumerations
                    CreateMultiStateValueDiscreteItemVariable(multiStateValueDiscreteFolder, "001", "001", new string[] {"open", "closed", "jammed"});
                    CreateMultiStateValueDiscreteItemVariable(multiStateValueDiscreteFolder, "002", "002", new string[] {"red", "green", "blue", "cyan"});
                    CreateMultiStateValueDiscreteItemVariable(multiStateValueDiscreteFolder, "003", "003", new string[] {"lolo", "lo", "normal", "hi", "hihi"});
                    CreateMultiStateValueDiscreteItemVariable(multiStateValueDiscreteFolder, "004", "004", new string[] {"left", "right", "center"});
                    CreateMultiStateValueDiscreteItemVariable(multiStateValueDiscreteFolder, "005", "005", new string[] {"circle", "cross", "triangle"});

                    // Add our Nodes to the folder and specify varying data types
                    CreateMultiStateValueDiscreteItemVariable(multiStateValueDiscreteFolder, "Byte", "Byte", DataTypeIds.Byte, new string[] {"open", "closed", "jammed"});
                    CreateMultiStateValueDiscreteItemVariable(multiStateValueDiscreteFolder, "Int16", "Int16", DataTypeIds.Int16, new string[] {"red", "green", "blue", "cyan"});
                    CreateMultiStateValueDiscreteItemVariable(multiStateValueDiscreteFolder, "Int32", "Int32", DataTypeIds.Int32, new string[] {"lolo", "lo", "normal", "hi", "hihi"});
                    CreateMultiStateValueDiscreteItemVariable(multiStateValueDiscreteFolder, "Int64", "Int64", DataTypeIds.Int64, new string[] {"left", "right", "center"});
                    CreateMultiStateValueDiscreteItemVariable(multiStateValueDiscreteFolder, "SByte", "SByte", DataTypeIds.SByte, new string[] {"open", "closed", "jammed"});
                    CreateMultiStateValueDiscreteItemVariable(multiStateValueDiscreteFolder, "UInt16", "UInt16", DataTypeIds.UInt16, new string[] {"red", "green", "blue", "cyan"});
                    CreateMultiStateValueDiscreteItemVariable(multiStateValueDiscreteFolder, "UInt32", "UInt32", DataTypeIds.UInt32, new string[] {"lolo", "lo", "normal", "hi", "hihi"});
                    CreateMultiStateValueDiscreteItemVariable(multiStateValueDiscreteFolder, "UInt64", "UInt64", DataTypeIds.UInt64, new string[] {"left", "right", "center"});

                    #endregion

                    #region References

                    FolderState referencesFolder = CreateFolder(root, "References");
                    
                    BaseDataVariableState referencesInstructions = CreateVariable(referencesFolder, "Instructions", DataTypeIds.String, ValueRanks.Scalar);
                    referencesInstructions.Value = "This folder will contain nodes that have specific Reference configurations.";
                    variables.Add(referencesInstructions);

                    // create variable nodes with specific references
                    BaseDataVariableState hasForwardReference = CreateMeshVariable(referencesFolder, "HasForwardReference", "HasForwardReference");
                    hasForwardReference.AddReference(ReferenceTypes.HasCause, false, variables[0].NodeId);
                    variables.Add(hasForwardReference);

                    BaseDataVariableState hasInverseReference = CreateMeshVariable(referencesFolder, "HasInverseReference", "HasInverseReference");
                    hasInverseReference.AddReference(ReferenceTypes.HasCause, true, variables[0].NodeId);
                    variables.Add(hasInverseReference);

                    BaseDataVariableState has3InverseReference = null;
                    for (int i = 1; i <= 5; i++)
                    {
                        string referenceString = "Has3ForwardReferences";
                        if (i > 1)
                        {
                            referenceString += i.ToString();
                        }
                        BaseDataVariableState has3ForwardReferences = CreateMeshVariable(referencesFolder, referenceString, referenceString);
                        has3ForwardReferences.AddReference(ReferenceTypes.HasCause, false, variables[0].NodeId);
                        has3ForwardReferences.AddReference(ReferenceTypes.HasCause, false, variables[1].NodeId);
                        has3ForwardReferences.AddReference(ReferenceTypes.HasCause, false, variables[2].NodeId);
                        if (i == 1)
                        {
                            has3InverseReference = has3ForwardReferences;
                        }
                        variables.Add(has3ForwardReferences);
                    }

                    BaseDataVariableState has3InverseReferences = CreateMeshVariable(referencesFolder, "Has3InverseReferences", "Has3InverseReferences");
                    has3InverseReferences.AddReference(ReferenceTypes.HasEffect, true, variables[0].NodeId);
                    has3InverseReferences.AddReference(ReferenceTypes.HasEffect, true, variables[1].NodeId);
                    has3InverseReferences.AddReference(ReferenceTypes.HasEffect, true, variables[2].NodeId);
                    variables.Add(has3InverseReferences);

                    BaseDataVariableState hasForwardAndInverseReferences = CreateMeshVariable(referencesFolder, "HasForwardAndInverseReference", "HasForwardAndInverseReference", hasForwardReference, hasInverseReference,
                        has3InverseReference, has3InverseReferences, variables[0]);
                    variables.Add(hasForwardAndInverseReferences);

                    #endregion

                    #region AccessRights

                    FolderState folderAccessRights = CreateFolder(root, "AccessRights");
                    
                    BaseDataVariableState accessRightsInstructions = CreateVariable(folderAccessRights, "Instructions", DataTypeIds.String, ValueRanks.Scalar);
                    accessRightsInstructions.Value = "This folder will be accessible to all who enter, but contents therein will be secured.";
                    variables.Add(accessRightsInstructions);

                    // sub-folder for "AccessAll"
                    FolderState folderAccessRightsAccessAll = CreateFolder(folderAccessRights, "AccessAll");
                    
                    BaseDataVariableState arAllRO = CreateVariable(folderAccessRightsAccessAll, "RO", BuiltInType.Int16, ValueRanks.Scalar);
                    arAllRO.AccessLevel = AccessLevels.CurrentRead;
                    arAllRO.UserAccessLevel = AccessLevels.CurrentRead;
                    variables.Add(arAllRO);
                    BaseDataVariableState arAllWO = CreateVariable(folderAccessRightsAccessAll, "WO", BuiltInType.Int16, ValueRanks.Scalar);
                    arAllWO.AccessLevel = AccessLevels.CurrentWrite;
                    arAllWO.UserAccessLevel = AccessLevels.CurrentWrite;
                    variables.Add(arAllWO);
                    BaseDataVariableState arAllRW = CreateVariable(folderAccessRightsAccessAll, "RW", BuiltInType.Int16, ValueRanks.Scalar);
                    arAllRW.AccessLevel = AccessLevels.CurrentReadOrWrite;
                    arAllRW.UserAccessLevel = AccessLevels.CurrentReadOrWrite;
                    variables.Add(arAllRW);
                    BaseDataVariableState arAllRONotUser = CreateVariable(folderAccessRightsAccessAll, "RO_NotUser", BuiltInType.Int16, ValueRanks.Scalar);
                    arAllRONotUser.AccessLevel = AccessLevels.CurrentRead;
                    arAllRONotUser.UserAccessLevel = AccessLevels.None;
                    variables.Add(arAllRONotUser);
                    BaseDataVariableState arAllWONotUser = CreateVariable(folderAccessRightsAccessAll, "WO_NotUser", BuiltInType.Int16, ValueRanks.Scalar);
                    arAllWONotUser.AccessLevel = AccessLevels.CurrentWrite;
                    arAllWONotUser.UserAccessLevel = AccessLevels.None;
                    variables.Add(arAllWONotUser);
                    BaseDataVariableState arAllRWNotUser = CreateVariable(folderAccessRightsAccessAll, "RW_NotUser", BuiltInType.Int16, ValueRanks.Scalar);
                    arAllRWNotUser.AccessLevel = AccessLevels.CurrentReadOrWrite;
                    arAllRWNotUser.UserAccessLevel = AccessLevels.CurrentRead;
                    variables.Add(arAllRWNotUser);
                    BaseDataVariableState arAllROUserRW = CreateVariable(folderAccessRightsAccessAll, "RO_User1_RW", BuiltInType.Int16, ValueRanks.Scalar);
                    arAllROUserRW.AccessLevel = AccessLevels.CurrentRead;
                    arAllROUserRW.UserAccessLevel = AccessLevels.CurrentReadOrWrite;
                    variables.Add(arAllROUserRW);
                    BaseDataVariableState arAllROGroupRW = CreateVariable(folderAccessRightsAccessAll, "RO_Group1_RW", BuiltInType.Int16, ValueRanks.Scalar);
                    arAllROGroupRW.AccessLevel = AccessLevels.CurrentRead;
                    arAllROGroupRW.UserAccessLevel = AccessLevels.CurrentReadOrWrite;
                    variables.Add(arAllROGroupRW);

                    // sub-folder for "AccessUser1"
                    FolderState folderAccessRightsAccessUser1 = CreateFolder(folderAccessRights, "AccessUser1");
                    
                    BaseDataVariableState arUserRO = CreateVariable(folderAccessRightsAccessUser1, "RO", BuiltInType.Int16, ValueRanks.Scalar);
                    arUserRO.AccessLevel = AccessLevels.CurrentRead;
                    arUserRO.UserAccessLevel = AccessLevels.CurrentRead;
                    variables.Add(arUserRO);
                    BaseDataVariableState arUserWO = CreateVariable(folderAccessRightsAccessUser1, "WO", BuiltInType.Int16, ValueRanks.Scalar);
                    arUserWO.AccessLevel = AccessLevels.CurrentWrite;
                    arUserWO.UserAccessLevel = AccessLevels.CurrentWrite;
                    variables.Add(arUserWO);
                    BaseDataVariableState arUserRW = CreateVariable(folderAccessRightsAccessUser1, "RW", BuiltInType.Int16, ValueRanks.Scalar);
                    arUserRW.AccessLevel = AccessLevels.CurrentReadOrWrite;
                    arUserRW.UserAccessLevel = AccessLevels.CurrentReadOrWrite;
                    variables.Add(arUserRW);

                    // sub-folder for "AccessGroup1"
                    FolderState folderAccessRightsAccessGroup1 = CreateFolder(folderAccessRights, "AccessGroup1");
                    
                    BaseDataVariableState arGroupRO = CreateVariable(folderAccessRightsAccessGroup1, "RO", BuiltInType.Int16, ValueRanks.Scalar);
                    arGroupRO.AccessLevel = AccessLevels.CurrentRead;
                    arGroupRO.UserAccessLevel = AccessLevels.CurrentRead;
                    variables.Add(arGroupRO);
                    BaseDataVariableState arGroupWO = CreateVariable(folderAccessRightsAccessGroup1, "WO", BuiltInType.Int16, ValueRanks.Scalar);
                    arGroupWO.AccessLevel = AccessLevels.CurrentWrite;
                    arGroupWO.UserAccessLevel = AccessLevels.CurrentWrite;
                    variables.Add(arGroupWO);
                    BaseDataVariableState arGroupRW = CreateVariable(folderAccessRightsAccessGroup1, "RW", BuiltInType.Int16, ValueRanks.Scalar);
                    arGroupRW.AccessLevel = AccessLevels.CurrentReadOrWrite;
                    arGroupRW.UserAccessLevel = AccessLevels.CurrentReadOrWrite;
                    variables.Add(arGroupRW);

                    #endregion

                    #region NodeIds

                    FolderState nodeIdsFolder = CreateFolder(root, "NodeIds");
                    
                    BaseDataVariableState nodeIdsInstructions = CreateVariable(folderAccessRights, "Instructions", DataTypeIds.String, ValueRanks.Scalar);
                    nodeIdsInstructions.Value = "All supported Node types are available except whichever is in use for the other nodes.";
                    variables.Add(nodeIdsInstructions);

                    BaseDataVariableState integerNodeId = CreateVariable(nodeIdsFolder, "Int16Integer", DataTypeIds.Int16, ValueRanks.Scalar);
                    integerNodeId.NodeId = new NodeId((uint) 9202, NamespaceIndex);
                    variables.Add(integerNodeId);

                    variables.Add(CreateVariable(nodeIdsFolder, "Int16String", DataTypeIds.Int16, ValueRanks.Scalar));

                    BaseDataVariableState guidNodeId = CreateVariable(nodeIdsFolder, "Int16GUID", DataTypeIds.Int16, ValueRanks.Scalar);
                    guidNodeId.NodeId = new NodeId(new Guid("00000000-0000-0000-0000-000000009204"), NamespaceIndex);
                    variables.Add(guidNodeId);

                    BaseDataVariableState opaqueNodeId = CreateVariable(nodeIdsFolder, "Int16Opaque", DataTypeIds.Int16, ValueRanks.Scalar);
                    opaqueNodeId.NodeId = new NodeId(new byte[] {9, 2, 0, 5}, NamespaceIndex);
                    variables.Add(opaqueNodeId);

                    #endregion

                    #region Methods

                    FolderState methodsFolder = CreateFolder(root, "Methods");
                    
                    BaseDataVariableState methodsInstructions = CreateVariable(methodsFolder, "Instructions", DataTypeIds.String, ValueRanks.Scalar);
                    methodsInstructions.Value = "Contains methods with varying parameter definitions.";
                    variables.Add(methodsInstructions);

                    MethodState voidMethod = CreateMethod(methodsFolder, "Void", onCallHandler: OnVoidCall);

                    #region Add Method
                    Argument[] inputAdd = new Argument[]
                    {
                        new Argument() {Name = "Float value", Description = "Float value", DataType = DataTypeIds.Float, ValueRank = ValueRanks.Scalar},
                        new Argument() {Name = "UInt32 value", Description = "UInt32 value", DataType = DataTypeIds.UInt32, ValueRank = ValueRanks.Scalar}
                    };

                    Argument[] outputAdd = new Argument[]
                    {
                        new Argument() {Name = "Add Result", Description = "Add Result", DataType = DataTypeIds.Float, ValueRank = ValueRanks.Scalar}
                    };

                    MethodState addMethod = CreateMethod(methodsFolder, "Add", inputAdd, outputAdd, OnAddCall);

                    #endregion

                    #region Multiply Method
                    Argument[] inputMultiply = new Argument[]
                    {
                        new Argument() {Name = "Int16 value", Description = "Int16 value", DataType = DataTypeIds.Int16, ValueRank = ValueRanks.Scalar},
                        new Argument() {Name = "UInt16 value", Description = "UInt16 value", DataType = DataTypeIds.UInt16, ValueRank = ValueRanks.Scalar}
                    };

                    Argument[] outputMultiply = new Argument[]
                    {
                        new Argument() {Name = "Multiply Result", Description = "Multiply Result", DataType = DataTypeIds.Int32, ValueRank = ValueRanks.Scalar}
                    };

                    MethodState multiplyMethod = CreateMethod(methodsFolder, "Multiply", inputMultiply, outputMultiply, OnMultiplyCall);
                    #endregion

                    #region Divide Method

                    Argument[] inputDivide = new Argument[]
                   {
                        new Argument() {Name = "Int32 value", Description = "Int32 value", DataType = DataTypeIds.Int32, ValueRank = ValueRanks.Scalar},
                        new Argument() {Name = "UInt16 value", Description = "UInt16 value", DataType = DataTypeIds.UInt16, ValueRank = ValueRanks.Scalar}
                    };

                    Argument[] outputDivide = new Argument[]
                    {
                        new Argument() {Name = "Divide Result", Description = "Divide Result", DataType = DataTypeIds.Float, ValueRank = ValueRanks.Scalar}
                    };

                    MethodState divideMethod = CreateMethod(methodsFolder, "Divide", inputDivide, outputDivide, OnDivideCall);

                    #endregion

                    #region Substract Method

                    Argument[] inputSubtract = new Argument[]
                    {
                        new Argument() {Name = "Int16 value", Description = "Int16 value", DataType = DataTypeIds.Int16, ValueRank = ValueRanks.Scalar},
                        new Argument() {Name = "Byte value", Description = "Byte value", DataType = DataTypeIds.Byte, ValueRank = ValueRanks.Scalar}
                    };

                    Argument[] outputSubtract = new Argument[]
                    {
                        new Argument() {Name = "Subtract Result", Description = "Subtract Result", DataType = DataTypeIds.Int16, ValueRank = ValueRanks.Scalar}
                    };

                    MethodState subtractMethod = CreateMethod(methodsFolder, "Subtract", inputSubtract, outputSubtract, OnSubstractCall);

                    #endregion

                    #region Hello Method

                    Argument[] inputHello = new Argument[]
                    {
                        new Argument() {Name = "String value", Description = "String value", DataType = DataTypeIds.String, ValueRank = ValueRanks.Scalar}
                    };

                    Argument[] outputHello = new Argument[]
                    {
                        new Argument() {Name = "Hello Result", Description = "Hello Result", DataType = DataTypeIds.String, ValueRank = ValueRanks.Scalar}
                    };

                    MethodState helloMethod = CreateMethod(methodsFolder, "Hello", inputHello, outputHello, OnHelloCall);

                    #endregion

                    #region Input Method

                    Argument[] inputInput = new Argument[]
                    {
                        new Argument() {Name = "String value", Description = "String value", DataType = DataTypeIds.String, ValueRank = ValueRanks.Scalar}
                    };
                    MethodState inputMethod = CreateMethod(methodsFolder, "Input", inputInput, onCallHandler: OnInputCall);

                    #endregion

                    #region Output Method

                    Argument[] outputOutput = new Argument[]
                      {
                        new Argument() {Name = "Output Result", Description = "Output Result", DataType = DataTypeIds.String, ValueRank = ValueRanks.Scalar}
                    };
                    MethodState outputMethod = CreateMethod(methodsFolder, "Output", null, outputOutput, OnOutputCall);

                    #endregion

                    #endregion

                    #region Views

                    FolderState viewsFolder = CreateFolder(root, "Views");
                    const string views = "Views_";

                    ViewState viewStateOperations = CreateView(viewsFolder, externalReferences, views + "Operations", "Operations");
                    ViewState viewStateEngineering = CreateView(viewsFolder, externalReferences, views + "Engineering", "Engineering");

                    #endregion

                    #region Locales

                    FolderState localesFolder = CreateFolder(root, "Locales");
                    
                    BaseDataVariableState qnEnglishVariable = CreateVariable(localesFolder, "QNEnglish", DataTypeIds.QualifiedName, ValueRanks.Scalar);
                    qnEnglishVariable.Description = new LocalizedText("en", "English");
                    qnEnglishVariable.Value = new QualifiedName("Hello World", NamespaceIndex);
                    variables.Add(qnEnglishVariable);
                    BaseDataVariableState ltEnglishVariable = CreateVariable(localesFolder, "LTEnglish", DataTypeIds.LocalizedText, ValueRanks.Scalar);
                    ltEnglishVariable.Description = new LocalizedText("en", "English");
                    ltEnglishVariable.Value = new LocalizedText("en", "Hello World");
                    variables.Add(ltEnglishVariable);

                    BaseDataVariableState qnFrancaisVariable = CreateVariable(localesFolder, "QNFrancais", DataTypeIds.QualifiedName, ValueRanks.Scalar);
                    qnFrancaisVariable.Description = new LocalizedText("en", "Francais");
                    qnFrancaisVariable.Value = new QualifiedName("Salut tout le monde", NamespaceIndex);
                    variables.Add(qnFrancaisVariable);
                    BaseDataVariableState ltFrancaisVariable = CreateVariable(localesFolder, "LTFrancais", DataTypeIds.LocalizedText, ValueRanks.Scalar);
                    ltFrancaisVariable.Description = new LocalizedText("en", "Francais");
                    ltFrancaisVariable.Value = new LocalizedText("fr", "Salut tout le monde");
                    variables.Add(ltFrancaisVariable);

                    BaseDataVariableState qnDeutschVariable = CreateVariable(localesFolder, "QNDeutsch", DataTypeIds.QualifiedName, ValueRanks.Scalar);
                    qnDeutschVariable.Description = new LocalizedText("en", "Deutsch");
                    qnDeutschVariable.Value = new QualifiedName("Hallo Welt", NamespaceIndex);
                    variables.Add(qnDeutschVariable);
                    BaseDataVariableState ltDeutschVariable = CreateVariable(localesFolder, "LTDeutsch", DataTypeIds.LocalizedText, ValueRanks.Scalar);
                    ltDeutschVariable.Description = new LocalizedText("en", "Deutsch");
                    ltDeutschVariable.Value = new LocalizedText("de", "Hallo Welt");
                    variables.Add(ltDeutschVariable);

                    BaseDataVariableState qnEspanolVariable = CreateVariable(localesFolder, "QNEspanol", DataTypeIds.QualifiedName, ValueRanks.Scalar);
                    qnEspanolVariable.Description = new LocalizedText("en", "Espanol");
                    qnEspanolVariable.Value = new QualifiedName("Hola mundo", NamespaceIndex);
                    variables.Add(qnEspanolVariable);
                    BaseDataVariableState ltEspanolVariable = CreateVariable(localesFolder, "LTEspanol", DataTypeIds.LocalizedText, ValueRanks.Scalar);
                    ltEspanolVariable.Description = new LocalizedText("en", "Espanol");
                    ltEspanolVariable.Value = new LocalizedText("es", "Hola mundo");
                    variables.Add(ltEspanolVariable);

                    BaseDataVariableState qnJapaneseVariable = CreateVariable(localesFolder, "QN日本の", DataTypeIds.QualifiedName, ValueRanks.Scalar);
                    qnJapaneseVariable.Description = new LocalizedText("en", "Japanese");
                    qnJapaneseVariable.Value = new QualifiedName("ハローワールド", NamespaceIndex);
                    variables.Add(qnJapaneseVariable);
                    BaseDataVariableState ltJapaneseVariable = CreateVariable(localesFolder, "LT日本の", DataTypeIds.LocalizedText, ValueRanks.Scalar);
                    ltJapaneseVariable.Description = new LocalizedText("en", "Japanese");
                    ltJapaneseVariable.Value = new LocalizedText("jp", "ハローワールド");
                    variables.Add(ltJapaneseVariable);

                    BaseDataVariableState qnChineseVariable = CreateVariable(localesFolder, "QN中國的", DataTypeIds.QualifiedName, ValueRanks.Scalar);
                    qnChineseVariable.Description = new LocalizedText("en", "Chinese");
                    qnChineseVariable.Value = new QualifiedName("世界您好", NamespaceIndex);
                    variables.Add(qnChineseVariable);
                    BaseDataVariableState ltChineseVariable = CreateVariable(localesFolder, "LT中國的", DataTypeIds.LocalizedText, ValueRanks.Scalar);
                    ltChineseVariable.Description = new LocalizedText("en", "Chinese");
                    ltChineseVariable.Value = new LocalizedText("ch", "世界您好");
                    variables.Add(ltChineseVariable);

                    BaseDataVariableState qnRussianVariable = CreateVariable(localesFolder, "QNрусский", DataTypeIds.QualifiedName, ValueRanks.Scalar);
                    qnRussianVariable.Description = new LocalizedText("en", "Russian");
                    qnRussianVariable.Value = new QualifiedName("LTрусский", NamespaceIndex);
                    variables.Add(qnRussianVariable);
                    BaseDataVariableState ltRussianVariable = CreateVariable(localesFolder, "LTрусский", DataTypeIds.LocalizedText, ValueRanks.Scalar);
                    ltRussianVariable.Description = new LocalizedText("en", "Russian");
                    ltRussianVariable.Value = new LocalizedText("ru", "LTрусский");
                    variables.Add(ltRussianVariable);

                    BaseDataVariableState qnArabicVariable = CreateVariable(localesFolder, "QNالعربية", DataTypeIds.QualifiedName, ValueRanks.Scalar);
                    qnArabicVariable.Description = new LocalizedText("en", "Arabic");
                    qnArabicVariable.Value = new QualifiedName("مرحبا بالعال", NamespaceIndex);
                    variables.Add(qnArabicVariable);
                    BaseDataVariableState ltArabicVariable = CreateVariable(localesFolder, "LTالعربية", DataTypeIds.LocalizedText, ValueRanks.Scalar);
                    ltArabicVariable.Description = new LocalizedText("en", "Arabic");
                    ltArabicVariable.Value = new LocalizedText("ae", "مرحبا بالعال");
                    variables.Add(ltArabicVariable);

                    BaseDataVariableState qnKlingonVariable = CreateVariable(localesFolder, "QNtlhIngan", DataTypeIds.QualifiedName, ValueRanks.Scalar);
                    qnKlingonVariable.Description = new LocalizedText("en", "Klingon");
                    qnKlingonVariable.Value = new QualifiedName("qo' vIvan", NamespaceIndex);
                    variables.Add(qnKlingonVariable);
                    BaseDataVariableState ltKlingonVariable = CreateVariable(localesFolder, "LTtlhIngan", DataTypeIds.LocalizedText, ValueRanks.Scalar);
                    ltKlingonVariable.Description = new LocalizedText("en", "Klingon");
                    ltKlingonVariable.Value = new LocalizedText("ko", "qo' vIvan");
                    variables.Add(ltKlingonVariable);

                    #endregion

                    #region Attributes

                    FolderState folderAttributes = CreateFolder(root, "Attributes");

                    #region AccessAll

                    FolderState folderAttributesAccessAll = CreateFolder(folderAttributes, "AccessAll");
                    
                    BaseDataVariableState accessLevelAccessAll = CreateVariable(folderAttributesAccessAll, "AccessLevel", DataTypeIds.Double, ValueRanks.Scalar);
                    accessLevelAccessAll.WriteMask = AttributeWriteMask.AccessLevel;
                    accessLevelAccessAll.UserWriteMask = AttributeWriteMask.AccessLevel;
                    variables.Add(accessLevelAccessAll);

                    BaseDataVariableState arrayDimensionsAccessLevel = CreateVariable(folderAttributesAccessAll, "ArrayDimensions", DataTypeIds.Double, ValueRanks.Scalar);
                    arrayDimensionsAccessLevel.WriteMask = AttributeWriteMask.ArrayDimensions;
                    arrayDimensionsAccessLevel.UserWriteMask = AttributeWriteMask.ArrayDimensions;
                    variables.Add(arrayDimensionsAccessLevel);

                    BaseDataVariableState browseNameAccessLevel = CreateVariable(folderAttributesAccessAll, "BrowseName", DataTypeIds.Double, ValueRanks.Scalar);
                    browseNameAccessLevel.WriteMask = AttributeWriteMask.BrowseName;
                    browseNameAccessLevel.UserWriteMask = AttributeWriteMask.BrowseName;
                    variables.Add(browseNameAccessLevel);

                    BaseDataVariableState containsNoLoopsAccessLevel = CreateVariable(folderAttributesAccessAll, "ContainsNoLoops", DataTypeIds.Double, ValueRanks.Scalar);
                    containsNoLoopsAccessLevel.WriteMask = AttributeWriteMask.ContainsNoLoops;
                    containsNoLoopsAccessLevel.UserWriteMask = AttributeWriteMask.ContainsNoLoops;
                    variables.Add(containsNoLoopsAccessLevel);

                    BaseDataVariableState dataTypeAccessLevel = CreateVariable(folderAttributesAccessAll, "DataType", DataTypeIds.Double, ValueRanks.Scalar);
                    dataTypeAccessLevel.WriteMask = AttributeWriteMask.DataType;
                    dataTypeAccessLevel.UserWriteMask = AttributeWriteMask.DataType;
                    variables.Add(dataTypeAccessLevel);

                    BaseDataVariableState descriptionAccessLevel = CreateVariable(folderAttributesAccessAll, "Description", DataTypeIds.Double, ValueRanks.Scalar);
                    descriptionAccessLevel.WriteMask = AttributeWriteMask.Description;
                    descriptionAccessLevel.UserWriteMask = AttributeWriteMask.Description;
                    variables.Add(descriptionAccessLevel);

                    BaseDataVariableState eventNotifierAccessLevel = CreateVariable(folderAttributesAccessAll, "EventNotifier", DataTypeIds.Double, ValueRanks.Scalar);
                    eventNotifierAccessLevel.WriteMask = AttributeWriteMask.EventNotifier;
                    eventNotifierAccessLevel.UserWriteMask = AttributeWriteMask.EventNotifier;
                    variables.Add(eventNotifierAccessLevel);

                    BaseDataVariableState executableAccessLevel = CreateVariable(folderAttributesAccessAll, "Executable", DataTypeIds.Double, ValueRanks.Scalar);
                    executableAccessLevel.WriteMask = AttributeWriteMask.Executable;
                    executableAccessLevel.UserWriteMask = AttributeWriteMask.Executable;
                    variables.Add(executableAccessLevel);

                    BaseDataVariableState historizingAccessLevel = CreateVariable(folderAttributesAccessAll, "Historizing", DataTypeIds.Double, ValueRanks.Scalar);
                    historizingAccessLevel.WriteMask = AttributeWriteMask.Historizing;
                    historizingAccessLevel.UserWriteMask = AttributeWriteMask.Historizing;
                    variables.Add(historizingAccessLevel);

                    BaseDataVariableState inverseNameAccessLevel = CreateVariable(folderAttributesAccessAll, "InverseName", DataTypeIds.Double, ValueRanks.Scalar);
                    inverseNameAccessLevel.WriteMask = AttributeWriteMask.InverseName;
                    inverseNameAccessLevel.UserWriteMask = AttributeWriteMask.InverseName;
                    variables.Add(inverseNameAccessLevel);

                    BaseDataVariableState isAbstractAccessLevel = CreateVariable(folderAttributesAccessAll, "IsAbstract", DataTypeIds.Double, ValueRanks.Scalar);
                    isAbstractAccessLevel.WriteMask = AttributeWriteMask.IsAbstract;
                    isAbstractAccessLevel.UserWriteMask = AttributeWriteMask.IsAbstract;
                    variables.Add(isAbstractAccessLevel);

                    BaseDataVariableState minimumSamplingIntervalAccessLevel = CreateVariable(folderAttributesAccessAll, "MinimumSamplingInterval", DataTypeIds.Double, ValueRanks.Scalar);
                    minimumSamplingIntervalAccessLevel.WriteMask = AttributeWriteMask.MinimumSamplingInterval;
                    minimumSamplingIntervalAccessLevel.UserWriteMask = AttributeWriteMask.MinimumSamplingInterval;
                    variables.Add(minimumSamplingIntervalAccessLevel);

                    BaseDataVariableState nodeClassIntervalAccessLevel = CreateVariable(folderAttributesAccessAll, "NodeClass", DataTypeIds.Double, ValueRanks.Scalar);
                    nodeClassIntervalAccessLevel.WriteMask = AttributeWriteMask.NodeClass;
                    nodeClassIntervalAccessLevel.UserWriteMask = AttributeWriteMask.NodeClass;
                    variables.Add(nodeClassIntervalAccessLevel);

                    BaseDataVariableState nodeIdAccessLevel = CreateVariable(folderAttributesAccessAll, "NodeId", DataTypeIds.Double, ValueRanks.Scalar);
                    nodeIdAccessLevel.WriteMask = AttributeWriteMask.NodeId;
                    nodeIdAccessLevel.UserWriteMask = AttributeWriteMask.NodeId;
                    variables.Add(nodeIdAccessLevel);

                    BaseDataVariableState symmetricAccessLevel = CreateVariable(folderAttributesAccessAll, "Symmetric", DataTypeIds.Double, ValueRanks.Scalar);
                    symmetricAccessLevel.WriteMask = AttributeWriteMask.Symmetric;
                    symmetricAccessLevel.UserWriteMask = AttributeWriteMask.Symmetric;
                    variables.Add(symmetricAccessLevel);

                    BaseDataVariableState userAccessLevelAccessLevel = CreateVariable(folderAttributesAccessAll, "UserAccessLevel", DataTypeIds.Double, ValueRanks.Scalar);
                    userAccessLevelAccessLevel.WriteMask = AttributeWriteMask.UserAccessLevel;
                    userAccessLevelAccessLevel.UserWriteMask = AttributeWriteMask.UserAccessLevel;
                    variables.Add(userAccessLevelAccessLevel);

                    BaseDataVariableState userExecutableAccessLevel = CreateVariable(folderAttributesAccessAll, "UserExecutable", DataTypeIds.Double, ValueRanks.Scalar);
                    userExecutableAccessLevel.WriteMask = AttributeWriteMask.UserExecutable;
                    userExecutableAccessLevel.UserWriteMask = AttributeWriteMask.UserExecutable;
                    variables.Add(userExecutableAccessLevel);

                    BaseDataVariableState valueRankAccessLevel = CreateVariable(folderAttributesAccessAll, "ValueRank", DataTypeIds.Double, ValueRanks.Scalar);
                    valueRankAccessLevel.WriteMask = AttributeWriteMask.ValueRank;
                    valueRankAccessLevel.UserWriteMask = AttributeWriteMask.ValueRank;
                    variables.Add(valueRankAccessLevel);

                    BaseDataVariableState writeMaskAccessLevel = CreateVariable(folderAttributesAccessAll, "WriteMask", DataTypeIds.Double, ValueRanks.Scalar);
                    writeMaskAccessLevel.WriteMask = AttributeWriteMask.WriteMask;
                    writeMaskAccessLevel.UserWriteMask = AttributeWriteMask.WriteMask;
                    variables.Add(writeMaskAccessLevel);

                    BaseDataVariableState valueForVariableTypeAccessLevel = CreateVariable(folderAttributesAccessAll, "ValueForVariableType", DataTypeIds.Double, ValueRanks.Scalar);
                    valueForVariableTypeAccessLevel.WriteMask = AttributeWriteMask.ValueForVariableType;
                    valueForVariableTypeAccessLevel.UserWriteMask = AttributeWriteMask.ValueForVariableType;
                    variables.Add(valueForVariableTypeAccessLevel);

                    BaseDataVariableState allAccessLevel = CreateVariable(folderAttributesAccessAll, "All", DataTypeIds.Double, ValueRanks.Scalar);
                    allAccessLevel.WriteMask = AttributeWriteMask.AccessLevel | AttributeWriteMask.ArrayDimensions | AttributeWriteMask.BrowseName | AttributeWriteMask.ContainsNoLoops | AttributeWriteMask.DataType |
                                               AttributeWriteMask.Description | AttributeWriteMask.DisplayName | AttributeWriteMask.EventNotifier | AttributeWriteMask.Executable | AttributeWriteMask.Historizing | AttributeWriteMask.InverseName |
                                               AttributeWriteMask.IsAbstract |
                                               AttributeWriteMask.MinimumSamplingInterval | AttributeWriteMask.NodeClass | AttributeWriteMask.NodeId | AttributeWriteMask.Symmetric | AttributeWriteMask.UserAccessLevel |
                                               AttributeWriteMask.UserExecutable |
                                               AttributeWriteMask.UserWriteMask | AttributeWriteMask.ValueForVariableType | AttributeWriteMask.ValueRank | AttributeWriteMask.WriteMask;
                    allAccessLevel.UserWriteMask = AttributeWriteMask.AccessLevel | AttributeWriteMask.ArrayDimensions | AttributeWriteMask.BrowseName | AttributeWriteMask.ContainsNoLoops | AttributeWriteMask.DataType |
                                                   AttributeWriteMask.Description | AttributeWriteMask.DisplayName | AttributeWriteMask.EventNotifier | AttributeWriteMask.Executable | AttributeWriteMask.Historizing | AttributeWriteMask.InverseName |
                                                   AttributeWriteMask.IsAbstract |
                                                   AttributeWriteMask.MinimumSamplingInterval | AttributeWriteMask.NodeClass | AttributeWriteMask.NodeId | AttributeWriteMask.Symmetric | AttributeWriteMask.UserAccessLevel |
                                                   AttributeWriteMask.UserExecutable |
                                                   AttributeWriteMask.UserWriteMask | AttributeWriteMask.ValueForVariableType | AttributeWriteMask.ValueRank | AttributeWriteMask.WriteMask;
                    variables.Add(allAccessLevel);

                    #endregion

                    #region AccessUser1

                    FolderState folderAttributesAccessUser1 = CreateFolder(folderAttributes, "AccessUser1");
                    
                    BaseDataVariableState accessLevelAccessUser1 = CreateVariable(folderAttributesAccessUser1, "AccessLevel", DataTypeIds.Double, ValueRanks.Scalar);
                    accessLevelAccessAll.WriteMask = AttributeWriteMask.AccessLevel;
                    accessLevelAccessAll.UserWriteMask = AttributeWriteMask.AccessLevel;
                    variables.Add(accessLevelAccessAll);

                    BaseDataVariableState arrayDimensionsAccessUser1 = CreateVariable(folderAttributesAccessUser1, "ArrayDimensions", DataTypeIds.Double, ValueRanks.Scalar);
                    arrayDimensionsAccessUser1.WriteMask = AttributeWriteMask.ArrayDimensions;
                    arrayDimensionsAccessUser1.UserWriteMask = AttributeWriteMask.ArrayDimensions;
                    variables.Add(arrayDimensionsAccessUser1);

                    BaseDataVariableState browseNameAccessUser1 = CreateVariable(folderAttributesAccessUser1, "BrowseName", DataTypeIds.Double, ValueRanks.Scalar);
                    browseNameAccessUser1.WriteMask = AttributeWriteMask.BrowseName;
                    browseNameAccessUser1.UserWriteMask = AttributeWriteMask.BrowseName;
                    variables.Add(browseNameAccessUser1);

                    BaseDataVariableState containsNoLoopsAccessUser1 = CreateVariable(folderAttributesAccessUser1, "ContainsNoLoops", DataTypeIds.Double, ValueRanks.Scalar);
                    containsNoLoopsAccessUser1.WriteMask = AttributeWriteMask.ContainsNoLoops;
                    containsNoLoopsAccessUser1.UserWriteMask = AttributeWriteMask.ContainsNoLoops;
                    variables.Add(containsNoLoopsAccessUser1);

                    BaseDataVariableState dataTypeAccessUser1 = CreateVariable(folderAttributesAccessUser1, "DataType", DataTypeIds.Double, ValueRanks.Scalar);
                    dataTypeAccessUser1.WriteMask = AttributeWriteMask.DataType;
                    dataTypeAccessUser1.UserWriteMask = AttributeWriteMask.DataType;
                    variables.Add(dataTypeAccessUser1);

                    BaseDataVariableState descriptionAccessUser1 = CreateVariable(folderAttributesAccessUser1, "Description", DataTypeIds.Double, ValueRanks.Scalar);
                    descriptionAccessUser1.WriteMask = AttributeWriteMask.Description;
                    descriptionAccessUser1.UserWriteMask = AttributeWriteMask.Description;
                    variables.Add(descriptionAccessUser1);

                    BaseDataVariableState eventNotifierAccessUser1 = CreateVariable(folderAttributesAccessUser1, "EventNotifier", DataTypeIds.Double, ValueRanks.Scalar);
                    eventNotifierAccessUser1.WriteMask = AttributeWriteMask.EventNotifier;
                    eventNotifierAccessUser1.UserWriteMask = AttributeWriteMask.EventNotifier;
                    variables.Add(eventNotifierAccessUser1);

                    BaseDataVariableState executableAccessUser1 = CreateVariable(folderAttributesAccessUser1, "Executable", DataTypeIds.Double, ValueRanks.Scalar);
                    executableAccessUser1.WriteMask = AttributeWriteMask.Executable;
                    executableAccessUser1.UserWriteMask = AttributeWriteMask.Executable;
                    variables.Add(executableAccessUser1);

                    BaseDataVariableState historizingAccessUser1 = CreateVariable(folderAttributesAccessUser1, "Historizing", DataTypeIds.Double, ValueRanks.Scalar);
                    historizingAccessUser1.WriteMask = AttributeWriteMask.Historizing;
                    historizingAccessUser1.UserWriteMask = AttributeWriteMask.Historizing;
                    variables.Add(historizingAccessUser1);

                    BaseDataVariableState inverseNameAccessUser1 = CreateVariable(folderAttributesAccessUser1, "InverseName", DataTypeIds.Double, ValueRanks.Scalar);
                    inverseNameAccessUser1.WriteMask = AttributeWriteMask.InverseName;
                    inverseNameAccessUser1.UserWriteMask = AttributeWriteMask.InverseName;
                    variables.Add(inverseNameAccessUser1);

                    BaseDataVariableState isAbstractAccessUser1 = CreateVariable(folderAttributesAccessUser1, "IsAbstract", DataTypeIds.Double, ValueRanks.Scalar);
                    isAbstractAccessUser1.WriteMask = AttributeWriteMask.IsAbstract;
                    isAbstractAccessUser1.UserWriteMask = AttributeWriteMask.IsAbstract;
                    variables.Add(isAbstractAccessUser1);

                    BaseDataVariableState minimumSamplingIntervalAccessUser1 = CreateVariable(folderAttributesAccessUser1, "MinimumSamplingInterval", DataTypeIds.Double, ValueRanks.Scalar);
                    minimumSamplingIntervalAccessUser1.WriteMask = AttributeWriteMask.MinimumSamplingInterval;
                    minimumSamplingIntervalAccessUser1.UserWriteMask = AttributeWriteMask.MinimumSamplingInterval;
                    variables.Add(minimumSamplingIntervalAccessUser1);

                    BaseDataVariableState nodeClassIntervalAccessUser1 = CreateVariable(folderAttributesAccessUser1, "NodeClass", DataTypeIds.Double, ValueRanks.Scalar);
                    nodeClassIntervalAccessUser1.WriteMask = AttributeWriteMask.NodeClass;
                    nodeClassIntervalAccessUser1.UserWriteMask = AttributeWriteMask.NodeClass;
                    variables.Add(nodeClassIntervalAccessUser1);

                    BaseDataVariableState nodeIdAccessUser1 = CreateVariable(folderAttributesAccessUser1, "NodeId", DataTypeIds.Double, ValueRanks.Scalar);
                    nodeIdAccessUser1.WriteMask = AttributeWriteMask.NodeId;
                    nodeIdAccessUser1.UserWriteMask = AttributeWriteMask.NodeId;
                    variables.Add(nodeIdAccessUser1);

                    BaseDataVariableState symmetricAccessUser1 = CreateVariable(folderAttributesAccessUser1, "Symmetric", DataTypeIds.Double, ValueRanks.Scalar);
                    symmetricAccessUser1.WriteMask = AttributeWriteMask.Symmetric;
                    symmetricAccessUser1.UserWriteMask = AttributeWriteMask.Symmetric;
                    variables.Add(symmetricAccessUser1);

                    BaseDataVariableState userAccessUser1AccessUser1 = CreateVariable(folderAttributesAccessUser1, "UserAccessUser1", DataTypeIds.Double, ValueRanks.Scalar);
                    userAccessUser1AccessUser1.WriteMask = AttributeWriteMask.UserAccessLevel;
                    userAccessUser1AccessUser1.UserWriteMask = AttributeWriteMask.UserAccessLevel;
                    variables.Add(userAccessUser1AccessUser1);

                    BaseDataVariableState userExecutableAccessUser1 = CreateVariable(folderAttributesAccessUser1, "UserExecutable", DataTypeIds.Double, ValueRanks.Scalar);
                    userExecutableAccessUser1.WriteMask = AttributeWriteMask.UserExecutable;
                    userExecutableAccessUser1.UserWriteMask = AttributeWriteMask.UserExecutable;
                    variables.Add(userExecutableAccessUser1);

                    BaseDataVariableState valueRankAccessUser1 = CreateVariable(folderAttributesAccessUser1, "ValueRank", DataTypeIds.Double, ValueRanks.Scalar);
                    valueRankAccessUser1.WriteMask = AttributeWriteMask.ValueRank;
                    valueRankAccessUser1.UserWriteMask = AttributeWriteMask.ValueRank;
                    variables.Add(valueRankAccessUser1);

                    BaseDataVariableState writeMaskAccessUser1 = CreateVariable(folderAttributesAccessUser1, "WriteMask", DataTypeIds.Double, ValueRanks.Scalar);
                    writeMaskAccessUser1.WriteMask = AttributeWriteMask.WriteMask;
                    writeMaskAccessUser1.UserWriteMask = AttributeWriteMask.WriteMask;
                    variables.Add(writeMaskAccessUser1);

                    BaseDataVariableState valueForVariableTypeAccessUser1 = CreateVariable(folderAttributesAccessUser1, "ValueForVariableType", DataTypeIds.Double, ValueRanks.Scalar);
                    valueForVariableTypeAccessUser1.WriteMask = AttributeWriteMask.ValueForVariableType;
                    valueForVariableTypeAccessUser1.UserWriteMask = AttributeWriteMask.ValueForVariableType;
                    variables.Add(valueForVariableTypeAccessUser1);

                    BaseDataVariableState allAccessUser1 = CreateVariable(folderAttributesAccessUser1, "All", DataTypeIds.Double, ValueRanks.Scalar);
                    allAccessUser1.WriteMask = AttributeWriteMask.AccessLevel | AttributeWriteMask.ArrayDimensions | AttributeWriteMask.BrowseName | AttributeWriteMask.ContainsNoLoops | AttributeWriteMask.DataType |
                                               AttributeWriteMask.Description | AttributeWriteMask.DisplayName | AttributeWriteMask.EventNotifier | AttributeWriteMask.Executable | AttributeWriteMask.Historizing | AttributeWriteMask.InverseName |
                                               AttributeWriteMask.IsAbstract |
                                               AttributeWriteMask.MinimumSamplingInterval | AttributeWriteMask.NodeClass | AttributeWriteMask.NodeId | AttributeWriteMask.Symmetric | AttributeWriteMask.UserAccessLevel |
                                               AttributeWriteMask.UserExecutable |
                                               AttributeWriteMask.UserWriteMask | AttributeWriteMask.ValueForVariableType | AttributeWriteMask.ValueRank | AttributeWriteMask.WriteMask;
                    allAccessUser1.UserWriteMask = AttributeWriteMask.AccessLevel | AttributeWriteMask.ArrayDimensions | AttributeWriteMask.BrowseName | AttributeWriteMask.ContainsNoLoops | AttributeWriteMask.DataType |
                                                   AttributeWriteMask.Description | AttributeWriteMask.DisplayName | AttributeWriteMask.EventNotifier | AttributeWriteMask.Executable | AttributeWriteMask.Historizing | AttributeWriteMask.InverseName |
                                                   AttributeWriteMask.IsAbstract |
                                                   AttributeWriteMask.MinimumSamplingInterval | AttributeWriteMask.NodeClass | AttributeWriteMask.NodeId | AttributeWriteMask.Symmetric | AttributeWriteMask.UserAccessLevel |
                                                   AttributeWriteMask.UserExecutable |
                                                   AttributeWriteMask.UserWriteMask | AttributeWriteMask.ValueForVariableType | AttributeWriteMask.ValueRank | AttributeWriteMask.WriteMask;
                    variables.Add(allAccessUser1);

                    #endregion

                    #endregion

                    #region MyCompany

                    FolderState myCompanyFolder = CreateFolder(root, "MyCompany");
                    
                    BaseDataVariableState myCompanyInstructions = CreateVariable(myCompanyFolder, "Instructions", DataTypeIds.String, ValueRanks.Scalar);
                    myCompanyInstructions.Value = "A place for the vendor to describe their address-space.";
                    variables.Add(myCompanyInstructions);

                    #endregion
                }
                catch (Exception e)
                {
                    Utils.Trace(e, "Error creating the address space.");
                }

                AddPredefinedNode(SystemContext, root);
                m_simulationTimer = new Timer(DoSimulation, null, 1000, 1000);

                // Import a node set file containing structured data types.
                ImportNodeSet();
            }
        }

        private ServiceResult OnWriteInterval(ISystemContext context, NodeState node, ref object value)
        {
            try
            {
                m_simulationInterval = (UInt16) value;

                if (m_simulationEnabled)
                {
                    m_simulationTimer.Change(100, (int) m_simulationInterval);
                }

                return ServiceResult.Good;
            }
            catch (Exception e)
            {
                Utils.Trace(e, "Error writing Interval variable.");
                return ServiceResult.Create(e, StatusCodes.Bad, "Error writing Interval variable.");
            }
        }

        private ServiceResult OnWriteEnabled(ISystemContext context, NodeState node, ref object value)
        {
            try
            {
                m_simulationEnabled = (bool) value;

                if (m_simulationEnabled)
                {
                    m_simulationTimer.Change(100, (int) m_simulationInterval);
                }
                else
                {
                    m_simulationTimer.Change(100, 0);
                }

                return ServiceResult.Good;
            }
            catch (Exception e)
            {
                Utils.Trace(e, "Error writing Enabled variable.");
                return ServiceResult.Create(e, StatusCodes.Bad, "Error writing Enabled variable.");
            }
        }

        /// <summary>
        /// Creates a new folder.
        /// </summary>
        private FolderState CreateFolder(NodeState parent, string path, string name)
        {
            FolderState folder = CreateFolder(parent, path);
            folder.SymbolicName = name;
            folder.DisplayName = new LocalizedText("en", name);

            return folder;
        }
        
        /// <summary>
        /// Creates a new object.
        /// </summary>
        private BaseObjectState CreateObject(NodeState parent, string path, string name)
        {
            BaseObjectState folder = CreateObject(parent, path);
            folder.BrowseName = new QualifiedName(name, NamespaceIndex);
            folder.DisplayName = folder.BrowseName.Name;
            folder.SymbolicName = name;

            return folder;
        }

        /// <summary>
        /// Creates a new object type.
        /// </summary>
        private BaseObjectTypeState CreateObjectType(NodeState parent, IDictionary<NodeId, IList<IReference>> externalReferences, string path, string name)
        {
            BaseObjectTypeState type = new BaseObjectTypeState();

            type.SymbolicName = name;
            type.SuperTypeId = ObjectTypeIds.BaseObjectType;
            type.NodeId = new NodeId(path, NamespaceIndex);
            type.BrowseName = new QualifiedName(name, NamespaceIndex);
            type.DisplayName = type.BrowseName.Name;
            type.WriteMask = AttributeWriteMask.None;
            type.UserWriteMask = AttributeWriteMask.None;
            type.IsAbstract = false;

            IList<IReference> references = null;

            if (!externalReferences.TryGetValue(ObjectTypeIds.BaseObjectType, out references))
            {
                externalReferences[ObjectTypeIds.BaseObjectType] = references = new List<IReference>();
            }

            references.Add(new NodeStateReference(ReferenceTypes.HasSubtype, false, type.NodeId));

            if (parent != null)
            {
                parent.AddReference(ReferenceTypes.Organizes, false, type.NodeId);
                type.AddReference(ReferenceTypes.Organizes, true, parent.NodeId);
            }

            AddPredefinedNode(SystemContext, type);
            return type;
        }

        /// <summary>
        /// Creates a new variable.
        /// </summary>
        private BaseDataVariableState CreateMeshVariable(NodeState parent, string path, string name, params NodeState[] peers)
        {
            BaseDataVariableState variable = CreateVariable(parent, name, BuiltInType.Double, ValueRanks.Scalar);

            if (peers != null)
            {
                foreach (NodeState peer in peers)
                {
                    peer.AddReference(ReferenceTypes.HasCause, false, variable.NodeId);
                    variable.AddReference(ReferenceTypes.HasCause, true, peer.NodeId);
                    peer.AddReference(ReferenceTypes.HasEffect, true, variable.NodeId);
                    variable.AddReference(ReferenceTypes.HasEffect, false, peer.NodeId);
                }
            }

            return variable;
        }

        /// <summary>
        /// Creates a new variable.
        /// </summary>
        private DataItemState CreateDataItemVariable(NodeState parent, string name, BuiltInType dataType, int valueRank)
        {
            DataItemState variable = base.CreateDataItemVariable(parent, name, (uint)dataType, valueRank);

            variable.DisplayName = new LocalizedText("en", name);

            variable.Value = Opc.Ua.TypeInfo.GetDefaultValue((uint)dataType, valueRank, Server.TypeTree);

            variable.ValuePrecision.Value = 2;
            variable.ValuePrecision.AccessLevel = AccessLevels.CurrentReadOrWrite;
            variable.ValuePrecision.UserAccessLevel = AccessLevels.CurrentReadOrWrite;
            variable.Definition.Value = String.Empty;
            variable.Definition.AccessLevel = AccessLevels.CurrentReadOrWrite;
            variable.Definition.UserAccessLevel = AccessLevels.CurrentReadOrWrite;

            return variable;
        }

        private DataItemState[] CreateDataItemVariables(NodeState parent, string name, BuiltInType dataType, int valueRank, UInt16 numVariables)
        {
            List<DataItemState> itemsCreated = new List<DataItemState>();
            // create the default name first:
            itemsCreated.Add(CreateDataItemVariable(parent, name, dataType, valueRank));
            // now to create the remaining NUMBERED items
            for (uint i = 0; i < numVariables; i++)
            {
                string newName = string.Format("{0}{1}", name, i.ToString("000"));
                string newPath = string.Format("{0}/Mass/{1}", name, newName);
                itemsCreated.Add(CreateDataItemVariable(parent, newName, dataType, valueRank));
            } //for i
            return (itemsCreated.ToArray());
        }

        private ServiceResult OnWriteDataItem(
            ISystemContext context,
            NodeState node,
            NumericRange indexRange,
            QualifiedName dataEncoding,
            ref object value,
            ref StatusCode statusCode,
            ref DateTime timestamp)
        {
            DataItemState variable = node as DataItemState;

            // verify data type.
            Opc.Ua.TypeInfo typeInfo = Opc.Ua.TypeInfo.IsInstanceOfDataType(
                value,
                variable.DataType,
                variable.ValueRank,
                context.NamespaceUris,
                context.TypeTable);

            if (typeInfo == null || typeInfo == Opc.Ua.TypeInfo.Unknown)
            {
                return StatusCodes.BadTypeMismatch;
            }

            if (typeInfo.BuiltInType != BuiltInType.DateTime)
            {
                double number = Convert.ToDouble(value);
                number = Math.Round(number, (int) variable.ValuePrecision.Value);
                value = Opc.Ua.TypeInfo.Cast(number, typeInfo.BuiltInType);
            }

            return ServiceResult.Good;
        }

        /// <summary>
        /// Creates a new variable.
        /// </summary>
        private AnalogItemState CreateAnalogItemVariable(NodeState parent, string path, string name, BuiltInType dataType, int valueRank)
        {
            return (CreateAnalogItemVariable(parent, path, name, dataType, valueRank, null));
        }

        private AnalogItemState CreateAnalogItemVariable(NodeState parent, string path, string name, BuiltInType dataType, int valueRank, object initialValues)
        {
            return (CreateAnalogItemVariable(parent, path, name, dataType, valueRank, initialValues, null));
        }

        private AnalogItemState CreateAnalogItemVariable(NodeState parent, string path, string name, BuiltInType dataType, int valueRank, object initialValues, Range customRange)
        {
            return CreateAnalogItemVariable(parent, path, name, (uint) dataType, valueRank, initialValues, customRange);
        }

        private AnalogItemState CreateAnalogItemVariable(NodeState parent, string path, string name, NodeId dataType, int valueRank, object initialValues, Range customRange)
        {
            AnalogItemState variable = new AnalogItemState(parent);
            variable.BrowseName = new QualifiedName(path, NamespaceIndex);
            variable.EngineeringUnits = new PropertyState<EUInformation>(variable);
            variable.InstrumentRange = new PropertyState<Range>(variable);

            variable.Create(
                SystemContext,
                new NodeId(path, NamespaceIndex),
                variable.BrowseName,
                null,
                true);

            variable.NodeId = new NodeId(path, NamespaceIndex);
            variable.SymbolicName = name;
            variable.DisplayName = new LocalizedText("en", name);
            variable.WriteMask = AttributeWriteMask.None;
            variable.UserWriteMask = AttributeWriteMask.None;
            variable.ReferenceTypeId = ReferenceTypes.Organizes;
            variable.DataType = dataType;
            variable.ValueRank = valueRank;
            variable.AccessLevel = AccessLevels.CurrentReadOrWrite;
            variable.UserAccessLevel = AccessLevels.CurrentReadOrWrite;
            variable.Historizing = false;

            if (valueRank == ValueRanks.OneDimension)
            {
                variable.ArrayDimensions = new ReadOnlyList<uint>(new List<uint> {0});
            }
            else if (valueRank == ValueRanks.TwoDimensions)
            {
                variable.ArrayDimensions = new ReadOnlyList<uint>(new List<uint> {0, 0});
            }

            BuiltInType builtInType = Opc.Ua.TypeInfo.GetBuiltInType(dataType, Server.TypeTree);

            // Simulate a mV Voltmeter
            Range newRange = GetAnalogRange(builtInType);
            // Using anything but 120,-10 fails a few tests
            newRange.High = Math.Min(newRange.High, 120);
            newRange.Low = Math.Max(newRange.Low, -10);
            variable.InstrumentRange.Value = newRange;

            if (customRange != null)
            {
                variable.EURange.Value = customRange;
            }
            else
            {
                variable.EURange.Value = new Range(100, 0);
            }

            if (initialValues == null)
            {
                variable.Value = Opc.Ua.TypeInfo.GetDefaultValue(dataType, valueRank, Server.TypeTree);
            }
            else
            {
                variable.Value = initialValues;
            }

            variable.StatusCode = StatusCodes.Good;
            variable.Timestamp = DateTime.UtcNow;
            // The latest UNECE version (Rev 11, published in 2015) is available here:
            // http://www.opcfoundation.org/UA/EngineeringUnits/UNECE/rec20_latest_08052015.zip
            variable.EngineeringUnits.Value = new EUInformation("mV", "millivolt", "http://www.opcfoundation.org/UA/units/un/cefact");
            // The mapping of the UNECE codes to OPC UA(EUInformation.unitId) is available here:
            // http://www.opcfoundation.org/UA/EngineeringUnits/UNECE/UNECE_to_OPCUA.csv
            variable.EngineeringUnits.Value.UnitId = 12890; // "2Z"
            variable.OnWriteValue = OnWriteAnalog;
            variable.EURange.OnWriteValue = OnWriteAnalogRange;
            variable.EURange.AccessLevel = AccessLevels.CurrentReadOrWrite;
            variable.EURange.UserAccessLevel = AccessLevels.CurrentReadOrWrite;
            variable.EngineeringUnits.AccessLevel = AccessLevels.CurrentReadOrWrite;
            variable.EngineeringUnits.UserAccessLevel = AccessLevels.CurrentReadOrWrite;
            variable.InstrumentRange.OnWriteValue = OnWriteAnalogRange;
            variable.InstrumentRange.AccessLevel = AccessLevels.CurrentReadOrWrite;
            variable.InstrumentRange.UserAccessLevel = AccessLevels.CurrentReadOrWrite;

            if (parent != null)
            {
                parent.AddChild(variable);
            }

            return variable;
        }

        /// <summary>
        /// Creates a new variable.
        /// </summary>
        private DataItemState CreateTwoStateDiscreteItemVariable(NodeState parent, string path, string name, string trueState, string falseState)
        {
            TwoStateDiscreteState variable = new TwoStateDiscreteState(parent);

            variable.NodeId = new NodeId(path, NamespaceIndex);
            variable.BrowseName = new QualifiedName(path, NamespaceIndex);
            variable.DisplayName = new LocalizedText("en", name);
            variable.WriteMask = AttributeWriteMask.None;
            variable.UserWriteMask = AttributeWriteMask.None;

            variable.Create(
                SystemContext,
                null,
                variable.BrowseName,
                null,
                true);

            variable.SymbolicName = name;
            variable.ReferenceTypeId = ReferenceTypes.Organizes;
            variable.DataType = DataTypeIds.Boolean;
            variable.ValueRank = ValueRanks.Scalar;
            variable.AccessLevel = AccessLevels.CurrentReadOrWrite;
            variable.UserAccessLevel = AccessLevels.CurrentReadOrWrite;
            variable.Historizing = false;
            variable.Value = (bool) GetNewValue(variable);
            variable.StatusCode = StatusCodes.Good;
            variable.Timestamp = DateTime.UtcNow;

            variable.TrueState.Value = trueState;
            variable.TrueState.AccessLevel = AccessLevels.CurrentReadOrWrite;
            variable.TrueState.UserAccessLevel = AccessLevels.CurrentReadOrWrite;

            variable.FalseState.Value = falseState;
            variable.FalseState.AccessLevel = AccessLevels.CurrentReadOrWrite;
            variable.FalseState.UserAccessLevel = AccessLevels.CurrentReadOrWrite;

            if (parent != null)
            {
                parent.AddChild(variable);
            }

            return variable;
        }

        /// <summary>
        /// Creates a new variable.
        /// </summary>
        private DataItemState CreateMultiStateDiscreteItemVariable(NodeState parent, string path, string name, params string[] values)
        {
            MultiStateDiscreteState variable = new MultiStateDiscreteState(parent);

            variable.NodeId = new NodeId(path, NamespaceIndex);
            variable.BrowseName = new QualifiedName(path, NamespaceIndex);
            variable.DisplayName = new LocalizedText("en", name);
            variable.WriteMask = AttributeWriteMask.None;
            variable.UserWriteMask = AttributeWriteMask.None;

            variable.Create(
                SystemContext,
                null,
                variable.BrowseName,
                null,
                true);

            variable.SymbolicName = name;
            variable.ReferenceTypeId = ReferenceTypes.Organizes;
            variable.DataType = DataTypeIds.UInt32;
            variable.ValueRank = ValueRanks.Scalar;
            variable.AccessLevel = AccessLevels.CurrentReadOrWrite;
            variable.UserAccessLevel = AccessLevels.CurrentReadOrWrite;
            variable.Historizing = false;
            variable.Value = (uint) 0;
            variable.StatusCode = StatusCodes.Good;
            variable.Timestamp = DateTime.UtcNow;
            variable.OnWriteValue = OnWriteDiscrete;

            LocalizedText[] strings = new LocalizedText[values.Length];

            for (int ii = 0; ii < strings.Length; ii++)
            {
                strings[ii] = values[ii];
            }

            variable.EnumStrings.Value = strings;
            variable.EnumStrings.AccessLevel = AccessLevels.CurrentReadOrWrite;
            variable.EnumStrings.UserAccessLevel = AccessLevels.CurrentReadOrWrite;

            if (parent != null)
            {
                parent.AddChild(variable);
            }

            return variable;
        }

        /// <summary>
        /// Creates a new UInt32 variable.
        /// </summary>
        private DataItemState CreateMultiStateValueDiscreteItemVariable(NodeState parent, string path, string name, params string[] enumNames)
        {
            return CreateMultiStateValueDiscreteItemVariable(parent, path, name, null, enumNames);
        }

        /// <summary>
        /// Creates a new variable.
        /// </summary>
        private DataItemState CreateMultiStateValueDiscreteItemVariable(NodeState parent, string path, string name, NodeId nodeId, params string[] enumNames)
        {
            MultiStateValueDiscreteState variable = new MultiStateValueDiscreteState(parent);

            variable.NodeId = new NodeId(path, NamespaceIndex);
            variable.BrowseName = new QualifiedName(path, NamespaceIndex);
            variable.DisplayName = new LocalizedText("en", name);
            variable.WriteMask = AttributeWriteMask.None;
            variable.UserWriteMask = AttributeWriteMask.None;

            variable.Create(
                SystemContext,
                null,
                variable.BrowseName,
                null,
                true);

            variable.SymbolicName = name;
            variable.ReferenceTypeId = ReferenceTypes.Organizes;
            variable.DataType = (nodeId == null) ? DataTypeIds.UInt32 : nodeId;
            variable.ValueRank = ValueRanks.Scalar;
            variable.AccessLevel = AccessLevels.CurrentReadOrWrite;
            variable.UserAccessLevel = AccessLevels.CurrentReadOrWrite;
            variable.Historizing = false;
            variable.Value = (uint) 0;
            variable.StatusCode = StatusCodes.Good;
            variable.Timestamp = DateTime.UtcNow;
            variable.OnWriteValue = OnWriteValueDiscrete;

            // there are two enumerations for this type:
            // EnumStrings = the string representations for enumerated values
            // ValueAsText = the actual enumerated value

            // set the enumerated strings
            LocalizedText[] strings = new LocalizedText[enumNames.Length];
            for (int ii = 0; ii < strings.Length; ii++)
            {
                strings[ii] = enumNames[ii];
            }

            // set the enumerated values
            EnumValueType[] values = new EnumValueType[enumNames.Length];
            for (int ii = 0; ii < values.Length; ii++)
            {
                values[ii] = new EnumValueType();
                values[ii].Value = ii;
                values[ii].Description = strings[ii];
                values[ii].DisplayName = strings[ii];
            }
            variable.EnumValues.Value = values;
            variable.EnumValues.AccessLevel = AccessLevels.CurrentReadOrWrite;
            variable.EnumValues.UserAccessLevel = AccessLevels.CurrentReadOrWrite;
            variable.ValueAsText.Value = variable.EnumValues.Value[0].DisplayName;

            if (parent != null)
            {
                parent.AddChild(variable);
            }

            return variable;
        }

        private ServiceResult OnWriteDiscrete(
            ISystemContext context,
            NodeState node,
            NumericRange indexRange,
            QualifiedName dataEncoding,
            ref object value,
            ref StatusCode statusCode,
            ref DateTime timestamp)
        {
            MultiStateDiscreteState variable = node as MultiStateDiscreteState;

            // verify data type.
            Opc.Ua.TypeInfo typeInfo = Opc.Ua.TypeInfo.IsInstanceOfDataType(
                value,
                variable.DataType,
                variable.ValueRank,
                context.NamespaceUris,
                context.TypeTable);

            if (typeInfo == null || typeInfo == Opc.Ua.TypeInfo.Unknown)
            {
                return StatusCodes.BadTypeMismatch;
            }

            if (indexRange != NumericRange.Empty)
            {
                return StatusCodes.BadIndexRangeInvalid;
            }

            double number = Convert.ToDouble(value);

            if (number >= variable.EnumStrings.Value.Length | number < 0)
            {
                return StatusCodes.BadOutOfRange;
            }

            return ServiceResult.Good;
        }

        private ServiceResult OnWriteValueDiscrete(
            ISystemContext context,
            NodeState node,
            NumericRange indexRange,
            QualifiedName dataEncoding,
            ref object value,
            ref StatusCode statusCode,
            ref DateTime timestamp)
        {
            MultiStateValueDiscreteState variable = node as MultiStateValueDiscreteState;

            TypeInfo typeInfo = TypeInfo.Construct(value);

            if (variable == null ||
                typeInfo == null ||
                typeInfo == Opc.Ua.TypeInfo.Unknown ||
                !TypeInfo.IsNumericType(typeInfo.BuiltInType))
            {
                return StatusCodes.BadTypeMismatch;
            }

            if (indexRange != NumericRange.Empty)
            {
                return StatusCodes.BadIndexRangeInvalid;
            }

            Int32 number = Convert.ToInt32(value);
            if (number >= variable.EnumValues.Value.Length || number < 0)
            {
                return StatusCodes.BadOutOfRange;
            }

            if (!node.SetChildValue(context, BrowseNames.ValueAsText, variable.EnumValues.Value[number].DisplayName, true))
            {
                return StatusCodes.BadOutOfRange;
            }

            node.ClearChangeMasks(context, true);

            return ServiceResult.Good;
        }

        private ServiceResult OnWriteAnalog(
            ISystemContext context,
            NodeState node,
            NumericRange indexRange,
            QualifiedName dataEncoding,
            ref object value,
            ref StatusCode statusCode,
            ref DateTime timestamp)
        {
            AnalogItemState variable = node as AnalogItemState;

            // verify data type.
            Opc.Ua.TypeInfo typeInfo = Opc.Ua.TypeInfo.IsInstanceOfDataType(
                value,
                variable.DataType,
                variable.ValueRank,
                context.NamespaceUris,
                context.TypeTable);

            if (typeInfo == null || typeInfo == Opc.Ua.TypeInfo.Unknown)
            {
                return StatusCodes.BadTypeMismatch;
            }

            // check index range.
            if (variable.ValueRank >= 0)
            {
                if (indexRange != NumericRange.Empty)
                {
                    object target = variable.Value;
                    ServiceResult result = indexRange.UpdateRange(ref target, value);

                    if (ServiceResult.IsBad(result))
                    {
                        return result;
                    }

                    value = target;
                }
            }

            // check instrument range.
            else
            {
                if (indexRange != NumericRange.Empty)
                {
                    return StatusCodes.BadIndexRangeInvalid;
                }

                double number = Convert.ToDouble(value);

                if (variable.InstrumentRange != null && (number < variable.InstrumentRange.Value.Low || number > variable.InstrumentRange.Value.High))
                {
                    return StatusCodes.BadOutOfRange;
                }
            }

            return ServiceResult.Good;
        }

        private ServiceResult OnWriteAnalogRange(
            ISystemContext context,
            NodeState node,
            NumericRange indexRange,
            QualifiedName dataEncoding,
            ref object value,
            ref StatusCode statusCode,
            ref DateTime timestamp)
        {
            PropertyState<Range> variable = node as PropertyState<Range>;
            ExtensionObject extensionObject = value as ExtensionObject;
            TypeInfo typeInfo = TypeInfo.Construct(value);

            if (variable == null ||
                extensionObject == null ||
                typeInfo == null ||
                typeInfo == Opc.Ua.TypeInfo.Unknown)
            {
                return StatusCodes.BadTypeMismatch;
            }

            Range newRange = extensionObject.Body as Range;
            AnalogItemState parent = variable.Parent as AnalogItemState;
            if (newRange == null ||
                parent == null)
            {
                return StatusCodes.BadTypeMismatch;
            }

            if (indexRange != NumericRange.Empty)
            {
                return StatusCodes.BadIndexRangeInvalid;
            }

            TypeInfo parentTypeInfo = TypeInfo.Construct(parent.Value);
            Range parentRange = GetAnalogRange(parentTypeInfo.BuiltInType);
            if (parentRange.High < newRange.High ||
                parentRange.Low > newRange.Low)
            {
                return StatusCodes.BadOutOfRange;
            }

            value = newRange;

            return ServiceResult.Good;
        }

        /// <summary>
        /// Creates a new variable.
        /// </summary>
        private BaseDataVariableState CreateVariable(NodeState parent, string name, BuiltInType dataType, int valueRank)
        {
            return CreateVariable(parent, name, (uint) dataType, valueRank);
        }

        /// <summary>
        /// Creates a new variable.
        /// </summary>
        private BaseDataVariableState CreateVariable(NodeState parent, string name, NodeId dataType, int valueRank)
        {
            BaseDataVariableState variable = base.CreateVariable(parent, name, dataType, valueRank);
            variable.DisplayName = new LocalizedText("en", name);
            variable.SymbolicName = name;
            variable.Value = GetNewValue(variable);
            
            return variable;
        }

        private BaseDataVariableState[] CreateVariables(NodeState parent, string path, string name, BuiltInType dataType, int valueRank, UInt16 numVariables)
        {
            return CreateVariables(parent, path, name, (uint) dataType, valueRank, numVariables);
        }

        private BaseDataVariableState[] CreateVariables(NodeState parent, string path, string name, NodeId dataType, int valueRank, UInt16 numVariables)
        {
            // first, create a new Parent folder for this data-type
            FolderState newParentFolder = CreateFolder(parent, path, name);

            List<BaseDataVariableState> itemsCreated = new List<BaseDataVariableState>();
            // now to create the remaining NUMBERED items
            for (uint i = 0; i < numVariables; i++)
            {
                string newName = string.Format("{0}_{1}", name, i.ToString("00"));
                string newPath = string.Format("{0}_{1}", path, newName);
                itemsCreated.Add(CreateVariable(newParentFolder, newName, dataType, valueRank));
            }
            return (itemsCreated.ToArray());
        }

        /// <summary>
        /// Creates a new variable.
        /// </summary>
        private BaseDataVariableState CreateDynamicVariable(NodeState parent, string path, string name, BuiltInType dataType, int valueRank)
        {
            return CreateDynamicVariable(parent, name, (uint) dataType, valueRank);
        }

        /// <summary>
        /// Creates a new variable.
        /// </summary>
        private BaseDataVariableState CreateDynamicVariable(NodeState parent, string name, NodeId dataType, int valueRank)
        {
            BaseDataVariableState variable = CreateVariable(parent, name, dataType, valueRank);
            m_dynamicNodes.Add(variable);
            return variable;
        }

        private BaseDataVariableState[] CreateDynamicVariables(NodeState parent, string path, string name, BuiltInType dataType, int valueRank, uint numVariables)
        {
            return CreateDynamicVariables(parent, name, (uint) dataType, valueRank, numVariables);

        }

        private BaseDataVariableState[] CreateDynamicVariables(NodeState parent, string name, NodeId dataType, int valueRank, uint numVariables)
        {
            // first, create a new Parent folder for this data-type
            FolderState newParentFolder = CreateFolder(parent, name);

            List<BaseDataVariableState> itemsCreated = new List<BaseDataVariableState>();
            // now to create the remaining NUMBERED items
            for (uint i = 0; i < numVariables; i++)
            {
                string newName = string.Format("{0}_{1}", name, i.ToString("00"));
                //string newPath = string.Format("{0}_{1}", name, newName);
                itemsCreated.Add(CreateDynamicVariable(newParentFolder, newName, dataType, valueRank));
            } //for i
            return (itemsCreated.ToArray());
        }

        /// <summary>
        /// Creates a new variable type.
        /// </summary>
        private BaseVariableTypeState CreateVariableType(NodeState parent, IDictionary<NodeId, IList<IReference>> externalReferences, string path, string name, BuiltInType dataType, int valueRank)
        {
            BaseDataVariableTypeState type = new BaseDataVariableTypeState();

            type.SymbolicName = name;
            type.SuperTypeId = VariableTypeIds.BaseDataVariableType;
            type.NodeId = new NodeId(path, NamespaceIndex);
            type.BrowseName = new QualifiedName(name, NamespaceIndex);
            type.DisplayName = type.BrowseName.Name;
            type.WriteMask = AttributeWriteMask.None;
            type.UserWriteMask = AttributeWriteMask.None;
            type.IsAbstract = false;
            type.DataType = (uint) dataType;
            type.ValueRank = valueRank;
            type.Value = null;

            IList<IReference> references = null;

            if (!externalReferences.TryGetValue(VariableTypeIds.BaseDataVariableType, out references))
            {
                externalReferences[VariableTypeIds.BaseDataVariableType] = references = new List<IReference>();
            }

            references.Add(new NodeStateReference(ReferenceTypes.HasSubtype, false, type.NodeId));

            if (parent != null)
            {
                parent.AddReference(ReferenceTypes.Organizes, false, type.NodeId);
                type.AddReference(ReferenceTypes.Organizes, true, parent.NodeId);
            }

            AddPredefinedNode(SystemContext, type);
            return type;
        }

        /// <summary>
        /// Creates a new data type.
        /// </summary>
        private DataTypeState CreateDataType(NodeState parent, IDictionary<NodeId, IList<IReference>> externalReferences, string path, string name)
        {
            DataTypeState type = new DataTypeState();

            type.SymbolicName = name;
            type.SuperTypeId = DataTypeIds.Structure;
            type.NodeId = new NodeId(path, NamespaceIndex);
            type.BrowseName = new QualifiedName(name, NamespaceIndex);
            type.DisplayName = type.BrowseName.Name;
            type.WriteMask = AttributeWriteMask.None;
            type.UserWriteMask = AttributeWriteMask.None;
            type.IsAbstract = false;

            IList<IReference> references = null;

            if (!externalReferences.TryGetValue(DataTypeIds.Structure, out references))
            {
                externalReferences[DataTypeIds.Structure] = references = new List<IReference>();
            }

            references.Add(new NodeStateReference(ReferenceTypeIds.HasSubtype, false, type.NodeId));

            if (parent != null)
            {
                parent.AddReference(ReferenceTypes.Organizes, false, type.NodeId);
                type.AddReference(ReferenceTypes.Organizes, true, parent.NodeId);
            }

            AddPredefinedNode(SystemContext, type);
            return type;
        }

        /// <summary>
        /// Creates a new reference type.
        /// </summary>
        private ReferenceTypeState CreateReferenceType(NodeState parent, IDictionary<NodeId, IList<IReference>> externalReferences, string path, string name)
        {
            ReferenceTypeState type = new ReferenceTypeState();

            type.SymbolicName = name;
            type.SuperTypeId = ReferenceTypeIds.NonHierarchicalReferences;
            type.NodeId = new NodeId(path, NamespaceIndex);
            type.BrowseName = new QualifiedName(name, NamespaceIndex);
            type.DisplayName = type.BrowseName.Name;
            type.WriteMask = AttributeWriteMask.None;
            type.UserWriteMask = AttributeWriteMask.None;
            type.IsAbstract = false;
            type.Symmetric = true;
            type.InverseName = name;

            IList<IReference> references = null;

            if (!externalReferences.TryGetValue(ReferenceTypeIds.NonHierarchicalReferences, out references))
            {
                externalReferences[ReferenceTypeIds.NonHierarchicalReferences] = references = new List<IReference>();
            }

            references.Add(new NodeStateReference(ReferenceTypeIds.HasSubtype, false, type.NodeId));

            if (parent != null)
            {
                parent.AddReference(ReferenceTypes.Organizes, false, type.NodeId);
                type.AddReference(ReferenceTypes.Organizes, true, parent.NodeId);
            }

            AddPredefinedNode(SystemContext, type);
            return type;
        }

        /// <summary>
        /// Creates a new view.
        /// </summary>
        private ViewState CreateView(NodeState parent, IDictionary<NodeId, IList<IReference>> externalReferences, string path, string name)
        {
            ViewState type = new ViewState();

            type.SymbolicName = name;
            type.NodeId = new NodeId(path, NamespaceIndex);
            type.BrowseName = new QualifiedName(name, NamespaceIndex);
            type.DisplayName = type.BrowseName.Name;
            type.WriteMask = AttributeWriteMask.None;
            type.UserWriteMask = AttributeWriteMask.None;
            type.ContainsNoLoops = true;

            IList<IReference> references = null;

            if (!externalReferences.TryGetValue(ObjectIds.ViewsFolder, out references))
            {
                externalReferences[ObjectIds.ViewsFolder] = references = new List<IReference>();
            }

            type.AddReference(ReferenceTypeIds.Organizes, true, ObjectIds.ViewsFolder);
            references.Add(new NodeStateReference(ReferenceTypeIds.Organizes, false, type.NodeId));

            if (parent != null)
            {
                parent.AddReference(ReferenceTypes.Organizes, false, type.NodeId);
                type.AddReference(ReferenceTypes.Organizes, true, parent.NodeId);
            }

            AddPredefinedNode(SystemContext, type);
            return type;
        }

        private ServiceResult OnVoidCall(
            ISystemContext context,
            MethodState method,
            IList<object> inputArguments,
            IList<object> outputArguments)
        {
            return ServiceResult.Good;
        }

        private ServiceResult OnAddCall(
            ISystemContext context,
            MethodState method,
            IList<object> inputArguments,
            IList<object> outputArguments)
        {

            // all arguments must be provided.
            if (inputArguments.Count < 2)
            {
                return StatusCodes.BadArgumentsMissing;
            }

            try
            {
                float floatValue = (float) inputArguments[0];
                UInt32 uintValue = (UInt32) inputArguments[1];

                // set output parameter
                outputArguments[0] = (float) (floatValue + uintValue);
                return ServiceResult.Good;
            }
            catch
            {
                return new ServiceResult(StatusCodes.BadInvalidArgument);
            }
        }

        private ServiceResult OnMultiplyCall(
            ISystemContext context,
            MethodState method,
            IList<object> inputArguments,
            IList<object> outputArguments)
        {

            // all arguments must be provided.
            if (inputArguments.Count < 2)
            {
                return StatusCodes.BadArgumentsMissing;
            }

            try
            {
                Int16 op1 = (Int16) inputArguments[0];
                UInt16 op2 = (UInt16) inputArguments[1];

                // set output parameter
                outputArguments[0] = (Int32) (op1 * op2);
                return ServiceResult.Good;
            }
            catch
            {
                return new ServiceResult(StatusCodes.BadInvalidArgument);
            }
        }

        private ServiceResult OnDivideCall(
            ISystemContext context,
            MethodState method,
            IList<object> inputArguments,
            IList<object> outputArguments)
        {

            // all arguments must be provided.
            if (inputArguments.Count < 2)
            {
                return StatusCodes.BadArgumentsMissing;
            }

            try
            {
                Int32 op1 = (Int32) inputArguments[0];
                UInt16 op2 = (UInt16) inputArguments[1];

                // set output parameter
                outputArguments[0] = (float) ((float) op1 / (float) op2);
                return ServiceResult.Good;
            }
            catch
            {
                return new ServiceResult(StatusCodes.BadInvalidArgument);
            }
        }

        private ServiceResult OnSubstractCall(
            ISystemContext context,
            MethodState method,
            IList<object> inputArguments,
            IList<object> outputArguments)
        {

            // all arguments must be provided.
            if (inputArguments.Count < 2)
            {
                return StatusCodes.BadArgumentsMissing;
            }

            try
            {
                Int16 op1 = (Int16) inputArguments[0];
                Byte op2 = (Byte) inputArguments[1];

                // set output parameter
                outputArguments[0] = (Int16) (op1 - op2);
                return ServiceResult.Good;
            }
            catch
            {
                return new ServiceResult(StatusCodes.BadInvalidArgument);
            }
        }

        private ServiceResult OnHelloCall(
            ISystemContext context,
            MethodState method,
            IList<object> inputArguments,
            IList<object> outputArguments)
        {

            // all arguments must be provided.
            if (inputArguments.Count < 1)
            {
                return StatusCodes.BadArgumentsMissing;
            }

            try
            {
                string op1 = (string) inputArguments[0];

                // set output parameter
                outputArguments[0] = (string) ("hello " + op1);
                return ServiceResult.Good;
            }
            catch
            {
                return new ServiceResult(StatusCodes.BadInvalidArgument);
            }
        }

        private ServiceResult OnInputCall(
            ISystemContext context,
            MethodState method,
            IList<object> inputArguments,
            IList<object> outputArguments)
        {

            // all arguments must be provided.
            if (inputArguments.Count < 1)
            {
                return StatusCodes.BadArgumentsMissing;
            }

            return ServiceResult.Good;
        }

        private ServiceResult OnOutputCall(
            ISystemContext context,
            MethodState method,
            IList<object> inputArguments,
            IList<object> outputArguments)
        {
            // all arguments must be provided.
            try
            {
                // set output parameter
                outputArguments[0] = (string) ("Output");
                return ServiceResult.Good;
            }
            catch
            {
                return new ServiceResult(StatusCodes.BadInvalidArgument);
            }
        }

        private object GetNewValue(BaseVariableState variable)
        {
            if (m_generator == null)
            {
                m_generator = new Opc.Ua.Test.DataGenerator(null);
                m_generator.BoundaryValueFrequency = 0;
            }

            object value = null;

            while (value == null)
            {
                value = m_generator.GetRandom(variable.DataType, variable.ValueRank, new uint[] {10}, Server.TypeTree);
            }

            return value;
        }

        private void DoSimulation(object state)
        {
            try
            {
                lock (Lock)
                {
                    foreach (BaseDataVariableState variable in m_dynamicNodes)
                    {
                        variable.Value = GetNewValue(variable);
                        variable.Timestamp = DateTime.UtcNow;
                        variable.ClearChangeMasks(SystemContext, false);
                    }
                }
            }
            catch (Exception e)
            {
                Utils.Trace(e, "Unexpected error doing simulation.");
            }
        }

        /// <summary>
        /// Frees any resources allocated for the address space.
        /// </summary>
        public override void DeleteAddressSpace()
        {
            lock (Lock)
            {
                // TBD
            }
        }

        /// <summary>
        /// Returns a unique handle for the node.
        /// </summary>
        protected override NodeHandle GetManagerHandle(ServerSystemContext context, NodeId nodeId, IDictionary<NodeId, NodeState> cache)
        {
            lock (Lock)
            {
                // quickly exclude nodes that are not in the namespace. 
                if (!IsNodeIdInNamespace(nodeId))
                {
                    return null;
                }

                NodeState node = null;

                if (!PredefinedNodes.TryGetValue(nodeId, out node))
                {
                    return null;
                }

                NodeHandle handle = new NodeHandle();

                handle.NodeId = nodeId;
                handle.Node = node;
                handle.Validated = true;

                return handle;
            }
        }

        /// <summary>
        /// Verifies that the specified node exists.
        /// </summary>
        protected override NodeState ValidateNode(
            ServerSystemContext context,
            NodeHandle handle,
            IDictionary<NodeId, NodeState> cache)
        {
            // not valid if no root.
            if (handle == null)
            {
                return null;
            }

            // check if previously validated.
            if (handle.Validated)
            {
                return handle.Node;
            }

            // TBD

            return null;
        }

        /// <summary>
        /// Imports into the address space an xml file containing the model structure
        /// </summary>
        private ServiceResult ImportNodeSet()
        {
            try
            {
                string resourceName = "XamarinSampleServer._SampleServer.ReferenceServer.Model.ReferenceServer.NodeSet2.xml";
                XmlElement[] extensions = ImportNodeSetFromResource(SystemContext, resourceName);
            }
            catch (Exception ex)
            {
                Utils.Trace(Utils.TraceMasks.Error, "ReferenceNodeManager.Import", "Error loading node set: {0}", ex.Message);
                throw new ServiceResultException(ex, StatusCodes.Bad);
            }

            return ServiceResult.Good;
        }

        private XmlElement[] ImportNodeSetFromResource(ISystemContext context, string resourceName)
        {
            NodeStateCollection predefinedNodes = new NodeStateCollection();
            List<string> newNamespaceUris = new List<string>();

            XmlElement[] extensions = LoadFromNodeSet2XmlFromResource(context, resourceName, true, newNamespaceUris, predefinedNodes);

            // Add the node set to the node manager
            for (int ii = 0; ii < predefinedNodes.Count; ii++)
            {
                AddPredefinedNode(context, predefinedNodes[ii]);
            }

            foreach (var item in NamespaceUris)
            {
                if (newNamespaceUris.Contains(item))
                {
                    newNamespaceUris.Remove(item);
                }
            }

            if (newNamespaceUris.Count > 0)
            {
                List<string> allNamespaceUris = newNamespaceUris.ToList();
                allNamespaceUris.AddRange(NamespaceUris);

                SetNamespaces(allNamespaceUris.ToArray());
            }

            UpdateRegistration(this, newNamespaceUris);

            // Ensure the reverse references exist
            Dictionary<NodeId, IList<IReference>> externalReferences = new Dictionary<NodeId, IList<IReference>>();
            AddReverseReferences(externalReferences);

            foreach (var item in externalReferences)
            {
                Server.NodeManager.AddReferences(item.Key, item.Value);
            }

            return extensions;
        }

        /// <summary>
        /// Loads the NodeSet2.xml file and returns the Extensions data of the node set
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="resourceName"></param>
        /// <param name="updateTables">if set to <c>true</c> the namespace and server tables are updated with any new URIs.</param>
        /// <param name="namespaceUris">Returns the NamespaceUris defined in the node set.</param>
        /// <param name="predefinedNodes">The required NodeStateCollection</param>
        /// <returns>The collection of global extensions of the NodeSet2.xml file.</returns>
        private XmlElement[] LoadFromNodeSet2XmlFromResource(ISystemContext context, string resourceName, bool updateTables, List<string> namespaceUris, NodeStateCollection predefinedNodes)
        {
            if (resourceName == null) throw new ArgumentNullException(nameof(resourceName));

            System.Reflection.Assembly assembly = System.Reflection.Assembly.GetExecutingAssembly();
            Stream stream = assembly.GetManifestResourceStream(resourceName);

            if (stream == null)
            {
                throw ServiceResultException.Create(StatusCodes.BadDecodingError, "Could not load nodes from resource: {0}", resourceName);
            }

            return LoadFromNodeSet2(context, stream, updateTables, namespaceUris, predefinedNodes);
        }

        /// <summary>
        /// Reads the schema information from a NodeSet2 XML document
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="istrm">The data stream containing a UANodeSet file.</param>
        /// <param name="updateTables">If set to <c>true</c> the namespace and server tables are updated with any new URIs.</param>
        /// <param name="namespaceUris">Returns the NamespaceUris defined in the node set.</param>
        /// /// <param name="predefinedNodes">The required NodeStateCollection</param>
        /// <returns>The collection of global extensions of the node set.</returns>
        private XmlElement[] LoadFromNodeSet2(ISystemContext context, Stream istrm, bool updateTables, List<string> namespaceUris, NodeStateCollection predefinedNodes)
        {
            Opc.Ua.Export.UANodeSet nodeSet = Opc.Ua.Export.UANodeSet.Read(istrm);

            if (nodeSet != null)
            {
                // Update namespace table
                if (updateTables)
                {
                    if (nodeSet.NamespaceUris != null && context.NamespaceUris != null)
                    {
                        for (int ii = 0; ii < nodeSet.NamespaceUris.Length; ii++)
                        {
                            context.NamespaceUris.GetIndexOrAppend(nodeSet.NamespaceUris[ii]);
                            namespaceUris.Add(nodeSet.NamespaceUris[ii]);
                        }
                    }
                }

                // Update server table
                if (updateTables)
                {
                    if (nodeSet.ServerUris != null && context.ServerUris != null)
                    {
                        for (int ii = 0; ii < nodeSet.ServerUris.Length; ii++)
                        {
                            context.ServerUris.GetIndexOrAppend(nodeSet.ServerUris[ii]);
                        }
                    }
                }

                // Load nodes
                nodeSet.Import(context, predefinedNodes);

                return nodeSet.Extensions;
            }

            return null;
        }

        /// <summary>
        /// Updates the registration of the node manager in case of nodeset2.xml import
        /// </summary>
        /// <param name="nodeManager">The node manager that performed the import.</param>
        /// <param name="newNamespaceUris">The new namespace uris that were imported.</param>
        private void UpdateRegistration(INodeManager nodeManager, List<string> newNamespaceUris)
        {
            if (nodeManager == null || newNamespaceUris == null)
            {
                return;
            }

            int index = -1;
            int arrayLength = 0;
            foreach (var namespaceUri in newNamespaceUris)
            {
                index = Server.NamespaceUris.GetIndex(namespaceUri);
                if (index == -1)
                {
                    // Something bad happened
                    Utils.Trace(Utils.TraceMasks.Error, "Nodeset2xmlNodeManager.UpdateRegistration", "Namespace uri: " + namespaceUri + " was not found in the server's namespace table.");

                    continue;
                }

                // m_namespaceManagers is declared Private in MasterNodeManager, therefore we must use Reflection to access it
                System.Reflection.FieldInfo fieldInfo = Server.NodeManager.GetType().GetField("m_namespaceManagers",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.GetField);

                if (fieldInfo != null)
                {
                    var namespaceManagers = fieldInfo.GetValue(Server.NodeManager) as INodeManager[][];

                    if (namespaceManagers != null)
                    {
                        if (index <= namespaceManagers.Length - 1)
                        {
                            arrayLength = namespaceManagers[index].Length;
                            Array.Resize(ref namespaceManagers[index], arrayLength + 1);
                            namespaceManagers[index][arrayLength] = nodeManager;
                        }
                        else
                        {
                            Array.Resize(ref namespaceManagers, namespaceManagers.Length + 1);
                            namespaceManagers[namespaceManagers.Length - 1] = new INodeManager[] {nodeManager};
                        }

                        fieldInfo.SetValue(Server.NodeManager, namespaceManagers);
                    }
                }
            }
        }

        #endregion
    }
}
