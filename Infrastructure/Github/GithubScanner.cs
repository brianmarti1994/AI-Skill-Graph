using Application.Abstractions;
using Application.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Github
{
    public sealed class GithubScanner : IGithubScanner
    {
        private readonly HttpClient _http;

        public GithubScanner(HttpClient http)
        {
            _http = http;
            _http.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("CvAiMatcher", "1.0"));
            _http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
        }

        public async Task<IReadOnlyList<GithubRepoSummary>> ScanAsync(string githubUrl, CancellationToken ct)
        {
            var user = ParseUser(githubUrl);
            if (user is null) return Array.Empty<GithubRepoSummary>();

            var repos = await _http.GetFromJsonAsync<List<Repo>>($"/users/{user}/repos?per_page=8&sort=updated", ct)
                       ?? new();

            var results = new List<GithubRepoSummary>();

            foreach (var r in repos.Where(r => !r.Fork))
            {
                var langs = await _http.GetFromJsonAsync<Dictionary<string, long>>($"/repos/{user}/{r.Name}/languages", ct)
                            ?? new();

                results.Add(new GithubRepoSummary(
                    r.Name,
                    r.HtmlUrl ?? $"https://github.com/{user}/{r.Name}",
                    langs
                ));
            }

            return results;
        }

        private static string? ParseUser(string url)
        {
            // supports: https://github.com/username or https://github.com/username/
            if (!Uri.TryCreate(url, UriKind.Absolute, out var u)) return null;
            if (!u.Host.Contains("github.com", StringComparison.OrdinalIgnoreCase)) return null;

            var parts = u.AbsolutePath.Trim('/').Split('/', StringSplitOptions.RemoveEmptyEntries);
            return parts.Length >= 1 ? parts[0] : null;
        }

        private sealed class Repo
        {
            public string Name { get; set; } = "";
            public bool Fork { get; set; }
            public string? HtmlUrl { get; set; }
        }
    }
}
