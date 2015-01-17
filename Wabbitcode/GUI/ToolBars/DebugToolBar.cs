﻿using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using Revsoft.Wabbitcode.Actions;
using Revsoft.Wabbitcode.Extensions;
using Revsoft.Wabbitcode.Services.Debugger;
using Revsoft.Wabbitcode.Services.Interfaces;
using Revsoft.Wabbitcode.Utils;

namespace Revsoft.Wabbitcode.GUI.ToolBars
{
    internal sealed partial class DebugToolBar : ToolStrip
    {
        private static readonly ComponentResourceManager Resources = new ComponentResourceManager(typeof(DebugToolBar));

        private readonly ToolStripButton _runDebuggerToolButton = new ToolStripButton
        {
            DisplayStyle = ToolStripItemDisplayStyle.Image,
            Image = ((Image) (Resources.GetObject("runDebuggerToolButton"))),
            Text = "Start Debug"
        };

        private readonly ToolStripButton _pauseToolButton = new ToolStripButton
        {
            DisplayStyle = ToolStripItemDisplayStyle.Image,
            Enabled = false,
            Image = ((Image) (Resources.GetObject("pauseToolButton"))),
            Text = "Pause"
        };

        private readonly ToolStripButton _stopDebugToolButton = new ToolStripButton
        {
            DisplayStyle = ToolStripItemDisplayStyle.Image,
            Enabled = false,
            Image = ((Image) (Resources.GetObject("stopToolButton"))),
            Text = "Stop"
        };

        private readonly ToolStripButton _restartToolStripButton = new ToolStripButton
        {
            DisplayStyle = ToolStripItemDisplayStyle.Image,
            Enabled = false,
            Image = ((Image) (Resources.GetObject("restartToolStripButton"))),
            Text = "Restart"
        };

        private readonly ToolStripSeparator _toolStripSeparator1 = new ToolStripSeparator();

        private readonly ToolStripButton _gotoCurrentToolButton = new ToolStripButton
        {
            DisplayStyle = ToolStripItemDisplayStyle.Image,
            Enabled = false,
            Image = ((Image) (Resources.GetObject("gotoCurrentToolButton"))),
            Text = "Goto Current Line"
        };

        private readonly ToolStripButton _stepToolButton = new ToolStripButton
        {
            DisplayStyle = ToolStripItemDisplayStyle.Image,
            Enabled = false,
            Image = ((Image) (Resources.GetObject("stepToolButton"))),
            Text = "Step"
        };

        private readonly ToolStripButton _stepOverToolButton = new ToolStripButton
        {
            DisplayStyle = ToolStripItemDisplayStyle.Image,
            Enabled = false,
            Image = ((Image) (Resources.GetObject("stepOverToolButton"))),
            Text = "Step Over"
        };

        private readonly ToolStripButton _stepOutToolButton = new ToolStripButton
        {
            DisplayStyle = ToolStripItemDisplayStyle.Image,
            Enabled = false,
            Image = ((Image) (Resources.GetObject("stepOutToolButton"))),
            Text = "Step Out"
        };

        private readonly IDebuggerService _debuggerService;

        public DebugToolBar()
        {
            AllowItemReorder = true;
            GripStyle = ToolStripGripStyle.Hidden;
            Items.AddRange(new ToolStripItem[]
            {
                _runDebuggerToolButton,
                _pauseToolButton,
                _stopDebugToolButton,
                _restartToolStripButton,
                _toolStripSeparator1,
                _gotoCurrentToolButton,
                _stepToolButton,
                _stepOverToolButton,
                _stepOutToolButton
            });
            RenderMode = ToolStripRenderMode.System;
            GripStyle = ToolStripGripStyle.Visible;

            _runDebuggerToolButton.Click += runDebuggerToolButton_Click;
            _pauseToolButton.Click += pauseToolButton_Click;
            _stopDebugToolButton.Click += stopDebugToolButton_Click;
            _restartToolStripButton.Click += restartToolStripButton_Click;
            _gotoCurrentToolButton.Click += gotoCurrentToolButton_Click;
            _stepToolButton.Click += stepToolButton_Click;
            _stepOverToolButton.Click += stepOverToolButton_Click;
            _stepOutToolButton.Click += stepOutToolButton_Click;

            _debuggerService = DependencyFactory.Resolve<IDebuggerService>();
            _debuggerService.OnDebuggingStarted += DebuggerService_OnDebuggingStarted;
            _debuggerService.OnDebuggingEnded += DebuggerService_OnDebuggingEnded;
        }

