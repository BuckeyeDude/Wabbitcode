﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Revsoft.Wabbitcode.Exceptions;
using Revsoft.Wabbitcode.Services.Interfaces;
using Revsoft.Wabbitcode.Services.Symbols;
using Revsoft.Wabbitcode.Utils;
using WabbitemuLib;
using System.Diagnostics;

namespace Revsoft.Wabbitcode.Services.Debugger
{
    public sealed class WabbitcodeDebugger : IWabbitcodeDebugger
    {
        #region Private Fields

        private byte _appPage;
        private ushort _oldSp;
        private IWabbitemuDebugger _debugger;
        private bool _disposed;
        private bool _isStepping;
        private readonly List<KeyValuePair<ushort, ushort>> _memoryAllocations;

        private IBreakpoint _stepOverBreakpoint;
        private IBreakpoint _stepOutBreakpoint;
        private IBreakpoint _jforceBreakpoint;
        private IBreakpoint _ramClearBreakpoint;
        private IBreakpoint _insertMemBreakpoint;
        private IBreakpoint _delMemBreakpoint;

        private readonly ISymbolService _symbolService;
        private readonly IFileService _fileService;
        private readonly IDebuggerService _debuggerService;

        #endregion

        #region Constants

        private const ushort RamCode = 0x8100;
        private const ushort TopStackApp = 0xFFDF;
        private const ushort MachineStackBottom = 0xFE66;

        #endregion

        #region Public Properties

        private string CurrentDebuggingFile { get; set; }

        public IZ80 CPU
        {
            get { return _debugger.CPU; }
        }

        public Image ScreenImage
        {
            get { return _debugger.GetScreenImage(); }
        }

        private bool IsAnApp { get; set; }

        public bool IsRunning
        {
            get { return _debugger != null && _debugger.Running; }
        }

        public Stack<StackEntry> MachineStack { get; private set; }

        public Stack<CallStackEntry> CallStack { get; private set; }

        #endregion

        #region Events

        public event DebuggerRunning DebuggerRunningChanged;
        public event DebuggerStep DebuggerStep;

        #endregion

        public WabbitcodeDebugger(string outputFile)
        {
            _disposed = false;

            _debuggerService = DependencyFactory.Resolve<IDebuggerService>();
            _fileService = DependencyFactory.Resolve<IFileService>();
            _symbolService = DependencyFactory.Resolve<ISymbolService>();

            WabbitcodeBreakpointManager.OnBreakpointAdded += WabbitcodeBreakpointManager_OnBreakpointAdded;
            WabbitcodeBreakpointManager.OnBreakpointRemoved += WabbitcodeBreakpointManager_OnBreakpointRemoved;

            Debug.WriteLine("Creating wabbitemu debugger");
            _debugger = new WabbitemuDebugger();
            Debug.WriteLine("Loading file " + outputFile);
            _debugger.LoadFile(outputFile);
            _debugger.Visible = true;
            _debugger.OnBreakpoint += BreakpointHit;
            _debugger.OnClose += DebuggerOnClose;

            CurrentDebuggingFile = outputFile;
            IsAnApp = outputFile.EndsWith(".8xk");
            _memoryAllocations = new List<KeyValuePair<ushort, ushort>>();
            CallStack = new Stack<CallStackEntry>();
            MachineStack = new Stack<StackEntry>();
            _oldSp = IsAnApp ? TopStackApp : (ushort) 0xFFFF;
            SetupInternalBreakpoints();
        }

        #region Memory and Paging

        public byte ReadByte(ushort address)
        {
            return _debugger.Memory.ReadByte(address);
        }

        public ushort ReadShort(ushort address)
        {
            return _debugger.Memory.ReadWord(address);
        }

        public byte[] ReadMemory(ushort address, ushort count)
        {
            return (byte[]) _debugger.Memory.Read(address, count);
        }

