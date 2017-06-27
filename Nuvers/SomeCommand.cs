using System.ComponentModel.Composition;
using System.Reflection;
using NuGet.Versioning;

namespace Nuvers
{
    [Export(typeof(SomeCommand))]
    [Command(typeof(Resources), "somecmd", "SomeCommandDescription")]
    public class SomeCommand : Command
    {
        private readonly CommandManager _commandManager;
        private readonly string _commandExe;

        [Option(typeof(Resources), "MyOptionDescription")]
        public string MyOption { get; set; }

        public SomeCommand()
        {
            //_commandManager = commandManager;
            _commandExe = Assembly.GetExecutingAssembly().GetName().Name;
        }
        public override void ExecuteCommand()
        {
            Console.WriteLine("usage: {0} <command> [args] [options] ", _commandExe);
            if (!string.IsNullOrEmpty(MyOption))
                Console.WriteLine($"Your option: {MyOption}");
        }
    }
}