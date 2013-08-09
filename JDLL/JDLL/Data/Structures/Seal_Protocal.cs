﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using System.Threading.Tasks;

using JDLL.Util;
using JDLL.Exceptions;

namespace JDLL.Data.Structures
{
    public class Seal_Protocal
    {
        public const String NO_VALUE = "N/A";

        #region Writing
        public static void CreateFile(String Path, String NAME)
        {
            FileIO.CreateFile(Path, "NAME;" + NAME);
        }

        public static void WriteEntry(String Path, String Prefix, String Name, String Value)
        {
            IsSealFile(Path);

            FileIO.WriteToFile(Path, Prefix + ";" + Value + ";NAME=" + Name);
        }

        public static bool DeleteEntry(String Path, String Prefix, String Name)
        {
            try
            {
                IsSealFile(Path);

                String[] Contents = FileIO.ReadFile(Path);
                List<String> NewContent = new List<String>();

                foreach (String s in Contents)
                    if (s.StartsWith(Prefix) && GetValueName(s).Equals(Name))
                        continue;
                    else
                        NewContent.Add(s);

                FileIO.ReplaceAll(Path, NewContent.ToArray());

                return true;
            }
            catch
            {
                return false;
            }
        }
        #endregion

        #region Reading
        public static String GetFileName(String Path)
        {
            try
            {
                IsSealFile(Path);
                return FileIO.ReadFile(Path)[0].Split(';')[1];
            }
            catch(FileNotSealException ex)
            {
                return NO_VALUE;
            }
        }

        public static String[] GetData(String Path)
        {
            IsSealFile(Path);
            return FileIO.ReadFile(Path);
        }

        public static String[] GetDataWithPrefix(String Path, String Prefix)
        {
            IsSealFile(Path);

            String[] FullData = FileIO.ReadFile(Path);

            List<String> PrefixData = new List<String>();

            foreach (String s in FullData)
                if (s.StartsWith(Prefix + ";"))
                    PrefixData.Add(s);


            return PrefixData.ToArray();
        }

        public static String GetEntryName(String Value)
        {
            String[] Data = Value.Split(';');

            String Name = "";

            foreach (String s in Data)
                if (s.StartsWith("NAME"))
                    Name = s.Split('=')[1];

            if (Name.Equals(""))
                throw new MalformedEntryException("Entry is missing \"NAME=\"");

            return Name;
        }

        public static String GetEntryPrefix(String Value)
        {
            String[] s = Value.Split(';');

            return s[0];
        }

        public static String[] GetEntryData(String Value, bool withPrefix)
        {
            String[] s = Value.Split(';');
            List<String> Data = new List<String>();

            if (withPrefix)
                Data.Add(s[0]);

            for (int i = 0; i < s.Length; i++)
                if (i == 0 || s[i].StartsWith("NAME"))
                    continue;
                else
                    Data.Add(s[i]);

            return Data.ToArray();
        }

        public static String GetEntryByName(String Path, String Prefix, String Name)
        {
            IsSealFile(Path);

            String[] Contents = FileIO.ReadFile(Path);

            foreach (String s in Contents)
                if (s.StartsWith(Prefix + ";"))
                    if (s.Split(';').Last().Contains("NAME"))
                        if (s.Split(';').Last().Contains(Name))
                            return s;

            return NO_VALUE;
        }

        public static void IsSealFile(String Path)
        {
            String[] Data = FileIO.ReadFile(Path);

            if (!Data[0].Contains("NAME;"))
                throw new FileNotSealException(Path + " is not part of the Seal Data structure");

            int rand = 0;

            foreach (String s in Data)
                if (rand == 1)
                    if (!s.Contains("NAME="))
                        throw new MalformedEntryException("Entry is missing \"NAME=\"");
                    else
                        rand++;
        }
        #endregion
    }
}
