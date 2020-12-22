using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReCT_IDE
{
    [Serializable]
    public class Project
    {
        public string Name;
        public string Icon;
        public string MainClass;

        public Project(string name, string icon, string mainClass)
        {
            Name = name;
            Icon = icon;
            MainClass = mainClass;
        }
    }
}
