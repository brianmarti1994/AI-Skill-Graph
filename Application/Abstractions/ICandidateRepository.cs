using Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Abstractions
{
    public interface ICandidateRepository
    {
        Task AddAsync(Candidate candidate, CancellationToken ct);
        Task<Candidate?> GetAsync(Guid id, CancellationToken ct);
    }
}
