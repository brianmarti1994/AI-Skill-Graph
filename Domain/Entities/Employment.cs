using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entities
{
    public sealed class Employment
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid CandidateId { get; set; }

        public string Company { get; set; } = "";
        public string Title { get; set; } = "";

        public DateOnly? StartDate { get; set; }
        public DateOnly? EndDate { get; set; } // null = current
        public string Summary { get; set; } = "";
    }
}
