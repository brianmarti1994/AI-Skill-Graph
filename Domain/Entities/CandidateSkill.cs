using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entities
{
    public sealed class CandidateSkill
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid CandidateId { get; set; }
        public string Name { get; set; } = "";
        public decimal Years { get; set; }
    }
}
