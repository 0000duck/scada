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
 * Summary  : Input channel properties
 * 
 * Author   : Mikhail Shiryaev
 * Created  : 2005
 * Modified : 2012
 */

using System;

namespace Scada.Client
{
	/// <summary>
    /// Input channel properties
    /// <para>�������� �������� ������</para>
	/// </summary>
	public class CnlProps : IComparable
	{
        /// <summary>
        /// �����������
        /// </summary>
        public CnlProps()
        {
            CnlNum = -1;
            CnlName = "";
            ObjNum = 0;
            ObjName = "";
            KPNum = 0;
            KPName = "";
            IconFileName = "";
            ParamName = "";
            ShowNumber = true;
            DecDigits = 3;
            UnitArr = null;
            CtrlCnlNum = 0;
            EvSound = false;
        }

        /// <summary>
        /// �����������
        /// </summary>
        public CnlProps(int cnlNum)
            : this()
        {
            CnlNum = cnlNum;
        }


        /// <summary>
        /// �������� ��� ���������� ����� �������� ������
        /// </summary>
        public int CnlNum { get; set; }

		/// <summary>
        /// �������� ��� ���������� ������������ �������� ������
        /// </summary>
		public string CnlName { get; set; }

        /// <summary>
        /// �������� ��� ���������� ����� �������
        /// </summary>
        public int ObjNum { get; set; }

        /// <summary>
        /// �������� ��� ���������� ������������ �������
        /// </summary>
        public string ObjName { get; set; }

        /// <summary>
        /// �������� ��� ���������� ����� ��
        /// </summary>
        public int KPNum { get; set; }

        /// <summary>
        /// �������� ��� ���������� ������������ ��
        /// </summary>
        public string KPName { get; set; }

		/// <summary>
        /// �������� ��� ���������� �������� ��� ����� ������
        /// </summary>
        public string IconFileName { get; set; }

		/// <summary>
        /// �������� ��� ���������� ������������ ���������
        /// </summary>
        public string ParamName { get; set; }

		/// <summary>
        /// �������� ��� ���������� ������� ������ �������� ������ ��� �����
        /// </summary>
        public bool ShowNumber { get; set; }

		/// <summary>
        /// �������� ��� ���������� ���������� ������ ������� ����� ��� ������ ��������
        /// </summary>
        public int DecDigits { get; set; }

        /// <summary>
        /// �������� ��� ���������� �����������
        /// </summary>
        public string[] UnitArr { get; set; }

        /// <summary>
        /// �������� ��� ���������� ����� ������ ����������
        /// </summary>
        public int CtrlCnlNum { get; set; }

        /// <summary>
        /// �������� ��� ���������� ������� ����� �������
        /// </summary>
        public bool EvSound { get; set; }


		#region IComparable Members
        /// <summary>
        /// Compares the current instance with another object of the same type
        /// </summary>
		public int CompareTo(object obj)
		{
            return CnlNum.CompareTo((int)obj);
		}
		#endregion
	}
}
