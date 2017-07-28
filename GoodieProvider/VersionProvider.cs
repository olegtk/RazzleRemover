﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace GoodieProvider
{
    public class VersionProvider
    {
        MultiValueDictionary<string, string> PackageVersions = new MultiValueDictionary<string, string>();

        public void ProcessAllProjects(string sourcePath)
        {
            if (!Directory.Exists(sourcePath)) throw new DirectoryNotFoundException($"Directory {sourcePath} does not exist");
            var allProjects = Directory.EnumerateFiles(sourcePath, "*.csproj", SearchOption.AllDirectories);
            foreach (var project in allProjects)
            {
                GetVersionsAndModifyProject(project);
            }
            // Save the versions.props
        }

        private void GetVersionsAndModifyProject(string project)
        {
            var xe = XElement.Load(project);
            var references = xe.Elements().Descendants().Where(n => n.Name.LocalName == "PackageReference");
            foreach (var reference in references)
            {
                var name = reference.Attributes().Single(n => n.Name.LocalName == "Include").Value;
                var version = reference.Elements().SingleOrDefault(n => n.Name.LocalName == "Version")?.Value;
                if (version == null)
                {
                    Console.WriteLine($"Unable to get version for {name} in {Path.GetFileName(project)}");
                    continue;
                }
                if (version.StartsWith("$("))
                {
                    // This is a msbuild variable. This project was already converted.
                    continue;
                }
                PackageVersions.Add(name, version);
                var propertyName = getPropertyNameForPackage(name);
                reference.Elements().Single(n => n.Name.LocalName == "Version").SetValue(propertyName);
            }
            var x = xe;
        }

        /// <summary>
        /// Creates a MSBuild property for a given package name.
        /// Removes dots from the given package name.
        /// </summary>
        private string getPropertyNameForPackage(string name)
        {
            var processedName = name.Replace(".", "");
            return $"$({processedName})";
        }
    }
}