        private bool _isDebugging;

        private void DebuggerService_OnDebuggingStarted(object sender, DebuggingEventArgs e)
        {
            _isDebugging = true;
            e.Debugger.DebuggerRunningChanged += Debugger_OnDebuggerRunningChanged;
            e.Debugger.DebuggerStep += Debugger_OnDebuggerStep;
            EnableIcons();
        }

        private void DebuggerService_OnDebuggingEnded(object sender, DebuggingEventArgs e)
        {
            _isDebugging = false;
            e.Debugger.DebuggerRunningChanged += Debugger_OnDebuggerRunningChanged;
            e.Debugger.DebuggerStep += Debugger_OnDebuggerStep;
            EnableIcons();
        }

        private void Debugger_OnDebuggerStep(object o, DebuggerStepEventArgs args)
        {
            EnableIcons();
        }

        private void Debugger_OnDebuggerRunningChanged(object o, DebuggerRunningEventArgs args)
        {
            EnableIcons();
        }

        private void EnableIcons()
        {
            if (InvokeRequired)
            {
                this.Invoke(EnableIcons);
                return;
            }

            bool isRunning = _isDebugging && _debuggerService.CurrentDebugger.IsRunning;
            bool enabled = _isDebugging && !isRunning;
            bool hasCallStack = _isDebugging && _debuggerService.CurrentDebugger.CallStack.Count > 0;

            _gotoCurrentToolButton.Enabled = enabled;
            _stepToolButton.Enabled = enabled;
            _stepOverToolButton.Enabled = enabled;
            _stepOutToolButton.Enabled = enabled && hasCallStack;
            _stopDebugToolButton.Enabled = _isDebugging;
            _restartToolStripButton.Enabled = _isDebugging;
            _runDebuggerToolButton.Enabled = enabled || !_isDebugging;
            _pauseToolButton.Enabled = isRunning;
        }

        private static void runDebuggerToolButton_Click(object sender, EventArgs e)
        {
            AbstractUiAction.RunCommand(new StartDebuggerAction());
        }

        private static void pauseToolButton_Click(object sender, EventArgs e)
        {
            AbstractUiAction.RunCommand(new PauseDebuggerAction());
        }

        private static void stopDebugToolButton_Click(object sender, EventArgs e)
        {
            AbstractUiAction.RunCommand(new StopDebuggerAction());
        }

        private static void restartToolStripButton_Click(object sender, EventArgs e)
        {
            AbstractUiAction.RunCommand(new RestartDebuggerAction());
        }

        private static void stepToolButton_Click(object sender, EventArgs e)
        {
            AbstractUiAction.RunCommand(new StepDebuggerAction());
        }

        private static void stepOverToolButton_Click(object sender, EventArgs e)
        {
            AbstractUiAction.RunCommand(new StepOverDebuggerAction());
        }

        private static void stepOutToolButton_Click(object sender, EventArgs e)
        {
            AbstractUiAction.RunCommand(new StepOutDebuggerAction());
        }

        private void gotoCurrentToolButton_Click(object sender, EventArgs e)
        {
            IWabbitcodeDebugger debugger = _debuggerService.CurrentDebugger;
            DocumentLocation location = debugger.GetAddressLocation(debugger.CPU.PC);
            AbstractUiAction.RunCommand(new GotoLineAction(location));
        }
    }
}