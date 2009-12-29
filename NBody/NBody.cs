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
using System;
using System.Collections.Generic;
using System.Reflection;
using System.IO;
using Erlang.NET;
using GASS.CUDA;
using GASS.CUDA.Types;

namespace NBody
{
    public class NBody : IDisposable
    {
        public static byte[] GetResource(string fullname)
        {
            Assembly asm = Assembly.GetExecutingAssembly();

            using (Stream s = asm.GetManifestResourceStream(fullname))
            {
                using (StreamReader r = new StreamReader(s))
                {
                    return System.Text.Encoding.ASCII.GetBytes(r.ReadToEnd());
                }
            }
        }

        private static byte[] m_nbody_kernel = GetResource("NBody.Resources.nbody_kernel.cubin");

        private readonly CUDA m_CUDA;
        private readonly float m_DeltaTime;
        private readonly float m_Damping;
        private readonly int m_NumBodies;
        private CUfunction m_IntegrateBodies;
        private CUdeviceptr m_SofteningSquared;
        private int m_Current = 0;
        private Float4[][] h_Pos;
        private Float4[][] h_Vel;
        private CUdeviceptr[] d_Pos;
        private CUdeviceptr[] d_Vel;
        
        private Float4[] HostOldPos
        {
            get { return h_Pos[m_Current]; }
        }

        private Float4[] HostOldVel
        {
            get { return h_Vel[m_Current]; }
        }

        private Float4[] HostNewPos
        {
            get { return h_Pos[1 - m_Current]; }
        }

        private Float4[] HostNewVel
        {
            get { return h_Vel[1 - m_Current]; }
        }

        private CUdeviceptr DeviceOldPos
        {
            get { return d_Pos[m_Current]; }
        }

        private CUdeviceptr DeviceOldVel
        {
            get { return d_Vel[m_Current]; }
        }

        private CUdeviceptr DeviceNewPos
        {
            get { return d_Pos[1 - m_Current]; }
        }

        private CUdeviceptr DeviceNewVel
        {
            get { return d_Vel[1 - m_Current]; }
        }

        private void Swap()
        {
            m_Current = 1 - m_Current;
        }
        
        public NBody(CUDA CUDA, float DeltaTime, float Damping, int NumBodies)
        {
            m_CUDA = CUDA;
            m_DeltaTime = DeltaTime;
            m_Damping = Damping;
            m_NumBodies = NumBodies;
        }

        private float dot(Float4 a, Float4 b)
        {
            return a.x * b.x + a.y * b.y + a.z * b.z;
        }

        public void Initialize()
        {
            float softeningSquared = 0.00125f;
            Random random = new Random();

            m_CUDA.LoadModule(m_nbody_kernel);
            m_IntegrateBodies = m_CUDA.GetModuleFunction("IntegrateBodies");
            m_SofteningSquared = m_CUDA.GetModuleGlobal("softeningSquared");

            h_Pos = new Float4[2][] { new Float4[m_NumBodies], new Float4[m_NumBodies] };
            h_Vel = new Float4[2][] { new Float4[m_NumBodies], new Float4[m_NumBodies] };
            d_Pos = new CUdeviceptr[2] { m_CUDA.Allocate<Float4>(HostOldPos), m_CUDA.Allocate<Float4>(HostNewPos) };
            d_Vel = new CUdeviceptr[2] { m_CUDA.Allocate<Float4>(HostOldVel), m_CUDA.Allocate<Float4>(HostNewVel) };

            float scale = 3.0f;
            float vscale = scale * 1.0f;

            for (int i = 0; i < HostOldPos.Length; i++)
            {
            recalc:
                HostOldPos[i].x = (float)(random.NextDouble() * 2 - 1.0);
                HostOldPos[i].y = (float)(random.NextDouble() * 2 - 1.0);
                HostOldPos[i].z = (float)(random.NextDouble() * 2 - 1.0);
                HostOldPos[i].w = 1.0f;

                if (dot(HostOldPos[i], HostOldPos[i]) > 1.0f)
                    goto recalc;

                HostOldPos[i].x *= scale;
                HostOldPos[i].y *= scale;
                HostOldPos[i].z *= scale;
            }

            for (int i = 0; i < HostOldVel.Length; i++)
            {
            recalc:
                HostOldVel[i].x = (float)(random.NextDouble() * 2 - 1.0);
                HostOldVel[i].y = (float)(random.NextDouble() * 2 - 1.0);
                HostOldVel[i].z = (float)(random.NextDouble() * 2 - 1.0);
                HostOldVel[i].w = 1.0f;
                if (dot(HostOldVel[i], HostOldVel[i]) > 1.0f)
                    goto recalc;

                HostOldPos[i].x *= vscale;
                HostOldPos[i].y *= vscale;
                HostOldPos[i].z *= vscale;
            }

            m_CUDA.CopyHostToDevice<Float4>(DeviceOldPos, HostOldPos);
            m_CUDA.CopyHostToDevice<Float4>(DeviceOldVel, HostOldVel);
            m_CUDA.CopyHostToDevice<float>(m_SofteningSquared, new float[] { softeningSquared });
        }

