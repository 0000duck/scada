/*
 * Copyright 2014 Mikhail Shiryaev
 * 
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 * 
 *     http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 * 
 * 
 * Product  : Rapid SCADA
 * Module   : ScadaData
 * Summary  : Communication with SCADA-Server
 * 
 * Author   : Mikhail Shiryaev
 * Created  : 2006
 * Modified : 2014
 */

#undef DETAILED_LOG // �������� � ������ ��������� ���������� �� ������ ������� �� SCADA-��������

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Data;
using Scada.Data;
using Utils;

namespace Scada.Client
{
    /// <summary>
    /// Communication with SCADA-Server
    /// <para>����� ������� �� SCADA-��������</para>
    /// </summary>
    public class ServerComm
    {
        /// <summary>
        /// ��������� ������ ������� �� SCADA-��������
        /// </summary>
        public enum CommStates
        {
            /// <summary>
            /// ���������� �� �����������
            /// </summary>
            Disconnected,
            /// <summary>
            /// ���������� �����������
            /// </summary>
            Connected,
            /// <summary>
            /// ���������� ����������� � ��������� ������������
            /// </summary>
            Authorized,
            /// <summary>
            /// SCADA-������ �� �����
            /// </summary>
            NotReady,
            /// <summary>
            /// ������ ������ �������
            /// </summary>
            Error,
            /// <summary>
            /// �������� ������ �� ������� ��� ������
            /// </summary>
            WaitResponse
        }

        /// <summary>
        /// ���� �������������
        /// </summary>
        public enum Roles
        {
            /// <summary>
            /// ��������
            /// </summary>
            Disabled = 0x00,
            /// <summary>
            /// �������������
            /// </summary>
            Admin = 0x01,
            /// <summary>
            /// ���������
            /// </summary>
            Dispatcher = 0x02,
            /// <summary>
            /// �����
            /// </summary>
            Guest = 0x03,
            /// <summary>
            /// ����������
            /// </summary>
            App = 0x04,
            /// <summary>
            /// ������������� ����
            /// </summary>
            /// <remarks>����������� ������������� ������������� ���� ����� 0x0B</remarks>
            Custom = 0x0B,
            /// <summary>
            /// ������ (�������� ��� ������������ ��� ������)
            /// </summary>
            Err = 0xFF
        }

        /// <summary>
        /// ���������� �� SCADA-�������
        /// </summary>
        public enum Dirs
        {
            /// <summary>
            /// ���������� �������� �����
            /// </summary>
            Cur = 0x01,
            /// <summary>
            /// ���������� ������� ������
            /// </summary>
            Hour = 0x02,
            /// <summary>
            /// ���������� �������� ������
            /// </summary>
            Min = 0x03,
            /// <summary>
            /// ���������� �������
            /// </summary>
            Events = 0x04,
            /// <summary>
            /// ���������� ���� ������������ � ������� DAT
            /// </summary>
            BaseDAT = 0x05,
            /// <summary>
            /// ���������� ���������� (������, ���� � �.�.)
            /// </summary>
            Itf = 0x06
        }

        /// <summary>
        /// ������� ������ � ������ ������
        /// </summary>
        public delegate void WriteToLogDelegate(string text);


        /// <summary>
        /// ������� �������� ������ �� TCP, ��
        /// </summary>
        protected const int TcpSendTimeout = 1000;
        /// <summary>
        /// ������� ����� ������ �� TCP, ��
        /// </summary>
        protected const int TcpReceiveTimeout = 5000;
        /// <summary>
        /// �������� ��������� ������� ����������
        /// </summary>
        protected readonly TimeSpan ConnectSpan = TimeSpan.FromSeconds(10);
        /// <summary>
        /// �������� �������� ���������� ���� ������� ��������� �������
        /// </summary>
        protected readonly TimeSpan PingSpan = TimeSpan.FromSeconds(30);


        /// <summary>
        /// ��������� ���������� �� SCADA-��������
        /// </summary>
        protected CommSettings commSettings;
        /// <summary>
        /// ������ ������
        /// </summary>
        protected Log log;
        /// <summary>
        /// ����� ������ � ������ ������
        /// </summary>
        protected WriteToLogDelegate writeToLog { get; set; }
        /// <summary>
        /// TCP-������ ��� ������ ������� �� SCADA-��������
        /// </summary>
        protected TcpClient tcpClient;
        /// <summary>
        /// ����� ������ TCP-�������
        /// </summary>
        protected NetworkStream netStream;
        /// <summary>
        /// ������ ��� ������������� ������ ������� �� SCADA-��������
        /// </summary>
        protected object tcpLock;
        /// <summary>
        /// ��������� ������ ������� �� SCADA-��������
        /// </summary>
        protected CommStates commState;
        /// <summary>
        /// ������ SCADA-�������
        /// </summary>
        protected string serverVersion;
        /// <summary>
        /// ����� ��������� ������ ������ �������������� ����������
        /// </summary>
        protected DateTime restConnSuccDT;
        /// <summary>
        /// ����� ���������� ������ ������ �������������� ����������
        /// </summary>
        protected DateTime restConnErrDT;
        /// <summary>
        /// ��������� �� ������
        /// </summary>
        protected string errMsg;


        /// <summary>
        /// �����������
        /// </summary>
        protected ServerComm()
        {
            tcpClient = null;
            netStream = null;
            tcpLock = new object();
            commState = CommStates.Disconnected;
            serverVersion = "";
            restConnSuccDT = DateTime.MinValue;
            restConnErrDT = DateTime.MinValue;
            errMsg = "";
        }

        /// <summary>
        /// ����������� � ���������� �������� ���������� �� SCADA-��������
        /// </summary>
        /// <remarks>������������ ������ ������ Scada.Client.AppData.Log</remarks>
        public ServerComm(CommSettings commSettings)
            : this()
        {
            this.commSettings = commSettings;
            this.log = null;
            this.writeToLog = null;
        }

        /// <summary>
        /// ����������� � ���������� �������� ���������� �� SCADA-�������� � ������� ������
        /// </summary>
        public ServerComm(CommSettings commSettings, Log log)
            : this()
        {
            this.commSettings = commSettings;
            this.log = log;
            this.writeToLog = null;
        }

        /// <summary>
        /// ����������� � ���������� �������� ���������� �� SCADA-�������� � ������ ������ � ������ ������
        /// </summary>
        public ServerComm(CommSettings commSettings, WriteToLogDelegate writeToLog)
            : this()
        {
            this.commSettings = commSettings;
            this.log = null;
            this.writeToLog = writeToLog;
        }


        /// <summary>
        /// �������� ��������� ���������� �� SCADA-��������
        /// </summary>
        public CommSettings CommSettings
        {
            get
            {
                return commSettings;
            }
        }

        /// <summary>
        /// �������� ��������� ������ ������� �� SCADA-��������
        /// </summary>
        public CommStates CommState
        {
            get
            {
                return commState;
            }
        }

        /// <summary>
        /// �������� �������� ��������� ������ ������� �� SCADA-��������
        /// </summary>
        public string CommStateDescr
        {
            get
            {
                StringBuilder stateDescr = new StringBuilder();
                if (serverVersion != "")
                    stateDescr.Append(Localization.UseRussian ? "������ " : "version ").
                        Append(serverVersion).Append(", ");

                switch (commState)
                {
                    case CommStates.Disconnected:
                        stateDescr.Append(Localization.UseRussian ? "���������� �� �����������" : 
                            "not connected");
                        break;
                    case CommStates.Connected:
                        stateDescr.Append(Localization.UseRussian ? "���������� �����������" : 
                            "connected");
                        break;
                    case CommStates.Authorized:
                        stateDescr.Append(Localization.UseRussian ? "����������� �������" : 
                            "authentication is successful");
                        break;
                    case CommStates.NotReady:
                        stateDescr.Append(Localization.UseRussian ? "SCADA-������ �� �����" : 
                            "SCADA-Server isn't ready");
                        break;
                    case CommStates.Error:
                        stateDescr.Append(Localization.UseRussian ? "������ ������ �������" : 
                            "communication error");
                        break;
                    case CommStates.WaitResponse:
                        stateDescr.Append(Localization.UseRussian ? "�������� ������" : 
                            "waiting for response");
                        break;
                }

                if (errMsg != "")
                    stateDescr.Append(" - ").Append(errMsg);

                return stateDescr.ToString();
            }
        }

