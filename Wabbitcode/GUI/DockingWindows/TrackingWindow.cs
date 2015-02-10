﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using Aga.Controls.Tree.NodeControls;
using Revsoft.Wabbitcode.Extensions;
using Revsoft.Wabbitcode.GUI.Dialogs;
using Revsoft.Wabbitcode.GUI.DockingWindows.Tracking;
using Revsoft.Wabbitcode.Services.Debugger;
using Revsoft.Wabbitcode.Services.Interfaces;
using Revsoft.Wabbitcode.Utils;

namespace Revsoft.Wabbitcode.GUI.DockingWindows
{
    public partial class TrackingWindow : ToolWindow
    {
        private readonly List<VariableFullViewer> _fullViewers = new List<VariableFullViewer>();
        private readonly TrackingTreeModel _model;
        private readonly IDebuggerService _debuggerService;

        private IWabbitcodeDebugger _debugger;
        private TrackingVariableRowModel _emptyRowModel;

        public TrackingWindow()
        {
            InitializeComponent();

            _debuggerService = DependencyFactory.Resolve<IDebuggerService>();
            _debuggerService.OnDebuggingStarted += DebuggerService_OnDebuggingStarted;
            _debuggerService.OnDebuggingEnded += DebuggerService_OnDebuggingEnded;

            _model = new TrackingTreeModel();
            AddEmptyRow();
            variablesDataView.Model = _model;
            EnablePanel(false);
        }

        private void DebuggerService_OnDebuggingStarted(object sender, DebuggingEventArgs e)
        {
            _valueTypeBox.DropDownItems.Clear();
            _valueTypeBox.DropDownItems.AddRange(VariableDisplayManager.Instance.ControllerNames);

            _debugger = e.Debugger;
            _debugger.DebuggerStep += OnDebuggerOnDebuggerStep;
            _debugger.DebuggerRunningChanged += OnDebuggerOnDebuggerRunningChanged;
        }

        private void DebuggerService_OnDebuggingEnded(object sender, DebuggingEventArgs e)
        {
            _debugger.DebuggerStep -= OnDebuggerOnDebuggerStep;
            _debugger.DebuggerRunningChanged -= OnDebuggerOnDebuggerRunningChanged;
            _debugger = null;
            EnablePanel(false);
            foreach (var viewer in _fullViewers)
            {
                viewer.Close();
            }
        }

        private void OnDebuggerOnDebuggerRunningChanged(object o, DebuggerRunningEventArgs args)
        {
            if (!args.Running)
            {
                UpdateAllRows();
            }

            EnablePanel(!args.Running);
        }

        private void OnDebuggerOnDebuggerStep(object o, DebuggerStepEventArgs args)
        {
            UpdateAllRows();
            EnablePanel(true);
        }

        protected override sealed void EnablePanel(bool enabled)
        {
            base.EnablePanel(enabled);

            variablesDataView.Enabled = enabled;
        }

        #region Clipboard Operation

        public override void Copy()
        {
            if (variablesDataView == null)
            {
                return;
            }

            if (variablesDataView.CurrentEditor != null)
            {
                Clipboard.SetData(DataFormats.Text, variablesDataView.CurrentEditor.Text);
            }
        }

        public override void Paste()
        {
            if (variablesDataView.CurrentEditor != null)
            {
                variablesDataView.CurrentEditor.Text = Clipboard.GetData(DataFormats.Text).ToString();
            }
        }

        #endregion

        #region Row Data

        private void ValueTypeBox_OnChangesApplied(object sender, EventArgs eventArgs)
        {
            var model = (TrackingVariableRowModel)variablesDataView.CurrentNode.Tag;
            model.IsCacheValid = false;
            _model.OnStructureChanged();
        }

        private void AddressBoxOnChangesApplied(object sender, EventArgs eventArgs)
        {
            var model = (TrackingVariableRowModel)variablesDataView.CurrentNode.Tag;
            model.IsCacheValid = false;
            _model.OnStructureChanged();

            if (!string.IsNullOrEmpty(_emptyRowModel.Address))
            {
                AddEmptyRow();
            }
        }

        private void NumBytesBox_OnChangesApplied(object sender, EventArgs eventArgs)
        {
            var model = (TrackingVariableRowModel)variablesDataView.CurrentNode.Tag;
            model.IsCacheValid = false;
            _model.OnStructureChanged();

            if (!string.IsNullOrEmpty(_emptyRowModel.Address))
            {
                AddEmptyRow();
            }
        }


        private void CheckIsEditEnabledValueNeeded(object sender, NodeControlValueEventArgs e)
        {
            e.Value = !(e.Node.Tag is ChildTrackingVariableRowModel);
        }

        private void AddEmptyRow()
        {
            _emptyRowModel = new TrackingVariableRowModel(_debuggerService, VariableDisplayManager.Instance);
            _model.Nodes.Add(_emptyRowModel);
            _model.OnStructureChanged();
        }

        #endregion

        private void UpdateAllRows()
        {
            variablesDataView.Root.IsExpanded = true;
            foreach (var model in _model.Nodes)
            {
                model.IsCacheValid = false;
                var recalcedValue = model.Value;
                if (recalcedValue == null)
                {
                    throw new Exception("Invalid value");
                }
            }

            if (InvokeRequired)
            {
                this.Invoke(() => _model.OnStructureChanged());
            }
            else
            {
                _model.OnStructureChanged();
            }
        }

        private void temp_FormClosed(object sender, FormClosedEventArgs e)
        {
            foreach (VariableFullViewer test in _fullViewers.Where(test => test.Tag == ((Form) sender).Tag))
            {
                _fullViewers.Remove(test);
                break;
            }
        }

        private void variablesDataView_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode != Keys.Delete)
            {
                return;
            }

            var currentModel = (TrackingVariableRowModel)variablesDataView.CurrentNode.Tag;
            if (currentModel == _emptyRowModel)
            {
                return;
            }

            _model.Nodes.Remove(currentModel);
            _model.OnNodesRemoved(currentModel);
        }

        private void variablesDataView_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            var node = variablesDataView.GetNodeControlInfoAt(e.Location);
            if (node.Node == null || node.Control.ParentColumn == null 
                || node.Control.ParentColumn.Index != variableValueCol.Index)
            {
                return;
            }

            var model = node.Node.Tag as TrackingVariableRowModel;
            if (model == null)
            {
                return;
            }

            var fullValue = model.FullValue;
            if (fullValue == null)
            {
                return;
            }

            VariableFullViewer temp = new VariableFullViewer(model.Address, fullValue)
            {
                Tag = fullValue
            };

            temp.Show();
            _fullViewers.Add(temp);
            temp.FormClosed += temp_FormClosed;
        }
    }
}