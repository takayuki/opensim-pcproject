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
using log4net;
using OpenMetaverse;
using OpenSim.Framework;
using OpenSim.Framework.Communications;
using OpenSim.Region.Framework.Interfaces;
using OpenSim.Region.Framework.Scenes;
using OpenSim.Region.Framework.Scenes.Serialization;
using OpenSim.Framework.Servers;

namespace OpenSim.Region.OptionalModules.Scripting.PC
{
    public partial class PCVM : IDisposable
    {

        private bool OpCreateBox()
        {
            SceneObjectPart part = CreatePrim(CreateBox, "PC Box", CurrentPoint, Vector3.One);
            Stack.Push(new PCSceneObjectPart(part));
            return true;
        }

        private bool OpCreatePrism()
        {
            SceneObjectPart part = CreatePrim(CreatePrism, "PC Prism", CurrentPoint, Vector3.One);
            Stack.Push(new PCSceneObjectPart(part));
            return true;
        }

        private bool OpCreatePyramid()
        {
            SceneObjectPart part = CreatePrim(CreatePyramid, "PC Pyramid", CurrentPoint, Vector3.One);
            Stack.Push(new PCSceneObjectPart(part));
            return true;
        }

        private bool OpCreateTetrahedron()
        {
            SceneObjectPart part = CreatePrim(CreateTetrahedron, "PC Tetrahedron", CurrentPoint, Vector3.One);
            Stack.Push(new PCSceneObjectPart(part));
            return true;
        }

        private bool OpCreateCylinder()
        {
            SceneObjectPart part = CreatePrim(CreateCylinder, "PC Cylinder", CurrentPoint, Vector3.One);
            Stack.Push(new PCSceneObjectPart(part));
            return true;
        }

        private bool OpCreateHemicylinder()
        {
            SceneObjectPart part = CreatePrim(CreateHemicylinder, "PC Hemicylinder", CurrentPoint, Vector3.One);
            Stack.Push(new PCSceneObjectPart(part));
            return true;
        }

        private bool OpCreateCone()
        {
            SceneObjectPart part = CreatePrim(CreateCone, "PC Cone", CurrentPoint, Vector3.One);
            Stack.Push(new PCSceneObjectPart(part));
            return true;
        }

        private bool OpCreateSphere()
        {
            SceneObjectPart part = CreatePrim(CreateSphere, "PC Sphere", CurrentPoint, Vector3.One);
            Stack.Push(new PCSceneObjectPart(part));
            return true;
        }

        private bool OpCreateHemicone()
        {
            SceneObjectPart part = CreatePrim(CreateHemicone, "PC Hemicone", CurrentPoint, Vector3.One);
            Stack.Push(new PCSceneObjectPart(part));
            return true;
        }

        private bool OpCreateHemisphere()
        {
            SceneObjectPart part = CreatePrim(CreateHemisphere, "PC Hemisphere", CurrentPoint, Vector3.One);
            Stack.Push(new PCSceneObjectPart(part));
            return true;
        }

        private bool OpCreateTorus()
        {
            SceneObjectPart part = CreatePrim(CreateTorus, "PC Torus", CurrentPoint, Vector3.One);
            Stack.Push(new PCSceneObjectPart(part));
            return true;
        }

        private bool OpCreateTube()
        {
            SceneObjectPart part = CreatePrim(CreateTube, "PC Tube", CurrentPoint, Vector3.One);
            Stack.Push(new PCSceneObjectPart(part));
            return true;
        }

        private bool OpCreateRing()
        {
            SceneObjectPart part = CreatePrim(CreateRing, "PC Ring", CurrentPoint, Vector3.One);
            Stack.Push(new PCSceneObjectPart(part));
            return true;
        }

        private bool OpGetSize()
        {
            PCObj part;

            try
            {
                part = Stack.Pop();
            }
            catch (InvalidOperationException)
            {
                throw new PCEmptyStackException();
            }
            if (!(part is PCSceneObjectPart))
            {
                Stack.Push(part);
                throw new PCTypeCheckException();
            }
            Stack.Push(new PCVector3(((PCSceneObjectPart)part).val.Scale));
            return true;
        }

        private bool OpSetSize()
        {
            PCObj param;
            PCObj part;

            try
            {
                param = Stack.Pop();
            }
            catch (InvalidOperationException)
            {
                throw new PCEmptyStackException();
            }
            if (!(param is PCVector3))
            {
                Stack.Push(param);
                throw new PCTypeCheckException();
            }
            try
            {
                part = Stack.Pop();
            }
            catch (InvalidOperationException)
            {
                throw new PCEmptyStackException();
            }
            if (!(part is PCSceneObjectPart))
            {
                Stack.Push(part);
                throw new PCTypeCheckException();
            }
            ((PCSceneObjectPart)part).val.Scale = ((PCVector3)param).val;
            ((PCSceneObjectPart)part).val.ScheduleFullUpdate();
            return true;
        }

        private bool OpGetPathcut()
        {
            PCObj part;

            try
            {
                part = Stack.Pop();
            }
            catch (InvalidOperationException)
            {
                throw new PCEmptyStackException();
            }
            if (!(part is PCSceneObjectPart))
            {
                Stack.Push(part);
                throw new PCTypeCheckException();
            }

            float begin, end;
            GetPathcut(((PCSceneObjectPart)part).val, out begin, out end);
            Stack.Push(new PCVector2(begin, end));
            return true;
        }

        private bool OpSetPathcut()
        {
            PCObj param;
            PCObj part;

            try
            {
                param = Stack.Pop();
            }
            catch (InvalidOperationException)
            {
                throw new PCEmptyStackException();
            }
            if (!(param is PCVector2))
            {
                Stack.Push(param);
                throw new PCTypeCheckException();
            }
            try
            {
                part = Stack.Pop();
            }
            catch (InvalidOperationException)
            {
                throw new PCEmptyStackException();
            }
            if (!(part is PCSceneObjectPart))
            {
                Stack.Push(part);
                throw new PCTypeCheckException();
            }

            try
            {
                SetPathcut(((PCSceneObjectPart)part).val, ((PCVector2)param).val.X, ((PCVector2)param).val.Y);
            }
            catch (OverflowException)
            {
                Stack.Push(part);
                Stack.Push(param);
                throw new PCOutOfRangeException();
            }
            return true;
        }

        private bool OpGetSlice()
        {
            PCObj part;

            try
            {
                part = Stack.Pop();
            }
            catch (InvalidOperationException)
            {
                throw new PCEmptyStackException();
            }
            if (!(part is PCSceneObjectPart))
            {
                Stack.Push(part);
                throw new PCTypeCheckException();
            }

            float begin, end;
            GetSlice(((PCSceneObjectPart)part).val, out begin, out end);
            Stack.Push(new PCVector2(begin, end));
            return true;
        }

        private bool OpSetSlice()
        {
            PCObj param;
            PCObj part;

            try
            {
                param = Stack.Pop();
            }
            catch (InvalidOperationException)
            {
                throw new PCEmptyStackException();
            }
            if (!(param is PCVector2))
            {
                Stack.Push(param);
                throw new PCTypeCheckException();
            }
            try
            {
                part = Stack.Pop();
            }
            catch (InvalidOperationException)
            {
                throw new PCEmptyStackException();
            }
            if (!(part is PCSceneObjectPart))
            {
                Stack.Push(part);
                throw new PCTypeCheckException();
            }

            try
            {
                SetSlice(((PCSceneObjectPart)part).val, ((PCVector2)param).val.X, ((PCVector2)param).val.Y);
            }
            catch (OverflowException)
            {
                Stack.Push(part);
                Stack.Push(param);
                throw new PCOutOfRangeException();
            }
            return true;
        }

        private bool OpGetDimple()
        {
            PCObj part;

            try
            {
                part = Stack.Pop();
            }
            catch (InvalidOperationException)
            {
                throw new PCEmptyStackException();
            }
            if (!(part is PCSceneObjectPart))
            {
                Stack.Push(part);
                throw new PCTypeCheckException();
            }

            float begin, end;
            GetDimple(((PCSceneObjectPart)part).val, out begin, out end);
            Stack.Push(new PCVector2(begin, end));
            return true;
        }

