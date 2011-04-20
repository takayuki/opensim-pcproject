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
using System.Collections;
using System.Collections.Generic;
using OpenMetaverse;
using OpenSim.Framework;
using OpenSim.Data;

namespace OpenSim.Data.Null
{
    public class NullAvatarData : IAvatarData
    {
        private static Dictionary<UUID, AvatarBaseData> m_DataByUUID = new Dictionary<UUID, AvatarBaseData>();

        public NullAvatarData(string connectionString, string realm)
        {
        }

        public AvatarBaseData[] Get(string field, string val)
        {
            if (field == "PrincipalID")
            {
                UUID id = UUID.Zero;
                if (UUID.TryParse(val, out id))
                    if (m_DataByUUID.ContainsKey(id))
                        return new AvatarBaseData[] { m_DataByUUID[id] };
            }

            // Fail
            return new AvatarBaseData[0];
        }

        public bool Store(AvatarBaseData data)
        {
            m_DataByUUID[data.PrincipalID] = data;
            return true;
        }

        public bool Delete(UUID principalID, string name)
        {
            if (m_DataByUUID.ContainsKey(principalID) && m_DataByUUID[principalID].Data.ContainsKey(name))
            {
                    m_DataByUUID[principalID].Data.Remove(name);
                    return true;
            }

            return false;
        }

        public bool Delete(string field, string val)
        {
            if (field == "PrincipalID")
            {
                UUID id = UUID.Zero;
                if (UUID.TryParse(val, out id))
                    if (m_DataByUUID.ContainsKey(id))
                    {
                        m_DataByUUID.Remove(id);
                        return true;
                    }
            }

            return false;
        }

    }
}
