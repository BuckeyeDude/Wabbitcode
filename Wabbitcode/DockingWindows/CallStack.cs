﻿using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Revsoft.Wabbitcode.Services;
using Revsoft.Wabbitcode.Services.Debugger;
using Revsoft.Wabbitcode.Services.Interfaces;
using Revsoft.Wabbitcode.Utils;
using System;
using System.Windows.Forms;

namespace Revsoft.Wabbitcode.DockingWindows
{
	public partial class CallStack : ToolWindow
	{
		private IWabbitcodeDebugger _debugger;

        private readonly IDockingService _dockingService;
        private readonly IDocumentService _documentService;

        private readonly List<DocumentLocation> _callLocations = new List<DocumentLocation>();

		public CallStack(IDockingService dockingService, IDocumentService documentService)
			: base(dockingService)
		{
			InitializeComponent();

			_dockingService = dockingService;
            WabbitcodeDebugger.OnDebuggingStarted += mainForm_OnDebuggingStarted;
		    _documentService = documentService;
		}

		void mainForm_OnDebuggingStarted(object sender, DebuggingEventArgs e)
		{
			_debugger = e.Debugger;
			_debugger.OnDebuggerStep += (o, args) => _dockingService.Invoke(UpdateStack);
			_debugger.OnDebuggerRunningChanged += (o, args) => _dockingService.Invoke(UpdateStack);
		}

		private void UpdateStack()
		{
            _callLocations.Clear();
            callStackView.Rows.Clear();
		    var dataGridViewRows = new List<DataGridViewRow>();
		    foreach (var call in _debugger.CallStack.Reverse())
		    {
		        var row = new DataGridViewRow();
                row.CreateCells(callStackView, call.CallType, call.CallName);
                dataGridViewRows.Add(row);
                _callLocations.Add(call.CallLocation);
		    }
            
		    callStackView.Rows.AddRange(dataGridViewRows.ToArray());
		}

		private void callStackView_DoubleClick(object sender, EventArgs e)
		{
			if (callStackView.SelectedRows.Count == 0)
			{
				return;
			}

		    DocumentLocation location = _callLocations[callStackView.SelectedRows[0].Index];
		    _documentService.GotoLine(location.FileName, location.LineNumber);
		}
	
		#region IClipboardOperation

		public override void Copy()
		{
			if (callStackView == null)
			{
				return;
			}

			DataObject data = callStackView.GetClipboardContent();
			if (data != null)
			{
				Clipboard.SetDataObject(data);
			}
		}

		#endregion

        private void callStackView_RowPostPaint(object sender, DataGridViewRowPostPaintEventArgs e)
        {
            callStackView.Rows[e.RowIndex].DefaultCellStyle.ForeColor = Enabled ? Color.Black : Color.Gray;
        }
	}
}