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
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading;
using NUnit.Framework;
using OpenMetaverse;
using OpenSim.Data;
using OpenSim.Framework;
using OpenSim.Framework.Serialization;
using OpenSim.Framework.Serialization.External;
using OpenSim.Framework.Communications;
using OpenSim.Region.CoreModules.Avatar.Inventory.Archiver;
using OpenSim.Region.CoreModules.World.Serialiser;
using OpenSim.Region.Framework.Scenes;
using OpenSim.Region.Framework.Scenes.Serialization;
using OpenSim.Services.Interfaces;
using OpenSim.Tests.Common;
using OpenSim.Tests.Common.Mock;

namespace OpenSim.Region.CoreModules.Avatar.Inventory.Archiver.Tests
{
    [TestFixture]
    public class PathTests : InventoryArchiveTestCase
    {
        /// <summary>
        /// Test saving an inventory path to a V0.1 OpenSim Inventory Archive 
        /// (subject to change since there is no fixed format yet).
        /// </summary>
        [Test]
        public void TestSavePathToIarV0_1()
        {
            TestHelper.InMethod();
//            log4net.Config.XmlConfigurator.Configure();

            InventoryArchiverModule archiverModule = new InventoryArchiverModule();

            Scene scene = SceneSetupHelpers.SetupScene();
            SceneSetupHelpers.SetupSceneModules(scene, archiverModule);

            // Create user
            string userFirstName = "Jock";
            string userLastName = "Stirrup";
            string userPassword = "troll";
            UUID userId = UUID.Parse("00000000-0000-0000-0000-000000000020");
            UserAccountHelpers.CreateUserWithInventory(scene, userFirstName, userLastName, userId, userPassword);
            
            // Create asset
            SceneObjectGroup object1;
            SceneObjectPart part1;
            {
                string partName = "My Little Dog Object";
                UUID ownerId = UUID.Parse("00000000-0000-0000-0000-000000000040");
                PrimitiveBaseShape shape = PrimitiveBaseShape.CreateSphere();
                Vector3 groupPosition = new Vector3(10, 20, 30);
                Quaternion rotationOffset = new Quaternion(20, 30, 40, 50);
                Vector3 offsetPosition = new Vector3(5, 10, 15);

                part1 = new SceneObjectPart(ownerId, shape, groupPosition, rotationOffset, offsetPosition);
                part1.Name = partName;

                object1 = new SceneObjectGroup(part1);
                scene.AddNewSceneObject(object1, false);
            }

            UUID asset1Id = UUID.Parse("00000000-0000-0000-0000-000000000060");
            AssetBase asset1 = AssetHelpers.CreateAsset(asset1Id, object1);
            scene.AssetService.Store(asset1);

            // Create item
            UUID item1Id = UUID.Parse("00000000-0000-0000-0000-000000000080");
            InventoryItemBase item1 = new InventoryItemBase();
            item1.Name = "My Little Dog";
            item1.AssetID = asset1.FullID;
            item1.ID = item1Id;
            InventoryFolderBase objsFolder 
                = InventoryArchiveUtils.FindFolderByPath(scene.InventoryService, userId, "Objects")[0];
            item1.Folder = objsFolder.ID;
            scene.AddInventoryItem(item1);

            MemoryStream archiveWriteStream = new MemoryStream();
            archiverModule.OnInventoryArchiveSaved += SaveCompleted;

            // Test saving a particular path
            mre.Reset();
            archiverModule.ArchiveInventory(
                Guid.NewGuid(), userFirstName, userLastName, "Objects", userPassword, archiveWriteStream);
            mre.WaitOne(60000, false);

            byte[] archive = archiveWriteStream.ToArray();
            MemoryStream archiveReadStream = new MemoryStream(archive);
            TarArchiveReader tar = new TarArchiveReader(archiveReadStream);

            //bool gotControlFile = false;
            bool gotObject1File = false;
            //bool gotObject2File = false;
            string expectedObject1FileName = InventoryArchiveWriteRequest.CreateArchiveItemName(item1);
            string expectedObject1FilePath = string.Format(
                "{0}{1}{2}",
                ArchiveConstants.INVENTORY_PATH,
                InventoryArchiveWriteRequest.CreateArchiveFolderName(objsFolder),
                expectedObject1FileName);

            string filePath;
            TarArchiveReader.TarEntryType tarEntryType;

//            Console.WriteLine("Reading archive");
            
            while (tar.ReadEntry(out filePath, out tarEntryType) != null)
            {
//                Console.WriteLine("Got {0}", filePath);

//                if (ArchiveConstants.CONTROL_FILE_PATH == filePath)
//                {
//                    gotControlFile = true;
//                }
                
                if (filePath.StartsWith(ArchiveConstants.INVENTORY_PATH) && filePath.EndsWith(".xml"))
                {
//                    string fileName = filePath.Remove(0, "Objects/".Length);
//
//                    if (fileName.StartsWith(part1.Name))
//                    {
                        Assert.That(expectedObject1FilePath, Is.EqualTo(filePath));
                        gotObject1File = true;
//                    }
//                    else if (fileName.StartsWith(part2.Name))
//                    {
//                        Assert.That(fileName, Is.EqualTo(expectedObject2FileName));
//                        gotObject2File = true;
//                    }
                }
            }

//            Assert.That(gotControlFile, Is.True, "No control file in archive");
            Assert.That(gotObject1File, Is.True, "No item1 file in archive");
//            Assert.That(gotObject2File, Is.True, "No object2 file in archive");

            // TODO: Test presence of more files and contents of files.
        }
        
