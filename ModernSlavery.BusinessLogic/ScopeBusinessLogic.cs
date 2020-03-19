﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using ModernSlavery.BusinessLogic.Models.FileModels;
using ModernSlavery.BusinessLogic.Models.Scope;
using ModernSlavery.Core;
using ModernSlavery.Core.Classes.ErrorMessages;
using ModernSlavery.Core.Interfaces;
using ModernSlavery.Entities;
using ModernSlavery.Extensions;
using ModernSlavery.Entities.Enums;
using ModernSlavery.SharedKernel.Options;

namespace ModernSlavery.BusinessLogic
{

    public interface IScopeBusinessLogic
    {

        // scope repo
        Task<OrganisationScope> GetScopeByIdAsync(long organisationScopeId);
        OrganisationScope GetLatestScopeBySnapshotYear(Organisation organisation, int snapshotYear = 0);
        Task<OrganisationScope> GetLatestScopeBySnapshotYearAsync(long organisationId, int snapshotYear = 0);
        Task<OrganisationScope> GetPendingScopeRegistrationAsync(string emailAddress);
        Task SaveScopeAsync(Organisation org, bool saveToDatabase = true, params OrganisationScope[] newScopes);
        Task SaveScopesAsync(Organisation org, IEnumerable<OrganisationScope> newScopes, bool saveToDatabase = true);

        // nso repo
        Task<OrganisationScope> GetScopeByEmployerReferenceAsync(string employerReference, int snapshotYear = 0);

        // business logic
        Task<ScopeStatuses> GetLatestScopeStatusForSnapshotYearAsync(long organisationId, int snapshotYear = 0);
        ScopeStatuses GetLatestScopeStatusForSnapshotYear(Organisation org, int snapshotYear = 0);
        Task<OrganisationScope> UpdateScopeStatusAsync(long existingOrgScopeId, ScopeStatuses newStatus);

        Task<CustomResult<OrganisationScope>> AddScopeAsync(Organisation organisation,
            ScopeStatuses newStatus,
            User currentUser,
            int snapshotYear,
            string comment,
            bool saveToDatabase);

        IEnumerable<ScopesFileModel> GetScopesFileModelByYear(int year);
        Task<HashSet<Organisation>> SetPresumedScopesAsync();

        Task<HashSet<OrganisationMissingScope>> FindOrgsWhereScopeNotSetAsync();
        bool FillMissingScopes(Organisation org);

        Task<HashSet<Organisation>> SetScopeStatusesAsync();
        OrganisationScope SetPresumedScope(Organisation org, ScopeStatuses scopeStatus, DateTime snapshotDate, User user = null);

    }

    public class ScopeBusinessLogic : IScopeBusinessLogic
    {

        private readonly ICommonBusinessLogic _commonBusinessLogic;
        private readonly ISearchBusinessLogic _searchBusinessLogic;
        private readonly GlobalOptions GlobalOptions;
        public ScopeBusinessLogic(ICommonBusinessLogic commonBusinessLogic,
            IDataRepository dataRepo,
            ISearchBusinessLogic searchBusinessLogic,
            GlobalOptions globalOptions)
        {
            _commonBusinessLogic = commonBusinessLogic;
            _searchBusinessLogic = searchBusinessLogic;
            DataRepository = dataRepo;
            GlobalOptions = globalOptions;
        }

        private IDataRepository DataRepository { get; }

        /// <summary>
        ///     Returns the latest scope status for an organisation and snapshot year
        /// </summary>
        /// <param name="organisationId"></param>
        /// <param name="snapshotYear"></param>
        public virtual async Task<ScopeStatuses> GetLatestScopeStatusForSnapshotYearAsync(long organisationId, int snapshotYear)
        {
            OrganisationScope latestScope = await GetLatestScopeBySnapshotYearAsync(organisationId, snapshotYear);
            if (latestScope == null)
            {
                return ScopeStatuses.Unknown;
            }

            return latestScope.ScopeStatus;
        }

        /// <summary>
        ///     Returns the latest scope status for an organisation and snapshot year
        /// </summary>
        /// <param name="organisationId"></param>
        /// <param name="snapshotYear"></param>
        public virtual ScopeStatuses GetLatestScopeStatusForSnapshotYear(Organisation org, int snapshotYear = 0)
        {
            OrganisationScope latestScope = GetLatestScopeBySnapshotYear(org, snapshotYear);
            if (latestScope == null)
            {
                return ScopeStatuses.Unknown;
            }

            return latestScope.ScopeStatus;
        }

