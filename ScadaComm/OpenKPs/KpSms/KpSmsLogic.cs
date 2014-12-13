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
 * Module   : KpSms
 * Summary  : Device communication logic
 * 
 * Author   : Mikhail Shiryaev
 * Created  : 2009
 * Modified : 2014
 * 
 * Description
 * Sending and receiving SMS messages using AT commands.
 */

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Text;

namespace Scada.Comm.KP
{
    /// <summary>
    /// Device communication logic
    /// <para>������ ������ ��</para>
    /// </summary>
    public sealed class KpSmsLogic : KPLogic
    {
        /// <summary>
        /// ���������
        /// </summary>
        private class Message
        {
            public int Index;          // ������
            public int Status;         // ������: 0 - �� ���������, 1 - ���������, 2 - �� ����������, 3 - ����������
            public int Length;         // ����� PDU ��� ����� ������ ������ ���������

            public string Phone;       // ���������� ����� �����������
            public DateTime TimeStamp; // ��������� ����� ������ ���������
            public string Text;        // ����� ���������
            public object[] Reference; // ������ �� ��������� � ������ ����� ������� ����� �����

            public Message()
            {
                Index = 0;
                Status = 0;
                Length = 0;

                Phone = "";
                TimeStamp = DateTime.MinValue;
                Text = "";
                Reference = null;
            }
        }

        /// <summary>
        /// ������ ��������� ��� ����� ������� ����� �����
        /// </summary>
        private class MessageObjList : List<object[]>
        {
            public override string ToString()
            {
                return "List of " + Count + " messages";
            }
        }

        private const int MaxEventCount = 999999; // ������������ �������� �������� ������� ��

        private bool primary;          // �������� �� �� ����� �����, ����� ������� � GSM-����������
        List<Message> messageList;     // ������ ���������, ���������� GSM-����������


        /// <summary>
        /// �����������
        /// </summary>
        public KpSmsLogic(int number)
            : base(number)
        {
            primary = false;
            messageList = new List<Message>();
            
            CanSendCmd = true;
            InitArrays(2, 0);

            if (Localization.UseRussian)
            {
                KPParams[0] = new Param(1, "�����");
                KPParams[1] = new Param(2, "���-�� �������");
            }
            else
            {
                KPParams[0] = new Param(1, "Connection");
                KPParams[1] = new Param(2, "Event count");
            }
        }


