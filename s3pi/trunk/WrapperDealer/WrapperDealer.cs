/***************************************************************************
 *  Copyright (C) 2009 by Peter L Jones                                    *
 *  pljones@users.sf.net                                                   *
 *                                                                         *
 *  This file is part of the Sims 3 Package Interface (s3pi)               *
 *                                                                         *
 *  s3pi is free software: you can redistribute it and/or modify           *
 *  it under the terms of the GNU General Public License as published by   *
 *  the Free Software Foundation, either version 3 of the License, or      *
 *  (at your option) any later version.                                    *
 *                                                                         *
 *  s3pi is distributed in the hope that it will be useful,                *
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of         *
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the          *
 *  GNU General Public License for more details.                           *
 *                                                                         *
 *  You should have received a copy of the GNU General Public License      *
 *  along with s3pi.  If not, see <http://www.gnu.org/licenses/>.          *
 ***************************************************************************/
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using s3pi.Interfaces;

namespace s3pi.WrapperDealer
{
    /// <summary>
    /// Responsible for associating ResourceType in the IResourceIndexEntry with a particular class (a "wrapper") that understands it
    /// or the default wrapper.
    /// </summary>
    public static class WrapperDealer
    {
        /// <summary>
        /// Create a new Resource of the requested type, allowing the wrapper to initialise it appropriately
        /// </summary>
        /// <param name="APIversion">API version of request</param>
        /// <param name="resourceType">Type of resource (currently a string like "0xDEADBEEF")</param>
        /// <returns></returns>
        public static IResource CreateNewResource(int APIversion, string resourceType)
        {
            return WrapperForType(resourceType, new object[] { APIversion, null });
        }


        /// <summary>
        /// Retrieve a resource from a package, readying the appropriate wrapper or the default wrapper
        /// </summary>
        /// <param name="APIversion">API version of request</param>
        /// <param name="pkg">Package containing <typeparamref name="IResourceCode"/> <paramref name="rie"/></param>
        /// <param name="rie">Identifies resource to be returned</param>
        /// <returns>A resource from the package</returns>
        public static IResource GetResource(int APIversion, IPackage pkg, IResourceIndexEntry rie, bool AlwaysDefault)
        {
            return WrapperForType(AlwaysDefault ? "*" : rie["ResourceType"], new object[] { APIversion, (pkg as APackage).GetResource(rie) });
        }


        /// <summary>
        /// Retrieve a resource from a package, readying the appropriate wrapper
        /// </summary>
        /// <param name="APIversion">API version of request</param>
        /// <param name="pkg">Package containing <typeparamref name="IResourceCode"/> <paramref name="rie"/></param>
        /// <param name="rie">Identifies resource to be returned</param>
        /// <returns>A resource from the package</returns>
        public static IResource GetResource(int APIversion, IPackage pkg, IResourceIndexEntry rie) { return GetResource(APIversion, pkg, rie, false); }

        #region Implementation
        static List<KeyValuePair<string, Type>> typeMap = null;
        static Dictionary<Type, Assembly> assemblyMap = null;

        static WrapperDealer()
        {
            string folder = Path.GetDirectoryName(typeof(WrapperDealer).Assembly.Location);
            //string folder = (string)Microsoft.Win32.Registry.LocalMachine.GetValue(@"Software\s3pi\s3pi\InstallDir", null);
            typeMap = new List<KeyValuePair<string, Type>>();
            assemblyMap = new Dictionary<Type, Assembly>();
            foreach (string path in Directory.GetFiles(folder, "*.dll"))
            {
                try
                {
                    Assembly dotNetDll = Assembly.LoadFile(path);
                    Type[] types = dotNetDll.GetTypes();
                    foreach (Type t in types)
                    {
                        if (!t.IsSubclassOf(typeof(AResourceHandler))) continue;

                        AResourceHandler arh = (AResourceHandler)dotNetDll.CreateInstance(t.FullName, false, BindingFlags.CreateInstance, null,
                            new object[] { }, null, null);

                        if (arh == null) continue;

                        foreach (Type k in arh.Keys)
                        {
                            if (assemblyMap.ContainsKey(k)) continue;
                            assemblyMap.Add(k, dotNetDll);
                            foreach (string s in arh[k])
                                typeMap.Add(new KeyValuePair<string, Type>(s, k));
                        }
                    }
                }
                catch { }
            }
        }

        static IResource WrapperForType(string type, object[] parms)
        {
            Type t = null;
            foreach (KeyValuePair<string, Type> kvp in typeMap) if (kvp.Key == "*") { t = kvp.Value; break; }
            if (Settings.Settings.Checking) if (t == null)
                    throw new InvalidOperationException("Could not find Default Resource handler");

            foreach (KeyValuePair<string, Type> kvp in typeMap) if (kvp.Key == type) { t = kvp.Value; break; }
            Assembly a = assemblyMap[t];

            return (IResource)a.CreateInstance(t.FullName, false, BindingFlags.CreateInstance, null, parms, null, null);
        }
        #endregion
    }
}
