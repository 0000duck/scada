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
 * Summary  : Adapter for reading and writing data tables
 * 
 * Author   : Mikhail Shiryaev
 * Created  : 2005
 * Modified : 2013
 * 
 * --------------------------------
 * Table file structure (version 2)
 * --------------------------------
 * File consists of snapshots, each snapshot has the structure:
 * N               - channel number list length          (UInt16)
 * n0..nN-1        - channel number list                 (N * UInt16)
 * CS              - check sum of the previous data      (UInt16)
 * Time            - snapshot time stamp                 (Double)
 * {Value, Status} - snapshot data                       (N * (Double + Byte))
 * 
 * Channel numbers in the list are unique and sorted in ascending order.
 * If the channel number list equals the previous list, it is skipped and N is set to 0.
 * CS = (UInt16)(N + n0 + ... + nN-1 + 1)
 */

using System;
using System.IO;
using System.Data;

namespace Scada.Data
{
    /// <summary>
    /// Adapter for reading and writing data tables
    /// <para>������� ��� ������ � ������ ������ ������</para>
    /// </summary>
    public class SrezAdapter
    {
        /// <summary>
        /// ����� ������� ������ ������� ������� � ������� ����������
        /// </summary>
        protected static readonly byte[] EmptyCnlNumsBuf = new byte[] { 0x00, 0x00, 0x01, 0x00 };

        /// <summary>
        /// ���������� ������� ������
        /// </summary>
        protected string directory;
        /// <summary>
        /// ������� � �������� �����
        /// </summary>
        protected Stream ioStream;
        /// <summary>
        /// ��� ����� ������� ������
        /// </summary>
        protected string tableName;
        /// <summary>
        /// ������ ��� ����� ������� ������
        /// </summary>
        protected string fileName;
        /// <summary>
        /// ������ � ������ ����������� ����� ���� �� �����
        /// </summary>
        protected bool fileMode;


        /// <summary>
        /// �����������
        /// </summary>
        public SrezAdapter()
        {
            directory = "";
            ioStream = null;
            tableName = "";
            fileName = "";
            fileMode = true;
        }


        /// <summary>
        /// �������� ��� ���������� ���������� ������� ������
        /// </summary>
        public string Directory
        {
            get
            {
                return directory;
            }
            set
            {
                ioStream = null;
                fileMode = true;
                if (directory != value)
                {
                    directory = value;
                    fileName = directory + tableName;
                }
            }
        }

        /// <summary>
        /// �������� ��� ���������� ������� � �������� ����� (������ ����������)
        /// </summary>
        public Stream Stream
        {
            get
            {
                return ioStream;
            }
            set
            {
                directory = "";
                ioStream = value;
                fileName = tableName;
                fileMode = false;
            }
        }

        /// <summary>
        /// �������� ��� ���������� ��� ����� ������� ������
        /// </summary>
        public string TableName
        {
            get
            {
                return tableName;
            }
            set
            {
                if (tableName != value)
                {
                    tableName = value;
                    fileName = directory + tableName;
                }
            }
        }

        /// <summary>
        /// �������� ��� ���������� ������ ��� ����� ������� ������
        /// </summary>
        public string FileName
        {
            get
            {
                return fileName;
            }
            set
            {
                if (fileName != value)
                {
                    directory = Path.GetDirectoryName(value);
                    ioStream = null;
                    tableName = Path.GetFileName(value);
                    fileName = value;
                    fileMode = true;
                }
            }
        }


        /// <summary>
        /// ������� ������ �������� ������ �� ��������� ������
        /// </summary>
        protected void ExtractCnlData(byte[] buf, ref int bufInd, out double cnlVal, out byte cnlStat)
        {
            cnlVal = BitConverter.ToDouble(buf, bufInd);
            cnlStat = buf[bufInd + 8];
            bufInd += 9;
        }

        /// <summary>
        /// �������� ����� �������� ��������� ����� � ������� ����������
        /// </summary>
        protected byte[] GetSrezDescrBuf(SrezTable.SrezDescr srezDescr)
        {
            ushort cnlNumsLen = (ushort)srezDescr.CnlNums.Length;
            byte[] cnlNumsBuf = new byte[cnlNumsLen * 2 + 4];
            cnlNumsBuf[0] = (byte)(cnlNumsLen % 256);
            cnlNumsBuf[1] = (byte)(cnlNumsLen / 256);
            int bufPos = 2;

            for (int i = 0; i < cnlNumsLen; i++)
            {
                ushort cnlNum = (ushort)srezDescr.CnlNums[i];
                cnlNumsBuf[bufPos++] = (byte)(cnlNum % 256);
                cnlNumsBuf[bufPos++] = (byte)(cnlNum / 256);
            }

            cnlNumsBuf[bufPos++] = (byte)(srezDescr.CS % 256);
            cnlNumsBuf[bufPos++] = (byte)(srezDescr.CS / 256);

            return cnlNumsBuf;
        }

