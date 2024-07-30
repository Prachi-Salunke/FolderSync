using System;
using System.IO;
using System.Security.Cryptography;
using System.Threading;

class FolderSync
{
    static string sourceDir;
    static string replicaDir;
    static int intervalSeconds;
    static string logFile;
    static Timer syncTimer;

    static void Main(string[] args)
    {
        if (args.Length != 4)
        {
            Console.WriteLine("Usage: <sourceDir> <replicaDir> <intervalSeconds> <logFile>");
            return;
        }

        sourceDir = args[0];
        replicaDir = args[1];
        intervalSeconds = int.Parse(args[2]);
        logFile = args[3];

        if (!Directory.Exists(sourceDir) || !Directory.Exists(replicaDir))
        {
            Console.WriteLine("Source or replica directory does not exist.");
            return;
        }

        SyncFolders();

        syncTimer = new Timer(SyncFolders, null, intervalSeconds * 1000, intervalSeconds * 1000);

        Console.WriteLine("Synchronization is running. Press Enter to stop.");
        Console.ReadLine();
    }

    static void SyncFolders(object state = null)
    {
        try
        {
            var sourceFiles = Directory.GetFiles(sourceDir, "*", SearchOption.AllDirectories);
            var replicaFiles = Directory.GetFiles(replicaDir, "*", SearchOption.AllDirectories);

            foreach (var srcFile in sourceFiles)
            {
                var relativePath = Path.GetRelativePath(sourceDir, srcFile);
                var destFile = Path.Combine(replicaDir, relativePath);

                var destDir = Path.GetDirectoryName(destFile);
                if (!Directory.Exists(destDir))
                {
                    Directory.CreateDirectory(destDir);
                    Log("Created directory: " + destDir);
                }

                if (!File.Exists(destFile) || !FilesAreSame(srcFile, destFile))
                {
                    File.Copy(srcFile, destFile, true);
                    Log("Copied file: " + srcFile + " to " + destFile);
                }
            }

            foreach (var repFile in replicaFiles)
            {
                var relativePath = Path.GetRelativePath(replicaDir, repFile);
                var srcFile = Path.Combine(sourceDir, relativePath);

                if (!File.Exists(srcFile))
                {
                    File.Delete(repFile);
                    Log("Deleted file: " + repFile);
                }
            }

            var replicaDirs = Directory.GetDirectories(replicaDir, "*", SearchOption.AllDirectories);
            foreach (var repDir in replicaDirs)
            {
                var relativePath = Path.GetRelativePath(replicaDir, repDir);
                var srcDir = Path.Combine(sourceDir, relativePath);

                if (!Directory.Exists(srcDir))
                {
                    Directory.Delete(repDir, true);
                    Log("Deleted directory: " + repDir);
                }
            }

            Log("Synchronization complete.");
        }
        catch (Exception ex)
        {
            Log("Error: " + ex.Message);
        }
    }

    static bool FilesAreSame(string file1, string file2)
    {
        using (var md5 = MD5.Create())
        {
            var hash1 = md5.ComputeHash(File.ReadAllBytes(file1));
            var hash2 = md5.ComputeHash(File.ReadAllBytes(file2));
            return hash1.SequenceEqual(hash2);
        }
    }

    static void Log(string message)
    {
        var logMessage = DateTime.Now + ": " + message;
        Console.WriteLine(logMessage);
        File.AppendAllText(logFile, logMessage + Environment.NewLine);
    }
}
