﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace TCOCFP
{
    public class ConfigFileHandler
    {
        private ConfigData ConfigData;
        private ConfigParsedData ConfigParsedData;
        private IComputeFunc ComputeFuncs;
        private IEnumerationFunc EnumerationFuncs;
        private string FileName { get; }

        public ConfigFileHandler(IComputeFunc computeFuncs, IEnumerationFunc enumerationFuncs, string fileName = "ConfigFile.ocfp")
        {
            ComputeFuncs = computeFuncs;
            EnumerationFuncs = enumerationFuncs;
            ConfigData = new ConfigData();
            FileName = fileName;
        }

        public void InitConfigFile(bool overwrite)
        {
            var filePath = GetFilePath();
            var fileExsists = File.Exists(filePath);
            if (fileExsists && overwrite)
            {
                File.Delete(filePath);
                File.CreateText(filePath);
            }
            else if (!fileExsists)
            {
                File.CreateText(filePath);
            }
        }

        public ConfigParsedData LoadConfigFromFile(IArguments arguments)
        {
            try
            {
                var lines = new List<string>();
                var fileContent = File.ReadLines(GetFilePath());
                foreach (var line in fileContent)
                {
                    if ((string.IsNullOrWhiteSpace(line) || line.StartsWith("#")) && lines.Count > 0)
                    {
                        ConfigData.Add(ParseSection(lines));
                        lines = new List<string>();
                    }
                    else if (!string.IsNullOrWhiteSpace(line) && !line.StartsWith("#"))
                    {
                        lines.Add(line);
                    }
                }
                if (lines.Count > 0)
                {
                    ConfigData.Add(ParseSection(lines));
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return ConfigParsedData = ConfigData.ComputeAndReplace(arguments, ComputeFuncs, EnumerationFuncs);
        }

        public void WriteToConfig(string section, string value)
        {
            ConfigData.Add(CreateConfigSection(section, value));
        }

        private ConfigSection CreateConfigSection(string section, string value, bool isStringHeader = true)
        {
            return new ConfigSection(section, value, isStringHeader: isStringHeader, computeFuncs: ComputeFuncs, enumerationFuncs: EnumerationFuncs);
        }

        private ConfigSection CreateConfigSection(string section, List<string> values, bool isStringHeader = true)
        {
            return new ConfigSection(section, values, isStringHeader, computeFuncs: ComputeFuncs, enumerationFuncs: EnumerationFuncs);
        }

        private ConfigSection ParseSection(List<string> lines)
        {
            if (lines.Count < 2)
            {
                throw new ArgumentException(nameof(lines));
            }

            var header = lines.First();
            var parsedHeader = ParseHeader(header);

            return CreateConfigSection(parsedHeader.key, ParseSubSection(lines), parsedHeader.IsStringHeader);
        }

        private List<string> ParseSubSection(List<string> lines)
        {
            return lines.GetRange(1, lines.Count - 1);//All but the first row
        }

        private (string key, bool IsStringHeader) ParseHeader(string header)
        {

            if (IsWellFormedStringSectionHeader(header))
            {
                var key = header.Trim('[', ']');
                return (key, true);
            }
            else if (IsWellFormedBoolSectionHeader(header))
            {
                var key = header.Trim('<', '>');
                return (key, false);
            }
            else
            {
                throw new ArgumentException(nameof(header));
            }
        }

        private bool IsWellFormedStringSectionHeader(string header) => header.StartsWith("[") && header.EndsWith("]");
        private bool IsWellFormedBoolSectionHeader(string header) => header.StartsWith("<") && header.EndsWith(">");
        private string GetFilePath() => Path.Combine(Environment.CurrentDirectory, FileName);
    }
}