        private bool OpSetDimple()
        {
            PCObj param;
            PCObj part;

            try
            {
                param = Stack.Pop();
            }
            catch (InvalidOperationException)
            {
                throw new PCEmptyStackException();
            }
            if (!(param is PCVector2))
            {
                Stack.Push(param);
                throw new PCTypeCheckException();
            }
            try
            {
                part = Stack.Pop();
            }
            catch (InvalidOperationException)
            {
                throw new PCEmptyStackException();
            }
            if (!(part is PCSceneObjectPart))
            {
                Stack.Push(part);
                throw new PCTypeCheckException();
            }

            try
            {
                SetDimple(((PCSceneObjectPart)part).val, ((PCVector2)param).val.X, ((PCVector2)param).val.Y);
            }
            catch (OverflowException)
            {
                Stack.Push(part);
                Stack.Push(param);
                throw new PCOutOfRangeException();
            }
            return true;
        }

        private bool OpGetProfilecut()
        {
            PCObj part;

            try
            {
                part = Stack.Pop();
            }
            catch (InvalidOperationException)
            {
                throw new PCEmptyStackException();
            }
            if (!(part is PCSceneObjectPart))
            {
                Stack.Push(part);
                throw new PCTypeCheckException();
            }

            float begin, end;
            GetProfilecut(((PCSceneObjectPart)part).val, out begin, out end);
            Stack.Push(new PCVector2(begin, end));
            return true;
        }

        private bool OpSetProfilecut()
        {
            PCObj param;
            PCObj part;

            try
            {
                param = Stack.Pop();
            }
            catch (InvalidOperationException)
            {
                throw new PCEmptyStackException();
            }
            if (!(param is PCVector2))
            {
                Stack.Push(param);
                throw new PCTypeCheckException();
            }
            try
            {
                part = Stack.Pop();
            }
            catch (InvalidOperationException)
            {
                throw new PCEmptyStackException();
            }
            if (!(part is PCSceneObjectPart))
            {
                Stack.Push(part);
                throw new PCTypeCheckException();
            }

            try
            {
                SetProfilecut(((PCSceneObjectPart)part).val, ((PCVector2)param).val.X, ((PCVector2)param).val.Y);
            }
            catch (OverflowException)
            {
                Stack.Push(part);
                Stack.Push(param);
                throw new PCOutOfRangeException();
            }
            return true;
        }

        private bool OpGetHoleShape()
        {
            PCObj part;

            try
            {
                part = Stack.Pop();
            }
            catch (InvalidOperationException)
            {
                throw new PCEmptyStackException();
            }
            if (!(part is PCSceneObjectPart))
            {
                Stack.Push(part);
                throw new PCTypeCheckException();
            }

            int value;
            GetHollowShape(((PCSceneObjectPart)part).val, out value);
            Stack.Push(new PCInt(value));
            return true;
        }

        private bool OpSetHoleShape()
        {
            PCObj param;
            PCObj part;

            try
            {
                param = Stack.Pop();
            }
            catch (InvalidOperationException)
            {
                throw new PCEmptyStackException();
            }
            if (!(param is PCInt))
            {
                Stack.Push(param);
                throw new PCTypeCheckException();
            }
            try
            {
                part = Stack.Pop();
            }
            catch (InvalidOperationException)
            {
                throw new PCEmptyStackException();
            }
            if (!(part is PCSceneObjectPart))
            {
                Stack.Push(part);
                throw new PCTypeCheckException();
            }

            try
            {
                SetHollowShape(((PCSceneObjectPart)part).val, (ushort)((PCConst)param).ToInt());
            }
            catch (OverflowException)
            {
                Stack.Push(part);
                Stack.Push(param);
                throw new PCOutOfRangeException();
            }
            return true;
        }

        private bool OpGetHollow()
        {
            PCObj part;

            try
            {
                part = Stack.Pop();
            }
            catch (InvalidOperationException)
            {
                throw new PCEmptyStackException();
            }
            if (!(part is PCSceneObjectPart))
            {
                Stack.Push(part);
                throw new PCTypeCheckException();
            }

            float value;
            GetHollow(((PCSceneObjectPart)part).val, out value);
            Stack.Push(new PCFloat(value));
            return true;
        }

        private bool OpSetHollow()
        {
            PCObj param;
            PCObj part;

            try
            {
                param = Stack.Pop();
            }
            catch (InvalidOperationException)
            {
                throw new PCEmptyStackException();
            }
            if (!(param is PCConst))
            {
                Stack.Push(param);
                throw new PCTypeCheckException();
            }
            try
            {
                part = Stack.Pop();
            }
            catch (InvalidOperationException)
            {
                throw new PCEmptyStackException();
            }
            if (!(part is PCSceneObjectPart))
            {
                Stack.Push(part);
                throw new PCTypeCheckException();
            }

            try
            {
                SetHollow(((PCSceneObjectPart)part).val, ((PCConst)param).ToFloat());
            }
            catch (OverflowException)
            {
                Stack.Push(part);
                Stack.Push(param);
                throw new PCOutOfRangeException();
            }
            return true;
        }

        private bool OpGetSkew()
        {
            PCObj part;

            try
            {
                part = Stack.Pop();
            }
            catch (InvalidOperationException)
            {
                throw new PCEmptyStackException();
            }
            if (!(part is PCSceneObjectPart))
            {
                Stack.Push(part);
                throw new PCTypeCheckException();
            }

            float value;
            GetSkew(((PCSceneObjectPart)part).val, out value);
            Stack.Push(new PCFloat(value));
            return true;
        }

        private bool OpSetSkew()
        {
            PCObj param;
            PCObj part;

            try
            {
                param = Stack.Pop();
            }
            catch (InvalidOperationException)
            {
                throw new PCEmptyStackException();
            }
            if (!(param is PCConst))
            {
                Stack.Push(param);
                throw new PCTypeCheckException();
            }
            try
            {
                part = Stack.Pop();
            }
            catch (InvalidOperationException)
            {
                throw new PCEmptyStackException();
            }
            if (!(part is PCSceneObjectPart))
            {
                Stack.Push(part);
                throw new PCTypeCheckException();
            }

            try
            {
                SetSkew(((PCSceneObjectPart)part).val, ((PCConst)param).ToFloat());
            }
            catch (OverflowException)
            {
                Stack.Push(part);
                Stack.Push(param);
                throw new PCOutOfRangeException();
            }
            return true;
        }

        private bool OpGetTwist()
        {
            PCObj part;

            try
            {
                part = Stack.Pop();
            }
            catch (InvalidOperationException)
            {
                throw new PCEmptyStackException();
            }
            if (!(part is PCSceneObjectPart))
            {
                Stack.Push(part);
                throw new PCTypeCheckException();
            }

            float begin, end;
            GetTwist(((PCSceneObjectPart)part).val, out begin, out end);
            Stack.Push(new PCVector2(begin, end));
            return true;
        }

        private bool OpSetTwist()
        {
            PCObj param;
            PCObj part;

            try
            {
                param = Stack.Pop();
            }
            catch (InvalidOperationException)
            {
                throw new PCEmptyStackException();
            }
            if (!(param is PCVector2))
            {
                Stack.Push(param);
                throw new PCTypeCheckException();
            }
            try
            {
                part = Stack.Pop();
            }
            catch (InvalidOperationException)
            {
                throw new PCEmptyStackException();
            }
            if (!(part is PCSceneObjectPart))
            {
                Stack.Push(part);
                throw new PCTypeCheckException();
            }

            try
            {
                SetTwist(((PCSceneObjectPart)part).val, ((PCVector2)param).val.X, ((PCVector2)param).val.Y);
            }
            catch (OverflowException)
            {
                Stack.Push(part);
                Stack.Push(param);
                throw new PCOutOfRangeException();
            }
            return true;
        }

        private bool OpGetHoleSize()
        {
            PCObj part;

            try
            {
                part = Stack.Pop();
            }
            catch (InvalidOperationException)
            {
                throw new PCEmptyStackException();
            }
            if (!(part is PCSceneObjectPart))
            {
                Stack.Push(part);
                throw new PCTypeCheckException();
            }

            float x, y;
            GetHoleSize(((PCSceneObjectPart)part).val, out x, out y);
            Stack.Push(new PCVector2(x, y));
            return true;
        }

