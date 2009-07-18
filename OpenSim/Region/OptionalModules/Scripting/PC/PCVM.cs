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
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using log4net;
using Nini.Config;
using OpenMetaverse;
using Tools;
using OpenSim.Framework;
using OpenSim.Framework.Communications;
using OpenSim.Framework.Servers;
using OpenSim.Region.Framework.Interfaces;
using OpenSim.Region.Framework.Scenes;

namespace OpenSim.Region.OptionalModules.Scripting.PC
{
    public class PCEmptyStackException : Exception
    {
        public PCEmptyStackException() :
            base("empty stack") { }
    }

    public class PCTypeCheckException : Exception
    {
        public PCTypeCheckException() :
            base("wrong type operand(s)") { }
    }

    public class PCNotFoundException : Exception
    {
        public PCNotFoundException(string name) :
            base(String.Format("{0} is not found", name)) { }
    }

    public class PCExitWithoutLoop : Exception
    {
        public PCExitWithoutLoop() :
            base("exit operator not within loop") { }
    }

    public class PCNotImplementedException : Exception
    {
        public PCNotImplementedException(string name) :
            base(String.Format("{0} is not implemented", name)) { }
    }

    public delegate bool PCOperator();

    public abstract class PCObj
    {
        public PCObj() { }
        public abstract override string ToString();
    }

    public abstract class PCConst : PCObj
    {
        public PCConst() { }
        public abstract float ToFloat();
        public abstract int ToInt();
    }

    public class PCFloat : PCConst
    {
        public float val;
        public PCFloat(float val) { this.val = val; }
        public override float ToFloat() { return val; }
        public override int ToInt() { return (int)val; }
        public override string ToString() { return val.ToString(); }
    }

    public class PCInt : PCConst
    {
        public int val;
        public PCInt(int val) { this.val = val; }
        public override float ToFloat() { return (float)val; }
        public override int ToInt() { return val; }
        public override string ToString() { return val.ToString(); }
    }
    
    public class PCBool : PCObj
    {
        public bool val;
        public PCBool(bool val) { this.val = val; }
        public override string ToString() { return val.ToString(); }
    }

    public class PCSym : PCObj
    {
        public string val;
        public PCSym(string val) { this.val = val; }
        public override string ToString() { return "/" + val.ToString(); }
    }

    public class PCStr : PCObj
    {
        public string val;
        public PCStr(string val) { this.val = val; }
        public override string ToString() { return "\"" + val.ToString() + "\""; }
    }

    public class PCMark : PCObj
    {
        public string val;
        public PCMark() { }
        public override string ToString() { return "["; }
    }

    public class PCUUID : PCObj
    {
        public UUID val;
        public PCUUID(UUID val) { this.val = val; }
        public override string ToString() { return val.ToString(); }
    }

    public class PCVector2 : PCObj
    {
        public Vector2 val;
        public PCVector2(Vector2 val) { this.val = val; }
        public PCVector2(float x, float y) { val = new Vector2(x, y); }
        public override string ToString() { return val.ToString(); }
    }

    public class PCVector3 : PCObj
    {
        public Vector3 val;
        public PCVector3(Vector3 val) { this.val = val; }
        public PCVector3(float x, float y, float z) { val = new Vector3(x, y, z); }
        public override string ToString() { return val.ToString(); }
    }

    public class PCVector4 : PCObj
    {
        public Vector4 val;
        public PCVector4(Vector4 val) { this.val = val; }
        public PCVector4(float x, float y, float z, float w)
        {
            val = new Vector4(x, y, z, w);
        }
        public override string ToString() { return val.ToString(); }
    }

    public class PCNull : PCObj
    {
        public PCNull() { }
        public override string ToString() { return "null"; }
    }
    
    public class PCOp : PCObj
    {
        public PCOperator op;
        public PCOp(PCOperator op) { this.op = op; }
        public override string ToString() { return "<op>"; }
    }

    public class PCFun : PCObj
    {
        public Compiler.ExpPair exp;
        public PCFun(Compiler.ExpPair exp) { this.exp = exp; }
        public override string ToString() { return "<fun>"; }
    }

    public class PCDict : PCObj
    {
        private Dictionary<string, PCObj> dict;

        public PCDict()
        {
            this.dict = new Dictionary<string, PCObj>();
        }

        public PCObj this[string name]
        {
            get { return dict[name]; }
            set { dict[name] = value; }
        }

        public bool ContainsKey(string key)
        {
            return dict.ContainsKey(key);
        }

        public void Add(string key, PCObj val)
        {
            dict.Add(key, val);
        }

        public override string ToString()
        {
            string s = "{";
            bool f = false;
            foreach (string key in dict.Keys)
            {
                if (f) s += " ";
                else f = true;
                s += String.Format("{0}: {1}", key, dict[key].ToString());
            }
            s += "}";
            return s;
        }
    }

    public class PCArray : PCObj
    {
        public List<PCObj> val;

        public PCArray()
        {
            this.val = new List<PCObj>();
        }

        public PCArray(PCObj[] array)
        {
            this.val = new List<PCObj>();
            foreach (PCObj o in array)
            {
                val.Add(o);
            }
        }

        public int Length { get { return val.Count; } }

        public PCObj this[int idx]
        {
            get { return val[idx]; }
            set { val[idx] = value; }
        }

        public void Add(PCObj mem)
        {
            val.Add(mem);
        }
        
        public override string ToString()
        {
            string s = "[";
            bool f = false;
            foreach (PCObj o in val)
            {
                if (f) s += " ";
                else f = true;
                s += o.ToString();
            }
            s += "]";
            return s;
        }
    }

    public class PCSceneObjectPart : PCObj
    {
        public SceneObjectPart var;
        public PCSceneObjectPart(SceneObjectPart var) { this.var = var; }
        public override string ToString()
        {
            return "<" + var.ToString() + "," + var.RotationOffset.ToString() + ">";
        }
    }
    
    public partial class PCVM : IDisposable
    {
        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private Scene m_scene;
        private IConfigSource m_source;

        private class Pair<T>
        {
            public T hd;
            public Pair<T> tl;
            public Pair(T hd, Pair<T> tl) { this.hd = hd; this.tl = tl; }
        }

        private Stack<PCObj> Stack;
        private Pair<Compiler.ExpPair> Frame;
        private Pair<Pair<Compiler.ExpPair>> TraceLoop;
        private Compiler.ExpPair NextInstr;
        private PCDict[] m_dict = new PCDict[2];
        private Stack<PCDict> m_dictionary_stack;
        private PCDict m_userdict;
        private Stack<GraphicState> m_graphicstate_stack;
        private System.Random m_random;

        private PCDict SystemDict
        {
            get { return m_dict[1]; }
            set { m_dict[1] = value; }
        }

        private PCDict UserDict
        {
            get { return m_dict[0]; }
            set { m_dict[0] = value; }
        }

