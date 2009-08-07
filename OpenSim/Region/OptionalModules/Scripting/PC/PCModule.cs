/*
 * Copyright (c) 2009 Takayuki Usui
 * All rights reserved.
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the following conditions
 * are met:
 * 1. Redistributions of source code must retain the above copyright
 *    notice, this list of conditions and the following disclaimer.
 * 2. Redistributions in binary form must reproduce the above copyright
 *    notice, this list of conditions and the following disclaimer in the
 *    documentation and/or other materials provided with the distribution.
 *
 * THIS SOFTWARE IS PROVIDED BY THE DEVELOPERS AND CONTRIBUTORS ``AS IS''
 * AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO,
 * THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR
 * PURPOSE ARE DISCLAIMED.IN NO EVENT SHALL THE FOUNDATION OR CONTRIBUTORS
 * BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR
 * CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF
 * SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR
 * BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY,
 * WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE
 * OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN
 * IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
 */
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
 *     * Neither the name of the OpenSim Project nor the
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
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using OpenMetaverse;
using log4net;
using Nini.Config;
using Nwc.XmlRpc;
using OpenSim.Framework;
using OpenSim.Framework.Communications;
using OpenSim.Region.Framework.Interfaces;
using OpenSim.Region.Framework.Scenes;
using OpenSim.Framework.Servers;
using OpenSim.Region.CoreModules.Framework.InterfaceCommander;

namespace OpenSim.Region.OptionalModules.Scripting.PC
{
    public class PCModule : IRegionModule, ICommandableModule
    {
        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private static readonly Commander m_commander = new Commander("pc");
        private Scene m_scene;
        private IConfigSource m_source;

        public void Initialise(Scene scene, IConfigSource source)
        {
            m_scene = scene;
            m_source = source;
            
            m_scene.EventManager.OnPluginConsole += EventManager_OnPluginConsole;
        }

        public void PostInitialise()
        {
            InitializeCommander();
        }
        
        public void Close()
        {
        }

        public string Name
        {
            get { return "Procedural Creation Module"; }
        }

        public bool IsSharedModule
        {
            get { return false; }
        }
        public ICommander CommandInterface
        {
            get { return m_commander; }
        }

        private void EventManager_OnPluginConsole(string[] args)
        {
            if (args[0] == "pc")
            {
                if (args.Length == 1)
                {
                    m_commander.ProcessConsoleCommand("new", new string[0]);
                    return;
                }
                string[] tmpArgs = new string[args.Length - 2];
                int i;
                for (i = 2; i < args.Length; i++)
                    tmpArgs[i - 2] = args[i];
                m_commander.ProcessConsoleCommand(args[1], tmpArgs);
            }
        }

        private void InitializeCommander()
        {
            Command commandNew = new Command("new", CommandIntentions.COMMAND_HAZARDOUS, CommandNew,
                "Create a new instance of the PC virtual machine");
            Command commandExec = new Command("exec", CommandIntentions.COMMAND_HAZARDOUS, CommandExec,
                "Execute a PC script on a new instance of the PC virtual machine");
            commandExec.AddArgument("filename",
                "The PC script file you wish to load from", "String");
            Command commandLoad = new Command("load", CommandIntentions.COMMAND_HAZARDOUS, CommandLoad,
                "Load a PC application module");
            commandLoad.AddArgument("filename",
                "The PC application module you wish to load", "String");
            commandLoad.AddArgument("debug",
                "Start the application with the debugger", "Boolean");

            m_commander.RegisterCommand("new", commandNew);
            m_commander.RegisterCommand("exec", commandExec);
            m_commander.RegisterCommand("load", commandLoad);
            
            m_scene.RegisterModuleCommander(m_commander);
        }

        private void CommandNew(Object[] args)
        {
            PCVM.ReadEvalLoop(m_scene,m_source);
        }

        private void CommandExec(Object[] args)
        {
            string path = (string)args[0];
            using (StreamReader file = new StreamReader(path))
            {
                PCVM.Load(m_scene, m_source, file);
            }
        }

        private void CommandLoad(Object[] args)
        {
            string path = (string)args[0];
            bool debug = (bool)args[1];
            Assembly assembly;
            Type applicationType;
            IPCApplication application;

            try
            {
                assembly = Assembly.LoadFrom(path);
                applicationType = null;

                foreach (Type pluginType in assembly.GetTypes())
                {
                    if (pluginType.IsPublic)
                    {
                        Type typeInterface = pluginType.GetInterface("IPCApplication", true);

                        if (typeInterface != null)
                        {
                            applicationType = pluginType;
                            break;
                        }
                    }
                }
                if (applicationType != null)
                {
                    application = (IPCApplication)Activator.CreateInstance(applicationType);
                    using (PCVM vm = new PCVM(m_scene, m_source, new PCDict()))
                    {
                        try
                        {
                            Tools.Parser parser = PCVM.MakeParser();
                            application.Initialize(m_scene, m_source, parser, vm);
                            application.Run(debug);
                        }
                        catch
                        {
                        }
                    }
                }
            }
            finally
            {
            }
        }
    }
}