        private bool OpSetHoleSize()
        {
            PCObj param;
            PCObj part;

            try
            {
                param = Stack.Pop();
            }
            catch (InvalidOperationException)
            {
                throw new PCEmptyStackException();
            }
            if (!(param is PCVector2))
            {
                Stack.Push(param);
                throw new PCTypeCheckException();
            }
            try
            {
                part = Stack.Pop();
            }
            catch (InvalidOperationException)
            {
                throw new PCEmptyStackException();
            }
            if (!(part is PCSceneObjectPart))
            {
                Stack.Push(part);
                throw new PCTypeCheckException();
            }

            try
            {
                SetHoleSize(((PCSceneObjectPart)part).val, ((PCVector2)param).val.X, ((PCVector2)param).val.Y);
            }
            catch (OverflowException)
            {
                Stack.Push(part);
                Stack.Push(param);
                throw new PCOutOfRangeException();
            }
            return true;
        }

        private bool OpGetTaper()
        {
            PCObj part;

            try
            {
                part = Stack.Pop();
            }
            catch (InvalidOperationException)
            {
                throw new PCEmptyStackException();
            }
            if (!(part is PCSceneObjectPart))
            {
                Stack.Push(part);
                throw new PCTypeCheckException();
            }
            float x, y;
            GetTaper(((PCSceneObjectPart)part).val, out x, out y);
            Stack.Push(new PCVector2(x, y));
            return true;
        }

        private bool OpSetTaper()
        {
            PCObj param;
            PCObj part;

            try
            {
                param = Stack.Pop();
            }
            catch (InvalidOperationException)
            {
                throw new PCEmptyStackException();
            }
            if (!(param is PCVector2))
            {
                Stack.Push(param);
                throw new PCTypeCheckException();
            }
            try
            {
                part = Stack.Pop();
            }
            catch (InvalidOperationException)
            {
                throw new PCEmptyStackException();
            }
            if (!(part is PCSceneObjectPart))
            {
                Stack.Push(part);
                throw new PCTypeCheckException();
            }

            try
            {
                SetTaper(((PCSceneObjectPart)part).val, ((PCVector2)param).val.X, ((PCVector2)param).val.Y);
            }
            catch (OverflowException)
            {
                Stack.Push(part);
                Stack.Push(param);
                throw new PCOutOfRangeException();
            }
            return true;
        }

        private bool OpGetShear()
        {
            PCObj part;

            try
            {
                part = Stack.Pop();
            }
            catch (InvalidOperationException)
            {
                throw new PCEmptyStackException();
            }
            if (!(part is PCSceneObjectPart))
            {
                Stack.Push(part);
                throw new PCTypeCheckException();
            }
            float x, y;
            GetShear(((PCSceneObjectPart)part).val, out x, out y);
            Stack.Push(new PCVector2(x, y));
            return true;
        }

        private bool OpSetShear()
        {
            PCObj param;
            PCObj part;

            try
            {
                param = Stack.Pop();
            }
            catch (InvalidOperationException)
            {
                throw new PCEmptyStackException();
            }
            if (!(param is PCVector2))
            {
                Stack.Push(param);
                throw new PCTypeCheckException();
            }
            try
            {
                part = Stack.Pop();
            }
            catch (InvalidOperationException)
            {
                throw new PCEmptyStackException();
            }
            if (!(part is PCSceneObjectPart))
            {
                Stack.Push(part);
                throw new PCTypeCheckException();
            }

            try
            {
                SetShere(((PCSceneObjectPart)part).val, ((PCVector2)param).val.X, ((PCVector2)param).val.Y);
            }
            catch (OverflowException)
            {
                Stack.Push(part);
                Stack.Push(param);
                throw new PCOutOfRangeException();
            }
            return true;
        }

        private bool OpGetRadiusOffset()
        {
            PCObj part;

            try
            {
                part = Stack.Pop();
            }
            catch (InvalidOperationException)
            {
                throw new PCEmptyStackException();
            }
            if (!(part is PCSceneObjectPart))
            {
                Stack.Push(part);
                throw new PCTypeCheckException();
            }
            Stack.Push(new PCFloat(UnpackPathRadiusOffset(((PCSceneObjectPart)part).val.Shape.PathRadiusOffset)));
            return true;
        }

        private bool OpSetRadiusOffset()
        {
            PCObj param;
            PCObj part;

            try
            {
                param = Stack.Pop();
            }
            catch (InvalidOperationException)
            {
                throw new PCEmptyStackException();
            }
            if (!(param is PCConst))
            {
                Stack.Push(param);
                throw new PCTypeCheckException();
            }
            try
            {
                part = Stack.Pop();
            }
            catch (InvalidOperationException)
            {
                throw new PCEmptyStackException();
            }
            if (!(part is PCSceneObjectPart))
            {
                Stack.Push(part);
                throw new PCTypeCheckException();
            }

            try
            {
                ((PCSceneObjectPart)part).val.Shape.PathRadiusOffset = PackPathRadiusOffset(((PCConst)param).ToFloat());
                ((PCSceneObjectPart)part).val.ScheduleFullUpdate();
            }
            catch (OverflowException)
            {
                Stack.Push(part);
                Stack.Push(param);
                throw new PCOutOfRangeException();
            }
            return true;
        }

        private bool OpGetRevolution()
        {
            PCObj part;

            try
            {
                part = Stack.Pop();
            }
            catch (InvalidOperationException)
            {
                throw new PCEmptyStackException();
            }
            if (!(part is PCSceneObjectPart))
            {
                Stack.Push(part);
                throw new PCTypeCheckException();
            }
            Stack.Push(new PCFloat(UnpackPathRevolution(((PCSceneObjectPart)part).val.Shape.PathRevolutions)));
            return true;
        }

        private bool OpSetRevolution()
        {
            PCObj param;
            PCObj part;

            try
            {
                param = Stack.Pop();
            }
            catch (InvalidOperationException)
            {
                throw new PCEmptyStackException();
            }
            if (!(param is PCConst))
            {
                Stack.Push(param);
                throw new PCTypeCheckException();
            }
            try
            {
                part = Stack.Pop();
            }
            catch (InvalidOperationException)
            {
                throw new PCEmptyStackException();
            }
            if (!(part is PCSceneObjectPart))
            {
                Stack.Push(part);
                throw new PCTypeCheckException();
            }

            try
            {
                ((PCSceneObjectPart)part).val.Shape.PathRevolutions = PackPathRevolution(((PCConst)param).ToFloat());
                ((PCSceneObjectPart)part).val.ScheduleFullUpdate();
            }
            catch (OverflowException)
            {
                Stack.Push(part);
                Stack.Push(param);
                throw new PCOutOfRangeException();
            }
            return true;
        }

        private bool OpSetColor()
        {
            PCObj face;
            PCObj color;
            PCObj part;

            try
            {
                face = Stack.Pop();
            }
            catch (InvalidOperationException)
            {
                throw new PCEmptyStackException();
            }
            if (!(face is PCConst))
            {
                Stack.Push(face);
                throw new PCTypeCheckException();
            }
            try
            {
                color = Stack.Pop();
            }
            catch (InvalidOperationException)
            {
                throw new PCEmptyStackException();
            }
            if (!(color is PCVector3))
            {
                Stack.Push(color);
                throw new PCTypeCheckException();
            }
            try
            {
                part = Stack.Pop();
            }
            catch (InvalidOperationException)
            {
                throw new PCEmptyStackException();
            }
            if (!(part is PCSceneObjectPart))
            {
                Stack.Push(part);
                throw new PCTypeCheckException();
            }
            SetColor(((PCSceneObjectPart)part).val, ((PCVector3)color).val, ((PCConst)face).ToInt());
            return true;
        }