        /// <summary>
        /// ��������� ������ ��������� �� ������� ������, ���������� �� GSM-���������
        /// </summary>
        private bool FillMessageList(List<string> inData, out string errMsg)
        {
            StringBuilder errMsgSB = new StringBuilder();
            messageList.Clear();

            int i = 1;
            int lineCnt = inData.Count;
            while (i <= lineCnt)
            {
                string line = inData[i - 1];
                if (line.StartsWith("+CMGL: ") && line.Length > 7)
                {
                    // ��������� �������, ������� � ����� ���������
                    Message msg = new Message();
                    string[] parts = line.Substring(7).Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length >= 3)
                    {
                        int val1, val2, val3;
                        if (int.TryParse(parts[0], out val1) && int.TryParse(parts[1], out val2) &&
                            int.TryParse(parts[parts.Length - 1], out val3))
                        {
                            msg.Index = val1;
                            msg.Status = val2;
                            msg.Length = val3;
                            i++;
                        }
                    }

                    // ����������� PDU
                    if (msg.Index > 0)
                    {
                        if (i <= lineCnt)
                        {
                            line = inData[i - 1]; // PDU
                            try
                            {
                                int scaLen = int.Parse(line.Substring(0, 2));         // ����� ������ ������ ���������
                                int oaPos = scaLen * 2 + 4;                           // ������� ������ �����������

                                if (msg.Length == (line.Length - oaPos + 2) / 2)
                                {
                                    int oaLen = int.Parse(line.Substring(oaPos, 2),
                                        NumberStyles.HexNumber);                      // ����� ������ �����������
                                    if (oaLen % 2 > 0) oaLen++;
                                    msg.Phone = DecodePhone(line.Substring(oaPos + 2, oaLen + 2));

                                    int sctsPos = oaPos + oaLen + 8;                  // ������� ��������� �����
                                    msg.TimeStamp = new DateTime(int.Parse("20" + line[sctsPos + 1] + line[sctsPos]),
                                        int.Parse(line[sctsPos + 3].ToString() + line[sctsPos + 2]),
                                        int.Parse(line[sctsPos + 5].ToString() + line[sctsPos + 4]),
                                        int.Parse(line[sctsPos + 7].ToString() + line[sctsPos + 6]),
                                        int.Parse(line[sctsPos + 9].ToString() + line[sctsPos + 8]),
                                        int.Parse(line[sctsPos + 11].ToString() + line[sctsPos + 10]));

                                    string dcs = line.Substring(sctsPos - 2, 2);      // ���������
                                    int udPos = sctsPos + 16;                         // ������� ������ ���������
                                    int udl = int.Parse(line.Substring(udPos - 2, 2),
                                        NumberStyles.HexNumber);                      // ����� ������ ���������
                                    string ud = line.Substring(udPos);                // ����� ���������
                                    if (dcs == "00" && ud.Length * 4 / 7 == udl || dcs != "00" && ud.Length == udl * 2)
                                    {
                                        if (dcs == "00")
                                            msg.Text = Decode7bitText(ud);
                                        else if (dcs == "F6")
                                            msg.Text = Decode8bitText(ud);
                                        else if (dcs == "08")
                                            msg.Text = DecodeUnicodeText(ud);

                                        messageList.Add(msg);
                                    }
                                    else
                                    {
                                        errMsgSB.AppendLine(string.Format(Localization.UseRussian ?
                                            "������ � ������ {0}: ������������ ����� ������ ���������" :
                                            "Error in line {0}: incorrect message length", i));
                                    }
                                }
                                else
                                {
                                    errMsgSB.AppendLine(string.Format(Localization.UseRussian ?
                                        "������ � ������ {0}: ������������ ����� PDU" :
                                        "Error in line {0}: incorrect PDU length", i));
                                }
                            }
                            catch
                            {
                                errMsgSB.AppendLine(string.Format(Localization.UseRussian ?
                                    "������ � ������ {0}: ���������� ������������ PDU" :
                                    "Error in line {0}: unable to decode PDU", i));
                            }
                        }
                        else
                        {
                            errMsgSB.AppendLine(Localization.UseRussian ?
                                "������: ������������ ���������� ������� ������" :
                                "Error: incorrect termination of the input data");
                        }
                    }
                    else
                    {
                        errMsgSB.AppendLine(string.Format(Localization.UseRussian ?
                            "������ � ������ {0}: ������������ ��������� ���������" :
                            "Error in line {0}: incorrect message parameters", i));
                    }
                }

                i++;
            }

            errMsg = errMsgSB.ToString();
            return errMsg == "";
        }

        /// <summary>
        /// ������������ ���������� �����
        /// </summary>
        private string DecodePhone(string phone)
        {
            StringBuilder result = new StringBuilder();
            if (phone.StartsWith("91"))
                result.Append("+");

            for (int i = 2; i < phone.Length; i += 2)
            {
                if (i + 1 < phone.Length)
                {
                    char c = phone[i + 1];
                    if ('0' <= c && c <= '9') 
                        result.Append(c);

                    c = phone[i];
                    if ('0' <= c && c <= '9') 
                        result.Append(c);
                }
            }

            return result.ToString();
        }

        /// <summary>
        /// ������������ ���������� �����
        /// </summary>
        private string EncodePhone(string phone)
        {
            StringBuilder result = new StringBuilder();
            int phoneLen = phone.Length;

            if (phoneLen > 0)
            {
                if (phone[0] == '+')
                {
                    phone = phone.Substring(1);
                    result.Append("91");
                    phoneLen--;
                }
                else
                    result.Append("81");

                int i = 1;
                while (i < phoneLen)
                {
                    result.Append(phone[i]);
                    result.Append(phone[i - 1]);
                    i += 2;
                }
                if (i == phoneLen)
                {
                    result.Append('F');
                    result.Append(phone[i - 1]);
                }
            }

            return phoneLen.ToString("X2") + result.ToString();
        }

        /// <summary>
        /// ������������� ���������� ������ 16-������� ����� � ����
        /// </summary>
        private byte HexToByte(string s)
        {
            try { return (byte)int.Parse(s, NumberStyles.HexNumber);}
            catch { return 0; }
        }

