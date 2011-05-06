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
using System.Net;
using OpenMetaverse;
using OpenMetaverse.Packets;
using OpenMetaverse.StructuredData;
using OpenMetaverse.Messages.Linden;

namespace OpenSim.Region.ClientStack.Linden
{
    public class EventQueueHelper
    {
        private EventQueueHelper() {} // no construction possible, it's an utility class

        private static byte[] ulongToByteArray(ulong uLongValue)
        {
            // Reverse endianness of RegionHandle
            return new byte[]
            {
                (byte)((uLongValue >> 56) % 256),
                (byte)((uLongValue >> 48) % 256),
                (byte)((uLongValue >> 40) % 256),
                (byte)((uLongValue >> 32) % 256),
                (byte)((uLongValue >> 24) % 256),
                (byte)((uLongValue >> 16) % 256),
                (byte)((uLongValue >> 8) % 256),
                (byte)(uLongValue % 256)
            };
        }

//        private static byte[] uintToByteArray(uint uIntValue)
//        {
//            byte[] result = new byte[4];
//            Utils.UIntToBytesBig(uIntValue, result, 0);
//            return result;
//        }

        public static OSD BuildEvent(string eventName, OSD eventBody)
        {
            OSDMap llsdEvent = new OSDMap(2);
            llsdEvent.Add("message", new OSDString(eventName));
            llsdEvent.Add("body", eventBody);

            return llsdEvent;
        }

        public static OSD EnableSimulator(ulong handle, IPEndPoint endPoint)
        {
            OSDMap llsdSimInfo = new OSDMap(3);

            llsdSimInfo.Add("Handle", new OSDBinary(ulongToByteArray(handle)));
            llsdSimInfo.Add("IP", new OSDBinary(endPoint.Address.GetAddressBytes()));
            llsdSimInfo.Add("Port", new OSDInteger(endPoint.Port));

            OSDArray arr = new OSDArray(1);
            arr.Add(llsdSimInfo);

            OSDMap llsdBody = new OSDMap(1);
            llsdBody.Add("SimulatorInfo", arr);

            return BuildEvent("EnableSimulator", llsdBody);
        }

        public static OSD DisableSimulator(ulong handle)
        {
            //OSDMap llsdSimInfo = new OSDMap(1);

            //llsdSimInfo.Add("Handle", new OSDBinary(regionHandleToByteArray(handle)));

            //OSDArray arr = new OSDArray(1);
            //arr.Add(llsdSimInfo);

            OSDMap llsdBody = new OSDMap(0);
            //llsdBody.Add("SimulatorInfo", arr);

            return BuildEvent("DisableSimulator", llsdBody);
        }
        
        public static OSD CrossRegion(ulong handle, Vector3 pos, Vector3 lookAt,
                                      IPEndPoint newRegionExternalEndPoint,
                                      string capsURL, UUID agentID, UUID sessionID)
        {
            OSDArray lookAtArr = new OSDArray(3);
            lookAtArr.Add(OSD.FromReal(lookAt.X));
            lookAtArr.Add(OSD.FromReal(lookAt.Y));
            lookAtArr.Add(OSD.FromReal(lookAt.Z));

            OSDArray positionArr = new OSDArray(3);
            positionArr.Add(OSD.FromReal(pos.X));
            positionArr.Add(OSD.FromReal(pos.Y));
            positionArr.Add(OSD.FromReal(pos.Z));

            OSDMap infoMap = new OSDMap(2);
            infoMap.Add("LookAt", lookAtArr);
            infoMap.Add("Position", positionArr);

            OSDArray infoArr = new OSDArray(1);
            infoArr.Add(infoMap);

            OSDMap agentDataMap = new OSDMap(2);
            agentDataMap.Add("AgentID", OSD.FromUUID(agentID));
            agentDataMap.Add("SessionID",  OSD.FromUUID(sessionID));

            OSDArray agentDataArr = new OSDArray(1);
            agentDataArr.Add(agentDataMap);

            OSDMap regionDataMap = new OSDMap(4);
            regionDataMap.Add("RegionHandle", OSD.FromBinary(ulongToByteArray(handle)));
            regionDataMap.Add("SeedCapability", OSD.FromString(capsURL));
            regionDataMap.Add("SimIP", OSD.FromBinary(newRegionExternalEndPoint.Address.GetAddressBytes()));
            regionDataMap.Add("SimPort", OSD.FromInteger(newRegionExternalEndPoint.Port));

            OSDArray regionDataArr = new OSDArray(1);
            regionDataArr.Add(regionDataMap);

            OSDMap llsdBody = new OSDMap(3);
            llsdBody.Add("Info", infoArr);
            llsdBody.Add("AgentData", agentDataArr);
            llsdBody.Add("RegionData", regionDataArr);

            return BuildEvent("CrossedRegion", llsdBody);
        }

