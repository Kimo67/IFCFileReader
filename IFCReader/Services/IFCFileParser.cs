using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace IFCFileReader.Services
{
    public class IFCFileParser
    {
        public Dictionary<string, List<string>> Parse(string filePath)
        {
            var data = new Dictionary<string, List<string>>();
            var entityRegex = new Regex(@"#(\d+)=([A-Z0-9_]+)\((.*)\);");

            using (StreamReader reader = new StreamReader(filePath))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    var match = entityRegex.Match(line);
                    if (match.Success)
                    {
                        string id = match.Groups[1].Value;
                        string entityType = match.Groups[2].Value;
                        string parameters = match.Groups[3].Value;

                        if (!data.ContainsKey(entityType))
                        {
                            data[entityType] = new List<string>();
                        }
                        // On récupère uniquement ce qui commence par #
                        data[entityType].Add($"#{id} ({parameters})"); 
                    }
                }
            }

            return data;
        }
    }
}
