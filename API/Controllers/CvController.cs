using API.Models;
using Application.Abstractions;
using Application.Models;
using Application.Services;
using Infrastructure.CvText;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [ApiController]
    [Route("api/cv")]
    public sealed class CvController : ControllerBase
    {
        private readonly CvTextExtractor _textExtractor;
        private readonly IAiCvExtractor _ai;
        private readonly ICandidateRepository _repo;
        private readonly IGithubScanner _github;
        private readonly MatchingService _matcher;

        public CvController(
            CvTextExtractor textExtractor,
            IAiCvExtractor ai,
            ICandidateRepository repo,
            IGithubScanner github,
            MatchingService matcher)
        {
            _textExtractor = textExtractor;
            _ai = ai;
            _repo = repo;
            _github = github;
            _matcher = matcher;
        }

        /// <summary>
        /// Upload CV (PDF/DOCX/TXT) + target role prompt => AI extraction + match score + GitHub scan.
        /// </summary>
        [HttpPost("analyze")]
        [Consumes("multipart/form-data")]
       // [RequestSizeLimit(15 * 1024 * 1024)]
        public async Task<ActionResult<AnalyzeCvResponse>> Analyze(
       [FromForm] AnalyzeCvFormRequest request,
       CancellationToken ct)
        {
            if (request.File is null || request.File.Length == 0)
                return BadRequest("Empty file.");

            if (string.IsNullOrWhiteSpace(request.TargetRolePrompt))
                return BadRequest("targetRolePrompt is required.");

            await using var stream = request.File.OpenReadStream();
            var cvText = await _textExtractor.ExtractAsync(stream, request.File.FileName, ct);

            var extracted = await _ai.ExtractAsync(cvText, request.TargetRolePrompt, ct);

            var candidate = new Domain.Entities.Candidate
            {
                FullName = extracted.FullName,
                Email = extracted.Email,
                Phone = extracted.Phone,
                Location = extracted.Location,
                GithubUrl = extracted.GithubUrl,
                LinkedInUrl = extracted.LinkedInUrl,
                TotalYearsExperience = extracted.TotalYearsExperience,
                Skills = extracted.Skills.Select(s => new Domain.Entities.CandidateSkill
                {
                    Name = s.Name,
                    Years = s.Years
                }).ToList(),
                EmploymentHistory = extracted.Employment.Select(e => new Domain.Entities.Employment
                {
                    Company = e.Company,
                    Title = e.Title,
                    StartDate = TryDateOnly(e.Start),
                    EndDate = TryDateOnly(e.End),
                    Summary = e.Summary
                }).ToList()
            };

            await _repo.AddAsync(candidate, ct);

            var skillBars = candidate.Skills
                .OrderByDescending(s => s.Years)
                .Select(s => new Application.Models.SkillBarPoint(s.Name, s.Years))
                .ToList();

            var (pct, light) = _matcher.Score(skillBars, request.TargetRolePrompt, request.MustHaveCsv);

            var repos = Array.Empty<Application.Models.GithubRepoSummary>();
            if (!string.IsNullOrWhiteSpace(candidate.GithubUrl))
                repos = (await _github.ScanAsync(candidate.GithubUrl!, ct)).ToArray();

            var response = new Application.Models.AnalyzeCvResponse(
                candidate.Id,
                candidate.FullName,
                candidate.TotalYearsExperience,
                pct,
                light,
                skillBars,
                repos
            );

            return Ok(response);
        }

        private static DateOnly? TryDateOnly(string? s)
        {
            if (string.IsNullOrWhiteSpace(s)) return null;
            s = s.Trim();

            if (DateTime.TryParse(s, out var dt))
                return DateOnly.FromDateTime(dt);

            if (s.Length == 7 && DateTime.TryParse(s + "-01", out dt))
                return DateOnly.FromDateTime(dt);

            return null;
        }
    }
}