        /// <summary>
        /// �������� ��������� �� ������
        /// </summary>
        public string ErrMsg
        {
            get
            {
                return errMsg;
            }
        }


        /// <summary>
        /// ��������� ������ ������ ��� �������� ������� ��� ����� �� SCADA-��������
        /// </summary>
        protected bool CheckDataFormat(byte[] buffer, int cmdNum)
        {
            return CheckDataFormat(buffer, cmdNum, buffer.Length);
        }

        /// <summary>
        /// ��������� ������ ������ ��� �������� ������� ��� ����� �� SCADA-��������, ������ ������������ ����� ������
        /// </summary>
        protected bool CheckDataFormat(byte[] buffer, int cmdNum, int bufLen)
        {
            return bufLen >= 3 && buffer[0] + 256 * buffer[1] == bufLen && buffer[2] == cmdNum;
        }

        /// <summary>
        /// �������� ��������� ����������� ���������� �� SCADA-�������
        /// </summary>
        protected string DirToString(Dirs directory)
        {
            switch (directory)
            {
                case Dirs.Cur:
                    return "[Srez]\\";
                case Dirs.Hour:
                    return "[Hr]\\";
                case Dirs.Min:
                    return "[Min]\\";
                case Dirs.Events:
                    return "[Ev]\\";
                case Dirs.BaseDAT:
                    return "[Base]\\";
                case Dirs.Itf:
                    return "[Itf]\\";
                default:
                    return "";
            }
        }

        /// <summary>
        /// �������� �������� � ������ ������
        /// </summary>
        protected void WriteAction(string actText, Log.ActTypes actType)
        {
            if (log != null)
                log.WriteAction(actText, actType);
            else if (writeToLog != null)
                writeToLog(actText);
        }

        /// <summary>
        /// ���������� ���������� �� SCADA-�������� � ���������� �����������
        /// </summary>
        protected bool Connect()
        {
            try
            {
                commState = CommStates.Disconnected;
                WriteAction(string.Format(Localization.UseRussian ? 
                    "��������� ���������� �� SCADA-�������� \"{0}\"" : 
                    "Connect to SCADA-Server \"{0}\"", commSettings.ServerHost), Log.ActTypes.Action);

                // ����������� IP-������, ���� �� ������ � ������������ ���������
                IPAddress ipAddress = null;
                try { ipAddress = IPAddress.Parse(commSettings.ServerHost); }
                catch { }

                // ��������, ��������� � ������� ��������� ����������
                tcpClient = new TcpClient();
                tcpClient.NoDelay = true;            // sends data immediately upon calling NetworkStream.Write
                tcpClient.ReceiveBufferSize = 16384; // 16 ��
                tcpClient.SendBufferSize = 8192;     // 8 ��, ������ �� ���������
                tcpClient.SendTimeout = TcpSendTimeout;
                tcpClient.ReceiveTimeout = TcpReceiveTimeout;

                if (ipAddress == null)
                    tcpClient.Connect(commSettings.ServerHost, commSettings.ServerPort);
                else
                    tcpClient.Connect(ipAddress, commSettings.ServerPort);

                netStream = tcpClient.GetStream();

                // ��������� ������ SCADA-�������
                byte[] buf = new byte[5];
                int bytesRead = netStream.Read(buf, 0, 5);

                // ��������� ��������� ������ ������
                if (bytesRead == buf.Length && CheckDataFormat(buf, 0x00))
                {
                    commState = CommStates.Connected;
                    serverVersion = buf[4] + "." + buf[3]; 

                    // ������ ������������ ����� � ������ ������������, ��� ����
                    byte userLen = (byte)commSettings.ServerUser.Length;
                    byte pwdLen = (byte)commSettings.ServerPwd.Length;
                    buf = new byte[5 + userLen + pwdLen];

                    buf[0] = (byte)(buf.Length % 256);
                    buf[1] = (byte)(buf.Length / 256);
                    buf[2] = 0x01;
                    buf[3] = userLen;
                    Array.Copy(Encoding.Default.GetBytes(commSettings.ServerUser), 0, buf, 4, userLen);
                    buf[4 + userLen] = pwdLen;
                    Array.Copy(Encoding.Default.GetBytes(commSettings.ServerPwd), 0, buf, 5 + userLen, pwdLen);

                    netStream.Write(buf, 0, buf.Length);

                    // ���� ����������
                    buf = new byte[4];
                    bytesRead = netStream.Read(buf, 0, 4);

                    // ��������� ��������� ������
                    if (bytesRead == buf.Length && CheckDataFormat(buf, 0x01))
                    {
                        Roles role = (Roles)buf[3];

                        if (role == Roles.App)
                        {
                            commState = CommStates.Authorized;
                        }
                        else if (role < Roles.Err)
                        {
                            errMsg = Localization.UseRussian ? "������������ ���� ��� ���������� �� SCADA-��������" :
                                "Insufficient rights to connect to SCADA-Server";
                            WriteAction(errMsg, Log.ActTypes.Error);
                            commState = CommStates.Error;
                        }
                        else // role == Roles.Err
                        {
                            errMsg = Localization.UseRussian ? "�������� ��� ������������ ��� ������" :
                                "User name or password is incorrect";
                            WriteAction(errMsg, Log.ActTypes.Error);
                            commState = CommStates.Error;
                        }
                    }
                    else
                    {
                        errMsg = Localization.UseRussian ? 
                            "�������� ������ ������ SCADA-������� �� ������ ������������ ����� � ������" :
                            "Incorrect SCADA-Server response to check user name and password request";
                        WriteAction(errMsg, Log.ActTypes.Error);
                        commState = CommStates.Error;
                    }
                }
                else
                {
                    errMsg = Localization.UseRussian ? "�������� ������ ������ SCADA-������� �� ������ ������" :
                        "Incorrect SCADA-Server response to version request";
                    WriteAction(errMsg, Log.ActTypes.Error);
                    commState = CommStates.Error;
                }
            }
            catch (Exception ex)
            {
                errMsg = (Localization.UseRussian ? "������ ��� ��������� ���������� �� SCADA-��������: " : 
                    "Error connecting to SCADA-Server: ") + ex.Message;
                WriteAction(errMsg, Log.ActTypes.Exception);
            }

            // ������� ����������
            if (commState == CommStates.Authorized)
            {
                return true;
            }
            else
            {
                Disconnect();
                return false;
            }
        }

        /// <summary>
        /// ��������� ���������� �� SCADA-��������
        /// </summary>
        protected void Disconnect()
        {
            try
            {
                commState = CommStates.Disconnected;
                serverVersion = "";

                if (tcpClient != null)
                {
                    WriteAction(Localization.UseRussian ? "������ ���������� �� SCADA-��������" : 
                        "Disconnect from SCADA-Server", Log.ActTypes.Action);

                    if (netStream != null)
                    {
                        // ������� (��� ����������� ������������) � �������� ������ ������ TCP-�������
                        ClearNetStream();
                        netStream.Close();
                        netStream = null;
                    }

                    tcpClient.Close();
                    tcpClient = null;
                }
            }
            catch (Exception ex)
            {
                errMsg = (Localization.UseRussian ? "������ ��� ������� ���������� �� SCADA-��������: " : 
                    "Error disconnecting from SCADA-Server: ") + ex.Message;
                WriteAction(errMsg, Log.ActTypes.Exception);
            }
        }