        /// <summary>
        ///     Returns the latest scope for an organisation
        /// </summary>
        /// <param name="employerReference"></param>
        public virtual async Task<OrganisationScope> GetScopeByEmployerReferenceAsync(string employerReference, int snapshotYear = 0)
        {
            Organisation org = await GetOrgByEmployerReferenceAsync(employerReference);
            if (org == null)
            {
                return null;
            }

            return GetLatestScopeBySnapshotYear(org, snapshotYear);
        }

        /// <summary>
        ///     Creates a new scope record using an existing scope and applies a new status
        /// </summary>
        /// <param name="existingOrgScopeId"></param>
        /// <param name="newStatus"></param>
        public virtual async Task<OrganisationScope> UpdateScopeStatusAsync(long existingOrgScopeId, ScopeStatuses newStatus)
        {
            OrganisationScope oldOrgScope = await DataRepository.FirstOrDefaultAsync<OrganisationScope>(os => os.OrganisationScopeId == existingOrgScopeId);

            // when OrganisationScope isn't found then throw ArgumentOutOfRangeException
            if (oldOrgScope == null)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(existingOrgScopeId),
                    $"Cannot find organisation with OrganisationScopeId: {existingOrgScopeId}");
            }

            Organisation org = await DataRepository.FirstOrDefaultAsync<Organisation>(o => o.OrganisationId == oldOrgScope.OrganisationId);
            // when Organisation isn't found then throw ArgumentOutOfRangeException
            if (org == null)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(oldOrgScope.OrganisationId),
                    $"Cannot find organisation with OrganisationId: {oldOrgScope.OrganisationId}");
            }

            // When Organisation is Found Then Save New Scope Record With New Status
            var newScope = new OrganisationScope {
                OrganisationId = oldOrgScope.OrganisationId,
                ContactEmailAddress = oldOrgScope.ContactEmailAddress,
                ContactFirstname = oldOrgScope.ContactFirstname,
                ContactLastname = oldOrgScope.ContactLastname,
                ReadGuidance = oldOrgScope.ReadGuidance,
                Reason = oldOrgScope.Reason,
                ScopeStatus = newStatus,
                ScopeStatusDate = VirtualDateTime.Now,
                RegisterStatus = oldOrgScope.RegisterStatus,
                RegisterStatusDate = oldOrgScope.RegisterStatusDate,
                // carry the snapshot date over
                SnapshotDate = oldOrgScope.SnapshotDate
            };

            await SaveScopeAsync(org, true, newScope);
            return newScope;
        }

        public virtual async Task<CustomResult<OrganisationScope>> AddScopeAsync(Organisation organisation,
            ScopeStatuses newStatus,
            User currentUser,
            int snapshotYear,
            string comment,
            bool saveToDatabase)
        {
            snapshotYear = organisation
                .GetAccountingStartDate(snapshotYear)
                .Year;

            OrganisationScope oldOrgScope = organisation.GetLatestScopeForSnapshotYearOrThrow(snapshotYear);

            if (oldOrgScope.ScopeStatus == newStatus)
            {
                return new CustomResult<OrganisationScope>(
                    InternalMessages.SameScopesCannotBeUpdated(newStatus, oldOrgScope.ScopeStatus, snapshotYear));
            }

            // When Organisation is Found Then Save New Scope Record With New Status
            var newScope = new OrganisationScope {
                OrganisationId = oldOrgScope.OrganisationId,
                Organisation = organisation,
                /* Updated by the current user */
                ContactEmailAddress = currentUser.EmailAddress,
                ContactFirstname = currentUser.Firstname,
                ContactLastname = currentUser.Lastname,
                ReadGuidance = oldOrgScope.ReadGuidance,
                Reason = !string.IsNullOrEmpty(comment)
                    ? comment
                    : oldOrgScope.Reason,
                ScopeStatus = newStatus,
                ScopeStatusDate = VirtualDateTime.Now,
                StatusDetails = currentUser.IsAdministrator()
                    ? "Changed by Admin"
                    : null,
                RegisterStatus = oldOrgScope.RegisterStatus,
                RegisterStatusDate = oldOrgScope.RegisterStatusDate,
                // carry the snapshot date over
                SnapshotDate = oldOrgScope.SnapshotDate
            };

            await SaveScopeAsync(organisation, saveToDatabase, newScope);

            return new CustomResult<OrganisationScope>(newScope);
        }

        public virtual async Task SaveScopeAsync(Organisation org, bool saveToDatabase = true, params OrganisationScope[] newScopes)
        {
            await SaveScopesAsync(org, newScopes, saveToDatabase);
        }

        public virtual async Task SaveScopesAsync(Organisation org, IEnumerable<OrganisationScope> newScopes, bool saveToDatabase = true)
        {
            foreach (OrganisationScope newScope in newScopes.OrderBy(s => s.SnapshotDate).ThenBy(s => s.ScopeStatusDate))
            {
                // find any prev submitted scopes in the same snapshot year year and retire them
                org.OrganisationScopes
                    .Where(x => x.Status == ScopeRowStatuses.Active && x.SnapshotDate.Year == newScope.SnapshotDate.Year)
                    .ToList()
                    .ForEach(x => x.Status = ScopeRowStatuses.Retired);

                // add the new scope
                newScope.Status = ScopeRowStatuses.Active;
                org.OrganisationScopes.Add(newScope);
                if (org.LatestScope == null
                    || newScope.SnapshotDate > org.LatestScope.SnapshotDate
                    || newScope.SnapshotDate == org.LatestScope.SnapshotDate && newScope.ScopeStatusDate >= org.LatestScope.ScopeStatusDate)
                {
                    org.LatestScope = newScope;
                }
            }

            // save to db
            if (saveToDatabase)
            {
                await DataRepository.SaveChangesAsync();
                ////Update the search index
                await _searchBusinessLogic.UpdateSearchIndexAsync(org);
            }
        }

        public virtual IEnumerable<ScopesFileModel> GetScopesFileModelByYear(int year)
        {
            IQueryable<OrganisationScope> scopes = DataRepository.GetAll<OrganisationScope>()
                .Where(s => s.SnapshotDate.Year == year && s.Status == ScopeRowStatuses.Active);

#if DEBUG
            if (Debugger.IsAttached)
            {
                scopes = scopes.Take(100);
            }
#endif
            IQueryable<ScopesFileModel> records = scopes.Select(
                o => new ScopesFileModel {
                    OrganisationId = o.OrganisationId,
                    OrganisationName = o.Organisation.OrganisationName,
                    DUNSNumber = o.Organisation.DUNSNumber,
                    EmployerReference = o.Organisation.EmployerReference,
                    OrganisationScopeId = o.OrganisationScopeId,
                    ScopeStatus = o.ScopeStatus,
                    ScopeStatusDate = o.ScopeStatusDate,
                    RegisterStatus = o.RegisterStatus,
                    RegisterStatusDate = o.RegisterStatusDate,
                    ContactEmailAddress = o.ContactEmailAddress,
                    ContactFirstname = o.ContactFirstname,
                    ContactLastname = o.ContactLastname,
                    ReadGuidance = o.ReadGuidance,
                    Reason = o.Reason,
                    CampaignId = o.CampaignId
                });

            return records;
        }

        public async Task<HashSet<Organisation>> SetScopeStatusesAsync()
        {
            DateTime lastSnapshotDate = DateTime.MinValue;
            long lastOrganisationId = -1;
            int index = -1;
            var count = 0;
            IOrderedQueryable<OrganisationScope> scopes = DataRepository.GetAll<OrganisationScope>()
                .OrderBy(os => os.SnapshotDate)
                .ThenBy(os => os.OrganisationId)
                .ThenByDescending(os => os.ScopeStatusDate);
            var changedOrgs = new HashSet<Organisation>();
            foreach (OrganisationScope scope in scopes)
            {
                count++;
                if (lastSnapshotDate != scope.SnapshotDate || lastOrganisationId != scope.OrganisationId)
                {
                    index = 0;
                }
                else
                {
                    index++;
                }

                //Set the status
                ScopeRowStatuses newStatus = index == 0 ? ScopeRowStatuses.Active : ScopeRowStatuses.Retired;
                if (scope.Status != newStatus)
                {
                    scope.Status = newStatus;
                    changedOrgs.Add(scope.Organisation);
                }

                lastSnapshotDate = scope.SnapshotDate;
                lastOrganisationId = scope.OrganisationId;
            }

            await DataRepository.SaveChangesAsync();

            return changedOrgs;
        }

        public async Task<HashSet<Organisation>> SetPresumedScopesAsync()
        {
            HashSet<OrganisationMissingScope> missingOrgs = await FindOrgsWhereScopeNotSetAsync();
            var changedOrgs = new HashSet<Organisation>();

            foreach (OrganisationMissingScope org in missingOrgs)
            {
                if (FillMissingScopes(org.Organisation))
                {
                    changedOrgs.Add(org.Organisation);
                }
            }

            if (changedOrgs.Count > 0)
            {
                await DataRepository.SaveChangesAsync();
            }

            return changedOrgs;
        }

        public bool FillMissingScopes(Organisation org)
        {
            int firstYear = GlobalOptions.FirstReportingYear;
            DateTime currentSnapshotDate = _commonBusinessLogic.GetAccountingStartDate(org.SectorType);
            int currentSnapshotYear = currentSnapshotDate.Year;
            var prevYearScope = ScopeStatuses.Unknown;
            var neverDeclaredScope = true;
            var changed = false;

            for (int snapshotYear = firstYear; snapshotYear <= currentSnapshotYear; snapshotYear++)
            {
                OrganisationScope scope = org.GetLatestScopeForSnapshotYear(snapshotYear);

                // if we already have a scope then flag (prevYearScope, neverDeclaredScope) and skip this year
                if (scope != null && scope.ScopeStatus != ScopeStatuses.Unknown)
                {
                    prevYearScope = scope.ScopeStatus;
                    neverDeclaredScope = false;
                    continue;
                }

                // determine the snapshot date from year
                var snapshotDate = new DateTime(snapshotYear, currentSnapshotDate.Month, currentSnapshotDate.Day);

                // determine if need to presume scope
                bool shouldPresumeScope = neverDeclaredScope
                                          && (prevYearScope == ScopeStatuses.PresumedOutOfScope
                                              || prevYearScope == ScopeStatuses.PresumedInScope
                                              || prevYearScope == ScopeStatuses.Unknown);

                // presumed scope from created date
                if (shouldPresumeScope)
                {
                    bool createdAfterSnapshotYear = org.Created >= snapshotDate.AddYears(1);
                    if (createdAfterSnapshotYear)
                    {
                        prevYearScope = ScopeStatuses.PresumedOutOfScope;
                    }
                    else
                    {
                        prevYearScope = ScopeStatuses.PresumedInScope;
                    }
                }
                // otherwise presume scope from declared scope
                else if (prevYearScope == ScopeStatuses.InScope)
                {
                    prevYearScope = ScopeStatuses.PresumedInScope;
                }
                else if (prevYearScope == ScopeStatuses.OutOfScope)
                {
                    prevYearScope = ScopeStatuses.PresumedOutOfScope;
                }

                // update the scope status
                SetPresumedScope(org, prevYearScope, snapshotDate);

                changed = true;
            }

            // set the latest scope if not set
            if (org.LatestScope == null)
            {
                org.LatestScope = org.GetCurrentScope();
                changed = true;
            }

            return changed;
        }

        public async Task<HashSet<OrganisationMissingScope>> FindOrgsWhereScopeNotSetAsync()
        {
            // get all orgs of any status
            var allOrgs = await DataRepository.ToListAsync<Organisation>();

            int firstYear = GlobalOptions.FirstReportingYear;

            // find all orgs who have no scope or unknown scope statuses
            var orgsWithMissingScope = new HashSet<OrganisationMissingScope>();
            foreach (Organisation org in allOrgs)
            {
                DateTime currentSnapshotDate = _commonBusinessLogic.GetAccountingStartDate(org.SectorType);
                int currentYear = currentSnapshotDate.Year;
                var missingSnapshotYears = new List<int>();

                // for all snapshot years check if scope exists
                for (int year = firstYear; year <= currentYear; year++)
                {
                    OrganisationScope scope = org.GetLatestScopeForSnapshotYear(year);
                    if (scope == null || scope.ScopeStatus == ScopeStatuses.Unknown)
                    {
                        missingSnapshotYears.Add(year);
                    }
                }

                // collect
                if (missingSnapshotYears.Count > 0)
                {
                    orgsWithMissingScope.Add(
                        new OrganisationMissingScope {Organisation = org, MissingSnapshotYears = missingSnapshotYears});
                }
            }

            return orgsWithMissingScope;
        }

        /// <summary>
        ///     Adds a new scope and updates the latest scope (if required)
        /// </summary>
        /// <param name="org"></param>
        /// <param name="scopeStatus"></param>
        /// <param name="snapshotDate"></param>
        /// <param name="currentUser"></param>
        public virtual OrganisationScope SetPresumedScope(Organisation org,
            ScopeStatuses scopeStatus,
            DateTime snapshotDate,
            User currentUser = null)
        {
            //Ensure scopestatus is presumed
            if (scopeStatus != ScopeStatuses.PresumedInScope && scopeStatus != ScopeStatuses.PresumedOutOfScope)
            {
                throw new ArgumentOutOfRangeException(nameof(scopeStatus));
            }

            //Check no previous scopes
            if (org.OrganisationScopes.Any(os => os.SnapshotDate == snapshotDate))
            {
                throw new ArgumentException(
                    $"A scope already exists for snapshot year {snapshotDate.Year} for organisation employer reference '{org.EmployerReference}'",
                    nameof(scopeStatus));
            }

            //Check for conflict with previous years scope
            if (snapshotDate.Year > GlobalOptions.FirstReportingYear)
            {
                ScopeStatuses previousScope = GetLatestScopeStatusForSnapshotYear(org, snapshotDate.Year - 1);
                if (previousScope == ScopeStatuses.InScope && scopeStatus == ScopeStatuses.PresumedOutOfScope
                    || previousScope == ScopeStatuses.OutOfScope && scopeStatus == ScopeStatuses.PresumedInScope)
                {
                    throw new ArgumentException(
                        $"Cannot set {scopeStatus} for snapshot year {snapshotDate.Year} when previos year was {previousScope} for organisation employer reference '{org.EmployerReference}'",
                        nameof(scopeStatus));
                }
            }

            var newScope = new OrganisationScope {
                OrganisationId = org.OrganisationId,
                ContactEmailAddress = currentUser?.EmailAddress,
                ContactFirstname = currentUser?.Firstname,
                ContactLastname = currentUser?.Lastname,
                ScopeStatus = scopeStatus,
                Status = ScopeRowStatuses.Active,
                StatusDetails = "Generated by the system",
                SnapshotDate = snapshotDate
            };

            org.OrganisationScopes.Add(newScope);

            return newScope;
        }

        public async Task<Organisation> GetOrgByEmployerReferenceAsync(string employerReference)
        {
            Organisation org = await DataRepository.FirstOrDefaultAsync<Organisation>(o => o.EmployerReference == employerReference);
            return org;
        }

        #region Repo

        public virtual async Task<OrganisationScope> GetScopeByIdAsync(long organisationScopeId)
        {
            return await DataRepository.FirstOrDefaultAsync<OrganisationScope>(o => o.OrganisationScopeId == organisationScopeId);
        }

        /// <summary>
        ///     Gets the latest scope for the specified organisation id and snapshot year
        /// </summary>
        /// <param name="organisationId"></param>
        /// <param name="snapshotYear"></param>
        public virtual async Task<OrganisationScope> GetLatestScopeBySnapshotYearAsync(long organisationId, int snapshotYear = 0)
        {
            Organisation org = await DataRepository.FirstOrDefaultAsync<Organisation>(o => o.OrganisationId == organisationId);
            if (org == null)
            {
                throw new ArgumentException($"Cannot find organisation with id {organisationId}", nameof(organisationId));
            }

            return GetLatestScopeBySnapshotYear(org, snapshotYear);
        }

        /// <summary>
        ///     Gets the latest scope for the specified organisation id and snapshot year
        /// </summary>
        /// <param name="organisationId"></param>
        /// <param name="snapshotYear"></param>
        public virtual OrganisationScope GetLatestScopeBySnapshotYear(Organisation organisation, int snapshotYear = 0)
        {
            if (snapshotYear == 0)
            {
                snapshotYear = _commonBusinessLogic.GetAccountingStartDate(organisation.SectorType).Year;
            }

            OrganisationScope orgScope = organisation.OrganisationScopes
                .SingleOrDefault(s => s.SnapshotDate.Year == snapshotYear && s.Status == ScopeRowStatuses.Active);

            return orgScope;
        }

        public virtual async Task<OrganisationScope> GetPendingScopeRegistrationAsync(string emailAddress)
        {
            var result=await DataRepository.FirstOrDefaultByDescendingAsync<OrganisationScope,DateTime>(s => s.RegisterStatusDate,o => o.RegisterStatus == RegisterStatuses.RegisterPending && o.ContactEmailAddress == emailAddress);
            return result;
        }

        #endregion

    }
}