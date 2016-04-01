using System;
using System.Text.RegularExpressions;
using System.IO;
using System.Drawing;
using System.Collections.Generic;

using FreeImageAPI;

namespace Compiler
{
    class Base64Image : IDisposable
    {
        private FIBITMAP dib = new FIBITMAP();

        public Base64Image(string dataURI) : this(dataURI, FREE_IMAGE_LOAD_FLAGS.DEFAULT)
        {
        }

        public Base64Image(string dataURI, FREE_IMAGE_LOAD_FLAGS flags)
        {
            Match match = Regex.Match(dataURI, @"data:image/(?<type>.+?);base64,(?<data>.+)");
            if (match.Groups["data"] != null)
            {
                byte[] byteArray = Convert.FromBase64String(match.Groups["data"].Value);
                using (MemoryStream byteStream = new MemoryStream(byteArray))
                {
                    dib.SetNull();
                    FREE_IMAGE_FORMAT format = FREE_IMAGE_FORMAT.FIF_PNG;
                    if (match.Groups["type"] != null)
                    {
                        switch (match.Groups["type"].Value.ToLower())
                        {
                            case "bmp":
                                {
                                    format = FREE_IMAGE_FORMAT.FIF_BMP;
                                    break;
                                }
                            case "png":
                                {
                                    format = FREE_IMAGE_FORMAT.FIF_PNG;
                                    break;
                                }
                            case "jpeg":
                            case "jpg":
                                {
                                    format = FREE_IMAGE_FORMAT.FIF_JPEG;
                                    break;
                                }
                            case "tga":
                                {
                                    format = FREE_IMAGE_FORMAT.FIF_TARGA;
                                    break;
                                }
                        }
                    }
                    dib = FreeImage.LoadFromStream(byteStream, flags, ref format);
                }
            }
        }

        public void Dispose()
        {
            dib.SetNull();
        }

        ~Base64Image()
        {
            Dispose();
        }

        public void Transform(Schema.SkinFile.Attachment.Transform trnsfrm)
        {
            if (FreeImage.GetWidth(dib) <= 1 || FreeImage.GetHeight(dib) <= 1)
                return;
            FREE_IMAGE_FILTER filter = FREE_IMAGE_FILTER.FILTER_BSPLINE;

            if (trnsfrm.scaleFilter != null)
                switch (trnsfrm.scaleFilter.ToUpper())
                {
                    case "BOX":
                        filter = FREE_IMAGE_FILTER.FILTER_BOX;
                        break;
                    case "BICUBIC":
                        filter = FREE_IMAGE_FILTER.FILTER_BICUBIC;
                        break;
                    case "BILINEAR":
                        filter = FREE_IMAGE_FILTER.FILTER_BILINEAR;
                        break;
                    case "BSPLINE":
                        filter = FREE_IMAGE_FILTER.FILTER_BSPLINE;
                        break;
                    case "CATMULLROM":
                        filter = FREE_IMAGE_FILTER.FILTER_CATMULLROM;
                        break;
                    case "LANCZOS3":
                        filter = FREE_IMAGE_FILTER.FILTER_LANCZOS3;
                        break;
                }
            RectangleF originalDimensions, rotationDimensions;
            GraphicsUnit pageUnit = GraphicsUnit.Pixel;
            dib = FreeImage.Rescale(dib, (int)(FreeImage.GetWidth(dib) * trnsfrm.scaleX), (int)(FreeImage.GetHeight(dib) * trnsfrm.scaleY), filter);
            originalDimensions = FreeImage.GetBitmap(dib).GetBounds(ref pageUnit);

            RGBQUAD bgColor = new RGBQUAD();
            bgColor.rgbRed = 0x00;
            bgColor.rgbGreen = 0x00;
            bgColor.rgbBlue = 0x00;
            bgColor.rgbReserved = 0x00;
            // TODO: вычесть из положения разницу между оригинальными размерами и размерами после поворота (по крайней мере сверху)
            //int size = (int)(trnsfrm.x > 0 ? trnsfrm.x : 0) + (int)(trnsfrm.y > 0 ? trnsfrm.y : 0);
            //trnsfrm.angle = -45;
            /*
            double cos = Math.Cos(-trnsfrm.angle * Math.PI / 180), sin = Math.Sin(-trnsfrm.angle * Math.PI / 180);
            Point[] points = new Point[4] {
                new Point((int)(originalDimensions.Width / 2), (int)(originalDimensions.Height / 2)),  // top left //(int)(originalDimensions.Width / 2 * cos - originalDimensions.Height / 2 * sin), (int)(originalDimensions.Width / 2 * sin + originalDimensions.Height / 2 * cos)),
                new Point((int)(originalDimensions.Width / 2), (int)(-originalDimensions.Height / 2)), // top right
                new Point((int)(-originalDimensions.Width / 2), (int)(originalDimensions.Height / 2)), // bottom left
                new Point((int)(-originalDimensions.Width / 2), (int)(-originalDimensions.Height / 2)) // bottom right
            };
            for (int i = 0; i < points.Length; i++)
            {
                points[i].X = (int)(points[i].X * cos - points[i].Y * sin);
                points[i].Y = (int)(points[i].X * sin + points[i].Y * cos);
            }
            int maxRight = points[0].X, maxBottom = points[0].Y;
            for (int i = 0; i < points.Length; i++)
            {
                if (points[i].X > maxRight) maxRight = points[i].X;
                if (points[i].Y > maxBottom) maxBottom = points[i].Y;
            }
            
            Console.WriteLine(points[0]);
            Console.WriteLine(points[1]);
            Console.WriteLine(points[2]);
            Console.WriteLine(points[3]);
            
            Console.WriteLine(maxRight);
            Console.WriteLine(maxBottom);
            int rightOffset = (int)(maxRight - originalDimensions.Width / 2), bottomOffset = (int)(maxBottom - originalDimensions.Height / 2);
            Console.WriteLine(rightOffset);
            Console.WriteLine(bottomOffset);
            */
            /*
            Console.WriteLine(originalDimensions);
            Console.WriteLine(originalDimensions.Width / 2 * Math.Cos(trnsfrm.angle * Math.PI / 180) - originalDimensions.Height / 2 * Math.Sin(trnsfrm.angle * Math.PI / 180));
            Console.WriteLine(originalDimensions.Width / 2 * Math.Sin(trnsfrm.angle * Math.PI / 180) + originalDimensions.Height / 2 * Math.Cos(trnsfrm.angle * Math.PI / 180));
            */
            dib = FreeImage.Rotate<RGBQUAD>(dib, -trnsfrm.angle, bgColor);
            rotationDimensions = FreeImage.GetBitmap(dib).GetBounds(ref pageUnit);
            dib = FreeImage.EnlargeCanvas<RGBQUAD>(dib,
                0,
                0,
                (int)(trnsfrm.x > 0 ? trnsfrm.x : 0),// + rightOffset,//(int)(originalDimensions.Width / 2 * Math.Cos(trnsfrm.angle * Math.PI / 180) - originalDimensions.Height / 2 * Math.Sin(trnsfrm.angle * Math.PI / 180) - originalDimensions.Width / 2) + 2,
                (int)(trnsfrm.y > 0 ? trnsfrm.y : 0),// + bottomOffset,//(int)(originalDimensions.Width / 2 * Math.Sin(trnsfrm.angle * Math.PI / 180) + originalDimensions.Height / 2 * Math.Cos(trnsfrm.angle * Math.PI / 180)),
                bgColor,
                FREE_IMAGE_COLOR_OPTIONS.FICO_RGBA
            );
            //dib = FreeImage.RotateEx(dib, 0, trnsfrm.x, trnsfrm.y, 0, 0, true);
            //dib = FreeImage.Rotate<RGBQUAD>(dib, trnsfrm.angle, bgColor);

            //dib = FreeImage.RotateEx(dib, 0, trnsfrm.x, trnsfrm.y, originalDimensions.Width / 2, originalDimensions.Height / 2, true);
            dib = FreeImage.RotateEx(dib, 0, trnsfrm.x, trnsfrm.y, 0, 0, true);
            dib = FreeImage.EnlargeCanvas<RGBQUAD>(dib,
                (int)(rotationDimensions.Width - originalDimensions.Width) / -2,
                (int)(rotationDimensions.Height - originalDimensions.Height) / -2,
                0,
                0,
                bgColor,
                FREE_IMAGE_COLOR_OPTIONS.FICO_RGBA
            );
            //dib = FreeImage.RotateEx(dib, 0, trnsfrm.x - (rotationDimensions.Width - originalDimensions.Width) / 2, trnsfrm.y - (rotationDimensions.Height - originalDimensions.Height) / 2, 0, 0, true);
            //dib = FreeImage.RotateEx(dib, 0, trnsfrm.x, trnsfrm.y, 0, 0, true);
        }

