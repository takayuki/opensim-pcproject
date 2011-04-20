﻿/*
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
 * THIS SOFTWARE IS PROVIDED BY THE DEVELOPERS ''AS IS'' AND ANY
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
using System.Data;
using OpenMetaverse;
using OpenSim.Framework;
using System.Data.SqlClient;
using System.Reflection;
using System.Text;
using log4net;

namespace OpenSim.Data.MSSQL
{
    public class MSSQLXInventoryData : IXInventoryData
    {
//        private static readonly ILog m_log = LogManager.GetLogger(
//                MethodBase.GetCurrentMethod().DeclaringType);

        private MSSQLGenericTableHandler<XInventoryFolder> m_Folders;
        private MSSQLItemHandler m_Items;

        public MSSQLXInventoryData(string conn, string realm)
        {
            m_Folders = new MSSQLGenericTableHandler<XInventoryFolder>(
                    conn, "inventoryfolders", "InventoryStore");
            m_Items = new MSSQLItemHandler(
                    conn, "inventoryitems", String.Empty);
        }

        public XInventoryFolder[] GetFolders(string[] fields, string[] vals)
        {
            return m_Folders.Get(fields, vals);
        }

        public XInventoryItem[] GetItems(string[] fields, string[] vals)
        {
            return m_Items.Get(fields, vals);
        }

        public bool StoreFolder(XInventoryFolder folder)
        {
            return m_Folders.Store(folder);
        }

        public bool StoreItem(XInventoryItem item)
        {
            return m_Items.Store(item);
        }

        public bool DeleteFolders(string field, string val)
        {
            return m_Folders.Delete(field, val);
        }

        public bool DeleteItems(string field, string val)
        {
            return m_Items.Delete(field, val);
        }

        public bool MoveItem(string id, string newParent)
        {
            return m_Items.MoveItem(id, newParent);
        }

        public XInventoryItem[] GetActiveGestures(UUID principalID)
        {
            return m_Items.GetActiveGestures(principalID);
        }

        public int GetAssetPermissions(UUID principalID, UUID assetID)
        {
            return m_Items.GetAssetPermissions(principalID, assetID);
        }
    }

    public class MSSQLItemHandler : MSSQLGenericTableHandler<XInventoryItem>
    {
        public MSSQLItemHandler(string c, string t, string m) :
            base(c, t, m)
        {
        }

        public bool MoveItem(string id, string newParent)
        {
            using (SqlConnection conn = new SqlConnection(m_ConnectionString))
            using (SqlCommand cmd = new SqlCommand())
            {

                cmd.CommandText = String.Format("update {0} set parentFolderID = @ParentFolderID where inventoryID = @InventoryID", m_Realm);
                cmd.Parameters.Add(m_database.CreateParameter("@ParentFolderID", newParent));
                cmd.Parameters.Add(m_database.CreateParameter("@InventoryID", id));
                cmd.Connection = conn;
                conn.Open();
                return cmd.ExecuteNonQuery() == 0 ? false : true;
            }
        }

        public XInventoryItem[] GetActiveGestures(UUID principalID)
        {
            using (SqlConnection conn = new SqlConnection(m_ConnectionString))
            using (SqlCommand cmd = new SqlCommand())
            {
                cmd.CommandText = String.Format("select * from inventoryitems where avatarId = @uuid and assetType = @type and flags = 1", m_Realm);

                cmd.Parameters.Add(m_database.CreateParameter("@uuid", principalID.ToString()));
                cmd.Parameters.Add(m_database.CreateParameter("@type", (int)AssetType.Gesture));
                cmd.Connection = conn;
                conn.Open();
                return DoQuery(cmd);
            }
        }

        public int GetAssetPermissions(UUID principalID, UUID assetID)
        {
            using (SqlConnection conn = new SqlConnection(m_ConnectionString))
            using (SqlCommand cmd = new SqlCommand())
            {
                cmd.CommandText = String.Format("select bit_or(inventoryCurrentPermissions) as inventoryCurrentPermissions from inventoryitems where avatarID = @PrincipalID and assetID = @AssetID group by assetID", m_Realm);
                cmd.Parameters.Add(m_database.CreateParameter("@PrincipalID", principalID.ToString()));
                cmd.Parameters.Add(m_database.CreateParameter("@AssetID", assetID.ToString()));
                cmd.Connection = conn;
                conn.Open();
                using (SqlDataReader reader = cmd.ExecuteReader())
                {

                    int perms = 0;

                    if (reader.Read())
                    {
                        perms = Convert.ToInt32(reader["inventoryCurrentPermissions"]);
                    }

                    return perms;
                }

            }
        }
    }
}