        /// <summary>
        /// �������� ����� ������ ����� � ������� ����������
        /// </summary>
        protected byte[] GetCnlDataBuf(SrezTable.CnlData[] cnlData)
        {
            int cnlCnt = cnlData.Length;
            byte[] srezDataBuf = new byte[cnlCnt * 9];

            for (int i = 0, k = 0; i < cnlCnt; i++)
            {
                SrezTable.CnlData data = cnlData[i];
                BitConverter.GetBytes(data.Val).CopyTo(srezDataBuf, k);
                srezDataBuf[k + 8] = (byte)data.Stat;
                k += 9;
            }

            return srezDataBuf;
        }

        /// <summary>
        /// ��������� ������ dest �� ����� ������ FileName
        /// </summary>
        protected void FillObj(object dest)
        {
            Stream stream = null;
            BinaryReader reader = null;
            DateTime fillTime = DateTime.Now;

            SrezTableLight srezTableLight = dest as SrezTableLight;
            DataTable dataTable = dest as DataTable;
            Trend trend = dest as Trend;

            SrezTable srezTable = srezTableLight as SrezTable;
            SrezTableLight.Srez lastStoredSrez = null;

            try
            {
                if (srezTableLight == null && dataTable == null && trend == null)
                    throw new Exception("Destination object is invalid.");

                // ���������� ������� ��� �������� ������
                if (srezTableLight != null)
                {
                    srezTableLight.Clear();
                    srezTableLight.TableName = tableName;

                    if (srezTable != null)
                        srezTable.BeginLoadData();
                }
                else if (dataTable != null)
                {
                    // ������������ ��������� �������
                    dataTable.BeginLoadData();
                    dataTable.DefaultView.Sort = "";

                    if (dataTable.Columns.Count == 0)
                    {
                        dataTable.Columns.Add("DateTime", typeof(DateTime));
                        dataTable.Columns.Add("CnlNum", typeof(int));
                        dataTable.Columns.Add("Val", typeof(double));
                        dataTable.Columns.Add("Stat", typeof(int));
                        dataTable.DefaultView.AllowNew = false;
                        dataTable.DefaultView.AllowEdit = false;
                        dataTable.DefaultView.AllowDelete = false;
                    }
                    else
                    {
                        dataTable.Rows.Clear();
                    }
                }
                else // trend != null
                {
                    trend.Clear();
                    trend.TableName = tableName;
                }

                // ���������� ������� �������
                stream = ioStream == null ?
                    new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite) :
                    ioStream;
                reader = new BinaryReader(stream);

                DateTime date = Arithmetic.ExtractDate(tableName); // ����������� ���� ������
                SrezTable.SrezDescr srezDescr = null;              // �������� �����
                int[] cnlNums = null; // ������ �� ������ ������� ������� �� �������� �����
                while (stream.Position < stream.Length)
                {
                    // ���������� ������ ������� ������� � ��
                    int cnlNumCnt = reader.ReadUInt16();
                    if (cnlNumCnt > 0)
                    {
                        // �������� ������� ������� � ����� ��� ���������� �������� ������
                        int cnlNumSize = cnlNumCnt * 2;
                        byte[] buf = new byte[cnlNumSize];
                        int readSize = reader.Read(buf, 0, cnlNumSize);

                        // �������� �������� ����� � ���������� ������� ������� �� ������ 
                        // � ��������� �� ������������ � ���������������
                        if (readSize == cnlNumSize)
                        {
                            int prevCnlNum = -1;
                            srezDescr = new SrezTable.SrezDescr(cnlNumCnt);
                            cnlNums = srezDescr.CnlNums;
                            for (int i = 0; i < cnlNumCnt; i++)
                            {
                                int cnlNum = BitConverter.ToUInt16(buf, i * 2);
                                if (prevCnlNum >= cnlNum)
                                    throw new Exception("Table is incorrect.");
                                cnlNums[i] = prevCnlNum = cnlNum;
                            }
                            srezDescr.CalcCS();
                        }
                    }
                    else if (srezDescr == null)
                    {
                        throw new Exception("Table is incorrect.");
                    }

                    // ���������� � �������� ��
                    ushort cs = reader.ReadUInt16();
                    bool csOk = cnlNumCnt > 0 ? srezDescr.CS == cs : cs == 1;

                    // ���������� ������ �����
                    int cnlCnt = cnlNums.Length;   // ���������� ������� � �����
                    int srezDataSize = cnlCnt * 9; // ������ ������ �����
                    if (csOk)
                    {
                        long srezPos = stream.Position;
                        double time = reader.ReadDouble();
                        int hour, min, sec;
                        Arithmetic.DecodeTime(time, out hour, out min, out sec);
                        DateTime srezDT = new DateTime(date.Year, date.Month, date.Day, hour, min, sec);

                        // ������������� ������ �����
                        SrezTableLight.Srez srez;
                        if (srezTable != null)
                        {
                            srez = new SrezTable.Srez(srezDT, srezDescr) 
                            { 
                                State = DataRowState.Unchanged,
                                Position = srezPos
                            };
                        }
                        else if (srezTableLight != null)
                        {
                            srez = new SrezTableLight.Srez(srezDT, cnlCnt);
                            cnlNums.CopyTo(srez.CnlNums, 0);
                        }
                        else // srezTableLight == null
                        {
                            srez = null;
                        }

                        // ���������� ������ ������� �������
                        int bufInd = 0;
                        double val;
                        byte stat;
                        if (trend != null)
                        {
                            // ����� ������ ���������� ������ ��� ������
                            int index = Array.BinarySearch<int>(cnlNums, trend.CnlNum);
                            if (index >= 0)
                            {
                                stream.Seek(index * 9, SeekOrigin.Current);
                                byte[] buf = new byte[9];
                                int readSize = reader.Read(buf, 0, 9);
                                if (readSize == 9)
                                {
                                    ExtractCnlData(buf, ref bufInd, out val, out stat);
                                    Trend.Point point = new Trend.Point(srezDT, val, stat);
                                    trend.Points.Add(point);
                                    stream.Seek(srezDataSize - (index + 1) * 9, SeekOrigin.Current);
                                }
                            }
                            else
                            {
                                stream.Seek(srezDataSize, SeekOrigin.Current);
                            }
                        }
                        else
                        {
                            // �������� ������ ����� � ����� ��� ���������� �������� ������
                            byte[] buf = new byte[srezDataSize];
                            int readSize = reader.Read(buf, 0, srezDataSize);

                            // ��������� ������� ������ �� ������
                            if (srezTableLight != null)
                            {
                                for (int i = 0; i < cnlCnt; i++)
                                {
                                    ExtractCnlData(buf, ref bufInd, out val, out stat);

                                    srez.CnlNums[i] = cnlNums[i];
                                    srez.CnlData[i].Val = val;
                                    srez.CnlData[i].Stat = stat;

                                    if (bufInd >= readSize)
                                        break;
                                }

                                srezTableLight.AddSrez(srez);
                                lastStoredSrez = srez;
                            }
                            else // dataTable != null
                            {
                                for (int i = 0; i < cnlCnt; i++)
                                {
                                    ExtractCnlData(buf, ref bufInd, out val, out stat);

                                    DataRow row = dataTable.NewRow();
                                    row["DateTime"] = srezDT;
                                    row["CnlNum"] = cnlNums[i];
                                    row["Val"] = val;
                                    row["Stat"] = stat;
                                    dataTable.Rows.Add(row);

                                    if (bufInd >= readSize)
                                        break;
                                }
                            }
                        }
                    }
                    else
                    {
                        // ���������� ����, ������ ��� ������ ���, ��� ��� ������������� ������ ������� �������
                        stream.Seek(srezDataSize + 8, SeekOrigin.Current);
                    }
                }
            }
            catch (EndOfStreamException)
            {
                // ���������� �������� ��������� �����
            }
            catch
            {
                fillTime = DateTime.MinValue;
                throw;
            }
            finally
            {
                if (fileMode)
                {
                    if (reader != null)
                        reader.Close();
                    if (stream != null)
                        stream.Close();
                }

                if (srezTableLight != null)
                {
                    srezTableLight.LastFillTime = fillTime;
                    if (srezTable != null)
                    {
                        srezTable.LastStoredSrez = (SrezTable.Srez)lastStoredSrez;
                        srezTable.EndLoadData();
                    }
                }
                else if (dataTable != null)
                {
                    dataTable.EndLoadData();
                    dataTable.AcceptChanges();
                    dataTable.DefaultView.Sort = "DateTime, CnlNum";
                }
                else if (trend != null)
                {
                    trend.LastFillTime = fillTime;
                    trend.Sort();
                }
            }
        }


