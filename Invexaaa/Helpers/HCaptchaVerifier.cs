// SnomiAssignmentReal/Helpers/HCaptchaVerifier.cs
using Microsoft.Extensions.Configuration;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace SnomiAssignmentReal.Helpers
{
    public class HCaptchaVerifier
    {
        private readonly HttpClient _http;
        private readonly IConfiguration _cfg;

        public HCaptchaVerifier(HttpClient http, IConfiguration cfg)
        {
            _http = http;
            _cfg = cfg;
        }

        public async Task<bool> VerifyAsync(string token, string? remoteIp)
        {
            if (string.IsNullOrWhiteSpace(token)) return false;

            var secret = _cfg["Captcha:hCaptcha:Secret"];
            var url = _cfg["Captcha:hCaptcha:VerifyUrl"] ?? "https://hcaptcha.com/siteverify";

            var form = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("secret", secret ?? ""),
                new KeyValuePair<string, string>("response", token),
                new KeyValuePair<string, string>("remoteip", remoteIp ?? "")
            });

            var resp = await _http.PostAsync(url, form);
            if (!resp.IsSuccessStatusCode) return false;

            var json = await resp.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            return doc.RootElement.TryGetProperty("success", out var ok) && ok.GetBoolean();
        }
    }
}
