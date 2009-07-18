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
 *	Changes for banner(1)
 *
 *      @(#)Copyright (c) 1995, Simon J. Gerraty.
 *      
 *      This is free software.  It comes with NO WARRANTY.
 *      Permission to use, modify and distribute this source code 
 *      is granted subject to the following conditions.
 *      1/ that the above copyright notice and this notice 
 *      are preserved in all copies and that due credit be given 
 *      to the author.  
 *      2/ that any changes to this code are clearly commented 
 *      as such so that the author does not get blamed for bugs 
 *      other than his own.
 *      
 *      Please send copies of changes and bug-fixes to:
 *      sjg@zen.void.oz.au
 */
/*
 * Copyright (c) 1983, 1993
 *	The Regents of the University of California.  All rights reserved.
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the following conditions
 * are met:
 * 1. Redistributions of source code must retain the above copyright
 *    notice, this list of conditions and the following disclaimer.
 * 2. Redistributions in binary form must reproduce the above copyright
 *    notice, this list of conditions and the following disclaimer in the
 *    documentation and/or other materials provided with the distribution.
 * 3. Neither the name of the University nor the names of its contributors
 *    may be used to endorse or promote products derived from this software
 *    without specific prior written permission.
 *
 * THIS SOFTWARE IS PROVIDED BY THE REGENTS AND CONTRIBUTORS ``AS IS'' AND
 * ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE
 * IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE
 * ARE DISCLAIMED.  IN NO EVENT SHALL THE REGENTS OR CONTRIBUTORS BE LIABLE
 * FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL
 * DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS
 * OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION)
 * HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT
 * LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY
 * OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF
 * SUCH DAMAGE.
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

namespace OpenSim.Region.OptionalModules.Scripting.PC
{
    public partial class PCVM : IDisposable
    {
        private Dictionary<int, PCArray> m_banner = new Dictionary<int, PCArray>();
        private int m_banner_width = 8;
        private int m_banner_height = 9;
        
        private bool OpBanner()
        {
            PCObj pt;
            PCObj str;

            try
            {
                pt = Stack.Pop();
            }
            catch (InvalidOperationException)
            {
                throw new PCEmptyStackException();
            }
            if (!(pt is PCVector2))
            {
                Stack.Push(pt);
                throw new PCTypeCheckException();
            }
            try
            {
                str = Stack.Pop();
            }
            catch (InvalidOperationException)
            {
                throw new PCEmptyStackException();
            }
            if (!(str is PCStr))
            {
                Stack.Push(str);
                throw new PCTypeCheckException();
            }
            PCArray glyphs = new PCArray();
            float ptx = ((PCVector2)pt).val.X;
            float pty = ((PCVector2)pt).val.Y;
            float cursor = 0f;

            foreach (char ch in ((PCStr)str).val)
            {
                PCArray glyph = new PCArray();

                foreach (PCObj o in m_banner[Convert.ToInt32(ch)].val)
                {
                    float x = ((PCVector2)o).val.X * ptx / m_banner_width;
                    float y = ((PCVector2)o).val.Y * pty / m_banner_height;
                    glyph.Add(new PCVector3(x + cursor, y, 0));
                }
                glyphs.Add(glyph);
                cursor += ptx;
            }
            Stack.Push(glyphs);
            return true;
        }

        private void InitBanner()
        {
            m_banner.Add(32,new PCArray(new PCVector2[] {
            }));
            /* ! */
            m_banner.Add(33,new PCArray(new PCVector2[] {
                new PCVector2(3f,8f), new PCVector2(4f,8f), new PCVector2(3f,7f),
                new PCVector2(4f,7f), new PCVector2(3f,6f), new PCVector2(4f,6f),
                new PCVector2(3f,5f), new PCVector2(4f,5f), new PCVector2(3f,4f),
                new PCVector2(4f,4f), new PCVector2(3f,1f), new PCVector2(4f,1f),
                new PCVector2(3f,0f), new PCVector2(4f,0f),
            }));
            /* " */
            m_banner.Add(34,new PCArray(new PCVector2[] {
                new PCVector2(2f,8f), new PCVector2(5f,8f), new PCVector2(2f,7f),
                new PCVector2(5f,7f),
            }));
            /* # */
            m_banner.Add(35,new PCArray(new PCVector2[] {
                new PCVector2(3f,7f), new PCVector2(5f,7f), new PCVector2(3f,6f),
                new PCVector2(5f,6f), new PCVector2(1f,5f), new PCVector2(2f,5f),
                new PCVector2(3f,5f), new PCVector2(4f,5f), new PCVector2(5f,5f),
                new PCVector2(6f,5f), new PCVector2(7f,5f), new PCVector2(3f,4f),
                new PCVector2(5f,4f), new PCVector2(1f,3f), new PCVector2(2f,3f),
                new PCVector2(3f,3f), new PCVector2(4f,3f), new PCVector2(5f,3f),
                new PCVector2(6f,3f), new PCVector2(7f,3f), new PCVector2(3f,2f),
                new PCVector2(5f,2f), new PCVector2(3f,1f), new PCVector2(5f,1f),
            }));
            /* $ */
            m_banner.Add(36,new PCArray(new PCVector2[] {
                new PCVector2(4f,8f), new PCVector2(2f,7f), new PCVector2(3f,7f),
                new PCVector2(4f,7f), new PCVector2(5f,7f), new PCVector2(6f,7f),
                new PCVector2(1f,6f), new PCVector2(4f,6f), new PCVector2(7f,6f),
                new PCVector2(1f,5f), new PCVector2(4f,5f), new PCVector2(2f,4f),
                new PCVector2(3f,4f), new PCVector2(4f,4f), new PCVector2(5f,4f),
                new PCVector2(6f,4f), new PCVector2(4f,3f), new PCVector2(7f,3f),
                new PCVector2(1f,2f), new PCVector2(4f,2f), new PCVector2(7f,2f),
                new PCVector2(2f,1f), new PCVector2(3f,1f), new PCVector2(4f,1f),
                new PCVector2(5f,1f), new PCVector2(6f,1f), new PCVector2(4f,0f),
            }));
            /* % */
            m_banner.Add(37,new PCArray(new PCVector2[] {
                new PCVector2(2f,8f), new PCVector2(1f,7f), new PCVector2(3f,7f),
                new PCVector2(7f,7f), new PCVector2(2f,6f), new PCVector2(6f,6f),
                new PCVector2(5f,5f), new PCVector2(4f,4f), new PCVector2(3f,3f),
                new PCVector2(2f,2f), new PCVector2(6f,2f), new PCVector2(1f,1f),
                new PCVector2(5f,1f), new PCVector2(7f,1f), new PCVector2(6f,0f),
            }));
            /* & */
            m_banner.Add(38,new PCArray(new PCVector2[] {
                new PCVector2(2f,8f), new PCVector2(3f,8f), new PCVector2(1f,7f),
                new PCVector2(4f,7f), new PCVector2(1f,6f), new PCVector2(5f,6f),
                new PCVector2(2f,5f), new PCVector2(4f,5f), new PCVector2(3f,4f),
                new PCVector2(2f,3f), new PCVector2(4f,3f), new PCVector2(7f,3f),
                new PCVector2(1f,2f), new PCVector2(5f,2f), new PCVector2(6f,2f),
                new PCVector2(1f,1f), new PCVector2(5f,1f), new PCVector2(6f,1f),
                new PCVector2(2f,0f), new PCVector2(3f,0f), new PCVector2(4f,0f),
                new PCVector2(7f,0f),
            }));
            /* ' */
            m_banner.Add(39,new PCArray(new PCVector2[] {
                new PCVector2(4f,8f), new PCVector2(5f,8f), new PCVector2(4f,7f),
                new PCVector2(5f,7f), new PCVector2(4f,6f), new PCVector2(3f,5f),
            }));
            /* ( */
            m_banner.Add(40,new PCArray(new PCVector2[] {
                new PCVector2(5f,8f), new PCVector2(4f,7f), new PCVector2(3f,6f),
                new PCVector2(3f,5f), new PCVector2(3f,4f), new PCVector2(3f,3f),
                new PCVector2(3f,2f), new PCVector2(4f,1f), new PCVector2(5f,0f),
            }));
            /* ) */
            m_banner.Add(41,new PCArray(new PCVector2[] {
                new PCVector2(3f,8f), new PCVector2(4f,7f), new PCVector2(5f,6f),
                new PCVector2(5f,5f), new PCVector2(5f,4f), new PCVector2(5f,3f),
                new PCVector2(5f,2f), new PCVector2(4f,1f), new PCVector2(3f,0f),
            }));
            /* * */
            m_banner.Add(42,new PCArray(new PCVector2[] {
                new PCVector2(4f,7f), new PCVector2(1f,6f), new PCVector2(4f,6f),
                new PCVector2(7f,6f), new PCVector2(2f,5f), new PCVector2(4f,5f),
                new PCVector2(6f,5f), new PCVector2(3f,4f), new PCVector2(4f,4f),
                new PCVector2(5f,4f), new PCVector2(2f,3f), new PCVector2(4f,3f),
                new PCVector2(6f,3f), new PCVector2(1f,2f), new PCVector2(4f,2f),
                new PCVector2(7f,2f), new PCVector2(4f,1f),
            }));
            /* + */
            m_banner.Add(43,new PCArray(new PCVector2[] {
                new PCVector2(4f,7f), new PCVector2(4f,6f), new PCVector2(4f,5f),
                new PCVector2(1f,4f), new PCVector2(2f,4f), new PCVector2(3f,4f),
                new PCVector2(4f,4f), new PCVector2(5f,4f), new PCVector2(6f,4f),
                new PCVector2(7f,4f), new PCVector2(4f,3f), new PCVector2(4f,2f),
                new PCVector2(4f,1f),
            }));
            /* , */
            m_banner.Add(44,new PCArray(new PCVector2[] {
                new PCVector2(3f,1f), new PCVector2(4f,1f), new PCVector2(3f,0f),
                new PCVector2(4f,0f), new PCVector2(3f,-1f), new PCVector2(2f,-2f),
            }));
            /* - */
            m_banner.Add(45,new PCArray(new PCVector2[] {
                new PCVector2(1f,4f), new PCVector2(2f,4f), new PCVector2(3f,4f),
                new PCVector2(4f,4f), new PCVector2(5f,4f), new PCVector2(6f,4f),
                new PCVector2(7f,4f),
            }));
            /* . */
            m_banner.Add(46,new PCArray(new PCVector2[] {
                new PCVector2(3f,1f), new PCVector2(4f,1f), new PCVector2(3f,0f),
                new PCVector2(4f,0f),
            }));
            /* / */
            m_banner.Add(47,new PCArray(new PCVector2[] {
                new PCVector2(7f,7f), new PCVector2(6f,6f), new PCVector2(5f,5f),
                new PCVector2(4f,4f), new PCVector2(3f,3f), new PCVector2(2f,2f),
                new PCVector2(1f,1f),
            }));
            /* 0 */
            m_banner.Add(48,new PCArray(new PCVector2[] {
                new PCVector2(2f,8f), new PCVector2(3f,8f), new PCVector2(4f,8f),
                new PCVector2(5f,8f), new PCVector2(6f,8f), new PCVector2(1f,7f),
                new PCVector2(7f,7f), new PCVector2(1f,6f), new PCVector2(6f,6f),
                new PCVector2(7f,6f), new PCVector2(1f,5f), new PCVector2(5f,5f),
                new PCVector2(7f,5f), new PCVector2(1f,4f), new PCVector2(4f,4f),
                new PCVector2(7f,4f), new PCVector2(1f,3f), new PCVector2(3f,3f),
                new PCVector2(7f,3f), new PCVector2(1f,2f), new PCVector2(2f,2f),
                new PCVector2(7f,2f), new PCVector2(1f,1f), new PCVector2(7f,1f),
                new PCVector2(2f,0f), new PCVector2(3f,0f), new PCVector2(4f,0f),
                new PCVector2(5f,0f), new PCVector2(6f,0f),
            }));
            /* 1 */
            m_banner.Add(49,new PCArray(new PCVector2[] {
                new PCVector2(4f,8f), new PCVector2(3f,7f), new PCVector2(4f,7f),
                new PCVector2(2f,6f), new PCVector2(4f,6f), new PCVector2(4f,5f),
                new PCVector2(4f,4f), new PCVector2(4f,3f), new PCVector2(4f,2f),
                new PCVector2(4f,1f), new PCVector2(2f,0f), new PCVector2(3f,0f),
                new PCVector2(4f,0f), new PCVector2(5f,0f), new PCVector2(6f,0f),
            }));
            /* 2 */
            m_banner.Add(50,new PCArray(new PCVector2[] {
                new PCVector2(2f,8f), new PCVector2(3f,8f), new PCVector2(4f,8f),
                new PCVector2(5f,8f), new PCVector2(6f,8f), new PCVector2(1f,7f),
                new PCVector2(7f,7f), new PCVector2(7f,6f), new PCVector2(6f,5f),
                new PCVector2(3f,4f), new PCVector2(4f,4f), new PCVector2(5f,4f),
                new PCVector2(2f,3f), new PCVector2(1f,2f), new PCVector2(1f,1f),
                new PCVector2(1f,0f), new PCVector2(2f,0f), new PCVector2(3f,0f),
                new PCVector2(4f,0f), new PCVector2(5f,0f), new PCVector2(6f,0f),
                new PCVector2(7f,0f),
            }));
            /* 3 */
            m_banner.Add(51,new PCArray(new PCVector2[] {
                new PCVector2(2f,8f), new PCVector2(3f,8f), new PCVector2(4f,8f),
                new PCVector2(5f,8f), new PCVector2(6f,8f), new PCVector2(1f,7f),
                new PCVector2(7f,7f), new PCVector2(7f,6f), new PCVector2(7f,5f),
                new PCVector2(3f,4f), new PCVector2(4f,4f), new PCVector2(5f,4f),
                new PCVector2(6f,4f), new PCVector2(7f,3f), new PCVector2(7f,2f),
                new PCVector2(1f,1f), new PCVector2(7f,1f), new PCVector2(2f,0f),
                new PCVector2(3f,0f), new PCVector2(4f,0f), new PCVector2(5f,0f),
                new PCVector2(6f,0f),
            }));
            /* 4 */
            m_banner.Add(52,new PCArray(new PCVector2[] {
                new PCVector2(6f,8f), new PCVector2(5f,7f), new PCVector2(6f,7f),
                new PCVector2(4f,6f), new PCVector2(6f,6f), new PCVector2(3f,5f),
                new PCVector2(6f,5f), new PCVector2(2f,4f), new PCVector2(6f,4f),
                new PCVector2(1f,3f), new PCVector2(6f,3f), new PCVector2(1f,2f),
                new PCVector2(2f,2f), new PCVector2(3f,2f), new PCVector2(4f,2f),
                new PCVector2(5f,2f), new PCVector2(6f,2f), new PCVector2(7f,2f),
                new PCVector2(6f,1f), new PCVector2(6f,0f),
            }));
            /* 5 */
            m_banner.Add(53,new PCArray(new PCVector2[] {
                new PCVector2(1f,8f), new PCVector2(2f,8f), new PCVector2(3f,8f),
                new PCVector2(4f,8f), new PCVector2(5f,8f), new PCVector2(6f,8f),
                new PCVector2(7f,8f), new PCVector2(1f,7f), new PCVector2(1f,6f),
                new PCVector2(1f,5f), new PCVector2(2f,5f), new PCVector2(3f,5f),
                new PCVector2(4f,5f), new PCVector2(5f,5f), new PCVector2(6f,4f),
                new PCVector2(7f,3f), new PCVector2(7f,2f), new PCVector2(1f,1f),
                new PCVector2(6f,1f), new PCVector2(2f,0f), new PCVector2(3f,0f),
                new PCVector2(4f,0f), new PCVector2(5f,0f),
            }));
            /* 6 */
            m_banner.Add(54,new PCArray(new PCVector2[] {
                new PCVector2(3f,8f), new PCVector2(4f,8f), new PCVector2(5f,8f),
                new PCVector2(6f,8f), new PCVector2(2f,7f), new PCVector2(1f,6f),
                new PCVector2(1f,5f), new PCVector2(1f,4f), new PCVector2(3f,4f),
                new PCVector2(4f,4f), new PCVector2(5f,4f), new PCVector2(6f,4f),
                new PCVector2(1f,3f), new PCVector2(2f,3f), new PCVector2(7f,3f),
                new PCVector2(1f,2f), new PCVector2(7f,2f), new PCVector2(1f,1f),
                new PCVector2(7f,1f), new PCVector2(2f,0f), new PCVector2(3f,0f),
                new PCVector2(4f,0f), new PCVector2(5f,0f), new PCVector2(6f,0f),
            }));
            /* 7 */
            m_banner.Add(55,new PCArray(new PCVector2[] {
                new PCVector2(1f,8f), new PCVector2(2f,8f), new PCVector2(3f,8f),
                new PCVector2(4f,8f), new PCVector2(5f,8f), new PCVector2(6f,8f),
                new PCVector2(7f,8f), new PCVector2(1f,7f), new PCVector2(7f,7f),
                new PCVector2(6f,6f), new PCVector2(5f,5f), new PCVector2(4f,4f),
                new PCVector2(3f,3f), new PCVector2(3f,2f), new PCVector2(3f,1f),
                new PCVector2(3f,0f),
            }));
            /* 8 */
            m_banner.Add(56,new PCArray(new PCVector2[] {
                new PCVector2(2f,8f), new PCVector2(3f,8f), new PCVector2(4f,8f),
                new PCVector2(5f,8f), new PCVector2(6f,8f), new PCVector2(1f,7f),
                new PCVector2(7f,7f), new PCVector2(1f,6f), new PCVector2(7f,6f),
                new PCVector2(1f,5f), new PCVector2(7f,5f), new PCVector2(2f,4f),
                new PCVector2(3f,4f), new PCVector2(4f,4f), new PCVector2(5f,4f),
                new PCVector2(6f,4f), new PCVector2(1f,3f), new PCVector2(7f,3f),
                new PCVector2(1f,2f), new PCVector2(7f,2f), new PCVector2(1f,1f),
                new PCVector2(7f,1f), new PCVector2(2f,0f), new PCVector2(3f,0f),
                new PCVector2(4f,0f), new PCVector2(5f,0f), new PCVector2(6f,0f),
            }));
            /* 9 */
            m_banner.Add(57,new PCArray(new PCVector2[] {
                new PCVector2(2f,8f), new PCVector2(3f,8f), new PCVector2(4f,8f),
                new PCVector2(5f,8f), new PCVector2(6f,8f), new PCVector2(1f,7f),
                new PCVector2(7f,7f), new PCVector2(1f,6f), new PCVector2(7f,6f),
                new PCVector2(1f,5f), new PCVector2(7f,5f), new PCVector2(2f,4f),
                new PCVector2(3f,4f), new PCVector2(4f,4f), new PCVector2(5f,4f),
                new PCVector2(6f,4f), new PCVector2(7f,4f), new PCVector2(7f,3f),
                new PCVector2(7f,2f), new PCVector2(1f,1f), new PCVector2(7f,1f),
                new PCVector2(2f,0f), new PCVector2(3f,0f), new PCVector2(4f,0f),
                new PCVector2(5f,0f),
            }));
            /* : */
            m_banner.Add(58,new PCArray(new PCVector2[] {
                new PCVector2(3f,5f), new PCVector2(4f,5f), new PCVector2(3f,4f),
                new PCVector2(4f,4f), new PCVector2(3f,1f), new PCVector2(4f,1f),
                new PCVector2(3f,0f), new PCVector2(4f,0f),
            }));
            /* ; */
            m_banner.Add(59,new PCArray(new PCVector2[] {
                new PCVector2(3f,5f), new PCVector2(4f,5f), new PCVector2(3f,4f),
                new PCVector2(4f,4f), new PCVector2(3f,1f), new PCVector2(4f,1f),
                new PCVector2(3f,0f), new PCVector2(4f,0f), new PCVector2(3f,-1f),
                new PCVector2(2f,-2f),
            }));
            /* < */
            m_banner.Add(60,new PCArray(new PCVector2[] {
                new PCVector2(5f,8f), new PCVector2(4f,7f), new PCVector2(3f,6f),
                new PCVector2(2f,5f), new PCVector2(1f,4f), new PCVector2(2f,3f),
                new PCVector2(3f,2f), new PCVector2(4f,1f), new PCVector2(5f,0f),
            }));
            /* = */
            m_banner.Add(61,new PCArray(new PCVector2[] {
                new PCVector2(1f,5f), new PCVector2(2f,5f), new PCVector2(3f,5f),
                new PCVector2(4f,5f), new PCVector2(5f,5f), new PCVector2(6f,5f),
                new PCVector2(7f,5f), new PCVector2(1f,3f), new PCVector2(2f,3f),
                new PCVector2(3f,3f), new PCVector2(4f,3f), new PCVector2(5f,3f),
                new PCVector2(6f,3f), new PCVector2(7f,3f),
            }));
            /* > */
            m_banner.Add(62,new PCArray(new PCVector2[] {
                new PCVector2(3f,8f), new PCVector2(4f,7f), new PCVector2(5f,6f),
                new PCVector2(6f,5f), new PCVector2(7f,4f), new PCVector2(6f,3f),
                new PCVector2(5f,2f), new PCVector2(4f,1f), new PCVector2(3f,0f),
            }));
            /* ? */
            m_banner.Add(63,new PCArray(new PCVector2[] {
                new PCVector2(3f,8f), new PCVector2(4f,8f), new PCVector2(5f,8f),
                new PCVector2(6f,8f), new PCVector2(2f,7f), new PCVector2(7f,7f),
                new PCVector2(2f,6f), new PCVector2(7f,6f), new PCVector2(7f,5f),
                new PCVector2(5f,4f), new PCVector2(6f,4f), new PCVector2(4f,3f),
                new PCVector2(4f,2f), new PCVector2(4f,0f),
            }));
            /* @ */
            m_banner.Add(64,new PCArray(new PCVector2[] {
                new PCVector2(3f,8f), new PCVector2(4f,8f), new PCVector2(5f,8f),
                new PCVector2(6f,8f), new PCVector2(2f,7f), new PCVector2(7f,7f),
                new PCVector2(1f,6f), new PCVector2(4f,6f), new PCVector2(5f,6f),
                new PCVector2(7f,6f), new PCVector2(1f,5f), new PCVector2(3f,5f),
                new PCVector2(5f,5f), new PCVector2(7f,5f), new PCVector2(1f,4f),
                new PCVector2(3f,4f), new PCVector2(5f,4f), new PCVector2(7f,4f),
                new PCVector2(1f,3f), new PCVector2(3f,3f), new PCVector2(4f,3f),
                new PCVector2(5f,3f), new PCVector2(6f,3f), new PCVector2(1f,2f),
                new PCVector2(2f,1f), new PCVector2(7f,1f), new PCVector2(3f,0f),
                new PCVector2(4f,0f), new PCVector2(5f,0f), new PCVector2(6f,0f),
            }));
            /* A */
            m_banner.Add(65,new PCArray(new PCVector2[] {
                new PCVector2(3f,8f), new PCVector2(4f,8f), new PCVector2(5f,8f),
                new PCVector2(2f,7f), new PCVector2(6f,7f), new PCVector2(1f,6f),
                new PCVector2(7f,6f), new PCVector2(1f,5f), new PCVector2(7f,5f),
                new PCVector2(1f,4f), new PCVector2(2f,4f), new PCVector2(3f,4f),
                new PCVector2(4f,4f), new PCVector2(5f,4f), new PCVector2(6f,4f),
                new PCVector2(7f,4f), new PCVector2(1f,3f), new PCVector2(7f,3f),
                new PCVector2(1f,2f), new PCVector2(7f,2f), new PCVector2(1f,1f),
                new PCVector2(7f,1f), new PCVector2(1f,0f), new PCVector2(7f,0f),
            }));
            /* B */
            m_banner.Add(66,new PCArray(new PCVector2[] {
                new PCVector2(1f,8f), new PCVector2(2f,8f), new PCVector2(3f,8f),
                new PCVector2(4f,8f), new PCVector2(5f,8f), new PCVector2(6f,8f),
                new PCVector2(2f,7f), new PCVector2(7f,7f), new PCVector2(2f,6f),
                new PCVector2(7f,6f), new PCVector2(2f,5f), new PCVector2(7f,5f),
                new PCVector2(2f,4f), new PCVector2(3f,4f), new PCVector2(4f,4f),
                new PCVector2(5f,4f), new PCVector2(6f,4f), new PCVector2(2f,3f),
                new PCVector2(7f,3f), new PCVector2(2f,2f), new PCVector2(7f,2f),
                new PCVector2(2f,1f), new PCVector2(7f,1f), new PCVector2(1f,0f),
                new PCVector2(2f,0f), new PCVector2(3f,0f), new PCVector2(4f,0f),
                new PCVector2(5f,0f), new PCVector2(6f,0f),
            }));
            /* C */
            m_banner.Add(67,new PCArray(new PCVector2[] {
                new PCVector2(3f,8f), new PCVector2(4f,8f), new PCVector2(5f,8f),
                new PCVector2(6f,8f), new PCVector2(2f,7f), new PCVector2(7f,7f),
                new PCVector2(1f,6f), new PCVector2(1f,5f), new PCVector2(1f,4f),
                new PCVector2(1f,3f), new PCVector2(1f,2f), new PCVector2(2f,1f),
                new PCVector2(7f,1f), new PCVector2(3f,0f), new PCVector2(4f,0f),
                new PCVector2(5f,0f), new PCVector2(6f,0f),
            }));
            /* D */
            m_banner.Add(68,new PCArray(new PCVector2[] {
                new PCVector2(1f,8f), new PCVector2(2f,8f), new PCVector2(3f,8f),
                new PCVector2(4f,8f), new PCVector2(5f,8f), new PCVector2(2f,7f),
                new PCVector2(6f,7f), new PCVector2(2f,6f), new PCVector2(7f,6f),
                new PCVector2(2f,5f), new PCVector2(7f,5f), new PCVector2(2f,4f),
                new PCVector2(7f,4f), new PCVector2(2f,3f), new PCVector2(7f,3f),
                new PCVector2(2f,2f), new PCVector2(7f,2f), new PCVector2(2f,1f),
                new PCVector2(6f,1f), new PCVector2(1f,0f), new PCVector2(2f,0f),
                new PCVector2(3f,0f), new PCVector2(4f,0f), new PCVector2(5f,0f),
            }));
            /* E */
            m_banner.Add(69,new PCArray(new PCVector2[] {
                new PCVector2(1f,8f), new PCVector2(2f,8f), new PCVector2(3f,8f),
                new PCVector2(4f,8f), new PCVector2(5f,8f), new PCVector2(6f,8f),
                new PCVector2(7f,8f), new PCVector2(1f,7f), new PCVector2(1f,6f),
                new PCVector2(1f,5f), new PCVector2(1f,4f), new PCVector2(2f,4f),
                new PCVector2(3f,4f), new PCVector2(4f,4f), new PCVector2(5f,4f),
                new PCVector2(6f,4f), new PCVector2(1f,3f), new PCVector2(1f,2f),
                new PCVector2(1f,1f), new PCVector2(1f,0f), new PCVector2(2f,0f),
                new PCVector2(3f,0f), new PCVector2(4f,0f), new PCVector2(5f,0f),
                new PCVector2(6f,0f), new PCVector2(7f,0f),
            }));
            /* F */
            m_banner.Add(70,new PCArray(new PCVector2[] {
                new PCVector2(1f,8f), new PCVector2(2f,8f), new PCVector2(3f,8f),
                new PCVector2(4f,8f), new PCVector2(5f,8f), new PCVector2(6f,8f),
                new PCVector2(7f,8f), new PCVector2(1f,7f), new PCVector2(1f,6f),
                new PCVector2(1f,5f), new PCVector2(1f,4f), new PCVector2(2f,4f),
                new PCVector2(3f,4f), new PCVector2(4f,4f), new PCVector2(5f,4f),
                new PCVector2(6f,4f), new PCVector2(1f,3f), new PCVector2(1f,2f),
                new PCVector2(1f,1f), new PCVector2(1f,0f),
            }));
            /* G */
            m_banner.Add(71,new PCArray(new PCVector2[] {
                new PCVector2(3f,8f), new PCVector2(4f,8f), new PCVector2(5f,8f),
                new PCVector2(6f,8f), new PCVector2(2f,7f), new PCVector2(7f,7f),
                new PCVector2(1f,6f), new PCVector2(1f,5f), new PCVector2(1f,4f),
                new PCVector2(1f,3f), new PCVector2(4f,3f), new PCVector2(5f,3f),
                new PCVector2(6f,3f), new PCVector2(7f,3f), new PCVector2(1f,2f),
                new PCVector2(7f,2f), new PCVector2(2f,1f), new PCVector2(7f,1f),
                new PCVector2(3f,0f), new PCVector2(4f,0f), new PCVector2(5f,0f),
                new PCVector2(6f,0f),
            }));
            /* H */
            m_banner.Add(72,new PCArray(new PCVector2[] {
                new PCVector2(1f,8f), new PCVector2(7f,8f), new PCVector2(1f,7f),
                new PCVector2(7f,7f), new PCVector2(1f,6f), new PCVector2(7f,6f),
                new PCVector2(1f,5f), new PCVector2(7f,5f), new PCVector2(1f,4f),
                new PCVector2(2f,4f), new PCVector2(3f,4f), new PCVector2(4f,4f),
                new PCVector2(5f,4f), new PCVector2(6f,4f), new PCVector2(7f,4f),
                new PCVector2(1f,3f), new PCVector2(7f,3f), new PCVector2(1f,2f),
                new PCVector2(7f,2f), new PCVector2(1f,1f), new PCVector2(7f,1f),
                new PCVector2(1f,0f), new PCVector2(7f,0f),
            }));
            /* I */
            m_banner.Add(73,new PCArray(new PCVector2[] {
                new PCVector2(2f,8f), new PCVector2(3f,8f), new PCVector2(4f,8f),
                new PCVector2(5f,8f), new PCVector2(6f,8f), new PCVector2(4f,7f),
                new PCVector2(4f,6f), new PCVector2(4f,5f), new PCVector2(4f,4f),
                new PCVector2(4f,3f), new PCVector2(4f,2f), new PCVector2(4f,1f),
                new PCVector2(2f,0f), new PCVector2(3f,0f), new PCVector2(4f,0f),
                new PCVector2(5f,0f), new PCVector2(6f,0f),
            }));
            /* J */
            m_banner.Add(74,new PCArray(new PCVector2[] {
                new PCVector2(3f,8f), new PCVector2(4f,8f), new PCVector2(5f,8f),
                new PCVector2(6f,8f), new PCVector2(7f,8f), new PCVector2(5f,7f),
                new PCVector2(5f,6f), new PCVector2(5f,5f), new PCVector2(5f,4f),
                new PCVector2(5f,3f), new PCVector2(5f,2f), new PCVector2(1f,1f),
                new PCVector2(5f,1f), new PCVector2(2f,0f), new PCVector2(3f,0f),
                new PCVector2(4f,0f),
            }));
            /* K */
            m_banner.Add(75,new PCArray(new PCVector2[] {
                new PCVector2(1f,8f), new PCVector2(7f,8f), new PCVector2(1f,7f),
                new PCVector2(6f,7f), new PCVector2(1f,6f), new PCVector2(5f,6f),
                new PCVector2(1f,5f), new PCVector2(4f,5f), new PCVector2(1f,4f),
                new PCVector2(3f,4f), new PCVector2(1f,3f), new PCVector2(2f,3f),
                new PCVector2(4f,3f), new PCVector2(1f,2f), new PCVector2(5f,2f),
                new PCVector2(1f,1f), new PCVector2(6f,1f), new PCVector2(1f,0f),
                new PCVector2(7f,0f),
            }));
            /* L */
            m_banner.Add(76,new PCArray(new PCVector2[] {
                new PCVector2(1f,8f), new PCVector2(1f,7f), new PCVector2(1f,6f),
                new PCVector2(1f,5f), new PCVector2(1f,4f), new PCVector2(1f,3f),
                new PCVector2(1f,2f), new PCVector2(1f,1f), new PCVector2(1f,0f),
                new PCVector2(2f,0f), new PCVector2(3f,0f), new PCVector2(4f,0f),
                new PCVector2(5f,0f), new PCVector2(6f,0f), new PCVector2(7f,0f),
            }));
            /* M */
            m_banner.Add(77,new PCArray(new PCVector2[] {
                new PCVector2(1f,8f), new PCVector2(7f,8f), new PCVector2(1f,7f),
                new PCVector2(2f,7f), new PCVector2(6f,7f), new PCVector2(7f,7f),
                new PCVector2(1f,6f), new PCVector2(3f,6f), new PCVector2(5f,6f),
                new PCVector2(7f,6f), new PCVector2(1f,5f), new PCVector2(4f,5f),
                new PCVector2(7f,5f), new PCVector2(1f,4f), new PCVector2(7f,4f),
                new PCVector2(1f,3f), new PCVector2(7f,3f), new PCVector2(1f,2f),
                new PCVector2(7f,2f), new PCVector2(1f,1f), new PCVector2(7f,1f),
                new PCVector2(1f,0f), new PCVector2(7f,0f),
            }));
            /* N */
            m_banner.Add(78,new PCArray(new PCVector2[] {
                new PCVector2(1f,8f), new PCVector2(7f,8f), new PCVector2(1f,7f),
                new PCVector2(2f,7f), new PCVector2(7f,7f), new PCVector2(1f,6f),
                new PCVector2(3f,6f), new PCVector2(7f,6f), new PCVector2(1f,5f),
                new PCVector2(4f,5f), new PCVector2(7f,5f), new PCVector2(1f,4f),
                new PCVector2(5f,4f), new PCVector2(7f,4f), new PCVector2(1f,3f),
                new PCVector2(6f,3f), new PCVector2(7f,3f), new PCVector2(1f,2f),
                new PCVector2(7f,2f), new PCVector2(1f,1f), new PCVector2(7f,1f),
                new PCVector2(1f,0f), new PCVector2(7f,0f),
            }));
            /* O */
            m_banner.Add(79,new PCArray(new PCVector2[] {
                new PCVector2(3f,8f), new PCVector2(4f,8f), new PCVector2(5f,8f),
                new PCVector2(2f,7f), new PCVector2(6f,7f), new PCVector2(1f,6f),
                new PCVector2(7f,6f), new PCVector2(1f,5f), new PCVector2(7f,5f),
                new PCVector2(1f,4f), new PCVector2(7f,4f), new PCVector2(1f,3f),
                new PCVector2(7f,3f), new PCVector2(1f,2f), new PCVector2(7f,2f),
                new PCVector2(2f,1f), new PCVector2(6f,1f), new PCVector2(3f,0f),
                new PCVector2(4f,0f), new PCVector2(5f,0f),
            }));
            /* P */
            m_banner.Add(80,new PCArray(new PCVector2[] {
                new PCVector2(1f,8f), new PCVector2(2f,8f), new PCVector2(3f,8f),
                new PCVector2(4f,8f), new PCVector2(5f,8f), new PCVector2(6f,8f),
                new PCVector2(1f,7f), new PCVector2(7f,7f), new PCVector2(1f,6f),
                new PCVector2(7f,6f), new PCVector2(1f,5f), new PCVector2(7f,5f),
                new PCVector2(1f,4f), new PCVector2(2f,4f), new PCVector2(3f,4f),
                new PCVector2(4f,4f), new PCVector2(5f,4f), new PCVector2(6f,4f),
                new PCVector2(1f,3f), new PCVector2(1f,2f), new PCVector2(1f,1f),
                new PCVector2(1f,0f),
            }));
            /* Q */
            m_banner.Add(81,new PCArray(new PCVector2[] {
                new PCVector2(3f,8f), new PCVector2(4f,8f), new PCVector2(5f,8f),
                new PCVector2(2f,7f), new PCVector2(6f,7f), new PCVector2(1f,6f),
                new PCVector2(7f,6f), new PCVector2(1f,5f), new PCVector2(7f,5f),
                new PCVector2(1f,4f), new PCVector2(7f,4f), new PCVector2(1f,3f),
                new PCVector2(4f,3f), new PCVector2(7f,3f), new PCVector2(1f,2f),
                new PCVector2(5f,2f), new PCVector2(7f,2f), new PCVector2(2f,1f),
                new PCVector2(6f,1f), new PCVector2(3f,0f), new PCVector2(4f,0f),
                new PCVector2(5f,0f), new PCVector2(7f,0f),
            }));
            /* R */
            m_banner.Add(82,new PCArray(new PCVector2[] {
                new PCVector2(1f,8f), new PCVector2(2f,8f), new PCVector2(3f,8f),
                new PCVector2(4f,8f), new PCVector2(5f,8f), new PCVector2(6f,8f),
                new PCVector2(1f,7f), new PCVector2(7f,7f), new PCVector2(1f,6f),
                new PCVector2(7f,6f), new PCVector2(1f,5f), new PCVector2(7f,5f),
                new PCVector2(1f,4f), new PCVector2(2f,4f), new PCVector2(3f,4f),
                new PCVector2(4f,4f), new PCVector2(5f,4f), new PCVector2(6f,4f),
                new PCVector2(1f,3f), new PCVector2(4f,3f), new PCVector2(1f,2f),
                new PCVector2(5f,2f), new PCVector2(1f,1f), new PCVector2(6f,1f),
                new PCVector2(1f,0f), new PCVector2(7f,0f),
            }));
            /* S */
            m_banner.Add(83,new PCArray(new PCVector2[] {
                new PCVector2(2f,8f), new PCVector2(3f,8f), new PCVector2(4f,8f),
                new PCVector2(5f,8f), new PCVector2(6f,8f), new PCVector2(1f,7f),
                new PCVector2(7f,7f), new PCVector2(1f,6f), new PCVector2(1f,5f),
                new PCVector2(2f,4f), new PCVector2(3f,4f), new PCVector2(4f,4f),
                new PCVector2(5f,4f), new PCVector2(6f,4f), new PCVector2(7f,3f),
                new PCVector2(7f,2f), new PCVector2(1f,1f), new PCVector2(7f,1f),
                new PCVector2(2f,0f), new PCVector2(3f,0f), new PCVector2(4f,0f),
                new PCVector2(5f,0f), new PCVector2(6f,0f),
            }));
            /* T */
            m_banner.Add(84,new PCArray(new PCVector2[] {
                new PCVector2(1f,8f), new PCVector2(2f,8f), new PCVector2(3f,8f),
                new PCVector2(4f,8f), new PCVector2(5f,8f), new PCVector2(6f,8f),
                new PCVector2(7f,8f), new PCVector2(4f,7f), new PCVector2(4f,6f),
                new PCVector2(4f,5f), new PCVector2(4f,4f), new PCVector2(4f,3f),
                new PCVector2(4f,2f), new PCVector2(4f,1f), new PCVector2(4f,0f),
            }));
            /* U */
            m_banner.Add(85,new PCArray(new PCVector2[] {
                new PCVector2(1f,8f), new PCVector2(7f,8f), new PCVector2(1f,7f),
                new PCVector2(7f,7f), new PCVector2(1f,6f), new PCVector2(7f,6f),
                new PCVector2(1f,5f), new PCVector2(7f,5f), new PCVector2(1f,4f),
                new PCVector2(7f,4f), new PCVector2(1f,3f), new PCVector2(7f,3f),
                new PCVector2(1f,2f), new PCVector2(7f,2f), new PCVector2(1f,1f),
                new PCVector2(7f,1f), new PCVector2(2f,0f), new PCVector2(3f,0f),
                new PCVector2(4f,0f), new PCVector2(5f,0f), new PCVector2(6f,0f),
            }));
            /* V */
            m_banner.Add(86,new PCArray(new PCVector2[] {
                new PCVector2(1f,8f), new PCVector2(7f,8f), new PCVector2(1f,7f),
                new PCVector2(7f,7f), new PCVector2(1f,6f), new PCVector2(7f,6f),
                new PCVector2(2f,5f), new PCVector2(6f,5f), new PCVector2(2f,4f),
                new PCVector2(6f,4f), new PCVector2(3f,3f), new PCVector2(5f,3f),
                new PCVector2(3f,2f), new PCVector2(5f,2f), new PCVector2(4f,1f),
                new PCVector2(4f,0f),
            }));
            /* W */
            m_banner.Add(87,new PCArray(new PCVector2[] {
                new PCVector2(1f,8f), new PCVector2(7f,8f), new PCVector2(1f,7f),
                new PCVector2(7f,7f), new PCVector2(1f,6f), new PCVector2(7f,6f),
                new PCVector2(1f,5f), new PCVector2(7f,5f), new PCVector2(1f,4f),
                new PCVector2(4f,4f), new PCVector2(7f,4f), new PCVector2(1f,3f),
                new PCVector2(4f,3f), new PCVector2(7f,3f), new PCVector2(1f,2f),
                new PCVector2(3f,2f), new PCVector2(5f,2f), new PCVector2(7f,2f),
                new PCVector2(1f,1f), new PCVector2(2f,1f), new PCVector2(6f,1f),
                new PCVector2(7f,1f), new PCVector2(1f,0f), new PCVector2(7f,0f),
            }));
            /* X */
            m_banner.Add(88,new PCArray(new PCVector2[] {
                new PCVector2(1f,8f), new PCVector2(7f,8f), new PCVector2(1f,7f),
                new PCVector2(7f,7f), new PCVector2(2f,6f), new PCVector2(6f,6f),
                new PCVector2(3f,5f), new PCVector2(5f,5f), new PCVector2(4f,4f),
                new PCVector2(3f,3f), new PCVector2(5f,3f), new PCVector2(2f,2f),
                new PCVector2(6f,2f), new PCVector2(1f,1f), new PCVector2(7f,1f),
                new PCVector2(1f,0f), new PCVector2(7f,0f),
            }));
            /* Y */
            m_banner.Add(89,new PCArray(new PCVector2[] {
                new PCVector2(1f,8f), new PCVector2(7f,8f), new PCVector2(1f,7f),
                new PCVector2(7f,7f), new PCVector2(2f,6f), new PCVector2(6f,6f),
                new PCVector2(3f,5f), new PCVector2(5f,5f), new PCVector2(4f,4f),
                new PCVector2(4f,3f), new PCVector2(4f,2f), new PCVector2(4f,1f),
                new PCVector2(4f,0f),
            }));
            /* Z */
            m_banner.Add(90,new PCArray(new PCVector2[] {
                new PCVector2(1f,8f), new PCVector2(2f,8f), new PCVector2(3f,8f),
                new PCVector2(4f,8f), new PCVector2(5f,8f), new PCVector2(6f,8f),
                new PCVector2(7f,8f), new PCVector2(7f,7f), new PCVector2(6f,6f),
                new PCVector2(5f,5f), new PCVector2(4f,4f), new PCVector2(3f,3f),
                new PCVector2(2f,2f), new PCVector2(1f,1f), new PCVector2(1f,0f),
                new PCVector2(2f,0f), new PCVector2(3f,0f), new PCVector2(4f,0f),
                new PCVector2(5f,0f), new PCVector2(6f,0f), new PCVector2(7f,0f),
            }));
            /* [ */
            m_banner.Add(91,new PCArray(new PCVector2[] {
                new PCVector2(2f,8f), new PCVector2(3f,8f), new PCVector2(4f,8f),
                new PCVector2(5f,8f), new PCVector2(2f,7f), new PCVector2(2f,6f),
                new PCVector2(2f,5f), new PCVector2(2f,4f), new PCVector2(2f,3f),
                new PCVector2(2f,2f), new PCVector2(2f,1f), new PCVector2(2f,0f),
                new PCVector2(3f,0f), new PCVector2(4f,0f), new PCVector2(5f,0f),
            }));
            /* \ */
            m_banner.Add(92,new PCArray(new PCVector2[] {
                new PCVector2(1f,7f), new PCVector2(2f,6f), new PCVector2(3f,5f),
                new PCVector2(4f,4f), new PCVector2(5f,3f), new PCVector2(6f,2f),
                new PCVector2(7f,1f),
            }));
            /* ] */
            m_banner.Add(93,new PCArray(new PCVector2[] {
                new PCVector2(3f,8f), new PCVector2(4f,8f), new PCVector2(5f,8f),
                new PCVector2(6f,8f), new PCVector2(6f,7f), new PCVector2(6f,6f),
                new PCVector2(6f,5f), new PCVector2(6f,4f), new PCVector2(6f,3f),
                new PCVector2(6f,2f), new PCVector2(6f,1f), new PCVector2(3f,0f),
                new PCVector2(4f,0f), new PCVector2(5f,0f), new PCVector2(6f,0f),
            }));
            /* ^ */
            m_banner.Add(94,new PCArray(new PCVector2[] {
                new PCVector2(4f,8f), new PCVector2(3f,7f), new PCVector2(5f,7f),
                new PCVector2(2f,6f), new PCVector2(6f,6f), new PCVector2(1f,5f),
                new PCVector2(7f,5f),
            }));
            /* _ */
            m_banner.Add(95,new PCArray(new PCVector2[] {
                new PCVector2(1f,-2f), new PCVector2(2f,-2f), new PCVector2(3f,-2f),
                new PCVector2(4f,-2f), new PCVector2(5f,-2f), new PCVector2(6f,-2f),
                new PCVector2(7f,-2f),
            }));
            /* ` */
            m_banner.Add(96,new PCArray(new PCVector2[] {
                new PCVector2(3f,8f), new PCVector2(4f,8f), new PCVector2(3f,7f),
                new PCVector2(4f,7f), new PCVector2(4f,6f), new PCVector2(5f,5f),
            }));
            /* a */
            m_banner.Add(97,new PCArray(new PCVector2[] {
                new PCVector2(2f,5f), new PCVector2(3f,5f), new PCVector2(4f,5f),
                new PCVector2(5f,5f), new PCVector2(6f,4f), new PCVector2(2f,3f),
                new PCVector2(3f,3f), new PCVector2(4f,3f), new PCVector2(5f,3f),
                new PCVector2(6f,3f), new PCVector2(1f,2f), new PCVector2(7f,2f),
                new PCVector2(1f,1f), new PCVector2(6f,1f), new PCVector2(7f,1f),
                new PCVector2(2f,0f), new PCVector2(3f,0f), new PCVector2(4f,0f),
                new PCVector2(5f,0f), new PCVector2(7f,0f),
            }));
            /* b */
            m_banner.Add(98,new PCArray(new PCVector2[] {
                new PCVector2(1f,8f), new PCVector2(1f,7f), new PCVector2(1f,6f),
                new PCVector2(1f,5f), new PCVector2(3f,5f), new PCVector2(4f,5f),
                new PCVector2(5f,5f), new PCVector2(1f,4f), new PCVector2(2f,4f),
                new PCVector2(6f,4f), new PCVector2(1f,3f), new PCVector2(7f,3f),
                new PCVector2(1f,2f), new PCVector2(7f,2f), new PCVector2(1f,1f),
                new PCVector2(2f,1f), new PCVector2(6f,1f), new PCVector2(1f,0f),
                new PCVector2(3f,0f), new PCVector2(4f,0f), new PCVector2(5f,0f),
            }));
            /* c */
            m_banner.Add(99,new PCArray(new PCVector2[] {
                new PCVector2(2f,5f), new PCVector2(3f,5f), new PCVector2(4f,5f),
                new PCVector2(5f,5f), new PCVector2(1f,4f), new PCVector2(6f,4f),
                new PCVector2(1f,3f), new PCVector2(1f,2f), new PCVector2(1f,1f),
                new PCVector2(6f,1f), new PCVector2(2f,0f), new PCVector2(3f,0f),
                new PCVector2(4f,0f), new PCVector2(5f,0f),
            }));
            /* d */
            m_banner.Add(100,new PCArray(new PCVector2[] {
                new PCVector2(6f,8f), new PCVector2(6f,7f), new PCVector2(6f,6f),
                new PCVector2(2f,5f), new PCVector2(3f,5f), new PCVector2(4f,5f),
                new PCVector2(6f,5f), new PCVector2(1f,4f), new PCVector2(5f,4f),
                new PCVector2(6f,4f), new PCVector2(1f,3f), new PCVector2(6f,3f),
                new PCVector2(1f,2f), new PCVector2(6f,2f), new PCVector2(1f,1f),
                new PCVector2(5f,1f), new PCVector2(6f,1f), new PCVector2(2f,0f),
                new PCVector2(3f,0f), new PCVector2(4f,0f), new PCVector2(6f,0f),
            }));
            /* e */
            m_banner.Add(101,new PCArray(new PCVector2[] {
                new PCVector2(2f,5f), new PCVector2(3f,5f), new PCVector2(4f,5f),
                new PCVector2(5f,5f), new PCVector2(1f,4f), new PCVector2(6f,4f),
                new PCVector2(1f,3f), new PCVector2(2f,3f), new PCVector2(3f,3f),
                new PCVector2(4f,3f), new PCVector2(5f,3f), new PCVector2(6f,3f),
                new PCVector2(1f,2f), new PCVector2(1f,1f), new PCVector2(6f,1f),
                new PCVector2(2f,0f), new PCVector2(3f,0f), new PCVector2(4f,0f),
                new PCVector2(5f,0f),
            }));
            /* f */
            m_banner.Add(102,new PCArray(new PCVector2[] {
                new PCVector2(4f,8f), new PCVector2(5f,8f), new PCVector2(3f,7f),
                new PCVector2(6f,7f), new PCVector2(3f,6f), new PCVector2(3f,5f),
                new PCVector2(1f,4f), new PCVector2(2f,4f), new PCVector2(3f,4f),
                new PCVector2(4f,4f), new PCVector2(5f,4f), new PCVector2(3f,3f),
                new PCVector2(3f,2f), new PCVector2(3f,1f), new PCVector2(3f,0f),
            }));
            /* g */
            m_banner.Add(103,new PCArray(new PCVector2[] {
                new PCVector2(2f,5f), new PCVector2(3f,5f), new PCVector2(4f,5f),
                new PCVector2(6f,5f), new PCVector2(1f,4f), new PCVector2(5f,4f),
                new PCVector2(6f,4f), new PCVector2(1f,3f), new PCVector2(6f,3f),
                new PCVector2(1f,2f), new PCVector2(6f,2f), new PCVector2(1f,1f),
                new PCVector2(5f,1f), new PCVector2(6f,1f), new PCVector2(2f,0f),
                new PCVector2(3f,0f), new PCVector2(4f,0f), new PCVector2(6f,0f),
                new PCVector2(6f,-1f), new PCVector2(1f,-2f), new PCVector2(6f,-2f),
                new PCVector2(2f,-3f), new PCVector2(3f,-3f), new PCVector2(4f,-3f),
                new PCVector2(5f,-3f),
            }));
            /* h */
            m_banner.Add(104,new PCArray(new PCVector2[] {
                new PCVector2(1f,8f), new PCVector2(1f,7f), new PCVector2(1f,6f),
                new PCVector2(1f,5f), new PCVector2(3f,5f), new PCVector2(4f,5f),
                new PCVector2(5f,5f), new PCVector2(1f,4f), new PCVector2(2f,4f),
                new PCVector2(6f,4f), new PCVector2(1f,3f), new PCVector2(6f,3f),
                new PCVector2(1f,2f), new PCVector2(6f,2f), new PCVector2(1f,1f),
                new PCVector2(6f,1f), new PCVector2(1f,0f), new PCVector2(6f,0f),
            }));
            /* i */
            m_banner.Add(105,new PCArray(new PCVector2[] {
                new PCVector2(4f,7f), new PCVector2(3f,5f), new PCVector2(4f,5f),
                new PCVector2(4f,4f), new PCVector2(4f,3f), new PCVector2(4f,2f),
                new PCVector2(4f,1f), new PCVector2(3f,0f), new PCVector2(4f,0f),
                new PCVector2(5f,0f),
            }));
            /* j */
            m_banner.Add(106,new PCArray(new PCVector2[] {
                new PCVector2(5f,5f), new PCVector2(6f,5f), new PCVector2(6f,4f),
                new PCVector2(6f,3f), new PCVector2(6f,2f), new PCVector2(6f,1f),
                new PCVector2(6f,0f), new PCVector2(6f,-1f), new PCVector2(2f,-2f),
                new PCVector2(6f,-2f), new PCVector2(3f,-3f), new PCVector2(4f,-3f),
                new PCVector2(5f,-3f),
            }));
            /* k */
            m_banner.Add(107,new PCArray(new PCVector2[] {
                new PCVector2(1f,8f), new PCVector2(1f,7f), new PCVector2(1f,6f),
                new PCVector2(1f,5f), new PCVector2(5f,5f), new PCVector2(1f,4f),
                new PCVector2(4f,4f), new PCVector2(1f,3f), new PCVector2(3f,3f),
                new PCVector2(1f,2f), new PCVector2(2f,2f), new PCVector2(4f,2f),
                new PCVector2(1f,1f), new PCVector2(5f,1f), new PCVector2(1f,0f),
                new PCVector2(6f,0f),
            }));
            /* l */
            m_banner.Add(108,new PCArray(new PCVector2[] {
                new PCVector2(3f,8f), new PCVector2(4f,8f), new PCVector2(4f,7f),
                new PCVector2(4f,6f), new PCVector2(4f,5f), new PCVector2(4f,4f),
                new PCVector2(4f,3f), new PCVector2(4f,2f), new PCVector2(4f,1f),
                new PCVector2(3f,0f), new PCVector2(4f,0f), new PCVector2(5f,0f),
            }));
            /* m */
            m_banner.Add(109,new PCArray(new PCVector2[] {
                new PCVector2(1f,5f), new PCVector2(3f,5f), new PCVector2(5f,5f),
                new PCVector2(6f,5f), new PCVector2(1f,4f), new PCVector2(2f,4f),
                new PCVector2(4f,4f), new PCVector2(7f,4f), new PCVector2(1f,3f),
                new PCVector2(4f,3f), new PCVector2(7f,3f), new PCVector2(1f,2f),
                new PCVector2(4f,2f), new PCVector2(7f,2f), new PCVector2(1f,1f),
                new PCVector2(4f,1f), new PCVector2(7f,1f), new PCVector2(1f,0f),
                new PCVector2(4f,0f), new PCVector2(7f,0f),
            }));
            /* n */
            m_banner.Add(110,new PCArray(new PCVector2[] {
                new PCVector2(1f,5f), new PCVector2(3f,5f), new PCVector2(4f,5f),
                new PCVector2(5f,5f), new PCVector2(1f,4f), new PCVector2(2f,4f),
                new PCVector2(6f,4f), new PCVector2(1f,3f), new PCVector2(6f,3f),
                new PCVector2(1f,2f), new PCVector2(6f,2f), new PCVector2(1f,1f),
                new PCVector2(6f,1f), new PCVector2(1f,0f), new PCVector2(6f,0f),
            }));
            /* o */
            m_banner.Add(111,new PCArray(new PCVector2[] {
                new PCVector2(2f,5f), new PCVector2(3f,5f), new PCVector2(4f,5f),
                new PCVector2(5f,5f), new PCVector2(1f,4f), new PCVector2(6f,4f),
                new PCVector2(1f,3f), new PCVector2(6f,3f), new PCVector2(1f,2f),
                new PCVector2(6f,2f), new PCVector2(1f,1f), new PCVector2(6f,1f),
                new PCVector2(2f,0f), new PCVector2(3f,0f), new PCVector2(4f,0f),
                new PCVector2(5f,0f),
            }));
            /* p */
            m_banner.Add(112,new PCArray(new PCVector2[] {
                new PCVector2(1f,5f), new PCVector2(3f,5f), new PCVector2(4f,5f),
                new PCVector2(5f,5f), new PCVector2(1f,4f), new PCVector2(2f,4f),
                new PCVector2(6f,4f), new PCVector2(1f,3f), new PCVector2(6f,3f),
                new PCVector2(1f,2f), new PCVector2(6f,2f), new PCVector2(1f,1f),
                new PCVector2(2f,1f), new PCVector2(6f,1f), new PCVector2(1f,0f),
                new PCVector2(3f,0f), new PCVector2(4f,0f), new PCVector2(5f,0f),
                new PCVector2(1f,-1f), new PCVector2(1f,-2f), new PCVector2(1f,-3f),
            }));
            /* q */
            m_banner.Add(113,new PCArray(new PCVector2[] {
                new PCVector2(2f,5f), new PCVector2(3f,5f), new PCVector2(4f,5f),
                new PCVector2(6f,5f), new PCVector2(1f,4f), new PCVector2(5f,4f),
                new PCVector2(6f,4f), new PCVector2(1f,3f), new PCVector2(6f,3f),
                new PCVector2(1f,2f), new PCVector2(6f,2f), new PCVector2(1f,1f),
                new PCVector2(5f,1f), new PCVector2(6f,1f), new PCVector2(2f,0f),
                new PCVector2(3f,0f), new PCVector2(4f,0f), new PCVector2(6f,0f),
                new PCVector2(6f,-1f), new PCVector2(6f,-2f), new PCVector2(6f,-3f),
            }));
            /* r */
            m_banner.Add(114,new PCArray(new PCVector2[] {
                new PCVector2(1f,5f), new PCVector2(3f,5f), new PCVector2(4f,5f),
                new PCVector2(5f,5f), new PCVector2(1f,4f), new PCVector2(2f,4f),
                new PCVector2(6f,4f), new PCVector2(1f,3f), new PCVector2(1f,2f),
                new PCVector2(1f,1f), new PCVector2(1f,0f),
            }));
            /* s */
            m_banner.Add(115,new PCArray(new PCVector2[] {
                new PCVector2(2f,5f), new PCVector2(3f,5f), new PCVector2(4f,5f),
                new PCVector2(5f,5f), new PCVector2(1f,4f), new PCVector2(6f,4f),
                new PCVector2(2f,3f), new PCVector2(3f,3f), new PCVector2(4f,2f),
                new PCVector2(5f,2f), new PCVector2(1f,1f), new PCVector2(6f,1f),
                new PCVector2(2f,0f), new PCVector2(3f,0f), new PCVector2(4f,0f),
                new PCVector2(5f,0f),
            }));
            /* t */
            m_banner.Add(116,new PCArray(new PCVector2[] {
                new PCVector2(3f,7f), new PCVector2(3f,6f), new PCVector2(1f,5f),
                new PCVector2(2f,5f), new PCVector2(3f,5f), new PCVector2(4f,5f),
                new PCVector2(5f,5f), new PCVector2(3f,4f), new PCVector2(3f,3f),
                new PCVector2(3f,2f), new PCVector2(3f,1f), new PCVector2(6f,1f),
                new PCVector2(4f,0f), new PCVector2(5f,0f),
            }));
            /* u */
            m_banner.Add(117,new PCArray(new PCVector2[] {
                new PCVector2(1f,5f), new PCVector2(6f,5f), new PCVector2(1f,4f),
                new PCVector2(6f,4f), new PCVector2(1f,3f), new PCVector2(6f,3f),
                new PCVector2(1f,2f), new PCVector2(6f,2f), new PCVector2(1f,1f),
                new PCVector2(5f,1f), new PCVector2(6f,1f), new PCVector2(2f,0f),
                new PCVector2(3f,0f), new PCVector2(4f,0f), new PCVector2(6f,0f),
            }));
            /* v */
            m_banner.Add(118,new PCArray(new PCVector2[] {
                new PCVector2(1f,5f), new PCVector2(7f,5f), new PCVector2(1f,4f),
                new PCVector2(7f,4f), new PCVector2(1f,3f), new PCVector2(7f,3f),
                new PCVector2(2f,2f), new PCVector2(6f,2f), new PCVector2(3f,1f),
                new PCVector2(5f,1f), new PCVector2(4f,0f),
            }));
            /* w */
            m_banner.Add(119,new PCArray(new PCVector2[] {
                new PCVector2(1f,5f), new PCVector2(7f,5f), new PCVector2(1f,4f),
                new PCVector2(4f,4f), new PCVector2(7f,4f), new PCVector2(1f,3f),
                new PCVector2(4f,3f), new PCVector2(7f,3f), new PCVector2(1f,2f),
                new PCVector2(4f,2f), new PCVector2(7f,2f), new PCVector2(1f,1f),
                new PCVector2(4f,1f), new PCVector2(7f,1f), new PCVector2(2f,0f),
                new PCVector2(3f,0f), new PCVector2(5f,0f), new PCVector2(6f,0f),
            }));
            /* x */
            m_banner.Add(120,new PCArray(new PCVector2[] {
                new PCVector2(1f,5f), new PCVector2(6f,5f), new PCVector2(2f,4f),
                new PCVector2(5f,4f), new PCVector2(3f,3f), new PCVector2(4f,3f),
                new PCVector2(3f,2f), new PCVector2(4f,2f), new PCVector2(2f,1f),
                new PCVector2(5f,1f), new PCVector2(1f,0f), new PCVector2(6f,0f),
            }));
            /* y */
            m_banner.Add(121,new PCArray(new PCVector2[] {
                new PCVector2(1f,5f), new PCVector2(6f,5f), new PCVector2(1f,4f),
                new PCVector2(6f,4f), new PCVector2(1f,3f), new PCVector2(6f,3f),
                new PCVector2(1f,2f), new PCVector2(6f,2f), new PCVector2(1f,1f),
                new PCVector2(5f,1f), new PCVector2(6f,1f), new PCVector2(2f,0f),
                new PCVector2(3f,0f), new PCVector2(4f,0f), new PCVector2(6f,0f),
                new PCVector2(6f,-1f), new PCVector2(1f,-2f), new PCVector2(6f,-2f),
                new PCVector2(2f,-3f), new PCVector2(3f,-3f), new PCVector2(4f,-3f),
                new PCVector2(5f,-3f),
            }));
            /* z */
            m_banner.Add(122,new PCArray(new PCVector2[] {
                new PCVector2(1f,5f), new PCVector2(2f,5f), new PCVector2(3f,5f),
                new PCVector2(4f,5f), new PCVector2(5f,5f), new PCVector2(6f,5f),
                new PCVector2(5f,4f), new PCVector2(4f,3f), new PCVector2(3f,2f),
                new PCVector2(2f,1f), new PCVector2(1f,0f), new PCVector2(2f,0f),
                new PCVector2(3f,0f), new PCVector2(4f,0f), new PCVector2(5f,0f),
                new PCVector2(6f,0f),
            }));
            /* { */
            m_banner.Add(123,new PCArray(new PCVector2[] {
                new PCVector2(4f,8f), new PCVector2(5f,8f), new PCVector2(3f,7f),
                new PCVector2(3f,6f), new PCVector2(3f,5f), new PCVector2(2f,4f),
                new PCVector2(3f,3f), new PCVector2(3f,2f), new PCVector2(3f,1f),
                new PCVector2(4f,0f), new PCVector2(5f,0f),
            }));
            /* | */
            m_banner.Add(124,new PCArray(new PCVector2[] {
                new PCVector2(4f,8f), new PCVector2(4f,7f), new PCVector2(4f,6f),
                new PCVector2(4f,5f), new PCVector2(4f,4f), new PCVector2(4f,3f),
                new PCVector2(4f,2f), new PCVector2(4f,1f), new PCVector2(4f,0f),
            }));
            /* } */
            m_banner.Add(125,new PCArray(new PCVector2[] {
                new PCVector2(3f,8f), new PCVector2(4f,8f), new PCVector2(5f,7f),
                new PCVector2(5f,6f), new PCVector2(5f,5f), new PCVector2(6f,4f),
                new PCVector2(5f,3f), new PCVector2(5f,2f), new PCVector2(5f,1f),
                new PCVector2(3f,0f), new PCVector2(4f,0f),
            }));
            /* ~ */
            m_banner.Add(126,new PCArray(new PCVector2[] {
                new PCVector2(2f,8f), new PCVector2(3f,8f), new PCVector2(1f,7f),
                new PCVector2(4f,7f), new PCVector2(7f,7f), new PCVector2(5f,6f),
                new PCVector2(6f,6f),
            }));
            /* rub-out */
            m_banner.Add(127,new PCArray(new PCVector2[] {
                new PCVector2(2f,8f), new PCVector2(5f,8f), new PCVector2(1f,7f),
                new PCVector2(4f,7f), new PCVector2(7f,7f), new PCVector2(3f,6f),
                new PCVector2(6f,6f), new PCVector2(2f,5f), new PCVector2(5f,5f),
                new PCVector2(1f,4f), new PCVector2(4f,4f), new PCVector2(7f,4f),
                new PCVector2(3f,3f), new PCVector2(6f,3f), new PCVector2(2f,2f),
                new PCVector2(5f,2f), new PCVector2(1f,1f), new PCVector2(4f,1f),
                new PCVector2(7f,1f), new PCVector2(3f,0f), new PCVector2(6f,0f),
            }));
        }
    }
}
