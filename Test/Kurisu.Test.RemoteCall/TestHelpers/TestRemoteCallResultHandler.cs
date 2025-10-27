using System.Net;
using Kurisu.RemoteCall.Abstractions;

namespace Kurisu.Test.RemoteCall.TestHelpers;

public class TestRemoteCallResultHandler : IRemoteCallResultHandler
{
    public TResult Handle<TResult>(HttpStatusCode statusCode, string responseBody)
    {
        // For testing purposes, if TResult is string, parse JSON to get data.value
        if (typeof(TResult) == typeof(string))
        {
            // naive parse
            var start = responseBody.IndexOf("\"value\":") ;
            if (start >= 0)
            {
                var sub = responseBody.Substring(start);
                var firstQuote = sub.IndexOf('"', 8);
                var secondQuote = sub.IndexOf('"', firstQuote + 1);
                var val = sub.Substring(firstQuote + 1, secondQuote - firstQuote - 1);
                return (TResult)(object)val;
            }

            return (TResult)(object)responseBody;
        }

        return default;
    }
}

