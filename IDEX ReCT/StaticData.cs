using System;
using System.Collections.Generic;
using ElectronNET.API;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace IDEX_ReCT
{
    public static class StaticData
    {
        public static int ActiveTab = 0;
        
        public static List<Tab> Tabs = new List<Tab>();
        public static BrowserWindow Window;

        public static Tab CurrentTab => Tabs[ActiveTab];

        public static void Init()
        {
            Tabs.Add(new Tab("//ReCT Compiler v2.2 with IDEX v1.0\npackage sys;\nsys::Print(\"Hello World!\");"));
        }

        public static string AssembleTabs()
        {
            object[][] tabs = new object[Tabs.Count][];
            for (int i = 0; i < Tabs.Count; i++)
            {
                tabs[i] = new object[2];
                tabs[i][0] = Tabs[i].FileName == "" ? "untitled" : Path.GetFileName(Tabs[i].FileName);
                tabs[i][1] = Tabs[i].Saved;
            }

            return JsonSerializer.Serialize(new Dictionary<string, object>(){ {"tabs", tabs}, {"active", ActiveTab} });
        }

        public struct tabstct
        {
            public string name;
            public bool saved;
        }
    }

    public class Tab
    {
        public string Code = "";
        public string FileName = "";
        public bool Saved = true;

        public Tab()
        {

        }
        
        public Tab(string code)
        {
            Code = code;
        }
    }
}