        public static OSD TeleportFinishEvent(
            ulong regionHandle, byte simAccess, IPEndPoint regionExternalEndPoint,
            uint locationID, uint flags, string capsURL, UUID agentID)
        {
            OSDMap info = new OSDMap();
            info.Add("AgentID", OSD.FromUUID(agentID));
            info.Add("LocationID", OSD.FromInteger(4)); // TODO what is this?
            info.Add("RegionHandle", OSD.FromBinary(ulongToByteArray(regionHandle)));
            info.Add("SeedCapability", OSD.FromString(capsURL));
            info.Add("SimAccess", OSD.FromInteger(simAccess));
            info.Add("SimIP", OSD.FromBinary(regionExternalEndPoint.Address.GetAddressBytes()));
            info.Add("SimPort", OSD.FromInteger(regionExternalEndPoint.Port));
            info.Add("TeleportFlags", OSD.FromULong(1L << 4)); // AgentManager.TeleportFlags.ViaLocation

            OSDArray infoArr = new OSDArray();
            infoArr.Add(info);

            OSDMap body = new OSDMap();
            body.Add("Info", infoArr);

            return BuildEvent("TeleportFinish", body);
        }

        public static OSD ScriptRunningReplyEvent(UUID objectID, UUID itemID, bool running, bool mono)
        {
            OSDMap script = new OSDMap();
            script.Add("ObjectID", OSD.FromUUID(objectID));
            script.Add("ItemID", OSD.FromUUID(itemID));
            script.Add("Running", OSD.FromBoolean(running));
            script.Add("Mono", OSD.FromBoolean(mono));
            
            OSDArray scriptArr = new OSDArray();
            scriptArr.Add(script);
            
            OSDMap body = new OSDMap();
            body.Add("Script", scriptArr);
            
            return BuildEvent("ScriptRunningReply", body);
        }

        public static OSD EstablishAgentCommunication(UUID agentID, string simIpAndPort, string seedcap)
        {
            OSDMap body = new OSDMap(3);
            body.Add("agent-id", new OSDUUID(agentID));
            body.Add("sim-ip-and-port", new OSDString(simIpAndPort));
            body.Add("seed-capability", new OSDString(seedcap));

            return BuildEvent("EstablishAgentCommunication", body);
        }

        public static OSD KeepAliveEvent()
        {
            return BuildEvent("FAKEEVENT", new OSDMap());
        }

        public static OSD AgentParams(UUID agentID, bool checkEstate, int godLevel, bool limitedToEstate)
        {
            OSDMap body = new OSDMap(4);

            body.Add("agent_id", new OSDUUID(agentID));
            body.Add("check_estate", new OSDInteger(checkEstate ? 1 : 0));
            body.Add("god_level", new OSDInteger(godLevel));
            body.Add("limited_to_estate", new OSDInteger(limitedToEstate ? 1 : 0));

            return body;
        }

        public static OSD InstantMessageParams(UUID fromAgent, string message, UUID toAgent,
            string fromName, byte dialog, uint timeStamp, bool offline, int parentEstateID,
            Vector3 position, uint ttl, UUID transactionID, bool fromGroup, byte[] binaryBucket)
        {
            OSDMap messageParams = new OSDMap(15);
            messageParams.Add("type", new OSDInteger((int)dialog));
            
            OSDArray positionArray = new OSDArray(3);
            positionArray.Add(OSD.FromReal(position.X));
            positionArray.Add(OSD.FromReal(position.Y));
            positionArray.Add(OSD.FromReal(position.Z));
            messageParams.Add("position", positionArray);

            messageParams.Add("region_id", new OSDUUID(UUID.Zero));
            messageParams.Add("to_id", new OSDUUID(toAgent));
            messageParams.Add("source", new OSDInteger(0));

            OSDMap data = new OSDMap(1);
            data.Add("binary_bucket", OSD.FromBinary(binaryBucket));
            messageParams.Add("data", data);
            messageParams.Add("message", new OSDString(message));
            messageParams.Add("id", new OSDUUID(transactionID));
            messageParams.Add("from_name", new OSDString(fromName));
            messageParams.Add("timestamp", new OSDInteger((int)timeStamp));
            messageParams.Add("offline", new OSDInteger(offline ? 1 : 0));
            messageParams.Add("parent_estate_id", new OSDInteger(parentEstateID));
            messageParams.Add("ttl", new OSDInteger((int)ttl));
            messageParams.Add("from_id", new OSDUUID(fromAgent));
            messageParams.Add("from_group", new OSDInteger(fromGroup ? 1 : 0));

            return messageParams;
        }

