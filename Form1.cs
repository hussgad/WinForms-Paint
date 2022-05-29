using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WinFormsPaint
{
    public partial class MainForm : Form
    {
        private Drawing dr;
        private PictureBox? lastButton;

        public MainForm()
        {
            InitializeComponent();

            dr = new Drawing(Tools.Brush, new Bitmap(pictureBox.Width, pictureBox.Height), pictureBox, Color.Black, 2, currentColorButton);

            foreach (KnownColor color in Enum.GetValues(typeof(KnownColor)))
            {
                PictureBox btn = new PictureBox();
                btn.Size = new Size(30, 30);
                btn.BackColor = Color.FromKnownColor(color);
                btn.Click += new EventHandler(colorChange);
                colorLayoutPanel.Controls.Add(btn);
                if(btn.BackColor == Color.Black)
                    btn.BackgroundImage = WinFormsPaint.Properties.Resources.dashedLines;
            }
        }

        private void colorChange(object sender, EventArgs e)
        {
            if (lastButton != null)
                lastButton.BackgroundImage = null;
            PictureBox button = (PictureBox)sender;
            button.BackgroundImage = WinFormsPaint.Properties.Resources.dashedLines;
            lastButton = button;
            dr.color = button.BackColor;
        }

        private void MainForm_Resize(object sender, EventArgs e)
        {
            dr.ResizeCanvas(pictureBox.Width, pictureBox.Height);
        }

        private void pictureBox_MouseDown(object sender, MouseEventArgs e)
        {
            dr.isDrawing = true;
            dr.isPreview = true;
            dr.point = new Point(e.X, e.Y);
        }

        private void pictureBox_MouseUp(object sender, MouseEventArgs e)
        {
            dr.isDrawing = false;
            dr.isPreview = false;
            dr.Draw(e);
        }

        private void pictureBox_MouseMove(object sender, MouseEventArgs e)
        {
            if(dr.isDrawing)
            {
                dr.Draw(e);
            }
        }

        private void rectangleButton_Click(object sender, EventArgs e)
        {
            dr.Tool = Tools.Rectangle;
        }

        private void ellipseButton_Click(object sender, EventArgs e)
        {
            dr.Tool = Tools.Circle;
        }

        private void brushButton_Click(object sender, EventArgs e)
        {
            dr.Tool = Tools.Brush;
        }

        private void clearButton_Click(object sender, EventArgs e)
        {
            dr.ClearCanvas();
        }

        private void thicknessBox_TextUpdate(object sender, EventArgs e)
        {
            ToolStripComboBox cb = (ToolStripComboBox)sender;
            if(!string.IsNullOrEmpty(cb.Text))
                dr.thickness = Int32.Parse(cb.Text);
        }

        private void saveButton_Click(object sender, EventArgs e)
        {
            SaveFileDialog dialog = new SaveFileDialog();
            dialog.Title = "Open Image";
            dialog.Filter = "bmp files (*.bmp)|*.bmp|JPEG Image (*.jpeg)|*.jpeg|Png Image (*.png)|*.png";
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                dr.bitmap.Save(dialog.FileName);
            }
        }

        private void loadButton_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog dlg = new OpenFileDialog())
            {
                dlg.Title = "Open Image";
                dlg.Filter = "bmp files (*.bmp)|*.bmp|JPEG Image (*.jpeg)|*.jpeg|Png Image (*.png)|*.png";

                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    dr.bitmap = new Bitmap(dlg.FileName);
                    dr.pictureBox.Image = dr.bitmap;
                }
            }
            this.Size = new Size(dr.bitmap.Width + dr.pictureBox.Location.X + dr.pictureBox.Size.Width,
                dr.bitmap.Height + dr.pictureBox.Location.Y + dr.pictureBox.Size.Height);
        }

        private void englishButton_Click(object sender, EventArgs e)
        {
            System.Threading.Thread.CurrentThread.CurrentUICulture = new System.Globalization.CultureInfo("en-GB");
            ComponentResourceManager resources = new ComponentResourceManager(typeof(MainForm));
            foreach (ToolStripItem item in this.toolStrip.Items)
                resources.ApplyResources(item, item.Name, new System.Globalization.CultureInfo("en-GB"));
            applyResources(resources, this.Controls);
        }

        private void polishButton_Click(object sender, EventArgs e)
        {
            System.Threading.Thread.CurrentThread.CurrentUICulture = new System.Globalization.CultureInfo("pl-PL");
            ComponentResourceManager resources = new ComponentResourceManager(typeof(MainForm));
            foreach(ToolStripItem item in this.toolStrip.Items)
                resources.ApplyResources(item, item.Name, new System.Globalization.CultureInfo("pl-PL"));
            applyResources(resources, this.Controls);
        }

        private void applyResources(ComponentResourceManager resources, Control.ControlCollection ctls)
        {
            foreach (Control ctl in ctls)
            {
                resources.ApplyResources(ctl, ctl.Name);
                applyResources(resources, ctl.Controls);
            }
        }
    }

    public enum Tools
    {
        Brush = 0,
        Rectangle,
        Circle
    }


    public class Drawing
    {
        private Tools _tool;      
        public Tools Tool
        {
            get { return _tool; }
            set { _tool = value; ToolChange(); }
        }

        private Color _color;
        public Color color
        {
            get { return _color; }
            set { _color = value; ColorChange(); }
        }

        private int _thickness;

        public int thickness
        {
            get { return _thickness; }
            set { _thickness = value; ThicknessChange(); }
        }

        public ToolStripButton CurrentColorButton { get; set; }
        public Bitmap bitmap { get; set; }
        public PictureBox pictureBox { get; set; }
        public Pen pen { get; set; }
        public Point point { get; set; }
        public bool isDrawing { get; set; }
        public bool isPreview { get; set; }

        public Action<MouseEventArgs>? Draw;

        public Drawing(Tools initialTool, Bitmap bitmap, PictureBox pictureBox, Color color, float thickness, ToolStripButton button)
        {
            Tool = initialTool;
            this.bitmap = bitmap;
            this.pictureBox = pictureBox;
            pen = new Pen(color, thickness);
            this.pictureBox.Image = this.bitmap;
            CurrentColorButton = button;
            CurrentColorButton.BackColor = color;
        }

        private void ToolChange()
        {
            switch(Tool)
            {
                case Tools.Brush:
                    Draw = DrawLine;
                    break;
                case Tools.Rectangle:
                    Draw = DrawRectangle;
                    break;
                case Tools.Circle:
                    Draw = DrawCircle;
                    break;
            }
        }

        private void ColorChange()
        {
            pen.Color = color;
            CurrentColorButton.BackColor = color;
        }

        private void ThicknessChange()
        {
            if(thickness != 0)
                pen.Width = thickness;
        }

        private void DrawLine(MouseEventArgs e)
        {
            using (Graphics g = Graphics.FromImage(bitmap))
            {
                g.DrawLine(pen, point.X, point.Y, e.X, e.Y);
                point = new Point(e.X, e.Y);
                pictureBox.Refresh();
            }
        }

        private void DrawRectangle(MouseEventArgs e)
        {
            if (isPreview)
            {
                Bitmap preview = new Bitmap(bitmap, bitmap.Size.Width, bitmap.Size.Height);
                pictureBox.Image = preview;
            }
            else 
                pictureBox.Image = bitmap;
            using (Graphics g = Graphics.FromImage(pictureBox.Image))
                g.DrawRectangle(pen, Math.Min(e.X, point.X), Math.Min(e.Y, point.Y), Math.Abs(e.X - point.X), Math.Abs(e.Y - point.Y));
            pictureBox.Refresh();
        }

        private void DrawCircle(MouseEventArgs e)
        {
            if (isPreview)
            {
                Bitmap preview = new Bitmap(bitmap, bitmap.Size.Width, bitmap.Size.Height);
                pictureBox.Image = preview;
            }
            else pictureBox.Image = bitmap;
            using (Graphics g = Graphics.FromImage(pictureBox.Image))
                g.DrawEllipse(pen, Math.Min(e.X, point.X), Math.Min(e.Y, point.Y), Math.Abs(e.X - point.X), Math.Abs(e.Y - point.Y));
            pictureBox.Refresh();
        }

        public void ResizeCanvas(int width, int height)
        {
            Bitmap resized = new Bitmap(width, height);
            using (Graphics g = Graphics.FromImage(resized))
            {
                g.Clear(Color.White);
                g.DrawImage(bitmap, 0, 0);
            }
            bitmap = resized;
            pictureBox.Image = bitmap;
            pictureBox.Refresh();
        }

        public void ClearCanvas()
        {
            using (Graphics g = Graphics.FromImage(bitmap))
            {
                g.Clear(Color.White);
            }
            pictureBox.Refresh();
        }
    }
}
