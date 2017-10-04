using Opc.Ua;
using System;
using TestServer.StatisticsModule;

namespace TestServer.SimulationModule
{
    public class SimulationVariable : DataItemState
    {
		public uint DataTypeId { get; set; }

        #region Constructors

        public SimulationVariable(NodeState parent) : this(parent, false)
        {
        }

        public SimulationVariable(NodeState parent, bool disableStatistics) : base(parent)
        {
			m_bDisableStatistics = disableStatistics;
			DataTypeId = Opc.Ua.DataTypes.UInt32;

			if (!disableStatistics)
			{
				this.OnSimpleReadValue = OnReadSimulationVariable;
				this.OnSimpleWriteValue = OnWriteSimulationVariable;
			}
        }

        #endregion

        #region Override

        /// <summary>
        /// Register the read of the variable
        /// </summary>
        protected ServiceResult OnReadSimulationVariable(
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
        protected ServiceResult OnWriteSimulationVariable(
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

        internal void IncrementValue(double pIncrement)
		{
			Variant val = (Variant) Value;

			if (ValueRank == ValueRanks.Scalar)
			{
				switch(DataTypeId)
				{
                    case Opc.Ua.DataTypes.UInt32:
						uint increment = (uint) pIncrement;
						val.Value = (uint) val.Value + increment;
						break;
				}
			}
			else if (ValueRank == ValueRanks.OneDimension)
			{
				switch(DataTypeId)
				{
                    case Opc.Ua.DataTypes.UInt32:
						{
							uint increment = (uint) pIncrement;
							val.Value = (uint) val.Value + increment;
							break;
						}
                    case Opc.Ua.DataTypes.Byte:
						{
							byte increment = (byte) pIncrement;
							byte[] prevValue = (byte[]) val.Value;

							prevValue = (byte[]) prevValue.Clone();
							prevValue[0] += increment;

							val.Value = prevValue;
							
							break;
						}
				}
			}
			
			Value = val;
			Timestamp = DateTime.UtcNow;

			ClearChangeMasks(null, false);

            //register the write and SimulationChange in Statistics
			if (!m_bDisableStatistics)
			{
				StatisticNodeManager statisticNode = ApplicationModule.Instance.GetNodeManager<StatisticNodeManager>();
				if (statisticNode != null)
				{
					statisticNode.StatisticModule.RegisterWrite();
					statisticNode.StatisticModule.RegisterSimulationChange();
				}
			}
		}

		private bool m_bDisableStatistics;
    }
}