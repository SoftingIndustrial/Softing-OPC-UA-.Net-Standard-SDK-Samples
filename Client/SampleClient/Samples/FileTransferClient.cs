using System;
using System.Collections.Generic;
using System.Text;
using Opc.Ua;
using Softing.Opc.Ua.Client;
using SampleClient.Helpers;
using System.IO;

namespace SampleClient.Samples
{
    public class FileTransferClient : IDisposable
    {
        #region Private Fields

        // the nodeId of the DownloadFile which is specified on the FileTransferServer
        private const string DownloadNodeID = "ns=9;i=2";

        // the nodeId of the UploadFile from the server
        private const string UploadNodeID = "ns=9;i=23";
        
        // the nodeId of the ByteString element from the server
        private const string ByteStringNodeID = "ns=9;i=44";

        private const string DownloadFilePath = @"Files\DownloadFile.xml";
        private const string UploadFilePath = @"Files\UploadClientFile.xml";
        private const string ByteStringFilePath = @"Files\ByteStringFile.xml";

        private const int ChunkSize = 512;
        private ClientSession m_session;
        private readonly UaApplication m_application;

        #endregion

        #region Constructor

        public FileTransferClient(UaApplication application)
        {
            m_application = application;
        }

        #endregion

        #region IDisposable implementation
        /// <summary>
        /// Dispose the opened session or/and file handlers
        /// </summary>
        public void Dispose()
        {
            if (m_session != null)
            {
                DisconnectSession();
            }
        }
        #endregion

        #region Public Methods

        /// <summary>
        /// Creates a new session and connect to the server.
        /// </summary>
        public void CreateSession()
        {
            if (m_session != null)
            {
                Console.WriteLine("Session already created.");
                return;
            }

            m_session = m_application.CreateSession(Program.ServerUrl);
            m_session.SessionName = "Softing FileTransfer Sample Client";
            
            try
            {
                // connect session
                m_session.Connect(false, true);
                Console.WriteLine("Session is connected.");
            }
            catch (Exception ex)
            {
                Console.WriteLine(String.Format("CreateSession Error: {0}", ex.Message));
            }
        }

        /// <summary>
        /// Disconnect the current session.
        /// </summary>
        public void DisconnectSession()
        {
            if (m_session == null)
            {
                Console.WriteLine("Session is not created, please use \"1\" command");
                return;
            }

            try
            {
                m_session.Disconnect(false);
                Console.WriteLine("Session is disconnected.");
            }
            catch (Exception ex)
            {
                Console.WriteLine(String.Format("DisconnectSession Error: {0}", ex.Message));
            }

            m_session.Dispose();
            m_session = null;
        }

        /// <summary>
        /// Download the file from the server to the specified path.
        /// </summary>
        public void DownloadFile()
        {
            if (m_session == null)
            {
                Console.WriteLine("Session is not created, please use \"1\" command");
                return;
            }

            try
            {
                NodeId nodeID = new NodeId(DownloadNodeID);
                FileStateHelper fileState = new FileStateHelper(m_session, Path.GetFileName(DownloadFilePath), nodeID);
                
                // Open the file in Read mode
                StatusCode statusCode = fileState.Open(1);
                if (StatusCode.IsBad(statusCode))
                {
                    Console.WriteLine("Unable to open the file in read mode.");
                    return;
                }

                // Send the file in chunks of <chunkSize> bytes
                using (FileStream fs = new FileStream(DownloadFilePath, FileMode.Create))
                {
                    ulong totalSize = fileState.Size;
                    ulong cTotalRead = 0;

                    while (cTotalRead < totalSize)
                    {
                        int cRead = totalSize - cTotalRead > ChunkSize ? ChunkSize : (int)(totalSize - cTotalRead);
                        byte[] buffer;

                        statusCode = fileState.Read(cRead, out buffer);

                        if (StatusCode.IsBad(statusCode))
                        {
                            fileState.Close();
                            Console.WriteLine(string.Format("\nStatus Code is: {0}\n", statusCode));

                            return;
                        }

                        fs.Write(buffer, 0, cRead);
                        cTotalRead += (ulong)cRead;
                        Console.Write("\rReading {0} bytes of {1} - {2}% complete", cTotalRead, totalSize, cTotalRead * 100 / totalSize);
                    }

                    Console.WriteLine();
                }

                // Close the file
                StatusCode closeStatusCode = fileState.Close();
                if (StatusCode.IsBad(statusCode))
                {
                    Console.WriteLine("Unable to close the file.");
                    return;
                }
                Console.WriteLine("The File was downloaded successfully.");
            }
            catch (Exception e)
            {
                string logMessage = String.Format("Download File Error : {0}.", e.Message);
                Console.WriteLine(logMessage);
                Console.WriteLine("DownloadFile error..." + e.Message);
            }
        }