        /// <summary>
        /// ������������ ����� � 7-������ ���������
        /// </summary>
        private string Decode7bitText(string text)
        {
            byte[] MaskL = new byte[] {0xFE, 0xFC, 0xF8, 0xF0, 0xE0, 0xC0, 0x80};
            byte[] MaskR = new byte[] {0x01, 0x03, 0x07, 0x0F, 0x1F, 0x3F, 0x7F};

            // ������ ��� �������� �� �� 16-������ �������� � ������ � �����
            byte[] buf = new byte[text.Length / 2];
            int bufPos = 0;
            for (int i = 0; i < text.Length - 1; i += 2)
                buf[bufPos++] = HexToByte(text.Substring(i, 2));

            // �����������
            byte[] result = new byte[text.Length];
            int resPos = 0;
            byte part = 0;
            byte bit = 7;

            for (int i = 0; i < bufPos; i++)
            {
                byte b = buf[i];
                byte sym = (byte)((part >> (bit + 1)) | ((b & MaskR[bit - 1]) << (7 - bit)));
                part = (byte)(b & MaskL[bit - 1]);
                result[resPos++] = sym;

                if (--bit == 0)
                {
                    sym = (byte)((b & 0xFE) >> 1);
                    part = 0;

                    result[resPos++] = sym;
                    bit = 7;
                }
            }

            return resPos > 0 ? new string(Encoding.Default.GetChars(result, 0, resPos)) : "";
        }

        /// <summary>
        /// ������������ ����� � 7-������ ���������
        /// </summary>
        private List<byte> Encode7bitText(string text)
        {
            List<byte> result = new List<byte>();
            byte[] bytes = Encoding.Default.GetBytes(text);

            byte bit = 7; // �� 7 �� 1
            int i = 0;
            int len = bytes.Length;
            while (i < len)
            {
                byte sym = (byte)(bytes[i] & 0x7F);
                byte nextSym = i < len - 1 ? (byte)(bytes[i + 1] & 0x7F) : (byte)0;
                byte code = (byte)((sym >> (7 - bit)) | (nextSym << bit));

                if (bit == 1)
                {
                    i++;
                    bit = 7;
                }
                else
                    bit--;

                result.Add(code);
                i++;
            }

            return result;
        }

        /// <summary>
        /// ������������ ����� � 8-������ ���������
        /// </summary>
        private string Decode8bitText(string text)
        {
            byte[] buf = new byte[text.Length / 2];
            int bufPos = 0;

            for (int i = 0; i < text.Length - 1; i += 2)
                buf[bufPos++] = HexToByte(text.Substring(i, 2));

            return bufPos > 0 ? new string(Encoding.Default.GetChars(buf, 0, bufPos)) : "";
        }

        /// <summary>
        /// ������������ ����� � ��������� Unicode
        /// </summary>
        private string DecodeUnicodeText(string text)
        {
            StringBuilder result = new StringBuilder();

            for (int i = 0; i < text.Length - 3; i += 4)
                try
                {
                    int val = int.Parse(text.Substring(i, 4), NumberStyles.HexNumber);
                    result.Append(char.ConvertFromUtf32(val));
                }
                catch { }

            return result.ToString();
        }

        /// <summary>
        /// ������������ ����� � ��������� Unicode
        /// </summary>
        private List<byte> EncodeUnicodeText(string text)
        {
            List<byte> result = new List<byte>();

            for (int i = 0; i < text.Length; i++)
            {
                int val = char.ConvertToUtf32(text, i);
                result.Add((byte)(val >> 8 & 0xFF));
                result.Add((byte)(val & 0xFF));
            }

            return result;
        }

        /// <summary>
        /// ������� Protocol Data Unit ��� �������� ���������
        /// </summary>
        private string MakePDU(string phone, string text, out int pduLen)
        {
            // ����� ���������
            bool sevenBit = true;
            for (int i = 0; i < text.Length && sevenBit; i++)
            {
                char c = text[i];
                if ((c < ' ' || c > 'z') && c != '\n')
                    sevenBit = false;
            }

            // ��������� ����� ������, ���������� ��� ��������
            if (sevenBit)
            {
                if (text.Length > 140)
                    text = text.Substring(0, 140);
            }
            else
            {
                if (text.Length > 70)
                    text = text.Substring(0, 70);
            }

            // ������������ PDU
            StringBuilder pdu = new StringBuilder();
            pdu.Append("00");                     // Service Center Adress (SCA)
            pdu.Append("01");                     // PDU-type
            pdu.Append("00");                     // Message Reference (MR)
            pdu.Append(EncodePhone(phone));       // Destination Adress (DA)
            pdu.Append("00");                     // Protocol Identifier (PID)
            pdu.Append(sevenBit ? "00" : "08");   // Data Coding Scheme (DCS)

            List<byte> ud = sevenBit ? Encode7bitText(text) : EncodeUnicodeText(text);
            pdu.Append(ud.Count.ToString("X2"));  // User Data Length (UDL)
            foreach (byte b in ud)
                pdu.Append(b.ToString("X2"));     // User Data (UD)

            pduLen = (pdu.Length - 2) / 2;
            return pdu.ToString();
        }