        private bool OpSetTexture()
        {
            PCObj face;
            PCObj texture;
            PCObj part;

            try
            {
                face = Stack.Pop();
            }
            catch (InvalidOperationException)
            {
                throw new PCEmptyStackException();
            }
            if (!(face is PCConst))
            {
                Stack.Push(face);
                throw new PCTypeCheckException();
            }
            try
            {
                texture = Stack.Pop();
            }
            catch (InvalidOperationException)
            {
                throw new PCEmptyStackException();
            }
            if (!(texture is PCUUID))
            {
                Stack.Push(texture);
                throw new PCTypeCheckException();
            }
            try
            {
                part = Stack.Pop();
            }
            catch (InvalidOperationException)
            {
                throw new PCEmptyStackException();
            }
            if (!(part is PCSceneObjectPart))
            {
                Stack.Push(part);
                throw new PCTypeCheckException();
            }
            SetTexture(((PCSceneObjectPart)part).val, ((PCUUID)texture).val, ((PCConst)face).ToInt());
            return true;
        }

        private bool OpSetGlow()
        {
            PCObj face;
            PCObj glow;
            PCObj part;

            try
            {
                face = Stack.Pop();
            }
            catch (InvalidOperationException)
            {
                throw new PCEmptyStackException();
            }
            if (!(face is PCConst))
            {
                Stack.Push(face);
                throw new PCTypeCheckException();
            }
            try
            {
                glow = Stack.Pop();
            }
            catch (InvalidOperationException)
            {
                throw new PCEmptyStackException();
            }
            if (!(glow is PCConst))
            {
                Stack.Push(glow);
                throw new PCTypeCheckException();
            }
            try
            {
                part = Stack.Pop();
            }
            catch (InvalidOperationException)
            {
                throw new PCEmptyStackException();
            }
            if (!(part is PCSceneObjectPart))
            {
                Stack.Push(part);
                throw new PCTypeCheckException();
            }
            SetGlow(((PCSceneObjectPart)part).val, (((PCConst)glow).ToFloat()), ((PCConst)face).ToInt());
            return true;
        }

        private bool OpSetShiny()
        {
            PCObj face;
            PCObj bump;
            PCObj shiny;
            PCObj part;

            try
            {
                face = Stack.Pop();
            }
            catch (InvalidOperationException)
            {
                throw new PCEmptyStackException();
            }
            if (!(face is PCConst))
            {
                Stack.Push(face);
                throw new PCTypeCheckException();
            }
            try
            {
                bump = Stack.Pop();
            }
            catch (InvalidOperationException)
            {
                throw new PCEmptyStackException();
            }
            if (!(bump is PCConst))
            {
                Stack.Push(bump);
                throw new PCTypeCheckException();
            }
            try
            {
                shiny = Stack.Pop();
            }
            catch (InvalidOperationException)
            {
                throw new PCEmptyStackException();
            }
            if (!(shiny is PCConst))
            {
                Stack.Push(shiny);
                throw new PCTypeCheckException();
            }
            try
            {
                part = Stack.Pop();
            }
            catch (InvalidOperationException)
            {
                throw new PCEmptyStackException();
            }
            if (!(part is PCSceneObjectPart))
            {
                Stack.Push(part);
                throw new PCTypeCheckException();
            }
            SetShiny(((PCSceneObjectPart)part).val, ((PCConst)shiny).ToInt(), (Bumpiness)((PCConst)bump).ToInt(), ((PCConst)face).ToInt());
            return true;
        }

        private bool OpSetFullBright()
        {
            PCObj face;
            PCObj flag;
            PCObj part;

            try
            {
                face = Stack.Pop();
            }
            catch (InvalidOperationException)
            {
                throw new PCEmptyStackException();
            }
            if (!(face is PCConst))
            {
                Stack.Push(face);
                throw new PCTypeCheckException();
            }
            try
            {
                flag = Stack.Pop();
            }
            catch (InvalidOperationException)
            {
                throw new PCEmptyStackException();
            }
            if (!(flag is PCBool))
            {
                Stack.Push(flag);
                throw new PCTypeCheckException();
            }
            try
            {
                part = Stack.Pop();
            }
            catch (InvalidOperationException)
            {
                throw new PCEmptyStackException();
            }
            if (!(part is PCSceneObjectPart))
            {
                Stack.Push(part);
                throw new PCTypeCheckException();
            }
            SetFullBright(((PCSceneObjectPart)part).val, ((PCBool)flag).val, ((PCConst)face).ToInt());
            return true;
        }

        private bool OpSetAlpha()
        {
            PCObj face;
            PCObj alpha;
            PCObj part;

            try
            {
                face = Stack.Pop();
            }
            catch (InvalidOperationException)
            {
                throw new PCEmptyStackException();
            }
            if (!(face is PCConst))
            {
                Stack.Push(face);
                throw new PCTypeCheckException();
            }
            try
            {
                alpha = Stack.Pop();
            }
            catch (InvalidOperationException)
            {
                throw new PCEmptyStackException();
            }
            if (!(alpha is PCConst))
            {
                Stack.Push(alpha);
                throw new PCTypeCheckException();
            }
            try
            {
                part = Stack.Pop();
            }
            catch (InvalidOperationException)
            {
                throw new PCEmptyStackException();
            }
            if (!(part is PCSceneObjectPart))
            {
                Stack.Push(part);
                throw new PCTypeCheckException();
            }
            SetAlpha(((PCSceneObjectPart)part).val, ((PCConst)alpha).ToFloat(), ((PCConst)face).ToInt());
            return true;
        }

        private bool OpGetTemporary()
        {
            PCObj part;

            try
            {
                part = Stack.Pop();
            }
            catch (InvalidOperationException)
            {
                throw new PCEmptyStackException();
            }
            if (!(part is PCSceneObjectPart))
            {
                Stack.Push(part);
                throw new PCTypeCheckException();
            }
            Stack.Push(new PCBool(GetTemporary(((PCSceneObjectPart)part).val)));
            return true;
        }

        private bool OpSetTemporary()
        {
            PCObj param;
            PCObj part;

            try
            {
                param = Stack.Pop();
            }
            catch (InvalidOperationException)
            {
                throw new PCEmptyStackException();
            }
            if (!(param is PCBool))
            {
                Stack.Push(param);
                throw new PCTypeCheckException();
            }
            try
            {
                part = Stack.Pop();
            }
            catch (InvalidOperationException)
            {
                throw new PCEmptyStackException();
            }
            if (!(part is PCSceneObjectPart))
            {
                Stack.Push(part);
                throw new PCTypeCheckException();
            }
            SetTemporary(((PCSceneObjectPart)part).val, ((PCBool)param).val);
            return true;
        }

        private bool OpGetPhantom()
        {
            PCObj part;

            try
            {
                part = Stack.Pop();
            }
            catch (InvalidOperationException)
            {
                throw new PCEmptyStackException();
            }
            if (!(part is PCSceneObjectPart))
            {
                Stack.Push(part);
                throw new PCTypeCheckException();
            }
            Stack.Push(new PCBool(GetPhantom(((PCSceneObjectPart)part).val)));
            return true;
        }

        private bool OpSetPhantom()
        {
            PCObj param;
            PCObj part;

            try
            {
                param = Stack.Pop();
            }
            catch (InvalidOperationException)
            {
                throw new PCEmptyStackException();
            }
            if (!(param is PCBool))
            {
                Stack.Push(param);
                throw new PCTypeCheckException();
            }
            try
            {
                part = Stack.Pop();
            }
            catch (InvalidOperationException)
            {
                throw new PCEmptyStackException();
            }
            if (!(part is PCSceneObjectPart))
            {
                Stack.Push(part);
                throw new PCTypeCheckException();
            }
            SetPhantom(((PCSceneObjectPart)part).val, ((PCBool)param).val);
            return true;
        }

        private bool OpGetPhysics()
        {
            PCObj part;

            try
            {
                part = Stack.Pop();
            }
            catch (InvalidOperationException)
            {
                throw new PCEmptyStackException();
            }
            if (!(part is PCSceneObjectPart))
            {
                Stack.Push(part);
                throw new PCTypeCheckException();
            }
            Stack.Push(new PCBool(GetPhysics(((PCSceneObjectPart)part).val)));
            return true;
        }

