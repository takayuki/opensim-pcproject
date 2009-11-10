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
using System.Reflection;
using log4net;
using Nini.Config;
using OpenSim.Framework;
using OpenSim.Region.Framework.Interfaces;
using OpenSim.Region.Framework.Scenes;
using Erlang.NET;
using Tools;

namespace OpenSim.Region.OptionalModules.Scripting.PC
{
    internal class KernelActor : OtpActor
    {
        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private Scene m_scene;
        private IConfigSource m_source;
        
        private OtpNode m_node;
        private OtpActorMbox m_mbox;
        
        public KernelActor(Scene scene, IConfigSource source, OtpNode node, OtpActorMbox mbox)
            : base(mbox)
        {
            m_scene = scene;
            m_source = source;
            m_node = node;
            m_mbox = mbox;
        }

        public override IEnumerator<OtpActor.Continuation> GetEnumerator()
        {
            while (true)
            {
                OtpMsg msg = null;
                OtpErlangPid sender = null;
                OtpErlangTuple reply = null;

                yield return (delegate(OtpMsg received) { msg = received; });

                try
                {
                    OtpErlangTuple t = (OtpErlangTuple)msg.getMsg();
                    sender = (OtpErlangPid)t.elementAt(0);
                    string instr = ((OtpErlangAtom)t.elementAt(1)).ToString();
                    if (instr == "echo")
                    {
                        OtpErlangObject[] v = new OtpErlangObject[3];
                        v[0] = sender;
                        v[1] = new OtpErlangAtom("ok");
                        v[2] = t.elementAt(2);
                        reply = new OtpErlangTuple(v);
                    }
                    else if (instr == "new")
                    {
                        OtpActorMbox newmbox = (OtpActorMbox)m_node.createMbox(false);
                        PCVMActor newactor = new PCVMActor(m_scene, m_source, m_node, newmbox);

                        m_node.react(newactor);

                        OtpErlangObject[] v = new OtpErlangObject[3];
                        v[0] = sender;
                        v[1] = new OtpErlangAtom("ok");
                        v[2] = newmbox.Self;
                        reply = new OtpErlangTuple(v);
                    }
                }
                catch (Exception e)
                {
                    m_log.Debug("[Distributed PC] Invalid message format: " + msg.getMsg());

                    OtpErlangObject[] v = new OtpErlangObject[3];
                    v[0] = sender;
                    v[1] = new OtpErlangAtom("error");
                    v[2] = new OtpErlangString(e.Message);
                    reply = new OtpErlangTuple(v);
                }
                finally
                {
                    if (sender != null)
                    {
                        m_mbox.send(sender, reply);
                    }
                }
            }
        }
    }

    internal class PCVMActor : OtpActor
    {
        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private Scene m_scene;
        private IConfigSource m_source;

        private OtpNode m_node;
        private OtpActorMbox m_mbox = null;

        private PCVM m_vm;
        private PCDict m_dict;

        public PCVMActor(Scene scene, IConfigSource source, OtpNode node, OtpActorMbox mbox)
            : base(mbox)
        {
            m_scene = scene;
            m_source = source;
            m_node = node;
            m_mbox = mbox;

            m_dict = new PCDict();
            m_vm = new PCVM(m_scene, m_source, m_dict);
        }

