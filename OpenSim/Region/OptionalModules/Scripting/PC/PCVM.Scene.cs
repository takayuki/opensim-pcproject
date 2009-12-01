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
        private List<PCSceneObjectPart> m_shownSceneObjectPart = new List<PCSceneObjectPart>();

        public void Dispose()
        {
            foreach (PCSceneObjectPart part in m_shownSceneObjectPart)
            {
                if ((part.val.Flags & PrimFlags.Temporary) != 0)
                {
                    m_scene.DeleteSceneObject(part.val.ParentGroup, false);
                }
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

        Quaternion CurrentRotate
        {
            get { return CurrentGraphicState.Rotate; }
        }

        private Vector3 Transform(Vector3 s)
        {
            Vector4 d = Vector4.Transform(new Vector4(s, 1), CurrentTransformation);
            return new Vector3(d.X/d.W, d.Y/d.W, d.Z/d.W);
        }

        private Vector3 InverseTransform(Vector3 s)
        {
            Matrix4 InverseRotateMatrix = Matrix4.CreateFromQuaternion(Quaternion.Inverse(CurrentRotate));
            Vector3 origin = Transform(Vector3.Zero);
            Vector4 d = Vector4.Transform(Vector4FromVector3(Vector3.Subtract(s, origin)), InverseRotateMatrix);
            return new Vector3(d.X / d.W, d.Y / d.W, d.Z / d.W);
        }

        private Quaternion Rotate(Quaternion s)
        {
            return CurrentRotate * s;
        }

        private Quaternion InverseRotate(Quaternion s)
        {
            return Quaternion.Inverse(CurrentRotate) * s;
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

        private bool OpSnapshot()
        {
            PCObj parts;

            try
            {
                parts = Stack.Pop();
            }
            catch (InvalidOperationException)
            {
                throw new PCEmptyStackException();
            }
            if (!(parts is PCArray))
            {
                Stack.Push(parts);
                throw new PCTypeCheckException();
            }
            foreach (PCObj o in ((PCArray)parts).val)
            {
                if (!(o is PCSceneObjectPart))
                {
                    Stack.Push(parts);
                    throw new PCTypeCheckException();
                }
            }

            PCSceneObjectPart[] items = new PCSceneObjectPart[((PCArray)parts).Length];
            for (int i = 0; i < items.Length; i++)
            {
                items[i] = (PCSceneObjectPart)((PCArray)parts).val[i];
            }
            Stack.Push(new PCSceneSnapshot(items));
            return true;
        }

        private bool OpLoadSnapshot()
        {
            PCObj snapshot;

            try
            {
                snapshot = Stack.Pop();
            }
            catch (InvalidOperationException)
            {
                throw new PCEmptyStackException();
            }
            if (!(snapshot is PCSceneSnapshot))
            {
                Stack.Push(snapshot);
                throw new PCTypeCheckException();
            }

            PCArray o = new PCArray();
            foreach (PCSceneSnapshot.SnapshotItem item in ((PCSceneSnapshot)snapshot).val)
            {
                o.Add(item.PCSceneObjectPart);
            }
            Stack.Push(o);
            return true;
        }

        private bool OpLoadScene()
        {
            PCArray o = new PCArray();

            foreach (PCSceneObjectPart part in m_shownSceneObjectPart)
            {
                o.Add(part);
            }
            Stack.Push(o);
            return true;
        }
        
        private Quaternion QuaternionFromVector4(Vector4 v)
        {
            Quaternion q;
            q = new Quaternion(v.X, v.Y, v.Z, v.W);
            q.Normalize();
            return q;
        }

        private Vector4 Vector4FromVector3(Vector3 v)
        {
            return new Vector4(v, 1.0f);
        }

        private Vector3 Vector3FromVector4(Vector4 v)
        {
            return new Vector3(v.X/v.W, v.Y/v.W, v.Z/v.W);
        }

        private bool OpTranslateSnapshot()
        {
            PCObj param;
            PCObj snapshot;

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
                snapshot = Stack.Pop();
            }
            catch (InvalidOperationException)
            {
                throw new PCEmptyStackException();
            }
            if (!(snapshot is PCSceneSnapshot))
            {
                Stack.Push(snapshot);
                throw new PCTypeCheckException();
            }

            Matrix4 rotm = Matrix4.CreateFromQuaternion(CurrentRotate);
            foreach (PCSceneSnapshot.SnapshotItem item in ((PCSceneSnapshot)snapshot).val)
            {
                PCSceneObjectPart pcpart = item.PCSceneObjectPart;
                SceneObjectPart part = pcpart.val;
                Vector3 disp = Vector3FromVector4(Vector4.Transform(((PCVector3)param).val, rotm));
                Vector3 newpos = part.AbsolutePosition + disp;
                part.UpdateGroupPosition(newpos);
            }
            return true;
        }

        private bool OpRotateSnapshot()
        {
            PCObj param;
            PCObj snapshot;

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
                snapshot = Stack.Pop();
            }
            catch (InvalidOperationException)
            {
                throw new PCEmptyStackException();
            }
            if (!(snapshot is PCSceneSnapshot))
            {
                Stack.Push(snapshot);
                throw new PCTypeCheckException();
            }

            Quaternion rotq = QuaternionFromVector4(((PCVector4)param).val);
            Matrix4 rotm = Matrix4.CreateFromQuaternion(rotq);
            Vector4 origin = Vector4FromVector3(Transform(Vector3.Zero));
            foreach (PCSceneSnapshot.SnapshotItem item in ((PCSceneSnapshot)snapshot).val)
            {
                PCSceneObjectPart pcpart = item.PCSceneObjectPart;
                SceneObjectPart part = pcpart.val;
                Vector4 pos = Vector4.Subtract(Vector4FromVector3(part.AbsolutePosition), origin);
                Vector4 newpos = Vector4.Transform(pos, rotm) + origin;
                part.UpdateGroupPosition(Vector3FromVector4(newpos));
                part.UpdateRotation(rotq * part.RotationOffset);
                part.ParentGroup.AbsolutePosition = part.ParentGroup.AbsolutePosition;
            }
            return true;
        }

        private bool OpSetSnapshotPosition()
        {
            PCObj param;
            PCObj snapshot;

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
                snapshot = Stack.Pop();
            }
            catch (InvalidOperationException)
            {
                throw new PCEmptyStackException();
            }
            if (!(snapshot is PCSceneSnapshot))
            {
                Stack.Push(snapshot);
                throw new PCTypeCheckException();
            }

            Matrix4 rotm = Matrix4.CreateFromQuaternion(CurrentRotate);
            foreach (PCSceneSnapshot.SnapshotItem item in ((PCSceneSnapshot)snapshot).val)
            {
                PCSceneObjectPart pcpart = item.PCSceneObjectPart;
                SceneObjectPart part = pcpart.val;
                Vector3 disp = Vector3FromVector4(Vector4.Transform(((PCVector3)param).val, rotm));
                Vector3 newpos = item.Position + disp;
                part.UpdateGroupPosition(newpos);
            }
            return true;
        }

        private bool OpSetSnapshotRotation()
        {
            PCObj param;
            PCObj snapshot;
            
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
                snapshot = Stack.Pop();
            }
            catch (InvalidOperationException)
            {
                throw new PCEmptyStackException();
            }
            if (!(snapshot is PCSceneSnapshot))
            {
                Stack.Push(snapshot);
                throw new PCTypeCheckException();
            }
            
            Quaternion InverseRotate = Quaternion.Inverse(CurrentRotate);
            Matrix4 InverseRotateMatrix = Matrix4.CreateFromQuaternion(InverseRotate);
            Matrix4 RotateMatrix = Matrix4.CreateFromQuaternion(CurrentRotate);
            Quaternion rotq = QuaternionFromVector4(((PCVector4)param).val);
            Matrix4 rotm = Matrix4.CreateFromQuaternion(rotq);
            Vector4 origin = Vector4FromVector3(Transform(Vector3.Zero));
            foreach (PCSceneSnapshot.SnapshotItem item in ((PCSceneSnapshot)snapshot).val)
            {
                PCSceneObjectPart pcpart = item.PCSceneObjectPart;
                SceneObjectPart part = pcpart.val;
                Vector4 disp = Vector4.Subtract(Vector4FromVector3(item.Position), origin);
                disp = Vector4.Transform(disp, InverseRotateMatrix);
                disp = Vector4.Transform(disp, rotm);
                disp = Vector4.Transform(disp, RotateMatrix);
                Vector4 newpos = disp + origin;
                part.UpdateGroupPosition(Vector3FromVector4(newpos));
                part.UpdateRotation(Rotate(rotq * InverseRotate * item.Rotation));
                part.ParentGroup.AbsolutePosition = part.ParentGroup.AbsolutePosition;
            }
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
            Stack.Push(new PCVector3(GetPosition(((PCSceneObjectPart)part).val)));
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
            SetPosition(((PCSceneObjectPart)part).val, ((PCVector3)param).val);
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
            Vector3 newpos = ((PCSceneObjectPart)part).val.ParentGroup.AbsolutePosition + ((PCVector3)param).val;
            SetPosition(((PCSceneObjectPart)part).val, newpos);
            return true;
        }

        private bool OpGetRotation()
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
            Quaternion rot = GetRotation(((PCSceneObjectPart)part).val);
            Stack.Push(new PCVector4(rot.X, rot.Y, rot.Z, rot.W));
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
            Vector4 rot = ((PCVector4)param).val;
            SetRotation(((PCSceneObjectPart)part).val, new Quaternion(rot.X, rot.Y, rot.Z, rot.W));
            return true;
        }
        
        private bool OpRez()
        {
            PCObj uuid;

            try
            {
                uuid = Stack.Pop();
            }
            catch (InvalidOperationException)
            {
                throw new PCEmptyStackException();
            }
            if (!(uuid is PCUUID))
            {
                Stack.Push(uuid);
                throw new PCTypeCheckException();
            }

            AssetBase asset = null;
            try
            {
                asset = m_scene.AssetService.Get(((PCUUID)uuid).val.ToString());
            }
            catch (Exception)
            {
            }

            if (asset == null)
            {
                Stack.Push(uuid);
                throw new PCNotFoundException(((PCUUID)uuid).val.ToString());
            }
            if (asset.Type != (uint)AssetType.Object)
            {
                Stack.Push(uuid);
                throw new PCTypeCheckException();
            }

            SceneObjectPart root = null;
            try
            {
                string data = Utils.BytesToString(asset.Data);
                SceneObjectGroup sceneObject = SceneObjectSerializer.FromOriginalXmlFormat(data);
                root = sceneObject.RootPart;
                root.ObjectFlags &= ~((uint)PrimFlags.Phantom);
                root.ObjectFlags |= (uint)PrimFlags.Temporary;
                sceneObject.SetScene(m_scene);
                SetPosition(root, CurrentPoint);
                SetRotation(root,Quaternion.Identity);
            }
            catch (Exception)
            {
                Stack.Push(uuid);
                throw new PCTypeCheckException();
            }
            Stack.Push(new PCSceneObjectPart(root));
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
            SceneObjectPart part = ((PCSceneObjectPart)o).val;
            if (m_scene.AddNewSceneObject(part.ParentGroup, false))
            {
                m_shownSceneObjectPart.Add((PCSceneObjectPart)o);
                m_log.InfoFormat("create: part: {0}", part.UUID.ToString());
            }
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
    }
}