        public void ApplyFilters(List<Schema.SkinFile.Attachment.Filter> filters)
        {
            foreach (Schema.SkinFile.Attachment.Filter filter in filters)
            {
                switch (filter.name)
                {
                    case "invert":
                        {
                            ///
                            /// TODO: Variable invertion using amount property
                            ///
                            FreeImage.AdjustColors(dib, 0, 0, 0, true);
                            break;
                        }
                    case "colorOverlay":
                        {
                            try
                            {
                                ///
                                /// TODO: Variable overlay using amount property
                                ///
                                RGBQUAD overlayColor = new RGBQUAD(), oldColor;
                                overlayColor.uintValue = Convert.ToUInt32(filter.color, 16);
                                //Console.WriteLine((double)filter.amount / 100);
                                for (uint y = 0; y < FreeImage.GetHeight(dib); y++)
                                {
                                    for (uint x = 0; x < FreeImage.GetWidth(dib); x++)
                                    {
                                        FreeImage.GetPixelColor(dib, x, y, out oldColor);
                                        overlayColor.rgbReserved = oldColor.rgbReserved;
                                        FreeImage.SetPixelColor(dib, x, y, ref overlayColor);
                                    }
                                }
                            }
                            catch { }
                            break;
                        }
                }
            }
        }

        public bool SaveSprite(string filename, int[] matrix)
        {
            FIBITMAP sprite = FreeImage.RotateEx(dib, 0, -matrix[0], -matrix[1], 0, 0, true);
            sprite = FreeImage.EnlargeCanvas<RGBQUAD>(sprite, 0, 0, -(int)FreeImage.GetWidth(dib) + matrix[2], -(int)FreeImage.GetHeight(dib) + matrix[3], new RGBQUAD(Color.Transparent), FREE_IMAGE_COLOR_OPTIONS.FICO_RGBA);
            return FreeImage.SaveEx(sprite, filename);
        }

        public bool Save(string filename, FREE_IMAGE_FORMAT format = FREE_IMAGE_FORMAT.FIF_UNKNOWN, FREE_IMAGE_SAVE_FLAGS flags = FREE_IMAGE_SAVE_FLAGS.DEFAULT)
        {
            return FreeImage.SaveEx(dib, filename, format, flags);
        }
    }
}
