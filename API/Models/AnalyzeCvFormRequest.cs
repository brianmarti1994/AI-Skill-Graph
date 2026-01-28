using Microsoft.AspNetCore.Mvc;

namespace API.Models
{
    public sealed class AnalyzeCvFormRequest
    {
        [FromForm(Name = "file")]
        public IFormFile File { get; set; } = default!;

        [FromForm(Name = "targetRolePrompt")]
        public string TargetRolePrompt { get; set; } = string.Empty;

        [FromForm(Name = "mustHaveCsv")]
        public string? MustHaveCsv { get; set; }
    }
}
