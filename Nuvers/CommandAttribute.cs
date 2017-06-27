using System;

namespace Nuvers
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    public sealed class CommandAttribute : Attribute
    {
        private string _description;
        private string _usageSummary;
        private string _usageDescription;
        private string _example;

        public string CommandName { get; private set; }
        public Type ResourceType { get; private set; }
        public string DescriptionResourceName { get; private set; }


        public string AltName { get; set; }
        public int MinArgs { get; set; }
        public int MaxArgs { get; set; }
        public string UsageSummaryResourceName { get; set; }
        public string UsageDescriptionResourceName { get; set; }
        public string UsageExampleResourceName { get; set; }

        public string Description
        {
            get => ResourceType != null && !String.IsNullOrEmpty(DescriptionResourceName)
                ? ResourceHelper.GetLocalizedString(ResourceType, DescriptionResourceName)
                : _description;
            private set => _description = value;
        }

        public string UsageSummary
        {
            get => ResourceType != null && !String.IsNullOrEmpty(UsageSummaryResourceName)
                ? ResourceHelper.GetLocalizedString(ResourceType, UsageSummaryResourceName)
                : _usageSummary;
            set => _usageSummary = value;
        }

        public string UsageDescription
        {
            get => ResourceType != null && !String.IsNullOrEmpty(UsageDescriptionResourceName)
                ? ResourceHelper.GetLocalizedString(ResourceType, UsageDescriptionResourceName)
                : _usageDescription;
            set => _usageDescription = value;
        }

        public string UsageExample
        {
            get => ResourceType != null && !String.IsNullOrEmpty(UsageExampleResourceName)
                ? ResourceHelper.GetLocalizedString(ResourceType, UsageExampleResourceName)
                : _example;
            set => _example = value;
        }

        public CommandAttribute(string commandName, string description)
        {
            CommandName = commandName;
            Description = description;
            MinArgs = 0;
            MaxArgs = Int32.MaxValue;
        }

        public CommandAttribute(Type resourceType, string commandName, string descriptionResourceName)
        {
            ResourceType = resourceType;
            CommandName = commandName;
            DescriptionResourceName = descriptionResourceName;
            MinArgs = 0;
            MaxArgs = Int32.MaxValue;
        }
    }
}