        /// <summary>
        /// Upload the file to the specified path.
        /// </summary>
        public void UploadFile()
        {
            if (m_session == null)
            {
                Console.WriteLine("Session is not created, please use \"1\" command");
                return;
            }

            try
            {
                string fileName = Path.GetFileName(UploadFilePath);
                if (!File.Exists(UploadFilePath))
                {
                    throw new Exception(string.Format("The file {0} cannot be found.", fileName));
                }

                NodeId uploadNodeID = new NodeId(UploadNodeID);
                FileStateHelper fileState = new FileStateHelper(m_session, fileName, uploadNodeID);
                
                if (!fileState.Writable)
                {
                    Console.WriteLine("The file writable property is false.");

                    return;
                }

                // Open the file in Write and EraseExisting mode
                StatusCode readStatusCode = fileState.Open(2 | 4);
                if (StatusCode.IsBad(readStatusCode))
                {
                    Console.WriteLine(string.Format("\nStatus Code is: {0}\n", readStatusCode));
                    return;
                }

                // Send the file content in chunks of chunkSize bytes
                using (FileStream fs = new FileStream(UploadFilePath, FileMode.Open))
                {
                    FileInfo fi = new FileInfo(UploadFilePath);
                    ulong totalSize = (ulong)fi.Length;
                    ulong totalWrite = 0;

                    if (totalSize == 0)
                    {
                        Console.WriteLine("The file to be written becacuse has the size 0.");

                        return;
                    }


                    byte[] buffer = new byte[ChunkSize];
                    int cRead;

                    while ((cRead = fs.Read(buffer, 0, ChunkSize)) > 0)
                    {
                        // if the amount of available bytes is less than the chunkSize requested
                        // we need to strip the buffer
                        byte[] data;

                        if (cRead < ChunkSize)
                        {
                            byte[] readData = new byte[cRead];
                            Array.Copy(buffer, readData, cRead);
                            data = readData;
                        }
                        else
                        {
                            data = buffer;
                        }

                        StatusCode statusCode = fileState.Write(data);

                        if (StatusCode.IsBad(statusCode))
                        {
                            fileState.Close();
                            Console.WriteLine("\nStatus Code is: {0}\n", statusCode);

                            return;
                        }

                        totalWrite += (ulong)data.Length;

                        Console.Write("\rWriting {0} bytes of {1} - {2}% complete", totalWrite, totalSize, totalWrite * 100 / totalSize);
                    }

                    Console.WriteLine();
                }

                // Close the file
                StatusCode closeStatusCode = fileState.Close();

                if (StatusCode.IsBad(closeStatusCode))
                {
                    Console.WriteLine("\nStatus Code is: {0}\n", closeStatusCode);

                    return;
                }
                
                Console.WriteLine("The File was uploaded with success.");
            }
            catch (Exception e)
            {
                string logMessage = String.Format("Upload File Error : {0}.", e.Message);
                Console.WriteLine(logMessage);
            }
        }

        /// <summary>
        /// Download the ByteString to the specified path.
        /// </summary>
        public void ReadByteString()
        {
            if (m_session == null)
            {
                Console.WriteLine("Session is not created, please use \"1\" command");
                return;
            }

            try
            {
                ReadValueId readValueId = new ReadValueId();
                readValueId.NodeId = new NodeId(ByteStringNodeID);
                readValueId.AttributeId = Attributes.Value;

                using (FileStream fs = new FileStream(ByteStringFilePath, FileMode.Create))
                {
                    DataValueEx dataValue = m_session.Read(readValueId);
                    byte[] binaryData = dataValue.Value as byte[];

                    fs.Write(binaryData, 0, binaryData.Length);

                    Console.WriteLine("{0} bytes has been read.", binaryData.Length);
                }

                Console.WriteLine("The ByteString was downloaded successfully.");
            }
            catch (Exception e)
            {
                string logMessage = String.Format("Download ByteString Error : {0}.", e.Message);
                Console.WriteLine(logMessage);
                Console.WriteLine("Download ByteString error..." + e.Message);
            }
        }

        #endregion

        #region Private Methods
        #endregion
    }
}
