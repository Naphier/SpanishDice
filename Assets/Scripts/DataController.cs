using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class DataController
{
    public string error = "";
    public List<string> GetFileLines(string filePath, string fileName)
    {
        List<string> lines = new List<string>();
        string fullPath = Path.Combine(filePath, fileName);
        bool fileExists = File.Exists(fullPath);
        if (fileExists)
        {
            string line = "";
            StreamReader reader = new StreamReader(fullPath);
            int counter = 0;
            while ((line = reader.ReadLine()) != null)
            {
                if (!string.IsNullOrEmpty(line))
                    lines.Add(line);

                counter++;
                if (counter == int.MaxValue)
                {
                    Debug.LogError("'It's too big!' - She");
                    break;
                }
            }
        }


        if (lines == null || lines.Count <= 0)
        {
            error = string.Format("No data retrieved from file '{0}'\nFile {1}", 
                fullPath, 
                (fileExists ? "exists." : "not found."));
        }
        return lines;
    }
}

