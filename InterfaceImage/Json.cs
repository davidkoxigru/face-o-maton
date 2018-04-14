using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace InterfaceProj
{
    public class Json
    {
        public String photo;
        public int angle;
        public String type;
        public List<Item> items = new List<Item>();

        public void addItem(string path, string title)
        {
            var item = new Item();
            item.title = title;
            getPaths(path, item);
            items.Add(item);
        }

        private static void getPaths(string path, Item item)
        {
            var p = path.Remove(0, Path.GetPathRoot(path).Length);
            var paths = p.Split(Path.DirectorySeparatorChar);
            item.face = string.Join(Path.DirectorySeparatorChar.ToString(), paths, 0, 4);
            item.face += Path.DirectorySeparatorChar + "face.png";
            item.source = string.Join(Path.DirectorySeparatorChar.ToString(), paths, 0, 2);
            item.source += Path.DirectorySeparatorChar + paths[1] + "-" + paths[2] + ".jpg";
        }
    }

    public class Item 
    {
        public String title;
        public String face;
        public String source;
    }
}
