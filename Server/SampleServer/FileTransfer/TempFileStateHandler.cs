using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using Opc.Ua;
using Opc.Ua.Server;

namespace SampleServer.FileTransfer
{
    /// <summary>
    /// TemporaryFileTransferState handler class
    /// </summary>
    internal class TempFileStateHandler : FileStateHandler
    {
        public TempFileStateHandler(string filePath) : base(filePath)
        {
        }

        /// <summary>
        /// Close method callback
        /// </summary>
        /// <param name="context"></param>
        /// <param name="method"></param>
        /// <param name="objectId"></param>
        /// <param name="fileHandle"></param>
        /// <returns></returns>
        protected override ServiceResult OnCloseMethodCall(
            ISystemContext context,
            MethodState method,
            NodeId objectId,
            uint fileHandle)
        {
            try
            {
                base.OnCloseMethodCall(context, method, objectId, fileHandle);
                if (File.Exists(base.FilePath))
                {
                    File.Delete(base.FilePath);
                }

                // todo: add remove temporary nodes

                return StatusCodes.Good;
            }
            catch (Exception e)
            {
                throw new ServiceResultException(StatusCodes.BadUnexpectedError, e.Message);
            }
        }

        public FileStream GetTmpFileStream(uint fileHandle)
        {
            return base.GetFileStream(fileHandle);
        }

    }
}
