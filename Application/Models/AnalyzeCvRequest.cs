using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Models
{
    public sealed record AnalyzeCvRequest(
    string TargetRolePrompt,     // e.g. ".NET developer"
    string? MustHaveCsv = null   // optional: "C#,ASP.NET Core,EF Core,SQL"
);
}
