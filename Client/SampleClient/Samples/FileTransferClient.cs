/* ========================================================================
 * Copyright © 2011-2022 Softing Industrial Automation GmbH. 
 * All rights reserved.
 * 
 * The Software is subject to the Softing Industrial Automation GmbH’s 
 * license agreement, which can be found here:
 * https://industrial.softing.com/LA-SDK-en
 *  
 * ======================================================================*/

using System;
using System.Collections.Generic;
using System.Text;
using Opc.Ua;
using Softing.Opc.Ua.Client;
using SampleClient.Helpers;
using System.IO;
using Opc.Ua.Client;
using System.Threading.Tasks;

namespace SampleClient.Samples
{
    public class FileTransferClient 
    {
        #region Private Fields

        // the nodeId of the DownloadFile which is specified on the File Transfer Node Manager
        private const string DownloadNodeID = "ns=9;i=2";

        // the nodeId of the UploadFile from the server
        private const string UploadNodeID = "ns=9;i=24";

        // the nodeId of the ByteString element from the server
        private const string ByteStringNodeID = "ns=9;i=46";

        // the nodeId of the TemporaryFile element from the server
        private const string TemporaryFileNodeID = "ns=9;i=47";

        private string DownloadFilePath = Path.Combine("Files", "DownloadFile.xml");
        private string UploadFilePath = Path.Combine("Files", "UploadClientFile.xml");
        private string ByteStringFilePath = Path.Combine("Files", "ByteStringFile.xml");
        private string DownloadTemporaryFilePath = Path.Combine("Files", "DownloadTemporaryFile.xml");
        private string UploadTemporaryFilePath = Path.Combine("Files", "UploadTemporaryFile.xml");

        private const int ChunkSize = 512;
        private ClientSession m_session;
        private readonly UaApplication m_application;

        private const string SessionName = "FileTransferClient Session";
        private ServerState m_currentServerState = ServerState.Unknown;
        #endregion

        #region Constructor

