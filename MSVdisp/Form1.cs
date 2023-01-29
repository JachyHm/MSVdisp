using System.Diagnostics;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Security;
using System.Text;

namespace MSVdisp
{
    public partial class Form1 : Form
    {
        private const int NUM_OF_CHARACTERS = 0xFF - 0x20 + 1;
        private const int HEIGHT = 1024;
        private const int WIDTH = 512;
        private const string PRODUCT_PATH = @"g:\RailworksData\Source\JachyHm\CD460pack01\";
        private static readonly Rectangle DEST_RECT = new Rectangle(0, 0, WIDTH, HEIGHT);

        private bool codeDraw = false;

        struct FontArray
        {
            public uint[] Data;
            public uint Height;
            public uint Width;

            public FontArray(uint[] data)
            {
                Data = data;
                Trace.Assert(data.Length > 3);

                Height = data[0];
                Width = data[1];

                Trace.Assert(data.Length == 3 + ((Width + 1) * NUM_OF_CHARACTERS));
            }
        }

        private static FontArray font;
        private static Bitmap destImage = new Bitmap(WIDTH, HEIGHT);
        //private static Graphics destG = Graphics.FromImage(destImage);
        private static byte[,] leds = new byte[0, 0];
        private static Color TRANSPARENT_ORANGE = Color.FromArgb(0, 255, 102, 1);

        public Form1()
        {
            InitializeComponent();
            fontComboBox.Items.AddRange(Fonts.fonts.Keys.ToArray());
            fontComboBox.SelectedIndex = 0;

            /*destG.CompositingMode = CompositingMode.SourceCopy;
            destG.CompositingQuality = CompositingQuality.HighQuality;
            destG.InterpolationMode = InterpolationMode.HighQualityBicubic;
            destG.SmoothingMode = SmoothingMode.HighQuality;
            destG.PixelOffsetMode = PixelOffsetMode.HighQuality;*/

            DrawChar();
        }

        private bool codeIsWriting = false;
        private void TextBox1_TextChanged(object sender, EventArgs e)
        {
            if (codeIsWriting)
            {
                return;
            }

            byte c = (byte)(textBox1.Text.Length > 0 ? CodePagesEncodingProvider.Instance.GetEncoding("windows-1250")!.GetBytes(textBox1.Text)[0] : 0x20);
            if (c is < 0x20 or > 0xFF)
            {
                textBox1.Text = "";
                return;
            }

            codeIsWriting = true;
            numericUpDown1.Value = c;
            codeIsWriting = false;

            DrawChar();
        }

        private void NumericUpDown1_ValueChanged(object sender, EventArgs e)
        {
            if (codeIsWriting)
            {
                return;
            }

            codeIsWriting = true;
            textBox1.Text = CodePagesEncodingProvider.Instance.GetEncoding("windows-1250")!.GetString(new byte[] { (byte)numericUpDown1.Value });
            codeIsWriting = false;

            DrawChar();
        }

        private void fontComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            string fontId = (string)fontComboBox.SelectedItem;
            font = new FontArray(Fonts.fonts[fontId]);

            DrawChar(false, true);
        }

        private byte ExtractBit(uint colData, int row)
        {
            return (byte)((colData >> (int)(font.Height - row - 1)) & 0x1);
        }

        private byte[,] FillArray(uint offset)
        {
            byte[,] leds = new byte[font.Width, font.Height];

            for (byte x = 0; x < font.Width; x++)
            {
                uint column = font.Data[offset + x];
                for (byte y = 0; y < font.Height; y++)
                {
                    byte value = (byte)(ExtractBit(column, y) * byte.MaxValue);
                    leds[x, y] = value;

                    /*if (value == byte.MaxValue)
                        continue;

                    byte sum = 128;
                    for (byte i = (byte)(x == 0 ? 3 : 0); i < (x == font.Height-1 ? 6 : 9); i++)
                    {
                        if (i == 4)
                            i++;

                        sbyte row = (sbyte)(y+(i%3)-1);
                        if (row < 0)
                            continue;

                        uint colData = font.Data[offset + (i/3) + x - 1];
                        if (ExtractBit(colData, row) == 1)
                            sum = (byte)((sum >> 1)&255);
                    }
                    leds[x, y] = (byte)(128 - sum);*/
                }
            }

            return leds;
        }

