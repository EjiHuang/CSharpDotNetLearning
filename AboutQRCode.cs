        #region method

        /// <summary>
        ///     识别二维码并且绘制矩形
        /// </summary>
        private void DecodeQRCodeAndDrawRect()
        {
            foreach (var screen in Screen.AllScreens)
            {
                using (Bitmap bmpScreen = new Bitmap(screen.Bounds.Width, screen.Bounds.Height))
                {
                    using (Graphics g = Graphics.FromImage(bmpScreen))
                    {
                        g.CopyFromScreen(screen.Bounds.X, screen.Bounds.Y, 0, 0, bmpScreen.Size, CopyPixelOperation.SourceCopy);
                    }

                    var source = new BitmapLuminanceSource(bmpScreen);
                    var bitmap = new BinaryBitmap(new HybridBinarizer(source));
                    var result = new QRCodeReader().decode(bitmap);

                    var retX = result.ResultPoints.Select(o => o.X);
                    var retY = result.ResultPoints.Select(o => o.Y);
                    double minX = retX.Min();
                    double minY = retY.Min();
                    double maxX = retX.Max();
                    double maxY = retY.Max();

                    double margin = (maxX - minX) * 0.20f;
                    minX += -margin;
                    maxX += margin;
                    minY += -margin;
                    maxY += margin;

                    // 使用GDI+进行矩形绘制
                    using (var pen = new Pen(ColorTranslator.FromHtml("#F7190B"), 4F))
                    {
                        using (var g = Graphics.FromHdc(GetWindowDC(IntPtr.Zero)))
                            g.DrawRectangle(pen, (float) minX, (float) minY, (float) (maxX - minX), (float) (maxY - minY));
                    }
                }
            }
        }

        /// <summary>
        ///     在屏幕画矩形区域
        /// </summary>
        private void DrawRectOnScreen(double minX, double maxX, double minY, double maxY)
        {
            IntPtr windowDC = GetWindowDC(IntPtr.Zero);

            // 下
            PatBlt(windowDC, (int) minX, (int) maxY, (int) (maxX - minX), 5, PatBltTypes.PATINVERT);
            // 左
            PatBlt(windowDC, (int) minX, (int) minY - 3, 5, -((int) minY - (int) maxY - 6), PatBltTypes.PATINVERT);
            // 右
            PatBlt(windowDC, (int) maxX - 3, (int) maxY + 3, 5, (int) minY - (int) maxY - 6, PatBltTypes.PATINVERT);
            // 上
            PatBlt(windowDC, (int) maxX, (int) minY - 3, -((int) maxX - (int) minX), 5, PatBltTypes.PATINVERT);

        }

        #endregion

        #region native method

        [Serializable]
        public struct Rect
        {
            public int left;
            public int top;
            public int right;
            public int bottom;
        }

        public enum PatBltTypes
        {
            SRCCOPY = 0x00CC0020,     //将源矩形区域直接拷贝到目标矩形区域
            SRCPAINT = 0x00EE0086,    //通过使用布尔型的OR（或）操作符将源和目标矩形区域的颜色合并
            SRCAND = 0x008800C6,      //通过使用AND（与）操作符来将源和目标矩形区域内的颜色合并
            SRCINVERT = 0x00660046,   //通过使用布尔型的XOR（异或）操作符将源和目标矩形区域的颜色合并
            SRCERASE = 0x00440328,    //通过使用AND（与）操作符将目标矩形区域颜色取反后与源矩形区域的颜色值合并
            NOTSRCCOPY = 0x00330008,  //将源矩形区域颜色取反，于拷贝到目标矩形区域
            NOTSRCERASE = 0x001100A6, //使用布尔类型的OR（或）操作符组合源和目标矩形区域的颜色值，然后将合成的颜色取反
            MERGECOPY = 0x00C000CA,   //表示使用布尔型的AND（与）操作符将源矩形区域的颜色与特定模式组合一起
            MERGEPAINT = 0x00BB0226,  //通过使用布尔型的OR（或）操作符将反向的源矩形区域的颜色与目标矩形区域的颜色合并
            PATCOPY = 0x00F00021,     //将特定的模式拷贝到目标位图上
            PATPAINT = 0x00FB0A09,    //通过使用布尔OR（或）操作符将源矩形区域取反后的颜色值与特定模式的颜色合并。然后使用OR（或）操作符将该操作的结果与目标矩形区域内的颜色合并
            PATINVERT = 0x005A0049,   //通过使用XOR（异或）操作符将源和目标矩形区域内的颜色合并
            DSTINVERT = 0x00550009,   //表示使目标矩形区域颜色取反
            BLACKNESS = 0x00000042,   //表示使用与物理调色板的索引0相关的色彩来填充目标矩形区域，（对缺省的物理调色板而言，该颜色为黑色）。
            WHITENESS = 0x00FF0062    //使用与物理调色板中索引1有关的颜色填充目标矩形区域。（对于缺省物理调色板来说，这个颜色就是白色）。
        }

        [DllImport("gdi32.dll")]
        public static extern bool PatBlt(IntPtr hdc, int nXLeft, int nYLeft, int nWidth, int nHeight, PatBltTypes dwRop);

        [DllImport("user32.dll")]
        public static extern IntPtr GetWindowDC(IntPtr hWnd);

        [DllImport("user32.dll")]
        public extern static bool GetWindowRect(IntPtr hwnd, ref Rect rectangle);

        #endregion
