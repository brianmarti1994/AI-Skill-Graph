using Application.Abstractions;
using DocumentFormat.OpenXml.Bibliography;
using DocumentFormat.OpenXml.ExtendedProperties;
using DocumentFormat.OpenXml.Wordprocessing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Infrastructure.Ai
{
    public sealed class OllamaAiCvExtractor : IAiCvExtractor
    {
        private readonly HttpClient _http;
        private readonly string _model;

        public OllamaAiCvExtractor(HttpClient http, string model)
        {
            _http = http;
            _model = model;
        }

        public async Task<ExtractedCv> ExtractAsync(string cvText, string targetRolePrompt, CancellationToken ct)
        {
            var prompt =
    $"""
You are an ATS parser for technical resumes.

Target role: {targetRolePrompt}

TASK:
Extract structured data from the CV text and return ONLY raw JSON.

HARD RULES:
- Return ONLY JSON (no markdown, no ``` fences, no explanations).
- skills MUST be an array of objects with EXACT keys: name, years
  Example shape: skills = [ (name:"C#", years:5), (name:"ASP.NET Core", years:4) ]
- Never return skills as strings.
- If years are not clearly stated, estimate. If still unknown, set years = 0.5 (do NOT omit the skill).
- Include at least the TOP 15 skills you can infer.
- employment MUST be an array of objects with keys: company, title, start, end, summary

Return JSON fields exactly:
fullName, email, phone, location, githubUrl, linkedInUrl, totalYearsExperience,
skills, employment

CV TEXT:
---BEGIN---
{Truncate(NormalizeCvText(cvText), 18000)}
---END---
""";

            var req = new
            {
                model = _model,
                prompt,
                stream = false,
                // More compatible than schema with many models
                format = "json",
                options = new { temperature = 0.1 }
            };

            using var resp = await _http.PostAsJsonAsync("/api/generate", req, ct);
            resp.EnsureSuccessStatusCode();

            using var doc = await JsonDocument.ParseAsync(await resp.Content.ReadAsStreamAsync(ct), cancellationToken: ct);

            // Ollama returns a JSON object with "response" containing the model output string
            var responseText = doc.RootElement.TryGetProperty("response", out var r) ? r.GetString() : null;
            responseText ??= "{}";

            // Debug (optional) — keep while testing
            Console.WriteLine("===== OLLAMA RAW RESPONSE =====");
            Console.WriteLine(responseText);
            Console.WriteLine("===== END =====");

            var cleaned = CleanJson(responseText);
            var jsonObject = TryExtractFirstJsonObject(cleaned) ?? cleaned;

            using var jdoc = JsonDocument.Parse(jsonObject);
            var root = jdoc.RootElement;

            var fullName = GetString(root, "fullName") ?? "";
            var email = GetString(root, "email") ?? "";
            var phone = GetString(root, "phone") ?? "";
            var location = GetString(root, "location") ?? "";
            var githubUrl = NormalizeUrl(GetString(root, "githubUrl"));
            var linkedInUrl = NormalizeUrl(GetString(root, "linkedInUrl"));
            var totalYears = GetInt(root, "totalYearsExperience");

            // Read skills robustly
            var skills = ReadSkills(root);

            // Fallback: if still empty, do deterministic scan
            if (skills.Count == 0)
            {
                skills = FallbackSkillScan(cvText)
                    .Select(n => new ExtractedSkill(n, 0.5m))
                    .ToList();
            }
            else
            {
                // Merge in any missing known skills found in text (optional but helpful)
                var existing = new HashSet<string>(skills.Select(s => s.Name.Trim()), StringComparer.OrdinalIgnoreCase);
                foreach (var n in FallbackSkillScan(cvText))
                    if (existing.Add(n))
                        skills.Add(new ExtractedSkill(n, 0.5m));

                // de-dup again, keep max years
                skills = skills
                    .GroupBy(s => s.Name.Trim(), StringComparer.OrdinalIgnoreCase)
                    .Select(g => g.OrderByDescending(x => x.Years).First())
                    .ToList();
            }

            var employment = ReadEmployment(root);

            // If AI didn't compute total years, estimate from employment dates (best effort)
            if (totalYears <= 0)
                totalYears = EstimateTotalYearsFromEmployment(employment);

            return new ExtractedCv(
                fullName,
                email,
                phone,
                location,
                githubUrl,
                linkedInUrl,
                totalYears,
                skills,
                employment
            );
        }

        // ---------------------------
        // JSON parsing helpers
        // ---------------------------

        private static string? GetString(JsonElement root, string name)
        {
            if (root.ValueKind != JsonValueKind.Object) return null;
            if (!root.TryGetProperty(name, out var p)) return null;
            return p.ValueKind == JsonValueKind.String ? p.GetString() : null;
        }

        private static int GetInt(JsonElement root, string name)
        {
            if (root.ValueKind != JsonValueKind.Object) return 0;
            if (!root.TryGetProperty(name, out var p)) return 0;

            if (p.ValueKind == JsonValueKind.Number && p.TryGetInt32(out var n))
                return n;

            if (p.ValueKind == JsonValueKind.String && int.TryParse(p.GetString(), out n))
                return n;

            // try decimal->int
            if (p.ValueKind == JsonValueKind.Number && p.TryGetDecimal(out var d))
                return (int)Math.Round(d);

            return 0;
        }

        /// <summary>
        /// Accept multiple shapes:
        /// 1) skills: [{ "name": "C#", "years": 3 }]
        /// 2) skills: [{ "skill": "C#", "yrs": 3 }] (normalized)
        /// 3) skills: ["C#", "SQL"] (normalized)
        /// </summary>
        private static List<ExtractedSkill> ReadSkills(JsonElement root)
        {
            if (!root.TryGetProperty("skills", out var skillsEl) || skillsEl.ValueKind != JsonValueKind.Array)
                return new List<ExtractedSkill>();

            var list = new List<ExtractedSkill>();

            foreach (var item in skillsEl.EnumerateArray())
            {
                if (item.ValueKind == JsonValueKind.String)
                {
                    var n = item.GetString();
                    if (!string.IsNullOrWhiteSpace(n))
                        list.Add(new ExtractedSkill(n.Trim(), 0.5m));
                    continue;
                }

                if (item.ValueKind != JsonValueKind.Object)
                    continue;

                var name =
                    GetString(item, "name") ??
                    GetString(item, "skill") ??
                    GetString(item, "skillName") ??
                    GetString(item, "technology");

                if (string.IsNullOrWhiteSpace(name))
                    continue;

                decimal years = 0.5m; // default if unknown

                years = ReadDecimal(item, "years")
                        ?? ReadDecimal(item, "yrs")
                        ?? ReadDecimal(item, "experienceYears")
                        ?? 0.5m;

                // clamp silly outputs
                if (years < 0) years = 0.5m;
                if (years > 60) years = 60;

                list.Add(new ExtractedSkill(name.Trim(), years));
            }

            // De-dup, keep max years
            return list
                .GroupBy(s => s.Name.Trim(), StringComparer.OrdinalIgnoreCase)
                .Select(g => g.OrderByDescending(x => x.Years).First())
                .ToList();
        }

        private static decimal? ReadDecimal(JsonElement obj, string prop)
        {
            if (obj.ValueKind != JsonValueKind.Object) return null;
            if (!obj.TryGetProperty(prop, out var p)) return null;

            if (p.ValueKind == JsonValueKind.Number && p.TryGetDecimal(out var d))
                return d;

            if (p.ValueKind == JsonValueKind.String)
            {
                var s = p.GetString();
                if (string.IsNullOrWhiteSpace(s)) return null;

                // handle "3+", "5 yrs" etc.
                s = new string(s.Where(ch => char.IsDigit(ch) || ch == '.' || ch == ',').ToArray())
                    .Replace(',', '.');

                if (decimal.TryParse(s, out d))
                    return d;
            }

            return null;
        }

        private static IReadOnlyList<ExtractedJob> ReadEmployment(JsonElement root)
        {
            if (!root.TryGetProperty("employment", out var empEl) || empEl.ValueKind != JsonValueKind.Array)
                return Array.Empty<ExtractedJob>();

            var list = new List<ExtractedJob>();

            foreach (var item in empEl.EnumerateArray())
            {
                if (item.ValueKind != JsonValueKind.Object) continue;

                var company = GetString(item, "company") ?? "";
                var title = GetString(item, "title") ?? "";

                var start = GetString(item, "start") ?? GetString(item, "startDate");
                var end = GetString(item, "end") ?? GetString(item, "endDate");

                var summary = GetString(item, "summary") ?? "";

                if (string.IsNullOrWhiteSpace(company) && string.IsNullOrWhiteSpace(title))
                    continue;

                list.Add(new ExtractedJob(company, title, start, end, summary));
            }

            return list;
        }

        // ---------------------------
        // Text + JSON cleanup
        // ---------------------------

        private static string NormalizeCvText(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return "";

            text = text.Replace("\r", "\n");

            // remove hyphen line breaks: "ASP-\nNET" => "ASPNET" (AI can still infer)
            text = text.Replace("-\n", "");

            // trim lines and drop empties
            var lines = text.Split('\n')
                .Select(l => l.Trim())
                .Where(l => l.Length > 0);

            text = string.Join("\n", lines);

            while (text.Contains("  "))
                text = text.Replace("  ", " ");

            return text;
        }

        private static string CleanJson(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return "{}";
            s = s.Trim();

            // Remove ```json ... ``` or ``` ... ```
            if (s.StartsWith("```", StringComparison.Ordinal))
            {
                var firstNewLine = s.IndexOf('\n');
                if (firstNewLine >= 0)
                    s = s[(firstNewLine + 1)..];

                var lastFence = s.LastIndexOf("```", StringComparison.Ordinal);
                if (lastFence >= 0)
                    s = s[..lastFence];

                s = s.Trim();
            }

            // Remove "JSON:" prefix etc.
            var idx = s.IndexOf('{');
            if (idx > 0 && s[..idx].Any(char.IsLetter))
                s = s[idx..];

            return s.Trim();
        }

        private static string? TryExtractFirstJsonObject(string s)
        {
            var start = s.IndexOf('{');
            if (start < 0) return null;

            int depth = 0;
            for (int i = start; i < s.Length; i++)
            {
                if (s[i] == '{') depth++;
                else if (s[i] == '}')
                {
                    depth--;
                    if (depth == 0)
                        return s.Substring(start, i - start + 1);
                }
            }
            return null;
        }

        private static string Truncate(string s, int max)
            => s.Length <= max ? s : s[..max];

        private static string? NormalizeUrl(string? url)
        {
            if (string.IsNullOrWhiteSpace(url)) return null;
            url = url.Trim();
            return Uri.TryCreate(url, UriKind.Absolute, out _) ? url : null;
        }

        // ---------------------------
        // Deterministic fallback skill scan
        // ---------------------------

        private static IReadOnlyList<string> FallbackSkillScan(string text)
        {
            var keywords = new[]
            {
            "C#", ".NET", ".NET Core", ".NET 6", ".NET 7", ".NET 8",
            "ASP.NET", "ASP.NET Core", "Web API", "REST", "gRPC",
            "Entity Framework", "EF Core", "Dapper",
            "SQL", "SQL Server", "SQLite", "PostgreSQL", "MySQL",
            "Azure", "AWS", "GCP",
            "Docker", "Kubernetes",
            "Blazor", "Razor Pages", "MVC",
            "JavaScript", "TypeScript", "React", "Angular", "Vue",
            "HTML", "CSS",
            "Git", "GitHub", "CI/CD",
            "Microservices", "Clean Architecture"
        };

            var normalized = (text ?? "").ToLowerInvariant();
            var found = new List<string>();

            foreach (var k in keywords)
                if (normalized.Contains(k.ToLowerInvariant()))
                    found.Add(k);

            return found
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Take(20)
                .ToList();
        }

        private static int EstimateTotalYearsFromEmployment(IReadOnlyList<ExtractedJob> jobs)
        {
            // best-effort: if we can parse dates, sum ranges (not perfect with overlaps)
            DateTime? min = null;
            DateTime? max = null;

            foreach (var j in jobs)
            {
                var s = ParseDate(j.Start);
                var e = ParseDate(j.End) ?? DateTime.UtcNow;

                if (s is null) continue;

                min = min is null ? s : (s < min ? s : min);
                max = max is null ? e : (e > max ? e : max);
            }

            if (min is null || max is null || max <= min) return 0;

            var totalDays = (max.Value - min.Value).TotalDays;
            var years = totalDays / 365.25;
            return (int)Math.Round(years);
        }

        private static DateTime? ParseDate(string? s)
        {
            if (string.IsNullOrWhiteSpace(s)) return null;

            // accept "YYYY-MM" or "YYYY-MM-DD" or normal dates
            s = s.Trim();

            if (DateTime.TryParse(s, out var dt))
                return dt;

            if (s.Length == 7 && DateTime.TryParse(s + "-01", out dt))
                return dt;

            return null;
        }
    }
}
