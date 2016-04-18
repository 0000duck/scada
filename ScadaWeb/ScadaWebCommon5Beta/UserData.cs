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
        /// ����� ������ ���-����������
        /// </summary>
        private static readonly AppData AppData = AppData.GetAppData();

        
        /// <summary>
        /// �����������, �������������� �������� ������� ��� ����������
        /// </summary>
        private UserData()
        {
            IpAddress = "";
            SessionID = "";

            ClearUser();
            ClearAppDataRefs();
        }


        /// <summary>
        /// �������� IP-����� ������������
        /// </summary>
        public string IpAddress { get; private set; }

        /// <summary>
        /// �������� ������������� ������
        /// </summary>
        public string SessionID { get; private set; }


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
        /// �������� ����� ������������
        /// </summary>
        public UserRights UserRights { get; private set; }


        /// <summary>
        /// �������� ������ �� ��������� ���-����������
        /// </summary>
        public WebSettings WebSettings { get; private set; }

        /// <summary>
        /// �������� ������ �� ��������� �������������
        /// </summary>
        public ViewSettings ViewSettings { get; private set; }

        /// <summary>
        /// �������� ������ �� ������ ��������
        /// </summary>
        public List<PluginSpec> PluginSpecs { get; private set; }

        /// <summary>
        /// �������� ������ �� ������� ������������ �������������, ���� - ��� ���� �������������
        /// </summary>
        public Dictionary<string, ViewSpec> ViewSpecs { get; private set; }


        /// <summary>
        /// �������� ������ ������������
        /// </summary>
        private void ClearUser()
        {
            UserName = "";
            UserID = 0;
            RoleName = "";
            LoggedOn = false;
            LogonDT = DateTime.MinValue;
            UserRights = null;
        }

        /// <summary>
        /// �������� ������ �� ������� ����� ������ ����������
        /// </summary>
        private void ClearAppDataRefs()
        {
            WebSettings = null;
            ViewSettings = null;
            PluginSpecs = null;
            ViewSpecs = null;
        }

        /// <summary>
        /// �������� ������ �� ������� ����� ������ ����������
        /// </summary>
        private void UpdateAppDataRefs()
        {
            WebSettings = AppData.WebSettings;
            ViewSettings = AppData.ViewSettings;
            PluginSpecs = AppData.PluginSpecs;
            ViewSpecs = AppData.ViewSpecs;
        }


        /// <summary>
        /// ��������� ���� ������������ � �������
        /// </summary>
        /// <remarks>���� ������ ����� null, �� �� �� �����������</remarks>
        public bool Login(string username, string password, out string errMsg)
        {
            username = username == null ? "" : username.Trim();
            int roleID;

            if (AppData.CheckUser(username, password, password != null, out roleID, out errMsg))
            {
                // ���������� ������� ������������
                UserName = username;
                UserID = AppData.DataAccess.GetUserID(username);
                RoleID = roleID;
                RoleName = AppData.DataAccess.GetRoleName(RoleID);
                LoggedOn = true;
                LogonDT = DateTime.Now;

                UpdateAppDataRefs();
                UserRights = new UserRights();
                UserRights.Init(roleID);

                if (password == null)
                {
                    AppData.Log.WriteAction(string.Format(Localization.UseRussian ?
                        "���� � ������� ��� ������: {0} ({1}). IP-�����: {2}" :
                        "Login without a password: {0} ({1}). IP address: {2}", 
                        username, RoleName, IpAddress));
                }
                else
                {
                    AppData.Log.WriteAction(string.Format(Localization.UseRussian ?
                        "���� � �������: {0} ({1}). IP-�����: {2}" :
                        "Login: {0} ({1}). IP address: {2}", 
                        username, RoleName, IpAddress));
                }

                return true;
            }
            else
            {
                Logout();
                AppData.Log.WriteError(string.Format(Localization.UseRussian ?
                    "��������� ������� ����� � �������: {0}{1}. IP-�����: {2}" :
                    "Unsuccessful login attempt: {0}{1}. IP address: {2}",
                    username == "" ? "" : username + " - ", errMsg, IpAddress));
                return false;
            }
        }

        /// <summary>
        /// ��������� ���� ������������ � ������� ��� �������� ������
        /// </summary>
        public bool Login(string username, out string errMsg)
        {
            return Login(username, null, out errMsg);
        }

        /// <summary>
        /// ��������� ����� ������������ �� �������
        /// </summary>
        public void Logout()
        {
            if (LoggedOn)
            {
                AppData.Log.WriteAction(string.Format(Localization.UseRussian ?
                    "����� �� �������: {0}. IP-�����: {1}" :
                    "Logout: {0}. IP address: {1}", UserName, IpAddress));
            }

            ClearUser();
            UpdateAppDataRefs();
        }

        /// <summary>
        /// ���������, ��� ������������ ����� �������. 
        /// ���� ���� �� ��������, �� ������� �� �������� ����� ��� ������� ����������
        /// </summary>
        public void CheckLoggedOn(bool tryToLogin)
        {
            if (!LoggedOn)
            {
                if (tryToLogin)
                {
                    HttpContext httpContext = HttpContext.Current;
                    ScadaWebUtils.CheckHttpContext(httpContext);

                    // ������� ����� � �������������� cookies
                    string username;
                    string alert = "";

                    if (WebSettings.RemEnabled &&
                        AppData.RememberMe.ValidateUser(httpContext, out username, out alert))
                    {
                        Login(username, out alert);
                    }

                    // ������� �� �������� �����
                    if (!LoggedOn)
                    {
                        httpContext.Response.Redirect("~/Login.aspx" +
                            "?return=" + HttpUtility.UrlEncode(httpContext.Request.Url.ToString()) +
                            (alert == "" ? "" : "&alert=" + HttpUtility.UrlEncode(alert)));
                    }
                }
                else
                {
                    throw new ScadaException(WebPhrases.NotLoggedOn);
                }
            }
        }


        /// <summary>
        /// �������� ������ ������������ ����������
        /// </summary>
        /// <remarks>��� ���-���������� ������ ������������ ����������� � ������</remarks>
        public static UserData GetUserData()
        {
            HttpContext httpContext = HttpContext.Current;
            ScadaWebUtils.CheckHttpContext(httpContext);
            HttpSessionState session = httpContext.Session;
            UserData userData = session["UserData"] as UserData;

            if (userData == null)
            {
                // ���������� ������ ���-����������
                AppData.Refresh();

                // �������� ������ ������������
                userData = new UserData();
                session.Add("UserData", userData);

                // ��������� IP-������ � �������������� ������
                userData.IpAddress = httpContext.Request.UserHostAddress;
                userData.SessionID = session.SessionID;

                // ���������� ������ �� ������� ����� ������ ����������
                userData.UpdateAppDataRefs();
            }

            return userData;
        }

        /// <summary>
        /// ��������� ������������ ����� ������������
        /// </summary>
        public static void ValidateUserName(string username)
        {
            if (username == null)
                throw new ArgumentNullException("username", "Username must not be null.");

            if (username == "")
                throw new ArgumentException("Username must not be null or empty.", "username");

            if (username.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
                throw new ArgumentException("Username contains invalid character.", "username"); ;
        }
    }
}