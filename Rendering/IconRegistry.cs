using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HLTVDiscordBridge.Rendering
{
    public class IconRegistry
    {
        public IconRegistry()
        {

        }

        public List<string> GetIconNames(string directoryPath)
        {
            List<string> icons = new List<string>();

            string[] filepaths = Directory.GetFiles(directoryPath, "*.png");

            foreach(string filepath in filepaths)
            {
                icons.Add(Path.GetFileNameWithoutExtension(filepath));
            }

            return icons;
        }
    }
}