        public ushort? GetRegisterValue(string wordHovered)
        {
            if (_debugger == null)
            {
                return null;
            }

            switch (wordHovered.Trim().ToLower())
            {
                case "a":
                    return _debugger.CPU.A;
                case "f":
                    return _debugger.CPU.F;
                case "b":
                    return _debugger.CPU.B;
                case "c":
                    return _debugger.CPU.C;
                case "d":
                    return _debugger.CPU.D;
                case "e":
                    return _debugger.CPU.E;
                case "h":
                    return _debugger.CPU.H;
                case "l":
                    return _debugger.CPU.L;
                case "ixh":
                    return _debugger.CPU.IXH;
                case "ixl":
                    return _debugger.CPU.IXL;
                case "iyh":
                    return _debugger.CPU.IYH;
                case "iyl":
                    return _debugger.CPU.IYL;
                case "af":
                    return _debugger.CPU.AF;
                case "bc":
                    return _debugger.CPU.BC;
                case "de":
                    return _debugger.CPU.DE;
                case "hl":
                    return _debugger.CPU.HL;
                case "ix":
                    return _debugger.CPU.IX;
                case "iy":
                    return _debugger.CPU.IY;
                case "sp":
                    return _debugger.CPU.SP;
                case "pc":
                    return _debugger.CPU.PC;
                default:
                    return null;
            }
        }

        private byte GetRelativePageNum(ushort address)
        {
            IPage bank = _debugger.Memory.Bank[address >> 14];
            int page = bank.Index;
            if (bank.IsFlash)
            {
                page = _appPage - page;
            }
            return (byte) page;
        }

        private byte GetAbsolutePageNum(ushort address)
        {
            IPage bank = _debugger.Memory.Bank[address >> 14];
            int page = bank.Index;
            return (byte) page;
        }

        public DocumentLocation GetAddressLocation(ushort address)
        {
            int page = GetRelativePageNum(address);
            return _symbolService.ListTable.GetFileLocation(page, address, address >= 0x8000);
        }

        #endregion

        #region Startup

        public void EndDebug()
        {
            IsAnApp = false;

            if (_debugger == null)
            {
                return;
            }

            _debugger.EndDebug();
            _debugger = null;
        }

        private void DebuggerOnClose(object sender, EventArgs eventArgs)
        {
            _debuggerService.EndDebugging();
        }

        #endregion

        #region Running

        public void Run()
        {
            if (_debugger == null)
            {
                return;
            }

            _debugger.Step();
            _debugger.Running = true;

            if (DebuggerRunningChanged != null)
            {
                DebuggerRunningChanged(this, new DebuggerRunningEventArgs(null, true));
            }
        }

        public void Pause()
        {
            _debugger.Running = false;
            var currentPc = _debugger.CPU.PC;
            var maxStep = 500;
            var key = _symbolService.ListTable.GetFileLocation(currentPc, GetRelativePageNum(currentPc), currentPc >= 0x8000);
            while (key == null && maxStep >= 0)
            {
                _debugger.Step();
                currentPc = _debugger.CPU.PC;
                key = _symbolService.ListTable.GetFileLocation(GetRelativePageNum(currentPc), currentPc, currentPc >= 0x8000);
                maxStep--;
            }

            if (maxStep < 0)
            {
                throw new DebuggingException("Unable to pause here");
            }

            UpdateStack();

            if (DebuggerRunningChanged != null)
            {
                DebuggerRunningChanged(this, new DebuggerRunningEventArgs(key, false));
            }
        }

        public void SetPCToSelect(FilePath fileName, int lineNumber)
        {
            CalcLocation value = _symbolService.ListTable.GetCalcLocation(fileName, lineNumber);
            if (value == null)
            {
                throw new Exception("Unable to set statement here!");
            }

            _debugger.CPU.PC = value.Address;
            byte page = value.Page;
            if (IsAnApp)
            {
                page = (byte) (_appPage - page);
                _debugger.Memory.Bank[1] = _debugger.Memory.Flash[page];
            }
            else
            {
                _debugger.Memory.Bank[2] = _debugger.Memory.RAM[1];
            }

            if (DebuggerStep != null)
            {
                DebuggerStep(this, new DebuggerStepEventArgs(new DocumentLocation(fileName, lineNumber)));
            }
        }

        public void StartDebug()
        {
            Debug.WriteLine("Turning calc on");
            _debugger.TurnCalcOn();

            Debug.WriteLine("Verifying app");
            var app = VerifyApp(CurrentDebuggingFile);
            // once we have the app we can add breakpoints
            var breakpoints = WabbitcodeBreakpointManager.Breakpoints.ToList();
            foreach (WabbitcodeBreakpoint breakpoint in breakpoints)
            {
                Debug.WriteLine("Setting initial break" + breakpoint.File + " " + breakpoint.LineNumber);
                SetBreakpoint(breakpoint);
            }

            if (app != null)
            {
                LaunchApp(app.Name);
            }
        }