        private PCDict CurrentDict
        {
            get
            {
                if (m_dictionary_stack.Count == 0)
                {
                    return UserDict;
                }
                else
                {
                    return m_dictionary_stack.Peek();
                }
            }
        }

        private PCObj LookupDict(string name)
        {
            if (m_dictionary_stack.Count != 0)
            {
                foreach (PCDict dict in m_dictionary_stack)
                {
                    if (dict.ContainsKey(name))
                        return dict[name];
                }
            }
            foreach (PCDict dict in m_dict)
            {
                if (dict.ContainsKey(name))
                    return dict[name];
            }
            return null;
        }

        private GraphicState CurrentGraphicState
        {
            get { return m_graphicstate_stack.Peek(); }
        }

        public PCVM(Scene scene, IConfigSource source, PCDict user)
        {
            m_scene = scene;
            m_source = source;
            m_userdict = user;
            Init(true);
            InitBanner();
        }

        public void Init(bool fullinit)
        {
            if (!fullinit)
            {
                Frame = null;
                TraceLoop = null;
                NextInstr = null;
            }
            else
            {
                Stack = new Stack<PCObj>();
                Frame = null;
                TraceLoop = null;
                NextInstr = null;
                UserDict = m_userdict;
                SystemDict = new PCDict();
                m_dictionary_stack = new Stack<PCDict>();
                m_graphicstate_stack = new Stack<GraphicState>();
                m_graphicstate_stack.Push(new GraphicState());
                m_random = new System.Random();
                PostInit(SystemDict);
            }
        }

        private bool OpNop()
        {
            return true;
        }

        private bool OpNull()
        {
            Stack.Push(new PCNull());
            return true;
        }

        private bool OpPstack()
        {
            DumpStack(true);
            return true;
        }

        private bool OpClear()
        {
            Stack.Clear();
            return true;
        }

        private bool OpDup()
        {
            try
            {
                PCObj o = Stack.Pop();
                Stack.Push(o);
                Stack.Push(o);
            }
            catch (InvalidOperationException)
            {
                throw new PCEmptyStackException();
            }
            return true;
        }

        private bool OpExch()
        {
            try
            {
                PCObj o0 = Stack.Pop();
                PCObj o1 = Stack.Pop();
                Stack.Push(o0);
                Stack.Push(o1);
            }
            catch (InvalidOperationException)
            {
                throw new PCEmptyStackException();
            }
            return true;
        }

        private bool OpPop()
        {
            try
            {
                Stack.Pop();
            }
            catch (InvalidOperationException)
            {
                throw new PCEmptyStackException();
            }
            return true;
        }

        private bool OpRoll()
        {
            PCObj[] os = new PCObj[2];

            for (int i = 0; i < os.Length; i++)
            {
                try
                {
                    os[i] = Stack.Pop();
                }
                catch (InvalidOperationException)
                {
                    throw new PCEmptyStackException();
                }
                if (!(os[i] is PCConst))
                {
                    Stack.Push(os[i]);
                    throw new PCTypeCheckException();
                }
            }

            int j = ((PCConst)os[0]).ToInt();
            int n = ((PCConst)os[1]).ToInt();
            if (n < 0)
            {
                Stack.Push(os[1]);
                Stack.Push(os[0]);
                throw new PCTypeCheckException();
            }
            PCObj[] copy = new PCObj[n];

            for (int i = 0; i < n; i++)
            {
                try
                {
                    copy[i] = Stack.Pop();
                }
                catch (InvalidOperationException)
                {
                    throw new PCEmptyStackException();
                }
            }
            while (j < 0)
                j += n;
            for (int i = (n + j - 1); j <= i; i--)
                Stack.Push(copy[i % n]);
            return true;
        }

        private bool OpNot()
        {
            PCObj o;

            try
            {
                o = Stack.Pop();
            }
            catch (InvalidOperationException)
            {
                throw new PCEmptyStackException();
            }
            if (!(o is PCBool))
            {
                Stack.Push(o);
                throw new PCTypeCheckException();
            }
            bool val = !(((PCBool)o).val);
            Stack.Push(new PCBool(val));
            return true;
        }

        private bool OpNeg()
        {
            PCObj o;

            try
            {
                o = Stack.Pop();
            }
            catch (InvalidOperationException)
            {
                throw new PCEmptyStackException();
            }
            if (!(o is PCConst))
            {
                Stack.Push(o);
                throw new PCTypeCheckException();
            }
            if (o is PCInt)
            {
                int val = -(((PCInt)o).val);
                Stack.Push(new PCInt(val));
            }
            else
            {
                float val = -(((PCConst)o).ToFloat());
                Stack.Push(new PCFloat(val));
            }
            return true;
        }

        private bool OpAnd()
        {
            PCObj[] os = new PCObj[2];

            for (int i = 0; i < os.Length; i++)
            {
                try
                {
                    os[i] = Stack.Pop();
                }
                catch (InvalidOperationException)
                {
                    throw new PCEmptyStackException();
                }
                if (!(os[i] is PCBool))
                {
                    Stack.Push(os[i]);
                    throw new PCTypeCheckException();
                }
            }
            bool val = ((PCBool)os[1]).val & ((PCBool)os[0]).val;
            Stack.Push(new PCBool(val));
            return true;
        }

        private bool OpOr()
        {
            PCObj[] os = new PCObj[2];

            for (int i = 0; i < os.Length; i++)
            {
                try
                {
                    os[i] = Stack.Pop();
                }
                catch (InvalidOperationException)
                {
                    throw new PCEmptyStackException();
                }
                if (!(os[i] is PCBool))
                {
                    Stack.Push(os[i]);
                    throw new PCTypeCheckException();
                }
            }
            bool val = ((PCBool)os[1]).val | ((PCBool)os[0]).val;
            Stack.Push(new PCBool(val));
            return true;
        }

        private bool OpXor()
        {
            PCObj[] os = new PCObj[2];

            for (int i = 0; i < os.Length; i++)
            {
                try
                {
                    os[i] = Stack.Pop();
                }
                catch (InvalidOperationException)
                {
                    throw new PCEmptyStackException();
                }
                if (!(os[i] is PCBool))
                {
                    Stack.Push(os[i]);
                    throw new PCTypeCheckException();
                }
            }
            bool val = ((PCBool)os[1]).val ^ ((PCBool)os[0]).val;
            Stack.Push(new PCBool(val));
            return true;
        }

        private bool OpIsNull()
        {
            PCObj o;

            try
            {
                o = Stack.Pop();
            }
            catch (InvalidOperationException)
            {
                throw new PCEmptyStackException();
            }
            if (o is PCNull)
            {
                Stack.Push(o);
                Stack.Push(new PCBool(true));
            }
            else
            {
                Stack.Push(o);
                Stack.Push(new PCBool(false));
            }
            return true;
        }