        private OtpErlangObject ErlangObjectFromPCVMObject(PCObj input)
        {
            if (input is PCFloat)
                return new OtpErlangFloat(((PCFloat)input).val);
            if (input is PCInt)
                return new OtpErlangInt(((PCInt)input).val);
            if (input is PCBool)
                return new OtpErlangBoolean(((PCBool)input).val);
            if (input is PCSym)
                return new OtpErlangAtom(((PCSym)input).val);
            if (input is PCStr)
                return new OtpErlangString(((PCStr)input).val);
            if (input is PCMark)
                return new OtpErlangString(((PCMark)input).ToString());
            if (input is PCUUID)
                return new OtpErlangString(((PCUUID)input).ToString());
            if (input is PCVector2)
            {
                OtpErlangObject[] items = new OtpErlangObject[2];
                items[0] = new OtpErlangFloat(((PCVector2)input).val.X);
                items[1] = new OtpErlangFloat(((PCVector2)input).val.Y);
                return new OtpErlangTuple(items);
            }
            if (input is PCVector3)
            {
                OtpErlangObject[] items = new OtpErlangObject[3];
                items[0] = new OtpErlangFloat(((PCVector3)input).val.X);
                items[1] = new OtpErlangFloat(((PCVector3)input).val.Y);
                items[2] = new OtpErlangFloat(((PCVector3)input).val.Z);
                return new OtpErlangTuple(items);
            }
            if (input is PCVector4)
            {
                OtpErlangObject[] items = new OtpErlangObject[4];
                items[0] = new OtpErlangFloat(((PCVector4)input).val.X);
                items[1] = new OtpErlangFloat(((PCVector4)input).val.Y);
                items[2] = new OtpErlangFloat(((PCVector4)input).val.Z);
                items[3] = new OtpErlangFloat(((PCVector4)input).val.W);
                return new OtpErlangTuple(items);
            }
            if (input is PCNull)
                return new OtpErlangAtom(((PCNull)input).ToString());
            if (input is PCOp)
                return new OtpErlangAtom(((PCOp)input).ToString());
            if (input is PCFun)
                return new OtpErlangAtom(((PCFun)input).ToString());
            if (input is PCDict)
            {
                PCDict dict = (PCDict)input;
                OtpErlangObject[] items = new OtpErlangObject[dict.Dict.Count*2];

                int i = 0;
                foreach (KeyValuePair<string, PCObj> pair in dict.Dict)
                {
                    items[i * 2] = new OtpErlangAtom(pair.Key);
                    items[i * 2 + 1] = ErlangObjectFromPCVMObject(pair.Value);
                    i++;
                }
                return new OtpErlangList(items);
            }
            if (input is PCArray)
            {
                PCArray array = (PCArray)input;
                OtpErlangObject[] items = new OtpErlangObject[array.Length];
                for (int i = 0; i < array.Length; i++)
                {
                    items[i] = ErlangObjectFromPCVMObject(array[i]);
                }
                return new OtpErlangList(items);
            }
            if (input is PCSceneObjectPart)
                return ErlangObjectFromPCVMObject(new PCUUID(((PCSceneObjectPart)input).val.UUID));
            if (input is PCSceneSnapshot)
            {
                PCSceneSnapshot.SnapshotItem[] array = ((PCSceneSnapshot)input).val;
                OtpErlangObject[] items = new OtpErlangObject[array.Length];
                for (int i = 0; i < array.Length; i++)
                {
                    items[i] = ErlangObjectFromPCVMObject(array[i].PCSceneObjectPart);
                }
                return new OtpErlangList(items);
            }

            return new OtpErlangAtom("nonobject");
        }

        private OtpErlangObject ErlangObjectFromPCVMObject(PCObj[] input)
        {
            OtpErlangObject[] vals = new OtpErlangObject[input.Length];

            for (int i = 0; i <input.Length; i++)
            {
                vals[i] = ErlangObjectFromPCVMObject(input[i]);
            }
            return new OtpErlangList(vals);
        }

