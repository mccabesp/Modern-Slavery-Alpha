﻿using System.Collections.Generic;

namespace ModernSlavery.BusinessLogic.Models.Scope
{
    public class OrganisationMissingScope
    {
        public Entities.Organisation Organisation { get; set; }

        public List<int> MissingSnapshotYears { get; set; }
    }
}