        /// <summary>
        /// �������� �� ����� ������� ����� ����� ��� ������� ������ ���������, �������������� � ���� object[]
        /// </summary>
        private List<object[]> GetMessageObjList()
        {
            List<object[]> messages = CommonProps.ContainsKey("Messages") ? 
                CommonProps["Messages"] as List<object[]> : null;

            if (messages == null)
            {
                messages = new MessageObjList();
                CommonProps.Add("Messages", messages);
            }

            return messages;
        }

        /// <summary>
        /// ������������� ��������� � object[]
        /// </summary>
        /// <remarks>��� ����������: object[] {������, ������, �������, �����, �����, ����������}</remarks>
        private object[] ConvertMessage(Message message)
        {
            object[] msgObjArr = new object[6];

            if (message == null)
            {
                msgObjArr[0] = 0;
                msgObjArr[1] = 0;
                msgObjArr[2] = "";
                msgObjArr[3] = DateTime.MinValue;
                msgObjArr[4] = "";
                msgObjArr[5] = true;
            }
            else
            {
                msgObjArr[0] = message.Index;
                msgObjArr[1] = message.Status;
                msgObjArr[2] = message.Phone.Clone();
                msgObjArr[3] = message.TimeStamp;
                msgObjArr[4] = message.Text.Clone();
                msgObjArr[5] = false;
            }

            return msgObjArr;
        }

        /// <summary>
        /// �������� ������� � ������ ������� ��
        /// </summary>
        private void WriteEvent(DateTime timeStamp, string phone, string text, ref int eventCnt)
        {
            eventCnt++;
            Event ev = new Event(timeStamp, Number, KPParams[1]);
            ev.NewData = new ParamData(CurData[1].Val + eventCnt, 1);
            ev.Descr = phone == "" ? text : phone + "; " + text;
            EventList.Add(ev);
        }

        /// <summary>
        /// ��������� ��������� �� "���-�� �������"
        /// </summary>
        private void SetEventCount(int eventCnt)
        {
            double newVal = CurData[1].Val + eventCnt;
            if (newVal > MaxEventCount) 
                newVal = 0;
            SetParamData(1, newVal, 1);
        }


