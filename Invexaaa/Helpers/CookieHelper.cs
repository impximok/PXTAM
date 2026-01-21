using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace SnomiAssignmentReal.Helpers;

public static class CookieHelper
{
    public static void SetCookie(HttpResponse response, string key, object value, int? expireMinutes = null)
    {
        var options = new CookieOptions
        {
            Expires = expireMinutes.HasValue ? DateTimeOffset.Now.AddMinutes(expireMinutes.Value) : null
        };

        string json = JsonSerializer.Serialize(value);
        response.Cookies.Append(key, json, options);
    }

    public static T? GetCookie<T>(HttpRequest request, string key)
    {
        request.Cookies.TryGetValue(key, out string? json);
        return json == null ? default : JsonSerializer.Deserialize<T>(json);
    }
}
