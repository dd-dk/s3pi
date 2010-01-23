﻿/***************************************************************************
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
        static List<string> reserved = new List<string>(new string[] { // must be lower case; currently fixed at two helpers
                "helper1label", "helper1command", "helper1arguments", "helper1readonly", "helper1ignorewritetimestamp",
                "helper2label", "helper2command", "helper2arguments", "helper2readonly", "helper2ignorewritetimestamp",
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
            keywords.AddRange(AApiVersionedFields.GetContentFields(0, typeof(IResourceKey)).ToArray()); // must be correct case

            demoPlugins = new Dictionary<string, Dictionary<string, string>>();
            if (!File.Exists(Config)) return;

            StreamReader sr = new StreamReader(new FileStream(Config, FileMode.Open, FileAccess.Read, FileShare.Read));

            bool inCommentBlock = false;

            for (string s = sr.ReadLine(); s != null; s = sr.ReadLine())
            {
                s = s.Trim();

                #region Comments
                if (inCommentBlock)
                {
                    int i = s.IndexOf("*/");
                    if (i > -1)
                    {
                        s = s.Substring(i + 2).Trim();
                        inCommentBlock = false;
                    }
                }

                string[] commentMarks = { "#", ";", "//" };
                for (int i = 0; s.Length > 0 && i < commentMarks.Length; i++)
                {
                    int j = s.IndexOf(commentMarks[i]);
                    if (j > -1) s = s.Substring(0, j).Trim();
                }

                if (inCommentBlock || s.Length == 0) continue;

                if (s.Contains("/*"))
                {
                    s = s.Substring(0, s.IndexOf("/*")).Trim();
                    inCommentBlock = true;
                }
                #endregion

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

            sr.Close();

            List<string> toDelete = new List<string>();
            foreach (string group in demoPlugins.Keys)
                if (!(demoPlugins[group].ContainsKey("helper1command") || demoPlugins[group].ContainsKey("helper2command")))
                    toDelete.Add(group);
            foreach (string group in toDelete) demoPlugins.Remove(group);
        }

        struct Cmd
        {
            public struct Helper
            {
                public string label;
                public string command;
                public string arguments;
                public bool isReadOnly;
                public bool ignoreWriteTimestamp;
                public bool export;
            }
            public string group;
            public string filename;
            public Dictionary<string, Helper> helpers;
        }
        Cmd cmd = new Cmd();

        bool hasHelper(string hlp) { return cmd.helpers == null || cmd.helpers.Count == 0 ? false : cmd.helpers.ContainsKey(hlp); }
        public bool HasHelper1 { get { return hasHelper("1"); } }
        public bool HasHelper2 { get { return hasHelper("2"); } }

        string helperLabel(string hlp) { return hasHelper(hlp) ? cmd.helpers[hlp].label : ""; }
        public string Helper1Label { get { return helperLabel("1"); } }
        public string Helper2Label { get { return helperLabel("2"); } }

        MemoryStream execHelper(string hlp, IResource res)
        {
            if (!hasHelper(hlp)) return null;
            try
            {
                Cmd.Helper helper = cmd.helpers[hlp];
                DateTime lastWriteTime = new DateTime();
                if (helper.export)
                    lastWriteTime = pasteTo(res, cmd.filename);
                else
                    Clipboard.SetData(DataFormats.Serializable, res.Stream);

                bool result = Execute(res, cmd, helper.command, helper.arguments);
                if (!helper.isReadOnly && result)
                {
                    if (helper.export)
                    {
                        return copyFrom(cmd.filename, helper.ignoreWriteTimestamp, lastWriteTime);
                    }
                    else if (Clipboard.ContainsData(DataFormats.Serializable))
                    {
                        return Clipboard.GetData(DataFormats.Serializable) as MemoryStream;
                    }
                }
                return null;
            }
            finally
            {
                if (cmd.filename != null) File.Delete(cmd.filename);
            }
        }
        public MemoryStream Helper1(IResource res) { return execHelper("1", res); }
        public MemoryStream Helper2(IResource res) { return execHelper("2", res); }

        bool helperIsReadOnly(string hlp) { return hasHelper(hlp) ? cmd.helpers[hlp].isReadOnly : true; }
        public bool Helper1IsReadOnly { get { return helperIsReadOnly("1"); } }
        public bool Helper2IsReadOnly { get { return helperIsReadOnly("2"); } }

        /// <summary>
        /// Initialise a new Cmd structure for a given resource
        /// </summary>
        /// <param name="key">The resource index entry</param>
        /// <param name="res">The resource</param>
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

            cmd.helpers = new Dictionary<string, Cmd.Helper>();

            bool wantFilename = false;
            foreach (string hlp in new string[] { "1", "2", })
            {
                if (!demoPlugins[cmd.group].ContainsKey("helper" + hlp + "command")) continue;
                Cmd.Helper helper = new Cmd.Helper();
                helper.label = getString(cmd.group, "helper" + hlp + "label");
                helper.command = getString(cmd.group, "helper" + hlp + "command");
                helper.arguments = getString(cmd.group, "helper" + hlp + "arguments");
                helper.isReadOnly = getString(cmd.group, "helper" + hlp + "readonly").Length > 0;
                helper.export = helper.command.IndexOf("{}") >= 0 || helper.arguments.IndexOf("{}") >= 0;
                helper.ignoreWriteTimestamp = helper.export && getString(cmd.group, "helper" + hlp + "ignorewritetimestamp").Length > 0;
                cmd.helpers.Add(hlp, helper);
                if (helper.export) wantFilename = true;
            }
            if (wantFilename)
                cmd.filename = Path.Combine(Path.GetTempPath(), (s3pi.Extensions.TGIN)(key as AResourceIndexEntry));
        }
        string getString(string group, string key) { return demoPlugins[group].ContainsKey(key) ? demoPlugins[group][key] : ""; }

        public static MemoryStream Edit(IResourceIndexEntry key, IResource res, string command, bool wantsQuotes, bool ignoreWriteTimestamp)
        {
            string filename = Path.Combine(Path.GetTempPath(), (s3pi.Extensions.TGIN)(key as AResourceIndexEntry));
            try
            {
                DateTime lastWriteTime = pasteTo(res, filename);

                string quote = wantsQuotes ? new string(new char[] { '"' }) : "";
                bool result = Execute(res, new Cmd(), command, quote + filename + quote);
                if (!result) return null;

                return copyFrom(filename, ignoreWriteTimestamp, lastWriteTime);
            }
            finally
            {
                File.Delete(filename);
            }
        }

        static DateTime pasteTo(IResource res, string filename)
        {
            BinaryWriter bw = new BinaryWriter((new FileStream(filename, FileMode.Create, FileAccess.Write)));
            MemoryStream ms = res.Stream as MemoryStream;
            if (ms != null) bw.Write(ms.ToArray());
            else
            {
                res.Stream.Position = 0;
                bw.Write(new BinaryReader(res.Stream).ReadBytes((int)res.Stream.Length));
            }
            bw.Close();
            return File.GetLastWriteTime(filename);
        }
        static MemoryStream copyFrom(string filename, bool ignoreWriteTimestamp, DateTime lastWriteTime)
        {
            if (ignoreWriteTimestamp || File.GetLastWriteTime(filename) != lastWriteTime)
            {
                MemoryStream ms = new MemoryStream();
                FileStream fs = new FileStream(filename, FileMode.Open, FileAccess.Read);
                (new BinaryWriter(ms)).Write((new BinaryReader(fs)).ReadBytes((int)fs.Length));
                fs.Close();
                return ms;
            }
            return null;
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
            catch (Exception ex)
            {
                CopyableMessageBox.IssueException(ex,
                    typeof(DemoPlugins).Assembly.FullName + "\n" + String.Format("Application failed to start:\n{0}\n{1}", command, arguments),
                    "Launch failed");
                return false;
            }

            Application.DoEvents();
            while (!p.WaitForExit(500))
                Application.DoEvents();

            return p.ExitCode == 0;
        }
    }
}
