using System.ComponentModel;
using System.Reflection;
using System.Runtime.Serialization;

namespace Copy.Helpers
{
    /// <summary>
    /// Extension methods for <see cref="Enum"/> class.
    /// </summary>
    public static class EnumExtensions
    {

        /// <summary>
        /// Retrieves the value of an attribute of an enum value.
        /// </summary>
        /// <typeparam name="T">Attribute type</typeparam>
        /// <param name="value">Enum value</param>
        /// <returns>Attribute value</returns>
        public static T? GetAttribute<T>(this Enum value) where T : Attribute
        {
            return value.GetType()
                        .GetMember(value.ToString())
                        .FirstOrDefault()?
                        .GetCustomAttribute<T>();
        }
        /// <summary>
        /// Retrieves the description of an enum value.
        /// </summary>
        /// <param name="value">Enum value</param>
        /// <returns>Description</returns>
        public static string GetDescription(this Enum value)
        {
            return value.GetAttribute<DescriptionAttribute>()?.Description ?? value.ToString();
        }
        /// <summary>
        /// Retrieves the value of an <see cref="EnumMemberAttribute"/> of an enum value.
        /// </summary>
        /// <param name="value">Enum value</param>
        public static string GetEnumMember(this Enum value)
        {
            return value.GetAttribute<EnumMemberAttribute>()?.Value ?? value.ToString();
        }
    }
}