        /// <summary>
        /// ������� ���������� �� ������ ������ TCP-�������
        /// </summary>
        /// <remarks>����� ������������ ��� ���������� "��������" ������ ������</remarks>
        protected int ReadNetStream(byte[] buffer, int offset, int size)
        {
            int bytesRead = 0;
            DateTime startReadDT = DateTime.Now;

            do
            {
                bytesRead += netStream.Read(buffer, bytesRead + offset, size - bytesRead);
            } while (bytesRead < size && (DateTime.Now - startReadDT).TotalMilliseconds <= TcpReceiveTimeout);

            return bytesRead;
        }

        /// <summary>
        /// �������� ����� ������ TCP-�������
        /// </summary>
        protected void ClearNetStream()
        {
            try
            {
                if (netStream != null && netStream.DataAvailable)
                {
                    // ���������� ���������� ������ �� ������, �� �� ����� 100 ��
                    byte[] buf = new byte[1024];
                    int n = 0;
                    while (netStream.DataAvailable && ++n <= 100)
                        try { netStream.Read(buf, 0, 1024); }
                        catch { }
                }
            }
            catch (Exception ex)
            {
                errMsg = (Localization.UseRussian ? "������ ��� ������� �������� ������: " : 
                    "Error clear network stream: ") + ex.Message;
                WriteAction(errMsg, Log.ActTypes.Exception);
            }
        }

        /// <summary>
        /// ������������ ���������� �� SCADA-�������� � ���������� ����������� ��� �������������
        /// </summary>
        protected bool RestoreConnection()
        {
            bool connectNeeded = false; // ��������� ��������� ����������
            DateTime now = DateTime.Now;

            if (commState >= CommStates.Authorized)
            {
                if (now - restConnSuccDT > PingSpan)
                {
                    // �������� ����������
                    try
                    {
                        WriteAction(Localization.UseRussian ? "������ ��������� SCADA-�������" : 
                            "Request SCADA-Server state", Log.ActTypes.Action);
                        commState = CommStates.WaitResponse;

                        // ������ ��������� SCADA-������� (ping)
                        byte[] buf = new byte[3];
                        buf[0] = 0x03;
                        buf[1] = 0x00;
                        buf[2] = 0x02;
                        netStream.Write(buf, 0, 3);

                        // ���� ����������
                        buf = new byte[4];
                        netStream.Read(buf, 0, 4);

                        // ��������� ����������
                        if (CheckDataFormat(buf, 0x02))
                        {
                            commState = buf[3] > 0 ? CommStates.Authorized : CommStates.NotReady;
                        }
                        else
                        {
                            errMsg = Localization.UseRussian ? 
                                "�������� ������ ������ SCADA-������� �� ������ ���������" : 
                                "Incorrect SCADA-Server response to state request";
                            WriteAction(errMsg, Log.ActTypes.Error);
                            commState = CommStates.Error;
                            connectNeeded = true;
                        }
                    }
                    catch
                    {
                        connectNeeded = true;
                    }
                }
            }
            else if (now - restConnErrDT > ConnectSpan)
            {
                connectNeeded = true;
            }

            // ���������� ��� �������������
            if (connectNeeded)
            {
                if (tcpClient != null) 
                    Disconnect();

                if (Connect())
                {
                    restConnSuccDT = now;
                    restConnErrDT = DateTime.MinValue;
                    return true;
                }
                else
                {
                    restConnSuccDT = DateTime.MinValue;
                    restConnErrDT = now;
                    return false;
                }
            }
            else
            {
                ClearNetStream(); // ������� ������ ������ TCP-�������

                if (commState >= CommStates.Authorized)
                {
                    restConnSuccDT = now;
                    return true;
                }
                else
                {
                    errMsg = Localization.UseRussian ? "���������� ����������� �� SCADA-��������. ��������� �������." :
                        "Unable to connect to SCADA-Server. Try again.";
                    return false;
                }
            }
        }

        /// <summary>
        /// ������������ �������� �������� ����� ������ ����� TCP �� ���������
        /// </summary>
        protected void RestoreReceiveTimeout()
        {
            try 
            {
                if (tcpClient.ReceiveTimeout != TcpReceiveTimeout)
                    tcpClient.ReceiveTimeout = TcpReceiveTimeout;
            }
            catch { }
        }

        /// <summary>
        /// ������� ���� �� SCADA-�������
        /// </summary>
        protected bool ReceiveFile(Dirs dir, string fileName, Stream inStream)
        {
            bool result = false;
            string filePath = DirToString(dir) + fileName;

            try
            {
#if DETAILED_LOG
                WriteAction(string.Format(Localization.UseRussian ? "���� ����� {0} �� SCADA-�������" : 
                    "Receive file {0} from SCADA-Server", filePath), Log.ActTypes.Action);
#endif

                commState = CommStates.WaitResponse;
                tcpClient.ReceiveTimeout = commSettings.ServerTimeout;

                const int dataSize = 10240; // ������ ������������� ������ 10 ��
                const byte dataSizeL = dataSize % 256;
                const byte dataSizeH = dataSize / 256;

                byte[] buf = new byte[6 + dataSize]; // ����� ������������ � ���������� ������
                bool open = true;  // ����������� �������� �����
                bool stop = false; // ������� ���������� ����� ������

                while (!stop)
                {
                    if (open)
                    {
                        // �������� ������� �������� ����� � ������ ������
                        byte fileNameLen = (byte)fileName.Length;
                        int cmdLen = 7 + fileNameLen;
                        buf[0] = (byte)(cmdLen % 256);
                        buf[1] = (byte)(cmdLen / 256);
                        buf[2] = 0x08;
                        buf[3] = (byte)dir;
                        buf[4] = fileNameLen;
                        Array.Copy(Encoding.Default.GetBytes(fileName), 0, buf, 5, fileNameLen);
                        buf[cmdLen - 2] = dataSizeL;
                        buf[cmdLen - 1] = dataSizeH;
                        netStream.Write(buf, 0, cmdLen);
                    }
                    else
                    {
                        // �������� ������� ������ ������ �� �����
                        buf[0] = 0x05;
                        buf[1] = 0x00;
                        buf[2] = 0x0A;
                        buf[3] = dataSizeL;
                        buf[4] = dataSizeH;
                        netStream.Write(buf, 0, 5);
                    }

                    // ���� ���������� �������� ����� � ��������� ������
                    byte cmdNum = buf[2];
                    int headerLen = open ? 6 : 5;
                    int bytesRead = netStream.Read(buf, 0, headerLen);
                    int dataSizeRead = 0; // ������ ��������� �� ����� ������                    

                    if (bytesRead == headerLen)
                    {
                        dataSizeRead = buf[headerLen - 2] + 256 * buf[headerLen - 1];
                        if (0 < dataSizeRead && dataSizeRead <= dataSize)
                            bytesRead += ReadNetStream(buf, headerLen, dataSizeRead);
                    }

                    if (CheckDataFormat(buf, cmdNum, bytesRead) && bytesRead == dataSizeRead + headerLen)
                    {
                        if (open)
                        {
                            open = false;

                            if (buf[3] > 0) // ���� ������
                            {
                                inStream.Write(buf, 6, dataSizeRead);
                                commState = CommStates.Authorized;
                                stop = dataSizeRead < dataSize;
                            }
                            else
                            {
                                errMsg = string.Format(Localization.UseRussian ? 
                                    "SCADA-������� �� ������� ������� ���� {0}" : 
                                    "SCADA-Server unable to open file {0}", filePath);
                                WriteAction(errMsg, Log.ActTypes.Action);
                                commState = CommStates.NotReady;
                                stop = true;
                            }
                        }
                        else
                        {
                            inStream.Write(buf, 5, dataSizeRead);
                            commState = CommStates.Authorized;
                            stop = dataSizeRead < dataSize;
                        }
                    }
                    else
                    {
                        errMsg = string.Format(Localization.UseRussian ? 
                            "�������� ������ ������ SCADA-������� �� ������� �������� ��� ������ �� ����� {0}" :
                            "Incorrect SCADA-Server response to open file or read from file {0} command ", filePath);
                        WriteAction(errMsg, Log.ActTypes.Error);
                        commState = CommStates.Error;
                        stop = true;
                    }
                }

                // ����������� ����������
                if (commState == CommStates.Authorized)
                {
                    if (inStream.Length > 0)
                        inStream.Position = 0;
                    result = true;
                }
            }
            catch (Exception ex)
            {
                errMsg = string.Format(Localization.UseRussian ? "������ ��� ����� ����� {0} �� SCADA-�������: " :
                    "Error receiving file {0} from SCADA-Server: ", filePath) + ex.Message;
                WriteAction(errMsg, Log.ActTypes.Exception);
                Disconnect();
            }
            finally
            {
                RestoreReceiveTimeout();
            }

            return result;
        }

