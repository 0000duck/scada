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
 * Summary  : Output channel properties
 * 
 * Author   : Mikhail Shiryaev
 * Created  : 2008
 * Modified : 2012
 */

using System;

namespace Scada.Client
{
    /// <summary>
    /// Output channel properties
    /// <para>�������� ������ ����������</para>
    /// </summary>
    public class CtrlCnlProps
    {
        /// <summary>
        /// �����������
        /// </summary>
        public CtrlCnlProps()
        {
            CtrlCnlNum = -1;
            CtrlCnlName = "";
            ObjNum = 0;
            ObjName = "";
            KPNum = 0;
            KPName = "";
            CmdTypeID = 0;
            CmdValArr = null;
        }

        /// <summary>
        /// �����������
        /// </summary>
        public CtrlCnlProps(int ctrlCnlNum)
            : this()
        {
            CtrlCnlNum = ctrlCnlNum;
        }


        /// <summary>
        /// �������� ��� ���������� ����� ������ ����������
        /// </summary>
        public int CtrlCnlNum { get; set; }

        /// <summary>
        /// �������� ��� ���������� ������������ ������ ����������
        /// </summary>
        public string CtrlCnlName { get; set; }

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
        /// �������� ��� ���������� ������������� ���� �������
        /// </summary>
        public int CmdTypeID { get; set; }

        /// <summary>
        /// �������� ��� ���������� �������� �������
        /// </summary>
        public string[] CmdValArr { get; set; }
    }
}