        private bool OpEq()
        {
            PCObj[] os = new PCObj[2];

            for (int i = 0; i < os.Length; i++)
            {
                try
                {
                    os[i] = Stack.Pop();
                }
                catch (InvalidOperationException)
                {
                    throw new PCEmptyStackException();
                }
                if (!(os[i] is PCConst))
                {
                    Stack.Push(os[i]);
                    throw new PCTypeCheckException();
                }
            }
            if ((os[0] is PCInt) && (os[1] is PCInt))
            {
                bool val = ((PCInt)os[1]).val == ((PCInt)os[0]).val;
                Stack.Push(new PCBool(val));
            }
            else
            {
                bool val = ((PCConst)os[1]).ToFloat() == ((PCConst)os[0]).ToFloat();
                Stack.Push(new PCBool(val));
            }
            return true;
        }

        private bool OpNe()
        {
            PCObj[] os = new PCObj[2];

            for (int i = 0; i < os.Length; i++)
            {
                try
                {
                    os[i] = Stack.Pop();
                }
                catch (InvalidOperationException)
                {
                    throw new PCEmptyStackException();
                }
                if (!(os[i] is PCConst))
                {
                    Stack.Push(os[i]);
                    throw new PCTypeCheckException();
                }
            }
            if ((os[0] is PCInt) && (os[1] is PCInt))
            {
                bool val = ((PCInt)os[1]).val != ((PCInt)os[0]).val;
                Stack.Push(new PCBool(val));
            }
            else
            {
                bool val = ((PCConst)os[1]).ToFloat() != ((PCConst)os[0]).ToFloat();
                Stack.Push(new PCBool(val));
            }
            return true;
        }

        private bool OpGt()
        {
            PCObj[] os = new PCObj[2];

            for (int i = 0; i < os.Length; i++)
            {
                try
                {
                    os[i] = Stack.Pop();
                }
                catch (InvalidOperationException)
                {
                    throw new PCEmptyStackException();
                }
                if (!(os[i] is PCConst))
                {
                    Stack.Push(os[i]);
                    throw new PCTypeCheckException();
                }
            }
            if ((os[0] is PCInt) && (os[1] is PCInt))
            {
                bool val = ((PCInt)os[1]).val > ((PCInt)os[0]).val;
                Stack.Push(new PCBool(val));
            }
            else
            {
                bool val = ((PCConst)os[1]).ToFloat() > ((PCConst)os[0]).ToFloat();
                Stack.Push(new PCBool(val));
            }
            return true;
        }

        private bool OpGe()
        {
            PCObj[] os = new PCObj[2];

            for (int i = 0; i < os.Length; i++)
            {
                try
                {
                    os[i] = Stack.Pop();
                }
                catch (InvalidOperationException)
                {
                    throw new PCEmptyStackException();
                }
                if (!(os[i] is PCConst))
                {
                    Stack.Push(os[i]);
                    throw new PCTypeCheckException();
                }
            }
            if ((os[0] is PCInt) && (os[1] is PCInt))
            {
                bool val = ((PCInt)os[1]).val >= ((PCInt)os[0]).val;
                Stack.Push(new PCBool(val));
            }
            else
            {
                bool val = ((PCConst)os[1]).ToFloat() >= ((PCConst)os[0]).ToFloat();
                Stack.Push(new PCBool(val));
            }
            return true;
        }

        private bool OpLt()
        {
            PCObj[] os = new PCObj[2];

            for (int i = 0; i < os.Length; i++)
            {
                try
                {
                    os[i] = Stack.Pop();
                }
                catch (InvalidOperationException)
                {
                    throw new PCEmptyStackException();
                }
                if (!(os[i] is PCConst))
                {
                    Stack.Push(os[i]);
                    throw new PCTypeCheckException();
                }
            }
            if ((os[0] is PCInt) && (os[1] is PCInt))
            {
                bool val = ((PCInt)os[1]).val < ((PCInt)os[0]).val;
                Stack.Push(new PCBool(val));
            }
            else
            {
                bool val = ((PCConst)os[1]).ToFloat() < ((PCConst)os[0]).ToFloat();
                Stack.Push(new PCBool(val));
            }
            return true;
        }

        private bool OpLe()
        {
            PCObj[] os = new PCObj[2];

            for (int i = 0; i < os.Length; i++)
            {
                try
                {
                    os[i] = Stack.Pop();
                }
                catch (InvalidOperationException)
                {
                    throw new PCEmptyStackException();
                }
                if (!(os[i] is PCConst))
                {
                    Stack.Push(os[i]);
                    throw new PCTypeCheckException();
                }
            }
            if ((os[0] is PCInt) && (os[1] is PCInt))
            {
                bool val = ((PCInt)os[1]).val <= ((PCInt)os[0]).val;
                Stack.Push(new PCBool(val));
            }
            else
            {
                bool val = ((PCConst)os[1]).ToFloat() <= ((PCConst)os[0]).ToFloat();
                Stack.Push(new PCBool(val));
            }
            return true;
        }

        private bool OpAdd()
        {
            PCObj[] os = new PCObj[2];

            for (int i = 0; i < os.Length; i++)
            {
                try
                {
                    os[i] = Stack.Pop();
                }
                catch (InvalidOperationException)
                {
                    throw new PCEmptyStackException();
                }
                if (!(os[i] is PCConst))
                {
                    Stack.Push(os[i]);
                    throw new PCTypeCheckException();
                }
            }
            if ((os[0] is PCInt) && (os[1] is PCInt))
            {
                int val = ((PCInt)os[1]).val + ((PCInt)os[0]).val;
                Stack.Push(new PCInt(val));
            }
            else
            {
                float val = ((PCConst)os[1]).ToFloat() + ((PCConst)os[0]).ToFloat();
                Stack.Push(new PCFloat(val));
            }
            return true;
        }

        private bool OpSub()
        {
            PCObj[] os = new PCObj[2];

            for (int i = 0; i < os.Length; i++)
            {
                try
                {
                    os[i] = Stack.Pop();
                }
                catch (InvalidOperationException)
                {
                    throw new PCEmptyStackException();
                }
                if (!(os[i] is PCConst))
                {
                    Stack.Push(os[i]);
                    throw new PCTypeCheckException();
                }
            }
            if ((os[0] is PCInt) && (os[1] is PCInt))
            {
                int val = ((PCInt)os[1]).val - ((PCInt)os[0]).val;
                Stack.Push(new PCInt(val));
            }
            else
            {
                float val = ((PCConst)os[1]).ToFloat() - ((PCConst)os[0]).ToFloat();
                Stack.Push(new PCFloat(val));
            }
            return true;
        }

