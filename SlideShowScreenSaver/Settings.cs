using System;
using System.Windows;
using Microsoft.Win32;
// ReSharper disable UnusedMember.Global

namespace SlideShowScreenSaver
{
    /// 
    /// Save the state of various things in the Registry.
    /// 
    public class Settings : UserRegistrySettings
    {
        #region Public Properties - Settings that will be saved into the registry
        public const string RootKey = @"Software\Greenberg Consulting\SlideShowScreenSaver\1.0";

        public string TimingKey = "Timing"; 
        public int Timing
        {
            get
            {
                using (RegistryKey registryKey = this.OpenRegistryKey())
                {
                    return registryKey.GetInteger(TimingKey, 3);
                }
            } 
            set
            {
                using (RegistryKey registryKey = this.OpenRegistryKey())
                {
                    registryKey.Write(TimingKey, value);
                }
            }
        }
        public string PhotoFolderKey = "PhotoFolder";
        public string PhotoFolder
        {
            get
            {
                using (RegistryKey registryKey = this.OpenRegistryKey())
                {
                    return registryKey.GetString(PhotoFolderKey, Environment.GetFolderPath(Environment.SpecialFolder.MyPictures));
                }
            }
            set
            {
                using (RegistryKey registryKey = this.OpenRegistryKey())
                {
                    registryKey.Write(PhotoFolderKey, value);
                }
            }
        }

        #endregion

        #region Constructors
        public Settings() : this(RootKey)
        {
        }

        internal Settings(string registryKey)
            : base(registryKey)
        {
            this.ReadSettingsFromRegistry();
        }
        #endregion

        #region Read from registry
        /// <summary>
        /// Read all standard settings from registry
        /// </summary>
        public void ReadSettingsFromRegistry()
        {
            using (RegistryKey registryKey = this.OpenRegistryKey())
            {
                this.Timing = registryKey.GetInteger(TimingKey, 3);
            }
        }

        /// <summary>
        /// Check if a particular registry key exists
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public bool IsRegistryKeyExists(string key)
        {
            using (RegistryKey registryKey = this.OpenRegistryKey())
            {
                return !string.IsNullOrEmpty(registryKey.GetString(key, string.Empty));
            }
        }

        /// <summary>
        /// Get a single registry entry
        /// </summary>
        public string GetFromRegistry(string key)
        {
            using (RegistryKey registryKey = this.OpenRegistryKey())
            {
                return registryKey.GetString(key, string.Empty);
            }
        }
        #endregion

        #region Write to registry
        /// <summary>
        /// Write all settings to registry
        /// </summary>
        public void WriteSettingsToRegistry()
        {
            using (RegistryKey registryKey = this.OpenRegistryKey())
            {
                registryKey.Write(TimingKey, this.Timing);
            }
        }

        /// <summary>
        /// Write a single registry entry, which will eventually convert its type to a string as needed
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public void WriteToRegistry(string key, string value)
        {
            using (RegistryKey registryKey = this.OpenRegistryKey())
            {
                registryKey.Write(key, value);
            }
        }

        // ReSharper disable once UnusedMember.Global
        public void WriteToRegistry(string key, double value)
        {
            using (RegistryKey registryKey = this.OpenRegistryKey())
            {
                registryKey.Write(key, value);
            }
        }

        public void WriteToRegistry(string key, Rect value)
        {
            using (RegistryKey registryKey = this.OpenRegistryKey())
            {
                registryKey.Write(key, value);
            }
        }

        public void WriteToRegistry(string key, bool value)
        {
            using (RegistryKey registryKey = this.OpenRegistryKey())
            {
                registryKey.Write(key, value);
            }
        }
        #endregion
    }
}