        public FileTransferClient(UaApplication application)
        {
            m_application = application;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Initialize session
        /// </summary>
        public async Task Initialize()
        {
            if (m_session == null)
            {
                try
                {
                    // create the session object with no security and anonymous login    
                    m_session = m_application.CreateSession(Program.ServerUrl);
                    m_session.SessionName = SessionName;
                    m_session.KeepAlive += Session_KeepAlive;

                    // connect session
                    await m_session.ConnectAsync(false, true).ConfigureAwait(false);
                    Console.WriteLine("Session is connected.");
                }
                catch (Exception ex)
                {
                    Program.PrintException("CreateSession", ex);

                    if (m_session != null)
                    {
                        m_session.Dispose();
                        m_session = null;
                    }

                    return;
                }
            }
        }

        private void Session_KeepAlive(object sender, KeepAliveEventArgs e)
        {
            if (e.CurrentState != m_currentServerState)
            {
                m_currentServerState = e.CurrentState;
                Console.WriteLine("Session KeepAlive Server state changed to: {0}", m_currentServerState);
            }
        }

        /// <summary>
        /// Disconnect the current session
        /// </summary>
        public async Task Disconnect()
        {
            try
            {
                if (m_session != null)
                {
                    await m_session.DisconnectAsync(true).ConfigureAwait(false);
                    m_session.Dispose();
                    m_session = null;

                    Console.WriteLine("Session is disconnected.");
                }
                else
                {
                    Console.WriteLine("Session already disconnected.");
                }
            }
            catch (Exception ex)
            {
                Program.PrintException("DisconnectSession", ex);
            }
        }
        

        /// <summary>
        /// Download the file from the server to the specified path.
        /// </summary>
        public void DownloadFile()
        {
            if (m_session == null)
            {
                Console.WriteLine("The session is not initialized!");
                return;
            }

            try
            {
                NodeId nodeID = new NodeId(DownloadNodeID);
                FileStateHelper fileState = new FileStateHelper(m_session, Path.GetFileName(DownloadFilePath), nodeID);

                // Open the file in Read mode
                StatusCode openStatusCode = fileState.Open(FileStateMode.Read);
                if (StatusCode.IsBad(openStatusCode))
                {
                    Console.WriteLine("'Open' file state status code is: {0}", openStatusCode);
                    return;
                }

                // Send the file in chunks of <chunkSize> bytes
                using (FileStream fs = new FileStream(DownloadFilePath, FileMode.Create))
                {
                    ulong totalSize = fileState.Size;
                    ulong cTotalRead = 0;

                    while (cTotalRead < totalSize)
                    {
                        int cRead = totalSize - cTotalRead > ChunkSize ? ChunkSize : (int) (totalSize - cTotalRead);
                        byte[] buffer;

                        StatusCode readStatusCode = fileState.Read(cRead, out buffer);
                        if (StatusCode.IsBad(readStatusCode))
                        {
                            fileState.Close();
                            Console.WriteLine(string.Format("'Read' file state status code is: {0}", readStatusCode));
                            return;
                        }

                        fs.Write(buffer, 0, cRead);
                        cTotalRead += (ulong) cRead;
                        Console.Write("\rReading {0} bytes of {1} - {2}% complete ", cTotalRead, totalSize,
                            cTotalRead * 100 / totalSize);
                    }

                    Console.WriteLine();
                }

                // Close the file
                StatusCode closeStatusCode = fileState.Close();
                if (StatusCode.IsBad(closeStatusCode))
                {
                    Console.WriteLine("Unable to close the file state.");
                    return;
                }

                Console.WriteLine("The file was downloaded successfully.");
            }
            catch (Exception ex)
            {
                Program.PrintException("DownloadFile", ex);
            }
        }

        /// <summary>
        /// Upload the file to the specified path.
        /// </summary>
        public void UploadFile()
        {
            if (m_session == null)
            {
                Console.WriteLine("The session is not initialized!");
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
                    Console.WriteLine("The file state writable property is false.");
                    return;
                }

                // Open the file in Write and EraseExisting mode
                StatusCode openStatusCode = fileState.Open(FileStateMode.Write | FileStateMode.EraseExisting);
                if (StatusCode.IsBad(openStatusCode))
                {
                    Console.WriteLine(string.Format("'Open' file state status code is: {0}", openStatusCode));
                    return;
                }

                // Send the file content in chunks of chunkSize bytes
                using (FileStream fs = new FileStream(UploadFilePath, FileMode.Open))
                {
                    FileInfo fi = new FileInfo(UploadFilePath);
                    ulong totalSize = (ulong) fi.Length;
                    ulong totalWrite = 0;

                    if (totalSize == 0)
                    {
                        Console.WriteLine("The file can not be written because has the size 0.");

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

                        StatusCode writeStatusCode = fileState.Write(data);
                        if (StatusCode.IsBad(writeStatusCode))
                        {
                            fileState.Close();
                            Console.WriteLine("'Write' file state status code is: {0}", writeStatusCode);
                            return;
                        }

                        totalWrite += (ulong) data.Length;

                        Console.Write("\rWriting {0} bytes of {1} - {2}% complete ", totalWrite, totalSize,
                            totalWrite * 100 / totalSize);
                    }

                    Console.WriteLine();
                }

                // Close the file
                StatusCode closeStatusCode = fileState.Close();
                if (StatusCode.IsBad(closeStatusCode))
                {
                    Console.WriteLine("\nClose status Code is: {0}\n", closeStatusCode);

                    return;
                }

                Console.WriteLine("The file was uploaded successfully.");
            }
            catch (Exception ex)
            {
                Program.PrintException("UploadFile", ex);
            }
        }