        /// <summary>
        /// Test loading an IAR to various different inventory paths.
        /// </summary>
        [Test]
        public void TestLoadIarToInventoryPaths()
        {
            TestHelper.InMethod();
//            log4net.Config.XmlConfigurator.Configure();
            
            SerialiserModule serialiserModule = new SerialiserModule();
            InventoryArchiverModule archiverModule = new InventoryArchiverModule();
            
            // Annoyingly, we have to set up a scene even though inventory loading has nothing to do with a scene
            Scene scene = SceneSetupHelpers.SetupScene();
            
            SceneSetupHelpers.SetupSceneModules(scene, serialiserModule, archiverModule);

            UserAccountHelpers.CreateUserWithInventory(scene, m_uaMT, "meowfood");
            UserAccountHelpers.CreateUserWithInventory(scene, m_uaLL1, "hampshire");
            
            archiverModule.DearchiveInventory(m_uaMT.FirstName, m_uaMT.LastName, "/", "meowfood", m_iarStream);            
            InventoryItemBase foundItem1
                = InventoryArchiveUtils.FindItemByPath(scene.InventoryService, m_uaMT.PrincipalID, m_item1Name);
            
            Assert.That(foundItem1, Is.Not.Null, "Didn't find loaded item 1");            

            // Now try loading to a root child folder
            UserInventoryHelpers.CreateInventoryFolder(scene.InventoryService, m_uaMT.PrincipalID, "xA");
            MemoryStream archiveReadStream = new MemoryStream(m_iarStream.ToArray());
            archiverModule.DearchiveInventory(m_uaMT.FirstName, m_uaMT.LastName, "xA", "meowfood", archiveReadStream);

            InventoryItemBase foundItem2
                = InventoryArchiveUtils.FindItemByPath(scene.InventoryService, m_uaMT.PrincipalID, "xA/" + m_item1Name);
            Assert.That(foundItem2, Is.Not.Null, "Didn't find loaded item 2");

            // Now try loading to a more deeply nested folder
            UserInventoryHelpers.CreateInventoryFolder(scene.InventoryService, m_uaMT.PrincipalID, "xB/xC");
            archiveReadStream = new MemoryStream(archiveReadStream.ToArray());
            archiverModule.DearchiveInventory(m_uaMT.FirstName, m_uaMT.LastName, "xB/xC", "meowfood", archiveReadStream);

            InventoryItemBase foundItem3
                = InventoryArchiveUtils.FindItemByPath(scene.InventoryService, m_uaMT.PrincipalID, "xB/xC/" + m_item1Name);
            Assert.That(foundItem3, Is.Not.Null, "Didn't find loaded item 3");            
        }
        
