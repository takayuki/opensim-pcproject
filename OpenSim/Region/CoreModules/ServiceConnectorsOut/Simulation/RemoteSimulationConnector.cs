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

using System;
using System.Collections;
using System.IO;
using System.Net;
using System.Reflection;
using System.Text;
using log4net;
using Nini.Config;
using OpenMetaverse;
using OpenMetaverse.StructuredData;
using OpenSim.Framework;
using OpenSim.Framework.Communications;
using OpenSim.Region.Framework.Interfaces;
using OpenSim.Region.Framework.Scenes;
using OpenSim.Region.Framework.Scenes.Serialization;
using OpenSim.Services.Interfaces;
using OpenSim.Services.Connectors.Simulation;
using GridRegion = OpenSim.Services.Interfaces.GridRegion;

namespace OpenSim.Region.CoreModules.ServiceConnectorsOut.Simulation
{
    public class RemoteSimulationConnectorModule : ISharedRegionModule, ISimulationService
    {
        private bool initialized = false;
        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        protected bool m_enabled = false;
        protected Scene m_aScene;
        // RemoteSimulationConnector does not care about local regions; it delegates that to the Local module
        protected LocalSimulationConnectorModule m_localBackend;
        protected SimulationServiceConnector m_remoteConnector;

        protected bool m_safemode;
        protected IPAddress m_thisIP;

        #region IRegionModule

        public virtual void Initialise(IConfigSource config)
        {

            IConfig moduleConfig = config.Configs["Modules"];
            if (moduleConfig != null)
            {
                string name = moduleConfig.GetString("SimulationServices", "");
                if (name == Name)
                {
                    //IConfig userConfig = config.Configs["SimulationService"];
                    //if (userConfig == null)
                    //{
                    //    m_log.Error("[AVATAR CONNECTOR]: SimulationService missing from OpenSim.ini");
                    //    return;
                    //}

                    m_remoteConnector = new SimulationServiceConnector();

                    m_enabled = true;

                    m_log.Info("[SIMULATION CONNECTOR]: Remote simulation enabled");
                }
            }
        }

        public virtual void PostInitialise()
        {
        }

        public virtual void Close()
        {
        }

        public void AddRegion(Scene scene)
        {
            if (!m_enabled)
                return;

            if (!initialized)
            {
                InitOnce(scene);
                initialized = true;
            }
            InitEach(scene);
        }

        public void RemoveRegion(Scene scene)
        {
            if (m_enabled)
            {
                m_localBackend.RemoveScene(scene);
                scene.UnregisterModuleInterface<ISimulationService>(this);
            }
        }

        public void RegionLoaded(Scene scene)
        {
            if (!m_enabled)
                return;
        }

        public Type ReplaceableInterface
        {
            get { return null; }
        }

        public virtual string Name
        {
            get { return "RemoteSimulationConnectorModule"; }
        }

        protected virtual void InitEach(Scene scene)
        {
            m_localBackend.Init(scene);
            scene.RegisterModuleInterface<ISimulationService>(this);
        }

        protected virtual void InitOnce(Scene scene)
        {
            m_localBackend = new LocalSimulationConnectorModule();
            m_aScene = scene;
            //m_regionClient = new RegionToRegionClient(m_aScene, m_hyperlinkService);
            m_thisIP = Util.GetHostFromDNS(scene.RegionInfo.ExternalHostName);
        }

        #endregion /* IRegionModule */

        #region IInterregionComms

        public IScene GetScene(ulong handle)
        {
            return m_localBackend.GetScene(handle);
        }

        public ISimulationService GetInnerService()
        {
            return m_localBackend;
        }

        /**
         * Agent-related communications
         */

        public bool CreateAgent(GridRegion destination, AgentCircuitData aCircuit, uint teleportFlags, out string reason)
        {
            if (destination == null)
            {
                reason = "Given destination was null";
                m_log.DebugFormat("[REMOTE SIMULATION CONNECTOR]: CreateAgent was given a null destination");
                return false;
            }

            // Try local first
            if (m_localBackend.CreateAgent(destination, aCircuit, teleportFlags, out reason))
                return true;

            // else do the remote thing
            if (!m_localBackend.IsLocalRegion(destination.RegionID))
            {
                return m_remoteConnector.CreateAgent(destination, aCircuit, teleportFlags, out reason);
            }
            return false;
        }

        public bool UpdateAgent(GridRegion destination, AgentData cAgentData)
        {
            if (destination == null)
                return false;

            // Try local first
            if (m_localBackend.IsLocalRegion(destination.RegionHandle))
                return m_localBackend.UpdateAgent(destination, cAgentData);

            return m_remoteConnector.UpdateAgent(destination, cAgentData);
        }

        public bool UpdateAgent(GridRegion destination, AgentPosition cAgentData)
        {
            if (destination == null)
                return false;

            // Try local first
            if (m_localBackend.IsLocalRegion(destination.RegionHandle))
                return m_localBackend.UpdateAgent(destination, cAgentData);

            return m_remoteConnector.UpdateAgent(destination, cAgentData);
        }

        public bool RetrieveAgent(GridRegion destination, UUID id, out IAgentData agent)
        {
            agent = null;

            if (destination == null)
                return false;

            // Try local first
            if (m_localBackend.RetrieveAgent(destination, id, out agent))
                return true;

            // else do the remote thing
            if (!m_localBackend.IsLocalRegion(destination.RegionHandle))
                return m_remoteConnector.RetrieveAgent(destination, id, out agent);

            return false;

        }

        public bool QueryAccess(GridRegion destination, UUID id, Vector3 position, out string version, out string reason)
        {
            reason = "Communications failure";
            version = "Unknown";
            if (destination == null)
                return false;

            // Try local first
            if (m_localBackend.QueryAccess(destination, id, position, out version, out reason))
                return true;

            // else do the remote thing
            if (!m_localBackend.IsLocalRegion(destination.RegionID))
                return m_remoteConnector.QueryAccess(destination, id, position, out version, out reason);

            return false;

        }

        public bool ReleaseAgent(UUID origin, UUID id, string uri)
        {
            // Try local first
            if (m_localBackend.ReleaseAgent(origin, id, uri))
                return true;

            // else do the remote thing
            if (!m_localBackend.IsLocalRegion(origin))
                return m_remoteConnector.ReleaseAgent(origin, id, uri);

            return false;
        }


        public bool CloseAgent(GridRegion destination, UUID id)
        {
            if (destination == null)
                return false;

            // Try local first
            if (m_localBackend.CloseAgent(destination, id))
                return true;

            // else do the remote thing
            if (!m_localBackend.IsLocalRegion(destination.RegionHandle))
                return m_remoteConnector.CloseAgent(destination, id);
            
            return false;
        }

        /**
         * Object-related communications
         */

        public bool CreateObject(GridRegion destination, ISceneObject sog, bool isLocalCall)
        {
            if (destination == null)
                return false;

            // Try local first
            if (m_localBackend.CreateObject(destination, sog, isLocalCall))
            {
                //m_log.Debug("[REST COMMS]: LocalBackEnd SendCreateObject succeeded");
                return true;
            }

            // else do the remote thing
            if (!m_localBackend.IsLocalRegion(destination.RegionHandle))
                return m_remoteConnector.CreateObject(destination, sog, isLocalCall);

            return false;
        }

        public bool CreateObject(GridRegion destination, UUID userID, UUID itemID)
        {
            // Not Implemented
            return false;
        }

        #endregion /* IInterregionComms */

    }
}
