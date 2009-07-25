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
        static List<string> reserved = new List<string>(new string[] { // must be lower case
                "viewer", "viewerarguments",
                "editor", "editorarguments", "editorignorewritetimestamp",
                "wrapper",
            });
        static List<string> keywords = new List<string>();
        static Dictionary<string, Dictionary<string, string>> demoPlugins = null;

        static string config = "";

        public static string Config
        {
            get { return config != null && config.Length > 0 ? config : Path.Combine(Path.GetDirectoryName(typeof(DemoPlugins).Assembly.Location), "Helpers.txt"); }
            set { if (config != value) { config = value; demoPlugins = null; } }
        }

        static void ReadConfig()
        {
            keywords.AddRange(reserved.ToArray());
            keywords.AddRange(AApiVersionedFields.GetContentFields(0, typeof(IResourceIndexEntry)).ToArray()); // must be correct case

            demoPlugins = new Dictionary<string, Dictionary<string, string>>();
            if (!File.Exists(Config)) return;

            StreamReader sr = new StreamReader(new FileStream(Config, FileMode.Open, FileAccess.Read));

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
                string keyword = headtail[0].Trim();
                if (reserved.Contains(keyword.ToLower())) keyword = keyword.ToLower();
                if (!keywords.Contains(keyword)) continue;
                if (target.ContainsKey(keyword)) continue;
                target.Add(keyword, headtail[1].Trim());
            }

            List<string> toDelete = new List<string>();
            foreach (string group in demoPlugins.Keys)
                if (!(demoPlugins[group].ContainsKey("viewer") || demoPlugins[group].ContainsKey("editor")))
                    toDelete.Add(group);
            foreach (string group in toDelete) demoPlugins.Remove(group);
        }

        struct Cmd
        {
            public string group;
            public string filename;
            public bool hasViewer;
            public bool hasEditor;
            public bool exportViewer;
            public bool exportEditor;
            public bool ignoreWriteTimestamp;
        }
        Cmd cmd = new Cmd();
        public bool HasViewer { get { return cmd.hasViewer; } }
        public bool HasEditor { get { return cmd.hasEditor; } }

        /// <summary>
        /// Get the command line to execute for a given resource
        /// </summary>
        /// <param name="pkg">The package containing the resource</param>
        /// <param name="key">The resource index entry</param>
        /// <returns></returns>
        public DemoPlugins(IResourceIndexEntry key, IResource res)
        {
            if (demoPlugins == null) ReadConfig();

            if (res == null || key == null) return;

            string wrapper = res.GetType().Name.ToLower();

            bool match = false;

            foreach (string g in demoPlugins.Keys)
            {
                foreach (string kwd in demoPlugins[g].Keys)
                {
                    if (kwd.Equals("wrapper"))
                    {
                        if ((new List<string>(demoPlugins[g]["wrapper"].ToLower().Split(' '))).Contains(wrapper)) { cmd.group = g; match = true; goto matched; }
                        if ((new List<string>(demoPlugins[g]["wrapper"].Split(' '))).Contains("*")) { cmd.group = g; match = true; goto matched; }
                        continue;
                    }

                    if (reserved.Contains(kwd)) continue;

                    if (keywords.Contains(kwd))
                    {
                        if ((new List<string>(demoPlugins[g][kwd].Split(' '))).Contains("" + key[kwd])) { cmd.group = g; match = true; goto matched; }
                        if ((new List<string>(demoPlugins[g][kwd].Split(' '))).Contains("*")) { cmd.group = g; match = true; goto matched; }
                        continue;
                    }
                }
            }
        matched:
            if (!match) return;

            cmd.hasViewer = demoPlugins[cmd.group].ContainsKey("viewer");
            cmd.hasEditor = demoPlugins[cmd.group].ContainsKey("editor");
            cmd.exportViewer = cmd.hasViewer &&
                (demoPlugins[cmd.group]["viewer"].IndexOf("{}") >= 0
                || (demoPlugins[cmd.group].ContainsKey("viewerarguments") && demoPlugins[cmd.group]["viewerarguments"].IndexOf("{}") >= 0)
                );
            cmd.exportEditor = cmd.hasEditor &&
                (demoPlugins[cmd.group]["editor"].IndexOf("{}") >= 0
                || (demoPlugins[cmd.group].ContainsKey("editorarguments") && demoPlugins[cmd.group]["editorarguments"].IndexOf("{}") >= 0)
                );
            cmd.ignoreWriteTimestamp = cmd.exportEditor &&
                (demoPlugins[cmd.group].ContainsKey("editorignorewritetimestamp"));
            if (cmd.exportViewer || cmd.exportEditor)
                cmd.filename = Path.Combine(Path.GetTempPath(), (s3pi.Extensions.TGIN)(key as AResourceIndexEntry));
        }

        public bool View(IResource res)
        {
            if (!cmd.hasViewer) return false;

            DateTime lastWriteTime = new DateTime();
            if (cmd.exportViewer && Clipboard.ContainsData(DataFormats.Serializable))
                lastWriteTime = pasteTo(cmd.filename);

            bool result = Execute(res, cmd, demoPlugins[cmd.group]["viewer"], demoPlugins[cmd.group].ContainsKey("viewerarguments") ? demoPlugins[cmd.group]["viewerarguments"] : "");

            if (File.Exists(cmd.filename))
                File.Delete(cmd.filename);

            return result;
        }

        public bool Edit(IResource res)
        {
            if (!cmd.hasEditor) return false;

            DateTime lastWriteTime = new DateTime();
            if (cmd.exportEditor && Clipboard.ContainsData(DataFormats.Serializable))
                lastWriteTime = pasteTo(cmd.filename);

            bool result = Execute(res, cmd, demoPlugins[cmd.group]["editor"], demoPlugins[cmd.group].ContainsKey("editorarguments") ? demoPlugins[cmd.group]["editorarguments"] : "");

            if (result)
            {
                if (cmd.exportEditor)
                    result = copyFile(cmd.filename, cmd.ignoreWriteTimestamp ? new DateTime(0) : lastWriteTime);
            }

            return result;
        }

        public static bool Edit(IResourceIndexEntry key, IResource res, string command, bool wantsQuotes, bool ignoreWriteTimestamp)
        {
            string filename = Path.Combine(Path.GetTempPath(), (s3pi.Extensions.TGIN)(key as AResourceIndexEntry));
            FileStream fs = new FileStream(filename, FileMode.OpenOrCreate, FileAccess.Write);
            new BinaryWriter(fs).Write(new BinaryReader(res.Stream).ReadBytes((int)res.Stream.Length));
            fs.Close();

            DateTime lastWriteTime = File.GetLastWriteTime(filename);
            string fmt = wantsQuotes ? @"""{0}""" : "{0}";
            return Execute(res, new Cmd(), command, String.Format(fmt, filename)) && copyFile(filename, ignoreWriteTimestamp ? new DateTime(0) : lastWriteTime);
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

        static bool copyFile(string filename, DateTime lastWriteTime)
        {
            if (File.Exists(filename))
                try
                {
                    if (File.GetLastWriteTime(filename) != lastWriteTime)
                    {
                        MemoryStream ms = new MemoryStream();
                        FileStream fs = new FileStream(filename, FileMode.Open, FileAccess.Read);
                        (new BinaryWriter(ms)).Write((new BinaryReader(fs)).ReadBytes((int)fs.Length));
                        fs.Close();
                        Clipboard.SetData(DataFormats.Serializable, ms);
                        return true;
                    }
                }
                finally
                {
                    File.Delete(filename);
                }
            return false;
        }

        static bool Execute(IResource res, Cmd cmd, string command, string arguments)
        {
            command = command.Replace("{}", cmd.filename);
            arguments = arguments.Replace("{}", cmd.filename);
            foreach (string prop in res.ContentFields)
                if (arguments.IndexOf("{" + prop.ToLower() + "}") >= 0) arguments = arguments.Replace("{" + prop.ToLower() + "}", "" + res[prop]);

            System.Diagnostics.Process p = new System.Diagnostics.Process();

            p.StartInfo.FileName = command;
            p.StartInfo.Arguments = arguments;
            p.StartInfo.UseShellExecute = false;

            try { p.Start(); }
            catch(Exception ex)
            {
                string exmsg = ex.Message;
                for (Exception inex = ex.InnerException; inex != null; inex = inex.InnerException) exmsg += "\n" + inex.Message;
                MessageBox.Show(String.Format("Application failed to start:\n{0}\n{1}\n{2}", command, arguments, exmsg),
                    "Launch failed",
                    MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return false;
            }

            Application.DoEvents();
            while (!p.WaitForExit(500))
                Application.DoEvents();

            return p.ExitCode == 0;
        }
    }
}