        public void Step()
        {
            if (_isStepping)
            {
                return;
            }

            _isStepping = true;
            // need to clear the old breakpoint so lets save it
            ushort currentPc = _debugger.CPU.PC;
            byte oldPage = GetRelativePageNum(currentPc);
            DocumentLocation key = _symbolService.ListTable.GetFileLocation(oldPage, currentPc, currentPc >= 0x8000);
            DocumentLocation newKey = key;
            while (newKey == null || newKey.LineNumber == key.LineNumber)
            {
                _debugger.Step();
                newKey = _symbolService.ListTable.GetFileLocation(GetRelativePageNum(_debugger.CPU.PC), _debugger.CPU.PC, _debugger.CPU.PC >= 0x8000);
                // we are safe to check this here, because we are stepping one at a time meaning if the stack did change, it can't have changed much
                if (_oldSp != _debugger.CPU.SP)
                {
                    UpdateStack();
                }
            }

            ushort address = _debugger.CPU.PC;
            byte page = GetRelativePageNum(address);
            key = _symbolService.ListTable.GetFileLocation(page, address, address >= 0x8000);

            Task.Factory.StartNew(() => 
            {
                if (DebuggerStep != null)
                {
                    DebuggerStep(this, new DebuggerStepEventArgs(key));
                }
            });

            _isStepping = false;
        }

        public void StepOut()
        {
            if (_isStepping)
            {
                return;
            }

            _isStepping = true;
            DocumentLocation lastCallLocation = CallStack.Last().CallLocation;
            CalcLocation calcLocation = _symbolService.ListTable.GetCalcLocation(lastCallLocation.FileName, lastCallLocation.LineNumber);
            DocumentLocation docLocation = null;
            ushort address = calcLocation.Address;
            while (docLocation == null)
            {
                address++;
                docLocation = _symbolService.ListTable.GetFileLocation(calcLocation.Page, address, calcLocation.IsRam);
            }
            _stepOutBreakpoint = _debugger.SetBreakpoint(calcLocation.IsRam, (byte) (_appPage - calcLocation.Page), address);
            _debugger.OnBreakpoint += StepOutBreakpointEvent;
            _debugger.Step();
            _debugger.Running = true;
        }

        private void StepOutBreakpointEvent(object sender, BreakpointEventArgs breakpointEventArgs)
        {
            if (_debugger == null)
            {
                return;
            }

            _debugger.ClearBreakpoint(_stepOutBreakpoint);
            int page = GetRelativePageNum(_stepOutBreakpoint.Address.Address);
            DocumentLocation key = _symbolService.ListTable.GetFileLocation(page,
                _stepOutBreakpoint.Address.Address,
                !_stepOutBreakpoint.Address.Page.IsFlash);

            UpdateStack();
            _debugger.OnBreakpoint -= StepOutBreakpointEvent;

            Task.Factory.StartNew(() => 
            {
                if (DebuggerStep != null)
                {
                    DebuggerStep(this, new DebuggerStepEventArgs(key));
                }
            });

            _isStepping = false;
        }

        public void StepOver()
        {
            if (_isStepping)
            {
                return;
            }

            _isStepping = true;
            // need to clear the old breakpoint so lets save it
            ushort currentPc = _debugger.CPU.PC;
            byte oldPage = GetRelativePageNum(currentPc);
            DocumentLocation key = _symbolService.ListTable.GetFileLocation(oldPage, currentPc, currentPc >= 0x8000);

            string line = _fileService.GetLine(key.FileName, key.LineNumber);

            int commentIndex = line.IndexOf(";", StringComparison.Ordinal);
            if (commentIndex != -1)
            {
                line = line.Substring(0, commentIndex);
            }

            // if the line contains a special commmand (i.e. one that will go to who knows where)
            // we just want to step over it
            string[] specialCommands = {"jp", "jr", "ret", "djnz"};
            if (specialCommands.Any(s => line.Contains(s)))
            {
                _isStepping = false;
                Step();
                return;
            }

            do
            {
                currentPc++;
                oldPage = GetRelativePageNum(currentPc);
                key = _symbolService.ListTable.GetFileLocation(oldPage, currentPc, currentPc >= 0x8000);
            } while (key == null);

            _stepOverBreakpoint = _debugger.SetBreakpoint(currentPc >= 0x8000, GetAbsolutePageNum(currentPc), currentPc);
            _debugger.OnBreakpoint += StepOverBreakpointEvent;

            _debugger.Step();
            _debugger.Running = true;
        }

