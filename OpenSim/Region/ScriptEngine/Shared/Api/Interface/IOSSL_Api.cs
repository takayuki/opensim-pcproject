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

using System.Collections;
using OpenSim.Region.ScriptEngine.Interfaces;

using key = OpenSim.Region.ScriptEngine.Shared.LSL_Types.LSLString;
using rotation = OpenSim.Region.ScriptEngine.Shared.LSL_Types.Quaternion;
using vector = OpenSim.Region.ScriptEngine.Shared.LSL_Types.Vector3;
using LSL_List = OpenSim.Region.ScriptEngine.Shared.LSL_Types.list;
using LSL_String = OpenSim.Region.ScriptEngine.Shared.LSL_Types.LSLString;
using LSL_Integer = OpenSim.Region.ScriptEngine.Shared.LSL_Types.LSLInteger;
using LSL_Float = OpenSim.Region.ScriptEngine.Shared.LSL_Types.LSLFloat;

namespace OpenSim.Region.ScriptEngine.Shared.Api.Interfaces
{
    public enum ThreatLevel
    {
        None = 0,
        Nuisance = 1,
        VeryLow = 2,
        Low = 3,
        Moderate = 4,
        High = 5,
        VeryHigh = 6,
        Severe = 7
    };

    public interface IOSSL_Api
    {
        void CheckThreatLevel(ThreatLevel level, string function);

        //OpenSim functions
        string osSetDynamicTextureURL(string dynamicID, string contentType, string url, string extraParams, int timer);
        string osSetDynamicTextureURLBlend(string dynamicID, string contentType, string url, string extraParams,
                                           int timer, int alpha);
        string osSetDynamicTextureURLBlendFace(string dynamicID, string contentType, string url, string extraParams,
                                           bool blend, int disp, int timer, int alpha, int face);
        string osSetDynamicTextureData(string dynamicID, string contentType, string data, string extraParams, int timer);
        string osSetDynamicTextureDataBlend(string dynamicID, string contentType, string data, string extraParams,
                                            int timer, int alpha);
        string osSetDynamicTextureDataBlendFace(string dynamicID, string contentType, string data, string extraParams,
                                            bool blend, int disp, int timer, int alpha, int face);

        LSL_Float osTerrainGetHeight(int x, int y);
        LSL_Integer osTerrainSetHeight(int x, int y, double val);
        void osTerrainFlush();

        int osRegionRestart(double seconds);
        void osRegionNotice(string msg);
        bool osConsoleCommand(string Command);
        void osSetParcelMediaURL(string url);
        void osSetPrimFloatOnWater(int floatYN);
        void osSetParcelSIPAddress(string SIPAddress);

        // Avatar Info Commands
        string osGetAgentIP(string agent);
        LSL_List osGetAgents();

        // Teleport commands
        void osTeleportAgent(string agent, string regionName, LSL_Types.Vector3 position, LSL_Types.Vector3 lookat);
        void osTeleportAgent(string agent, uint regionX, uint regionY, LSL_Types.Vector3 position, LSL_Types.Vector3 lookat);
        void osTeleportAgent(string agent, LSL_Types.Vector3 position, LSL_Types.Vector3 lookat);

        // Animation commands
        void osAvatarPlayAnimation(string avatar, string animation);
        void osAvatarStopAnimation(string avatar, string animation);

        //texture draw functions
        string osMovePen(string drawList, int x, int y);
        string osDrawLine(string drawList, int startX, int startY, int endX, int endY);
        string osDrawLine(string drawList, int endX, int endY);
        string osDrawText(string drawList, string text);
        string osDrawEllipse(string drawList, int width, int height);
        string osDrawRectangle(string drawList, int width, int height);
        string osDrawFilledRectangle(string drawList, int width, int height);
        string osDrawPolygon(string drawList, LSL_List x, LSL_List y);
        string osDrawFilledPolygon(string drawList, LSL_List x, LSL_List y);
        string osSetFontName(string drawList, string fontName);
        string osSetFontSize(string drawList, int fontSize);
        string osSetPenSize(string drawList, int penSize);
        string osSetPenColour(string drawList, string colour);
        string osSetPenCap(string drawList, string direction, string type);
        string osDrawImage(string drawList, int width, int height, string imageUrl);
        vector osGetDrawStringSize(string contentType, string text, string fontName, int fontSize);
        void osSetStateEvents(int events);

        double osList2Double(LSL_Types.list src, int index);

        void osSetRegionWaterHeight(double height);
        void osSetRegionSunSettings(bool useEstateSun, bool sunFixed, double sunHour);
        void osSetEstateSunSettings(bool sunFixed, double sunHour);
        double osGetCurrentSunHour();
        double osSunGetParam(string param);
        void osSunSetParam(string param, double value);

        // Wind Module Functions
        string osWindActiveModelPluginName();
        void osWindParamSet(string plugin, string param, float value);
        float osWindParamGet(string plugin, string param);


        string osGetScriptEngineName();
        string osGetSimulatorVersion();
        Hashtable osParseJSON(string JSON);

        void osMessageObject(key objectUUID,string message);

        void osMakeNotecard(string notecardName, LSL_Types.list contents);

        string osGetNotecardLine(string name, int line);
        string osGetNotecard(string name);
        int osGetNumberOfNotecardLines(string name);

        string osAvatarName2Key(string firstname, string lastname);
        string osKey2Name(string id);

        // Grid Info Functions
        string osGetGridNick();
        string osGetGridName();
        string osGetGridLoginURI();

        LSL_String osFormatString(string str, LSL_List strings);
        LSL_List osMatchString(string src, string pattern, int start);

        // Information about data loaded into the region
        string osLoadedCreationDate();
        string osLoadedCreationTime();
        string osLoadedCreationID();

        LSL_List osGetLinkPrimitiveParams(int linknumber, LSL_List rules);


        key osNpcCreate(string user, string name, vector position, key cloneFrom);
        void osNpcMoveTo(key npc, vector position);
        void osNpcSay(key npc, string message);
        void osNpcRemove(key npc);

        key osGetMapTexture();
        key osGetRegionMapTexture(string regionName);
    }
}
