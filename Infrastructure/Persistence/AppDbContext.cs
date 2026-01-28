using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Persistence
{
    public sealed class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Candidate> Candidates => Set<Candidate>();
        public DbSet<CandidateSkill> CandidateSkills => Set<CandidateSkill>();
        public DbSet<Employment> Employment => Set<Employment>();

        protected override void OnModelCreating(ModelBuilder b)
        {
            b.Entity<Candidate>().HasKey(x => x.Id);

            b.Entity<Candidate>()
                .HasMany(x => x.Skills)
                .WithOne()
                .HasForeignKey(s => s.CandidateId)
                .OnDelete(DeleteBehavior.Cascade);

            b.Entity<Candidate>()
                .HasMany(x => x.EmploymentHistory)
                .WithOne()
                .HasForeignKey(e => e.CandidateId)
                .OnDelete(DeleteBehavior.Cascade);

            b.Entity<CandidateSkill>().HasKey(x => x.Id);
            b.Entity<Employment>().HasKey(x => x.Id);
        }
    }
}