        /// <summary>
        /// ��������� ������� dataTable �� ����� ��� ������
        /// </summary>
        public void Fill(DataTable dataTable)
        {
            FillObj(dataTable);
        }

        /// <summary>
        /// ��������� ������� srezTableLight �� ����� ��� ������
        /// </summary>
        public void Fill(SrezTableLight srezTableLight)
        {
            FillObj(srezTableLight);
        }

        /// <summary>
        /// ��������� ����� trend ������ cnlNum �� ����� ��� ������
        /// </summary>
        public void Fill(Trend trend)
        {
            FillObj(trend);
        }

        /// <summary>
        /// ������� �������, ��������� �� ������ �����, � ����� ��� ������
        /// </summary>
        /// <remarks>��� ������ ������� �������� �����</remarks>
        public void Create(SrezTable.Srez srez, DateTime srezDT)
        {
            if (srez == null)
                throw new ArgumentNullException("srez");

            Stream stream = null;
            BinaryWriter writer = null;

            try
            {
                stream = ioStream == null ?
                   new FileStream(fileName, FileMode.OpenOrCreate, FileAccess.Write, FileShare.ReadWrite) :
                   ioStream;
                writer = new BinaryWriter(stream);

                writer.Write(GetSrezDescrBuf(srez.SrezDescr));
                writer.Write(Arithmetic.EncodeDateTime(srezDT));
                writer.Write(GetCnlDataBuf(srez.CnlData));
                stream.SetLength(stream.Position);
            }
            finally
            {
                if (fileMode)
                {
                    if (writer != null)
                        writer.Close();
                    if (stream != null)
                        stream.Close();
                }
            }
        }

