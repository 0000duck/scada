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
 * Summary  : Trend for fast reading one input channel data
 * 
 * Author   : Mikhail Shiryaev
 * Created  : 2006
 * Modified : 2006
 */

using System;
using System.Collections.Generic;

namespace Scada.Data
{
    /// <summary>
    /// Trend for fast reading one input channel data
    /// <para>����� ��� �������� ������ ������ ������ �������� ������</para>
    /// </summary>
    public class Trend
    {
        /// <summary>
        /// ����� ������
        /// </summary>
        public struct Point : IComparable<Point>
        {
            /// <summary>
            /// �����������
            /// </summary>
            public Point(DateTime dateTime, double val, int stat)
            {
                DateTime = dateTime;
                Val = val;
                Stat = stat;
            }

            /// <summary>
            /// ��������� �����
            /// </summary>
            public DateTime DateTime;
            /// <summary>
            /// ��������
            /// </summary>
            public double Val;
            /// <summary>
            /// ������
            /// </summary>
            public int Stat;

            /// <summary>
            /// �������� ������� ������ � ������ �������� ������ �� ����
            /// </summary>
            public int CompareTo(Point other)
            {
                return DateTime.CompareTo(other.DateTime);
            }
        }

        /// <summary>
        /// ��� �������, � ������� ��������� �����
        /// </summary>
        protected string tableName;
        /// <summary>
        /// ����� ���������� ��������� ����� �������, � ������� ��������� �����
        /// </summary>
        protected DateTime fileModTime;
        /// <summary>
        /// ����� ��������� ��������� ���������� ������
        /// </summary>
        protected DateTime lastFillTime;
        /// <summary>
        /// ����� ������
        /// </summary>
        protected List<Point> points;
        /// <summary>
        /// ����� �������� ������ ������
        /// </summary>
        protected int cnlNum;


        /// <summary>
        /// �����������
        /// </summary>
        protected Trend()
        {
        }

        /// <summary>
        /// �����������
        /// </summary>
        /// <param name="cnlNum">����� �������� ������ ������</param>
        public Trend(int cnlNum)
        {
            tableName = "";
            fileModTime = DateTime.MinValue;
            lastFillTime = DateTime.MinValue;

            points = new List<Point>();
            this.cnlNum = cnlNum;
        }


        /// <summary>
        /// �������� ��� ���������� ��� ����� �������, � ������� ��������� �����
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
        /// �������� ��� ���������� ����� ���������� ��������� ����� �������, � ������� ��������� �����
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
        /// �������� ��� ���������� ����� ��������� ��������� ���������� ������
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
        /// ����� ������
        /// </summary>
        public List<Point> Points
        {
            get
            {
                return points;
            }        
        }

        /// <summary>
        /// ����� �������� ������ ������
        /// </summary>
        public int CnlNum
        {
            get
            {
                return cnlNum;
            }
        }


        /// <summary>
        /// ������������� ����� ������ �� �������
        /// </summary>
        public void Sort()
        {
            points.Sort();
        }

        /// <summary>
        /// �������� �����
        /// </summary>
        public void Clear()
        {
            points.Clear();
        }
    }
}