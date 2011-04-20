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
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Security;
using System.Text;
using log4net;
using Nini.Config;
using OpenMetaverse;
using OpenSim.Framework;
using OpenSim.Framework.Console;
using OpenSim.Region.CoreModules.Framework.InterfaceCommander;
using OpenSim.Region.Framework.Interfaces;
using OpenSim.Region.Framework.Scenes;

namespace OpenSim.Region.CoreModules.World.Estate
{
    /// <summary>
    /// Estate management console commands.
    /// </summary>
    public class EstateManagementCommands
    {
        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        
        protected EstateManagementModule m_module;
        
        protected Commander m_commander = new Commander("estate");
        
        public EstateManagementCommands(EstateManagementModule module)
        {
            m_module = module;
        }
        
        public void Initialise()
        {            
            m_log.DebugFormat("[ESTATE MODULE]: Setting up estate commands for region {0}", m_module.Scene.RegionInfo.RegionName);
            
            m_module.Scene.AddCommand(m_module, "set terrain texture",
                               "set terrain texture <number> <uuid> [<x>] [<y>]",
                               "Sets the terrain <number> to <uuid>, if <x> or <y> are specified, it will only " +
                               "set it on regions with a matching coordinate. Specify -1 in <x> or <y> to wildcard" +
                               " that coordinate.",
                               consoleSetTerrainTexture);

            m_module.Scene.AddCommand(m_module, "set terrain heights",
                               "set terrain heights <corner> <min> <max> [<x>] [<y>]",
                               "Sets the terrain texture heights on corner #<corner> to <min>/<max>, if <x> or <y> are specified, it will only " +
                               "set it on regions with a matching coordinate. Specify -1 in <x> or <y> to wildcard" +
                               " that coordinate. Corner # SW = 0, NW = 1, SE = 2, NE = 3.",
                               consoleSetTerrainHeights);            
            
            Command showCommand 
                = new Command("show", CommandIntentions.COMMAND_STATISTICAL, ShowEstatesCommand, "Shows all estates on the simulator.");

            m_commander.RegisterCommand("show", showCommand);

            m_module.Scene.RegisterModuleCommander(m_commander);            
            
            m_module.Scene.EventManager.OnPluginConsole += EventManagerOnPluginConsole;
        }       
        
        public void Close()
        {
            m_module.Scene.EventManager.OnPluginConsole -= EventManagerOnPluginConsole;
            m_module.Scene.UnregisterModuleCommander(m_commander.Name);            
        }
        
        /// <summary>
        /// Processes commandline input. Do not call directly.
        /// </summary>
        /// <param name="args">Commandline arguments</param>
        protected void EventManagerOnPluginConsole(string[] args)
        {
            if (args[0] == "estate")
            {
                if (args.Length == 1)
                {
                    m_commander.ProcessConsoleCommand("help", new string[0]);
                    return;
                }

                string[] tmpArgs = new string[args.Length - 2];
                int i;
                for (i = 2; i < args.Length; i++)
                    tmpArgs[i - 2] = args[i];

                m_commander.ProcessConsoleCommand(args[1], tmpArgs);
            }
        }            
        
        protected void consoleSetTerrainTexture(string module, string[] args)
        {
            string num = args[3];
            string uuid = args[4];
            int x = (args.Length > 5 ? int.Parse(args[5]) : -1);
            int y = (args.Length > 6 ? int.Parse(args[6]) : -1);

            if (x == -1 || m_module.Scene.RegionInfo.RegionLocX == x)
            {
                if (y == -1 || m_module.Scene.RegionInfo.RegionLocY == y)
                {
                    int corner = int.Parse(num);
                    UUID texture = UUID.Parse(uuid);

                    m_log.Debug("[ESTATEMODULE]: Setting terrain textures for " + m_module.Scene.RegionInfo.RegionName +
                                string.Format(" (C#{0} = {1})", corner, texture));

                    switch (corner)
                    {
                        case 0:
                            m_module.Scene.RegionInfo.RegionSettings.TerrainTexture1 = texture;
                            break;
                        case 1:
                            m_module.Scene.RegionInfo.RegionSettings.TerrainTexture2 = texture;
                            break;
                        case 2:
                            m_module.Scene.RegionInfo.RegionSettings.TerrainTexture3 = texture;
                            break;
                        case 3:
                            m_module.Scene.RegionInfo.RegionSettings.TerrainTexture4 = texture;
                            break;
                    }
                    
                    m_module.Scene.RegionInfo.RegionSettings.Save();
                    m_module.TriggerRegionInfoChange();
                    m_module.sendRegionInfoPacketToAll();
                }
            }
        }
 
        protected void consoleSetTerrainHeights(string module, string[] args)
        {
            string num = args[3];
            string min = args[4];
            string max = args[5];
            int x = (args.Length > 6 ? int.Parse(args[6]) : -1);
            int y = (args.Length > 7 ? int.Parse(args[7]) : -1);

            if (x == -1 || m_module.Scene.RegionInfo.RegionLocX == x)
            {
                if (y == -1 || m_module.Scene.RegionInfo.RegionLocY == y)
                {
                    int corner = int.Parse(num);
                    float lowValue = float.Parse(min, Culture.NumberFormatInfo);
                    float highValue = float.Parse(max, Culture.NumberFormatInfo);

                    m_log.Debug("[ESTATEMODULE]: Setting terrain heights " + m_module.Scene.RegionInfo.RegionName +
                                string.Format(" (C{0}, {1}-{2}", corner, lowValue, highValue));

                    switch (corner)
                    {
                        case 0:
                            m_module.Scene.RegionInfo.RegionSettings.Elevation1SW = lowValue;
                            m_module.Scene.RegionInfo.RegionSettings.Elevation2SW = highValue;
                            break;
                        case 1:
                            m_module.Scene.RegionInfo.RegionSettings.Elevation1NW = lowValue;
                            m_module.Scene.RegionInfo.RegionSettings.Elevation2NW = highValue;
                            break;
                        case 2:
                            m_module.Scene.RegionInfo.RegionSettings.Elevation1SE = lowValue;
                            m_module.Scene.RegionInfo.RegionSettings.Elevation2SE = highValue;
                            break;
                        case 3:
                            m_module.Scene.RegionInfo.RegionSettings.Elevation1NE = lowValue;
                            m_module.Scene.RegionInfo.RegionSettings.Elevation2NE = highValue;
                            break;
                    }
                    
                    m_module.Scene.RegionInfo.RegionSettings.Save();
                    m_module.TriggerRegionInfoChange();
                    m_module.sendRegionHandshakeToAll();
                }
            }
        }     
        
        protected void ShowEstatesCommand(Object[] args)
        {
            StringBuilder report = new StringBuilder();  
            RegionInfo ri = m_module.Scene.RegionInfo;
            EstateSettings es = ri.EstateSettings;
            
            report.AppendFormat("Estate information for region {0}\n", ri.RegionName);            
            report.AppendFormat(
                "{0,-20} {1,-7} {2,-20}\n",
                "Estate Name",
                "ID",
                "Owner");
            
            report.AppendFormat(
                "{0,-20} {1,-7} {2,-20}\n", 
                es.EstateName, es.EstateID, m_module.UserManager.GetUserName(es.EstateOwner));
            
            MainConsole.Instance.Output(report.ToString());
        }         
    }
}