namespace Revsoft.Wabbitcode.Services.Assembler
{
    public interface IAssemblerFactory
    {
        IAssembler CreateAssembler();
    }

    public class AssemblerFactory : IAssemblerFactory
    {
        public IAssembler CreateAssembler()
        {
            lock (this)
            {
                return new SpasmExeAssembler();
            }
        }
    }
}