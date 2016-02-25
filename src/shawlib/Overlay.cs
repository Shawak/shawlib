﻿using System;
using System.Drawing;
using System.Windows.Forms;

namespace ShawLib
{
    public partial class Overlay : Form
    {
        Margins marg;

        byte opacity = 255;
        public new virtual double Opacity
        {
            get
            {
                return (double)opacity / 255;
            }
            set
            {
                if (value < 0 || value > 1)
                    return;

                var val = 255 * value;
                val = Math.Round(val, 0);

                if (val > 255)
                    val = 255;
                else if (val < 0)
                    val = 0;

                opacity = (byte)val;
                NativeMethods.SetLayeredWindowAttributes(this.Handle, 0, opacity, 0x2);
            }
        }

        protected override CreateParams CreateParams
        {
            get
            {
                var createParams = base.CreateParams;
                createParams.ExStyle |= (int)ExtendedWindowStyles.Transparent | (int)ExtendedWindowStyles.Layered
                    | (int)ExtendedWindowStyles.ToolWindow | (int)ExtendedWindowStyles.TopMost;
                return createParams;
            }
        }

        public Overlay()
        {
            InitializeComponent();

            this.Visible = true;
            this.ShowInTaskbar = false;
            this.FormBorderStyle = FormBorderStyle.None;
            this.Opacity = 1;

            this.SetStyle(ControlStyles.AllPaintingInWmPaint, true);
            this.SetStyle(ControlStyles.OptimizedDoubleBuffer, true);
            this.SetStyle(ControlStyles.UserPaint, true);
            this.UpdateStyles();

            OnResize(null);
        }

        protected override void OnResize(EventArgs e)
        {
            marg.Left = 0;
            marg.Top = 0;
            marg.Right = this.Width;
            marg.Bottom = this.Height;
            NativeMethods.DwmExtendFrameIntoClientArea(this.Handle, ref marg);
            base.OnResize(e);
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {

        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.Clear(Color.Transparent);
            base.OnPaint(e);
        }
    }
}