        private void StepOverBreakpointEvent(object sender, EventArgs e)
        {
            if (_debugger == null)
            {
                return;
            }

            _debugger.ClearBreakpoint(_stepOverBreakpoint);
            int page = _stepOverBreakpoint.Address.Page.IsFlash
                ? _appPage - _stepOverBreakpoint.Address.Page.Index
                : _stepOverBreakpoint.Address.Page.Index;
            DocumentLocation key = _symbolService.ListTable.GetFileLocation(page,
                _stepOverBreakpoint.Address.Address,
                !_stepOverBreakpoint.Address.Page.IsFlash);

            UpdateStack();
            _debugger.OnBreakpoint -= StepOverBreakpointEvent;

            Task.Factory.StartNew(() => 
            {
                if (DebuggerStep != null)
                {
                    DebuggerStep(this, new DebuggerStepEventArgs(key));
                }
            });

            _isStepping = false;
        }

        #endregion

        #region MachineStack

        private void UpdateCallstack()
        {
            CallStack = new Stack<CallStackEntry>(
                MachineStack.Where(s => s.CallStackEntry != null)
                    .Select(s => s.CallStackEntry).Reverse());
        }

        private CallStackEntry CheckValidCall(int currentSp)
        {
            var callerAddress = ReadShort((ushort)currentSp);
            var calleePage = GetRelativePageNum(_debugger.CPU.PC);
            CallerInformation info = _symbolService.ListTable.CheckValidCall(callerAddress, callerAddress >= 0x8000, calleePage);
            if (info == null)
            {
                return null;
            }

            return new CallStackEntry(info, info.DocumentLocation);
        }

        private void UpdateStack()
        {
            var oldStackList = MachineStack.Reverse().ToList();
            MachineStack.Clear();
            int currentSp = _debugger.CPU.SP;
            ushort topStack = IsAnApp ? TopStackApp : (ushort) 0xFFFF;
            // avoid generating a massive callstack
            if (currentSp < MachineStackBottom)
            {
                int maxStackSize = topStack - MachineStackBottom;
                topStack = (ushort) (currentSp + maxStackSize);
            }

            if ((currentSp < _oldSp) || (currentSp < topStack && oldStackList.Count == 0))
            {
                // new stack entries to add
                while (currentSp != _oldSp && currentSp <= topStack)
                {
                    CallStackEntry callStackEntry = CheckValidCall(currentSp);
                    MachineStack.Push(new StackEntry((ushort) currentSp, ReadShort((ushort) currentSp), callStackEntry));
                    currentSp += 2;
                }
            }
            else if (currentSp > _oldSp)
            {
                // stack entries to remove
                oldStackList.RemoveAll(s => s.Address < currentSp);
            }

            foreach (StackEntry stackEntry in oldStackList)
            {
                int data = ReadShort((ushort) currentSp);
                if (stackEntry.Data != data)
                {
                    CallStackEntry callStackEntry = CheckValidCall(currentSp);
                    MachineStack.Push(new StackEntry((ushort) currentSp, (ushort) data, callStackEntry));
                }
                else
                {
                    MachineStack.Push(stackEntry);
                }
                currentSp += 2;
            }
            _oldSp = _debugger.CPU.SP;

            UpdateCallstack();
        }

        #endregion

        #region Breakpoints

        public bool SetBreakpoint(WabbitcodeBreakpoint newBreakpoint)
        {
            if (_debugger == null || newBreakpoint == null)
            {
                return false;
            }

            CalcLocation location = _symbolService.ListTable.GetCalcLocation(newBreakpoint.File, newBreakpoint.LineNumber + 1);
            if (location == null)
            {
                // move the breakpoint to the nearest location
                FilePath fileName = newBreakpoint.File;
                int lineNumber = newBreakpoint.LineNumber;
                CalcLocation value = _symbolService.ListTable.GetNextNearestCalcLocation(fileName, lineNumber + 1);
                if (value == null)
                {
                    return false;
                }

                DocumentLocation newLocation = _symbolService.ListTable.GetFileLocation(value.Page, value.Address, value.IsRam);
                WabbitcodeBreakpointManager.RemoveBreakpoint(fileName, lineNumber);
                WabbitcodeBreakpointManager.AddBreakpoint(newLocation.FileName, newLocation.LineNumber - 1);
                return true;
            }

            newBreakpoint.Page = location.Page;
            newBreakpoint.Address = location.Address;
            newBreakpoint.IsRam = location.IsRam;
            byte page = location.IsRam ? location.Page : (byte) (_appPage - newBreakpoint.Page);
            newBreakpoint.WabbitemuBreakpoint = _debugger.SetBreakpoint(newBreakpoint.IsRam,
                page, newBreakpoint.Address);
            return false;
        }

