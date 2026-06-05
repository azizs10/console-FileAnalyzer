using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using System.Collections.Generic;

class Program
{
    static void Main(string[] args)
    {
        Console.Clear();
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("=== Advanced File System Analyzer ===");
        Console.ResetColor();

        Console.Write("\nEnter the folder path to scan: ");
        string path = Console.ReadLine();

        if (!Directory.Exists(path))
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Error: The specified folder does not exist!");
            Console.ResetColor();
            return;
        }

        Console.WriteLine("\nWhat would you like to do?");
        Console.WriteLine("1. Find top 10 largest files");
        Console.WriteLine("2. Find empty directories");
        Console.WriteLine("3. Find duplicate files (via SHA-256 Hash)");
        Console.Write("\nSelect an option (1-3): ");
        
        string choice = Console.ReadLine();

        switch (choice)
        {
            case "1":
                FindBigFiles(path);
                break;
            case "2":
                FindEmptyDirectories(path);
                break;
            case "3":
                FindDuplicates(path);
                break;
            default:
                Console.WriteLine("Invalid choice.");
                break;
        }

        Console.WriteLine("\nPress any key to exit...");
        Console.ReadKey();
    }

    static void FindBigFiles(string path)
    {
        Console.WriteLine("\nScanning files... Please wait.");
        
        try
        {
            var bigFiles = Directory.EnumerateFiles(path, "*.*", SearchOption.AllDirectories)
                                    .Select(f => new FileInfo(f))
                                    .OrderByDescending(f => f.Length)
                                    .Take(10)
                                    .ToList();

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("\n=== TOP 10 LARGEST FILES ===");
            Console.ResetColor();

            foreach (var file in bigFiles)
            {
                double sizeInMb = (double)file.Length / (1024 * 1024);
                Console.WriteLine($"- {file.Name} ({sizeInMb:F2} MB) -> {file.FullName}");
            }
        }
        catch (UnauthorizedAccessException)
        {
            Console.WriteLine("Error: Access denied to some system files in this directory.");
        }
    }

    static void FindEmptyDirectories(string path)
    {
        Console.WriteLine("\nSearching for empty directories...");
        
        try
        {
            var dirs = Directory.EnumerateDirectories(path, "*", SearchOption.AllDirectories);
            int count = 0;

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("\n=== EMPTY DIRECTORIES ===");
            Console.ResetColor();

            foreach (var dir in dirs)
            {
                if (!Directory.EnumerateFileSystemEntries(dir).Any())
                {
                    Console.WriteLine($"- {dir}");
                    count++;
                }
            }

            if (count == 0) 
            {
                Console.WriteLine("No empty directories found.");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An error occurred: {ex.Message}");
        }
    }

    static void FindDuplicates(string path)
    {
        Console.WriteLine("\nRunning multi-threaded duplicate scan...");

        try
        {
            var allFiles = Directory.GetFiles(path, "*.*", SearchOption.AllDirectories);
            var hashDatabase = new ConcurrentDictionary<string, List<string>>();

            Parallel.ForEach(allFiles, filePath =>
            {
                try
                {
                    using (var sha256 = SHA256.Create())
                    using (var stream = File.OpenRead(filePath))
                    {
                        var hashBytes = sha256.ComputeHash(stream);
                        string hashString = BitConverter.ToString(hashBytes).Replace("-", "");

                        hashDatabase.AddOrUpdate(hashString,
                            new List<string> { filePath },
                            (key, oldList) => { lock(oldList) { oldList.Add(filePath); } return oldList; });
                    }
                }
                catch { }
            });

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("\n=== DUPLICATE FILES FOUND ===");
            Console.ResetColor();

            bool found = false;
            foreach (var pair in hashDatabase)
            {
                if (pair.Value.Count > 1)
                {
                    found = true;
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine($"\nIdentical files (Hash: {pair.Key.Substring(0, 8)}...):");
                    Console.ResetColor();
                    foreach (var file in pair.Value)
                    {
                        Console.WriteLine($"  -> {file}");
                    }
                }
            }

            if (!found) 
            {
                Console.WriteLine("No duplicate files found.");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
    }
}