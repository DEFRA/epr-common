﻿// <auto-generated />
namespace EPR.Common.Data.Tables.Audit
{
    using System;
    using EPR.Common.Sample.Data.Tables;
    using EPR.Common.Functions.Database.Entities.Interfaces;
using Functions.Database;
using Functions.Database.Entities;
using Functions.Database.Entities.Interfaces;

    public class OrgApplicationAudit : AuditEntityBase, IAudit<OrgApplication>
    {
        public Guid _CustomerOrganisationId { get; set; }

        public Guid _CustomerId { get; set; }

        public ICollection<OrgUser> _Users { get; set; }

        public DateTime _Created { get; set; }

        public DateTime _LastUpdated { get; set; }
    }
}