        private bool OpMul()
        {
            PCObj[] os = new PCObj[2];

            for (int i = 0; i < os.Length; i++)
            {
                try
                {
                    os[i] = Stack.Pop();
                }
                catch (InvalidOperationException)
                {
                    throw new PCEmptyStackException();
                }
                if (!(os[i] is PCConst))
                {
                    Stack.Push(os[i]);
                    throw new PCTypeCheckException();
                }
            }
            if ((os[0] is PCInt) && (os[1] is PCInt))
            {
                int val = ((PCInt)os[1]).val * ((PCInt)os[0]).val;
                Stack.Push(new PCInt(val));
            }
            else
            {
                float val = ((PCConst)os[1]).ToFloat() * ((PCConst)os[0]).ToFloat();
                Stack.Push(new PCFloat(val));
            }
            return true;
        }

        private bool OpDiv()
        {
            PCObj[] os = new PCObj[2];

            for (int i = 0; i < os.Length; i++)
            {
                try
                {
                    os[i] = Stack.Pop();
                }
                catch (InvalidOperationException)
                {
                    throw new PCEmptyStackException();
                }
                if (!(os[i] is PCConst))
                {
                    Stack.Push(os[i]);
                    throw new PCTypeCheckException();
                }
            }
            float val = ((PCConst)os[1]).ToFloat() / ((PCConst)os[0]).ToFloat();
            Stack.Push(new PCFloat(val));
            return true;
        }

        private bool OpIdiv()
        {
            PCObj[] os = new PCObj[2];

            for (int i = 0; i < os.Length; i++)
            {
                try
                {
                    os[i] = Stack.Pop();
                }
                catch (InvalidOperationException)
                {
                    throw new PCEmptyStackException();
                }
                if (!(os[i] is PCConst))
                {
                    Stack.Push(os[i]);
                    throw new PCTypeCheckException();
                }
            }
            if ((os[0] is PCInt) && (os[1] is PCInt))
            {
                int val = ((PCInt)os[1]).val / ((PCInt)os[0]).val;
                Stack.Push(new PCInt(val));
            }
            else
            {
                Stack.Push(os[1]);
                Stack.Push(os[0]);
                throw new PCTypeCheckException();
            }
            return true;
        }

        private bool OpMod()
        {
            PCObj[] os = new PCObj[2];

            for (int i = 0; i < os.Length; i++)
            {
                try
                {
                    os[i] = Stack.Pop();
                }
                catch (InvalidOperationException)
                {
                    throw new PCEmptyStackException();
                }
                if (!(os[i] is PCConst))
                {
                    Stack.Push(os[i]);
                    throw new PCTypeCheckException();
                }
            }
            if ((os[0] is PCInt) && (os[1] is PCInt))
            {
                int val = ((PCInt)os[1]).val % ((PCInt)os[0]).val;
                Stack.Push(new PCInt(val));
            }
            else
            {
                Stack.Push(os[1]);
                Stack.Push(os[0]);
                throw new PCTypeCheckException();
            }
            return true;
        }

        private bool OpArray()
        {
            Stack<PCObj> t = new Stack<PCObj>();
            PCArray a = new PCArray();
            PCObj o;

            while (true)
            {
                try
                {
                    o = Stack.Pop();
                    if (o is PCMark)
                        break;
                    t.Push(o);
                }
                catch (InvalidOperationException)
                {
                    throw new PCEmptyStackException();
                }
            }
            while (true)
            {
                try
                {
                    o = t.Pop();
                    a.Add(o);
                }
                catch (InvalidOperationException)
                {
                    break;
                }
            }
            Stack.Push(a);
            return true;
        }

        private bool OpLength()
        {
            PCObj o;

            try
            {
                o = Stack.Pop();
            }
            catch (InvalidOperationException)
            {
                throw new PCEmptyStackException();
            }
            if (!(o is PCArray))
            {
                Stack.Push(o);
                throw new PCTypeCheckException();
            }
            Stack.Push(new PCInt(((PCArray)o).Length));
            return true;
        }

        private bool OpGet()
        {
            PCObj o;

            try
            {
                o = Stack.Pop();
            }
            catch (InvalidOperationException)
            {
                throw new PCEmptyStackException();
            }
            if (!(o is PCConst))
            {
                Stack.Push(o);
                throw new PCTypeCheckException();
            }
            int idx = ((PCConst)o).ToInt();

            try
            {
                o = Stack.Pop();
            }
            catch (InvalidOperationException)
            {
                throw new PCEmptyStackException();
            }
            if (!(o is PCArray))
            {
                Stack.Push(o);
                throw new PCTypeCheckException();
            }
            Stack.Push(((PCArray)o)[idx]);
            return true;
        }

        private bool OpPut()
        {
            PCObj p;
            PCObj o;

            try
            {
                p = Stack.Pop();
            }
            catch (InvalidOperationException)
            {
                throw new PCEmptyStackException();
            }
            try
            {
                o = Stack.Pop();
            }
            catch (InvalidOperationException)
            {
                throw new PCEmptyStackException();
            }
            if (!(o is PCConst))
            {
                Stack.Push(o);
                throw new PCTypeCheckException();
            }
            int idx = ((PCConst)o).ToInt();

            try
            {
                o = Stack.Pop();
            }
            catch (InvalidOperationException)
            {
                throw new PCEmptyStackException();
            }
            if (!(o is PCArray))
            {
                Stack.Push(o);
                throw new PCTypeCheckException();
            }
            ((PCArray)o)[idx] = p;
            return true;
        }

        private bool OpVector2()
        {
            PCObj[] os = new PCObj[2];

            for (int i = 0; i < os.Length; i++)
            {
                try
                {
                    os[i] = Stack.Pop();
                }
                catch (InvalidOperationException)
                {
                    throw new PCEmptyStackException();
                }
                if (!(os[i] is PCConst))
                {
                    Stack.Push(os[i]);
                    throw new PCTypeCheckException();
                }
            }
            float x = ((PCConst)os[1]).ToFloat();
            float y = ((PCConst)os[0]).ToFloat();
            Stack.Push(new PCVector2(x, y));
            return true;
        }

        private bool OpVector3()
        {
            PCObj[] os = new PCObj[3];

            for (int i = 0; i < os.Length; i++)
            {
                try
                {
                    os[i] = Stack.Pop();
                }
                catch (InvalidOperationException)
                {
                    throw new PCEmptyStackException();
                }
                if (!(os[i] is PCConst))
                {
                    Stack.Push(os[i]);
                    throw new PCTypeCheckException();
                }
            }
            float x = ((PCConst)os[2]).ToFloat();
            float y = ((PCConst)os[1]).ToFloat();
            float z = ((PCConst)os[0]).ToFloat();
            Stack.Push(new PCVector3(x, y, z));
            return true;
        }

        private bool OpVector4()
        {
            PCObj[] os = new PCObj[4];

            for (int i = 0; i < os.Length; i++)
            {
                try
                {
                    os[i] = Stack.Pop();
                }
                catch (InvalidOperationException)
                {
                    throw new PCEmptyStackException();
                }
                if (!(os[i] is PCConst))
                {
                    Stack.Push(os[i]);
                    throw new PCTypeCheckException();
                }
            }
            float x = ((PCConst)os[3]).ToFloat();
            float y = ((PCConst)os[2]).ToFloat();
            float z = ((PCConst)os[1]).ToFloat();
            float w = ((PCConst)os[0]).ToFloat();
            Stack.Push(new PCVector4(x, y, z, w));
            return true;
        }

