using System;
using Discord;

namespace HLTVDiscordBridge.Modules;

public class MyLogMessage
{
    public LogSeverity Severity { init; get; }
    public string Source { init; get; }
    public string Message { init; get; }
    public Exception Exception { init; get; }

    public MyLogMessage(LogSeverity severity, Exception exception)
    {
        Severity = severity;
        Source = exception.Source;
        Message = exception.Message;
        Exception = exception;
    }
    
    public MyLogMessage(LogSeverity severity, string source, string message)
    {
        Severity = severity;
        Source = source;
        Message = message;
        Exception = null;
    }
    
    public MyLogMessage(LogMessage log)
    {
        Severity = log.Severity;
        Source = log.Source;
        Message = log.Message;
        Exception = log.Exception;
    }

    public LogMessage ToLogMessage()
    {
        return new LogMessage(Severity, Source, Message, Exception);
    }
}