        /// <summary>
        /// ��������� ������� �� SCADA-�������
        /// </summary>
        protected bool SendCommand(int userID, int ctrlCnl, double cmdVal, byte[] cmdData, int kpNum, out bool result)
        {
            Monitor.Enter(tcpLock);
            bool complete = false;
            result = false;
            errMsg = "";

            try
            {
                if (RestoreConnection())
                {
                    WriteAction(Localization.UseRussian ? "�������� ������� �� SCADA-�������" :
                        "Send telecommand to SCADA-Server", Log.ActTypes.Action);

                    commState = CommStates.WaitResponse;
                    tcpClient.ReceiveTimeout = commSettings.ServerTimeout;

                    // �������� �������
                    int cmdLen = double.IsNaN(cmdVal) ? cmdData == null ? 12 : 10 + cmdData.Length : 18;
                    byte[] buf = new byte[cmdLen];
                    buf[0] = (byte)(cmdLen % 256);
                    buf[1] = (byte)(cmdLen / 256);
                    buf[2] = 0x06;
                    buf[3] = (byte)(userID % 256);
                    buf[4] = (byte)(userID / 256);
                    buf[6] = (byte)(ctrlCnl % 256);
                    buf[7] = (byte)(ctrlCnl / 256);

                    if (!double.IsNaN(cmdVal)) // ����������� �������
                    {
                        buf[5] = 0x00;
                        buf[8] = 0x08;
                        buf[9] = 0x00;
                        byte[] bytes = BitConverter.GetBytes(cmdVal);
                        Array.Copy(bytes, 0, buf, 10, 8);
                    }
                    else if (cmdData != null) // �������� �������
                    {
                        buf[5] = 0x01;
                        int cmdDataLen = cmdData.Length;
                        buf[8] = (byte)(cmdDataLen % 256);
                        buf[9] = (byte)(cmdDataLen / 256);
                        Array.Copy(cmdData, 0, buf, 10, cmdDataLen);
                    }
                    else // ����� ��
                    {
                        buf[5] = 0x02;
                        buf[8] = 0x02;
                        buf[9] = 0x00;
                        buf[10] = (byte)(kpNum % 256);
                        buf[11] = (byte)(kpNum / 256);
                    }

                    netStream.Write(buf, 0, cmdLen);

                    // ���� ����������
                    buf = new byte[4];
                    int bytesRead = netStream.Read(buf, 0, 4);

                    // ��������� ���������� ������
                    if (bytesRead == buf.Length && CheckDataFormat(buf, 0x06))
                    {
                        result = buf[3] > 0;
                        commState = result ? CommStates.Authorized : CommStates.NotReady;
                        complete = true;
                    }
                    else
                    {
                        errMsg = Localization.UseRussian ? "�������� ������ ������ SCADA-������� �� ������� ��" :
                            "Incorrect SCADA-Server response to telecommand";
                        WriteAction(errMsg, Log.ActTypes.Error);
                        commState = CommStates.Error;
                    }
                }
            }
            catch (Exception ex)
            {
                errMsg = (Localization.UseRussian ? "������ ��� �������� ������� �� SCADA-�������: " :
                    "Error sending telecommand to SCADA-Server: ") + ex.Message;
                WriteAction(errMsg, Log.ActTypes.Exception);
                Disconnect();
            }
            finally
            {
                RestoreReceiveTimeout();
                Monitor.Exit(tcpLock);
            }

            return complete;
        }


        /// <summary>
        /// ��������� ������������ ����� � ������ ������������ �� SCADA-�������, �������� ��� ����.
        /// ���������� ���������� ���������� �������
        /// </summary>
        public bool CheckUser(string login, string password, out int roleID)
        {
            Monitor.Enter(tcpLock);
            bool result = false;
            roleID = (int)Roles.Disabled;
            errMsg = "";

            try
            {
                if (RestoreConnection())
                {
                    commState = CommStates.WaitResponse;
                    tcpClient.ReceiveTimeout = commSettings.ServerTimeout;

                    // ������ ������������ ����� � ������ ������������, ��� ����
                    byte userLen = login == null ? (byte)0 : (byte)login.Length;
                    byte pwdLen = password == null ? (byte)0 : (byte)password.Length;
                    byte[] buf = new byte[5 + userLen + pwdLen];

                    buf[0] = (byte)(buf.Length % 256);
                    buf[1] = (byte)(buf.Length / 256);
                    buf[2] = 0x01;
                    buf[3] = userLen;
                    if (userLen > 0)
                        Array.Copy(Encoding.Default.GetBytes(login), 0, buf, 4, userLen);
                    buf[4 + userLen] = pwdLen;
                    if (pwdLen > 0)
                        Array.Copy(Encoding.Default.GetBytes(password), 0, buf, 5 + userLen, pwdLen);

                    netStream.Write(buf, 0, buf.Length);

                    // ���� ����������
                    buf = new byte[4];
                    int bytesRead = netStream.Read(buf, 0, 4);

                    // ��������� ���������� ������
                    if (bytesRead == buf.Length && CheckDataFormat(buf, 0x01))
                    {
                        roleID = buf[3];
                        result = true;
                        commState = CommStates.Authorized;
                    }
                    else
                    {
                        errMsg = Localization.UseRussian ? 
                            "�������� ������ ������ SCADA-������� �� ������ ������������ ����� � ������" :
                            "Incorrect SCADA-Server response to check user name and password request";
                        WriteAction(errMsg, Log.ActTypes.Error);
                        commState = CommStates.Error;
                    }
                }
            }
            catch (Exception ex)
            {
                errMsg = (Localization.UseRussian ? 
                    "������ ��� ������� ������������ ����� � ������ ������������ �� SCADA-�������: " :
                    "Error requesting check user name and password to SCADA-Server: ") + ex.Message;
                WriteAction(errMsg, Log.ActTypes.Exception);
                Disconnect();
            }
            finally
            {
                RestoreReceiveTimeout();
                Monitor.Exit(tcpLock);
            }

            return result;
        }

