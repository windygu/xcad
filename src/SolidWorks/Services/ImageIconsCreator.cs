﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using Xarial.XCad.SolidWorks.Base;
using Xarial.XCad.SolidWorks.Exceptions;
using Xarial.XCad.UI;

namespace Xarial.XCad.SolidWorks.Services
{
    public class ImageIconsCreator : IIconsCreator
    {
        /// <summary>
        /// Icon data
        /// </summary>
        private class IconData
        {
            /// <summary>
            /// Source image in original format (not scaled, not modified)
            /// </summary>
            internal Image SourceIcon { get; }

            /// <summary>
            /// Path where the icon needs to be saved
            /// </summary>
            internal string TargetIconPath { get; }

            /// <summary>
            /// Required target size for the image
            /// </summary>
            internal Size TargetSize { get; }

            internal int Offset { get; }

            internal IconData(string iconsDir, Image sourceIcon, Size targetSize, int offset, string name)
            {
                SourceIcon = sourceIcon;
                TargetSize = targetSize;
                Offset = offset;
                TargetIconPath = Path.Combine(iconsDir, name);
            }
        }

        private readonly string m_IconsDir;

        public bool KeepIcons { get; set; }

        public ImageIconsCreator()
            : this(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString()))
        {
        }

        /// <param name="iconsDir">Directory to store the icons</param>
        /// <param name="disposeIcons">True to remove the icons when class is disposed</param>
        public ImageIconsCreator(string iconsDir)
        {
            m_IconsDir = iconsDir;
            KeepIcons = false;

            if (!Directory.Exists(m_IconsDir))
            {
                Directory.CreateDirectory(m_IconsDir);
            }
        }

