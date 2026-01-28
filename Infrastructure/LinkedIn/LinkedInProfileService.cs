using Application.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.LinkedIn
{
    // Stub: real implementation requires LinkedIn OAuth + approved API access.
    public sealed class LinkedInProfileService : ILinkedInProfileService
    {
        public Task<LinkedInProfile?> TryFetchAsync(string linkedInUrl, CancellationToken ct)
            => Task.FromResult<LinkedInProfile?>(null);
    }
}
