using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Models
{
    public sealed record AnalyzeCvResponse(
     Guid CandidateId,
     string FullName,
     int TotalYearsExperience,
     decimal MatchPercentage,
     string TrafficLight, // Green/Yellow/Red
     IReadOnlyList<SkillBarPoint> SkillBars,
     IReadOnlyList<GithubRepoSummary> GithubRepos
 );

    public sealed record SkillBarPoint(string Skill, decimal Years);

    public sealed record GithubRepoSummary(string Name, string Url, IReadOnlyDictionary<string, long> Languages);
}