        public override IEnumerator<OtpActor.Continuation> GetEnumerator()
        {
            m_log.Info("[Distributed PC] New PCVM is ready: " + m_mbox.Self);

            while (true)
            {
                OtpMsg msg = null;
                
                yield return (delegate(OtpMsg received) { msg = received; });

                OtpErlangObject obj = msg.getMsg();
                
                if (obj is OtpErlangAtom)
                {
                    string atom = ((OtpErlangAtom)obj).atomValue();
                    if (!String.IsNullOrEmpty(atom) && atom == "noconnection")
                    {
                        break;
                    }
                }

                OtpErlangPid sender = null;
                OtpErlangObject[] reply = new OtpErlangObject[3];

                try
                {
                    OtpErlangTuple t = (OtpErlangTuple)obj;
                    sender = (OtpErlangPid)t.elementAt(0);
                    
                    reply[0] = sender;
                    reply[1] = new OtpErlangAtom("ok");

                    string instr = ((OtpErlangAtom)t.elementAt(1)).ToString();
                    
                    if (instr == "load")
                    {
                        Parser parser = PCVM.MakeParser();
                        string script = ((OtpErlangString)t.elementAt(2)).stringValue();
                        bool debug = ((OtpErlangAtom)t.elementAt(3)).boolValue();

                        SYMBOL ast = parser.Parse(script);

                        m_vm.Call((Compiler.ExpPair)ast);

                        if (debug)
                        {
                            reply[2] = new OtpErlangAtom("continue");
                        }
                        else
                        {
                            Queue<PCObj> popped = new Queue<PCObj>();
                            try
                            {
                                m_vm.Finish(popped);
                            }
                            finally
                            {
                                reply = new OtpErlangObject[] {
                                    reply[0],
                                    reply[1],
                                    new OtpErlangAtom("finished"),
                                    ErlangObjectFromPCVMObject(popped.ToArray())
                                };
                            }
                        }
                    }
                    else if (instr == "step")
                    {
                        bool cont = true;
                        Queue<PCObj> popped = new Queue<PCObj>();
                        try
                        {
                            cont = m_vm.Step(popped);
                        }
                        finally
                        {
                            reply = new OtpErlangObject[] {
                                reply[0],
                                reply[1],
                                new OtpErlangAtom(cont ? "continue" : "finished"),
                                ErlangObjectFromPCVMObject(popped.ToArray())
                            };
                        }
                    }
                    else if (instr == "finish")
                    {
                        Queue<PCObj> popped = new Queue<PCObj>();
                        try
                        {
                            m_vm.Finish(popped);
                        }
                        finally
                        {
                            reply = new OtpErlangObject[] {
                                reply[0],
                                reply[1],
                                new OtpErlangAtom("finished"),
                                ErlangObjectFromPCVMObject(popped.ToArray())
                            };
                        }
                    }
                    else if (instr == "echo")
                    {
                        reply[2] = t.elementAt(2);
                    }
                    else if (instr == "exit")
                    {
                        reply[2] = new OtpErlangAtom("bye");
                        break;
                    }
                }
                catch (Exception e)
                {
                    m_log.Debug("[Distributed PC] Invalid message format: " + msg.getMsg());

                    reply[1] = new OtpErlangAtom("error");
                    reply[2] = new OtpErlangString(e.Message);
                }
                finally
                {
                    if (sender != null)
                    {
                        m_mbox.send(sender, new OtpErlangTuple(reply));
                    }
                }
            }

            m_log.Info("[Distributed PC] Delete PCVM instance: " + m_mbox.Self);
            m_vm.Dispose();
        }
    }

    public class DistributedPCModule : IRegionModule
    {
        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private Scene m_scene;
        private IConfigSource m_source;
        
        private OtpNode m_node = null;
        private OtpActorMbox m_mbox;
        private OtpActor m_kernel;

        public void Initialise(Scene scene, IConfigSource source)
        {
            m_scene = scene;
            m_source = source;

            try
            {
                m_node = new OtpNode("pc");
            }
            catch (Exception e)
            {
                m_log.ErrorFormat("[Distributed PC] Failed to create an Erlang Node: {0}", e.Message); 
            }
        }

        public void PostInitialise()
        {
            if (m_node != null)
            {
                m_mbox = (OtpActorMbox)m_node.createMbox("kernel", false);
                m_kernel = new KernelActor(m_scene, m_source, m_node, m_mbox);
                m_node.react(m_kernel);
            }
        }

        public void Close()
        {
            if (m_node != null)
            {
                m_node.close();
            }
        }

        public string Name
        {
            get { return "Distributed Procedural Creation Module"; }
        }

        public bool IsSharedModule
        {
            get { return false; }
        }
    }
}
