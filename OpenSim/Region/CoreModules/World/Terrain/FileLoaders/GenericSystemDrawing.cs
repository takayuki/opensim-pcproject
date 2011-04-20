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
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using OpenSim.Region.Framework.Interfaces;
using OpenSim.Region.Framework.Scenes;

namespace OpenSim.Region.CoreModules.World.Terrain.FileLoaders
{
    /// <summary>
    /// A virtual class designed to have methods overloaded,
    /// this class provides an interface for a generic image
    /// saving and loading mechanism, but does not specify the
    /// format. It should not be insubstantiated directly.
    /// </summary>
    public class GenericSystemDrawing : ITerrainLoader
    {
        #region ITerrainLoader Members

        public string FileExtension
        {
            get { return ".gsd"; }
        }

        /// <summary>
        /// Loads a file from a specified filename on the disk,
        /// parses the image using the System.Drawing parsers
        /// then returns a terrain channel. Values are
        /// returned based on HSL brightness between 0m and 128m
        /// </summary>
        /// <param name="filename">The target image to load</param>
        /// <returns>A terrain channel generated from the image.</returns>
        public virtual ITerrainChannel LoadFile(string filename)
        {
            return LoadBitmap(new Bitmap(filename));
        }

        public virtual ITerrainChannel LoadFile(string filename, int offsetX, int offsetY, int fileWidth, int fileHeight, int w, int h)
        {
            Bitmap bitmap = new Bitmap(filename);
            ITerrainChannel retval = new TerrainChannel(true);

            for (int x = 0; x < retval.Width; x++)
            {
                for (int y = 0; y < retval.Height; y++)
                {
                    retval[x, y] = bitmap.GetPixel(offsetX * retval.Width + x, (bitmap.Height - (retval.Height * (offsetY + 1))) + retval.Height - y - 1).GetBrightness() * 128;
                }
            }

            return retval;
        }

        public virtual ITerrainChannel LoadStream(Stream stream)
        {
            return LoadBitmap(new Bitmap(stream));
        }

        protected virtual ITerrainChannel LoadBitmap(Bitmap bitmap)
        {
            ITerrainChannel retval = new TerrainChannel(bitmap.Width, bitmap.Height);

            int x;
            for (x = 0; x < bitmap.Width; x++)
            {
                int y;
                for (y = 0; y < bitmap.Height; y++)
                {
                    retval[x, y] = bitmap.GetPixel(x, bitmap.Height - y - 1).GetBrightness() * 128;
                }
            }

            return retval;
        }

        /// <summary>
        /// Exports a file to a image on the disk using a System.Drawing exporter.
        /// </summary>
        /// <param name="filename">The target filename</param>
        /// <param name="map">The terrain channel being saved</param>
        public virtual void SaveFile(string filename, ITerrainChannel map)
        {
            Bitmap colours = CreateGrayscaleBitmapFromMap(map);

            colours.Save(filename, ImageFormat.Png);
        }

        /// <summary>
        /// Exports a stream using a System.Drawing exporter.
        /// </summary>
        /// <param name="stream">The target stream</param>
        /// <param name="map">The terrain channel being saved</param>
        public virtual void SaveStream(Stream stream, ITerrainChannel map)
        {
            Bitmap colours = CreateGrayscaleBitmapFromMap(map);

            colours.Save(stream, ImageFormat.Png);
        }

        public virtual void SaveFile(ITerrainChannel m_channel, string filename, 
                                     int offsetX, int offsetY,
                                     int fileWidth, int fileHeight,
                                     int regionSizeX, int regionSizeY)

