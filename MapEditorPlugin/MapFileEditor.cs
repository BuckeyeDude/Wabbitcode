using System;
using System.IO;
using System.Linq;
using System.Windows;
using Revsoft.Wabbitcode.Extensions;
using Revsoft.Wabbitcode.GUI.DocumentWindows;
using Revsoft.Wabbitcode.Services.Debugger;
using Revsoft.Wabbitcode.Services.Interfaces;
using Revsoft.Wabbitcode.Utils;
using WabbitemuLib;
using WPFZ80MapEditor;

namespace MapEditorPlugin
{
    public partial class MapFileEditor : AbstractFileEditor
    {
        private readonly IDebuggerService _debuggerService;
        private readonly ISymbolService _symbolService;
        
        private bool _isTesting;
        private IBreakpoint _mapLoadBreak;

        protected override bool DocumentChanged
        {
            get { return base.DocumentChanged || UndoManager.CanRedo(); }
            set { base.DocumentChanged = value; }
        }

        public static bool OpenDocument(FilePath filename)
        {
            var dockingService = DependencyFactory.Resolve<IDockingService>();
            var child = dockingService.Documents.OfType<MapFileEditor>().SingleOrDefault(e => e.FileName == filename);
            if (child != null)
            {
                child.Show();
                return true;
            }

            var name = Path.GetFileName(filename);
            var doc = new MapFileEditor()
            {
                Text = name,
                TabText = name,
                ToolTipText = filename
            };

            doc.OpenFile(filename);


            dockingService.ShowDockPanel(doc);
            return true;
        }

        private MapFileEditor()
        {
            InitializeComponent();

            _debuggerService = DependencyFactory.Resolve<IDebuggerService>();
            _symbolService = DependencyFactory.Resolve<ISymbolService>();

            _debuggerService.OnDebuggingStarted += DebuggerService_OnDebuggingStarted;
            _debuggerService.OnDebuggingEnded += DebuggerService_OnDebuggingEnded;
        }

        private void DebuggerService_OnDebuggingStarted(object sender, DebuggingEventArgs e)
        {
            var debugger = e.Debugger;
            debugger.DebuggerRunningChanged += Debugger_DebuggerRunningChanged;


            var mapLoadAddress = _symbolService.SymbolTable.GetAddressFromLabel("load_map");
            if (!mapLoadAddress.HasValue)
            {
                return;
            }

            var calcAddr = new CalcAddress();
            var calcPage = debugger.NativeDebugger.Memory.Flash[0x15 - (mapLoadAddress.Value >> 16)];

            calcAddr.Initialize(calcPage, (ushort) mapLoadAddress.Value);
            _mapLoadBreak = debugger.NativeDebugger.Breakpoints.Add(calcAddr);
            debugger.NativeDebugger.Breakpoint += NativeDebuggerOnBreakpoint;
        }

        private void NativeDebuggerOnBreakpoint(Wabbitemu calc, IBreakpoint breakpoint)
        {
            if (breakpoint.Address != _mapLoadBreak.Address)
            {
                return;
            }

            _debuggerService.CurrentDebugger.NativeDebugger.Breakpoint -= NativeDebuggerOnBreakpoint;
            _debuggerService.CurrentDebugger.NativeDebugger.Breakpoints.Remove(_mapLoadBreak);
            _debuggerService.CurrentDebugger.Run();
            StartTesting();
        }

        private void StartTesting()
        {
            var ramTileTableAddress = _symbolService.SymbolTable.GetAddressFromLabel("ram_tile_table");
            if (!ramTileTableAddress.HasValue)
            {
                return;
            }

            const string tilesetTable = "_TILESET_TABLE";
            ushort tileTableAddress = _debuggerService.CurrentDebugger.ReadShort((ushort) ramTileTableAddress.Value);

            string tileTableLabel = _symbolService.SymbolTable.GetLabelsFromAddress(tileTableAddress)
                .SingleOrDefault(s => s.EndsWith(tilesetTable));
            if (tileTableLabel == null)
            {
                return;
            }

            tileTableLabel = tileTableLabel.Remove(tileTableLabel.IndexOf(tilesetTable, StringComparison.Ordinal));
            if (_editor.Model.Scenario.ScenarioName != tileTableLabel)
            {
                return;
            }

            //_debuggerService.CurrentDebugger.NativeDebugger
            this.Invoke(() => { _editor.StartTesting(); });
            _isTesting = true;
        }

        private void DebuggerService_OnDebuggingEnded(object sender, DebuggingEventArgs e)
        {
            if (_isTesting)
            {
                this.Invoke(() => { _editor.StopTesting(); });
            }

            _isTesting = false;
            e.Debugger.DebuggerRunningChanged -= Debugger_DebuggerRunningChanged;
            _debuggerService.CurrentDebugger.NativeDebugger.Breakpoints.Remove(_mapLoadBreak);
        }

        private void Debugger_DebuggerRunningChanged(object sender, DebuggerRunningEventArgs e)
        {
            if (_mapLoadBreak == null)
            {
                return;
            }

            if (_debuggerService.CurrentDebugger.CPU.PC != _mapLoadBreak.Address.Address)
            {
                return;
            }
        }

        public override void Copy()
        {
            
        }

        public override void Cut()
        {
            
        }

        public override void Paste()
        {
            
        }

        public override void Undo()
        {
            UndoManager.Undo(_editor.Model);
        }

        public override void Redo()
        {
            UndoManager.Redo(_editor.Model);
        }

        public override void SelectAll()
        {
            
        }

        protected override void ReloadFile()
        {
            OpenFile(FileName);
        }

        public override void OpenFile(FilePath fileName)
        {
            base.OpenFile(fileName);
            _editor.OpenScenario(fileName);
        }

        protected override void SaveFileInner()
        {
            _editor.SaveScenario(FileName);
        }

        public AppModel AppModel
        {
            get { return _editor.Model; }
        }

        public FrameworkElement Child
        {
            get { return _editor; }
        }
    }
}
