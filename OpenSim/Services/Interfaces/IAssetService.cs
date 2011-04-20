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
using OpenSim.Framework;

namespace OpenSim.Services.Interfaces
{
    public delegate void AssetRetrieved(string id, Object sender, AssetBase asset);

    public interface IAssetService
    {
        /// <summary>
        /// Get an asset synchronously.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        AssetBase Get(string id);

        /// <summary>
        /// Get an asset's metadata
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        AssetMetadata GetMetadata(string id);
        
        /// <summary>
        /// Get an asset's data, ignoring the metadata.
        /// </summary>
        /// <param name="id"></param>
        /// <returns>null if there is no such asset</returns>
        byte[] GetData(string id);

        /// <summary>
        /// Synchronously fetches an asset from the local cache only
        /// </summary>
        /// <param name="id">Asset ID</param>
        /// <returns>The fetched asset, or null if it did not exist in the local cache</returns>
        AssetBase GetCached(string id);

        /// <summary>
        /// Get an asset synchronously or asynchronously (depending on whether 
        /// it is locally cached) and fire a callback with the fetched asset
        /// </summary>
        /// <param name="id">The asset id</param>
        /// <param name="sender">Represents the requester.  Passed back via the handler</param>
        /// <param name="handler">The handler to call back once the asset has been retrieved</param>
        /// <returns>True if the id was parseable, false otherwise</returns>
        bool Get(string id, Object sender, AssetRetrieved handler);

        /// <summary>
        /// Creates a new asset
        /// </summary>
        /// Returns a random ID if none is passed into it
        /// <param name="asset"></param>
        /// <returns></returns>
        string Store(AssetBase asset);

        /// <summary>
        /// Update an asset's content
        /// </summary>
        /// Attachments and bare scripts need this!!
        /// <param name="id"> </param>
        /// <param name="data"></param>
        /// <returns></returns>
        bool UpdateContent(string id, byte[] data);

        /// <summary>
        /// Delete an asset
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        bool Delete(string id);
    }
}