        private bool OpIf()
        {
            PCObj proc;
            PCObj cond;

            try
            {
                cond = Stack.Pop();
            }
            catch (InvalidOperationException)
            {
                throw new PCEmptyStackException();
            }
            if (!(cond is PCBool))
            {
                Stack.Push(cond);
                throw new PCTypeCheckException();
            }
            try
            {
                proc = Stack.Pop();
            }
            catch (InvalidOperationException)
            {
                throw new PCEmptyStackException();
            }
            if (!(proc is PCFun))
            {
                Stack.Push(proc);
                throw new PCTypeCheckException();
            }
            if (((PCBool)cond).val)
            {
                Call((Compiler.ExpPair)((PCFun)proc).exp);
                return false;
            }
            else
            {
                return true;
            }
        }

        private bool OpIfElse()
        {
            PCObj proc1;
            PCObj proc0;
            PCObj cond;

            try
            {
                cond = Stack.Pop();
            }
            catch (InvalidOperationException)
            {
                throw new PCEmptyStackException();
            }
            if (!(cond is PCBool))
            {
                Stack.Push(cond);
                throw new PCTypeCheckException();
            }
            try
            {
                proc0 = Stack.Pop();
            }
            catch (InvalidOperationException)
            {
                throw new PCEmptyStackException();
            }
            if (!(proc0 is PCFun))
            {
                Stack.Push(proc0);
                throw new PCTypeCheckException();
            }
            try
            {
                proc1 = Stack.Pop();
            }
            catch (InvalidOperationException)
            {
                throw new PCEmptyStackException();
            }
            if (!(proc1 is PCFun))
            {
                Stack.Push(proc1);
                throw new PCTypeCheckException();
            }
            if (((PCBool)cond).val)
            {
                Call((Compiler.ExpPair)((PCFun)proc1).exp);
                return false;
            }
            else
            {
                Call((Compiler.ExpPair)((PCFun)proc0).exp);
                return false;
            }
        }

        private bool OpLoop()
        {
            PCObj proc;

            try
            {
                proc = Stack.Pop();
            }
            catch (InvalidOperationException)
            {
                throw new PCEmptyStackException();
            }
            if (!(proc is PCFun))
            {
                Stack.Push(proc);
                throw new PCTypeCheckException();
            }
            EnterLoop((Compiler.ExpPair)((PCFun)proc).exp);
            return false;
        }


        private bool OpRepeat()
        {
            PCObj proc;
            PCObj o;

            try
            {
                proc = Stack.Pop();
            }
            catch (InvalidOperationException)
            {
                throw new PCEmptyStackException();
            }
            if (!(proc is PCFun))
            {
                Stack.Push(proc);
                throw new PCTypeCheckException();
            }
            try
            {
                o = Stack.Pop();
            }
            catch (InvalidOperationException)
            {
                throw new PCEmptyStackException();
            }
            if (!(o is PCConst))
            {
                Stack.Push(o);
                throw new PCTypeCheckException();
            }
            int repeat = ((PCConst)o).ToInt();

            EnterRepeat(repeat, (Compiler.ExpPair)((PCFun)proc).exp);
            return false;
        }

        private bool OpFor()
        {
            PCObj proc;
            PCObj[] os = new PCObj[3];

            try
            {
                proc = Stack.Pop();
            }
            catch (InvalidOperationException)
            {
                throw new PCEmptyStackException();
            }
            if (!(proc is PCFun))
            {
                Stack.Push(proc);
                throw new PCTypeCheckException();
            }
            for (int i = 0; i < os.Length; i++)
            {
                try
                {
                    os[i] = Stack.Pop();
                }
                catch (InvalidOperationException)
                {
                    throw new PCEmptyStackException();
                }
                if (!(os[i] is PCConst))
                {
                    Stack.Push(os[i]);
                    throw new PCTypeCheckException();
                }
            }
            float start = ((PCConst)os[2]).ToFloat();
            float incr = ((PCConst)os[1]).ToFloat();
            float stop = ((PCConst)os[0]).ToFloat();

            if ((0 < incr && start <= stop) || (incr < 0 && stop <= start))
            {
                Stack.Push(new PCFloat(start));
                EnterFor(start, incr, stop, (Compiler.ExpPair)((PCFun)proc).exp);
                return false;
            }
            else
            {
                return true;
            }
        }

        private bool OpForAll()
        {
            PCObj proc;
            PCObj o;

            try
            {
                proc = Stack.Pop();
            }
            catch (InvalidOperationException)
            {
                throw new PCEmptyStackException();
            }
            if (!(proc is PCFun))
            {
                Stack.Push(proc);
                throw new PCTypeCheckException();
            }
            try
            {
                o = Stack.Pop();
            }
            catch (InvalidOperationException)
            {
                throw new PCEmptyStackException();
            }
            if (!(o is PCArray))
            {
                Stack.Push(o);
                throw new PCTypeCheckException();
            }
            if (((PCArray)o).Length == 0)
            {
                return true;
            }
            else
            {
                Stack.Push(((PCArray)o)[0]);
                EnterForAll((PCArray)o, (Compiler.ExpPair)((PCFun)proc).exp);
                return false;
            }
        }

        private bool OpExit()
        {
            ExitLoop();
            return false;
        }

        private bool OpDef()
        {
            PCObj p;
            PCObj o;

            try
            {
                p = Stack.Pop();
            }
            catch (InvalidOperationException)
            {
                throw new PCEmptyStackException();
            }
            try
            {
                o = Stack.Pop();
            }
            catch (InvalidOperationException)
            {
                throw new PCEmptyStackException();
            }
            if (!(o is PCSym))
            {
                Stack.Push(o);
                throw new PCTypeCheckException();
            }

            PCDict dict = CurrentDict;
            string key = ((PCSym)o).val;
            if (dict.ContainsKey(key))
            {
                dict[key] = p;
            }
            else
            {
                dict.Add(key, p);
            }
            return true;
        }

        private bool OpBegin()
        {
            m_dictionary_stack.Push(new PCDict());
            return true;
        }

        private bool OpEnd()
        {
            m_dictionary_stack.Pop();
            return true;
        }

        private delegate double MathOperator(double f);
        private delegate double MathOperator2(double f, double g);

        private bool OpMath(MathOperator fun)
        {
            PCObj o;

            try
            {
                o = Stack.Pop();
            }
            catch (InvalidOperationException)
            {
                throw new PCEmptyStackException();
            }
            if (!(o is PCConst))
            {
                Stack.Push(o);
                throw new PCTypeCheckException();
            }
            Stack.Push(new PCFloat((float)fun(((PCConst)o).ToFloat())));
            return true;
        }

