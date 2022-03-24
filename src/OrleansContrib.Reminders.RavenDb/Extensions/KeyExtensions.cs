using System;

namespace OrleansContrib.Reminders.RavenDb.Extensions;

public static class KeyExtensions
{
    public static string Sanitize(this string key)
    {
        // Remove any characters that can't be used in Id
        // https://ravendb.net/docs/article-page/5.2/csharp/server/kb/document-identifier-generation#document-ids---limitations
        key = key
                .Replace('/', '_')        // Forward slash
                .Replace('\\', '_')       // Backslash
                .Replace('|', '_')        // Pipe sign
            ;     

        if (key.Length >= 512)
            throw new ArgumentException($"Key length {key.Length} is too long to be an Azure table key. Key={key}");

        return key;
    }
}