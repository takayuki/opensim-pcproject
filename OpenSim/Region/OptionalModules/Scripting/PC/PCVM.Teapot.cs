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
using OpenMetaverse;

namespace OpenSim.Region.OptionalModules.Scripting.PC
{
    public partial class PCVM : IDisposable
    {
        private bool OpTeapot()
        {
            PCObj mul;
            PCObj res;

            try
            {
                mul = Stack.Pop();
            }
            catch (InvalidOperationException)
            {
                throw new PCEmptyStackException();
            }
            if (!(mul is PCVector3))
            {
                Stack.Push(mul);
                throw new PCTypeCheckException();
            }
            try
            {
                res = Stack.Pop();
            }
            catch (InvalidOperationException)
            {
                throw new PCEmptyStackException();
            }
            if (!(res is PCVector2))
            {
                Stack.Push(res);
                throw new PCTypeCheckException();
            }

            int npatch = 32;
            int ru = (int)((PCVector2)res).val.X;
            int rv = (int)((PCVector2)res).val.Y;
            int i, j, p;
            PCArray lines;
            
            lines = new PCArray();
            for (p = 0; p < npatch; p++)
            {
                Vector3[,] r = GenPatch(p, ru, rv, ((PCVector3)mul).val);
                PCArray patch = new PCArray();
                for (i = 0; i <= ru; i++)
                {
                    PCArray line = new PCArray();
                    for (j = 0; j <= rv; j++)
                    {
                        Vector3 v = r[i, j];
                        line.Add(new PCVector3(v));
                    }
                    patch.Add(line);
                }
                lines.Add(patch);
            }
            Stack.Push(lines);

            lines = new PCArray();
            for (p = 0; p < npatch; p++)
            {
                Vector3[,] r = GenPatch(p, ru, rv, ((PCVector3)mul).val);
                PCArray patch = new PCArray();
                for (j = 0; j <= rv; j++)
                {
                    PCArray line = new PCArray();
                    for (i = 0; i <= ru; i++)
                    {        
                        Vector3 v = r[i, j];
                        line.Add(new PCVector3(v));
                    }
                    patch.Add(line);
                }
                lines.Add(patch);
            }
            Stack.Push(lines);

            PCArray vertex = new PCArray();
            for (p = 0; p < npatch; p++)
            {
                Vector3[] vs = GenVertex(p, ru, rv, ((PCVector3)mul).val);
                PCArray patch = new PCArray();
                foreach (Vector3 v in vs)
                {
                    patch.Add(new PCVector3(v));
                }
                vertex.Add(patch);
            }
            Stack.Push(vertex);
            return true;
        }

        private double BernPoly(int k,double t)
        {
            switch (k) {
                case 0:
                    return (1 - t) * (1 - t) * (1 - t);
                case 1:
	                return 3 * (1 - t) * (1 - t) * t;
                case 2:
	                return 3 * (1 - t) * t * t;
                case 3:
	                return t * t * t;
                default:
	                throw new PCNotImplementedException("higher bernstein polynomial");
            }
        }

        private Vector3 BezierSurf(int p, double u, double v, Vector3 mul)
        {
          int i,j;
          Vector3 r = new Vector3();

          for (i = 0; i < 4; i++) {
              for (j = 0; j < 4; j++) {
	              double w = BernPoly(i,u) * BernPoly(j,v);
	              int k = i * 4 + j;
	              r.X += (float)(w * patch[p,k,0]);
	              r.Y += (float)(w * patch[p,k,1]);
	              r.Z += (float)(w * patch[p,k,2]);
              }
          }
          return r * mul;
        }

        private Vector3[,] GenPatch(int patch, int ru, int rv, Vector3 mul)
        {    
            int i,j;
            double du = 1.0f / ((double)ru);
            double dv = 1.0f / ((double)rv);
            Vector3[,] r = new Vector3[ru+1,rv+1];
      
            for (i = 0; i <= ru; i++) {
	            for (j = 0; j <= rv; j++) {
	                r[i,j] = BezierSurf(patch,du*i,dv*j,mul);
	            }
            }
            return r;
        }

