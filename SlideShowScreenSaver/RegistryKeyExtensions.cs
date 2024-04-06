﻿using System;
using System.Diagnostics;
using System.Globalization;
using System.Windows;
using Microsoft.Win32;
// ReSharper disable UnusedMember.Global
// ReSharper disable ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract

namespace SlideShowScreenSaver
{
    /// <summary>
    /// Read and Write particular data types into the registry
    /// </summary>
    public static class RegistryKeyExtensions
    {
        #region Get (Read) values from the registry, returned as a particular type
        /// <summary>
        /// Get a boolean value from the registry
        /// </summary>
        public static bool GetBoolean(this RegistryKey registryKey, string subKeyPath, bool defaultValue)
        {
            string valueAsString = registryKey.GetString(subKeyPath);
            if (valueAsString != null)
            {
                if (Boolean.TryParse(valueAsString, out bool value))
                {
                    return value;
                }
            }
            return defaultValue;
        }


        /// <summary>
        /// Get a TimeSpan value as Seconds from the registry
        /// </summary>
        public static TimeSpan GetTimeSpanAsSeconds(this RegistryKey registryKey, string subKeyPath, TimeSpan defaultValue)
        {
            string value = registryKey.GetString(subKeyPath);
            if (value == null)
            {
                return defaultValue;
            }
            return int.TryParse(value, out int seconds)
                ? TimeSpan.FromSeconds(seconds)
                : defaultValue;
        }

        /// <summary>
        /// Get a Double  from the registry
        /// </summary>
        public static double GetDouble(this RegistryKey registryKey, string subKeyPath, double defaultValue)
        {
            string valueAsString = registryKey.GetString(subKeyPath);
            if (valueAsString != null)
            {
                if (Double.TryParse(valueAsString, out double value))
                {
                    return value;
                }
            }
            return defaultValue;
        }

        /// <summary>
        /// Get an enum from the registry
        /// </summary>
        public static TEnum GetEnum<TEnum>(this RegistryKey registryKey, string subKeyPath, TEnum defaultValue) where TEnum : struct, IComparable, IConvertible, IFormattable
        {
            string valueAsString = registryKey.GetString(subKeyPath);
            try
            {
                if (valueAsString != null)
                {
                    return (TEnum)Enum.Parse(typeof(TEnum), valueAsString);
                }
            }
            catch
            {
                // This will drop through to return default value
            }
            return defaultValue;
        }

        /// <summary>
        /// Get an Int from the registry
        /// </summary>
        public static int GetInteger(this RegistryKey registryKey, string subKeyPath, int defaultValue)
        {
            // Check the arguments for null 
            if (registryKey == null)
            {
                // this should not happen
                Debug.Print("Problem in GetInteger");
                // throw new ArgumentNullException(nameof(registryKey));
                return defaultValue;
            }

            object? value = registryKey.GetValue(subKeyPath);
            if (value == null)
            {
                return defaultValue;
            }

            if (value is Int32 iValue)
            {
                return iValue;
            }

            if (value is string @string)
            {
                return Int32.Parse(@string);
            }
            return defaultValue;
        }

        /// <summary>
        /// Get a rect from the registry. If there are issues, just return the default value.
        /// </summary>
        public static Rect GetRect(this RegistryKey registryKey, string subKeyPath, Rect defaultValue)
        {
            string rectAsString = registryKey.GetString(subKeyPath);

            if (rectAsString == null)
            {
                return defaultValue;
            }
            try
            {
                Rect rectangle = Rect.Parse(rectAsString);
                return rectangle;
            }
            catch
            {
                // The parse can fail if the number format was saved as a non-American number, eg, Portugese uses , vs. as the decimal place.
                // This shouldn't happen as I have used an invarient to save numbers, but just in case... this will drop through to return default value
            }
            return defaultValue;
        }

        /// <summary>
        /// Get a size from the registry
        /// </summary>
        // ReSharper disable once UnusedMember.Global
        public static Size GetSize(this RegistryKey registryKey, string subKeyPath, Size defaultValue)
        {
            string sizeAsString = registryKey.GetString(subKeyPath);
            if (sizeAsString == null)
            {
                return defaultValue;
            }
            try
            {
                Size size = Size.Parse(sizeAsString);
                return size;
            }
            catch
            {
                // This will drop through to return default value.

            }
            return defaultValue;
        }

