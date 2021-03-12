using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using CommandLine;
using CommandLine.Text;

namespace CSProjectChange
{
    /// <summary>
    /// Mode.
    /// </summary>
    enum Mode
    {
        /// <summary>
        /// Tag.
        /// </summary>
        tag,

        /// <summary>
        /// Package version.
        /// </summary>
        package
    }

    /// <summary>
    /// Options.
    /// </summary>
    class Options
    {
        [Option('f', "file", Required = true, HelpText = "Input file or folder to be processed.")]
        public string Filename { get; set; }

        [Option('t', "tag", Required = false, HelpText = "Tag or package name.")]
        public string Tag { get; set; }

        [Option('s', "subtag", Required = false, HelpText = "Subtag.")]
        public string Subtag { get; set; }

        [Option('v', "value", Required = false, HelpText = "Value or version.")]
        public string Value { get; set; }

        [Option('m', "mode", Required = false, HelpText = "Mode.")]
        public Mode Mode { get; set; }
    }

    class Program
    {
        private static Options _options;

        private static bool _error = false;

        static void Main(string[] args)
        {
            //﻿<?xml version="1.0" encoding="utf-8"?>
            Console.WriteLine("Small tool to change version and other tags in csproj modern format files.");
            Console.WriteLine("Usage for tag update: CSProjectChange -f filename -t tag-name -v new-value");
            Console.WriteLine("Usage for package version update: CSProjectChange -m package -f filename -t package-name -v new-version");
            Console.WriteLine("Use folder instead filename to update all csproj files recursively.");

            Parser.Default.ParseArguments<Options>(args)
                .WithParsed(RunOptions)
                .WithNotParsed(HandleParseError);
            
            if (_error)
            {
                return;
            }

            if (File.Exists(_options.Filename))
            {
                Update(_options.Filename);
                return;
            }

            if (Directory.Exists(_options.Filename))
            {
                foreach (var file in Directory.EnumerateFiles(_options.Filename, "*.csproj", SearchOption.AllDirectories))
                {
                    Update(file);
                }
            }
        }
        static void RunOptions(Options opts)
        {
            _options = opts;
        }

        static void HandleParseError(IEnumerable<Error> errs)
        {
            _error = true;

            foreach (var error in errs)
            {
                Console.WriteLine($"Error is source arguments: {error.Tag}");
            }
        }

        private static void UpdatePackageVersion(XDocument doc)
        {
            //select all leaf elements having name equals "tag"
            var elementsToUpdate = doc.Descendants()
                .Where(o => o.Name == "PackageReference" && !o.HasElements);
            foreach (XElement element in elementsToUpdate)
            {
                var attr = element.FirstAttribute;

                bool found = false;
                while (attr != null)
                {
                    if (attr.Name == "Include")
                    {
                        if (attr.Value == _options.Tag)
                        {
                            found = true; 
                        }

                        break;
                    }

                    attr = attr.NextAttribute;
                }

                attr = element.FirstAttribute;
                if (found)
                {
                    while (attr != null)
                    {
                        if (attr.Name == "Version")
                        {
                            attr.Value = _options.Value;
                        }

                        attr = attr.NextAttribute;
                    }

                    break;
                }
            }
        }

        private static void UpdateTag(XDocument doc)
        {
            //select all leaf elements having name equals "tag"
            var elementsToUpdate = doc.Descendants()
                .Where(o => o.Name == _options.Tag && !o.HasElements);

            //update elements value
            if (string.IsNullOrEmpty(_options.Subtag))
            {
                foreach (XElement element in elementsToUpdate)
                {
                    element.Value = _options.Value;
                }
            }
            else
            {
                foreach (XElement element in elementsToUpdate)
                {
                    var attr = element.FirstAttribute;
                    while (attr != null)
                    {
                        if (attr.Name == _options.Subtag)
                        {
                            attr.Value = _options.Value;
                        }

                        attr = attr.NextAttribute;
                    }
                }
            }
        }

        private static void Update(string filename)
        {
            Console.WriteLine($"Processing file: {filename}");
            var doc = XDocument.Load(filename);

            if (_options.Mode == Mode.tag)
            {
                UpdateTag(doc);
            }
            else
            {
                UpdatePackageVersion(doc);
            }

            //save the XML back as file
            doc.Save(filename);

            if (Path.GetExtension(filename).ToLowerInvariant() == ".csproj")
            {
                var lines = File.ReadAllLines(filename).ToList();
                if (lines[0].Contains("xml"))
                {
                    lines.RemoveAt(0);
                }

                File.WriteAllLines(filename, lines);
            }
        }
    }
}