        /// <summary>
        /// ����� ������ ��������� ��
        /// </summary>
        private void PrimarySession()
        {
            string logText; // ����� ��� ������ � log-���� ����� �����
            int i;

            // ���������� ���
            if (WorkState != WorkStates.Normal)
            {
                lastCommSucc = false;
                i = 0;
                while (i < CommLineParams.TriesCnt && !lastCommSucc && !Terminated)
                {
                    WriteToLog(Localization.UseRussian ? "���������� ���" : "Set echo off");
                    KPUtils.WriteLineToSerialPort(SerialPort, "ATE0", out logText);
                    WriteToLog(logText);

                    KPUtils.ReadLinesFromSerialPort(SerialPort, KPReqParams.Timeout, 
                        false, "OK", out lastCommSucc, out logText);
                    WriteToLog(logText);

                    FinishRequest();
                    i++;
                }
            }

            // ����� ������
            if (lastCommSucc)
            {
                lastCommSucc = false;
                i = 0;
                while (i < CommLineParams.TriesCnt && !lastCommSucc && !Terminated)
                {
                    WriteToLog(Localization.UseRussian ? "����� ������" : "Drop call");
                    KPUtils.WriteLineToSerialPort(SerialPort, "ATH" /*"AT+CHUP"*/, out logText);
                    WriteToLog(logText);

                    List<string> inData = KPUtils.ReadLinesFromSerialPort(SerialPort, KPReqParams.Timeout, 
                        false, "OK", out lastCommSucc, out logText);
                    WriteToLog(logText);

                    FinishRequest();
                    i++;
                }
            }

            // ��������� � �������� ���������, ���������� �����
            int eventCnt = 0; // ���������� ��������� �������

            if (lastCommSucc)
            {
                foreach (Message msg in messageList)
                {
                    // ��������� ���������, ���� ��� �� ���������� ������� ��
                    try
                    {
                        object[] msgObjArr = msg.Reference;
                        if (!(bool)msgObjArr[5] /*��������� �� ����������*/ && 
                            (int)msgObjArr[1] <= 1 /*�������� ���������*/)
                        {
                            // ������ �������
                            WriteEvent(msg.TimeStamp, msg.Phone, msg.Text, ref eventCnt);
                            msgObjArr[5] = true;
                        }
                    }
                    catch
                    {
                        WriteToLog((Localization.UseRussian ? "������ ��� ��������� ��������� " : 
                            "Error processing message ") + msg.Index);
                    }

                    // �������� ��������� �� ������ GSM-���������
                    bool deleteComplete = false;
                    i = 0;
                    while (i < CommLineParams.TriesCnt && !deleteComplete && !Terminated)
                    {
                        WriteToLog((Localization.UseRussian ? "�������� ��������� " : "Delete message ") + msg.Index);
                        KPUtils.WriteLineToSerialPort(SerialPort, "AT+CMGD=" + msg.Index, out logText);
                        WriteToLog(logText);

                        KPUtils.ReadLinesFromSerialPort(SerialPort, KPReqParams.Timeout,
                            false, "OK", out deleteComplete, out logText);
                        WriteToLog(logText);

                        FinishRequest();
                        i++;
                    }
                    lastCommSucc = lastCommSucc && deleteComplete;
                }

                messageList.Clear();
                GetMessageObjList().Clear();
            }

            SetEventCount(eventCnt);
            if (lastCommSucc)
                WriteToLog((Localization.UseRussian ? "���������� ���������� ���������: " : 
                    "Received message count: ") + eventCnt);

            // ������ ������ ���������
            if (lastCommSucc)
            {
                lastCommSucc = false;
                i = 0;

                while (i < CommLineParams.TriesCnt && !lastCommSucc && !Terminated)
                {
                    WriteToLog(Localization.UseRussian ? "������ ������ ���������" : "Request message list");
                    KPUtils.WriteLineToSerialPort(SerialPort, "AT+CMGL=4", out logText);
                    WriteToLog(logText);

                    List<string> inData = KPUtils.ReadLinesFromSerialPort(SerialPort, KPReqParams.Timeout, 
                        false, "OK", out lastCommSucc, out logText);
                    WriteToLog(logText);

                    // ����������� ���������
                    if (lastCommSucc)
                    {
                        string errMsg;
                        if (!FillMessageList(inData, out errMsg))
                        {
                            WriteToLog(errMsg);
                            lastCommSucc = false;
                        }
                    }

                    FinishRequest();
                    i++;
                }

                // ������ ��������� � ����� �������� ����� �����
                List<object[]> msgObjList = GetMessageObjList();
                foreach (Message msg in messageList)
                {
                    object[] msgObjArr = ConvertMessage(msg);
                    msg.Reference = msgObjArr;
                    msgObjList.Add(msgObjArr);
                }
            }

            // ����������� ������� �����
            double newVal = lastCommSucc ? 1.0 : -1.0;
            SetParamData(0, newVal, 1);
        }

        /// <summary>
        /// ����� ������ �� ��������� ��
        /// </summary>
        private void SecondarySession()
        {
            // ��������� ��������� �� ����� ������� ����� �����
            List<object[]> msgObjList = GetMessageObjList();
            int eventCnt = 0; // ���������� ��������� �������

            foreach (object[] msgObjArr in msgObjList)
            {
                try
                {
                    if (!(bool)msgObjArr[5]) // ��������� �� ����������
                    {
                        if ((string)msgObjArr[2] == CallNum /*���������� ����������� ������*/ &&
                            (int)msgObjArr[1] <= 1 /*�������� ���������*/)
                        {
                            // ������ �������
                            WriteEvent((DateTime)msgObjArr[3], "", (string)msgObjArr[4], ref eventCnt);
                            msgObjArr[5] = true;
                        }
                    }
                }
                catch
                {
                    int index;
                    try { index = (int)msgObjArr[0]; }
                    catch { index = 0; }
                    WriteToLog((Localization.UseRussian ? "������ ��� ��������� ���������" : 
                        "Error processing message") + (index > 0 ? " " + index : ""));
                }
            }

            SetEventCount(eventCnt);
            WriteToLog((Localization.UseRussian ? "���������� ���������� ���������: " :
                "Received message count: ") + eventCnt);
        }


