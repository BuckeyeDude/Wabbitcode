﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace Revsoft.TextEditor.Document
{
    public class IconManager
    {
        private List<MarginIcon> iconsToDraw = new List<MarginIcon>();
		internal List<MarginIcon> IconsToDraw
		{
			get { return iconsToDraw; }
		}

		public void ClearIcons()
		{
			iconsToDraw.Clear();
		}

        public void AddIcon(MarginIcon icon)
        {
            iconsToDraw.Add(icon);
        }

        public void RemoveIcon(MarginIcon icon)
        {
            iconsToDraw.Remove(icon);
        }
    }

    public class MarginIcon : Control
    {
        public Bitmap image;
        public int lineNum;
        public ToolTip toolTip;
        public MarginIcon(Bitmap image, int lineNum, ToolTip toolTip)
        {
            this.image = image;
            this.lineNum = lineNum;
            this.toolTip = toolTip;
            Bounds = new Rectangle(0, 0, 20, 800);
        }

        protected override void OnMouseHover(EventArgs e)
        {
            toolTip.Show(toolTip.ToolTipTitle, this, MousePosition.X, MousePosition.Y);
            base.OnMouseHover(e);
        }

        protected override void OnMouseClick(MouseEventArgs e)
        {
            MessageBox.Show("Hello");
            base.OnMouseClick(e);
        }

    }
}