        private Vector3[] GenVertex(int patch,int ru,int rv,Vector3 mul)
        {
            List<Vector3> ver = new List<Vector3>();

            Vector3[,] r = GenPatch(patch,ru,rv,mul);
            for (int i = 0; i <= ru; i++) {
                for (int j = 0; j <= rv; j++) {
                    ver.Add(r[i,j]);
                }
            }
            return ver.ToArray();
        }

        /*
         * The Utah Teapot
         * Ryan Holmes
         * http://www.holmes3d.net/graphics/teapot/
         * 
         * generated from teapotCGA.bpt
         */
        public static double[, ,] patch =
          {{{1.4, 0.0, 2.4},
            {1.4, -0.784, 2.4},
            {0.784, -1.4, 2.4},
            {0.0, -1.4, 2.4},
            {1.3375, 0.0, 2.53125},
            {1.3375, -0.749, 2.53125},
            {0.749, -1.3375, 2.53125},
            {0.0, -1.3375, 2.53125},
            {1.4375, 0.0, 2.53125},
            {1.4375, -0.805, 2.53125},
            {0.805, -1.4375, 2.53125},
            {0.0, -1.4375, 2.53125},
            {1.5, 0.0, 2.4},
            {1.5, -0.84, 2.4},
            {0.84, -1.5, 2.4},
            {0.0, -1.5, 2.4}},
           {{0.0, -1.4, 2.4},
            {-0.784, -1.4, 2.4},
            {-1.4, -0.784, 2.4},
            {-1.4, 0.0, 2.4},
            {0.0, -1.3375, 2.53125},
            {-0.749, -1.3375, 2.53125},
            {-1.3375, -0.749, 2.53125},
            {-1.3375, 0.0, 2.53125},
            {0.0, -1.4375, 2.53125},
            {-0.805, -1.4375, 2.53125},
            {-1.4375, -0.805, 2.53125},
            {-1.4375, 0.0, 2.53125},
            {0.0, -1.5, 2.4},
            {-0.84, -1.5, 2.4},
            {-1.5, -0.84, 2.4},
            {-1.5, 0.0, 2.4}},
           {{-1.4, 0.0, 2.4},
            {-1.4, 0.784, 2.4},
            {-0.784, 1.4, 2.4},
            {0.0, 1.4, 2.4},
            {-1.3375, 0.0, 2.53125},
            {-1.3375, 0.749, 2.53125},
            {-0.749, 1.3375, 2.53125},
            {0.0, 1.3375, 2.53125},
            {-1.4375, 0.0, 2.53125},
            {-1.4375, 0.805, 2.53125},
            {-0.805, 1.4375, 2.53125},
            {0.0, 1.4375, 2.53125},
            {-1.5, 0.0, 2.4},
            {-1.5, 0.84, 2.4},
            {-0.84, 1.5, 2.4},
            {0.0, 1.5, 2.4}},
           {{0.0, 1.4, 2.4},
            {0.784, 1.4, 2.4},
            {1.4, 0.784, 2.4},
            {1.4, 0.0, 2.4},
            {0.0, 1.3375, 2.53125},
            {0.749, 1.3375, 2.53125},
            {1.3375, 0.749, 2.53125},
            {1.3375, 0.0, 2.53125},
            {0.0, 1.4375, 2.53125},
            {0.805, 1.4375, 2.53125},
            {1.4375, 0.805, 2.53125},
            {1.4375, 0.0, 2.53125},
            {0.0, 1.5, 2.4},
            {0.84, 1.5, 2.4},
            {1.5, 0.84, 2.4},
            {1.5, 0.0, 2.4}},
           {{1.5, 0.0, 2.4},
            {1.5, -0.84, 2.4},
            {0.84, -1.5, 2.4},
            {0.0, -1.5, 2.4},
            {1.75, 0.0, 1.875},
            {1.75, -0.98, 1.875},
            {0.98, -1.75, 1.875},
            {0.0, -1.75, 1.875},
            {2.0, 0.0, 1.35},
            {2.0, -1.12, 1.35},
            {1.12, -2.0, 1.35},
            {0.0, -2.0, 1.35},
            {2.0, 0.0, 0.9},
            {2.0, -1.12, 0.9},
            {1.12, -2.0, 0.9},
            {0.0, -2.0, 0.9}},
           {{0.0, -1.5, 2.4},
            {-0.84, -1.5, 2.4},
            {-1.5, -0.84, 2.4},
            {-1.5, 0.0, 2.4},
            {0.0, -1.75, 1.875},
            {-0.98, -1.75, 1.875},
            {-1.75, -0.98, 1.875},
            {-1.75, 0.0, 1.875},
            {0.0, -2.0, 1.35},
            {-1.12, -2.0, 1.35},
            {-2.0, -1.12, 1.35},
            {-2.0, 0.0, 1.35},
            {0.0, -2.0, 0.9},
            {-1.12, -2.0, 0.9},
            {-2.0, -1.12, 0.9},
            {-2.0, 0.0, 0.9}},
           {{-1.5, 0.0, 2.4},
            {-1.5, 0.84, 2.4},
            {-0.84, 1.5, 2.4},
            {0.0, 1.5, 2.4},
            {-1.75, 0.0, 1.875},
            {-1.75, 0.98, 1.875},
            {-0.98, 1.75, 1.875},
            {0.0, 1.75, 1.875},
            {-2.0, 0.0, 1.35},
            {-2.0, 1.12, 1.35},
            {-1.12, 2.0, 1.35},
            {0.0, 2.0, 1.35},
            {-2.0, 0.0, 0.9},
            {-2.0, 1.12, 0.9},
            {-1.12, 2.0, 0.9},
            {0.0, 2.0, 0.9}},
           {{0.0, 1.5, 2.4},
            {0.84, 1.5, 2.4},
            {1.5, 0.84, 2.4},
            {1.5, 0.0, 2.4},
            {0.0, 1.75, 1.875},
            {0.98, 1.75, 1.875},
            {1.75, 0.98, 1.875},
            {1.75, 0.0, 1.875},
            {0.0, 2.0, 1.35},
            {1.12, 2.0, 1.35},
            {2.0, 1.12, 1.35},
            {2.0, 0.0, 1.35},
            {0.0, 2.0, 0.9},
            {1.12, 2.0, 0.9},
            {2.0, 1.12, 0.9},
            {2.0, 0.0, 0.9}},
           {{2.0, 0.0, 0.9},
            {2.0, -1.12, 0.9},
            {1.12, -2.0, 0.9},
            {0.0, -2.0, 0.9},
            {2.0, 0.0, 0.45},
            {2.0, -1.12, 0.45},
            {1.12, -2.0, 0.45},
            {0.0, -2.0, 0.45},
            {1.5, 0.0, 0.225},
            {1.5, -0.84, 0.225},
            {0.84, -1.5, 0.225},
            {0.0, -1.5, 0.225},
            {1.5, 0.0, 0.15},
            {1.5, -0.84, 0.15},
            {0.84, -1.5, 0.15},
            {0.0, -1.5, 0.15}},
           {{0.0, -2.0, 0.9},
            {-1.12, -2.0, 0.9},
            {-2.0, -1.12, 0.9},
            {-2.0, 0.0, 0.9},
            {0.0, -2.0, 0.45},
            {-1.12, -2.0, 0.45},
            {-2.0, -1.12, 0.45},
            {-2.0, 0.0, 0.45},
            {0.0, -1.5, 0.225},
            {-0.84, -1.5, 0.225},
            {-1.5, -0.84, 0.225},
            {-1.5, 0.0, 0.225},
            {0.0, -1.5, 0.15},
            {-0.84, -1.5, 0.15},
            {-1.5, -0.84, 0.15},
            {-1.5, 0.0, 0.15}},
           {{-2.0, 0.0, 0.9},
            {-2.0, 1.12, 0.9},
            {-1.12, 2.0, 0.9},
            {0.0, 2.0, 0.9},
            {-2.0, 0.0, 0.45},
            {-2.0, 1.12, 0.45},
            {-1.12, 2.0, 0.45},
            {0.0, 2.0, 0.45},
            {-1.5, 0.0, 0.225},
            {-1.5, 0.84, 0.225},
            {-0.84, 1.5, 0.225},
            {0.0, 1.5, 0.225},
            {-1.5, 0.0, 0.15},
            {-1.5, 0.84, 0.15},
            {-0.84, 1.5, 0.15},
            {0.0, 1.5, 0.15}},
           {{0.0, 2.0, 0.9},
            {1.12, 2.0, 0.9},
            {2.0, 1.12, 0.9},
            {2.0, 0.0, 0.9},
            {0.0, 2.0, 0.45},
            {1.12, 2.0, 0.45},
            {2.0, 1.12, 0.45},
            {2.0, 0.0, 0.45},
            {0.0, 1.5, 0.225},
            {0.84, 1.5, 0.225},
            {1.5, 0.84, 0.225},
            {1.5, 0.0, 0.225},
            {0.0, 1.5, 0.15},
            {0.84, 1.5, 0.15},
            {1.5, 0.84, 0.15},
            {1.5, 0.0, 0.15}},
           {{-1.6, 0.0, 2.025},
            {-1.6, -0.3, 2.025},
            {-1.5, -0.3, 2.25},
            {-1.5, 0.0, 2.25},
            {-2.3, 0.0, 2.025},
            {-2.3, -0.3, 2.025},
            {-2.5, -0.3, 2.25},
            {-2.5, 0.0, 2.25},
            {-2.7, 0.0, 2.025},
            {-2.7, -0.3, 2.025},
            {-3.0, -0.3, 2.25},
            {-3.0, 0.0, 2.25},
            {-2.7, 0.0, 1.8},
            {-2.7, -0.3, 1.8},
            {-3.0, -0.3, 1.8},
            {-3.0, 0.0, 1.8}},
           {{-1.5, 0.0, 2.25},
            {-1.5, 0.3, 2.25},
            {-1.6, 0.3, 2.025},
            {-1.6, 0.0, 2.025},
            {-2.5, 0.0, 2.25},
            {-2.5, 0.3, 2.25},
            {-2.3, 0.3, 2.025},
            {-2.3, 0.0, 2.025},
            {-3.0, 0.0, 2.25},
            {-3.0, 0.3, 2.25},
            {-2.7, 0.3, 2.025},
            {-2.7, 0.0, 2.025},
            {-3.0, 0.0, 1.8},
            {-3.0, 0.3, 1.8},
            {-2.7, 0.3, 1.8},
            {-2.7, 0.0, 1.8}},
           {{-2.7, 0.0, 1.8},
            {-2.7, -0.3, 1.8},
            {-3.0, -0.3, 1.8},
            {-3.0, 0.0, 1.8},
            {-2.7, 0.0, 1.575},
            {-2.7, -0.3, 1.575},
            {-3.0, -0.3, 1.35},
            {-3.0, 0.0, 1.35},
            {-2.5, 0.0, 1.125},
            {-2.5, -0.3, 1.125},
            {-2.65, -0.3, 0.9375},
            {-2.65, 0.0, 0.9375},
            {-2.0, 0.0, 0.9},
            {-2.0, -0.3, 0.9},
            {-1.9, -0.3, 0.6},
            {-1.9, 0.0, 0.6}},
           {{-3.0, 0.0, 1.8},
            {-3.0, 0.3, 1.8},
            {-2.7, 0.3, 1.8},
            {-2.7, 0.0, 1.8},
            {-3.0, 0.0, 1.35},
            {-3.0, 0.3, 1.35},
            {-2.7, 0.3, 1.575},
            {-2.7, 0.0, 1.575},
            {-2.65, 0.0, 0.9375},
            {-2.65, 0.3, 0.9375},
            {-2.5, 0.3, 1.125},
            {-2.5, 0.0, 1.125},
            {-1.9, 0.0, 0.6},
            {-1.9, 0.3, 0.6},
            {-2.0, 0.3, 0.9},
            {-2.0, 0.0, 0.9}},
           {{1.7, 0.0, 1.425},
            {1.7, -0.66, 1.425},
            {1.7, -0.66, 0.6},
            {1.7, 0.0, 0.6},
            {2.6, 0.0, 1.425},
            {2.6, -0.66, 1.425},
            {3.1, -0.66, 0.825},
            {3.1, 0.0, 0.825},
            {2.3, 0.0, 2.1},
            {2.3, -0.25, 2.1},
            {2.4, -0.25, 2.025},
            {2.4, 0.0, 2.025},
            {2.7, 0.0, 2.4},
            {2.7, -0.25, 2.4},
            {3.3, -0.25, 2.4},
            {3.3, 0.0, 2.4}},
           {{1.7, 0.0, 0.6},
            {1.7, 0.66, 0.6},
            {1.7, 0.66, 1.425},
            {1.7, 0.0, 1.425},
            {3.1, 0.0, 0.825},
            {3.1, 0.66, 0.825},
            {2.6, 0.66, 1.425},
            {2.6, 0.0, 1.425},
            {2.4, 0.0, 2.025},
            {2.4, 0.25, 2.025},
            {2.3, 0.25, 2.1},
            {2.3, 0.0, 2.1},
            {3.3, 0.0, 2.4},
            {3.3, 0.25, 2.4},
            {2.7, 0.25, 2.4},
            {2.7, 0.0, 2.4}},
           {{2.7, 0.0, 2.4},
            {2.7, -0.25, 2.4},
            {3.3, -0.25, 2.4},
            {3.3, 0.0, 2.4},
            {2.8, 0.0, 2.475},
            {2.8, -0.25, 2.475},
            {3.525, -0.25, 2.49375},
            {3.525, 0.0, 2.49375},
            {2.9, 0.0, 2.475},
            {2.9, -0.15, 2.475},
            {3.45, -0.15, 2.5125},
            {3.45, 0.0, 2.5125},
            {2.8, 0.0, 2.4},
            {2.8, -0.15, 2.4},
            {3.2, -0.15, 2.4},
            {3.2, 0.0, 2.4}},
           {{3.3, 0.0, 2.4},
            {3.3, 0.25, 2.4},
            {2.7, 0.25, 2.4},
            {2.7, 0.0, 2.4},
            {3.525, 0.0, 2.49375},
            {3.525, 0.25, 2.49375},
            {2.8, 0.25, 2.475},
            {2.8, 0.0, 2.475},
            {3.45, 0.0, 2.5125},
            {3.45, 0.15, 2.5125},
            {2.9, 0.15, 2.475},
            {2.9, 0.0, 2.475},
            {3.2, 0.0, 2.4},
            {3.2, 0.15, 2.4},
            {2.8, 0.15, 2.4},
            {2.8, 0.0, 2.4}},
           {{0.0, 0.0, 3.15},
            {0.0, 0.0, 3.15},
            {0.0, 0.0, 3.15},
            {0.0, 0.0, 3.15},
            {0.8, 0.0, 3.15},
            {0.8, -0.45, 3.15},
            {0.45, -0.8, 3.15},
            {0.0, -0.8, 3.15},
            {0.0, 0.0, 2.85},
            {0.0, 0.0, 2.85},
            {0.0, 0.0, 2.85},
            {0.0, 0.0, 2.85},
            {0.2, 0.0, 2.7},
            {0.2, -0.112, 2.7},
            {0.112, -0.2, 2.7},
            {0.0, -0.2, 2.7}},
           {{0.0, 0.0, 3.15},
            {0.0, 0.0, 3.15},
            {0.0, 0.0, 3.15},
            {0.0, 0.0, 3.15},
            {0.0, -0.8, 3.15},
            {-0.45, -0.8, 3.15},
            {-0.8, -0.45, 3.15},
            {-0.8, 0.0, 3.15},
            {0.0, 0.0, 2.85},
            {0.0, 0.0, 2.85},
            {0.0, 0.0, 2.85},
            {0.0, 0.0, 2.85},
            {0.0, -0.2, 2.7},
            {-0.112, -0.2, 2.7},
            {-0.2, -0.112, 2.7},
            {-0.2, 0.0, 2.7}},
           {{0.0, 0.0, 3.15},
            {0.0, 0.0, 3.15},
            {0.0, 0.0, 3.15},
            {0.0, 0.0, 3.15},
            {-0.8, 0.0, 3.15},
            {-0.8, 0.45, 3.15},
            {-0.45, 0.8, 3.15},
            {0.0, 0.8, 3.15},
            {0.0, 0.0, 2.85},
            {0.0, 0.0, 2.85},
            {0.0, 0.0, 2.85},
            {0.0, 0.0, 2.85},
            {-0.2, 0.0, 2.7},
            {-0.2, 0.112, 2.7},
            {-0.112, 0.2, 2.7},
            {0.0, 0.2, 2.7}},
           {{0.0, 0.0, 3.15},
            {0.0, 0.0, 3.15},
            {0.0, 0.0, 3.15},
            {0.0, 0.0, 3.15},
            {0.0, 0.8, 3.15},
            {0.45, 0.8, 3.15},
            {0.8, 0.45, 3.15},
            {0.8, 0.0, 3.15},
            {0.0, 0.0, 2.85},
            {0.0, 0.0, 2.85},
            {0.0, 0.0, 2.85},
            {0.0, 0.0, 2.85},
            {0.0, 0.2, 2.7},
            {0.112, 0.2, 2.7},
            {0.2, 0.112, 2.7},
            {0.2, 0.0, 2.7}},
           {{0.2, 0.0, 2.7},
            {0.2, -0.112, 2.7},
            {0.112, -0.2, 2.7},
            {0.0, -0.2, 2.7},
            {0.4, 0.0, 2.55},
            {0.4, -0.224, 2.55},
            {0.224, -0.4, 2.55},
            {0.0, -0.4, 2.55},
            {1.3, 0.0, 2.55},
            {1.3, -0.728, 2.55},
            {0.728, -1.3, 2.55},
            {0.0, -1.3, 2.55},
            {1.3, 0.0, 2.4},
            {1.3, -0.728, 2.4},
            {0.728, -1.3, 2.4},
            {0.0, -1.3, 2.4}},
           {{0.0, -0.2, 2.7},
            {-0.112, -0.2, 2.7},
            {-0.2, -0.112, 2.7},
            {-0.2, 0.0, 2.7},
            {0.0, -0.4, 2.55},
            {-0.224, -0.4, 2.55},
            {-0.4, -0.224, 2.55},
            {-0.4, 0.0, 2.55},
            {0.0, -1.3, 2.55},
            {-0.728, -1.3, 2.55},
            {-1.3, -0.728, 2.55},
            {-1.3, 0.0, 2.55},
            {0.0, -1.3, 2.4},
            {-0.728, -1.3, 2.4},
            {-1.3, -0.728, 2.4},
            {-1.3, 0.0, 2.4}},
           {{-0.2, 0.0, 2.7},
            {-0.2, 0.112, 2.7},
            {-0.112, 0.2, 2.7},
            {0.0, 0.2, 2.7},
            {-0.4, 0.0, 2.55},
            {-0.4, 0.224, 2.55},
            {-0.224, 0.4, 2.55},
            {0.0, 0.4, 2.55},
            {-1.3, 0.0, 2.55},
            {-1.3, 0.728, 2.55},
            {-0.728, 1.3, 2.55},
            {0.0, 1.3, 2.55},
            {-1.3, 0.0, 2.4},
            {-1.3, 0.728, 2.4},
            {-0.728, 1.3, 2.4},
            {0.0, 1.3, 2.4}},
           {{0.0, 0.2, 2.7},
            {0.112, 0.2, 2.7},
            {0.2, 0.112, 2.7},
            {0.2, 0.0, 2.7},
            {0.0, 0.4, 2.55},
            {0.224, 0.4, 2.55},
            {0.4, 0.224, 2.55},
            {0.4, 0.0, 2.55},
            {0.0, 1.3, 2.55},
            {0.728, 1.3, 2.55},
            {1.3, 0.728, 2.55},
            {1.3, 0.0, 2.55},
            {0.0, 1.3, 2.4},
            {0.728, 1.3, 2.4},
            {1.3, 0.728, 2.4},
            {1.3, 0.0, 2.4}},
           {{0.0, 0.0, 0.0},
            {0.0, 0.0, 0.0},
            {0.0, 0.0, 0.0},
            {0.0, 0.0, 0.0},
            {1.425, 0.0, 0.0},
            {1.425, 0.798, 0.0},
            {0.798, 1.425, 0.0},
            {0.0, 1.425, 0.0},
            {1.5, 0.0, 0.075},
            {1.5, 0.84, 0.075},
            {0.84, 1.5, 0.075},
            {0.0, 1.5, 0.075},
            {1.5, 0.0, 0.15},
            {1.5, 0.84, 0.15},
            {0.84, 1.5, 0.15},
            {0.0, 1.5, 0.15}},
           {{0.0, 0.0, 0.0},
            {0.0, 0.0, 0.0},
            {0.0, 0.0, 0.0},
            {0.0, 0.0, 0.0},
            {0.0, 1.425, 0.0},
            {-0.798, 1.425, 0.0},
            {-1.425, 0.798, 0.0},
            {-1.425, 0.0, 0.0},
            {0.0, 1.5, 0.075},
            {-0.84, 1.5, 0.075},
            {-1.5, 0.84, 0.075},
            {-1.5, 0.0, 0.075},
            {0.0, 1.5, 0.15},
            {-0.84, 1.5, 0.15},
            {-1.5, 0.84, 0.15},
            {-1.5, 0.0, 0.15}},
           {{0.0, 0.0, 0.0},
            {0.0, 0.0, 0.0},
            {0.0, 0.0, 0.0},
            {0.0, 0.0, 0.0},
            {-1.425, 0.0, 0.0},
            {-1.425, -0.798, 0.0},
            {-0.798, -1.425, 0.0},
            {0.0, -1.425, 0.0},
            {-1.5, 0.0, 0.075},
            {-1.5, -0.84, 0.075},
            {-0.84, -1.5, 0.075},
            {0.0, -1.5, 0.075},
            {-1.5, 0.0, 0.15},
            {-1.5, -0.84, 0.15},
            {-0.84, -1.5, 0.15},
            {0.0, -1.5, 0.15}},
           {{0.0, 0.0, 0.0},
            {0.0, 0.0, 0.0},
            {0.0, 0.0, 0.0},
            {0.0, 0.0, 0.0},
            {0.0, -1.425, 0.0},
            {0.798, -1.425, 0.0},
            {1.425, -0.798, 0.0},
            {1.425, 0.0, 0.0},
            {0.0, -1.5, 0.075},
            {0.84, -1.5, 0.075},
            {1.5, -0.84, 0.075},
            {1.5, 0.0, 0.075},
            {0.0, -1.5, 0.15},
            {0.84, -1.5, 0.15},
            {1.5, -0.84, 0.15},
            {1.5, 0.0, 0.15}}};
    }
}