        private bool OpSetPhysics()
        {
            PCObj param;
            PCObj part;

            try
            {
                param = Stack.Pop();
            }
            catch (InvalidOperationException)
            {
                throw new PCEmptyStackException();
            }
            if (!(param is PCBool))
            {
                Stack.Push(param);
                throw new PCTypeCheckException();
            }
            try
            {
                part = Stack.Pop();
            }
            catch (InvalidOperationException)
            {
                throw new PCEmptyStackException();
            }
            if (!(part is PCSceneObjectPart))
            {
                Stack.Push(part);
                throw new PCTypeCheckException();
            }
            SetPhysics(((PCSceneObjectPart)part).val, ((PCBool)param).val);
            return true;
        }

        private static ushort PackBeginCut(float value)
        {
            return Primitive.PackBeginCut(Util.Clamp<float>(value, 0, 1.0f));
        }

        private static ushort PackEndCut(float value)
        {
            return Primitive.PackEndCut(Util.Clamp<float>(value, 0, 1.0f));
        }

        private static float UnpackBeginCut(ushort value)
        {
            return Primitive.UnpackBeginCut(value);
        }

        private static float UnpackEndCut(ushort value)
        {
            return Primitive.UnpackEndCut(value);
        }

        private ushort PackHollow(float value)
        {
            return Util.Clamp<ushort>(Convert.ToUInt16(Math.Round(value * 500.0)), 0, 47500);
        }

        private float UnpackHollow(ushort value)
        {
            return (float)(Convert.ToDouble(value) / 500.0);
        }

        private sbyte PackSkew(float value)
        {
            return Util.Clamp<sbyte>(Convert.ToSByte(Math.Round(value * 100)), -95, 95);
        }

        private float UnpackSkew(sbyte value)
        {
            return (float)(Convert.ToDouble(value) / 100.0);
        }

        private static sbyte PackTwist180(float value)
        {
            return Util.Clamp<sbyte>(Convert.ToSByte(value * 100), -95, 95);
        }

        private static float UnpackTwist180(sbyte value)
        {
            return (float)Remap(Convert.ToDouble(value), -100, 100, -180, 180);
        }

        private static sbyte PackTwist360(float value)
        {
            return Util.Clamp<sbyte>(Convert.ToSByte(Remap(value, -360, 360, -100, 100)), -100, 100);
        }

        private static float UnpackTwist360(sbyte value)
        {
            return (float)Remap(Convert.ToDouble(value), -100, 100, -360, 360);
        }

        private static byte PackHoleSize(float value)
        {
            return Util.Clamp<byte>(Convert.ToByte(Math.Round((2.0f - value) * 100.0)), 100, 195);
        }

        private static float UnpackHoleSize(byte value)
        {
            return (float)(2.0 - Convert.ToDouble(value) / 100.0);
        }

        private static byte PackTaper(float value)
        {
            return Util.Clamp<byte>(Convert.ToByte(Math.Round((value + 1.0f) * 100.0)), 0, 200);
        }

        private static sbyte PackSTaper(float value)
        {
            return Util.Clamp<sbyte>(Convert.ToSByte(Math.Round(value * 100.0)), -100, 100);
        }

        private static float UnpackTaper(byte value)
        {
            return (float)(Convert.ToDouble(value) / 100.0 - 1.0);
        }

        private static float UnpackSTaper(sbyte value)
        {
            return (float)(Convert.ToDouble(value) / 100.0);
        }

        private static byte PackShear(float value)
        {
            return (byte)Util.Clamp<sbyte>(Convert.ToSByte(Math.Round(value * 100.0)), -50, 50);
        }

        private static float UnpackShear(byte value)
        {
            return (float)(Convert.ToDouble(value) / 100.0);
        }

        private static sbyte PackPathRadiusOffset(float value)
        {
            return Util.Clamp<sbyte>(Convert.ToSByte(Math.Round(value * 100.0)), -67, 67);
        }

        private static float UnpackPathRadiusOffset(sbyte value)
        {
            return (float)(Convert.ToDouble(value) / 100.0);
        }

        private static byte PackPathRevolution(float value)
        {
            return Util.Clamp<byte>(Convert.ToByte(Remap(value, 1.0, 4.0, 0.0, 200.0)), 0, 200);
        }

        private static float UnpackPathRevolution(byte value)
        {
            return (float)Remap(Convert.ToDouble(value), 0, 200, 1.0, 4.0);
        }

        private static double Remap(double input, double imin, double imax, double omin, double omax)
        {
            double output = (input - imin) / (imax - imin) * (omax - omin) + omin;
            return output;
        }

        private delegate PrimitiveBaseShape PrimitiveShapeFunction();

        private static PrimitiveShapeFunction CreateSphere = PrimitiveBaseShape.CreateSphere;
        private static PrimitiveShapeFunction CreateBox = PrimitiveBaseShape.CreateBox;

        private static PrimitiveBaseShape CreatePrism()
        {
            PrimitiveBaseShape shape = PrimitiveBaseShape.Create();

            shape.PathCurve = (byte)Extrusion.Straight;
            shape.ProfileShape = ProfileShape.Square;
            shape.PathScaleX = PackTaper(1.0f);
            shape.PathScaleY = PackTaper(0.0f);
            shape.PathShearX = PackShear(-0.5f);
            shape.PathShearY = PackShear(0.0f);
            return shape;
        }

        private static PrimitiveBaseShape CreatePyramid()
        {
            PrimitiveBaseShape shape = PrimitiveBaseShape.Create();

            shape.PathCurve = (byte)Extrusion.Straight;
            shape.ProfileShape = ProfileShape.Square;
            shape.PathScaleX = PackTaper(1.0f);
            shape.PathScaleY = PackTaper(1.0f);
            return shape;
        }

        private static PrimitiveBaseShape CreateTetrahedron()
        {
            PrimitiveBaseShape shape = PrimitiveBaseShape.Create();

            shape.PathCurve = (byte)Extrusion.Straight;
            shape.ProfileShape = ProfileShape.EquilateralTriangle;
            shape.PathScaleX = PackTaper(1.0f);
            shape.PathScaleY = PackTaper(1.0f);
            return shape;
        }

        private static PrimitiveBaseShape CreateCylinder()
        {
            PrimitiveBaseShape shape = PrimitiveBaseShape.Create();

            shape.PathCurve = (byte)Extrusion.Straight;
            shape.ProfileShape = ProfileShape.Circle;
            shape.PathScaleX = PackTaper(0);
            shape.PathScaleY = PackTaper(0);
            return shape;
        }

        private static PrimitiveBaseShape CreateHemicylinder()
        {
            PrimitiveBaseShape shape = PrimitiveBaseShape.Create();

            shape.PathCurve = (byte)Extrusion.Straight;
            shape.ProfileShape = ProfileShape.Circle;
            shape.ProfileBegin = PackBeginCut(0.25f);
            shape.ProfileEnd = PackEndCut(0.75f);
            shape.PathScaleX = PackTaper(0);
            shape.PathScaleY = PackTaper(0);
            return shape;
        }

        private static PrimitiveBaseShape CreateCone()
        {
            PrimitiveBaseShape shape = PrimitiveBaseShape.Create();

            shape.PathCurve = (byte)Extrusion.Straight;
            shape.ProfileShape = ProfileShape.Circle;
            shape.PathScaleX = PackTaper(1.0f);
            shape.PathScaleY = PackTaper(1.0f);
            return shape;
        }

        private static PrimitiveBaseShape CreateHemicone()
        {
            PrimitiveBaseShape shape = PrimitiveBaseShape.Create();

            shape.PathCurve = (byte)Extrusion.Straight;
            shape.ProfileShape = ProfileShape.Circle;
            shape.ProfileBegin = PackBeginCut(0.25f);
            shape.ProfileEnd = PackEndCut(0.75f);
            shape.PathScaleX = PackTaper(1.0f);
            shape.PathScaleY = PackTaper(1.0f);
            return shape;
        }

        private static PrimitiveBaseShape CreateHemisphere()
        {
            PrimitiveBaseShape shape = PrimitiveBaseShape.Create();

            shape.PathCurve = (byte)Extrusion.Curve1;
            shape.ProfileShape = ProfileShape.HalfCircle;
            shape.PathBegin = PackBeginCut(0.0f);
            shape.PathEnd = PackEndCut(0.5f);
            shape.PathScaleX = PackTaper(0);
            shape.PathScaleY = PackTaper(0);
            return shape;
        }

