using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entities
{
    public sealed class Candidate
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string FullName { get; set; } = "";
        public string Email { get; set; } = "";
        public string Phone { get; set; } = "";
        public string Location { get; set; } = "";

        public string? GithubUrl { get; set; }
        public string? LinkedInUrl { get; set; }

        public int TotalYearsExperience { get; set; }

        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

        public List<CandidateSkill> Skills { get; set; } = new();
        public List<Employment> EmploymentHistory { get; set; } = new();
    }
}
