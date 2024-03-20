using System;
using System.IO;
using System.Threading.Tasks;
using Discord;
using Discord.Net;
using ExCSS;

namespace HLTVDiscordBridge.Modules;

public static class Logger
{
    public static Task DiscordLog(LogMessage log)
    {
        Log(new MyLogMessage(log));
        return Task.CompletedTask;
    }

    public static void Log(MyLogMessage log)
    {
        string time = DateTime.Now.ToString("HH:mm:ss");
        TextWriter print = log.Severity is LogSeverity.Critical or LogSeverity.Error ? Console.Error : Console.Out;
        switch (log.Exception)
        {
            case HttpException hex:
                print.WriteLine($"[Discord/{log.Severity}/{log.Source}] {time} {hex.Message} | Reason: {hex.Reason}");
                break;
            case ApiError aex:
                print.WriteLine($"[HltvApi/{log.Severity}/{log.Source}] {time} {aex.Error} | Id: {aex.Id}");
                break;
            case DeploymentException dex:
                print.WriteLine($"[HltvApi/{log.Severity}/{log.Source}] {time} {dex.Message} | Stacktrace: {dex.StackTrace}");
                break;

            case null:
                print.WriteLine($"[General/{log.Severity}/{log.Source}] {time} {log.Message}");
                break;
        }
    }
}
