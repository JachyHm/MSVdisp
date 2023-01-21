using System.Diagnostics;

namespace MSVdisp
{
    public partial class Form1 : Form
    {
        private const int NUM_OF_CHARACTERS = 0xFF - 0x20 + 1;
        private static readonly Color BACKGROUND_COLOR = Color.Black;
        private static readonly Color TEXT_COLOR = Color.OrangeRed;
        private static readonly Color EMPTY_COLOR = Color.Transparent;

        public Form1()
        {
            InitializeComponent();
            fontComboBox.Items.AddRange(Fonts.fonts.Keys.ToArray());
            fontComboBox.SelectedIndex = 0;
            DrawChar();
        }

        private bool codeIsWriting = false;
        private void TextBox1_TextChanged(object sender, EventArgs e)
        {
            if (codeIsWriting)
            {
                return;
            }

            byte c = (byte)(textBox1.Text.Length > 0 ? textBox1.Text.ToCharArray()[0] : 0x20);
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
            textBox1.Text = ((char)numericUpDown1.Value).ToString();
            codeIsWriting = false;

            DrawChar();
        }

        private void fontComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            DrawChar();
        }

        private void DrawChar()
        {
            string fontId = (string)fontComboBox.SelectedItem;
            uint[] font = Fonts.fonts[fontId];

            Trace.Assert(font.Length > 3);

            uint fontHeight = font[0];
            uint fontWidth = font[1];

            Trace.Assert(font.Length == 3 + ((fontWidth + 1) * NUM_OF_CHARACTERS));

            int drawWidth = pictureBox1.Width;
            int drawHeight = pictureBox1.Height;

            int rescaleX = (int)(drawWidth/fontWidth);
            int rescaleY = (int)(drawHeight/fontHeight);
            int rescale = Math.Min(rescaleX, rescaleY);

            drawWidth = (int)fontWidth * rescale;
            drawHeight = (int)fontHeight * rescale;
            Bitmap bitmap = new(drawWidth, drawHeight);

            uint offset = 3 + (((uint)numericUpDown1.Value - 0x20)*(fontWidth+1));
            uint charWidth = font[offset++];
            for (int x = 0; x < drawWidth; x++)
            {
                uint column = font[offset + (x/rescale)];
                for (int y = 0; y < drawHeight; y++)
                {
                    if (x >= charWidth * rescale)
                    {
                        bitmap.SetPixel(x, y, EMPTY_COLOR);
                    }
                    else
                    {
                        uint value = (column >> (int)(fontHeight-(y/rescale)-1)) & 0x1;
                        bitmap.SetPixel(x, y, value == 1 ? TEXT_COLOR : BACKGROUND_COLOR);
                    }
                }
            }

            pictureBox1.Image = bitmap;
        }
    }
}