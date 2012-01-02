﻿/*************************************************************
 * 
 *  Copyright (C) 2011 by Peter L Jones
 *  pljones@users.sf.net
 * 
 * This is a derived work from:
 * 
 * PortableSettingsProvider.cs
 * Portable Settings Provider for C# applications
 * 
 * 2010- Michael Nathan
 * http://www.Geek-Republic.com
 * 
 * Licensed under Creative Commons CC BY-SA
 * http://creativecommons.org/licenses/by-sa/3.0/legalcode
 * 
 *************************************************************/
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Configuration;
using System.Xml;
using System.Xml.Serialization;
using System.IO;

namespace System.Configuration
{
    /// <summary>
    /// Provides persistence for application settings classes without the random folder naming of <see cref="LocalFileSettingsProvider"/>.
    /// </summary>
    public class PortableSettingsProvider : SettingsProvider//, IApplicationSettingsProvider
    {
        #region Template XML
        /* Define some static strings later used in our XML creation */
        
        // XML Root node
        private const string XMLROOT = "configuration";

        // Configuration declaration node
        private const string CONFIGNODE = "configSections";

        // Configuration section group declaration node
        private const string GROUPNODE = "sectionGroup";

        // User section node
        private const string USERNODE = "userSettings";

        // Application Specific Node
        private static string APPNODE = System.Diagnostics.Process.GetCurrentProcess().ProcessName + ".Properties.Settings";

        private System.Xml.XmlDocument xmlDoc = null;

        private static System.Xml.XmlDocument _xmlDocTemplate
        {
            get
            {
                System.Xml.XmlDocument _xmlDoc = new XmlDocument();
                // XML Declaration
                // <?xml version="1.0" encoding="utf-8"?>
                XmlDeclaration dec = _xmlDoc.CreateXmlDeclaration("1.0", "utf-8", null);
                _xmlDoc.AppendChild(dec);

                // Create root node and append to the document
                // <configuration>
                XmlElement rootNode = _xmlDoc.CreateElement(XMLROOT);
                _xmlDoc.AppendChild(rootNode);

                // Create Configuration Sections node and add as the first node under the root
                // <configSections>
                XmlElement configNode = _xmlDoc.CreateElement(CONFIGNODE);
                _xmlDoc.DocumentElement.PrependChild(configNode);

                // Create the user settings section group declaration and append to the config node above
                // <sectionGroup name="userSettings"...>
                XmlElement groupNode = _xmlDoc.CreateElement(GROUPNODE);
                groupNode.SetAttribute("name", USERNODE);
                groupNode.SetAttribute("type", "System.Configuration.UserSettingsGroup");
                configNode.AppendChild(groupNode);

                // Create the Application section declaration and append to the groupNode above
                // <section name="AppName.Properties.Settings"...>
                XmlElement newSection = _xmlDoc.CreateElement("section");
                newSection.SetAttribute("name", APPNODE);
                newSection.SetAttribute("type", "System.Configuration.ClientSettingsSection");
                groupNode.AppendChild(newSection);

                // Create the userSettings node and append to the root node
                // <userSettings>
                XmlElement userNode = _xmlDoc.CreateElement(USERNODE);
                _xmlDoc.DocumentElement.AppendChild(userNode);

                // Create the Application settings node and append to the userNode above
                // <AppName.Properties.Settings>
                XmlElement appNode = _xmlDoc.CreateElement(APPNODE);
                userNode.AppendChild(appNode);

                return _xmlDoc;
            }
        }
        #endregion
        
        /// <summary>
        /// Initializes the provider.
        /// </summary>
        /// <param name="name">The friendly name of the provider.</param>
        /// <param name="config">A collection of the name/value pairs representing the provider-specific attributes specified in the configuration for this provider.</param>
        /// <exception cref="ArgumentNullException">The name of the provider is null.</exception>
        /// <exception cref="ArgumentException">The name of the provider has a length of zero.</exception>
        /// <exception cref="InvalidOperationException">An attempt is made to call System.Configuration.Provider.ProviderBase.Initialize(System.String,System.Collections.Specialized.NameValueCollection) on a provider after the provider has already been initialized.</exception>
        public override void Initialize(string name, NameValueCollection config)
        {
            base.Initialize(this.ApplicationName, config);
        }

