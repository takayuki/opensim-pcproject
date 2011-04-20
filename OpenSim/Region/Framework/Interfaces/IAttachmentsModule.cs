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
using OpenMetaverse;
using OpenMetaverse.Packets;
using OpenSim.Framework;
using OpenSim.Region.Framework.Scenes;

namespace OpenSim.Region.Framework.Interfaces
{
    public interface IAttachmentsModule
    {
        /// <summary>
        /// Attach an object to an avatar from the world.
        /// </summary>
        /// <param name="controllingClient"></param>
        /// <param name="localID"></param>
        /// <param name="attachPoint"></param>
        /// <param name="rot"></param>
        /// <param name="silent"></param>
        void AttachObject(
            IClientAPI remoteClient, uint objectLocalID, uint AttachmentPt, bool silent);

        /// <summary>
        /// Attach an object to an avatar.
        /// </summary>
        /// <param name="controllingClient"></param>
        /// <param name="localID"></param>
        /// <param name="attachPoint"></param>
        /// <param name="rot"></param>
        /// <param name="attachPos"></param>
        /// <param name="silent"></param>
        /// <returns>true if the object was successfully attached, false otherwise</returns>
        bool AttachObject(
            IClientAPI remoteClient, SceneObjectGroup grp, uint AttachmentPt, bool silent);

        /// <summary>
        /// Rez an attachment from user inventory and change inventory status to match.
        /// </summary>
        /// <param name="remoteClient"></param>
        /// <param name="itemID"></param>
        /// <param name="AttachmentPt"></param>
        /// <returns>The scene object that was attached.  Null if the scene object could not be found</returns>
        UUID RezSingleAttachmentFromInventory(IClientAPI remoteClient, UUID itemID, uint AttachmentPt);

        /// <summary>
        /// Rez an attachment from user inventory
        /// </summary>
        /// <param name="remoteClient"></param>
        /// <param name="itemID"></param>
        /// <param name="AttachmentPt"></param>
        /// <param name="updateinventoryStatus">
        /// If true, we also update the user's inventory to show that the attachment is set.  If false, we do not.
        /// False is required so that we don't attempt to update information when a user enters a scene with the
        /// attachment already correctly set up in inventory.
        /// <returns>The uuid of the scene object that was attached.  Null if the scene object could not be found</returns>
        UUID RezSingleAttachmentFromInventory(
            IClientAPI remoteClient, UUID itemID, uint AttachmentPt, bool updateInventoryStatus);

        /// <summary>
        /// Rez multiple attachments from a user's inventory
        /// </summary>
        /// <param name="remoteClient"></param>
        /// <param name="header"></param>
        /// <param name="objects"></param>
        void RezMultipleAttachmentsFromInventory(
            IClientAPI remoteClient,
            RezMultipleAttachmentsFromInvPacket.HeaderDataBlock header,
            RezMultipleAttachmentsFromInvPacket.ObjectDataBlock[] objects);

        /// <summary>
        /// Detach an object from the avatar.
        /// </summary>
        ///
        /// This method is called in response to a client's detach request, so we only update the information in
        /// inventory
        /// <param name="objectLocalID"></param>
        /// <param name="remoteClient"></param>
        void DetachObject(uint objectLocalID, IClientAPI remoteClient);
            
        /// <summary>
        /// Detach the given item to the ground.
        /// </summary>
        /// <param name="itemID"></param>
        /// <param name="remoteClient"></param>
        void DetachSingleAttachmentToGround(UUID itemID, IClientAPI remoteClient);

        /// <summary>
        /// Update the user inventory to show a detach.
        /// </summary>
        /// <param name="itemID">/param>
        /// <param name="remoteClient"></param>
        void ShowDetachInUserInventory(UUID itemID, IClientAPI remoteClient);
        
        /// <summary>
        /// Update the position of an attachment.
        /// </summary>
        /// <param name="sog"></param>
        /// <param name="pos"></param>
        void UpdateAttachmentPosition(SceneObjectGroup sog, Vector3 pos);
        
        /// <summary>
        /// Update the user inventory with a changed attachment
        /// </summary>
        /// <param name="remoteClient">
        /// A <see cref="IClientAPI"/>
        /// </param>
        /// <param name="grp">
        /// A <see cref="SceneObjectGroup"/>
        /// </param>
        /// <param name="itemID">
        /// A <see cref="UUID"/>
        /// </param>
        /// <param name="agentID">
        /// A <see cref="UUID"/>
        /// </param>
        void UpdateKnownItem(IClientAPI remoteClient, SceneObjectGroup grp, UUID itemID, UUID agentID);
    }
}
