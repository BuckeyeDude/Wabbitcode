using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.Windows.Forms;
using System.Reflection;
using System.ComponentModel;

namespace Aga.Controls.Tree.NodeControls
{
	public class NodeTextBox : BaseTextControl
	{
		private const int MinTextBoxWidth = 30;

		public NodeTextBox()
		{
		}

		protected override Size CalculateEditorSize(EditorContext context)
		{
			if (Parent.UseColumns)
				return context.Bounds.Size;
			else
			{
				Size size = GetLabelSize(context.CurrentNode, context.DrawContext, _label);
				int width = Math.Max(size.Width + Font.Height, MinTextBoxWidth); // reserve a place for new typed character
				return new Size(width, size.Height);
			}
		}

		public override void KeyDown(KeyEventArgs args)
		{
		    bool isCharPress = char.IsLetterOrDigit((char) args.KeyCode);
            if ((args.KeyCode == Keys.F2 || (isCharPress && Parent.UseColumns)) &&
                Parent.CurrentNode != null && EditEnabled)
			{
				args.Handled = true;
				BeginEdit();
                if (args.KeyCode != Keys.F2)
			    {
			        char keyPress = (args.Modifiers & Keys.ShiftKey) != 0
			            ? (char) args.KeyData
			            : char.ToLower((char) args.KeyData);
			        SendKeys.Send(keyPress.ToString());
			    }
			}
		}

		protected override Control CreateEditor(TreeNodeAdv node)
		{
			TextBox textBox = CreateTextBox();
			textBox.TextAlign = TextAlign;
			textBox.Text = GetLabel(node);
			textBox.BorderStyle = BorderStyle.FixedSingle;
			textBox.TextChanged += EditorTextChanged;
			textBox.KeyDown += EditorKeyDown;
		    textBox.LostFocus += EditorLostFocus;
			_label = textBox.Text;
			SetEditControlProperties(textBox, node);
			return textBox;
		}

	    private void EditorLostFocus(object sender, EventArgs e)
	    {
	        EndEdit(true);
	    }

	    protected virtual TextBox CreateTextBox()
		{
            return new NodeTextBoxEditor();
		}

		protected override void DisposeEditor(Control editor)
		{
			var textBox = editor as TextBox;
			textBox.TextChanged -= EditorTextChanged;
			textBox.KeyDown -= EditorKeyDown;
		}

		private void EditorKeyDown(object sender, KeyEventArgs e)
		{
			if (e.KeyCode == Keys.Escape)
				EndEdit(false);
			else if (e.KeyCode == Keys.Enter)
				EndEdit(true);
            else if (e.KeyCode == Keys.Tab)
            {
                var currentNode = Parent.CurrentNode;
                var controls = Parent.GetNodeControls(Parent.CurrentNode);
                var controlsList = new List<NodeControlInfo>();
                int index = 0;
                int currentIndex = -1;
                foreach (var control in controls)
                {
                    controlsList.Add(control);
                    if (control.Control == this)
                    {
                        currentIndex = index;
                    }

                    index++;
                }

                EndEdit(true);

                Parent.CurrentNode = currentNode;
                if (currentIndex < controlsList.Count)
                {
                    var keyEvent = new KeyEventArgs(Keys.F2);
                    controlsList[currentIndex + 1].Control.KeyDown(keyEvent);
                }
            }
		}

		private string _label;
		private void EditorTextChanged(object sender, EventArgs e)
		{
			var textBox = sender as TextBox;
			_label = textBox.Text;
			Parent.UpdateEditorBounds();
		}

		protected override void DoApplyChanges(TreeNodeAdv node, Control editor)
		{
			var label = (editor as TextBox).Text;
			string oldLabel = GetLabel(node);
			if (oldLabel != label)
			{
				SetLabel(node, label);
				OnLabelChanged(node.Tag, oldLabel, label);
			}
		}

		public override void Cut(Control control)
		{
			(control as TextBox).Cut();
		}

		public override void Copy(Control control)
		{
			(control as TextBox).Copy();
		}

		public override void Paste(Control control)
		{
			(control as TextBox).Paste();
		}

		public override void Delete(Control control)
		{
			var textBox = control as TextBox;
			int len = Math.Max(textBox.SelectionLength, 1);
			if (textBox.SelectionStart < textBox.Text.Length)
			{
				int start = textBox.SelectionStart;
				textBox.Text = textBox.Text.Remove(textBox.SelectionStart, len);
				textBox.SelectionStart = start;
			}
		}

		public event EventHandler<LabelEventArgs> LabelChanged;
		protected void OnLabelChanged(object subject, string oldLabel, string newLabel)
		{
			if (LabelChanged != null)
				LabelChanged(this, new LabelEventArgs(subject, oldLabel, newLabel));
		}

        private class NodeTextBoxEditor : TextBox
        {
            protected override bool IsInputKey(Keys keyData)
            {
                return keyData == Keys.Tab || base.IsInputKey(keyData);
            }
        }
	}
}