        private static string _ApplicationName = null;
        /// <summary>
        /// Return the executing assembly name without extension.
        /// </summary>
        public override string ApplicationName
        {
            get
            {
                if (_ApplicationName == null)
                    _ApplicationName = System.Diagnostics.Process.GetCurrentProcess().ProcessName;
                return _ApplicationName;
                // return (System.Reflection.Assembly.GetExecutingAssembly().GetName().Name);
            }
            set { if (_ApplicationName == null) _ApplicationName = value; }
        }

        /// <summary>
        /// Provide the application settings filename.
        /// </summary>
        /// <returns>The application settings filename.</returns>
        public virtual string GetSettingsFilename() { return Path.Combine(GetSettingsPath(), ApplicationName + ".user.config"); }

        private static string _SettingsPath = null;
        /// <summary>
        /// Provide the settings location for user settings.
        /// </summary>
        /// <returns>The settings location for user settings.</returns>
        /// <remarks>The settings location is "%ApplicationData%\[AssemblyCompany]\getApplicationName()".</remarks>
        public virtual string GetSettingsPath()
        {
            if (_SettingsPath == null)
            {
                object[] conames = this.GetType().Assembly.GetCustomAttributes(typeof(System.Reflection.AssemblyCompanyAttribute), false);
                string coname = conames.Length == 1 ? ((System.Reflection.AssemblyCompanyAttribute)conames[0]).Company : "noCompany";
                _SettingsPath = Path.Combine(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), coname), ApplicationName);
            }
            return _SettingsPath;
        }

        /// <summary>
        /// Retrieve settings from the configuration file.
        /// </summary>
        /// <param name="sContext">Provides contextual information that the provider can use when persisting settings.</param>
        /// <param name="settingsColl">Contains a collection of <see cref="SettingsProperty"/> objects.</param>
        /// <returns>A collection of settings property values that map <see cref="SettingsProperty"/> objects to <see cref="SettingsPropertyValue"/> objects.</returns>
        public override SettingsPropertyValueCollection GetPropertyValues(SettingsContext sContext, SettingsPropertyCollection settingsColl)
        {
            // Create a collection of values to return
            SettingsPropertyValueCollection retValues = new SettingsPropertyValueCollection();

            // Create a temporary SettingsPropertyValue to reuse
            SettingsPropertyValue setVal;

            // Loop through the list of settings that the application has requested and add them
            // to our collection of return values.
            foreach (SettingsProperty sProp in settingsColl)
            {
                setVal = new SettingsPropertyValue(sProp);
                setVal.IsDirty = false;
                setVal.SerializedValue = GetSetting(sProp);
                retValues.Add(setVal);
            }
            return retValues;
        }

        /// <summary>
        /// Save any of the applications settings that have changed (flagged as "dirty").
        /// </summary>
        /// <param name="sContext">Provides contextual information that the provider can use when persisting settings.</param>
        /// <param name="settingsColl">Contains a collection of <see cref="SettingsProperty"/> objects.</param>
        /// <exception cref="System.Xml.XmlException">The operation would not result in a well formed XML document (for example, no document element or duplicate XML declarations).</exception>
        public override void SetPropertyValues(SettingsContext sContext, SettingsPropertyValueCollection settingsColl)
        {
            // Set the values in XML
            foreach (SettingsPropertyValue spVal in settingsColl)
                SetSetting(spVal);

            if (!Directory.Exists(GetSettingsPath()))
                Directory.CreateDirectory(GetSettingsPath());
            XMLConfig.Save(GetSettingsFilename());
        }

        private XmlDocument XMLConfig
        {
            get
            {
                // Check if we already have accessed the XML config file. If the xmlDoc object is empty, we have not.
                if (xmlDoc == null)
                {
                    if (File.Exists(GetSettingsFilename()))
                    {
                        try
                        {
                            xmlDoc = new XmlDocument();
                            xmlDoc.Load(GetSettingsFilename());
                        }
                        catch
                        {
                            xmlDoc = (XmlDocument)_xmlDocTemplate.Clone();
                        }
                    }
                    else
                    {
                        xmlDoc = (XmlDocument)_xmlDocTemplate.Clone();
                    }
                }
                return xmlDoc;
            }
        }

        // Retrieve values from the configuration file, or if the setting does not exist in the file, 
        // retrieve the value from the application's default configuration
        private object GetSetting(SettingsProperty setProp)
        {
            object retVal;
            try
            {
                // Search for the specific settings node we are looking for in the configuration file.
                // If it exists, return the InnerText or InnerXML of its first child node, depending on the setting type.

                // If the setting is serialized as a string, return the text stored in the config
                if (setProp.SerializeAs.ToString() == "String")
                {
                    return XMLConfig.SelectSingleNode("//setting[@name='" + setProp.Name + "']").FirstChild.InnerText;
                }

                // If the setting is stored as XML, deserialize it and return the proper object.
                else
                {
                    string xmlData = XMLConfig.SelectSingleNode("//setting[@name='" + setProp.Name + "']").FirstChild.InnerXml;
                    return @"" + xmlData;
                }
            }
            catch (Exception)
            {
                // Check to see if a default value is defined by the application.
                // If so, return that value, using the same rules for settings stored as Strings and XML as above
                if ((setProp.DefaultValue != null))
                {
                    if (setProp.SerializeAs.ToString() == "String")
                    {
                        retVal = setProp.DefaultValue.ToString();
                    }
                    else
                    {
                        string settingType = setProp.PropertyType.ToString();
                        string xmlData = setProp.DefaultValue.ToString();
                        XmlSerializer xs = new XmlSerializer(typeof(string[]));
                        string[] data = (string[])xs.Deserialize(new XmlTextReader(xmlData, XmlNodeType.Element, null));

                        switch (settingType)
                        {
                            case "System.Collections.Specialized.StringCollection":
                                StringCollection sc = new StringCollection();
                                sc.AddRange(data);
                                return sc;

                            default: return "";
                        }
                    }
                }
                else
                {
                    retVal = "";
                }
            }
            return retVal;
        }

        private void SetSetting(SettingsPropertyValue setProp)
        {
            // Define the XML path under which we want to write our settings if they do not already exist
            XmlNode SettingNode = null;

            try
            {
                // Search for the specific settings node we want to update.
                // If it exists, return its first child node, (the <value>data here</value> node)
                SettingNode = XMLConfig.SelectSingleNode("//setting[@name='" + setProp.Name + "']").FirstChild;
            }
            catch (Exception)
            {
                SettingNode = null;
            }

            // If we have a pointer to an actual XML node, update the value stored there
            if ((SettingNode != null))
            {
                if (setProp.Property.SerializeAs.ToString() == "String")
                {
                    SettingNode.InnerText = setProp.SerializedValue.ToString();
                }
                else
                {
                    // Write the object to the config serialized as Xml - we must remove the Xml declaration when writing
                    // the value, otherwise .Net's configuration system complains about the additional declaration.
                    SettingNode.InnerXml = setProp.SerializedValue.ToString().Replace(@"<?xml version=""1.0"" encoding=""utf-16""?>", "");
                }
            }
            else
            {
                // If the value did not already exist in this settings file, create a new entry for this setting

                // Search for the application settings node (<Appname.Properties.Settings>) and store it.
                XmlNode tmpNode = XMLConfig.SelectSingleNode("//" + APPNODE);

                // Create a new settings node and assign its name as well as how it will be serialized
                XmlElement newSetting = xmlDoc.CreateElement("setting");
                newSetting.SetAttribute("name", setProp.Name);

                if (setProp.Property.SerializeAs.ToString() == "String")
                {
                    newSetting.SetAttribute("serializeAs", "String");
                }
                else
                {
                    newSetting.SetAttribute("serializeAs", "Xml");
                }

                // Append this node to the application settings node (<Appname.Properties.Settings>)
                tmpNode.AppendChild(newSetting);

                // Create an element under our named settings node, and assign it the value we are trying to save
                XmlElement valueElement = xmlDoc.CreateElement("value");
                if (setProp.Property.SerializeAs.ToString() == "String")
                {
                    valueElement.InnerText = setProp.SerializedValue.ToString();
                }
                else
                {
                    // Write the object to the config serialized as Xml - we must remove the Xml declaration when writing
                    // the value, otherwise .Net's configuration system complains about the additional declaration
                    valueElement.InnerXml = setProp.SerializedValue.ToString().Replace(@"<?xml version=""1.0"" encoding=""utf-16""?>", "");
                }

                //Append this new element under the setting node we created above
                newSetting.AppendChild(valueElement);
            }
        }
    }
}