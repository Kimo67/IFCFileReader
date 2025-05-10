using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace IFCFileReader.Services
{
    public class IFCFileParser
    {
        public Dictionary<string, Dictionary<string, string>> Parse(string filePath)
        {
            var data = new Dictionary<string, Dictionary<string, string>>();
            var entityRegex = new Regex(@"#(\d+)=([A-Z0-9_]+)\((.*)\);");

            foreach (var line in File.ReadLines(filePath))
            {
                var match = entityRegex.Match(line);
                if (match.Success)
                {
                    string id = match.Groups[1].Value;
                    string entityType = match.Groups[2].Value;
                    string parameters = match.Groups[3].Value;

                    if (!data.ContainsKey(entityType))
                        data[entityType] = new Dictionary<string, string>();

                    data[entityType][id] = parameters;
                }
            }

            return data;
        }
    }
}
