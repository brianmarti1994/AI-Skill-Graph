using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Abstractions
{
    public interface IAiCvExtractor
    {
        Task<ExtractedCv> ExtractAsync(string cvText, string targetRolePrompt, CancellationToken ct);
    }

    public sealed record ExtractedCv(
        string FullName,
        string Email,
        string Phone,
        string Location,
        string? GithubUrl,
        string? LinkedInUrl,
        int TotalYearsExperience,
        IReadOnlyList<ExtractedSkill> Skills,
        IReadOnlyList<ExtractedJob> Employment
    );

    public sealed record ExtractedSkill(string Name, decimal Years);

    public sealed record ExtractedJob(
        string Company,
        string Title,
        string? Start,  // "YYYY-MM" or "YYYY-MM-DD" or null
        string? End,    // null/current
        string Summary
    );
}