        private bool OpMath2(MathOperator2 fun)
        {
            PCObj[] os = new PCObj[2];

            for (int i = 0; i < os.Length; i++)
            {
                try
                {
                    os[i] = Stack.Pop();
                }
                catch (InvalidOperationException)
                {
                    throw new PCEmptyStackException();
                }
                if (!(os[i] is PCConst))
                {
                    Stack.Push(os[i]);
                    throw new PCTypeCheckException();
                }
            }
            double x = ((PCConst)os[1]).ToFloat();
            double y = ((PCConst)os[0]).ToFloat();
            Stack.Push(new PCFloat((float)fun(x, y)));
            return true;
        }

        private bool OpCos()
        {
            return OpMath(Math.Cos);
        }

        private bool OpSin()
        {
            return OpMath(Math.Sin);
        }

        private bool OpTan()
        {
            return OpMath(Math.Tan);
        }

        private bool OpAcos()
        {
            return OpMath(Math.Acos);
        }

        private bool OpAsin()
        {
            return OpMath(Math.Asin);
        }

        private bool OpAtan()
        {
            return OpMath(Math.Atan);
        }

        private bool OpAtan2()
        {
            return OpMath2(Math.Atan2);
        }

        private bool OpLog()
        {
            return OpMath(Math.Log);
        }

        private bool OpLog10()
        {
            return OpMath(Math.Log10);
        }

        private bool OpExp()
        {
            return OpMath(Math.Exp);
        }

        private bool OpPow()
        {
            return OpMath2(Math.Pow);
        }

        private bool OpSqrt()
        {
            return OpMath(Math.Sqrt);
        }

        private bool OpCeiling()
        {
            return OpMath(Math.Ceiling);
        }

        private bool OpFloor()
        {
            return OpMath(Math.Floor);
        }

        private bool OpRound()
        {
            return OpMath(Math.Round);
        }

        private bool OpTruncate()
        {
            return OpMath(Math.Truncate);
        }

        private bool OpAbs()
        {
            return OpMath(Math.Abs);
        }


        private bool OpRand()
        {
            Stack.Push(new PCInt(m_random.Next()));
            return true;
        }

        private bool OpSRand()
        {
            PCObj o;

            try
            {
                o = Stack.Pop();
            }
            catch (InvalidOperationException)
            {
                throw new PCEmptyStackException();
            }
            if (!(o is PCConst))
            {
                Stack.Push(o);
                throw new PCTypeCheckException();
            }
            m_random = new System.Random(((PCConst)o).ToInt());
            return true;
        }

        private bool OpSleep()
        {
            PCObj o;

            try
            {
                o = Stack.Pop();
            }
            catch (InvalidOperationException)
            {
                throw new PCEmptyStackException();
            }
            if (!(o is PCConst))
            {
                Stack.Push(o);
                throw new PCTypeCheckException();
            }
            System.Threading.Thread.Sleep(((PCConst)o).ToInt());
            return true;
        }
        private void PostInit(PCDict system)
        {
            system["nop"] = new PCOp(OpNop);
            system["null"] = new PCOp(OpNull);
            system["pstack"] = new PCOp(OpPstack);
            system["clear"] = new PCOp(OpClear);
            system["dup"] = new PCOp(OpDup);
            system["exch"] = new PCOp(OpExch);
            system["pop"] = new PCOp(OpPop);
            system["roll"] = new PCOp(OpRoll);
            system["not"] = new PCOp(OpNot);
            system["neg"] = new PCOp(OpNeg);
            system["and"] = new PCOp(OpAnd);
            system["or"] = new PCOp(OpOr);
            system["xor"] = new PCOp(OpXor);
            system["isnull"] = new PCOp(OpIsNull);
            system["eq"] = new PCOp(OpEq);
            system["ne"] = new PCOp(OpNe);
            system["gt"] = new PCOp(OpGt);
            system["ge"] = new PCOp(OpGe);
            system["lt"] = new PCOp(OpLt);
            system["le"] = new PCOp(OpLe);
            system["add"] = new PCOp(OpAdd);
            system["sub"] = new PCOp(OpSub);
            system["mul"] = new PCOp(OpMul);
            system["div"] = new PCOp(OpDiv);
            system["idiv"] = new PCOp(OpIdiv);
            system["mod"] = new PCOp(OpMod);
            system["]"] = new PCOp(OpArray);
            system["length"] = new PCOp(OpLength);
            system["get"] = new PCOp(OpGet);
            system["put"] = new PCOp(OpPut);
            system["vector2"] = new PCOp(OpVector2);
            system["vector3"] = new PCOp(OpVector3);
            system["vector4"] = new PCOp(OpVector4);
            system["if"] = new PCOp(OpIf);
            system["ifelse"] = new PCOp(OpIfElse);
            system["loop"] = new PCOp(OpLoop);
            system["repeat"] = new PCOp(OpRepeat);
            system["for"] = new PCOp(OpFor);
            system["forall"] = new PCOp(OpForAll);
            system["exit"] = new PCOp(OpExit);
            system["def"] = new PCOp(OpDef);
            system["begin"] = new PCOp(OpBegin);
            system["end"] = new PCOp(OpEnd);
            system["cos"] = new PCOp(OpCos);
            system["sin"] = new PCOp(OpSin);
            system["tan"] = new PCOp(OpTan);
            system["acos"] = new PCOp(OpAcos);
            system["asin"] = new PCOp(OpAsin);
            system["atan"] = new PCOp(OpAtan);
            system["atan2"] = new PCOp(OpAtan2);
            system["log"] = new PCOp(OpLog);
            system["log10"] = new PCOp(OpLog10);
            system["exp"] = new PCOp(OpExp);
            system["pow"] = new PCOp(OpPow);
            system["sqrt"] = new PCOp(OpSqrt);
            system["ceiling"] = new PCOp(OpCeiling);
            system["floor"] = new PCOp(OpFloor);
            system["round"] = new PCOp(OpRound);
            system["truncate"] = new PCOp(OpTruncate);
            system["abs"] = new PCOp(OpAbs);
            system["rand"] = new PCOp(OpRand);
            system["srand"] = new PCOp(OpSRand);
            system["sleep"] = new PCOp(OpSleep);
            system["gsave"] = new PCOp(OpGSave);
            system["grestore"] = new PCOp(OpGRestore);
            system["translate"] = new PCOp(OpTranslate);
            system["rotate"] = new PCOp(OpRotate);
            system["currentpoint"] = new PCOp(OpCurrentPoint);
            system["moveto"] = new PCOp(OpMoveTo);
            system["rmoveto"] = new PCOp(OpRMoveTo);
            system["getposition"] = new PCOp(OpGetPosition);
            system["setposition"] = new PCOp(OpSetPosition);
            system["setrposition"] = new PCOp(OpSetRPosition);
            system["setrotation"] = new PCOp(OpSetRotation);
            system["setsize"] = new PCOp(OpSetSize);
            system["settaper"] = new PCOp(OpSetTaper);
            system["setcolor"] = new PCOp(OpSetColor);
            system["settexture"] = new PCOp(OpSetTexture);
            system["setglow"] = new PCOp(OpSetGlow);
            system["setshiny"] = new PCOp(OpSetShiny);
            system["setfullbright"] = new PCOp(OpSetFullBright);
            system["setalpha"] = new PCOp(OpSetAlpha);
            system["setphysics"] = new PCOp(OpSetPhysics);
            system["show"] = new PCOp(OpShow);
            system["createsphere"] = new PCOp(OpCreateSphere);
            system["createbox"] = new PCOp(OpCreateBox);
            system["createcylinder"] = new PCOp(OpCreateCylinder);
            system["currentlinewidth"] = new PCOp(OpCurrentLineWidth);
            system["setlinewidth"] = new PCOp(OpSetCurrentLineWidth);
            system["lineto"] = new PCOp(OpLineTo);
            system["rlineto"] = new PCOp(OpRLineTo);
            system["banner"] = new PCOp(OpBanner);
            system["M_PI"] = new PCFloat((float)Math.PI);
            system["M_E"] = new PCFloat((float)Math.E);
            system["ALL_SIDES"] = new PCInt(ALL_SIDES);
        }