        private void DrawChar(bool fullChar = false, bool redraw = false)
        {
            uint offset = 3 + (((uint)numericUpDown1.Value - 0x20) * (font.Width + 1));
            uint charWidth = fullChar ? font.Width : font.Data[offset++];
            leds = FillArray(offset);

            int rescaleX = (int)(WIDTH / font.Width);
            int rescaleY = (int)(HEIGHT / font.Height);
            int rescale = Math.Min(rescaleX, rescaleY);

            int drawWidth = (int)font.Width * rescale;
            int drawHeight = (int)font.Height * rescale;
            if (redraw)
                destImage = new(drawWidth, drawHeight);

            float ledSz = drawWidth / (font.Width);
            //var destImage = new Bitmap(WIDTH, HEIGHT);

            //destImage.SetResolution(tempBitmap.HorizontalResolution, tempBitmap.VerticalResolution);

            using (var g = Graphics.FromImage(destImage))
            {
                g.CompositingMode = CompositingMode.SourceCopy;
                g.CompositingQuality = CompositingQuality.HighQuality;
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.SmoothingMode = SmoothingMode.HighQuality;
                g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                g.Clear(TRANSPARENT_ORANGE);

                for (int x = 0; x < charWidth; x++)
                {
                    for (int y = 0; y < font.Height; y++)
                    {
                        byte val = leds[x, y];
                        if (val == 0 && !fullChar)
                            continue;
                        if (val == byte.MaxValue || fullChar)
                        {
                            //g.DrawImage(Resources.LED_zhas, x * ledSz, y * ledSz, ledSz, ledSz);
                            g.DrawImage(Resources.LED, x * ledSz, y * ledSz, ledSz, ledSz);
                        } /*else
                    {
                        ColorMatrix CMFade = new ColorMatrix();
                        ImageAttributes AFade = new ImageAttributes();
                        CMFade.Matrix33 = val/255f;
                        AFade.SetColorMatrix(CMFade, ColorMatrixFlag.Default, ColorAdjustType.Bitmap);
                        g.DrawImage(Resources.LED_zhas, new PointF[] { new PointF(x * ledSz, y * ledSz), new PointF(x * ledSz + ledSz, y * ledSz), new PointF(x * ledSz, y * ledSz + ledSz) }, new RectangleF(0, 0, 47, 42), GraphicsUnit.Pixel, AFade);
                    }*/
                    }
                }
            }

            /*using (var wrapMode = new ImageAttributes())
            {
                wrapMode.SetWrapMode(WrapMode.TileFlipXY);
                destG.DrawImage(tempBitmap, DEST_RECT, 0, 0, tempBitmap.Width, tempBitmap.Height, GraphicsUnit.Pixel, wrapMode);
            }*/

            if (!codeDraw)
                pictureBox1.Image = destImage;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            codeDraw = true;
            FolderBrowserDialog dialog = new()
            {
                AutoUpgradeEnabled = true,
                ShowNewFolderButton = true,
            };
            DialogResult result = dialog.ShowDialog();
            if (result != DialogResult.OK)
                return;

            string folder = dialog.SelectedPath;
            string relativeFolder = Path.GetRelativePath(PRODUCT_PATH, folder);
            using (StreamWriter sw = new(Path.Combine(folder, $"{((string)fontComboBox.SelectedItem).Replace(" ", "_")}.xml"), false))
            {
                sw.Write(Resources.xmlStart);
                Directory.CreateDirectory(Path.Combine(folder, "source"));
                for (int j = 0x20; j <= 0xFF; j++)
                {
                    numericUpDown1.Value = j;
                    string filename = $"decal_primarynumber_{j - 0x20}";
                    destImage.Save(Path.Combine(folder, "source", $"{filename}.png"), ImageFormat.Png);
                    sw.Write(string.Format(Resources.xmlData, SecurityElement.Escape(((char)j).ToString()), Path.Combine(relativeFolder, $"{filename}.dds")));
                }
                DrawChar(true);
                destImage.Save(Path.Combine(folder, "source", "fc.png"), ImageFormat.Png);
                sw.Write(Resources.xmlEnd);
                sw.Close();
            }
            MessageBox.Show("Generování OK!");
            codeDraw = false;
        }
    }
}