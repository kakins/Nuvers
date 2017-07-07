using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.Reflection;
using NuGet;

namespace Nuvers
{
    public class Program
    {
        private static string _projectPath = "C:\\ESI\\EsiServices\\Services_DEV_WD\\EsiServices.WebApi.Core";

        private static readonly string ThisExecutableName = typeof(Program).Assembly.GetName().Name;

        [ImportMany]
        public IEnumerable<ICommand> Commands { get; set; }

        [Import]
        public ICommandManager Manager { get; set; }

        [Import]
        public SomeCommand SomeCommand { get; set; }

        public static int Main(string[] args)
        {
            // ReadInput
#if !DEBUG
            // todo: replace with config settings?
            _projectPath = Directory.GetCurrentDirectory();
#endif

            return MainCore(_projectPath, args);
        }

        public static int MainCore(string workingDirectory, string[] args)
        {
            var fileSystem = new PhysicalFileSystem(workingDirectory);
            var console = new MyConsole();

            try
            {
                ProcessCommand(fileSystem, console, args, workingDirectory);
            }
            catch (Exception exception)
            {
                console.LogError(exception.Message);
                return 1;
            }

            return 0;
        }

        private static void ProcessCommand(IFileSystem fileSystem, IConsole console, string[] args, string workingDirectory)
        {
            ICommand command = GetCommand(fileSystem, console, args, workingDirectory);

            if (!ArgumentCountValid(command))
            {
                string commandName = command.CommandAttribute.CommandName;

                console.WriteLine(LocalizedResourceManager.GetString("InvalidArguments"), commandName);
            }
            else
            {
                SetConsoleInteractivity(console, command as Command);
                command.Execute();
            }
        }

        private static ICommand GetCommand(IFileSystem fileSystem, IConsole console, string[] args, string workingDirectory)
        {
            var program = new Program();

            program.Initialize(fileSystem, console);

            foreach (ICommand commandToRegister in program.Commands)
            {
                program.Manager.RegisterCommand(commandToRegister);
            }

            CommandLineParser parser = new CommandLineParser(program.Manager);

            var command = parser.ParseCommandLine(args) ?? program.SomeCommand;
            command.CurrentDirectory = command.CurrentDirectory = workingDirectory;
            return command;
        }

        private static void SetConsoleInteractivity(IConsole console, Command command)
        {
            // When running from inside VS, no input is available to our executable locking up VS.
            // VS sets up a couple of environment variables one of which is named VisualStudioVersion.
            // Every time this is setup, we will just fail.
            // TODO: Remove this in next iteration. This is meant for short-term backwards compat.
            //string vsSwitch = Environment.GetEnvironmentVariable("VisualStudioVersion");

            console.IsNonInteractive = command != null && command.NonInteractive;

            if (command != null)
            {
                console.Verbosity = command.Verbosity;
            }
        }

        public static bool ArgumentCountValid(ICommand command)
        {
            CommandAttribute attribute = command.CommandAttribute;
            return command.Arguments.Count >= attribute.MinArgs &&
                   command.Arguments.Count <= attribute.MaxArgs;
        }

        private void Initialize(IFileSystem fileSystem, IConsole console)
        {
            AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;

            using (var catalog = new AggregateCatalog(new AssemblyCatalog(GetType().Assembly)))
            {
                // not sure if I need this, or what it does..
                //if (!IgnoreExtensions)
                //{
                //    AddExtensionsToCatalog(catalog, console);
                //}

                try
                {
                    using (var container = new CompositionContainer(catalog))
                    {
                        container.ComposeExportedValue(console);
                        //container.ComposeExportedValue(fileSystem);
                        container.ComposeParts(this);
                    }
                }
                catch (ReflectionTypeLoadException ex) when (ex?.LoaderExceptions.Length > 0)
                {
                    throw new AggregateException(ex.LoaderExceptions);
                }
            }
        }

        private Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args) => 
            string.Equals(new AssemblyName(args.Name).Name, ThisExecutableName, StringComparison.OrdinalIgnoreCase) ? 
            typeof(Program).Assembly 
            : null;
    }

}