        private static PrimitiveBaseShape CreateTorus()
        {
            PrimitiveBaseShape shape = PrimitiveBaseShape.Create();

            shape.PathCurve = (byte)Extrusion.Curve1;
            shape.ProfileShape = ProfileShape.Circle;
            shape.PathScaleX = PackHoleSize(1.00f);
            shape.PathScaleY = PackHoleSize(0.25f);
            shape.PathTaperX = PackSTaper(0.0f);
            shape.PathTaperY = PackSTaper(0.0f);
            shape.ProfileBegin = PackBeginCut(0.0f);
            shape.ProfileEnd = PackEndCut(1.0f);
            return shape;
        }

        private static PrimitiveBaseShape CreateTube()
        {
            PrimitiveBaseShape shape = PrimitiveBaseShape.Create();

            shape.PathCurve = (byte)Extrusion.Curve1;
            shape.ProfileShape = ProfileShape.Square;
            shape.PathScaleX = PackHoleSize(1.00f);
            shape.PathScaleY = PackHoleSize(0.25f);
            shape.PathTaperX = PackSTaper(0.0f);
            shape.PathTaperY = PackSTaper(0.0f);
            shape.ProfileBegin = PackBeginCut(0.0f);
            shape.ProfileEnd = PackEndCut(1.0f);
            return shape;
        }

        private static PrimitiveBaseShape CreateRing()
        {
            PrimitiveBaseShape shape = PrimitiveBaseShape.Create();

            shape.PathCurve = (byte)Extrusion.Curve1;
            shape.ProfileShape = ProfileShape.EquilateralTriangle;
            shape.PathScaleX = PackHoleSize(1.00f);
            shape.PathScaleY = PackHoleSize(0.25f);
            shape.PathTaperX = PackSTaper(0.0f);
            shape.PathTaperY = PackSTaper(0.0f);
            shape.ProfileBegin = PackBeginCut(0.0f);
            shape.ProfileEnd = PackEndCut(1.0f);
            return shape;
        }

        private SceneObjectPart CreatePrim(PrimitiveShapeFunction shapeFunction, string name, Vector3 pos, Vector3 size)
        {
            SceneObjectPart part = new SceneObjectPart(UUID.Zero, shapeFunction(), Transform(pos), Rotate(Quaternion.Identity), Vector3.Zero);
            part.Name = name;
            part.Scale = size;
            part.ObjectFlags &= ~((uint)PrimFlags.Phantom);
            part.ObjectFlags |= (uint)PrimFlags.Temporary;
            SceneObjectGroup sceneObject = new SceneObjectGroup(part);
            part.SetParent(sceneObject);
            sceneObject.SetScene(m_scene);
            return part;
        }

        private Vector3 GetPosition(SceneObjectPart part)
        {
            return InverseTransform(part.AbsolutePosition);
        }

        private void SetPosition(SceneObjectPart part, Vector3 pos)
        {
            part.UpdateGroupPosition(Transform(pos));
        }

        private Quaternion GetRotation(SceneObjectPart part)
        {
            return InverseRotate(part.RotationOffset);
        }

        private void SetRotation(SceneObjectPart part, Quaternion rot)
        {
            part.UpdateRotation(Rotate(rot));
            part.ParentGroup.AbsolutePosition = part.ParentGroup.AbsolutePosition;
        }

        private bool GetTemporary(SceneObjectPart part)
        {
            return (part.ObjectFlags & (uint)PrimFlags.Temporary) != 0;
        }

        private void SetTemporary(SceneObjectPart part, bool flag)
        {
            if (flag)
            {
                part.ObjectFlags |= (uint)PrimFlags.Temporary;
            }
            else
            {
                part.ObjectFlags &= ~((uint)PrimFlags.Temporary);
            }
        }

        private bool GetPhantom(SceneObjectPart part)
        {
            return (part.ObjectFlags & (uint)PrimFlags.Phantom) != 0;
        }

        private void SetPhantom(SceneObjectPart part, bool flag)
        {
            if (flag)
            {
                part.ObjectFlags |= (uint)PrimFlags.Phantom;
            }
            else
            {
                part.ObjectFlags &= ~((uint)PrimFlags.Phantom);
            }
        }

        private bool GetPhysics(SceneObjectPart part)
        {
            return (part.ObjectFlags & (uint)PrimFlags.Physics) != 0;
        }

        private void SetPhysics(SceneObjectPart part, bool flag)
        {
            bool isTemporary = (part.ObjectFlags & (uint)PrimFlags.Temporary) != 0;
            bool isPhantom = (part.ObjectFlags & (uint)PrimFlags.Phantom) != 0;
            part.UpdatePrimFlags(flag, isTemporary, isPhantom, part.VolumeDetectActive);
        }

        static readonly int MAX_SIDES = 9;
        static readonly int ALL_SIDES = -1;

        private void SetColor(SceneObjectPart part, Vector3 rgb, int face)
        {
            float r = rgb.X;
            float g = rgb.Y;
            float b = rgb.Z;
            Primitive.TextureEntry tex = part.Shape.Textures;
            Color4 texcolor;
            if (face >= 0 && face < MAX_SIDES)
            {
                texcolor = tex.CreateFace((uint)face).RGBA;
                texcolor.R = Util.Clip(r, 0.0f, 1.0f);
                texcolor.G = Util.Clip(g, 0.0f, 1.0f);
                texcolor.B = Util.Clip(b, 0.0f, 1.0f);
                tex.FaceTextures[face].RGBA = texcolor;
                part.UpdateTexture(tex);
                return;
            }
            else if (face == ALL_SIDES)
            {
                for (uint i = 0; i < MAX_SIDES; i++)
                {
                    if (tex.FaceTextures[i] != null)
                    {
                        texcolor = tex.FaceTextures[i].RGBA;
                        texcolor.R = Util.Clip(r, 0.0f, 1.0f);
                        texcolor.G = Util.Clip(g, 0.0f, 1.0f);
                        texcolor.B = Util.Clip(b, 0.0f, 1.0f);
                        tex.FaceTextures[i].RGBA = texcolor;
                    }
                    texcolor = tex.DefaultTexture.RGBA;
                    texcolor.R = Util.Clip(r, 0.0f, 1.0f);
                    texcolor.G = Util.Clip(g, 0.0f, 1.0f);
                    texcolor.B = Util.Clip(b, 0.0f, 1.0f);
                    tex.DefaultTexture.RGBA = texcolor;
                }
                part.UpdateTexture(tex);
                return;
            }
        }

        private void SetTexture(SceneObjectPart part, UUID textureID, int face)
        {
            Primitive.TextureEntry tex = part.Shape.Textures;

            if (face >= 0 && face < MAX_SIDES)
            {
                Primitive.TextureEntryFace texface = tex.CreateFace((uint)face);
                texface.TextureID = textureID;
                tex.FaceTextures[face] = texface;
                part.UpdateTexture(tex);
                return;
            }
            else if (face == ALL_SIDES)
            {
                for (uint i = 0; i < MAX_SIDES; i++)
                {
                    if (tex.FaceTextures[i] != null)
                    {
                        tex.FaceTextures[i].TextureID = textureID;
                    }
                }
                tex.DefaultTexture.TextureID = textureID;
                part.UpdateTexture(tex);
                return;
            }
        }

        private void SetGlow(SceneObjectPart part, float glow, int face)
        {
            Primitive.TextureEntry tex = part.Shape.Textures;
            if (face >= 0 && face < MAX_SIDES)
            {
                tex.CreateFace((uint)face);
                tex.FaceTextures[face].Glow = glow;
                part.UpdateTexture(tex);
                return;
            }
            else if (face == ALL_SIDES)
            {
                for (uint i = 0; i < MAX_SIDES; i++)
                {
                    if (tex.FaceTextures[i] != null)
                    {
                        tex.FaceTextures[i].Glow = glow;
                    }
                    tex.DefaultTexture.Glow = glow;
                }
                part.UpdateTexture(tex);
                return;
            }
        }