        /// <summary>
        /// ������� ������� ���� ������������ �� SCADA-�������
        /// </summary>
        public bool ReceiveBaseTable(string tableName, DataTable dataTable)
        {
            Monitor.Enter(tcpLock);
            bool result = false;
            errMsg = "";

            try
            {
                try
                {
                    if (RestoreConnection())
                    {
                        using (MemoryStream memStream = new MemoryStream())
                        {
                            if (ReceiveFile(Dirs.BaseDAT, tableName, memStream))
                            {
                                BaseAdapter adapter = new BaseAdapter();
                                adapter.Stream = memStream;
                                adapter.TableName = tableName;
                                adapter.Fill(dataTable, false);
                                result = true;
                            }
                        }
                    }
                }
                finally
                {
                    // ������� �������, ���� �� ������� �������� ����� ������
                    if (!result)
                        dataTable.Rows.Clear();
                }
            }
            catch (Exception ex)
            {
                errMsg = (Localization.UseRussian ? "������ ��� ����� ������� ���� ������������ �� SCADA-�������: " : 
                    "Error receiving configuration database table from SCADA-Server: ") + ex.Message;
                WriteAction(errMsg, Log.ActTypes.Exception);
            }
            finally
            {
                Monitor.Exit(tcpLock);
            }

            return result;
        }

        /// <summary>
        /// ������� ������� ������ �� SCADA-�������
        /// </summary>
        public bool ReceiveSrezTable(string tableName, SrezTableLight srezTableLight)
        {
            Monitor.Enter(tcpLock);
            bool result = false;
            errMsg = "";

            try
            {
                try
                {
                    if (RestoreConnection())
                    {
                        // ����������� ���������� �������
                        Dirs dir = Dirs.Cur;
                        if (tableName.Length > 0)
                        {
                            if (tableName[0] == 'h')
                                dir = Dirs.Hour;
                            else if (tableName[0] == 'm')
                                dir = Dirs.Min;
                        }

                        // ���� ������
                        using (MemoryStream memStream = new MemoryStream())
                        {
                            if (ReceiveFile(dir, tableName, memStream))
                            {
                                SrezAdapter adapter = new SrezAdapter();
                                adapter.Stream = memStream;
                                adapter.TableName = tableName;
                                adapter.Fill(srezTableLight);
                                result = true;
                            }
                        }
                    }
                }
                finally
                {
                    // ������� �������, ���� �� ������� �������� ����� ������
                    if (!result)
                    {
                        srezTableLight.Clear();
                        srezTableLight.TableName = tableName;
                    }
                }
            }
            catch (Exception ex)
            {
                errMsg = (Localization.UseRussian ? "������ ��� ����� ������� ������ �� SCADA-�������: " :
                    "Error receiving data table from SCADA-Server: ") + ex.Message;
                WriteAction(errMsg, Log.ActTypes.Exception);
            }
            finally
            {
                Monitor.Exit(tcpLock);
            }

            return result;
        }

        /// <summary>
        /// ������� ����� �������� ������ �� SCADA-�������
        /// </summary>
        public bool ReceiveTrend(string tableName, DateTime date, Trend trend)
        {
            Monitor.Enter(tcpLock);
            bool result = false;
            errMsg = "";

            try
            {
                try
                {
                    if (RestoreConnection())
                    {
                        WriteAction(string.Format(Localization.UseRussian ? 
                            "���� ������ �������� ������ {0} �� SCADA-�������. ����: {1}" : 
                            "Receive input channel {0} trend from SCADA-Server. File: {1}", 
                            trend.CnlNum, tableName), Log.ActTypes.Action);

                        commState = CommStates.WaitResponse;
                        tcpClient.ReceiveTimeout = commSettings.ServerTimeout;

                        byte tableType;        // ��� �������: �������, ������� ��� ��������
                        byte year, month, day; // ���� ������������� ������

                        if (tableName == "current.dat")
                        {
                            tableType = (byte)0x01;
                            year = month = day = 0;
                        }
                        else
                        {
                            tableType = tableName.Length > 0 && tableName[0] == 'h' ? (byte)0x02 : (byte)0x03;
                            year = (byte)(date.Year % 100);
                            month = (byte)date.Month;
                            day = (byte)date.Day;
                        }

                        // �������� ������� ������ �������� ������
                        byte[] buf = new byte[13];
                        buf[0] = 0x0D;
                        buf[1] = 0x00;
                        buf[2] = 0x0D;
                        buf[3] = tableType;
                        buf[4] = year;
                        buf[5] = month;
                        buf[6] = day;
                        buf[7] = 0x01;
                        buf[8] = 0x00;
                        byte[] bytes = BitConverter.GetBytes(trend.CnlNum);
                        Array.Copy(bytes, 0, buf, 9, 4);
                        netStream.Write(buf, 0, 13);

                        // ���� ������ ������ �������� ������
                        buf = new byte[7];
                        int bytesRead = netStream.Read(buf, 0, 7);
                        int pointCnt = 0;

                        if (bytesRead == 7)
                        {
                            pointCnt = buf[5] + buf[6] * 256;

                            if (pointCnt > 0)
                            {
                                Array.Resize<byte>(ref buf, 7 + pointCnt * 18);
                                bytesRead += ReadNetStream(buf, 7, buf.Length - 7);
                            }
                        }

                        // ��������� ������ �������� ������ �� ���������� ������
                        if (bytesRead == buf.Length && buf[4] == 0x0D)
                        {
                            for (int i = 0; i < pointCnt; i++)
                            {
                                Trend.Point point;
                                int pos = i * 18 + 7;
                                point.DateTime = Arithmetic.DecodeDateTime(BitConverter.ToDouble(buf, pos));
                                point.Val = BitConverter.ToDouble(buf, pos + 8);
                                point.Stat = BitConverter.ToUInt16(buf, pos + 16);

                                trend.Points.Add(point);
                            }

                            trend.Sort();
                            result = true;
                            commState = CommStates.Authorized;
                        }
                        else
                        {
                            errMsg = Localization.UseRussian ? 
                                "�������� ������ ������ SCADA-������� �� ������ ������ �������� ������" :
                                "Incorrect SCADA-Server response to input channel trend request";
                            WriteAction(errMsg, Log.ActTypes.Error);
                            commState = CommStates.Error;
                        }
                    }
                }
                finally
                {
                    // ������� ������, ���� �� ������� �������� ����� ������
                    if (!result)
                    {
                        trend.Clear();
                        trend.TableName = tableName;
                    }
                }
            }
            catch (Exception ex)
            {
                errMsg = (Localization.UseRussian ? "������ ��� ����� ������ �������� ������ �� SCADA-�������: " :
                    "Error receiving input channel trend from SCADA-Server: ") + ex.Message;
                WriteAction(errMsg, Log.ActTypes.Exception);
                Disconnect();
            }
            finally
            {
                RestoreReceiveTimeout();
                Monitor.Exit(tcpLock);
            }

            return result;
        }

        /// <summary>
        /// ������� ������� ������� �� SCADA-�������
        /// </summary>
        public bool ReceiveEventTable(string tableName, EventTableLight eventTableLight)
        {
            Monitor.Enter(tcpLock);
            bool result = false;
            errMsg = "";

            try
            {
                try
                {
                    if (RestoreConnection())
                    {
                        using (MemoryStream memStream = new MemoryStream())
                        {
                            if (ReceiveFile(Dirs.Events, tableName, memStream))
                            {
                                EventAdapter adapter = new EventAdapter();
                                adapter.Stream = memStream;
                                adapter.TableName = tableName;
                                adapter.Fill(eventTableLight);
                                result = true;
                            }
                        }
                    }
                }
                finally
                {
                    // ������� �������, ���� �� ������� �������� ����� ������
                    if (!result)
                    {
                        eventTableLight.Clear();
                        eventTableLight.TableName = tableName;
                    }
                }
            }
            catch (Exception ex)
            {
                errMsg = (Localization.UseRussian ? "������ ��� ����� ������� ������� �� SCADA-�������: " : 
                    "Error receiving event table from SCADA-Server: ") + ex.Message;
                WriteAction(errMsg, Log.ActTypes.Exception);
            }
            finally
            {
                Monitor.Exit(tcpLock);
            }

            return result;
        }