        {
            // We need to do this because:
            // "Saving the image to the same file it was constructed from is not allowed and throws an exception."
            string tempName = offsetX + "_ " + offsetY + "_" + filename;

            Bitmap entireBitmap = null;
            Bitmap thisBitmap = null;
            if (File.Exists(filename))
            {
                File.Copy(filename, tempName);
                entireBitmap = new Bitmap(tempName);
                if (entireBitmap.Width != fileWidth * regionSizeX || entireBitmap.Height != fileHeight * regionSizeY)
                {
                    // old file, let's overwrite it
                    entireBitmap = new Bitmap(fileWidth * regionSizeX, fileHeight * regionSizeY);
                }
            }
            else
            {
                entireBitmap = new Bitmap(fileWidth * regionSizeX, fileHeight * regionSizeY);
            }

            thisBitmap = CreateGrayscaleBitmapFromMap(m_channel);
            Console.WriteLine("offsetX=" + offsetX + " offsetY=" + offsetY);
            for (int x = 0; x < regionSizeX; x++)
                for (int y = 0; y < regionSizeY; y++)
                    entireBitmap.SetPixel(x + offsetX * regionSizeX, y + (fileHeight - 1 - offsetY) * regionSizeY, thisBitmap.GetPixel(x, y));

            Save(entireBitmap, filename);
            thisBitmap.Dispose();
            entireBitmap.Dispose();

            if (File.Exists(tempName))
                File.Delete(tempName);
        }

        protected virtual void Save(Bitmap bmp, string filename)
        {
            bmp.Save(filename, ImageFormat.Png);
        }

        #endregion

        public override string ToString()
        {
            return "SYS.DRAWING";
        }

        /// <summary>
        /// Protected method, generates a grayscale bitmap
        /// image from a specified terrain channel.
        /// </summary>
        /// <param name="map">The terrain channel to export to bitmap</param>
        /// <returns>A System.Drawing.Bitmap containing a grayscale image</returns>
        protected static Bitmap CreateGrayscaleBitmapFromMap(ITerrainChannel map)
        {
            Bitmap bmp = new Bitmap(map.Width, map.Height);

            const int pallete = 256;

            Color[] grays = new Color[pallete];
            for (int i = 0; i < grays.Length; i++)
            {
                grays[i] = Color.FromArgb(i, i, i);
            }

            for (int y = 0; y < map.Height; y++)
            {
                for (int x = 0; x < map.Width; x++)
                {
                    // 512 is the largest possible height before colours clamp
                    int colorindex = (int) (Math.Max(Math.Min(1.0, map[x, y] / 128.0), 0.0) * (pallete - 1));

                    // Handle error conditions
                    if (colorindex > pallete - 1 || colorindex < 0)
                        bmp.SetPixel(x, map.Height - y - 1, Color.Red);
                    else
                        bmp.SetPixel(x, map.Height - y - 1, grays[colorindex]);
                }
            }
            return bmp;
        }

        /// <summary>
        /// Protected method, generates a coloured bitmap
        /// image from a specified terrain channel.
        /// </summary>
        /// <param name="map">The terrain channel to export to bitmap</param>
        /// <returns>A System.Drawing.Bitmap containing a coloured image</returns>
        protected static Bitmap CreateBitmapFromMap(ITerrainChannel map)
        {
            Bitmap gradientmapLd = new Bitmap("defaultstripe.png");

            int pallete = gradientmapLd.Height;

            Bitmap bmp = new Bitmap(map.Width, map.Height);
            Color[] colours = new Color[pallete];

            for (int i = 0; i < pallete; i++)
            {
                colours[i] = gradientmapLd.GetPixel(0, i);
            }

            for (int y = 0; y < map.Height; y++)
            {
                for (int x = 0; x < map.Width; x++)
                {
                    // 512 is the largest possible height before colours clamp
                    int colorindex = (int) (Math.Max(Math.Min(1.0, map[x, y] / 512.0), 0.0) * (pallete - 1));

                    // Handle error conditions
                    if (colorindex > pallete - 1 || colorindex < 0)
                        bmp.SetPixel(x, map.Height - y - 1, Color.Red);
                    else
                        bmp.SetPixel(x, map.Height - y - 1, colours[colorindex]);
                }
            }
            return bmp;
        }
    }
}
