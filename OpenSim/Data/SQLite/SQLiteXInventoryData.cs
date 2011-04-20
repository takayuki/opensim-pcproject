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
using System.Data;
using System.Reflection;
using System.Collections.Generic;
#if CSharpSqlite
    using Community.CsharpSqlite.Sqlite;
#else
    using Mono.Data.Sqlite;
#endif
using log4net;
using OpenMetaverse;
using OpenSim.Framework;

namespace OpenSim.Data.SQLite
{
    /// <summary>
    /// A MySQL Interface for the Asset Server
    /// </summary>
    public class SQLiteXInventoryData : IXInventoryData
    {
//        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private SQLiteGenericTableHandler<XInventoryFolder> m_Folders;
        private SqliteItemHandler m_Items;

        public SQLiteXInventoryData(string conn, string realm)
        {
            m_Folders = new SQLiteGenericTableHandler<XInventoryFolder>(
                    conn, "inventoryfolders", "XInventoryStore");
            m_Items = new SqliteItemHandler(
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
            if (folder.folderName.Length > 64)
                folder.folderName = folder.folderName.Substring(0, 64);

            return m_Folders.Store(folder);
        }

        public bool StoreItem(XInventoryItem item)
        {
            if (item.inventoryName.Length > 64)
                item.inventoryName = item.inventoryName.Substring(0, 64);
            if (item.inventoryDescription.Length > 128)
                item.inventoryDescription = item.inventoryDescription.Substring(0, 128);

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

    public class SqliteItemHandler : SQLiteGenericTableHandler<XInventoryItem>
    {
        public SqliteItemHandler(string c, string t, string m) :
                base(c, t, m)
        {
        }

        public bool MoveItem(string id, string newParent)
        {
            SqliteCommand cmd = new SqliteCommand();

            cmd.CommandText = String.Format("update {0} set parentFolderID = :ParentFolderID where inventoryID = :InventoryID", m_Realm);
            cmd.Parameters.Add(new SqliteParameter(":ParentFolderID", newParent));
            cmd.Parameters.Add(new SqliteParameter(":InventoryID", id));

            return ExecuteNonQuery(cmd, m_Connection) == 0 ? false : true;
        }

        public XInventoryItem[] GetActiveGestures(UUID principalID)
        {
            SqliteCommand cmd  = new SqliteCommand();
            cmd.CommandText = String.Format("select * from inventoryitems where avatarId = :uuid and assetType = :type and flags = 1", m_Realm);

            cmd.Parameters.Add(new SqliteParameter(":uuid", principalID.ToString()));
            cmd.Parameters.Add(new SqliteParameter(":type", (int)AssetType.Gesture));

            return DoQuery(cmd);
        }

        public int GetAssetPermissions(UUID principalID, UUID assetID)
        {
            SqliteCommand cmd = new SqliteCommand();

            cmd.CommandText = String.Format("select inventoryCurrentPermissions from inventoryitems where avatarID = :PrincipalID and assetID = :AssetID", m_Realm);
            cmd.Parameters.Add(new SqliteParameter(":PrincipalID", principalID.ToString()));
            cmd.Parameters.Add(new SqliteParameter(":AssetID", assetID.ToString()));

            IDataReader reader = ExecuteReader(cmd, m_Connection);

            int perms = 0;

            while (reader.Read())
            {
                perms |= Convert.ToInt32(reader["inventoryCurrentPermissions"]);
            }

            reader.Close();
            //CloseCommand(cmd);

            return perms;
        }
    }
}