        /// <summary>
        /// Download the ByteString to the specified path.
        /// </summary>
        public void ReadByteString()
        {
            if (m_session == null)
            {
                Console.WriteLine("The session is not initialized!");
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
            catch (Exception ex)
            {
                Program.PrintException("Download ByteString", ex);
            }
        }

        /// <summary>
        /// Read(download) temporary file content from the server node 
        /// </summary>
        public void DownloadTemporaryFile()
        {
            if (m_session == null)
            {
                Console.WriteLine("The session is not initialized!");
                return;
            }

            try
            {
                NodeId nodeID = new NodeId(TemporaryFileNodeID);
                string filename = Path.GetFileName(DownloadTemporaryFilePath);
                TemporaryFileTransferStateHelper tmpFileTransferState =
                    new TemporaryFileTransferStateHelper(m_session, filename, nodeID);

                StatusCode readFileStatusCode = tmpFileTransferState.GenerateFileForRead(null);
                if (StatusCode.IsBad(readFileStatusCode))
                {
                    Console.WriteLine("The server could not generate and open a new temporary file");
                    return;
                }

                FileStateHelper fileState = new FileStateHelper(m_session,
                    tmpFileTransferState.Filename,
                    tmpFileTransferState.FileNodeID);
                if (fileState != null)
                {
                    ulong totalSize = fileState.Size;
                    if (totalSize == 0)
                    {
                        Console.WriteLine("The file to be written because has the size 0.");
                        return;
                    }

                    // Copy the file in chunks of <chunkSize> bytes from server
                    using (FileStream fs = new FileStream(DownloadTemporaryFilePath, FileMode.Create))
                    {
                        ulong cTotalRead = 0;
                        while (cTotalRead < totalSize)
                        {
                            int cRead = totalSize - cTotalRead > ChunkSize ? ChunkSize : (int) (totalSize - cTotalRead);
                            byte[] buffer;

                            StatusCode readStatusCode = fileState.Read(cRead, out buffer);
                            if (StatusCode.IsBad(readStatusCode))
                            {
                                Console.WriteLine(string.Format("'Read' file state status code is: {0}", readStatusCode));
                                fileState.Close();
                                return;
                            }

                            fs.Write(buffer, 0, cRead);
                            cTotalRead += (ulong) cRead;
                            Console.Write("\rReading {0} bytes of {1} - {2}% complete ", cTotalRead, totalSize,
                                cTotalRead * 100 / totalSize);
                        }
                    }

                    Console.WriteLine();

                    // Close the file
                    StatusCode closeStatusCode = fileState.Close();
                    if (StatusCode.IsBad(closeStatusCode))
                    {
                        Console.WriteLine("Unable to close the temporary file.");
                        return;
                    }

                    Console.WriteLine("The temporary file was downloaded successfully.");
                }
                else
                {
                    Console.WriteLine("The temporary file helper initialization failed.");
                }
            }
            catch (Exception ex)
            {
                Program.PrintException("Download temporary file", ex);
            }
        }

        /// <summary>
        /// Write(upload) a client file content into temporary file on the server
        /// </summary>
        public void UploadTemporaryFile()
        {
            if (m_session == null)
            {
                Console.WriteLine("The session is not initialized!");
                return;
            }

            try
            {
                NodeId nodeID = new NodeId(TemporaryFileNodeID);
                string filename = Path.GetFileName(UploadTemporaryFilePath);
                TemporaryFileTransferStateHelper tmpFileTransferState =
                    new TemporaryFileTransferStateHelper(m_session, filename, nodeID);

                StatusCode writeFileStatusCode = tmpFileTransferState.GenerateFileForWrite(null);
                if (StatusCode.IsBad(writeFileStatusCode))
                {
                    Console.WriteLine("The server could not generate and open a new temporary file");
                    return;
                }

                FileStateHelper fileState = new FileStateHelper(m_session,
                    tmpFileTransferState.Filename,
                    tmpFileTransferState.FileNodeID);
                if (fileState != null)
                {
                    // Send the file content in chunks of chunkSize bytes
                    using (FileStream fs = new FileStream(UploadTemporaryFilePath, FileMode.Open))
                    {
                        FileInfo fi = new FileInfo(UploadTemporaryFilePath);
                        ulong totalSize = (ulong)fi.Length;
                        ulong totalWrite = 0;

                        if (totalSize == 0)
                        {
                            Console.WriteLine("The file to be written because has the size 0.");
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

                            StatusCode writeStatusCode = fileState.Write(data);
                            if (StatusCode.IsBad(writeStatusCode))
                            {
                                Console.WriteLine("'Write' file state status code is: {0}", writeStatusCode);
                                fileState.Close();
                                return;
                            }

                            totalWrite += (ulong) data.Length;

                            Console.Write("\rWriting {0} bytes of {1} - {2}% complete ", totalWrite, totalSize,
                                totalWrite * 100 / totalSize);
                        }
                    }

                    Console.WriteLine();

                    // Close and remove the file
                    StatusCode closeStatusCode = tmpFileTransferState.CloseAndCommit();
                    if (StatusCode.IsBad(closeStatusCode))
                    {
                        Console.WriteLine("\nClose status code is: {0}\n", closeStatusCode);
                        return;
                    }

                    Console.WriteLine("The temporary file was uploaded successfully.");
                }
                else
                {
                    Console.WriteLine("The temporary file helper initialization failed.");
                }
            }
            catch (Exception ex)
            {
                Program.PrintException("Upload temporary file", ex);
            }
            
        }
        #endregion
    }
}
