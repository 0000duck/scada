/*
 * Copyright 2016 Mikhail Shiryaev
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
 * Module   : ScadaWebCommon
 * Summary  : Application user data
 * 
 * Author   : Mikhail Shiryaev
 * Created  : 2007
 * Modified : 2016
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Web;
using System.Web.SessionState;
using Scada.Client;
using Utils;
using Scada.Web.Plugins;

namespace Scada.Web
{
    /// <summary>
    /// Application user data
    /// <para>������ ������������ ����������</para>
    /// </summary>
    /// <remarks>Inheritance is impossible because class is shared by different modules
    /// <para>������������ ����������, �.�. ����� ��������� ������������ ���������� ��������</para></remarks>
    public sealed class UserData
    {
        /// <summary>
        /// �����������, �������������� �������� ������� ��� ����������
        /// </summary>
        private UserData()
        {
            Clear();
        }


        /// <summary>
        /// �������� ��� ������������
        /// </summary>
        public string UserName { get; private set; }

        /// <summary>
        /// �������� ������������� ������������ � ���� ������������
        /// </summary>
        public int UserID { get; private set; }

        /// <summary>
        /// �������� ������������� ���� ������������
        /// </summary>
        public int RoleID { get; private set; }

        /// <summary>
        /// �������� ������������ ���� ������������
        /// </summary>
        public string RoleName { get; private set; }

        /// <summary>
        /// �������� �������, �������� �� ���� ������������ � �������
        /// </summary>
        public bool LoggedOn { get; private set; }

        /// <summary>
        /// �������� ���� � ����� ����� ������������ � �������
        /// </summary>
        public DateTime LogonDT { get; private set; }

        /// <summary>
        /// �������� IP-����� ������������
        /// </summary>
        public string IpAddress { get; private set; }
        

        /// <summary>
        /// �������� ������ ������������
        /// </summary>
        private void Clear()
        {
            UserName = "";
            UserID = 0;
            RoleName = "";
            LoggedOn = false;
            LogonDT = DateTime.MinValue;
            IpAddress = "";
        }


        /// <summary>
        /// ��������� ���� ������������ � �������
        /// </summary>
        /// <remarks>���� ������ ����� null, �� �� �� �����������</remarks>
        public bool Login(string login, string password, out string errMsg)
        {
            errMsg = "Not implemented";
            return false;
        }

        /// <summary>
        /// ��������� ���� ������������ � ������� ��� �������� ������
        /// </summary>
        public bool Login(string login)
        {
            string errMsg;
            return Login(login, null, out errMsg);
        }

        /// <summary>
        /// ��������� ������ ������������ � ������������ ����� ������
        /// </summary>
        public void Logout()
        {
            Clear();
        }


        /// <summary>
        /// �������� ������ ������������ ����������
        /// </summary>
        /// <remarks>��� ���-���������� ������ ������������ ����������� � ������</remarks>
        public static UserData GetUserData()
        {
            ScadaWebUtils.CheckSessionExists();
            HttpSessionState session = HttpContext.Current.Session;
            UserData userData = session["UserData"] as UserData;

            if (userData == null)
            {
                // �������� ������ ������������
                userData = new UserData();
                session.Add("UserData", userData);

                // ��������� IP-������
                userData.IpAddress = HttpContext.Current.Request.UserHostAddress;
            }

            return userData;
        }
    }
}