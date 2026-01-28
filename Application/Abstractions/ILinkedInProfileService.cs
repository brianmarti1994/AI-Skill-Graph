using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Abstractions
{
    public interface ILinkedInProfileService
    {
        Task<LinkedInProfile?> TryFetchAsync(string linkedInUrl, CancellationToken ct);
    }

    public sealed record LinkedInProfile(string Headline, string Location);
}
