using Application.Abstractions;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Persistence
{
    public sealed class CandidateRepository : ICandidateRepository
    {
        private readonly AppDbContext _db;
        public CandidateRepository(AppDbContext db) => _db = db;

        public async Task AddAsync(Candidate candidate, CancellationToken ct)
        {
            _db.Candidates.Add(candidate);
            await _db.SaveChangesAsync(ct);
        }

        public Task<Candidate?> GetAsync(Guid id, CancellationToken ct)
            => _db.Candidates
                .Include(c => c.Skills)
                .Include(c => c.EmploymentHistory)
                .FirstOrDefaultAsync(c => c.Id == id, ct);
    }
}