        public Compiler.Exp CurrentStep()
        {
            return NextInstr != null ? NextInstr.hd : null;
        }

        public string CurrentStepPos()
        {
            Compiler.Exp exp = CurrentStep();
            return exp != null ? exp.Pos : "----";
        }

        public void Inject(Compiler.ExpPair ast)
        {
            Compiler.ExpPair tail = ast;
            while (tail.tl != null)
            {
                tail = tail.tl;
            }
            tail.tl = NextInstr;
            NextInstr = ast;
        }

        public void Call(Compiler.ExpPair ast)
        {
            Frame = new Pair<Compiler.ExpPair>(NextInstr, Frame);
            NextInstr = ast;
        }

        private class LoopSentinel : Compiler.ISentinel
        {
            public bool Continue() { return true; }
        }

        private void EnterLoop(Compiler.ExpPair ast)
        {
            Compiler.ExpPair tail = ast;
            while (!(tail.hd is Compiler.ExpTail))
            {
                tail = tail.tl;
            }
            ((Compiler.ExpTail)tail.hd).Sentinel = new LoopSentinel();
            tail.tl = ast;

            Frame = new Pair<Compiler.ExpPair>(NextInstr, Frame);
            TraceLoop = new Pair<Pair<Compiler.ExpPair>>(Frame, TraceLoop);
            NextInstr = ast;
        }

        private class RepeatSentinel : Compiler.ISentinel
        {
            private int repeat;
            public RepeatSentinel(int repeat)
            {
                this.repeat = repeat;
            }
            public bool Continue() { return 0 < --repeat; }
        }

        private void EnterRepeat(int repeat, Compiler.ExpPair ast)
        {
            Compiler.ExpPair tail = ast;
            while (!(tail.hd is Compiler.ExpTail))
            {
                tail = tail.tl;
            }
            ((Compiler.ExpTail)tail.hd).Sentinel = new RepeatSentinel(repeat);
            tail.tl = ast;

            Frame = new Pair<Compiler.ExpPair>(NextInstr, Frame);
            TraceLoop = new Pair<Pair<Compiler.ExpPair>>(Frame, TraceLoop);
            NextInstr = ast;
        }

        private class ForSentinel : Compiler.ISentinel
        {
            private Stack<PCObj> stack;
            private float var;
            private float incr;
            private float stop;

            public ForSentinel(Stack<PCObj> stack, float start, float incr, float stop)
            {
                this.stack = stack;
                this.var = start;
                this.incr = incr;
                this.stop = stop;
            }