        public static OSD InstantMessage(UUID fromAgent, string message, UUID toAgent,
            string fromName, byte dialog, uint timeStamp, bool offline, int parentEstateID,
            Vector3 position, uint ttl, UUID transactionID, bool fromGroup, byte[] binaryBucket,
            bool checkEstate, int godLevel, bool limitedToEstate)
        {
            OSDMap im = new OSDMap(2);
            im.Add("message_params", InstantMessageParams(fromAgent, message, toAgent,
            fromName, dialog, timeStamp, offline, parentEstateID,
            position, ttl, transactionID, fromGroup, binaryBucket));

            im.Add("agent_params", AgentParams(fromAgent, checkEstate, godLevel, limitedToEstate));

            return im;
        }


        public static OSD ChatterboxInvitation(UUID sessionID, string sessionName,
            UUID fromAgent, string message, UUID toAgent, string fromName, byte dialog,
            uint timeStamp, bool offline, int parentEstateID, Vector3 position,
            uint ttl, UUID transactionID, bool fromGroup, byte[] binaryBucket)
        {
            OSDMap body = new OSDMap(5);
            body.Add("session_id", new OSDUUID(sessionID));
            body.Add("from_name", new OSDString(fromName));
            body.Add("session_name", new OSDString(sessionName));
            body.Add("from_id", new OSDUUID(fromAgent));

            body.Add("instantmessage", InstantMessage(fromAgent, message, toAgent,
            fromName, dialog, timeStamp, offline, parentEstateID, position,
            ttl, transactionID, fromGroup, binaryBucket, true, 0, true));

            OSDMap chatterboxInvitation = new OSDMap(2);
            chatterboxInvitation.Add("message", new OSDString("ChatterBoxInvitation"));
            chatterboxInvitation.Add("body", body);
            return chatterboxInvitation;
        }

        public static OSD ChatterBoxSessionAgentListUpdates(UUID sessionID,
            UUID agentID, bool canVoiceChat, bool isModerator, bool textMute)
        {
            OSDMap body = new OSDMap();
            OSDMap agentUpdates = new OSDMap();
            OSDMap infoDetail = new OSDMap();
            OSDMap mutes = new OSDMap();

            mutes.Add("text", OSD.FromBoolean(textMute));
            infoDetail.Add("can_voice_chat", OSD.FromBoolean(canVoiceChat));
            infoDetail.Add("is_moderator", OSD.FromBoolean(isModerator));
            infoDetail.Add("mutes", mutes);
            OSDMap info = new OSDMap();
            info.Add("info", infoDetail);
            agentUpdates.Add(agentID.ToString(), info);
            body.Add("agent_updates", agentUpdates);
            body.Add("session_id", OSD.FromUUID(sessionID));
            body.Add("updates", new OSD());

            OSDMap chatterBoxSessionAgentListUpdates = new OSDMap();
            chatterBoxSessionAgentListUpdates.Add("message", OSD.FromString("ChatterBoxSessionAgentListUpdates"));
            chatterBoxSessionAgentListUpdates.Add("body", body);

            return chatterBoxSessionAgentListUpdates;
        }

