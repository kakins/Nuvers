using System;

namespace Nuvers
{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public sealed class OptionAttribute : Attribute
    {
        private string _description;

        public string AltName { get; set; }
        public string DescriptionResourceName { get; }

        public string Description
        {
            get
            {
                if (ResourceType != null && !String.IsNullOrEmpty(DescriptionResourceName))
                {
                    return DescriptionResourceName;
                }
                return _description;

            }
            private set => _description = value;
        }

        public Type ResourceType { get; }

        public OptionAttribute(string description)
        {
            Description = description;
        }

        public OptionAttribute(Type resourceType, string descriptionResourceName)
        {
            ResourceType = resourceType;
            DescriptionResourceName = descriptionResourceName;
        }
    }
}