        public void Dispose()
        {
            m_CUDA.Free(DeviceNewPos);
            m_CUDA.Free(DeviceNewVel);
            m_CUDA.Free(DeviceOldPos);
            m_CUDA.Free(DeviceOldVel);
        }

        public void Update(ulong frame)
        {
            int num_thread = 256;

            m_CUDA.SetParameter(m_IntegrateBodies, 0, (uint)DeviceNewPos.Pointer);
            m_CUDA.SetParameter(m_IntegrateBodies, 4, (uint)DeviceNewVel.Pointer);
            m_CUDA.SetParameter(m_IntegrateBodies, 8, (uint)DeviceOldPos.Pointer);
            m_CUDA.SetParameter(m_IntegrateBodies, 12, (uint)DeviceOldVel.Pointer);
            m_CUDA.SetParameter(m_IntegrateBodies, 16, m_DeltaTime);
            m_CUDA.SetParameter(m_IntegrateBodies, 20, m_Damping);
            m_CUDA.SetParameter(m_IntegrateBodies, 24, (uint)m_NumBodies);
            m_CUDA.SetParameterSize(m_IntegrateBodies, 28);

            m_CUDA.SetFunctionBlockShape(m_IntegrateBodies, num_thread, 1, 1);
            m_CUDA.SetFunctionSharedSize(m_IntegrateBodies, (uint)(sizeof(float) * 8 * num_thread));

            m_CUDA.Launch(m_IntegrateBodies, 1, 1);

            CUResult result = m_CUDA.LastError;

            m_CUDA.CopyDeviceToHost(DeviceNewPos, HostNewPos);
            m_CUDA.CopyDeviceToHost(DeviceNewVel, HostNewVel);
        }

        public static OtpErlangTuple Load(OtpMbox mbox, OtpErlangPid pid, string script)
        {
            OtpErlangObject body;
            OtpErlangBoolean debug = new OtpErlangBoolean(false);
            OtpErlangObject message;

            if (script.Length <= 65535)
            {
                body = new OtpErlangString(script);
            }
            else
            {
                body = new OtpErlangBinary(System.Text.Encoding.GetEncoding("iso-8859-1").GetBytes(script));
            }
            message = new OtpErlangTuple(new OtpErlangObject[] { mbox.Self, new OtpErlangAtom("load"), body, debug });

            mbox.send(pid, message);
            OtpErlangTuple reply = (OtpErlangTuple)mbox.receive();
            Console.WriteLine("Load: {0}", reply);

            return reply;
        }

        public static void Main(string[] args)
        {
            OtpNode node = new OtpNode("gen");
            OtpMbox mbox = node.createMbox(true);
            OtpErlangObject message = new OtpErlangTuple(new OtpErlangObject[] { mbox.Self, new OtpErlangAtom("new") });

            mbox.send("kernel", "pc@3di0050d", message);
            OtpErlangTuple reply = (OtpErlangTuple)mbox.receive();

            OtpErlangPid self = (OtpErlangPid)reply.elementAt(0);
            OtpErlangAtom ok = (OtpErlangAtom)reply.elementAt(1);
            OtpErlangPid pid = (OtpErlangPid)reply.elementAt(2);

            Console.WriteLine("New: {0}", ok);
            if (ok.ToString() != "ok")
            {
                return;
            }

            mbox.link(pid);

            using (CUDA cuda = new CUDA(0, true))
            {
                float deltaTime = 0.1f;
                int nextTickCount;

                using (NBody nbody = new NBody(cuda, deltaTime, 1.0f, 32))
                {
                    string script = String.Empty;

                    nbody.Initialize();

                    script += String.Format("<128,128,50> translate\n");
                    script += String.Format("/C {{moveto createsphere dup <1,1,1> setsize dup show }} def\n");
                    
                    for (int i = 0; i < nbody.HostOldPos.Length; i++)
                    {
                        Float4 pos = nbody.HostOldPos[i];
                        script += String.Format("<{0},{1},{2}> C /b{3} exch def\n", pos.x, pos.y, pos.z, i);
                    }

                    Load(mbox, pid, script);
                    script = String.Empty;

                    nextTickCount = System.Environment.TickCount;
                    for (ulong frame = 0; frame < 300; frame++)
                    {
                        while (System.Environment.TickCount < nextTickCount);
                        nextTickCount = nextTickCount + (int)(deltaTime * 1000);

                        nbody.Update(0);
                        nbody.Swap();

                        for (int i = 0; i < nbody.HostOldPos.Length; i++)
                        {
                            Float4 pos = nbody.HostOldPos[i];
                            script += String.Format("b{3} <{0},{1},{2}> setposition \n", pos.x, pos.y, pos.z, i);
                        }

                        Load(mbox, pid, script);
                        script = String.Empty;
                    }
                }
            }

            Console.WriteLine("Hit return key to continue");
            Console.ReadLine();

            mbox.send(pid, new OtpErlangTuple(new OtpErlangObject[] { mbox.Self, new OtpErlangAtom("exit") }));
            reply = (OtpErlangTuple)mbox.receive();

            mbox.close();
            node.close();
        }
    }
}