        public void ClearBreakpoint(WabbitcodeBreakpoint newBreakpoint)
        {
            if (_debugger != null && newBreakpoint != null && newBreakpoint.WabbitemuBreakpoint != null)
            {
                _debugger.ClearBreakpoint(newBreakpoint.WabbitemuBreakpoint);
            }
        }

        private void BreakpointHit(object sender, BreakpointEventArgs e)
        {
            IBreakpoint breakEvent = e.Breakpoint;
            Debug.WriteLine("Hit breakpoint " + breakEvent.Address.Address);
            if ((breakEvent.Address == _jforceBreakpoint.Address && breakEvent.Address.Page == _jforceBreakpoint.Address.Page) ||
                (breakEvent.Address == _ramClearBreakpoint.Address && breakEvent.Address.Page == _ramClearBreakpoint.Address.Page))
            {
                DebuggerOnClose(null, EventArgs.Empty);
                return;
            }

            if (breakEvent.Address == _insertMemBreakpoint.Address && breakEvent.Address.Page == _insertMemBreakpoint.Address.Page)
            {
                _memoryAllocations.Add(new KeyValuePair<ushort, ushort>(_debugger.CPU.DE, _debugger.CPU.HL));
                _debugger.Step();
                _debugger.Running = true;
                return;
            }

            if (breakEvent.Address == _delMemBreakpoint.Address && breakEvent.Address.Page == _delMemBreakpoint.Address.Page)
            {
                _memoryAllocations.RemoveAll(kvp => kvp.Key == _debugger.CPU.HL && kvp.Value == _debugger.CPU.DE);
                _debugger.Step();
                _debugger.Running = true;
                return;
            }

            ushort address = breakEvent.Address.Address;
            int relativePage = GetRelativePageNum(address);
            WabbitcodeBreakpoint breakpoint = WabbitcodeBreakpointManager.Breakpoints.FirstOrDefault(
                b => b.Address == address && b.Page == (byte) relativePage && b.IsRam == address >= 0x8000);
            if (breakpoint == null)
            {
                return;
            }

            breakpoint.NumberOfTimesHit++;
            bool conditionsTrue = breakpoint.Enabled;
            switch (breakpoint.HitCountCondition)
            {
                case HitCountEnum.BreakEqualTo:
                    if (breakpoint.NumberOfTimesHit != breakpoint.HitCountConditionNumber)
                    {
                        conditionsTrue = false;
                    }
                    break;
                case HitCountEnum.BreakGreaterThanEqualTo:
                    if (breakpoint.NumberOfTimesHit < breakpoint.HitCountConditionNumber)
                    {
                        conditionsTrue = false;
                    }
                    break;
                case HitCountEnum.BreakMultipleOf:
                    if (breakpoint.NumberOfTimesHit % breakpoint.HitCountConditionNumber != 0)
                    {
                        conditionsTrue = false;
                    }
                    break;
            }

            if (conditionsTrue && breakpoint.EvalulateAllConditions(_debugger.CPU))
            {
                DocumentLocation key = _symbolService.ListTable.GetFileLocation(relativePage, address, !breakEvent.Address.Page.IsFlash);
                if (key == null)
                {
                    throw new InvalidOperationException("Unable to find breakpoint");
                }

                UpdateStack();

                if (DebuggerRunningChanged != null)
                {
                    DebuggerRunningChanged(this, new DebuggerRunningEventArgs(key, false));
                }
            }
            else
            {
                _debugger.Running = true;
            }
        }

        private void SetupInternalBreakpoints()
        {
            if (_debugger == null)
            {
                return;
            }

            // this is the start _JForceCmdNoChar
            const ushort jforceCmdNoChar = 0x4027;
            const ushort insertMem = 0x42F7;
            const ushort delMem = 0x4357;
            CalcLocation location = LookupBcallAddress(jforceCmdNoChar);
            _jforceBreakpoint = _debugger.SetBreakpoint(location.IsRam, location.Page, location.Address);
            // most likely location that a crash will end up
            _ramClearBreakpoint = _debugger.SetBreakpoint(false, 0, 0x0000);
            // for restarts we want to manually delmem
            location = LookupBcallAddress(insertMem);
            _insertMemBreakpoint = _debugger.SetBreakpoint(location.IsRam, location.Page, location.Address);
            // we need to track any memory freed as well
            location = LookupBcallAddress(delMem);
            _delMemBreakpoint = _debugger.SetBreakpoint(location.IsRam, location.Page, location.Address);
        }

