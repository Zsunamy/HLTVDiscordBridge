using System;

namespace HLTVDiscordBridge.HttpResponses;

public class ApiError : Exception
{
    public string Id;
    public string Error;

    public ApiError(string id, string error)
    {
        Id = id;
        Error = error;
    }
}