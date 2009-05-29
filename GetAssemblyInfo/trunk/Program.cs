/***************************************************************************
 *  Copyright (C) 2009 by Peter L Jones                                    *
 *  peter@users.sf.net                                                     *
 *                                                                         *
 *  GetAssemblyInfo is free software: you can redistribute it and/or       *
 *  modify it under the terms of the GNU General Public License as         *
 *  published by the Free Software Foundation, either version 3 of the     *
 *  License, or (at your option) any later version.                        *
 *                                                                         *
 *  GetAssemblyInfo is distributed in the hope that it will be useful,     *
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of         *
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the          *
 *  GNU General Public License for more details.                           *
 *                                                                         *
 *  You should have received a copy of the GNU General Public License      *
 *  along with GetAssemblyInfo.  If not, see                               *
 *  <http://www.gnu.org/licenses/>.                                        *
 ***************************************************************************/
using System;
using System.Reflection;

namespace GetAssemblyInfo
{
    class Program
    {
        static int Main(string[] args)
        {
            if (args.Length == 0)
            {
                Help();
                return 0;
            }
            bool CodeBase = false;
            bool EscapedCodeBase = false;
            bool FullName = true;
            bool GlobalAssemblyCache = false;
            bool ImageRuntimeVersion = false;
            bool Location = false;

            bool CultureInfo = false;
            bool Flags = false;
            bool Name = false;
            bool PublicKey = false;
            bool PublicKeyToken = false;
            bool ProcessorArchitecture = false;
            bool Version = false;

            bool specialFmt = false;
            string format = "";
            bool bindingRedirect = false;

            bool good = true;
            foreach (string s in args)
            {
                if (s.StartsWith("-") || s.StartsWith("/"))
                {
                    if ("help".Equals(s.Substring(1).ToLower())) { Help(); break; }
                    bool x = true;
                    string t = s.Substring(1);
                    if (t.StartsWith("no")) { t = t.Substring(2); x = false; }

                    if ("all".Equals(t.ToLower()))
                    {
                        CodeBase = EscapedCodeBase = FullName = GlobalAssemblyCache = ImageRuntimeVersion = Location =
                            CultureInfo = Flags = Name = PublicKey = PublicKeyToken = ProcessorArchitecture = Version = x; continue;
                    }

                    if ("bindingredirect".Equals(t.ToLower())) { bindingRedirect = x; continue; }

                    if ("dependentassembly".Equals(t.ToLower()))
                    {
                        specialFmt = true;
                        format =
                            "      <dependentAssembly>\r\n"+
                            "        <assemblyIdentity name=\"{9}\" publicKeyToken=\"{11}\" />\r\n"+
                            "        <codeBase version=\"{13}\" href=\"{2}\" />\r\n" +
                            (bindingRedirect ? "        <bindingRedirect oldVersion=\"0.0.0.0 - {13}\" newVersion=\"{13}\" />\r\n" : "") +
                            "      </dependentAssembly>";
                        continue;
                    }

                    if ("codebase".StartsWith(t.ToLower())) { CodeBase = x; continue; }
                    if ("escapedeodebase".StartsWith(t.ToLower())) { EscapedCodeBase = x; continue; }
                    if ("fullname".StartsWith(t.ToLower())) { FullName = x; continue; }
                    if ("globalassemblycache".StartsWith(t.ToLower())) { GlobalAssemblyCache = x; continue; }
                    if ("imageruntimeversion".StartsWith(t.ToLower())) { ImageRuntimeVersion = x; continue; }
                    if ("location".StartsWith(t.ToLower())) { Location = x; continue; }

                    if ("cultureinfo".StartsWith(t.ToLower())) { CultureInfo = x; continue; }
                    if ("flags".StartsWith(t.ToLower())) { Flags = x; continue; }
                    if ("name".StartsWith(t.ToLower())) { Name = x; continue; }
                    if ("publickey".StartsWith(t.ToLower())) { PublicKey = x; continue; }
                    if ("publickeytoken".StartsWith(t.ToLower())) { PublicKeyToken = x; continue; }
                    if ("pktoken".StartsWith(t.ToLower())) { PublicKeyToken = x; continue; }
                    if ("processorarchitecture".StartsWith(t.ToLower())) { ProcessorArchitecture = x; continue; }
                    if ("version".StartsWith(t.ToLower())) { Version = x; continue; }

                    Console.WriteLine(String.Format("Invalid option \"{0}\".", s));
                    good = false;
                    break;
                }
                try
                {
                    if (!specialFmt)
                    {
                        format = "Assembly=\"{0}\" " +
                            (CodeBase ? "CodeBase=\"{1}\" " : "") +
                            (EscapedCodeBase ? "EscapedCodeBase=\"{2}\" " : "") +
                            (FullName ? "FullName=\"{3}\" " : "") +
                            (GlobalAssemblyCache ? "GlobalAssemblyCache=\"{4}\" " : "") +
                            (ImageRuntimeVersion ? "ImageRuntimeVersion=\"{5}\" " : "") +
                            (Location ? "Location=\"{6}\" " : "") +
                            (CultureInfo ? "CultureInfo=\"{7}\" " : "") +
                            (Flags ? "Flags=\"{8}\" " : "") +
                            (Name ? "Name=\"{9}\" " : "") +
                            (PublicKey ? "PublicKey=\"{10}\" " : "") +
                            (PublicKeyToken ? "PublicKeyToken=\"{11}\" " : "") +
                            (ProcessorArchitecture ? "ProcessorArchitecture=\"{12}\" " : "") +
                            (Version ? "Version=\"{13}\" " : "")
                            ;
                    }
                    Assembly a = Assembly.LoadFrom(s);
                    AssemblyName an = new AssemblyName(a.FullName);
                    object[] p = new object[] {
                        s
                        , a.CodeBase, a.EscapedCodeBase, a.FullName, a.GlobalAssemblyCache.ToString(), a.ImageRuntimeVersion, a.Location
                        , an.CultureInfo, an.Flags, an.Name, Pretty(an.GetPublicKey()), Pretty(an.GetPublicKeyToken()), an.ProcessorArchitecture, an.Version
                    };
                    Console.WriteLine(String.Format(format, p));
                }
                catch
                {
                    Console.WriteLine(String.Format("Invalid assembly \"{0}\".", s));
                    good = false;
                }
            }
            return good ? 0 : 1;
        }

        static string Pretty(byte[] a)
        {
            if (a == null) return "";
            string s = "";
            foreach (byte b in a) s += b.ToString("X2").ToLower();
            return s;
        }

        static void Help()
        {
            Console.WriteLine(String.Format("{0} {1}",
                System.IO.Path.GetFileNameWithoutExtension(typeof(Program).Assembly.Location),
                "{[[-|/][no][option]...} assembly {{[opt]...} assembly...}"));
            Console.WriteLine("  where \"option\" is \"all\" or (the start of):");
            Console.WriteLine("  codebase");
            Console.WriteLine("  escapedeodebase");
            Console.WriteLine("  fullname (default)");
            Console.WriteLine("  globalassemblycache");
            Console.WriteLine("  imageruntimeversion");
            Console.WriteLine("  location");
            Console.WriteLine("  cultureinfo");
            Console.WriteLine("  flags");
            Console.WriteLine("  name");
            Console.WriteLine("  publickey");
            Console.WriteLine("  publickeytoken | pktoken");
            Console.WriteLine("  processorarchitecture");
            Console.WriteLine("  version");
            Console.WriteLine("or \"help\" for this message.");
        }
    }
}