        private void WabbitcodeBreakpointManager_OnBreakpointRemoved(object sender, WabbitcodeBreakpointEventArgs e)
        {
            ClearBreakpoint(e.Breakpoint);
        }

        private void WabbitcodeBreakpointManager_OnBreakpointAdded(object sender, WabbitcodeBreakpointEventArgs e)
        {
            e.Cancel = SetBreakpoint(e.Breakpoint);
        }

        #endregion

        #region TIOS Specifics

        /// <summary>
        /// Returns the location of the actual code a bcall will execute.
        /// This is what TIOS does when you do a bcall.
        /// </summary>
        /// <param name="bcallAddress">The address of the bcall to map</param>
        /// <returns>The address the bcall code is located at</returns>
        private CalcLocation LookupBcallAddress(int bcallAddress)
        {
            int page;
            if ((bcallAddress & (1 << 15)) != 0)
            {
                bcallAddress &= ~(1 << 15);
                switch (_debugger.Model)
                {
                    case CalcModel.TI_73:
                    case CalcModel.TI_83P:
                        page = 0x1F;
                        break;
                    case CalcModel.TI_83PSE:
                    case CalcModel.TI_84PSE:
                        page = 0x7F;
                        break;
                    case CalcModel.TI_84P:
                        page = 0x3F;
                        break;
                    default:
                        throw new InvalidOperationException("Invalid model");
                }
            }
            else if ((bcallAddress & (1 << 14)) != 0)
            {
                bcallAddress &= ~(1 << 14);
                switch (_debugger.Model)
                {
                    case CalcModel.TI_73:
                    case CalcModel.TI_83P:
                        page = 0x1B;
                        break;
                    case CalcModel.TI_83PSE:
                    case CalcModel.TI_84PSE:
                        page = 0x7B;
                        break;
                    case CalcModel.TI_84P:
                        page = 0x3B;
                        break;
                    default:
                        throw new InvalidOperationException("Invalid model");
                }
            }
            else
            {
                throw new InvalidOperationException("Tried looking up a local bcall");
            }

            bcallAddress += 0x4000;
            ushort realAddress = _debugger.Memory.Flash[page].ReadWord((ushort) bcallAddress);
            byte realPage = _debugger.Memory.Flash[page].ReadByte((ushort) (bcallAddress + 2));
            return new CalcLocation(realAddress, realPage, false);
        }

        private ITIApplication VerifyApp(string createdName)
        {
            if (_debugger == null || _debugger.Apps.Count == 0)
            {
                throw new DebuggingException("Application not found on calculator");
            }

            char[] buffer = new char[8];
            using (StreamReader appReader = new StreamReader(createdName))
            {
                for (int i = 0; i < 17; i++)
                {
                    appReader.Read();
                }

                appReader.ReadBlock(buffer, 0, 8);
            }

            string appName = new string(buffer);
            ITIApplication app = _debugger.Apps.Cast<ITIApplication>().SingleOrDefault(a => a.Name == appName);
            if (app == null || string.IsNullOrEmpty(app.Name))
            {
                throw new DebuggingException("Application not found on calculator");
            }

            _appPage = (byte) app.Page.Index;
            return app;
        }

        private void LaunchApp(string createdName)
        {
            Debug.WriteLine("Lauching app");
            const ushort progToEdit = 0x84BF;
            // this is code to do
            // bcall(_CloseEditBuf)
            // bcall(_ExecuteApp)
            byte[] launchAppCode = { 0xEF, 0xD3, 0x48, 0xEF, 0x51, 0x4C };
            byte[] createdNameBytes = Encoding.ASCII.GetBytes(createdName);
            // _ExecuteApp expects the name of the app to launch in progToEdit
            _debugger.Running = false;
            _debugger.Write(true, 1, progToEdit, createdNameBytes);
            _debugger.Write(true, 1, RamCode, launchAppCode);
            _debugger.CPU.Halt = false;
            _debugger.CPU.PC = RamCode;
            _debugger.Running = true;
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            Dispose(true);
        }

        private void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    if (_debugger != null)
                    {
                        _debugger.Dispose();
                    }
                }
            }
            _disposed = true;
        }

        #endregion
    }
}