        /// <summary>
        /// Test that things work when the load path specified starts with a slash
        /// </summary>
        [Test]
        public void TestLoadIarPathStartsWithSlash()
        {
            TestHelper.InMethod();
//            log4net.Config.XmlConfigurator.Configure();
            
            SerialiserModule serialiserModule = new SerialiserModule();
            InventoryArchiverModule archiverModule = new InventoryArchiverModule();
            Scene scene = SceneSetupHelpers.SetupScene();
            SceneSetupHelpers.SetupSceneModules(scene, serialiserModule, archiverModule);
            
            UserAccountHelpers.CreateUserWithInventory(scene, m_uaMT, "password");
            archiverModule.DearchiveInventory(m_uaMT.FirstName, m_uaMT.LastName, "/Objects", "password", m_iarStream);

            InventoryItemBase foundItem1
                = InventoryArchiveUtils.FindItemByPath(
                    scene.InventoryService, m_uaMT.PrincipalID, "/Objects/" + m_item1Name);
            
            Assert.That(foundItem1, Is.Not.Null, "Didn't find loaded item 1 in TestLoadIarFolderStartsWithSlash()");
        }
 
        [Test]
        public void TestLoadIarPathWithEscapedChars()
        {
            TestHelper.InMethod();
//            log4net.Config.XmlConfigurator.Configure();

            string itemName = "You & you are a mean/man/";
            string humanEscapedItemName = @"You & you are a mean\/man\/";
            string userPassword = "meowfood";

            InventoryArchiverModule archiverModule = new InventoryArchiverModule();

            Scene scene = SceneSetupHelpers.SetupScene();
            SceneSetupHelpers.SetupSceneModules(scene, archiverModule);

            // Create user
            string userFirstName = "Jock";
            string userLastName = "Stirrup";
            UUID userId = UUID.Parse("00000000-0000-0000-0000-000000000020");
            UserAccountHelpers.CreateUserWithInventory(scene, userFirstName, userLastName, userId, "meowfood");
            
            // Create asset
            SceneObjectGroup object1;
            SceneObjectPart part1;
            {
                string partName = "part name";
                UUID ownerId = UUID.Parse("00000000-0000-0000-0000-000000000040");
                PrimitiveBaseShape shape = PrimitiveBaseShape.CreateSphere();
                Vector3 groupPosition = new Vector3(10, 20, 30);
                Quaternion rotationOffset = new Quaternion(20, 30, 40, 50);
                Vector3 offsetPosition = new Vector3(5, 10, 15);

                part1
                    = new SceneObjectPart(
                        ownerId, shape, groupPosition, rotationOffset, offsetPosition);
                part1.Name = partName;

                object1 = new SceneObjectGroup(part1);
                scene.AddNewSceneObject(object1, false);
            }

            UUID asset1Id = UUID.Parse("00000000-0000-0000-0000-000000000060");
            AssetBase asset1 = AssetHelpers.CreateAsset(asset1Id, object1);
            scene.AssetService.Store(asset1);

            // Create item
            UUID item1Id = UUID.Parse("00000000-0000-0000-0000-000000000080");
            InventoryItemBase item1 = new InventoryItemBase();
            item1.Name = itemName;
            item1.AssetID = asset1.FullID;
            item1.ID = item1Id;
            InventoryFolderBase objsFolder 
                = InventoryArchiveUtils.FindFolderByPath(scene.InventoryService, userId, "Objects")[0];
            item1.Folder = objsFolder.ID;
            scene.AddInventoryItem(item1);

            MemoryStream archiveWriteStream = new MemoryStream();
            archiverModule.OnInventoryArchiveSaved += SaveCompleted;

            mre.Reset();
            archiverModule.ArchiveInventory(
                Guid.NewGuid(), userFirstName, userLastName, "Objects", userPassword, archiveWriteStream);
            mre.WaitOne(60000, false);

            // LOAD ITEM
            MemoryStream archiveReadStream = new MemoryStream(archiveWriteStream.ToArray());
            
            archiverModule.DearchiveInventory(userFirstName, userLastName, "Scripts", userPassword, archiveReadStream);

            InventoryItemBase foundItem1
                = InventoryArchiveUtils.FindItemByPath(
                    scene.InventoryService, userId, "Scripts/Objects/" + humanEscapedItemName);
            
            Assert.That(foundItem1, Is.Not.Null, "Didn't find loaded item 1");
//            Assert.That(
//                foundItem1.CreatorId, Is.EqualTo(userUuid), 
//                "Loaded item non-uuid creator doesn't match that of the loading user");
            Assert.That(
                foundItem1.Name, Is.EqualTo(itemName), 
                "Loaded item name doesn't match saved name");
        }
        