        /// <summary>
        /// �������� ��������� ������� ������ � ���� ��� �����
        /// </summary>
        public void Update(SrezTable srezTable)
        {
            if (srezTable == null)
                throw new ArgumentNullException("srezTable");

            Stream stream = null;
            BinaryWriter writer = null;

            try
            {
                stream = ioStream == null ?
                   new FileStream(fileName, FileMode.OpenOrCreate, FileAccess.Write, FileShare.ReadWrite) :
                   ioStream;
                writer = new BinaryWriter(stream);

                // ������ ��������� ������
                foreach (SrezTable.Srez srez in srezTable.ModifiedSrezList)
                {
                    stream.Seek(srez.Position + 8, SeekOrigin.Begin);
                    writer.Write(GetCnlDataBuf(srez.CnlData));
                }

                // ��������� ������� ������ ����������� ������ � �����, 
                // �������������� ������� ������ � ������ �������������
                SrezTable.Srez lastSrez = srezTable.LastStoredSrez;

                if (lastSrez == null)
                {
                    stream.Seek(0, SeekOrigin.Begin);
                }
                else
                {
                    stream.Seek(0, SeekOrigin.End);
                    long offset = lastSrez.Position + lastSrez.CnlNums.Length * 9 + 8;

                    if (stream.Position < offset)
                    {
                        byte[] buf = new byte[offset - stream.Position];
                        stream.Write(buf, 0, buf.Length);
                    }
                    else
                    {
                        stream.Seek(offset, SeekOrigin.Begin);
                    }
                }

                // ������ ����������� ������
                SrezTable.SrezDescr prevSrezDescr = lastSrez == null ? null : lastSrez.SrezDescr;

                foreach (SrezTable.Srez srez in srezTable.AddedSrezList)
                {
                    // ������ ������� ������� �����
                    if (srez.SrezDescr.Equals(prevSrezDescr))
                        writer.Write(EmptyCnlNumsBuf);
                    else
                        writer.Write(GetSrezDescrBuf(srez.SrezDescr));

                    prevSrezDescr = srez.SrezDescr;

                    // ������ ������ �����
                    srez.Position = stream.Position;
                    writer.Write(Arithmetic.EncodeDateTime(srez.DateTime));
                    writer.Write(GetCnlDataBuf(srez.CnlData));
                    lastSrez = srez;
                }

                // ������������� ��������� ���������� ���������
                srezTable.AcceptChanges();
                srezTable.LastStoredSrez = lastSrez;
            }
            finally
            {
                if (fileMode)
                {
                    if (writer != null)
                        writer.Close();
                    if (stream != null)
                        stream.Close();
                }
            }
        }
    }
}