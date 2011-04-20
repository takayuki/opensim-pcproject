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
using System.IO;
using System.Threading;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.Serialization;
using OpenMetaverse;
using OpenSim.Framework;
using OpenSim.Region.CoreModules;
using OpenSim.Region.Framework.Scenes;

namespace OpenSim.Region.ScriptEngine.Shared
{
    [Serializable]
    public class EventAbortException : Exception
    {
        public EventAbortException()
        {
        }

        protected EventAbortException(
                SerializationInfo info, 
                StreamingContext context)
        {
        }
    }

    [Serializable]
    public class SelfDeleteException : Exception
    {
        public SelfDeleteException()
        {
        }

        protected SelfDeleteException(
                SerializationInfo info, 
                StreamingContext context)
        {
        }
    }

    [Serializable]
    public class ScriptDeleteException : Exception
    {
        public ScriptDeleteException()
        {
        }

        protected ScriptDeleteException(
                SerializationInfo info, 
                StreamingContext context)
        {
        }
    }

    public class DetectParams
    {
        public DetectParams()
        {
            Key = UUID.Zero;
            OffsetPos = new LSL_Types.Vector3();
            LinkNum = 0;
            Group = UUID.Zero;
            Name = String.Empty;
            Owner = UUID.Zero;
            Position = new LSL_Types.Vector3();
            Rotation = new LSL_Types.Quaternion();
            Type = 0;
            Velocity = new LSL_Types.Vector3();
            initializeSurfaceTouch();
        }

        public UUID Key;
        public LSL_Types.Vector3 OffsetPos;
        public int LinkNum;
        public UUID Group;
        public string Name;
        public UUID Owner;
        public LSL_Types.Vector3 Position;
        public LSL_Types.Quaternion Rotation;
        public int Type;
        public LSL_Types.Vector3 Velocity;

        private LSL_Types.Vector3 touchST;
        public LSL_Types.Vector3 TouchST { get { return touchST; } }

        private LSL_Types.Vector3 touchNormal;
        public LSL_Types.Vector3 TouchNormal { get { return touchNormal; } }

        private LSL_Types.Vector3 touchBinormal;
        public LSL_Types.Vector3 TouchBinormal { get { return touchBinormal; } }

        private LSL_Types.Vector3 touchPos;
        public LSL_Types.Vector3 TouchPos { get { return touchPos; } }

        private LSL_Types.Vector3 touchUV;
        public LSL_Types.Vector3 TouchUV { get { return touchUV; } }

        private int touchFace;
        public int TouchFace { get { return touchFace; } }

        // This can be done in two places including the constructor
        // so be carefull what gets added here
        private void initializeSurfaceTouch()
        {
            touchST = new LSL_Types.Vector3(-1.0, -1.0, 0.0);
            touchNormal = new LSL_Types.Vector3();
            touchBinormal = new LSL_Types.Vector3();
            touchPos = new LSL_Types.Vector3();
            touchUV = new LSL_Types.Vector3(-1.0, -1.0, 0.0);
            touchFace = -1;
        }

        /*
         * Set up the surface touch detected values
         */
        public SurfaceTouchEventArgs SurfaceTouchArgs
        {
            set
            {
                if (value == null)
                {
                    // Initialise to defaults if no value
                    initializeSurfaceTouch();
                }
                else
                {
                    // Set the values from the touch data provided by the client
                    touchST = new LSL_Types.Vector3(value.STCoord.X, value.STCoord.Y, value.STCoord.Z);
                    touchUV = new LSL_Types.Vector3(value.UVCoord.X, value.UVCoord.Y, value.UVCoord.Z);
                    touchNormal = new LSL_Types.Vector3(value.Normal.X, value.Normal.Y, value.Normal.Z);
                    touchBinormal = new LSL_Types.Vector3(value.Binormal.X, value.Binormal.Y, value.Binormal.Z);
                    touchPos = new LSL_Types.Vector3(value.Position.X, value.Position.Y, value.Position.Z);
                    touchFace = value.FaceIndex;
                }
            }
        }

        public void Populate(Scene scene)
        {
            SceneObjectPart part = scene.GetSceneObjectPart(Key);
            if (part == null) // Avatar, maybe?
            {
                ScenePresence presence = scene.GetScenePresence(Key);
                if (presence == null)
                    return;

                Name = presence.Firstname + " " + presence.Lastname;
                Owner = Key;
                Position = new LSL_Types.Vector3(
                        presence.AbsolutePosition.X,
                        presence.AbsolutePosition.Y,
                        presence.AbsolutePosition.Z);
                Rotation = new LSL_Types.Quaternion(
                        presence.Rotation.X,
                        presence.Rotation.Y,
                        presence.Rotation.Z,
                        presence.Rotation.W);
                Velocity = new LSL_Types.Vector3(
                        presence.Velocity.X,
                        presence.Velocity.Y,
                        presence.Velocity.Z);

                Type = 0x01; // Avatar
                if (presence.Velocity != Vector3.Zero)
                    Type |= 0x02; // Active

                Group = presence.ControllingClient.ActiveGroupId;

                return;
            }

            part=part.ParentGroup.RootPart; // We detect objects only

            LinkNum = 0; // Not relevant

            Group = part.GroupID;
            Name = part.Name;
            Owner = part.OwnerID;
            if (part.Velocity == Vector3.Zero)
                Type = 0x04; // Passive
            else
                Type = 0x02; // Passive

            foreach (SceneObjectPart p in part.ParentGroup.Parts)
            {
                if (p.Inventory.ContainsScripts())
                {
                    Type |= 0x08; // Scripted
                    break;
                }
            }

            Position = new LSL_Types.Vector3(part.AbsolutePosition.X,
                                             part.AbsolutePosition.Y,
                                             part.AbsolutePosition.Z);

            Quaternion wr = part.ParentGroup.GroupRotation;
            Rotation = new LSL_Types.Quaternion(wr.X, wr.Y, wr.Z, wr.W);

            Velocity = new LSL_Types.Vector3(part.Velocity.X,
                                             part.Velocity.Y,
                                             part.Velocity.Z);
        }
    }

    /// <summary>
    /// Holds all the data required to execute a scripting event.
    /// </summary>
    public class EventParams
    {
        public EventParams(string eventName, Object[] eventParams, DetectParams[] detectParams)
        {
            EventName = eventName;
            Params = eventParams;
            DetectParams = detectParams;
        }

        public string EventName;
        public Object[] Params;
        public DetectParams[] DetectParams;
    }
}