        private void SetShiny(SceneObjectPart part, int shiny, Bumpiness bump, int face)
        {

            Shininess sval = new Shininess();

            switch (shiny)
            {
                case 0:
                    sval = Shininess.None;
                    break;
                case 1:
                    sval = Shininess.Low;
                    break;
                case 2:
                    sval = Shininess.Medium;
                    break;
                case 3:
                    sval = Shininess.High;
                    break;
                default:
                    sval = Shininess.None;
                    break;
            }

            Primitive.TextureEntry tex = part.Shape.Textures;
            if (face >= 0 && face < MAX_SIDES)
            {
                tex.CreateFace((uint)face);
                tex.FaceTextures[face].Shiny = sval;
                tex.FaceTextures[face].Bump = bump;
                part.UpdateTexture(tex);
                return;
            }
            else if (face == ALL_SIDES)
            {
                for (uint i = 0; i < MAX_SIDES; i++)
                {
                    if (tex.FaceTextures[i] != null)
                    {
                        tex.FaceTextures[i].Shiny = sval;
                        tex.FaceTextures[i].Bump = bump; ;
                    }
                    tex.DefaultTexture.Shiny = sval;
                    tex.DefaultTexture.Bump = bump;
                }
                part.UpdateTexture(tex);
                return;
            }
        }

        private void SetFullBright(SceneObjectPart part, bool bright, int face)
        {
            Primitive.TextureEntry tex = part.Shape.Textures;
            if (face >= 0 && face < MAX_SIDES)
            {
                tex.CreateFace((uint)face);
                tex.FaceTextures[face].Fullbright = bright;
                part.UpdateTexture(tex);
                return;
            }
            else if (face == ALL_SIDES)
            {
                for (uint i = 0; i < MAX_SIDES; i++)
                {
                    if (tex.FaceTextures[i] != null)
                    {
                        tex.FaceTextures[i].Fullbright = bright;
                    }
                }
                tex.DefaultTexture.Fullbright = bright;
                part.UpdateTexture(tex);
                return;
            }
        }

        private void SetAlpha(SceneObjectPart part, float alpha, int face)
        {
            Primitive.TextureEntry tex = part.Shape.Textures;
            Color4 texcolor;
            if (face >= 0 && face < MAX_SIDES)
            {
                texcolor = tex.CreateFace((uint)face).RGBA;
                texcolor.A = Util.Clip((float)alpha, 0.0f, 1.0f);
                tex.FaceTextures[face].RGBA = texcolor;
                part.UpdateTexture(tex);
                return;
            }
            else if (face == ALL_SIDES)
            {
                for (int i = 0; i < MAX_SIDES; i++)
                {
                    if (tex.FaceTextures[i] != null)
                    {
                        texcolor = tex.FaceTextures[i].RGBA;
                        texcolor.A = Util.Clip((float)alpha, 0.0f, 1.0f);
                        tex.FaceTextures[i].RGBA = texcolor;
                    }
                }
                texcolor = tex.DefaultTexture.RGBA;
                texcolor.A = Util.Clip((float)alpha, 0.0f, 1.0f);
                tex.DefaultTexture.RGBA = texcolor;
                part.UpdateTexture(tex);
                return;
            }
        }

        private bool GetPathcut(SceneObjectPart part, out float begin, out float end)
        {
            switch (GetScriptPrimType(part.Shape))
            {
                case OpenMetaverse.PrimType.Box:
                case OpenMetaverse.PrimType.Cylinder:
                case OpenMetaverse.PrimType.Prism:
                    begin = UnpackBeginCut(part.Shape.ProfileBegin);
                    end = UnpackEndCut(part.Shape.ProfileEnd);
                    break;
                default:
                    begin = UnpackBeginCut(part.Shape.PathBegin);
                    end = UnpackEndCut(part.Shape.PathEnd);
                    break;
            }
            return true;
        }

        private bool SetPathcut(SceneObjectPart part, float begin, float end)
        {
            switch (GetScriptPrimType(part.Shape))
            {
                case OpenMetaverse.PrimType.Box:
                case OpenMetaverse.PrimType.Cylinder:
                case OpenMetaverse.PrimType.Prism:
                    part.Shape.ProfileBegin = PackBeginCut(begin);
                    part.Shape.ProfileEnd = PackEndCut(end);
                    break;
                default:
                    part.Shape.PathBegin = PackBeginCut(begin);
                    part.Shape.PathEnd = PackEndCut(end);
                    break;
            }
            part.ScheduleFullUpdate();
            return true;
        }

        private bool GetSlice(SceneObjectPart part, out float begin, out float end)
        {
            switch (GetScriptPrimType(part.Shape))
            {
                case OpenMetaverse.PrimType.Box:
                case OpenMetaverse.PrimType.Cylinder:
                case OpenMetaverse.PrimType.Prism:
                    begin = UnpackBeginCut(part.Shape.PathBegin);
                    end = UnpackEndCut(part.Shape.PathEnd);
                    return true;
                default:
                    begin = 0;
                    end = 0;
                    return false;
            }
        }

        private bool SetSlice(SceneObjectPart part, float begin, float end)
        {
            switch (GetScriptPrimType(part.Shape))
            {
                case OpenMetaverse.PrimType.Box:
                case OpenMetaverse.PrimType.Cylinder:
                case OpenMetaverse.PrimType.Prism:
                    part.Shape.PathBegin = PackBeginCut(begin);
                    part.Shape.PathEnd = PackEndCut(end);
                    part.ScheduleFullUpdate();
                    return true;
                default:
                    return false;
            }
        }

        private bool GetDimple(SceneObjectPart part, out float begin, out float end)
        {
            switch (GetScriptPrimType(part.Shape))
            {
                case OpenMetaverse.PrimType.Sphere:
                    begin = UnpackBeginCut(part.Shape.ProfileBegin);
                    end = UnpackEndCut(part.Shape.ProfileEnd);
                    return true;
                default:
                    begin = 0;
                    end = 0;
                    return false;
            }
        }

        private bool SetDimple(SceneObjectPart part, float begin, float end)
        {
            switch (GetScriptPrimType(part.Shape))
            {
                case OpenMetaverse.PrimType.Sphere:
                    part.Shape.ProfileBegin = PackBeginCut(begin);
                    part.Shape.ProfileEnd = PackEndCut(end);
                    part.ScheduleFullUpdate();
                    return true;
                default:
                    return false;
            }
        }

        private bool GetProfilecut(SceneObjectPart part, out float begin, out float end)
        {
            switch (GetScriptPrimType(part.Shape))
            {
                case OpenMetaverse.PrimType.Torus:
                case OpenMetaverse.PrimType.Tube:
                case OpenMetaverse.PrimType.Ring:
                    begin = UnpackBeginCut(part.Shape.ProfileBegin);
                    end = UnpackEndCut(part.Shape.ProfileEnd);
                    return true;
                default:
                    begin = 0;
                    end = 0;
                    return false;
            }
        }

        private bool SetProfilecut(SceneObjectPart part, float begin, float end)
        {
            switch (GetScriptPrimType(part.Shape))
            {
                case OpenMetaverse.PrimType.Torus:
                case OpenMetaverse.PrimType.Tube:
                case OpenMetaverse.PrimType.Ring:
                    part.Shape.ProfileBegin = PackBeginCut(begin);
                    part.Shape.ProfileEnd = PackEndCut(end);
                    part.ScheduleFullUpdate();
                    return true;
                default:
                    return false;
            }
        }

        private bool GetHollowShape(SceneObjectPart part, out int value)
        {
            switch (GetScriptPrimType(part.Shape))
            {
                default:
                    value = (int)part.Shape.HollowShape;
                    return true;
            }
        }

        private bool SetHollowShape(SceneObjectPart part, int value)
        {
            switch (GetScriptPrimType(part.Shape))
            {
                default:
                    part.Shape.HollowShape = (HollowShape)value;
                    part.ScheduleFullUpdate();
                    return true;
            }
        }

        private bool GetHollow(SceneObjectPart part, out float value)
        {
            switch (GetScriptPrimType(part.Shape))
            {
                default:
                    value = UnpackHollow(part.Shape.ProfileHollow);
                    return true;
            }
        }

