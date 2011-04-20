/*
 * Copyright (c) Contributors, http://opensimulator.org/
 * See CONTRIBUTORS.TXT for a full list of copyright holders.
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the following conditions are met:
 *     * Redistributions of source code must retain the above copyright
 *       notice, this list of conditions and the following disclaimer.
 *     * Redistributions in binary form must reproduce the above copyright
 *       notice, this list of conditions and the following disclaimer in the
 *       documentation and/or other materials provided with the distribution.
 *     * Neither the name of the OpenSimulator Project nor the
 *       names of its contributors may be used to endorse or promote products
 *       derived from this software without specific prior written permission.
 *
 * THIS SOFTWARE IS PROVIDED BY THE DEVELOPERS ``AS IS'' AND ANY
 * EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
 * WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
 * DISCLAIMED. IN NO EVENT SHALL THE CONTRIBUTORS BE LIABLE FOR ANY
 * DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
 * (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
 * LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
 * ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
 * (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
 * SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
 */

using log4net;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Nini.Config;
using OpenSim.Framework;
using OpenSim.Framework.Communications;
using OpenSim.Services.Interfaces;
using GridRegion = OpenSim.Services.Interfaces.GridRegion;
using OpenSim.Server.Base;
using OpenMetaverse;

namespace OpenSim.Services.Connectors
{
    public class GridUserServicesConnector : IGridUserService
    {
        private static readonly ILog m_log =
                LogManager.GetLogger(
                MethodBase.GetCurrentMethod().DeclaringType);

        private string m_ServerURI = String.Empty;

        public GridUserServicesConnector()
        {
        }

        public GridUserServicesConnector(string serverURI)
        {
            m_ServerURI = serverURI.TrimEnd('/');
        }

        public GridUserServicesConnector(IConfigSource source)
        {
            Initialise(source);
        }

        public virtual void Initialise(IConfigSource source)
        {
            IConfig gridConfig = source.Configs["GridUserService"];
            if (gridConfig == null)
            {
                m_log.Error("[GRID USER CONNECTOR]: GridUserService missing from OpenSim.ini");
                throw new Exception("GridUser connector init error");
            }

            string serviceURI = gridConfig.GetString("GridUserServerURI",
                    String.Empty);

            if (serviceURI == String.Empty)
            {
                m_log.Error("[GRID USER CONNECTOR]: No Server URI named in section GridUserService");
                throw new Exception("GridUser connector init error");
            }
            m_ServerURI = serviceURI;
        }


        #region IGridUserService


        public GridUserInfo LoggedIn(string userID)
        {
            Dictionary<string, object> sendData = new Dictionary<string, object>();
            //sendData["SCOPEID"] = scopeID.ToString();
            sendData["VERSIONMIN"] = ProtocolVersions.ClientProtocolVersionMin.ToString();
            sendData["VERSIONMAX"] = ProtocolVersions.ClientProtocolVersionMax.ToString();
            sendData["METHOD"] = "loggedin";

            sendData["UserID"] = userID;

            return Get(sendData);

        }

        public bool LoggedOut(string userID, UUID sessionID, UUID region, Vector3 position, Vector3 lookat)
        {
            Dictionary<string, object> sendData = new Dictionary<string, object>();
            //sendData["SCOPEID"] = scopeID.ToString();
            sendData["VERSIONMIN"] = ProtocolVersions.ClientProtocolVersionMin.ToString();
            sendData["VERSIONMAX"] = ProtocolVersions.ClientProtocolVersionMax.ToString();
            sendData["METHOD"] = "loggedout";

            return Set(sendData, userID, region, position, lookat);
        }

        public bool SetHome(string userID, UUID regionID, Vector3 position, Vector3 lookAt)
        {
            Dictionary<string, object> sendData = new Dictionary<string, object>();
            //sendData["SCOPEID"] = scopeID.ToString();
            sendData["VERSIONMIN"] = ProtocolVersions.ClientProtocolVersionMin.ToString();
            sendData["VERSIONMAX"] = ProtocolVersions.ClientProtocolVersionMax.ToString();
            sendData["METHOD"] = "sethome";

            return Set(sendData, userID, regionID, position, lookAt);
        }

