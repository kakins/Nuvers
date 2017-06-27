using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Reflection;

namespace Nuvers
{
    internal static class TypeHelper
    {
        public static Type RemoveNullableFromType(Type type)
        {
            return Nullable.GetUnderlyingType(type) ?? type;
        }

        public static object ChangeType(object value, Type type)
        {
            if (type == null)
            {
                throw new ArgumentNullException("type");
            }

            if (value == null)
            {
                return TypeAllowsNull(type) ? null : Convert.ChangeType(value, type, CultureInfo.CurrentCulture);
            }

            type = RemoveNullableFromType(type);

            if (value.GetType() == type)
            {
                return value;
            }

            TypeConverter converter = TypeDescriptor.GetConverter(type);
            if (converter.CanConvertFrom(value.GetType()))
            {
                return converter.ConvertFrom(value);
            }

            TypeConverter otherConverter = TypeDescriptor.GetConverter(value.GetType());
            if (otherConverter.CanConvertTo(type))
            {
                return otherConverter.ConvertTo(value, type);
            }

            throw new InvalidOperationException(string.Format(CultureInfo.CurrentCulture,
                LocalizedResourceManager.GetString("UnableToConvertTypeError"), value.GetType(), type));
        }

        public static bool TypeAllowsNull(Type type) => Nullable.GetUnderlyingType(type) != null || !type.IsValueType;

        public static Type GetGenericCollectionType(Type type) => GetInterfaceType(type, typeof(ICollection<>));

        public static Type GetDictionaryType(Type type) => GetInterfaceType(type, typeof(IDictionary<,>));

        private static Type GetInterfaceType(Type type, Type interfaceType)
        {
            return type.IsGenericType && type.GetGenericTypeDefinition() == interfaceType
                ? type
                : type
                    .GetInterfaces()
                    .SingleOrDefault(tInterface => 
                        tInterface.IsGenericType &&
                        tInterface.GetGenericTypeDefinition() == interfaceType);
        }

        public static bool IsKeyValueProperty(PropertyInfo property) => GetDictionaryType(property.PropertyType) != null;

        public static bool IsMultiValuedProperty(PropertyInfo property) => GetGenericCollectionType(property.PropertyType) != null || IsKeyValueProperty(property);

        public static bool IsEnumProperty(PropertyInfo property) => property.PropertyType.IsEnum;
    }
}