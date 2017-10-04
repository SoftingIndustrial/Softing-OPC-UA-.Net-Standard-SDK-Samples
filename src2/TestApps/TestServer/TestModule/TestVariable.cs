using Opc.Ua;
using TestServer.StatisticsModule;

namespace TestServer.TestModule
{
    class TestVariable : DataItemState
    {
        #region Constructors
        public TestVariable(NodeState parent) : base(parent)
        {
            this.OnSimpleReadValue = OnReadTestVariable;
            this.OnSimpleWriteValue = OnWriteTestVariable;
        }
        #endregion

        #region Override

        /// <summary>
        /// Register the read of the variable
        /// </summary>
        protected ServiceResult OnReadTestVariable(
            ISystemContext context,
            NodeState node,
            ref object value)
        {
            //register the read in Statistics
            StatisticNodeManager statisticNode = ApplicationModule.Instance.GetNodeManager<StatisticNodeManager>();
            if (statisticNode != null)
            {
                statisticNode.StatisticModule.RegisterRead();
            }

            return StatusCodes.Good;           
        }


        /// <summary>
        /// Register the write of the variable
        /// </summary>
        protected ServiceResult OnWriteTestVariable(
            ISystemContext context,
            NodeState node,
            ref object value)
        {
            //register the write in Statistics
            StatisticNodeManager statisticNode = ApplicationModule.Instance.GetNodeManager<StatisticNodeManager>();
            if (statisticNode != null)
            {
                statisticNode.StatisticModule.RegisterWrite();
            }

            return StatusCodes.Good;
        }

        #endregion
    }        
}