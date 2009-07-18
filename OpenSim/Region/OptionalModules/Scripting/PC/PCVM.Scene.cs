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
using OpenSim.Framework.Servers;

namespace OpenSim.Region.OptionalModules.Scripting.PC
{
    internal class GraphicState
    {
        private Vector3 m_currentpoint;
        private float m_currentlinewidth;
        private Quaternion m_rotate;
        private Matrix4 m_transform;
        
        public Vector3 CurrentPoint
        {
            get { return m_currentpoint; }
            set { m_currentpoint = value; }
        }

        public float CurrentLineWidth
        {
            get { return m_currentlinewidth; }
            set { m_currentlinewidth = value; }
        }

        public Matrix4 Translate
        {
            set
            {
                m_transform = value * m_transform;
            }
        }

        public Quaternion Rotate
        {
            get { return m_rotate; }

            set
            {
                Quaternion rot = value;
                rot.Normalize();
                m_rotate = m_rotate * rot;
                m_transform = Matrix4.CreateFromQuaternion(rot) * m_transform;
            }
        }

        public Matrix4 Transformation
        {
            get { return m_transform; }
        }

        public GraphicState()
        {
            m_currentpoint = Vector3.Zero;
            m_currentlinewidth = 1.0f;
            m_rotate = Quaternion.Identity;
            m_transform = Matrix4.Identity;
        }

        public GraphicState(GraphicState g)
        {
            m_currentpoint = g.CurrentPoint;
            m_currentlinewidth = 1.0f;
            m_rotate = g.Rotate;
            m_transform = g.Transformation;
        }
    }

    public partial class PCVM : IDisposable
    {
        private List<SceneObjectPart> m_shownSceneObjectPart = new List<SceneObjectPart>();

        public void Dispose()
        {
            foreach (SceneObjectPart part in m_shownSceneObjectPart)
            {
                m_scene.DeleteSceneObject(part.ParentGroup, false);
            }
            m_shownSceneObjectPart.Clear();
        }

        Vector3 CurrentPoint
        {
            get { return CurrentGraphicState.CurrentPoint; }
            set { CurrentGraphicState.CurrentPoint = value; }
        }

        float CurrentLineWidth
        {
            get { return CurrentGraphicState.CurrentLineWidth; }
            set { CurrentGraphicState.CurrentLineWidth = value; }
        }

        Matrix4 CurrentTransformation
        {
            get { return CurrentGraphicState.Transformation; }
        }

        private Vector3 Transform(Vector3 s)
        {
            Vector4 d = Vector4.Transform(new Vector4(s, 1), CurrentTransformation);
            return new Vector3(d.X/d.W, d.Y/d.W, d.Z/d.W);
        }

        private Quaternion Rotate(Quaternion s)
        {
            return CurrentGraphicState.Rotate * s;
        }

        private bool OpGSave()
        {
            m_graphicstate_stack.Push(new GraphicState(CurrentGraphicState));
            return true;
        }

        private bool OpGRestore()
        {
            m_graphicstate_stack.Pop();
            return true;
        }

        private bool OpTranslate()
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
            if (!(o is PCVector3))
            {
                Stack.Push(o);
                throw new PCTypeCheckException();
            }
            CurrentGraphicState.Translate = Matrix4.CreateTranslation(((PCVector3)o).val);
            return true;
        }