        /// <summary>
        /// ������� ������������� �� SCADA-�������
        /// </summary>
        public bool ReceiveView(string fileName, BaseView view)
        {
            Monitor.Enter(tcpLock);
            bool result = false;
            errMsg = "";

            try
            {
                try
                {
                    if (RestoreConnection())
                    {
                        using (MemoryStream memStream = new MemoryStream())
                        {
                            if (ReceiveFile(Dirs.Itf, fileName, memStream))
                            {
                                view.LoadFromStream(memStream);
                                result = true;
                            }
                        }
                    }
                }
                finally
                {
                    // ������� �������������, ���� �� ������� �������� ����� ������
                    if (!result)
                        view.Clear();
                    // ��������� ������������ ������� ����������
                    view.ItfObjName = Path.GetFileName(fileName);
                }
            }
            catch (Exception ex)
            {
                errMsg = (Localization.UseRussian ? "������ ��� ����� ������������� �� SCADA-�������: " :
                    "Error receiving view from SCADA-Server: ") + ex.Message;
                WriteAction(errMsg, Log.ActTypes.Exception);
            }
            finally
            {
                Monitor.Exit(tcpLock);
            }

            return result;
        }

        /// <summary>
        /// ������� ������ ���������� �� SCADA-�������
        /// </summary>
        public bool ReceiveItfObj(string fileName, Stream stream)
        {
            Monitor.Enter(tcpLock);
            bool result = false;
            errMsg = "";

            try
            {
                if (RestoreConnection() && ReceiveFile(Dirs.Itf, fileName, stream))
                    result = true;
            }
            catch (Exception ex)
            {
                errMsg = (Localization.UseRussian ? "������ ��� ����� ������� ���������� �� SCADA-�������: " :
                    "Error receiving interface object from SCADA-Server: ") + ex.Message;
                WriteAction(errMsg, Log.ActTypes.Exception);
            }
            finally
            {
                Monitor.Exit(tcpLock);
            }

            return result;
        }

        /// <summary>
        /// ������� ���� � ����� ��������� ����� �� SCADA-�������.
        /// � ������ ���������� ����� ������������ ����������� ����
		/// </summary>
        public DateTime ReceiveFileAge(Dirs dir, string fileName)
        {
            Monitor.Enter(tcpLock);
            DateTime result = DateTime.MinValue;
            string filePath = DirToString(dir) + fileName;
            errMsg = "";

            try
            {
                if (RestoreConnection())
                {
#if DETAILED_LOG
                    WriteAction(string.Format(Localization.UseRussian ? 
                        "���� ���� � ������� ��������� ����� {0} �� SCADA-�������" :
                        "Receive date and time of file {0} modification from SCADA-Server", filePath), 
                        Log.ActTypes.Action);
#endif

                    commState = CommStates.WaitResponse;
                    tcpClient.ReceiveTimeout = commSettings.ServerTimeout;

                    // �������� ������� ���� � ������� ��������� �����
                    int cmdLen = 6 + fileName.Length;
                    byte[] buf = new byte[cmdLen];
                    buf[0] = (byte)(cmdLen % 256);
                    buf[1] = (byte)(cmdLen / 256);
                    buf[2] = 0x0C;
                    buf[3] = 0x01;
                    buf[4] = (byte)dir;
                    buf[5] = (byte)fileName.Length;
                    Array.Copy(Encoding.Default.GetBytes(fileName), 0, buf, 6, fileName.Length);
                    netStream.Write(buf, 0, cmdLen);

                    // ���� ���� � ������� ��������� �����
                    buf = new byte[12];
                    netStream.Read(buf, 0, 12);

                    // ��������� ���� � ������� ��������� �����
                    if (CheckDataFormat(buf, 0x0C))
                    {
                        double dt = BitConverter.ToDouble(buf, 4);
                        result = dt == 0.0 ? DateTime.MinValue : Arithmetic.DecodeDateTime(dt);
                        commState = CommStates.Authorized;
                    }
                    else
                    {
                        errMsg = string.Format(Localization.UseRussian ? 
                            "�������� ������ ������ SCADA-������� �� ������ ���� � ������� ��������� ����� {0}" :
                            "Incorrect SCADA-Server response to file modification date and time request", filePath);
                        WriteAction(errMsg, Log.ActTypes.Error);
                        commState = CommStates.Error;
                    }
                }
            }
            catch (Exception ex)
            {
                errMsg = string.Format(Localization.UseRussian ?
                        "������ ��� ����� ���� � ������� ��������� ����� {0} �� SCADA-�������: " :
                        "Error receiving date and time of file {0} modification from SCADA-Server: ", filePath) + 
                        ex.Message;
                WriteAction(errMsg, Log.ActTypes.Exception);
                Disconnect();
            }
            finally
            {
                RestoreReceiveTimeout();
                Monitor.Exit(tcpLock);
            }

            return result;
        }


        /// <summary>
        /// ��������� ����������� ������� �� SCADA-�������
        /// </summary>
        public bool SendStandardCommand(int userID, int ctrlCnl, double cmdVal, out bool result)
        {
            return SendCommand(userID, ctrlCnl, cmdVal, null, 0, out result);
        }

        /// <summary>
        /// ��������� �������� ������� �� SCADA-�������
        /// </summary>
        public bool SendBinaryCommand(int userID, int ctrlCnl, byte[] cmdData, out bool result)
        {
            return SendCommand(userID, ctrlCnl, double.NaN, cmdData, 0, out result);
        }

        /// <summary>
        /// ��������� ������� ������������� ������ �� SCADA-�������
        /// </summary>
        public bool SendRequestCommand(int userID, int ctrlCnl, int kpNum, out bool result)
        {
            return SendCommand(userID, ctrlCnl, double.NaN, null, kpNum, out result);
        }

        /// <summary>
        /// ������� ������� �� �� SCADA-�������
        /// </summary>
        /// <remarks>
        /// ��� ����������� ������� ������������ ������ ������� ����� null.
        /// ��� �������� ������� ������������ �������� ������� ����� double.NaN.
        /// ��� ������� ������ �� ������������ �������� ������� ����� double.NaN � ������ ������� ����� null.</remarks>
        public bool ReceiveCommand(out int kpNum, out int cmdNum, out double cmdVal, out byte[] cmdData)
        {
            Monitor.Enter(tcpLock);
            bool result = false;
            kpNum = 0;
            cmdNum = 0;
            cmdVal = double.NaN;
            cmdData = null;
            errMsg = "";

            try
            {
                if (RestoreConnection())
                {
                    commState = CommStates.WaitResponse;
                    tcpClient.ReceiveTimeout = commSettings.ServerTimeout;

                    // ������ �������
                    byte[] buf = new byte[3];
                    buf[0] = 0x03;
                    buf[1] = 0x00;
                    buf[2] = 0x07;
                    netStream.Write(buf, 0, 3);

                    // ���� �������
                    buf = new byte[5];
                    int bytesRead = netStream.Read(buf, 0, 5);
                    int cmdDataLen = 0;

                    if (bytesRead == 5)
                    {
                        cmdDataLen = buf[3] + buf[4] * 256;

                        if (cmdDataLen > 0)
                        {
                            Array.Resize<byte>(ref buf, 10 + cmdDataLen);
                            bytesRead += netStream.Read(buf, 5, 5 + cmdDataLen);
                        }
                    }

                    // ��������� ���������� ������
                    if (CheckDataFormat(buf, 0x07) && bytesRead == buf.Length)
                    {
                        if (cmdDataLen > 0)
                        {
                            byte cmdType = buf[5];

                            if (cmdType == 0)
                            {
                                cmdVal = BitConverter.ToDouble(buf, 10);
                            }
                            else if (cmdType == 1)
                            {
                                cmdData = new byte[cmdDataLen];
                                Array.Copy(buf, 10, cmdData, 0, cmdDataLen);
                            }

                            kpNum = buf[6] + buf[7] * 256;
                            cmdNum = buf[8] + buf[9] * 256;

                            commState = CommStates.Authorized;
                            result = true;
                        }
                        else // ������ � ������� ���
                        {
                            commState = CommStates.Authorized;
                        }
                    }
                    else
                    {
                        errMsg = Localization.UseRussian ?
                            "�������� ������ ������ SCADA-������� �� ������ ������� ��" :
                            "Incorrect SCADA-Server response to telecommand request";
                        WriteAction(errMsg, Log.ActTypes.Error);
                        commState = CommStates.Error;
                    }
                }
            }
            catch (Exception ex)
            {
                errMsg = (Localization.UseRussian ? "������ ��� ����� ������� �� �� SCADA-�������: " : 
                    "Error requesting telecommand from SCADA-Server: ") + ex.Message;
                WriteAction(errMsg, Log.ActTypes.Exception);
                Disconnect();
            }
            finally
            {
                RestoreReceiveTimeout();
                Monitor.Exit(tcpLock);
            }

            return result;
        }


