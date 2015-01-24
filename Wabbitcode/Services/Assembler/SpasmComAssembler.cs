﻿using System.Runtime.InteropServices;

namespace Revsoft.Wabbitcode.Services.Assembler
{
    internal class SpasmComAssembler : IAssembler
    {
        private static SPASM.Z80Assembler _spasm;

        public SpasmComAssembler()
        {
            if (_spasm == null)
            {
                _spasm = new SPASM.Z80Assembler();
            }
        }

        public void AddDefine(string name, string value)
        {
            lock (_spasm)
            {
                _spasm.Defines.Add(name, value);
            }
        }

        public void AddIncludeDir(string path)
        {
            lock (_spasm)
            {
                _spasm.IncludeDirectories.Add(path);
            }
        }

        public string Assemble(AssemblyFlags flags)
        {
            lock (_spasm)
            {
                try
                {
                    _spasm.Options = (uint) flags;
                    _spasm.Assemble();
                    return _spasm.StdOut.ReadAll();
                }
                catch (COMException)
                {
                    return "Error assembling.";
                }
            }
        }

        public string Assemble(string code, AssemblyFlags flags)
        {
            lock (_spasm)
            {
                try
                {
                    _spasm.Options = (uint) flags;
                    _spasm.Assemble(code);
                    return _spasm.StdOut.ReadAll();
                }
                catch (COMException)
                {
                    return string.Empty;
                }
            }
        }

        public void ClearDefines()
        {
            lock (_spasm)
            {
                _spasm.Defines.RemoveAll();
            }
        }

        public void ClearIncludeDirs()
        {
            lock (_spasm)
            {
                _spasm.IncludeDirectories.Clear();
            }
        }

        public void SetCaseSensitive(bool caseSensitive)
        {
            lock (_spasm)
            {
                _spasm.CaseSensitive = caseSensitive;
            }
        }

        public void SetInputFile(string file)
        {
            lock (_spasm)
            {
                _spasm.InputFile = file;
            }
        }

        public void SetOutputFile(string file)
        {
            lock (_spasm)
            {
                _spasm.OutputFile = file;
            }
        }

        public void SetWorkingDirectory(string file)
        {
            lock (_spasm)
            {
                _spasm.CurrentDirectory = file;
            }
        }

        public void Dispose()
        {
            _spasm = null;
        }
    }
}