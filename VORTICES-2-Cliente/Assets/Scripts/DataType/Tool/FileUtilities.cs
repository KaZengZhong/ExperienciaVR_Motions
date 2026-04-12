using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

public static class FileUtilities
{
    public static List<List<string>> GetGroupedFilenamesInDirectory(string directoryPath, string fileExtension)
    {
        try
        {
            string[] filePaths = Directory.GetFiles(directoryPath);
            List<string> validFileNames = filePaths.Where(filePath => filePath.EndsWith(fileExtension)).ToList();

            List<List<string>> groups = validFileNames
                .GroupBy(filename => GetPrefix(filename))
                .Select(group => group.ToList())
                .ToList();

            return groups;
        }
        catch (Exception e)
        {
            Console.WriteLine("Error getting grouped file names: " + e.Message);
            return null;
        }
    }

    private static string GetPrefix(string filename)
    {
        int underscoreIndex = filename.IndexOf('_');
        if (underscoreIndex >= 0)
        {
            return filename.Substring(0, underscoreIndex);
        }
        return filename;
    }
    public static List<string> GetFileNamesInDirectory(string directoryPath)
    {
        try
        {
            string[] filePaths = Directory.GetFiles(directoryPath);
            List<string> fileNames = new List<string>();

            foreach (string filePath in filePaths)
            {
                string fileName = Path.GetFileName(filePath);
                fileNames.Add(fileName);
            }

            return fileNames;
        }
        catch (Exception e)
        {
            Console.WriteLine("Error getting file names: " + e.Message);
            return null;
        }
    }
}
