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
 * Summary  : Snapshot table for fast read data access
 * 
 * Author   : Mikhail Shiryaev
 * Created  : 2006
 * Modified : 2013
 */

using System;
using System.Collections.Generic;

namespace Scada.Data
{
    /// <summary>
    /// Snapshot table for fast read data access
    /// <para>������� ������ ��� �������� ������� � ������ �� ������</para>
    /// </summary>
    public class SrezTableLight
    {
        /// <summary>
        /// ������ �������� ������
        /// </summary>
        public struct CnlData
        {
            /// <summary>
            /// ������ ������ �������� ������
            /// </summary>
            public static readonly CnlData Empty = new CnlData(0.0, 0);

            /// <summary>
            /// �����������
            /// </summary>
            /// <param name="val">��������</param>
            /// <param name="stat">������</param>
            public CnlData(double val, int stat)
                : this()
            {
                Val = val;
                Stat = stat;
            }

            /// <summary>
            /// �������� ��� ���������� ��������
            /// </summary>
            public double Val { get; set; }
            /// <summary>
            /// �������� ��� ���������� ������
            /// </summary>
            public int Stat { get; set; }
        }

        /// <summary>
        /// ���� ������ ������� ������� �� ����������� ������ �������
        /// </summary>
        public class Srez : IComparable<Srez>
        {
            /// <summary>
            /// �����������
            /// </summary>
            protected Srez()
            {
            }
            /// <summary>
            /// �����������
            /// </summary>
            /// <param name="dateTime">��������� ����� �����</param>
            /// <param name="cnlCnt">���������� ������� �������</param>
            public Srez(DateTime dateTime, int cnlCnt)
            {
                if (cnlCnt <= 0)
                    throw new ArgumentOutOfRangeException("cnlCnt");

                DateTime = dateTime;
                CnlNums = new int[cnlCnt];
                CnlData = new CnlData[cnlCnt];
            }
            
            
            /// <summary>
            /// �������� ��������� ����� �����
            /// </summary>
            public DateTime DateTime { get; protected set; }
            /// <summary>
            /// �������� ������ ������� �����, ������������� �� �����������
            /// </summary>
            public int[] CnlNums { get; protected set; }
            /// <summary>
            /// �������� ������ �����, ��������������� ��� ������� �������
            /// </summary>
            public CnlData[] CnlData { get; protected set; }

            /// <summary>
            /// �������� ������ �������� ������ �� ������
            /// </summary>
            public int GetCnlIndex(int cnlNum)
            {
                return Array.BinarySearch<int>(CnlNums, cnlNum);
            }
            /// <summary>
            /// �������� ������ �������� ������ �� ������
            /// </summary>
            public bool GetCnlData(int cnlNum, out CnlData cnlData)
            {
                int index = Array.BinarySearch<int>(CnlNums, cnlNum);
                if (index < 0)
                {
                    cnlData = SrezTableLight.CnlData.Empty;
                    return false;
                }
                else
                {
                    cnlData = CnlData[index];
                    return true;
                }
            }
            /// <summary>
            /// �������� ������� ������ � ������ �������� ������ �� ����
            /// </summary>
            public int CompareTo(Srez other)
            {
                return DateTime.CompareTo(other == null ? DateTime.MinValue : other.DateTime);
            }
        }

        /// <summary>
        /// ��� �������
        /// </summary>
        protected string tableName;
        /// <summary>
        /// ����� ���������� ��������� ����� �������
        /// </summary>
        protected DateTime fileModTime;
        /// <summary>
        /// ����� ��������� ��������� ���������� �������
        /// </summary>
        protected DateTime lastFillTime;


        /// <summary>
        /// �����������
        /// </summary>
        public SrezTableLight()
        {
            tableName = "";
            fileModTime = DateTime.MinValue;
            lastFillTime = DateTime.MinValue;
            SrezList = new SortedList<DateTime, Srez>();
        }


        /// <summary>
        /// �������� ��� ���������� ��� ����� �������
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
                    fileModTime = DateTime.MinValue;
                    lastFillTime = DateTime.MinValue;
                }
            }
        }

        /// <summary>
        /// �������� ��� ���������� ����� ���������� ��������� ����� �������
        /// </summary>
        public DateTime FileModTime
        {
            get
            {
                return fileModTime;
            }
            set
            {
                fileModTime = value;
            }
        }

        /// <summary>
        /// �������� ��� ���������� ����� ���������� ��������� ���������� �������
        /// </summary>
        public DateTime LastFillTime
        {
            get
            {
                return lastFillTime;
            }
            set
            {
                fileModTime = value;
            }
        }

        /// <summary>
        /// �������� ������������� ������ ������
        /// </summary>
        public SortedList<DateTime, Srez> SrezList { get; protected set; }


        /// <summary>
        /// �������� ���� � �������
        /// </summary>
        /// <remarks>���� � ������� ��� ���������� ���� � �������� ������ �������, 
        /// �� ���������� ������ ����� �� ����������</remarks>
        public virtual bool AddSrez(Srez srez)
        {
            if (srez == null)
                throw new ArgumentNullException("srez");

            if (SrezList.ContainsKey(srez.DateTime))
            {
                return false;
            }
            else
            {
                SrezList.Add(srez.DateTime, srez);
                return true;
            }
        }

        /// <summary>
        /// �������� ���� �� ����������� �����
        /// </summary>
        public Srez GetSrez(DateTime dateTime)
        {
            Srez srez;
            return SrezList.TryGetValue(dateTime, out srez) ? srez : null;
        }

        /// <summary>
        /// �������� ������� ������
        /// </summary>
        public virtual void Clear()
        {
            SrezList.Clear();
        }
    }
}