        private bool OpRotate()
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
            if (!(o is PCVector4))
            {
                Stack.Push(o);
                throw new PCTypeCheckException();
            }
            Vector4 q = ((PCVector4)o).val;
            CurrentGraphicState.Rotate = new Quaternion(q.X,q.Y,q.Z,q.W);
            return true;
        }
        
        private bool OpCurrentPoint()
        {
            Stack.Push(new PCVector3(CurrentPoint));
            return true;
        }

        private bool OpMoveTo()
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
            if (!(o is PCVector3))
            {
                Stack.Push(o);
                throw new PCTypeCheckException();
            }
            CurrentPoint = ((PCVector3)o).val;
            return true;
        }

        private bool OpRMoveTo()
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
            if (!(o is PCVector3))
            {
                Stack.Push(o);
                throw new PCTypeCheckException();
            }
            CurrentPoint += ((PCVector3)o).val;
            return true;
        }

        private bool OpGetPosition()
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
            Stack.Push(new PCVector3(((PCSceneObjectPart)part).var.ParentGroup.AbsolutePosition));
            return true;
        }

        private bool OpSetPosition()
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
            SetPosition(((PCSceneObjectPart)part).var, ((PCVector3)param).val);
            return true;
        }

        private bool OpSetRPosition()
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
            Vector3 newpos = ((PCSceneObjectPart)part).var.ParentGroup.AbsolutePosition + ((PCVector3)param).val;
            SetPosition(((PCSceneObjectPart)part).var, newpos);
            return true;
        }

        private bool OpSetRotation()
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
            if (!(param is PCVector4))
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
            Vector4 q = ((PCVector4)param).val;
            SetRotation(((PCSceneObjectPart)part).var, Rotate(new Quaternion(q.X, q.Y, q.Z, q.W)));
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
            ((PCSceneObjectPart)part).var.Scale = ((PCVector3)param).val;
            ((PCSceneObjectPart)part).var.ScheduleFullUpdate();
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
            byte taperx = Util.Clamp<byte>(Convert.ToByte(((((PCVector2)param).val.X) + 1.0f) * 100.0f), 0, 200);
            byte tapery = Util.Clamp<byte>(Convert.ToByte(((((PCVector2)param).val.Y) + 1.0f) * 100.0f), 0, 200);
            ((PCSceneObjectPart)part).var.Shape.PathScaleX = taperx;
            ((PCSceneObjectPart)part).var.Shape.PathScaleY = tapery;
            ((PCSceneObjectPart)part).var.ScheduleFullUpdate();
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
            SetColor(((PCSceneObjectPart)part).var, ((PCVector3)color).val, ((PCConst)face).ToInt());
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
            SetTexture(((PCSceneObjectPart)part).var, ((PCUUID)texture).val, ((PCConst)face).ToInt());
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
            SetGlow(((PCSceneObjectPart)part).var, (((PCConst)glow).ToFloat()), ((PCConst)face).ToInt());
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
            SetShiny(((PCSceneObjectPart)part).var, ((PCConst)shiny).ToInt(), (Bumpiness)((PCConst)bump).ToInt(), ((PCConst)face).ToInt());
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
            SetFullBright(((PCSceneObjectPart)part).var, ((PCBool)flag).val, ((PCConst)face).ToInt());
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
            SetAlpha(((PCSceneObjectPart)part).var,((PCConst)alpha).ToFloat(),((PCConst)face).ToInt());
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
            SetPhysics(((PCSceneObjectPart)part).var, ((PCBool)param).val);
            return true;
        }

        private bool OpShow()
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
            if (!(o is PCSceneObjectPart))
            {
                Stack.Push(o);
                throw new PCTypeCheckException();
            }
            SceneObjectPart part = ((PCSceneObjectPart)o).var;
            if (m_scene.AddNewSceneObject(part.ParentGroup, false))
            {
                m_shownSceneObjectPart.Add(part);
                m_log.InfoFormat("create: part: {0}", part.UUID.ToString());
            }
            return true;
        }
        
        private static PrimitiveShapeFunction CreateSphere = PrimitiveBaseShape.CreateSphere;
        private static PrimitiveShapeFunction CreateBox = PrimitiveBaseShape.CreateBox;

        private static PrimitiveBaseShape CreateCylinder()
        {
            PrimitiveBaseShape shape = PrimitiveBaseShape.Create();

            shape.PathCurve = (byte)Extrusion.Straight;
            shape.ProfileShape = ProfileShape.Circle;
            shape.PathScaleX = 100;
            shape.PathScaleY = 100;
            return shape;
        }

        private bool OpCreateSphere()
        {
            SceneObjectPart part = CreatePrim(CreateSphere, "PC Sphere", CurrentPoint, Vector3.One);
            Stack.Push(new PCSceneObjectPart(part));
            return true;
        }

        private bool OpCreateBox()
        {
            SceneObjectPart part = CreatePrim(CreateBox, "PC Box", CurrentPoint, Vector3.One);
            Stack.Push(new PCSceneObjectPart(part));
            return true;
        }


        private bool OpCreateCylinder()
        {
            SceneObjectPart part = CreatePrim(CreateCylinder, "PC Cylinder", CurrentPoint, Vector3.One);
            Stack.Push(new PCSceneObjectPart(part));
            return true;
        }

        private bool OpCurrentLineWidth()
        {
            Stack.Push(new PCFloat(CurrentLineWidth));
            return true;
        }

        private bool OpSetCurrentLineWidth()
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
            CurrentLineWidth = ((PCConst)o).ToFloat();
            return true;
        }

        private SceneObjectPart CreateLine(Vector3 from, Vector3 to)
        {
            Vector3 diff = Vector3.Subtract(to, from);
            Vector3 size = new Vector3(CurrentLineWidth, CurrentLineWidth, diff.Length());
            Vector3 pos = Vector3.Add(from, Vector3.Multiply(diff, 0.5f));
            Vector3 tz = Vector3.Normalize(diff);
            Vector3 ty = Vector3.Cross(tz, Vector3.UnitX);
            Vector3 tx = Vector3.Cross(ty, tz);
            Quaternion rot = Util.Axes2Rot(ty, Vector3.Negate(tx), tz);
            SceneObjectPart part = CreatePrim(CreateCylinder, "PC Line", pos, size);
            SetRotation(part, rot);
            CurrentPoint = to;
            return part;
        }

        private bool OpLineTo()
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
            if (!(o is PCVector3))
            {
                Stack.Push(o);
                throw new PCTypeCheckException();
            }
            
            Vector3 from = CurrentPoint;
            Vector3 to = ((PCVector3)o).val;
            Stack.Push(new PCSceneObjectPart(CreateLine(from, to)));
            return true;
        }

        private bool OpRLineTo()
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
            if (!(o is PCVector3))
            {
                Stack.Push(o);
                throw new PCTypeCheckException();
            }
            
            Vector3 from = CurrentPoint;
            Vector3 to = Vector3.Add(from,((PCVector3)o).val);
            Stack.Push(new PCSceneObjectPart(CreateLine(from, to)));
            return true;
        }
        
        private delegate PrimitiveBaseShape PrimitiveShapeFunction();

        private SceneObjectPart CreatePrim(PrimitiveShapeFunction shapeFunction, string name, Vector3 pos, Vector3 size)
        {
            SceneObjectGroup sceneObject = new SceneObjectGroup();
            SceneObjectPart part = new SceneObjectPart(UUID.Zero, shapeFunction(), Transform(pos), Rotate(Quaternion.Identity), Vector3.Zero);
            part.Name = name;
            part.Scale = size;
            part.ObjectFlags |= (uint)PrimFlags.Phantom;
            part.ObjectFlags |= (uint)PrimFlags.Temporary;
            sceneObject.SetRootPart(part);
            return part;
        }

        private void SetPosition(SceneObjectPart part, Vector3 pos)
        {
            part.UpdateGroupPosition(Transform(pos));
        }

        private void SetRotation(SceneObjectPart part, Quaternion rot)
        {
            part.UpdateRotation(Rotate(rot));
            part.ParentGroup.AbsolutePosition = part.ParentGroup.AbsolutePosition;
        }

        private void SetPhysics(SceneObjectPart part, bool flag)
        {
            bool isTemporary = (part.ObjectFlags & (uint)PrimFlags.Temporary) != 0;

            if (flag)
            {
                part.UpdatePrimFlags(true, isTemporary, false, part.VolumeDetectActive);
            }
            else
            {
                part.UpdatePrimFlags(false, isTemporary, true, part.VolumeDetectActive);
            }
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
    }
}