        /// <summary>
        /// Test replication of an archive path to the user's inventory.
        /// </summary>
        [Test]
        public void TestNewIarPath()
        {
            TestHelper.InMethod();
//            log4net.Config.XmlConfigurator.Configure();
            
            Scene scene = SceneSetupHelpers.SetupScene();
            UserAccount ua1 = UserAccountHelpers.CreateUserWithInventory(scene);
            
            Dictionary <string, InventoryFolderBase> foldersCreated = new Dictionary<string, InventoryFolderBase>();
            HashSet<InventoryNodeBase> nodesLoaded = new HashSet<InventoryNodeBase>();
            
            string folder1Name = "1";
            string folder2aName = "2a";
            string folder2bName = "2b";
            
            string folder1ArchiveName = InventoryArchiveWriteRequest.CreateArchiveFolderName(folder1Name, UUID.Random());
            string folder2aArchiveName = InventoryArchiveWriteRequest.CreateArchiveFolderName(folder2aName, UUID.Random());
            string folder2bArchiveName = InventoryArchiveWriteRequest.CreateArchiveFolderName(folder2bName, UUID.Random());
            
            string iarPath1 = string.Join("", new string[] { folder1ArchiveName, folder2aArchiveName });
            string iarPath2 = string.Join("", new string[] { folder1ArchiveName, folder2bArchiveName });

            {
                // Test replication of path1
                new InventoryArchiveReadRequest(scene, ua1, null, (Stream)null, false)
                    .ReplicateArchivePathToUserInventory(
                        iarPath1, scene.InventoryService.GetRootFolder(ua1.PrincipalID), 
                        foldersCreated, nodesLoaded);
    
                List<InventoryFolderBase> folder1Candidates
                    = InventoryArchiveUtils.FindFolderByPath(scene.InventoryService, ua1.PrincipalID, folder1Name);
                Assert.That(folder1Candidates.Count, Is.EqualTo(1));
                
                InventoryFolderBase folder1 = folder1Candidates[0];
                List<InventoryFolderBase> folder2aCandidates 
                    = InventoryArchiveUtils.FindFolderByPath(scene.InventoryService, folder1, folder2aName);
                Assert.That(folder2aCandidates.Count, Is.EqualTo(1));
            }
            
            {
                // Test replication of path2
                new InventoryArchiveReadRequest(scene, ua1, null, (Stream)null, false)
                    .ReplicateArchivePathToUserInventory(
                        iarPath2, scene.InventoryService.GetRootFolder(ua1.PrincipalID), 
                        foldersCreated, nodesLoaded);
    
                List<InventoryFolderBase> folder1Candidates
                    = InventoryArchiveUtils.FindFolderByPath(scene.InventoryService, ua1.PrincipalID, folder1Name);
                Assert.That(folder1Candidates.Count, Is.EqualTo(1));
                
                InventoryFolderBase folder1 = folder1Candidates[0]; 
                
                List<InventoryFolderBase> folder2aCandidates 
                    = InventoryArchiveUtils.FindFolderByPath(scene.InventoryService, folder1, folder2aName);
                Assert.That(folder2aCandidates.Count, Is.EqualTo(1));
                
                List<InventoryFolderBase> folder2bCandidates 
                    = InventoryArchiveUtils.FindFolderByPath(scene.InventoryService, folder1, folder2bName);
                Assert.That(folder2bCandidates.Count, Is.EqualTo(1));
            }
        }
        
