using Opc.Ua;
using System;
using TestServer.SimulationModule;

namespace TestServer
{
    public class VariableCommand : DataItemState
    {
        #region Constructors

        public VariableCommand(NodeState parent) : base(parent)
        {
        }

        #endregion

        public SimulationModuleCommand ServerCommand
        {
            get;
            set;
        }

        protected override ServiceResult ReadValueAttribute(ISystemContext context, NumericRange indexRange, QualifiedName dataEncoding, ref object value, ref DateTime sourceTimestamp)
        {
            return base.ReadValueAttribute(context, indexRange, dataEncoding, ref value, ref sourceTimestamp);
        }

        protected override ServiceResult WriteValueAttribute(ISystemContext context, NumericRange indexRange, object value, StatusCode statusCode, DateTime sourceTimestamp)
        {
            ServiceResult result = base.WriteValueAttribute(context, indexRange, value, statusCode, sourceTimestamp);
            ServiceResult executeResult;
            try
            {
                executeResult = ServerCommand.Execute();
            }
            catch
            {
                executeResult = StatusCodes.Bad;
            }

            if (ServiceResult.IsBad(executeResult))
            {
                return StatusCodes.Bad;
            }

            return result;
        }
    }
}