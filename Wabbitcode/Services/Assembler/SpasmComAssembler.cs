using System.Runtime.InteropServices;

namespace Revsoft.Wabbitcode.Services.Assembler
{
    internal class SpasmComAssembler : IAssembler
    {
        private SPASM.Z80Assembler _spasm;

        public SpasmComAssembler()
        {
            _spasm = new SPASM.Z80Assembler();
        }

        public void AddDefine(string name, string value)
        {
            _spasm.Defines.Add(name, value);
        }

        public void AddIncludeDir(string path)
        {
            _spasm.IncludeDirectories.Add(path);
        }

        public string Assemble(AssemblyFlags flags)
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

        public string Assemble(string code, AssemblyFlags flags)
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

        public void ClearDefines()
        {
             _spasm.Defines.RemoveAll();
        }

        public void ClearIncludeDirs()
        {
            _spasm.IncludeDirectories.Clear();
        }

        public void SetCaseSensitive(bool caseSensitive)
        {
            _spasm.CaseSensitive = caseSensitive;
        }

        public void SetInputFile(string file)
        {
            _spasm.InputFile = file;
        }

        public void SetOutputFile(string file)
        {
            _spasm.OutputFile = file;
        }

        public void SetWorkingDirectory(string file)
        {
            _spasm.CurrentDirectory = file;
        }

        public void Dispose()
        {
            _spasm = null;
        }
    }
}