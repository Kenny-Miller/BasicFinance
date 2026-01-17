using System;
using System.Collections.Generic;
using System.Text;
using BasicFinance.Domain.Interfaces;

namespace BasicFinance.Domain.Entities
{
    public class DataSpreadsheet: IEntity
    {
        public Guid Id { get; init; }
        public required string UserId { get; init; }
        public DateTimeOffset SystemCreatedDate { get; init; }
        public DateTimeOffset SystemModifiedDate { get; set; }
        public bool IsActive { get; set; }
    }
}