        /// <summary>
        /// ��������� ������� ���� SCADA-�������
        /// </summary>
        public bool SendSrez(SrezTableLight.Srez curSrez, out bool result)
        {
            Monitor.Enter(tcpLock);
            bool complete = false;
            result = false;
            errMsg = "";

            try
            {
                if (RestoreConnection())
                {
                    commState = CommStates.WaitResponse;
                    tcpClient.ReceiveTimeout = commSettings.ServerTimeout;

                    // �������� ������� ������ �������� �����
                    int cnlCnt = curSrez.CnlNums.Length;
                    int cmdLen = cnlCnt * 14 + 5;

                    byte[] buf = new byte[cmdLen];
                    buf[0] = (byte)(cmdLen % 256);
                    buf[1] = (byte)(cmdLen / 256);
                    buf[2] = 0x03;
                    buf[3] = (byte)(cnlCnt % 256);
                    buf[4] = (byte)(cnlCnt / 256);

                    for (int i = 0; i < cnlCnt; i++)
                    {
                        byte[] bytes = BitConverter.GetBytes((UInt32)curSrez.CnlNums[i]);
                        Array.Copy(bytes, 0, buf, i * 14 + 5, 4);

                        SrezTableLight.CnlData data = curSrez.CnlData[i];
                        bytes = BitConverter.GetBytes(data.Val);
                        Array.Copy(bytes, 0, buf, i * 14 + 9, 8);

                        bytes = BitConverter.GetBytes((UInt16)data.Stat);
                        Array.Copy(bytes, 0, buf, i * 14 + 17, 2);
                    }

                    netStream.Write(buf, 0, cmdLen);

                    // ���� ����������
                    buf = new byte[4];
                    int bytesRead = netStream.Read(buf, 0, 4);

                    // ��������� ���������� ������
                    if (bytesRead == buf.Length && CheckDataFormat(buf, 0x03))
                    {
                        result = buf[3] > 0;
                        commState = result ? CommStates.Authorized : CommStates.NotReady;
                        complete = true;
                    }
                    else
                    {
                        errMsg = Localization.UseRussian ? 
                            "�������� ������ ������ SCADA-������� �� ������� �������� �������� �����" :
                            "Incorrect SCADA-Server response to sending current data command";
                        WriteAction(errMsg, Log.ActTypes.Exception);
                        commState = CommStates.Error;
                    }
                }
            }
            catch (Exception ex)
            {
                errMsg = (Localization.UseRussian ? "������ ��� �������� �������� ����� SCADA-�������: " : 
                    "Error sending current data to SCADA-Server: ") + ex.Message;
                WriteAction(errMsg, Log.ActTypes.Exception);
                Disconnect();
            }
            finally
            {
                RestoreReceiveTimeout();
                Monitor.Exit(tcpLock);
            }

            return complete;
        }

        /// <summary>
        /// ��������� �������� ���� SCADA-�������
        /// </summary>
        public bool SendArchive(SrezTableLight.Srez arcSrez, out bool result)
        {
            Monitor.Enter(tcpLock);
            bool complete = false;
            result = false;
            errMsg = "";

            try
            {
                if (RestoreConnection())
                {
                    commState = CommStates.WaitResponse;
                    tcpClient.ReceiveTimeout = commSettings.ServerTimeout;

                    // �������� ������� ������ ��������� �����
                    int cnlCnt = arcSrez.CnlNums.Length;
                    int cmdLen = cnlCnt * 14 + 13;

                    byte[] buf = new byte[cmdLen];
                    buf[0] = (byte)(cmdLen % 256);
                    buf[1] = (byte)(cmdLen / 256);
                    buf[2] = 0x04;

                    double arcDT = Arithmetic.EncodeDateTime(arcSrez.DateTime);
                    byte[] bytes = BitConverter.GetBytes(arcDT);
                    Array.Copy(bytes, 0, buf, 3, 8);

                    buf[11] = (byte)(cnlCnt % 256);
                    buf[12] = (byte)(cnlCnt / 256);

                    for (int i = 0; i < cnlCnt; i++)
                    {
                        bytes = BitConverter.GetBytes((UInt32)arcSrez.CnlNums[i]);
                        Array.Copy(bytes, 0, buf, i * 14 + 13, 4);

                        SrezTableLight.CnlData data = arcSrez.CnlData[i];
                        bytes = BitConverter.GetBytes(data.Val);
                        Array.Copy(bytes, 0, buf, i * 14 + 17, 8);

                        bytes = BitConverter.GetBytes((UInt16)data.Stat);
                        Array.Copy(bytes, 0, buf, i * 14 + 25, 2);
                    }

                    netStream.Write(buf, 0, cmdLen);

                    // ���� ����������
                    buf = new byte[4];
                    int bytesRead = netStream.Read(buf, 0, 4);

                    // ��������� ���������� ������
                    if (bytesRead == buf.Length && CheckDataFormat(buf, 0x04))
                    {
                        result = buf[3] > 0;
                        commState = result ? CommStates.Authorized : CommStates.NotReady;
                        complete = true;
                    }
                    else
                    {
                        errMsg = Localization.UseRussian ?
                            "�������� ������ ������ SCADA-������� �� ������� �������� ��������� �����" :
                            "Incorrect SCADA-Server response to sending archive data command";
                        WriteAction(errMsg, Log.ActTypes.Exception);
                        commState = CommStates.Error;
                    }
                }
            }
            catch (Exception ex)
            {
                errMsg = (Localization.UseRussian ? "������ ��� �������� ��������� ����� SCADA-�������: " : 
                    "Error sending archive data to SCADA-Server: ") + ex.Message;
                WriteAction(errMsg, Log.ActTypes.Exception);
                Disconnect();
            }
            finally
            {
                RestoreReceiveTimeout();
                Monitor.Exit(tcpLock);
            }

            return complete;
        }