        /// <summary>
        /// Disposing temp icon files
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
        }

        /// <summary>
        /// Replaces the pixels in the image based on the custom replacer handler
        /// </summary>
        /// <param name="icon">Image to replace</param>
        /// <param name="mask">Handler to replace which is called for each pixel</param>
        /// <returns>Resulting image</returns>
        private Image ReplaceColor(Image icon, ColorMaskDelegate mask)
        {
            var maskImg = new Bitmap(icon);

            var rect = new Rectangle(0, 0, maskImg.Width, maskImg.Height);

            var bmpData = maskImg.LockBits(rect, ImageLockMode.ReadWrite,
                PixelFormat.Format32bppArgb);

            var ptr = bmpData.Scan0;

            var rgba = new byte[Math.Abs(bmpData.Stride) * maskImg.Height];

            Marshal.Copy(ptr, rgba, 0, rgba.Length);

            for (int i = 0; i < rgba.Length; i += 4)
            {
                mask.Invoke(ref rgba[i + 2], ref rgba[i + 1], ref rgba[i], ref rgba[i + 3]);
            }

            Marshal.Copy(rgba, 0, bmpData.Scan0, rgba.Length);

            maskImg.UnlockBits(bmpData);

            return maskImg;
        }

        public string[] ConvertIcon(IIcon icon)
        {
            var iconsData = CreateIconData(icon);

            foreach (var iconData in iconsData)
            {
                CreateBitmap(new Image[] { iconData.SourceIcon },
                    iconData.TargetIconPath,
                    iconData.TargetSize, iconData.Offset, icon.TransparencyKey);
            }

            return iconsData.Select(i => i.TargetIconPath).ToArray();
        }

        /// <inheritdoc/>
        public string[] ConvertIconsGroup(IIcon[] icons)
        {
            if (icons == null || !icons.Any())
            {
                throw new ArgumentNullException(nameof(icons));
            }

            IconData[,] iconsDataGroup = null;

            var transparencyKey = icons.First().TransparencyKey;

            for (int i = 0; i < icons.Length; i++)
            {
                if (icons[i].TransparencyKey != transparencyKey)
                {
                    throw new IconTransparencyMismatchException(i);
                }

                var data = CreateIconData(icons[i]);

                if (iconsDataGroup == null)
                {
                    iconsDataGroup = new IconData[data.Length, icons.Length];
                }

                for (int j = 0; j < data.Length; j++)
                {
                    iconsDataGroup[j, i] = data[j];
                }
            }

            var iconsPaths = new string[iconsDataGroup.GetLength(0)];

            for (int i = 0; i < iconsDataGroup.GetLength(0); i++)
            {
                var imgs = new Image[iconsDataGroup.GetLength(1)];
                for (int j = 0; j < iconsDataGroup.GetLength(1); j++)
                {
                    imgs[j] = iconsDataGroup[i, j].SourceIcon;
                }

                iconsPaths[i] = iconsDataGroup[i, 0].TargetIconPath;
                CreateBitmap(imgs, iconsPaths[i],
                    iconsDataGroup[i, 0].TargetSize, iconsDataGroup[i, 0].Offset, transparencyKey);
            }

            return iconsPaths;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!KeepIcons)
            {
                try
                {
                    Directory.Delete(m_IconsDir, true);
                }
                catch
                {
                }
            }
        }

        private void CreateBitmap(Image[] sourceIcons,
            string targetIcon, Size size, int offset, Color background)
        {
            var width = size.Width * sourceIcons.Length;
            var height = size.Height;

            using (var bmp = new Bitmap(width,
                height, PixelFormat.Format24bppRgb))
            {
                using (var graph = Graphics.FromImage(bmp))
                {
                    graph.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    graph.SmoothingMode = SmoothingMode.HighQuality;
                    graph.PixelOffsetMode = PixelOffsetMode.HighQuality;

                    using (var brush = new SolidBrush(background))
                    {
                        graph.FillRectangle(brush, 0, 0, bmp.Width, bmp.Height);
                    }

                    for (int i = 0; i < sourceIcons.Length; i++)
                    {
                        var sourceIcon = ReplaceColor(sourceIcons[i],
                            new ColorMaskDelegate((ref byte r, ref byte g, ref byte b, ref byte a) =>
                            {
                                if (r == background.R && g == background.G && b == background.B && a == background.A)
                                {
                                    b = (byte)((b == 0) ? 1 : (b - 1));
                                }
                            }));

                        if (bmp.HorizontalResolution != sourceIcon.HorizontalResolution
                            || bmp.VerticalResolution != sourceIcon.VerticalResolution)
                        {
                            bmp.SetResolution(
                                sourceIcon.HorizontalResolution,
                                sourceIcon.VerticalResolution);
                        }

                        var widthScale = (double)(size.Width - offset * 2) / (double)sourceIcon.Width;
                        var heightScale = (double)(size.Height - offset * 2) / (double)sourceIcon.Height;
                        var scale = Math.Min(widthScale, heightScale);

                        if (scale < 0)
                        {
                            throw new Exception("Target size of the icon cannot be calculated due to offset constraint");
                        }

                        var destX = (int)(size.Width - sourceIcon.Width * scale) / 2;
                        var destY = (int)(size.Height - sourceIcon.Height * scale) / 2;

                        int destWidth = (int)(sourceIcon.Width * scale);
                        int destHeight = (int)(sourceIcon.Height * scale);

                        destX += i * size.Width;

                        graph.DrawImage(sourceIcon,
                            new Rectangle(destX, destY, destWidth, destHeight),
                            new Rectangle(0, 0, sourceIcon.Width, sourceIcon.Height),
                            GraphicsUnit.Pixel);
                    }
                }

                var dir = Path.GetDirectoryName(targetIcon);

                if (!Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                bmp.Save(targetIcon, ImageFormat.Bmp);
            }
        }

        private IconData[] CreateIconData(IIcon icon)
        {
            if (icon == null)
            {
                throw new ArgumentNullException(nameof(icon));
            }

            var sizes = icon.GetIconSizes();

            if (sizes == null || !sizes.Any())
            {
                throw new NullReferenceException($"Specified icon '{icon.GetType().FullName}' doesn't provide any sizes");
            }

            var iconsData = sizes.Select(s =>
            {
                var src = FromXImage(s.SourceImage);

                if (s.Mask != null)
                {
                    src = ReplaceColor(src, s.Mask);
                }

                return new IconData(m_IconsDir, src, s.TargetSize, s.Offset, s.Name);
            }).ToArray();

            return iconsData;
        }

        private Image FromXImage(IXImage img)
        {
            using (var str = new MemoryStream(img.Buffer))
            {
                return Image.FromStream(str);
            }
        }
    }
}