        /// <summary>
        /// ��������� ����� ������ ��
        /// </summary>
        public override void Session()
        {
            base.Session();

            if (primary)
                PrimarySession();
            else
                SecondarySession();

            CalcSessStats();
        }

        /// <summary>
        /// ��������� ������� ��
        /// </summary>
        public override void SendCmd(Command cmd)
        {
            base.SendCmd(cmd);
            lastCommSucc = false;

            if (cmd.CmdType == CmdType.Binary && (cmd.CmdNum == 1 || cmd.CmdNum == 2))
            {
                string logText; // ����� ��� ������ � log-���� ����� �����
                string cmdData = new string(Encoding.Default.GetChars(cmd.CmdData));
                if (cmdData.Length > 0)
                {
                    if (cmd.CmdNum == 1)
                    {
                        // �������� ���������
                        // ������ �������: <�������>;<�����> 
                        // ���������� ����� ����������� ������ ��� ��������� �� �� ����� �����
                        int scPos = cmdData.IndexOf(';');
                        string phone = primary ? (scPos > 0 ? cmdData.Substring(0, scPos).Trim() : "") : CallNum;
                        string text = scPos < 0 ? cmdData : scPos + 1 < cmdData.Length ?
                            cmdData.Substring(scPos + 1).Trim() : "";

                        if (phone == "" || text == "")
                        {
                            WriteToLog(Localization.UseRussian ?
                                "����������� ���������� ����� ��� ����� ���������" :
                                "No telephone number or message text");
                        }
                        else
                        {
                            int pduLen;
                            string pdu = MakePDU(phone, text, out pduLen);

                            KPUtils.WriteLineToSerialPort(SerialPort, "AT+CMGS=" + pduLen, out logText);
                            WriteToLog(logText);
                            Thread.Sleep(100);

                            try
                            {
                                if (SerialPort != null) SerialPort.NewLine = "\x1A";
                                KPUtils.WriteLineToSerialPort(SerialPort, pdu, out logText);
                                WriteToLog(logText);
                            }
                            finally
                            {
                                if (SerialPort != null) SerialPort.NewLine = "\x0D";
                            }

                            List<string> inData = KPUtils.ReadLinesFromSerialPort(SerialPort, KPReqParams.Timeout,
                                false, "OK", out lastCommSucc, out logText);
                            WriteToLog(logText);

                            Thread.Sleep(KPReqParams.Delay);
                        }
                    }
                    else
                    {
                        // ������������ AT-�������
                        KPUtils.WriteLineToSerialPort(SerialPort, cmdData, out logText);
                        WriteToLog(logText);

                        List<string> inData = KPUtils.ReadLinesFromSerialPort(SerialPort, KPReqParams.Timeout,
                            false, new string[] { "OK", "ERROR" }, out lastCommSucc, out logText);
                        WriteToLog(logText);

                        Thread.Sleep(KPReqParams.Delay);
                    }
                }
                else
                {
                    WriteToLog(Localization.UseRussian ? "����������� ������ �������" : "No command data");
                }
            }
            else
            {
                WriteToLog(Localization.UseRussian ? "������������ �������" : "Illegal command");
            }

            CalcCmdStats();
        }
        
        /// <summary>
        /// ��������� �������� ��� ������� ����� �����
        /// </summary>
        public override void OnCommLineStart()
        {
            // �����������, �������� �� �� �������� �� ����� �����
            primary = KPReqParams.CmdLine.Trim().ToLower() == "primary";
            
            // ��������� ������� ����� ������ ��� ������ � ���������������� ������
            if (SerialPort != null)
                SerialPort.NewLine = "\x0D";
        }

        /// <summary>
        /// ������������� ������ ��������� �� � ������
        /// </summary>
        public override string ParamDataToStr(int signal, ParamData paramData)
        {
            if (paramData.Stat > 0)
            {
                if (signal == 1)
                    return paramData.Val > 0 ?
                        (Localization.UseRussian ? "����" : "Yes") :
                        (Localization.UseRussian ? "���" : "No");
                else if (signal == 2)
                    return ((int)paramData.Val).ToString();
            }

            return base.ParamDataToStr(signal, paramData);
        }
    }
}