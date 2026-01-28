using Application.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Services
{
    public sealed class MatchingService
    {
        public (decimal matchPct, string light) Score(
            IReadOnlyList<SkillBarPoint> candidateSkills,
            string targetRolePrompt,
            string? mustHaveCsv
        )
        {
            var must = ParseMustHaves(targetRolePrompt, mustHaveCsv);

            if (must.Count == 0)
                return (50m, "Yellow");

            var dict = candidateSkills
                .GroupBy(s => Normalize(s.Skill))
                .ToDictionary(g => g.Key, g => g.Max(x => x.Years));

            int hitCount = 0;
            decimal weighted = 0m;

            foreach (var m in must)
            {
                if (dict.TryGetValue(Normalize(m), out var yrs) && yrs > 0)
                {
                    hitCount++;
                    weighted += Math.Min(1m, yrs / 2m); // 2 years => full credit
                }
            }

            var pct = Math.Round((weighted / must.Count) * 100m, 2);

            // Traffic rule requested:
            // > 6 green, < 6 yellow, < 3 red
            // Interpreting as hit count thresholds:
            //string light =
            //    hitCount > 6 ? "Green" :
            //    hitCount < 3 ? "Red" :
            //    "Yellow";
            string light =
               pct > 80 ? "Green" :
               pct < 60 ? "Red" :
               "Yellow";

            return (pct, light);
        }

        private static List<string> ParseMustHaves(string targetRolePrompt, string? mustHaveCsv)
        {
            if (!string.IsNullOrWhiteSpace(mustHaveCsv))
                return mustHaveCsv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();

            // sensible defaults for ".NET developer"
            var p = targetRolePrompt.ToLowerInvariant();
            if (p.Contains(".net") || p.Contains("dotnet") || p.Contains("c#"))
            {
                return new List<string>
            {
                "C#", ".NET", "ASP.NET Core", "Web API", "Entity Framework", "SQL",
                "LINQ", "REST", "Git", "Azure", "Docker"
            };
            }

            return new();
        }

        private static string Normalize(string s)
            => new string(s.Trim().ToLowerInvariant().Where(char.IsLetterOrDigit).ToArray());
    }
}