        /// <summary>
        /// Test replication of a partly existing archive path to the user's inventory.  This should create
        /// a duplicate path without the merge option.
        /// </summary>
        [Test]
        public void TestPartExistingIarPath()
        {
            TestHelper.InMethod();
            //log4net.Config.XmlConfigurator.Configure();
            
            Scene scene = SceneSetupHelpers.SetupScene();
            UserAccount ua1 = UserAccountHelpers.CreateUserWithInventory(scene);
            
            string folder1ExistingName = "a";
            string folder2Name = "b";
            
            InventoryFolderBase folder1 
                = UserInventoryHelpers.CreateInventoryFolder(
                    scene.InventoryService, ua1.PrincipalID, folder1ExistingName);
            
            string folder1ArchiveName = InventoryArchiveWriteRequest.CreateArchiveFolderName(folder1ExistingName, UUID.Random());
            string folder2ArchiveName = InventoryArchiveWriteRequest.CreateArchiveFolderName(folder2Name, UUID.Random());
            
            string itemArchivePath = string.Join("", new string[] { folder1ArchiveName, folder2ArchiveName });
            
            new InventoryArchiveReadRequest(scene, ua1, null, (Stream)null, false)
                .ReplicateArchivePathToUserInventory(
                    itemArchivePath, scene.InventoryService.GetRootFolder(ua1.PrincipalID), 
                    new Dictionary<string, InventoryFolderBase>(), new HashSet<InventoryNodeBase>());

            List<InventoryFolderBase> folder1PostCandidates 
                = InventoryArchiveUtils.FindFolderByPath(scene.InventoryService, ua1.PrincipalID, folder1ExistingName);
            Assert.That(folder1PostCandidates.Count, Is.EqualTo(2));
            
            // FIXME: Temporarily, we're going to do something messy to make sure we pick up the created folder.
            InventoryFolderBase folder1Post = null;
            foreach (InventoryFolderBase folder in folder1PostCandidates)
            {
                if (folder.ID != folder1.ID)
                {
                    folder1Post = folder;
                    break;
                }
            }
//            Assert.That(folder1Post.ID, Is.EqualTo(folder1.ID));

            List<InventoryFolderBase> folder2PostCandidates 
                = InventoryArchiveUtils.FindFolderByPath(scene.InventoryService, folder1Post, "b");
            Assert.That(folder2PostCandidates.Count, Is.EqualTo(1));
        }
        
        /// <summary>
        /// Test replication of a partly existing archive path to the user's inventory.  This should create
        /// a merged path.
        /// </summary>
        [Test]
        public void TestMergeIarPath()
        {
            TestHelper.InMethod();
//            log4net.Config.XmlConfigurator.Configure();
            
            Scene scene = SceneSetupHelpers.SetupScene();
            UserAccount ua1 = UserAccountHelpers.CreateUserWithInventory(scene);
            
            string folder1ExistingName = "a";
            string folder2Name = "b";
            
            InventoryFolderBase folder1 
                = UserInventoryHelpers.CreateInventoryFolder(
                    scene.InventoryService, ua1.PrincipalID, folder1ExistingName);
            
            string folder1ArchiveName = InventoryArchiveWriteRequest.CreateArchiveFolderName(folder1ExistingName, UUID.Random());
            string folder2ArchiveName = InventoryArchiveWriteRequest.CreateArchiveFolderName(folder2Name, UUID.Random());
            
            string itemArchivePath = string.Join("", new string[] { folder1ArchiveName, folder2ArchiveName });
            
            new InventoryArchiveReadRequest(scene, ua1, folder1ExistingName, (Stream)null, true)
                .ReplicateArchivePathToUserInventory(
                    itemArchivePath, scene.InventoryService.GetRootFolder(ua1.PrincipalID), 
                    new Dictionary<string, InventoryFolderBase>(), new HashSet<InventoryNodeBase>());

            List<InventoryFolderBase> folder1PostCandidates 
                = InventoryArchiveUtils.FindFolderByPath(scene.InventoryService, ua1.PrincipalID, folder1ExistingName);
            Assert.That(folder1PostCandidates.Count, Is.EqualTo(1));
            Assert.That(folder1PostCandidates[0].ID, Is.EqualTo(folder1.ID));

            List<InventoryFolderBase> folder2PostCandidates 
                = InventoryArchiveUtils.FindFolderByPath(scene.InventoryService, folder1PostCandidates[0], "b");
            Assert.That(folder2PostCandidates.Count, Is.EqualTo(1));
        }
    }
}