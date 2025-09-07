using System;
using System.IO;
using HLTVDiscordBridge.Modules;
using Discord;

namespace HLTVDiscordBridge.Modules;

public static class CacheCleaner
{             
    public static void Clean()
    {
        var deletedFiles = 0;
        var deletedDirs = 0;
        
        //delete player-cards after 7 days
        deletedDirs += CleanDirectory("./cache/playercards", 7);
        
        //delete team-cards after 7 days  
        deletedDirs += CleanDirectory("./cache/teamcards", 7);
        
        // Clean old JSON cache files (older than 1 day)
        deletedFiles += CleanJsonFiles("./cache", 1);
        
        // Force garbage collection after cache cleaning
        if (deletedFiles > 0 || deletedDirs > 0)
        {
            Logger.Log(new MyLogMessage(LogSeverity.Verbose, "CacheCleaner", 
                $"Cleaned {deletedFiles} files and {deletedDirs} directories"));
            MemoryOptimizations.ForceGarbageCollection();
        }
    }
    
    private static int CleanDirectory(string path, int daysOld)
    {
        var deletedDirs = 0;
        Directory.CreateDirectory(path);
        
        foreach (string dir in Directory.GetDirectories(path))
        {
            if (Directory.GetCreationTime(dir).AddDays(daysOld).Date <= DateTime.Now.Date) 
            { 
                try
                {
                    Directory.Delete(dir, true);
                    deletedDirs++;
                }
                catch (Exception ex)
                {
                    Logger.Log(new MyLogMessage(LogSeverity.Warning, "CacheCleaner", 
                        $"Failed to delete directory {dir}: {ex.Message}"));
                }
            }                
        }
        return deletedDirs;
    }
    
    private static int CleanJsonFiles(string path, int daysOld)
    {
        var deletedFiles = 0;
        if (!Directory.Exists(path)) return 0;
        
        foreach (string file in Directory.GetFiles(path, "*.json"))
        {
            if (File.GetCreationTime(file).AddDays(daysOld).Date <= DateTime.Now.Date)
            {
                try
                {
                    File.Delete(file);
                    deletedFiles++;
                }
                catch (Exception ex)
                {
                    Logger.Log(new MyLogMessage(LogSeverity.Warning, "CacheCleaner", 
                        $"Failed to delete file {file}: {ex.Message}"));
                }
            }
        }
        return deletedFiles;
    }
}