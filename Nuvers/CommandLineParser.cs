using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using NuGet.Packaging;

namespace Nuvers
{
    public class CommandLineParser
    {
        private readonly ICommandManager _commandManager;

        public CommandLineParser(ICommandManager commandManager)
        {
            _commandManager = commandManager;
        }
        //
        public void ExtractOptions(ICommand command, IEnumerator<string> argsEnumerator)
        {
            List<string> arguments = new List<string>();
            IDictionary<OptionAttribute, PropertyInfo> properties = _commandManager.GetCommandOptions(command);

            while (true)
            {
                string option = GetNextCommandLineItem(argsEnumerator);

                if (option == null)
                    break;

                if (!option.StartsWith("-", StringComparison.OrdinalIgnoreCase))
                {
                    arguments.Add(option);
                    continue;
                }

                string optionText = option.Substring(1);
                string value = null;

                if (optionText.EndsWith("-", StringComparison.OrdinalIgnoreCase))
                {
                    optionText = optionText.TrimEnd('-');
                    value = "false";
                }

                KeyValuePair<OptionAttribute, PropertyInfo> result = GetPartialOptionMatch(
                    properties, 
                    prop => prop.Value.Name, 
                    prop => prop.Key.AltName,
                    option, optionText);

                PropertyInfo propInfo = result.Value;

                if (propInfo.PropertyType == typeof(bool))
                {
                    value = value ?? "true";
                }
                else
                {
                    value = GetNextCommandLineItem(argsEnumerator);
                }

                if (value == null)
                {
                    throw new CommandLineException(
                        LocalizedResourceManager.GetString("MissingOptionValueError"), option);
                }

                AssignValue(command, propInfo, option, value);
            }

            command.Arguments.AddRange(arguments);
        }

        private void AssignValue(ICommand command, PropertyInfo propInfo, string option, object value)
        {
            try
            {
                if (TypeHelper.IsMultiValuedProperty(propInfo))
                {
                    var stringValue = value as string;

                    dynamic list = propInfo.GetValue(command, null);

                    foreach (var item in stringValue.Split(new[] {';'}, StringSplitOptions.RemoveEmptyEntries))
                    {
                        if (TypeHelper.IsKeyValueProperty(propInfo))
                        {
                            int eqINdex = item.IndexOf("=", StringComparison.OrdinalIgnoreCase);

                            if (eqINdex > -1)
                            {
                                string propertyKey = item.Substring(0, eqINdex);
                                string propertyValue = item.Substring(eqINdex + 1);
                                list.Add(propertyKey, propertyValue);
                            }
                            else
                            {
                                list.Add(item);
                            }
                        }
                    }
                }
                else if (TypeHelper.IsEnumProperty(propInfo))
                {
                    var enumValue = Enum.GetValues(propInfo.PropertyType).Cast<object>();
                    value = GetPartialOptionMatch(
                        enumValue,
                        e => e.ToString(),
                        e => e.ToString(),
                        option,
                        value.ToString());
                }
                else
                {
                    propInfo.SetValue(command, TypeHelper.ChangeType(value, propInfo.PropertyType), index: null);
                }
            }
            catch (CommandLineException)
            {
                throw;
            }
            catch
            {
                throw new CommandLineException(LocalizedResourceManager.GetString("InvalidOptionValueError"), option, value);
            }
        }

        public ICommand ParseCommandLine(IEnumerable<string> commandLineArgs)
        {
            IEnumerator<string> argsEnumerator = commandLineArgs.GetEnumerator();
            // Get the desired command name
            string cmdName = GetNextCommandLineItem(argsEnumerator);
            if (cmdName == null)
            {
                return null;
            }
            // Get the command based on the name
            ICommand cmd = _commandManager.GetCommand(cmdName);
            if (cmd == null)
            {
                throw new CommandLineException(LocalizedResourceManager.GetString("UnknowCommandError"), cmdName);
            }
            ExtractOptions(cmd, argsEnumerator);
            return cmd;
        }

        private static TVal GetPartialOptionMatch<TVal>(IEnumerable<TVal> source, Func<TVal, string> getDisplayName,
            Func<TVal, string> getAltName, string option, string value)
        {
            IEnumerable<TVal> results = source
                .Where(item =>
                    getDisplayName(item).StartsWith(value, StringComparison.OrdinalIgnoreCase)
                    || (getAltName(item) ?? string.Empty).StartsWith(value, StringComparison.OrdinalIgnoreCase))
                .ToList();

            TVal result = results.FirstOrDefault();

            if (!results.Any())
            {
                throw new CommandLineException(LocalizedResourceManager.GetString("UnknownOptionError"), option);
            }

            if (!results.Skip(1).Any())
                return result;

            try
            {
                // When multiple results are found, if there's an exact match, return it.
                result = results.First(val => 
                    value.Equals(getDisplayName(val), StringComparison.OrdinalIgnoreCase) 
                    || value.Equals(getAltName(val), StringComparison.OrdinalIgnoreCase));
            }
            catch (InvalidOperationException)
            {
                throw new CommandLineException(
                    string.Format(CultureInfo.CurrentCulture, LocalizedResourceManager.GetString("AmbiguousOption"), 
                    value,
                    string.Join(" ", results.Select(getDisplayName))));
            }

            return result;
        }

        private string GetNextCommandLineItem(IEnumerator<string> argsEnumerator)
        {
            if (argsEnumerator == null || !argsEnumerator.MoveNext())
            {
                return null;
            }
            return argsEnumerator.Current;
        }
    }
}