using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Globalization;
using System.Linq;
using System.Reflection;

namespace Nuvers
{
    [Export(typeof(ICommandManager))]
    public class CommandManager : ICommandManager
    {
        private readonly IList<ICommand> _commands = new List<ICommand>();

        public IEnumerable<ICommand> GetCommands()
        {
            return _commands;
        }

        public ICommand GetCommand(string commandName)
        {
            IEnumerable<ICommand> results =
                _commands.Where(command => 
                    command.CommandAttribute.CommandName.StartsWith(commandName, StringComparison.OrdinalIgnoreCase) 
                    || (command.CommandAttribute.AltName ?? String.Empty).StartsWith(commandName, StringComparison.OrdinalIgnoreCase));

            IEnumerable<ICommand> commands = results as IList<ICommand> ?? results.ToList();

            if (!commands.Any())
            {
                throw new CommandLineException(LocalizedResourceManager.GetString("UnknowCommandError"), commandName);
            }

            ICommand matchedCommand = commands.First();

            if (commands.Skip(1).Any())
            {
                // Were there more than one results found?
                matchedCommand = commands.FirstOrDefault(command => 
                    command.CommandAttribute.CommandName.Equals(commandName, StringComparison.OrdinalIgnoreCase)
                    || commandName.Equals(command.CommandAttribute.AltName, StringComparison.OrdinalIgnoreCase));

                if (matchedCommand == null)
                {
                    // No exact match was found and the result returned multiple prefixes.
                    throw new CommandLineException(String.Format(CultureInfo.CurrentCulture, LocalizedResourceManager.GetString("AmbiguousCommand"), commandName,
                        String.Join(" ", commands.Select(c => c.CommandAttribute.CommandName))));
                }
            }

            return matchedCommand;
        }

        public IDictionary<OptionAttribute, PropertyInfo> GetCommandOptions(ICommand command)
        {
            var commandOptions = new Dictionary<OptionAttribute, PropertyInfo>();

            foreach (PropertyInfo propertyInfo in command.GetType().GetProperties())
            {
                if (!command.IncludedInHelp(propertyInfo.Name))
                    continue;

                foreach (OptionAttribute attribute in propertyInfo.GetCustomAttributes(typeof(OptionAttribute), inherit: true))
                {
                    if (!propertyInfo.CanWrite && !TypeHelper.IsMultiValuedProperty(propertyInfo))
                    {
                        // If the property has neither a setter nor is of a type that can be cast to ICollection<> then there's no way to assign 
                        // values to it. In this case throw.
                        throw new InvalidOperationException(String.Format(CultureInfo.CurrentCulture,
                            LocalizedResourceManager.GetString("OptionInvalidWithoutSetter"), command.GetType().FullName + "." + propertyInfo.Name));
                    }
                    commandOptions.Add(attribute, propertyInfo);
                }
            }

            return commandOptions;
        }

        public void RegisterCommand(ICommand command)
        {
            var attribute = command.CommandAttribute;

            if (attribute != null)
                _commands.Add(command);
        }


    }
}