            public bool Continue()
            {
                var += incr;
                if ((0 < incr && var <= stop) || (incr < 0 && stop <= var))
                {
                    stack.Push(new PCFloat(var));
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }

        private void EnterFor(float start, float incr, float stop, Compiler.ExpPair ast)
        {
            Compiler.ExpPair tail = ast;
            while (!(tail.hd is Compiler.ExpTail))
            {
                tail = tail.tl;
            }
            ((Compiler.ExpTail)tail.hd).Sentinel = new ForSentinel(Stack, start, incr, stop);
            tail.tl = ast;

            Frame = new Pair<Compiler.ExpPair>(NextInstr, Frame);
            TraceLoop = new Pair<Pair<Compiler.ExpPair>>(Frame, TraceLoop);
            NextInstr = ast;
        }

        private class ForAllSentinel : Compiler.ISentinel
        {
            private Stack<PCObj> stack;
            private PCArray array;
            private int var;

            public ForAllSentinel(Stack<PCObj> stack, PCArray array)
            {
                this.stack = stack;
                this.array = array;
                this.var = 0;
            }

            public bool Continue()
            {
                var++;
                if (var < array.Length)
                {
                    stack.Push(array[var]);
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }

        private void EnterForAll(PCArray array, Compiler.ExpPair ast)
        {
            Compiler.ExpPair tail = ast;
            while (!(tail.hd is Compiler.ExpTail))
            {
                tail = tail.tl;
            }
            ((Compiler.ExpTail)tail.hd).Sentinel = new ForAllSentinel(Stack, array);
            tail.tl = ast;

            Frame = new Pair<Compiler.ExpPair>(NextInstr, Frame);
            TraceLoop = new Pair<Pair<Compiler.ExpPair>>(Frame, TraceLoop);
            NextInstr = ast;
        }

        private bool Leave()
        {
            if (Frame == null)
            {
                NextInstr = null;
                return false;
            }
            else
            {
                NextInstr = Frame.hd;
                Frame = Frame.tl;
                if (NextInstr == null)
                {
                    return false;
                }
                else
                {
                    NextInstr = NextInstr.tl;
                    return true;
                }
            }
        }

        private void ExitLoop()
        {
            if (TraceLoop == null)
            {
                throw new PCExitWithoutLoop();
            }
            else
            {
                Frame = TraceLoop.hd;
                TraceLoop = TraceLoop.tl;
                Leave();
            }
        }

        private bool StepDo(Compiler.Exp exp)
        {
            if (exp is Compiler.ExpId)
            {
                PCObj ent = LookupDict(((Compiler.ExpId)exp).val);
                if (ent != null)
                {
                    if (ent is PCOp)
                    {
                        return ((PCOp)ent).op();
                    }
                    else if (ent is PCFun)
                    {
                        Call((Compiler.ExpPair)((PCFun)ent).exp);
                        return false;
                    }
                    else
                    {
                        Stack.Push(ent);
                    }
                    return true;
                }
                throw new PCNotFoundException(((Compiler.ExpId)exp).val);
            }
            else if (exp is Compiler.ExpNum)
            {
                Compiler.ExpNum num = (Compiler.ExpNum)exp;
                PCObj obj;
                if (num.obj is Compiler.ExpFloat)
                {
                    obj = new PCFloat(((Compiler.ExpFloat)(num.obj)).val);
                }
                else
                {
                    obj = new PCInt(((Compiler.ExpInt)(num.obj)).val);
                }
                Stack.Push(obj);
            }
            else if (exp is Compiler.ExpBool)
            {
                Stack.Push(new PCBool(((Compiler.ExpBool)exp).val));
            }
            else if (exp is Compiler.ExpSym)
            {
                Stack.Push(new PCSym(((Compiler.ExpSym)exp).val));
            }
            else if (exp is Compiler.ExpStr)
            {
                Stack.Push(new PCStr(((Compiler.ExpStr)exp).val));
            }
            else if (exp is Compiler.ExpMark)
            {
                Stack.Push(new PCMark());
            }
            else if (exp is Compiler.ExpUUID)
            {
                Stack.Push(new PCUUID(((Compiler.ExpUUID)exp).val));
            }
            else if (exp is Compiler.ExpVector2)
            {
                Stack.Push(new PCVector2(((Compiler.ExpVector2)exp).val));
            }
            else if (exp is Compiler.ExpVector3)
            {
                Stack.Push(new PCVector3(((Compiler.ExpVector3)exp).val));
            }
            else if (exp is Compiler.ExpVector4)
            {
                Stack.Push(new PCVector4(((Compiler.ExpVector4)exp).val));
            }
            else if (exp is Compiler.ExpFun)
            {
                Stack.Push(new PCFun(((Compiler.ExpFun)exp).val));
            }
            else
            {
                throw new PCNotImplementedException(exp.ToString());
            }
            return true;
        }

        public bool Step()
        {
            if (NextInstr == null)
            {
                return false;
            }
            else
            {
                if (NextInstr.hd is Compiler.ExpTail)
                {
                    if (((Compiler.ExpTail)NextInstr.hd).Sentinel.Continue())
                    {
                        NextInstr = NextInstr.tl;
                        return true;
                    }
                    else
                    {
                        return Leave();
                    }
                }
                else
                {
                    if (StepDo(NextInstr.hd))
                        NextInstr = NextInstr.tl;
                    return true;
                }
            }
        }

        public void Finish()
        {
            while (Step()) ;
        }

        public void ShowNextStep()
        {
            if (NextInstr == null)
            {
                return;
            }
            else
            {
                if (NextInstr.hd is Compiler.ExpTail)
                {
                    Compiler.Exp exp = NextInstr.hd;
                    Console.WriteLine("{0}", exp.ToString());
                }
                else
                {
                    Compiler.Exp exp = NextInstr.hd;
                    Console.WriteLine("=>{0} (at {1})", exp.ToString(), exp.Pos);
                }
            }
        }

        public void DumpStack(bool all)
        {
            Object[] copy = Stack.ToArray();
            int cnt = 0;
            int total = Stack.Count;

            Console.WriteLine("==");
            while (cnt < Stack.Count && (all || cnt < 20))
                Console.WriteLine("{0}", copy[cnt++].ToString());
            if (total != cnt)
                Console.WriteLine("...skip");
            Console.WriteLine("Total: {0} objects in stack", total);
        }

        private static Parser MakeParser()
        {
            Tools.YyParser parser = new Compiler.yyPCParser();
            ErrorHandler handler = new ErrorHandler(true);

            return new Compiler.PCParser(parser, handler);
        }

        public static bool Load(Scene scene, IConfigSource source,StreamReader script)
        {
            Parser parser = MakeParser();
            PCVM vm = new PCVM(scene, source, new PCDict());

            try
            {
                SYMBOL ast = parser.Parse(script);
                if (ast == null)
                    return false;
                vm.Call((Compiler.ExpPair)ast);
                vm.Finish();
                return true;
            }
            catch (Exception e)
            {
                m_log.InfoFormat("ERROR: {0} (at {1})", e.Message, vm.CurrentStepPos());
                m_log.InfoFormat("{0}", e.StackTrace);
                return false;
            }
            finally
            {
                vm.Dispose();
            }
        }

        public static bool Load(Scene scene, IConfigSource source,string script)
        {
            Parser parser = MakeParser();
            PCVM vm = new PCVM(scene, source, new PCDict());

            try
            {
                SYMBOL ast = parser.Parse(script);
                if (ast == null)
                    return false;
                vm.Call((Compiler.ExpPair)ast);
                vm.Finish();
                return true;
            }
            catch (Exception e)
            {
                m_log.InfoFormat("ERROR: {0} (at {1})", e.Message, vm.CurrentStepPos());
                m_log.InfoFormat("{0}", e.StackTrace);
                return false;
            }
            finally
            {
                vm.Dispose();
            }
        }

        public static void ReadEvalLoop(Scene scene, IConfigSource source)
        {
            Parser parser = MakeParser();
            PCVM vm = new PCVM(scene, source, new PCDict());

            try
            {
                do
                {
                    try
                    {
                        vm.ShowNextStep();

                        Console.Write("?");
                        string line = Console.ReadLine().Trim();

                        if (line == "")
                        {
                            vm.Step();
                            continue;
                        }
                        else if (line.ToCharArray()[0] == ':')
                        {
                            if (2 <= line.Length)
                            {
                                string command = line.Substring(0, 2);
                                if (command == ":s")
                                {
                                    vm.DumpStack(true);
                                    continue;
                                }
                                else if (command == ":f")
                                {
                                    vm.Finish();
                                    continue;
                                }
                                else if (command == ":l")
                                {
                                    string path = line.Substring(2).Trim();
                                    SYMBOL ast;
                                    using (StreamReader s = new StreamReader(path))
                                    {
                                        ast = parser.Parse(s);
                                    }
                                    if (ast == null)
                                        continue;
                                    vm.Call((Compiler.ExpPair)ast);
                                    continue;
                                }
                                else if (command == ":q")
                                {
                                    break;
                                }
                            }
                            Console.WriteLine("usage:");
                            Console.WriteLine("[return]   step");
                            Console.WriteLine(":l<file>   load");
                            Console.WriteLine(":s         dump");
                            Console.WriteLine(":f         finish");
                            Console.WriteLine(":q         quit");
                        }
                        else
                        {
                            SYMBOL ast = parser.Parse(line);
                            if (ast == null)
                                continue;
                            vm.Inject((Compiler.ExpPair)ast);
                            vm.Step();
                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("ERROR: {0} (at {1})", e.Message, vm.CurrentStepPos());
                        Console.WriteLine("{0}", e.StackTrace);
                        vm.Init(false);
                    }
                } while (true);
            }
            finally
            {
                vm.Dispose();
            }
        }
    }
}
