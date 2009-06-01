/***************************************************************************
 *  Copyright (C) 2009 by Peter L Jones                                    *
 *  peter@users.sf.net                                                     *
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
using s3pi.Interfaces;
using System.Windows.Forms;
using s3pi.WrapperDealer;

namespace s3pi.DemoPlugins
{
    /// <summary>
    /// Use this class to turn {IPackage, IResourceIndexEntry} tuples into commands to be executed
    /// </summary>
    public class DemoPlugins
    {
        static List<string> reserved = new List<string>(new string[] {
                "Editor", "Export", "Wrapper", "PropertyExists"
            });
        static List<string> keywords = new List<string>();
        static Dictionary<string, Dictionary<string, string>> demoPlugins = new Dictionary<string, Dictionary<string, string>>();

        static DemoPlugins()
        {
            foreach (string s in reserved) { keywords.Add(s.ToLower()); }
            foreach (string s in AApiVersionedFields.GetContentFields(0, typeof(IResourceIndexEntry))) { keywords.Add(s.ToLower()); }

            StreamReader sr = new StreamReader(new FileStream(Path.Combine(Path.GetDirectoryName(typeof(DemoPlugins).Assembly.Location), "DemoPlugins.txt"), FileMode.Open, FileAccess.Read));
            for (string s = sr.ReadLine(); s != null; s = sr.ReadLine())
            {
                s = s.Trim();
                if (s.Length == 0 || s.StartsWith("#") || s.StartsWith(";") || s.StartsWith("//")) continue;

                string[] headtail = s.Split(new char[] { '.' }, 2);
                if (headtail.Length != 2) continue;
                Dictionary<string, string> target;
                if (demoPlugins.ContainsKey(headtail[0].Trim())) target = demoPlugins[headtail[0].Trim()];
                else { target = new Dictionary<string, string>(); demoPlugins.Add(headtail[0].Trim(), target); }

                headtail = headtail[1].Trim().Split(new char[] { ':', '=' }, 2);
                if (headtail.Length != 2) continue;
                if (!keywords.Contains(headtail[0].Trim().ToLower())) continue;
                if (target.ContainsKey(headtail[0].Trim().ToLower())) continue;
                target.Add(headtail[0].Trim().ToLower(), headtail[1].Trim());
            }

            List<string> toDelete = new List<string>();
            foreach (string group in demoPlugins.Keys) if (!demoPlugins[group].ContainsKey("editor")) toDelete.Add(group);
            foreach (string group in toDelete) demoPlugins.Remove(group);
        }

        struct Cmd
        {
            public string group;
            public string filename;
            public bool isValid;
        }
        Cmd cmd = new Cmd();
        public bool IsValid { get { return cmd.isValid; } }

        /// <summary>
        /// Get the command line to execute for a given resource
        /// </summary>
        /// <param name="pkg">The package containing the resource</param>
        /// <param name="key">The resource index entry</param>
        /// <returns></returns>
        public DemoPlugins(IPackage pkg, IResourceIndexEntry key)
        {
            if (pkg == null || key == null) return;

            IResource res = WrapperDealer.WrapperDealer.GetResource(0, pkg, key);
            if (res == null) return;

            string wrapper = res.GetType().Name.ToLower();
            List<string> properties = new List<string>(); foreach (string s in res.ContentFields) properties.Add(s.ToLower());

            bool match = false;

            foreach (string g in demoPlugins.Keys)
            {
                foreach (string kwd in demoPlugins[g].Keys)
                {
                    if (kwd.Equals("wrapper"))
                        foreach (string wrp in demoPlugins[g]["wrapper"].ToLower().Split(' '))
                            if (wrapper.Equals(wrp)) { cmd.group = g; match = true; goto matched; }

                    if (kwd.Equals("propertyexists"))
                        foreach (string property in demoPlugins[g]["propertyexists"].ToLower().Split(' '))
                            if (properties.Contains(property)) { cmd.group = g; match = true; goto matched; }

                    if (reserved.Contains(kwd)) continue;

                    if (properties.Contains(kwd))
                    {
                        TypedValue tv = res[kwd];
                        foreach (string value in demoPlugins[g][kwd].ToLower().Split(' '))
                            if (value == ("" + tv).Trim().ToLower()) { cmd.group = g; match = true; goto matched; }
                    }
                }
            }
        matched:
            if (!match) return;

            if (demoPlugins[cmd.group].ContainsKey("export"))
                cmd.filename = Path.Combine(Path.GetTempPath(), getFilename(pkg, key));

            cmd.isValid = true;
        }

        public bool Execute(IResource res)
        {
            if (!cmd.isValid) return false;

            DateTime lastWriteTime = new DateTime();
            if (demoPlugins[cmd.group].ContainsKey("export") && Clipboard.ContainsData(DataFormats.Serializable))
                lastWriteTime = pasteTo(cmd.filename);

            string editor = demoPlugins[cmd.group]["editor"].Replace("{}", cmd.filename);
            foreach (string prop in res.ContentFields)
                if (editor.IndexOf("{" + prop.ToLower() + "}") >= 0) editor = editor.Replace("{" + prop.ToLower() + "}", "" + res[prop]);

            bool result = runProcess(editor);

            if (result)
            {
                if (demoPlugins[cmd.group].ContainsKey("export"))
                    result = copyFile(lastWriteTime);
            }
            if (File.Exists(cmd.filename))
                File.Delete(cmd.filename);

            return result;
        }

        string getFilename(IPackage pkg, IResourceIndexEntry key)
        {
            s3pi.Extensions.TGIN tgin = key as AResourceIndexEntry;
            IResourceIndexEntry nmap = pkg.Find(new string[] { "ResourceType" }, new TypedValue[] { new TypedValue(typeof(uint), (uint)0x0166038C) });
            if (nmap != null)
            {
                Dictionary<string, string> map = s3pi.WrapperDealer.WrapperDealer.GetResource(0, pkg, nmap) as Dictionary<string, string>;
                if (map != null)
                    map.TryGetValue(key["ResourceType"], out tgin.ResName);
            }
            return tgin;
        }

        DateTime pasteTo(string filename)
        {
            MemoryStream ms = Clipboard.GetData(DataFormats.Serializable) as MemoryStream;
            if (ms != null)
            {
                BinaryWriter bw = new BinaryWriter((new FileStream(cmd.filename, FileMode.Create, FileAccess.Write)));
                bw.Write((new BinaryReader(ms)).ReadBytes((int)ms.Length));
                bw.Close();
                return File.GetLastWriteTime(cmd.filename);
            }
            return new DateTime();
        }

        bool copyFile(DateTime lastWriteTime)
        {
            if (File.Exists(cmd.filename) && File.GetLastWriteTime(cmd.filename) != lastWriteTime)
            {
                MemoryStream ms = new MemoryStream();
                FileStream fs = new FileStream(cmd.filename, FileMode.Open, FileAccess.Read);
                (new BinaryWriter(ms)).Write((new BinaryReader(fs)).ReadBytes((int)fs.Length));
                fs.Close();
                Clipboard.SetData(DataFormats.Serializable, ms);
                return true;
            }
            return false;
        }

        bool runProcess(string editor)
        {
            System.Diagnostics.Process p = new System.Diagnostics.Process();

            string[] args = editor.Split(new char[] { ' ' }, 2);
            p.StartInfo.FileName = args[0];
            if (args.Length > 1) p.StartInfo.Arguments = args[1];

            p.StartInfo.UseShellExecute = true;
            p.StartInfo.RedirectStandardOutput = false;

            p.Start();
            p.WaitForExit();

            return p.ExitCode == 0;
        }
    }
}