        public static OSD GroupMembership(AgentGroupDataUpdatePacket groupUpdatePacket)
        {
            OSDMap groupUpdate = new OSDMap();
            groupUpdate.Add("message", OSD.FromString("AgentGroupDataUpdate"));

            OSDMap body = new OSDMap();
            OSDArray agentData = new OSDArray();
            OSDMap agentDataMap = new OSDMap();
            agentDataMap.Add("AgentID", OSD.FromUUID(groupUpdatePacket.AgentData.AgentID));
            agentData.Add(agentDataMap);
            body.Add("AgentData", agentData);

            OSDArray groupData = new OSDArray();

            foreach (AgentGroupDataUpdatePacket.GroupDataBlock groupDataBlock in groupUpdatePacket.GroupData)
            {
                OSDMap groupDataMap = new OSDMap();
                groupDataMap.Add("ListInProfile", OSD.FromBoolean(false));
                groupDataMap.Add("GroupID", OSD.FromUUID(groupDataBlock.GroupID));
                groupDataMap.Add("GroupInsigniaID", OSD.FromUUID(groupDataBlock.GroupInsigniaID));
                groupDataMap.Add("Contribution", OSD.FromInteger(groupDataBlock.Contribution));
                groupDataMap.Add("GroupPowers", OSD.FromBinary(ulongToByteArray(groupDataBlock.GroupPowers)));
                groupDataMap.Add("GroupName", OSD.FromString(Utils.BytesToString(groupDataBlock.GroupName)));
                groupDataMap.Add("AcceptNotices", OSD.FromBoolean(groupDataBlock.AcceptNotices));

                groupData.Add(groupDataMap);

            }
            body.Add("GroupData", groupData);
            groupUpdate.Add("body", body);

            return groupUpdate;
        }
        
        public static OSD PlacesQuery(PlacesReplyPacket PlacesReply)
        {
            OSDMap placesReply = new OSDMap();
            placesReply.Add("message", OSD.FromString("PlacesReplyMessage"));

            OSDMap body = new OSDMap();
            OSDArray agentData = new OSDArray();
            OSDMap agentDataMap = new OSDMap();
            agentDataMap.Add("AgentID", OSD.FromUUID(PlacesReply.AgentData.AgentID));
            agentDataMap.Add("QueryID", OSD.FromUUID(PlacesReply.AgentData.QueryID));
            agentDataMap.Add("TransactionID", OSD.FromUUID(PlacesReply.TransactionData.TransactionID));
            agentData.Add(agentDataMap);
            body.Add("AgentData", agentData);

            OSDArray QueryData = new OSDArray();

            foreach (PlacesReplyPacket.QueryDataBlock groupDataBlock in PlacesReply.QueryData)
            {
                OSDMap QueryDataMap = new OSDMap();
                QueryDataMap.Add("ActualArea", OSD.FromInteger(groupDataBlock.ActualArea));
                QueryDataMap.Add("BillableArea", OSD.FromInteger(groupDataBlock.BillableArea));
                QueryDataMap.Add("Description", OSD.FromBinary(groupDataBlock.Desc));
                QueryDataMap.Add("Dwell", OSD.FromInteger((int)groupDataBlock.Dwell));
                QueryDataMap.Add("Flags", OSD.FromString(Convert.ToString(groupDataBlock.Flags)));
                QueryDataMap.Add("GlobalX", OSD.FromInteger((int)groupDataBlock.GlobalX));
                QueryDataMap.Add("GlobalY", OSD.FromInteger((int)groupDataBlock.GlobalY));
                QueryDataMap.Add("GlobalZ", OSD.FromInteger((int)groupDataBlock.GlobalZ));
                QueryDataMap.Add("Name", OSD.FromBinary(groupDataBlock.Name));
                QueryDataMap.Add("OwnerID", OSD.FromUUID(groupDataBlock.OwnerID));
                QueryDataMap.Add("SimName", OSD.FromBinary(groupDataBlock.SimName));
                QueryDataMap.Add("SnapShotID", OSD.FromUUID(groupDataBlock.SnapshotID));
                QueryDataMap.Add("ProductSku", OSD.FromInteger(0));
                QueryDataMap.Add("Price", OSD.FromInteger(groupDataBlock.Price));
                
                QueryData.Add(QueryDataMap);
            }
            body.Add("QueryData", QueryData);
            placesReply.Add("QueryData[]", body);

            return placesReply;
        }

        public static OSD ParcelProperties(ParcelPropertiesMessage parcelPropertiesMessage)
        {
            OSDMap message = new OSDMap();
            message.Add("message", OSD.FromString("ParcelProperties"));
            OSD message_body = parcelPropertiesMessage.Serialize();
            message.Add("body", message_body);
            return message;
        }

    }
}