        /// <summary>
        /// Get a string from the registry
        /// </summary>
        public static string GetString(this RegistryKey registryKey, string subKeyPath, string defaultValue)
        {
            string valueAsString = registryKey.GetString(subKeyPath);
            if (valueAsString == null)
            {
                return defaultValue;
            }
            return valueAsString;
        }

        /// <summary>
        /// Get a REG_SZ key's value from the registry
        /// </summary>
        public static string GetString(this RegistryKey registryKey, string subKeyPath)
        {
            // Check the arguments for null 
            IsNullArgument(registryKey, nameof(registryKey));
            return (string)registryKey.GetValue(subKeyPath);
        }
        #endregion

        #region Write a particular type of value to the registry, depending on its type

        /// <summary>
        /// Write a boolean value to registry
        /// </summary>
        public static void Write(this RegistryKey registryKey, string subKeyPath, bool value)
        {
            try
            {
                registryKey.Write(subKeyPath, value.ToString().ToLowerInvariant());
            }
            catch
            {
                Debug.Print("Could not write registry for " + registryKey);
            }
        }

        /// <summary>
        /// Write a Double value to registry
        /// </summary>
        public static void Write(this RegistryKey registryKey, string subKeyPath, double value)
        {
            try
            {
                registryKey.Write(subKeyPath, value.ToString(CultureInfo.InvariantCulture));
            }
            catch
            {
                Debug.Print("Could not write registry for " + registryKey);
            }
        }

   
        /// <summary>
        /// Write an int value to registry
        /// </summary>
        public static void Write(this RegistryKey registryKey, string subKeyPath, int value)
        {
            // Check the arguments for null 
            IsNullArgument(registryKey, nameof(registryKey));
            try
            {
                registryKey.SetValue(subKeyPath, value, RegistryValueKind.DWord);
            }
            catch
            {
                Debug.Print("Could not write registry for " + registryKey);
            }
        }

        /// <summary>
        /// Write a Rect value to registry
        /// </summary>
        public static void Write(this RegistryKey registryKey, string subKeyPath, Rect value)
        {
            try
            {
                registryKey.Write(subKeyPath, value.ToString(CultureInfo.InvariantCulture));
            }
            catch
            {
                Debug.Print("Could not write registry for " + registryKey);
            }
        }

        /// <summary>
        /// Write a Size value to registry
        /// </summary>
        public static void Write(this RegistryKey registryKey, string subKeyPath, Size value)
        {
            try
            {
                registryKey.Write(subKeyPath, value.ToString(CultureInfo.InvariantCulture));
            }
            catch
            {
                Debug.Print("Could not write registry for " + registryKey);
            }
        }

        /// <summary>
        /// Write a string value to registry
        /// </summary>
        public static void Write(this RegistryKey registryKey, string subKeyPath, string value)
        {
            // Check the arguments for null 
            IsNullArgument(registryKey, nameof(registryKey));
            try
            {
                registryKey.SetValue(subKeyPath, value, RegistryValueKind.String);
            }
            catch
            {
                Debug.Print("Could not write registry for " + registryKey);
            }
        }

        /// <summary>
        /// Write a TimeSpan value as Seconds to registry
        /// </summary>
        public static void Write(this RegistryKey registryKey, string subKeyPath, TimeSpan value)
        {
            // Check the arguments for null 
            IsNullArgument(registryKey, nameof(registryKey));
            try
            {
                registryKey.SetValue(subKeyPath, value.TotalSeconds.ToString(CultureInfo.InvariantCulture), RegistryValueKind.String);
            }
            catch
            {
                Debug.Print("Could not write registry for " + registryKey);
            }

        }

        public static void IsNullArgument<T>(T value, string name) where T : class
        {
            if (value == null)
            {
                throw new ArgumentNullException(name);
            }
        }
        #endregion
    }
}
