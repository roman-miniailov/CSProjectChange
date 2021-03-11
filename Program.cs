using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace CSProjectChange
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Small tool to change version and other tags in csproj modern format files.");
            Console.WriteLine("Usage: CSProjectChange filename tag-name new-value");
            Console.WriteLine("Use folder instead filename to update all csproj files recursively.");

            if (args.Length != 3)
            {
                return;
            }

            var filename = args[0];
            var tag = args[1];
            var value = args[2];

            if (File.Exists(filename))
            {
                UpdateTag(filename, tag, value);
                return;
            }

            if (Directory.Exists(filename))
            {
                foreach (var file in Directory.EnumerateFiles(filename, "*.csproj", SearchOption.AllDirectories))
                {
                    UpdateTag(file, tag, value);
                }
            }
        }

        private static void UpdateTag(XDocument doc, string tag, string value)
        {
            //select all leaf elements having name equals "tag"
            var elementsToUpdate = doc.Descendants()
                .Where(o => o.Name == tag && !o.HasElements);

            //update elements value
            foreach (XElement element in elementsToUpdate)
            {
                element.Value = value;
            }
        }

        private static void UpdateTag(string filename, string tag, string value)
        {
            Console.WriteLine($"Processing file: {filename}");
            var doc = XDocument.Load(filename);

            UpdateTag(doc, tag, value);

            //save the XML back as file
            doc.Save(filename);
        }
    }
}