        public bool SetLastPosition(string userID, UUID sessionID, UUID regionID, Vector3 position, Vector3 lookAt)
        {
            Dictionary<string, object> sendData = new Dictionary<string, object>();
            //sendData["SCOPEID"] = scopeID.ToString();
            sendData["VERSIONMIN"] = ProtocolVersions.ClientProtocolVersionMin.ToString();
            sendData["VERSIONMAX"] = ProtocolVersions.ClientProtocolVersionMax.ToString();
            sendData["METHOD"] = "setposition";

            return Set(sendData, userID, regionID, position, lookAt);
        }

        public GridUserInfo GetGridUserInfo(string userID)
        {
            Dictionary<string, object> sendData = new Dictionary<string, object>();
            //sendData["SCOPEID"] = scopeID.ToString();
            sendData["VERSIONMIN"] = ProtocolVersions.ClientProtocolVersionMin.ToString();
            sendData["VERSIONMAX"] = ProtocolVersions.ClientProtocolVersionMax.ToString();
            sendData["METHOD"] = "getgriduserinfo";

            sendData["UserID"] = userID;

            return Get(sendData);
        }

        #endregion

        protected bool Set(Dictionary<string, object> sendData, string userID, UUID regionID, Vector3 position, Vector3 lookAt)
        {
            sendData["UserID"] = userID;
            sendData["RegionID"] = regionID.ToString();
            sendData["Position"] = position.ToString();
            sendData["LookAt"] = lookAt.ToString();

            string reqString = ServerUtils.BuildQueryString(sendData);
            // m_log.DebugFormat("[GRID USER CONNECTOR]: queryString = {0}", reqString);
            try
            {
                string reply = SynchronousRestFormsRequester.MakeRequest("POST",
                        m_ServerURI + "/griduser",
                        reqString);
                if (reply != string.Empty)
                {
                    Dictionary<string, object> replyData = ServerUtils.ParseXmlResponse(reply);

                    if (replyData.ContainsKey("result"))
                    {
                        if (replyData["result"].ToString().ToLower() == "success")
                            return true;
                        else
                            return false;
                    }
                    else
                        m_log.DebugFormat("[GRID USER CONNECTOR]: SetPosition reply data does not contain result field");

                }
                else
                    m_log.DebugFormat("[GRID USER CONNECTOR]: SetPosition received empty reply");
            }
            catch (Exception e)
            {
                m_log.DebugFormat("[GRID USER CONNECTOR]: Exception when contacting grid user server: {0}", e.Message);
            }

            return false;
        }

        protected GridUserInfo Get(Dictionary<string, object> sendData)
        {
            string reqString = ServerUtils.BuildQueryString(sendData);
            // m_log.DebugFormat("[GRID USER CONNECTOR]: queryString = {0}", reqString);
            try
            {
                string reply = SynchronousRestFormsRequester.MakeRequest("POST",
                        m_ServerURI + "/griduser",
                        reqString);
                if (reply != string.Empty)
                {
                    Dictionary<string, object> replyData = ServerUtils.ParseXmlResponse(reply);
                    GridUserInfo guinfo = null;

                    if ((replyData != null) && replyData.ContainsKey("result") && (replyData["result"] != null))
                    {
                        if (replyData["result"] is Dictionary<string, object>)
                            guinfo = new GridUserInfo((Dictionary<string, object>)replyData["result"]);
                    }

                    return guinfo;

                }
                else
                    m_log.DebugFormat("[GRID USER CONNECTOR]: Loggedin received empty reply");
            }
            catch (Exception e)
            {
                m_log.DebugFormat("[GRID USER CONNECTOR]: Exception when contacting grid user server: {0}", e.Message);
            }

            return null;

        }

    }
}