        private bool SetHollow(SceneObjectPart part, float value)
        {
            switch (GetScriptPrimType(part.Shape))
            {
                default:
                    part.Shape.ProfileHollow = PackHollow(value);
                    part.ScheduleFullUpdate();
                    return true;
            }
        }

        private bool GetSkew(SceneObjectPart part, out float value)
        {
            switch (GetScriptPrimType(part.Shape))
            {
                case OpenMetaverse.PrimType.Torus:
                case OpenMetaverse.PrimType.Tube:
                case OpenMetaverse.PrimType.Ring:
                    value = UnpackSkew(part.Shape.PathSkew);
                    return true;
                default:
                    value = 0;
                    return false;
            }
        }

        private bool SetSkew(SceneObjectPart part, float value)
        {
            switch (GetScriptPrimType(part.Shape))
            {
                case OpenMetaverse.PrimType.Torus:
                case OpenMetaverse.PrimType.Tube:
                case OpenMetaverse.PrimType.Ring:
                    part.Shape.PathSkew = PackSkew(value);
                    part.ScheduleFullUpdate();
                    return true;
                default:
                    return false;
            }
        }

        private bool GetTwist(SceneObjectPart part, out float begin, out float end)
        {
            switch (GetScriptPrimType(part.Shape))
            {
                case OpenMetaverse.PrimType.Box:
                case OpenMetaverse.PrimType.Cylinder:
                case OpenMetaverse.PrimType.Prism:
                    begin = UnpackTwist180(part.Shape.PathTwistBegin);
                    end = UnpackTwist180(part.Shape.PathTwist);
                    return true;
                default:
                    begin = UnpackTwist360(part.Shape.PathTwistBegin);
                    end = UnpackTwist360(part.Shape.PathTwist);
                    return true;
            }
        }

        private bool SetTwist(SceneObjectPart part, float begin, float end)
        {
            switch (GetScriptPrimType(part.Shape))
            {
                case OpenMetaverse.PrimType.Box:
                case OpenMetaverse.PrimType.Cylinder:
                case OpenMetaverse.PrimType.Prism:
                    part.Shape.PathTwistBegin = PackTwist180(begin);
                    part.Shape.PathTwist = PackTwist180(end);
                    part.ScheduleFullUpdate();
                    return true;
                default:
                    part.Shape.PathTwistBegin = PackTwist360(begin);
                    part.Shape.PathTwist = PackTwist360(end);
                    part.ScheduleFullUpdate();
                    return true;
            }
        }

        private bool GetHoleSize(SceneObjectPart part, out float x, out float y)
        {
            switch (GetScriptPrimType(part.Shape))
            {
                case OpenMetaverse.PrimType.Torus:
                case OpenMetaverse.PrimType.Tube:
                case OpenMetaverse.PrimType.Ring:
                    x = UnpackHoleSize(part.Shape.PathScaleX);
                    y = UnpackHoleSize(part.Shape.PathScaleY);
                    return true;
                default:
                    x = 0;
                    y = 0;
                    return false;
            }
        }

        private bool SetHoleSize(SceneObjectPart part, float x, float y)
        {
            switch (GetScriptPrimType(part.Shape))
            {
                case OpenMetaverse.PrimType.Torus:
                case OpenMetaverse.PrimType.Tube:
                case OpenMetaverse.PrimType.Ring:
                    part.Shape.PathScaleX = PackHoleSize(x);
                    part.Shape.PathScaleY = PackHoleSize(y);
                    part.ScheduleFullUpdate();
                    return true;
                default:
                    return false;
            }
        }

        private bool GetTaper(SceneObjectPart part, out float x, out float y)
        {
            switch (GetScriptPrimType(part.Shape))
            {
                case OpenMetaverse.PrimType.Box:
                case OpenMetaverse.PrimType.Cylinder:
                case OpenMetaverse.PrimType.Prism:
                    x = UnpackTaper(part.Shape.PathScaleX);
                    y = UnpackTaper(part.Shape.PathScaleY);
                    return true;
                case OpenMetaverse.PrimType.Torus:
                case OpenMetaverse.PrimType.Tube:
                case OpenMetaverse.PrimType.Ring:
                    x = UnpackSTaper(part.Shape.PathTaperX);
                    y = UnpackSTaper(part.Shape.PathTaperY);
                    return true;
                default:
                    x = 0;
                    y = 0;
                    return false;
            }
        }

        private bool SetTaper(SceneObjectPart part, float x, float y)
        {
            switch (GetScriptPrimType(part.Shape))
            {
                case OpenMetaverse.PrimType.Box:
                case OpenMetaverse.PrimType.Cylinder:
                case OpenMetaverse.PrimType.Prism:
                    part.Shape.PathScaleX = PackTaper(x);
                    part.Shape.PathScaleY = PackTaper(y);
                    part.ScheduleFullUpdate();
                    return true;
                case OpenMetaverse.PrimType.Torus:
                case OpenMetaverse.PrimType.Tube:
                case OpenMetaverse.PrimType.Ring:
                    part.Shape.PathTaperX = PackSTaper(x);
                    part.Shape.PathTaperY = PackSTaper(y);
                    part.ScheduleFullUpdate();
                    return true;
                default:
                    return false;
            }
        }

        private bool GetShear(SceneObjectPart part, out float x, out float y)
        {
            switch (GetScriptPrimType(part.Shape))
            {
                case OpenMetaverse.PrimType.Box:
                case OpenMetaverse.PrimType.Cylinder:
                case OpenMetaverse.PrimType.Prism:
                case OpenMetaverse.PrimType.Torus:
                case OpenMetaverse.PrimType.Tube:
                case OpenMetaverse.PrimType.Ring:
                    x = UnpackShear(part.Shape.PathShearX);
                    y = UnpackShear(part.Shape.PathShearY);
                    return true;
                default:
                    x = 0;
                    y = 0;
                    return false;
            }
        }

        private bool SetShere(SceneObjectPart part, float x, float y)
        {
            switch (GetScriptPrimType(part.Shape))
            {
                case OpenMetaverse.PrimType.Box:
                case OpenMetaverse.PrimType.Cylinder:
                case OpenMetaverse.PrimType.Prism:
                case OpenMetaverse.PrimType.Torus:
                case OpenMetaverse.PrimType.Tube:
                case OpenMetaverse.PrimType.Ring:
                    part.Shape.PathShearX = PackShear(x);
                    part.Shape.PathShearY = PackShear(y);
                    part.ScheduleFullUpdate();
                    return true;
                default:
                    return false;
            }
        }

        private OpenMetaverse.PrimType GetScriptPrimType(PrimitiveBaseShape primShape)
        {
            if (primShape.SculptEntry)
                return OpenMetaverse.PrimType.Sculpt;
            if ((primShape.ProfileCurve & 0x07) == (byte)ProfileShape.Square)
            {
                if (primShape.PathCurve == (byte)Extrusion.Straight)
                    return OpenMetaverse.PrimType.Box;
                else if (primShape.PathCurve == (byte)Extrusion.Curve1)
                    return OpenMetaverse.PrimType.Tube;
            }
            else if ((primShape.ProfileCurve & 0x07) == (byte)ProfileShape.Circle)
            {
                if (primShape.PathCurve == (byte)Extrusion.Straight)
                    return OpenMetaverse.PrimType.Cylinder;
                else if (primShape.PathCurve == (byte)Extrusion.Curve1)
                    return OpenMetaverse.PrimType.Torus;
            }
            else if ((primShape.ProfileCurve & 0x07) == (byte)ProfileShape.HalfCircle)
            {
                if (primShape.PathCurve == (byte)Extrusion.Curve1 || primShape.PathCurve == (byte)Extrusion.Curve2)
                    return OpenMetaverse.PrimType.Sphere;
            }
            else if ((primShape.ProfileCurve & 0x07) == (byte)ProfileShape.EquilateralTriangle)
            {
                if (primShape.PathCurve == (byte)Extrusion.Straight)
                    return OpenMetaverse.PrimType.Prism;
                else if (primShape.PathCurve == (byte)Extrusion.Curve1)
                    return OpenMetaverse.PrimType.Ring;
            }
            return OpenMetaverse.PrimType.Box;
        }
    }
}
