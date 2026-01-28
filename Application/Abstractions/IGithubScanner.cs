using Application.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Abstractions
{
    public interface IGithubScanner
    {
        Task<IReadOnlyList<GithubRepoSummary>> ScanAsync(string githubUrl, CancellationToken ct);
    }
}
