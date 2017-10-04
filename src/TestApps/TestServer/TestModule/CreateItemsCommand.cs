using Opc.Ua;
using System;

namespace TestServer.TestModule
{
    class CreateItemsCommand 
    {
        #region Constructors

        public CreateItemsCommand(NodeState parent, TestModuleNodeManager testModule)          
        {
            // Create Items command
            m_createItemsCommand = testModule.CreateVariable(parent, parent.BrowseName.Name + "/CreateItemsCommand", "CreateItemsCommand", BuiltInType.String, ValueRanks.Scalar);
            m_createItemsCommand.OnSimpleWriteValue = OnWriteCommandVariable;
            // Item Count param
            m_itemCountParam = testModule.CreateVariable(m_createItemsCommand, parent.BrowseName.Name + "/ItemCount", "ItemCount", BuiltInType.UInt32, ValueRanks.Scalar);
            // Item Type param
            m_itemTypeParam = testModule.CreateVariable(m_createItemsCommand, parent.BrowseName.Name + "/ItemType", "ItemType", BuiltInType.UInt32, ValueRanks.Scalar);
            // ItemIsAnalog param
            m_itemIsAnalogParam = testModule.CreateVariable(m_createItemsCommand, parent.BrowseName.Name + "/ItemIsAnalog", "ItemIsAnalog", BuiltInType.Boolean, ValueRanks.Scalar);

            // ItemSetID param
            m_itemSetIdParam = testModule.CreateVariable(m_createItemsCommand, parent.BrowseName.Name + "/ItemSetID", "ItemSetID", BuiltInType.String, ValueRanks.OneDimension);
            m_itemSetIdParam.Value = new string[0];
            m_itemSetIdParam.AccessLevel = AccessLevels.CurrentRead;
            m_itemSetIdParam.UserAccessLevel = AccessLevels.CurrentRead;            
            
        }
        
        #endregion

        #region Private Methods

        /// <summary>
        /// Handle the write of the variable
        /// </summary>
        protected ServiceResult OnWriteCommandVariable(ISystemContext context, NodeState node, ref object value)
        {
            try
            {
                // CreateVariables command received

                // retreive parameter values
                uint itemCount = (uint)m_itemCountParam.Value;
                VarType itemType = (VarType)((uint)m_itemTypeParam.Value);
                bool isAnalogItem = (bool)m_itemIsAnalogParam.Value;
                string itemSetID = (string)value;

                // Check if all parameters are set
                if (itemCount != 0 && itemType != VarType.Unknown && itemSetID != null)
                {
                    BuiltInType varType = BuiltInType.Null;

                    switch (itemType)
                    {
                        case VarType.Uint8:
                            varType = BuiltInType.Byte;
                            break;
                        case VarType.Int8:
                            varType = BuiltInType.SByte;
                            break;
                        case VarType.Uint16:
                            varType = BuiltInType.UInt16;
                            break;
                        case VarType.Int16:
                            varType = BuiltInType.Int16;
                            break;
                        case VarType.Uint32:
                            varType = BuiltInType.UInt32;
                            break;
                        case VarType.Int32:
                            varType = BuiltInType.Int32;
                            break;
                        case VarType.Uint64:
                            varType = BuiltInType.UInt64;
                            break;
                        case VarType.Int64:
                            varType = BuiltInType.Int64;
                            break;
                        case VarType.Float:
                            varType = BuiltInType.Float;
                            break;
                        case VarType.Double:
                            varType = BuiltInType.Double;
                            break;
                        default:
                            varType = BuiltInType.Int32;
                            break;
                    }

                    // Create the item set
                    CreateItems(itemCount, varType, isAnalogItem, itemSetID);

                    return StatusCodes.Good;
                }
                else
                {
                    // return error
                    return ServiceResult.Create(StatusCodes.Bad, "Create Variables error: Invalid Parameters!"); 
                }                
            }
            catch (Exception e)
            {
                // Error
                return ServiceResult.Create(e, StatusCodes.Bad, "Create Variables error!");
            }           
        }

        private void CreateItems(uint itemCount, BuiltInType itemType, bool isAnalogItem, string guid)
        {
            // Get NodeManager object
            TestModuleNodeManager testModule =  ApplicationModule.Instance.GetNodeManager<TestModuleNodeManager>();
            if (testModule != null)
            {
                // Create item folder
                string folderName = String.Format("TestItems_{0}", m_createdFolders);
                FolderState itemFolder = testModule.CreateFolder(m_createItemsCommand.Parent, folderName);
                m_createdFolders++;

                // Add Simulation Folder
                SimulationFolder simulationFolder = new SimulationFolder(itemFolder, testModule, itemCount, itemType, isAnalogItem);

                // Write created folder name in created folders list
                string[] currentValue = m_itemSetIdParam.Value as string[];
                string[] newValue = new string[currentValue.Length + 1];
                currentValue.CopyTo(newValue, 0);
                newValue[currentValue.Length] = folderName + "#" + guid;

                m_itemSetIdParam.Value = (object)newValue;
                m_itemSetIdParam.ClearChangeMasks(null, true);

                testModule.AddPredefinedNode(itemFolder);
            }
        }

        #endregion

        #region Public Properties

        public DataItemState CreateItemsCmd     { get { return m_createItemsCommand; } }
        public DataItemState ItemCountParam     { get { return m_itemCountParam; } }
        public DataItemState ItemTypeParam      { get { return m_itemTypeParam; } }
        public DataItemState ItemIsAnalogParam  { get { return m_itemIsAnalogParam; } }
        public DataItemState ItemSetIdParam     { get { return m_itemSetIdParam; } }
        
        #endregion

        #region Private members

        private DataItemState m_createItemsCommand;
        private DataItemState m_itemCountParam;
        private DataItemState m_itemTypeParam;
        private DataItemState m_itemIsAnalogParam;
        private DataItemState m_itemSetIdParam;

        private int m_createdFolders = 0;
        
        #endregion
    }
}