        /// <summary>
        /// ��������� ������� SCADA-�������
        /// </summary>
        public bool SendEvent(EventTableLight.Event aEvent, out bool result)
        {
            Monitor.Enter(tcpLock);
            bool complete = false;
            result = false;
            errMsg = "";

            try
            {
                if (RestoreConnection())
                {
                    commState = CommStates.WaitResponse;
                    tcpClient.ReceiveTimeout = commSettings.ServerTimeout;

                    // �������� ������� ������ �������
                    byte descrLen = (byte)aEvent.Descr.Length;
                    byte dataLen = (byte)aEvent.Data.Length;
                    int cmdLen = 46 + descrLen + dataLen;
                    byte[] buf = new byte[cmdLen];
                    buf[0] = (byte)(cmdLen % 256);
                    buf[1] = (byte)(cmdLen / 256);
                    buf[2] = 0x05;

                    double evDT = Arithmetic.EncodeDateTime(aEvent.DateTime);
                    byte[] bytes = BitConverter.GetBytes(evDT);
                    Array.Copy(bytes, 0, buf, 3, 8);

                    buf[11] = (byte)(aEvent.ObjNum % 256);
                    buf[12] = (byte)(aEvent.ObjNum / 256);
                    buf[13] = (byte)(aEvent.KPNum % 256);
                    buf[14] = (byte)(aEvent.KPNum / 256);
                    buf[15] = (byte)(aEvent.ParamID % 256);
                    buf[16] = (byte)(aEvent.ParamID / 256);

                    bytes = BitConverter.GetBytes(aEvent.CnlNum);
                    Array.Copy(bytes, 0, buf, 17, 4);
                    bytes = BitConverter.GetBytes(aEvent.OldCnlVal);
                    Array.Copy(bytes, 0, buf, 21, 8);
                    bytes = BitConverter.GetBytes(aEvent.OldCnlStat);
                    Array.Copy(bytes, 0, buf, 29, 2);
                    bytes = BitConverter.GetBytes(aEvent.NewCnlVal);
                    Array.Copy(bytes, 0, buf, 31, 8);
                    bytes = BitConverter.GetBytes(aEvent.NewCnlStat);
                    Array.Copy(bytes, 0, buf, 39, 2);

                    buf[41] = aEvent.Checked ? (byte)0x01 : (byte)0x00;
                    buf[42] = (byte)(aEvent.UserID % 256);
                    buf[43] = (byte)(aEvent.UserID / 256);

                    buf[44] = descrLen;
                    Array.Copy(Encoding.Default.GetBytes(aEvent.Descr), 0, buf, 45, descrLen);
                    buf[45 + descrLen] = dataLen;
                    Array.Copy(Encoding.Default.GetBytes(aEvent.Data), 0, buf, 46 + descrLen, dataLen);

                    netStream.Write(buf, 0, cmdLen);

                    // ���� ����������
                    buf = new byte[4];
                    int bytesRead = netStream.Read(buf, 0, 4);

                    // ��������� ���������� ������
                    if (bytesRead == buf.Length && CheckDataFormat(buf, 0x05))
                    {
                        result = buf[3] > 0;
                        commState = result ? CommStates.Authorized : CommStates.NotReady;
                        complete = true;
                    }
                    else
                    {
                        errMsg = Localization.UseRussian ?
                            "�������� ������ ������ SCADA-������� �� ������� �������� �������" :
                            "Incorrect SCADA-Server response to sending event command";
                        WriteAction(errMsg, Log.ActTypes.Error);
                        commState = CommStates.Error;
                    }
                }
            }
            catch (Exception ex)
            {
                errMsg = (Localization.UseRussian ? "������ ��� �������� ������� SCADA-�������: " :
                    "Error sending event to SCADA-Server: ") + ex.Message;
                WriteAction(errMsg, Log.ActTypes.Exception);
                Disconnect();
            }
            finally
            {
                RestoreReceiveTimeout();
                Monitor.Exit(tcpLock);
            }

            return complete;
        }

        /// <summary>
        /// ��������� ������� ������������ ������� SCADA-�������
        /// </summary>
        public bool CheckEvent(int userID, DateTime date, int evNum, out bool result)
        {
            Monitor.Enter(tcpLock);
            bool complete = false;
            result = false;
            errMsg = "";

            try
            {
                if (RestoreConnection())
                {
                    WriteAction(Localization.UseRussian ? "�������� ������� ������������ ������� SCADA-�������" :
                        "Send check event command to SCADA-Server", Log.ActTypes.Action);

                    commState = CommStates.WaitResponse;
                    tcpClient.ReceiveTimeout = commSettings.ServerTimeout;

                    // �������� �������
                    byte[] buf = new byte[10];
                    buf[0] = 0x0A;
                    buf[1] = 0x00;
                    buf[2] = 0x0E;
                    buf[3] = (byte)(userID % 256);
                    buf[4] = (byte)(userID / 256);
                    buf[5] = (byte)(date.Year % 100);
                    buf[6] = (byte)date.Month;
                    buf[7] = (byte)date.Day;
                    buf[8] = (byte)(evNum % 256);
                    buf[9] = (byte)(evNum / 256);
                    netStream.Write(buf, 0, 10);

                    // ���� ����������
                    buf = new byte[4];
                    int bytesRead = netStream.Read(buf, 0, 4);

                    // ��������� ���������� ������
                    if (bytesRead == buf.Length && CheckDataFormat(buf, 0x0E))
                    {
                        result = buf[3] > 0;
                        commState = result ? CommStates.Authorized : CommStates.NotReady;
                        complete = true;
                    }
                    else
                    {
                        errMsg = Localization.UseRussian ? 
                            "�������� ������ ������ SCADA-������� �� ������� ������������ �������" :
                            "Incorrect SCADA-Server response to check event command";
                        WriteAction(errMsg, Log.ActTypes.Error);
                        commState = CommStates.Error;
                    }
                }
            }
            catch (Exception ex)
            {
                errMsg = (Localization.UseRussian ? 
                    "������ ��� �������� ������� ������������ ������� SCADA-�������: " :
                    "Error sending check event command to SCADA-Server: ") + ex.Message;
                WriteAction(errMsg, Log.ActTypes.Exception);
                Disconnect();
            }
            finally
            {
                RestoreReceiveTimeout();
                Monitor.Exit(tcpLock);
            }

            return complete;
        }

        /// <summary>
        /// ��������� ������ �� SCADA-�������� � ���������� �������
        /// </summary>
        public void Close()
        {
            Disconnect();
        }


        /// <summary>
        /// �������� ���� ������������ �� � ��������������
        /// </summary>
        public static Roles GetRole(int roleID)
        {
            if ((int)Roles.Admin <= roleID && roleID <= (int)Roles.App)
                return (Roles)roleID;
            if ((int)Roles.Custom <= roleID && roleID < (int)Roles.Err)
                return Roles.Custom;
            else if (roleID == (int)Roles.Err)
                return Roles.Err;
            else
                return Roles.Disabled;
        }
        
        /// <summary>
        /// �������� ������������ ���� �� � ��������������
        /// </summary>
        public static string GetRoleName(int roleID)
        {
            return GetRoleName(GetRole(roleID));
        }

        /// <summary>
        /// �������� ������������ ����
        /// </summary>
        public static string GetRoleName(Roles role)
        {
            switch (role)
            {
                case Roles.Admin:
                    return Localization.UseRussian ? "�������������" : "Administrator";
                case Roles.Dispatcher:
                    return Localization.UseRussian ? "���������" : "Dispatcher";
                case Roles.Guest:
                    return Localization.UseRussian ? "�����" : "Guest";
                case Roles.App:
                    return Localization.UseRussian ? "����������" : "Application";
                case Roles.Custom:
                    return Localization.UseRussian ? "���������������� ����" : "Custom role";
                case Roles.Err:
                    return Localization.UseRussian ? "������" : "Error";
                default: // Roles.Disabled
                    return Localization.UseRussian ? "��������" : "Disabled";
            }
        }
    }
}
