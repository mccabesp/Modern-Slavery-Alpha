﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Security.Authentication;
using System.Threading.Tasks;
using ModernSlavery.BusinessDomain.Shared;
using ModernSlavery.BusinessDomain.Shared.Interfaces;
using ModernSlavery.BusinessDomain.Shared.Models;
using ModernSlavery.Core.Entities;
using ModernSlavery.Core.Extensions;
using ModernSlavery.WebUI.Submission.Models;

namespace ModernSlavery.WebUI.Submission.Classes
{
    public interface ISubmissionPresenter
    {
        ISubmissionService SubmissionService { get; }

        bool IsCurrentSnapshotYear(SectorTypes sector, int snapshotYear);

        bool IsHistoricSnapshotYear(SectorTypes sector, int snapshotYear);

        bool IsValidSnapshotYear(int snapshotYear);
        DateTime GetCurrentSnapshotDate(SectorTypes sector = SectorTypes.Private);
        SubmissionChangeSummary GetSubmissionChangeSummary(Return stashedReturn, Return databaseReturn);
        Task<Return> GetReturnFromDatabaseAsync(long organisationId, int snapshotYear = 0);

        Task<Return> GetSubmissionByIdAsync(long returnId);

        // presentation
        Task<ReturnViewModel> GetReturnViewModelAsync(long organisationId, int snapshotYear, long userId);

        bool ShouldUpdateLatestReturn(Organisation org, int snapshotYear);

        Task<ReportInfoModel> GetReportInfoModelWithLockedDraftAsync(Organisation organisation,
            DateTime? returnModifiedDate,
            int snapshotYear);

        Return CreateDraftSubmissionFromViewModel(ReturnViewModel stashedReturnViewModel);

        Task<List<ReportInfoModel>> GetAllEditableReportsAsync(UserOrganisation userOrg, DateTime currentSnapshotDate);

        #region DraftFile

        Task UpdateDraftFileAsync(long userRequestingTheUpdate, ReturnViewModel returnViewModel);
        Task<Draft> GetDraftFileAsync(long organisationId, int snapshotYear, long userIdRequestingDraft);
        Task KeepDraftFileLockedToUserAsync(ReturnViewModel returnViewModel, long userIdRequestingLock);
        Task CommitDraftFileAsync(ReturnViewModel returnViewModel);
        Task RestartDraftFileAsync(long organisationId, int snapshotYear, long userIdRequestingRollback);
        Task RollbackDraftFileAsync(ReturnViewModel returnViewModel);
        Task DiscardDraftFileAsync(ReturnViewModel returnViewModel);
        Task<Draft> GetDraftIfAvailableAsync(long organisationId, int snapshotYear);

        #endregion
    }

    public class SubmissionPresenter : ISubmissionPresenter
    {
        private readonly ISharedBusinessLogic _sharedBusinessLogic;

        public SubmissionPresenter(
            ISubmissionService submissionService,
            SubmissionOptions submissionOptions,
            ISharedBusinessLogic sharedBusinessLogic)

        {
            SubmissionService = submissionService;
            SubmissionOptions = submissionOptions;
            _sharedBusinessLogic = sharedBusinessLogic;
        }

        public SubmissionOptions SubmissionOptions { get; }
        public ISubmissionService SubmissionService { get; }

        public bool IsCurrentSnapshotYear(SectorTypes sector, int snapshotYear)
        {
            // Get the current reporting year
            var currentReportingStartYear = GetCurrentSnapshotDate(sector).Year;

            // Return year compare result
            return snapshotYear == currentReportingStartYear;
        }

        public bool IsHistoricSnapshotYear(SectorTypes sector, int snapshotYear)
        {
            // Get the previous reporting year
            var currentReportingStartYear = GetCurrentSnapshotDate(sector).Year;

            // Return year compare result
            return snapshotYear < currentReportingStartYear;
        }

        public virtual bool IsValidSnapshotYear(int snapshotYear)
        {
            return snapshotYear >= _sharedBusinessLogic.SharedOptions.FirstReportingYear;
        }

        public Return CreateDraftSubmissionFromViewModel(ReturnViewModel stashedReturnViewModel)
        {
            var orgSizeRange = stashedReturnViewModel.OrganisationSize.GetAttribute<RangeAttribute>();

            var result = new Return();
            result.AccountingDate = stashedReturnViewModel.AccountingDate;
            result.Status = ReturnStatuses.Draft;
            result.OrganisationId = stashedReturnViewModel.OrganisationId;
            result.Organisation =
                SubmissionService.SharedBusinessLogic.DataRepository.Get<Organisation>(result.OrganisationId);

            if (stashedReturnViewModel.DiffMeanBonusPercent != null)
                result.DiffMeanBonusPercent = stashedReturnViewModel.DiffMeanBonusPercent.Value;

            if (stashedReturnViewModel.DiffMeanHourlyPayPercent != null)
                result.DiffMeanHourlyPayPercent = stashedReturnViewModel.DiffMeanHourlyPayPercent.Value;

            if (stashedReturnViewModel.DiffMedianBonusPercent != null)
                result.DiffMedianBonusPercent = stashedReturnViewModel.DiffMedianBonusPercent.Value;

            //CompanyLinkToGPGInfo = model.CompanyLinkToGPGInfo,
            if (stashedReturnViewModel.CompanyLinkToGPGInfo != null)
                result.CompanyLinkToGPGInfo = stashedReturnViewModel.CompanyLinkToGPGInfo;

            if (stashedReturnViewModel.DiffMedianHourlyPercent != null)
                result.DiffMedianHourlyPercent = stashedReturnViewModel.DiffMedianHourlyPercent.Value;


            if (stashedReturnViewModel.FemaleLowerPayBand != null)
                result.FemaleLowerPayBand = stashedReturnViewModel.FemaleLowerPayBand.Value;

            if (stashedReturnViewModel.FemaleMedianBonusPayPercent != null)
                result.FemaleMedianBonusPayPercent = stashedReturnViewModel.FemaleMedianBonusPayPercent.Value;

            if (stashedReturnViewModel.FemaleMiddlePayBand != null)
                result.FemaleMiddlePayBand = stashedReturnViewModel.FemaleMiddlePayBand.Value;

            if (stashedReturnViewModel.FemaleUpperPayBand != null)
                result.FemaleUpperPayBand = stashedReturnViewModel.FemaleUpperPayBand.Value;

            if (stashedReturnViewModel.FemaleUpperQuartilePayBand != null)
                result.FemaleUpperQuartilePayBand = stashedReturnViewModel.FemaleUpperQuartilePayBand.Value;

            result.FirstName = stashedReturnViewModel.FirstName;
            result.LastName = stashedReturnViewModel.LastName;
            result.JobTitle = stashedReturnViewModel.JobTitle;

            if (stashedReturnViewModel.MaleLowerPayBand != null)
                result.MaleLowerPayBand = stashedReturnViewModel.MaleLowerPayBand.Value;

            if (stashedReturnViewModel.MaleMedianBonusPayPercent != null)
                result.MaleMedianBonusPayPercent = stashedReturnViewModel.MaleMedianBonusPayPercent.Value;

            if (stashedReturnViewModel.MaleUpperQuartilePayBand != null)
                result.MaleUpperQuartilePayBand = stashedReturnViewModel.MaleUpperQuartilePayBand.Value;

            if (stashedReturnViewModel.MaleMiddlePayBand != null)
                result.MaleMiddlePayBand = stashedReturnViewModel.MaleMiddlePayBand.Value;

            if (stashedReturnViewModel.MaleUpperPayBand != null)
                result.MaleUpperPayBand = stashedReturnViewModel.MaleUpperPayBand.Value;

            result.Status = ReturnStatuses.Draft;
            result.MinEmployees = (int) orgSizeRange.Minimum;
            result.MaxEmployees = (int) orgSizeRange.Maximum;
            result.LateReason = stashedReturnViewModel.LateReason;
            result.EHRCResponse = stashedReturnViewModel.EHRCResponse.ToBoolean();
            result.IsLateSubmission = result.CalculateIsLateSubmission();

            return result;
        }

        public SubmissionChangeSummary GetSubmissionChangeSummary(Return stashedReturn, Return databaseReturn)
        {
            // check website url for changes
            var websiteUrlChanged = stashedReturn.CompanyLinkToGPGInfo != databaseReturn.CompanyLinkToGPGInfo;

            // check figures for changes
            var figuresChanged = false;

            // works around decimal issue for things like 50 != 50.00
            var decimalFormat = "00.00";

            // check mean bonus difference
            if (stashedReturn.DiffMeanBonusPercent.HasValue
                && databaseReturn.DiffMeanBonusPercent.HasValue
                && stashedReturn.DiffMeanBonusPercent.Value.ToString(decimalFormat)
                != databaseReturn.DiffMeanBonusPercent.Value.ToString(decimalFormat))
                figuresChanged = true;
            else if (stashedReturn.DiffMeanBonusPercent.HasValue != databaseReturn.DiffMeanBonusPercent.HasValue)
                figuresChanged = true;

            // check median bonus difference
            if (stashedReturn.DiffMedianBonusPercent.HasValue
                && databaseReturn.DiffMedianBonusPercent.HasValue
                && stashedReturn.DiffMedianBonusPercent.Value.ToString(decimalFormat)
                != databaseReturn.DiffMedianBonusPercent.Value.ToString(decimalFormat))
                figuresChanged = true;
            else if (stashedReturn.DiffMedianBonusPercent.HasValue != databaseReturn.DiffMedianBonusPercent.HasValue)
                figuresChanged = true;

            if (stashedReturn.DiffMeanHourlyPayPercent.ToString(decimalFormat)
                != databaseReturn.DiffMeanHourlyPayPercent.ToString(decimalFormat))
                figuresChanged = true;

            if (stashedReturn.DiffMedianHourlyPercent.ToString(decimalFormat)
                != databaseReturn.DiffMedianHourlyPercent.ToString(decimalFormat))
                figuresChanged = true;

            if (stashedReturn.FemaleLowerPayBand.ToString(decimalFormat) !=
                databaseReturn.FemaleLowerPayBand.ToString(decimalFormat)) figuresChanged = true;

            if (stashedReturn.FemaleMedianBonusPayPercent.ToString(decimalFormat)
                != databaseReturn.FemaleMedianBonusPayPercent.ToString(decimalFormat))
                figuresChanged = true;

            if (stashedReturn.FemaleMiddlePayBand.ToString(decimalFormat) !=
                databaseReturn.FemaleMiddlePayBand.ToString(decimalFormat)) figuresChanged = true;

            if (stashedReturn.FemaleUpperPayBand.ToString(decimalFormat) !=
                databaseReturn.FemaleUpperPayBand.ToString(decimalFormat)) figuresChanged = true;

            if (stashedReturn.FemaleUpperQuartilePayBand.ToString(decimalFormat)
                != databaseReturn.FemaleUpperQuartilePayBand.ToString(decimalFormat))
                figuresChanged = true;

            if (stashedReturn.MaleLowerPayBand.ToString(decimalFormat) !=
                databaseReturn.MaleLowerPayBand.ToString(decimalFormat)) figuresChanged = true;

            if (stashedReturn.MaleMedianBonusPayPercent.ToString(decimalFormat)
                != databaseReturn.MaleMedianBonusPayPercent.ToString(decimalFormat))
                figuresChanged = true;

            if (stashedReturn.MaleUpperQuartilePayBand.ToString(decimalFormat)
                != databaseReturn.MaleUpperQuartilePayBand.ToString(decimalFormat))
                figuresChanged = true;

            if (stashedReturn.MaleMiddlePayBand.ToString(decimalFormat) !=
                databaseReturn.MaleMiddlePayBand.ToString(decimalFormat)) figuresChanged = true;

            if (stashedReturn.MaleUpperPayBand.ToString(decimalFormat) !=
                databaseReturn.MaleUpperPayBand.ToString(decimalFormat)) figuresChanged = true;

            // check person responsible for changes
            var personResonsibleChanged = false;
            if (stashedReturn.FirstName != databaseReturn.FirstName) personResonsibleChanged = true;

            if (stashedReturn.LastName != databaseReturn.LastName) personResonsibleChanged = true;

            if (stashedReturn.JobTitle != databaseReturn.JobTitle) personResonsibleChanged = true;

            // check size for changes
            var organisationSizeChanged = false;
            if (stashedReturn.MinEmployees != databaseReturn.MinEmployees) organisationSizeChanged = true;

            if (stashedReturn.MaxEmployees != databaseReturn.MaxEmployees) organisationSizeChanged = true;

            // is previous reporting start year
            var isPrevReportingStartYear = IsHistoricSnapshotYear(
                databaseReturn.Organisation.SectorType,
                databaseReturn.AccountingDate.Year);

            // check late reason for changes
            var lateReasonChanged = stashedReturn.LateReason != databaseReturn.LateReason
                                    || stashedReturn.EHRCResponse != databaseReturn.EHRCResponse;

            // record modifications
            var modifications = new List<string>();
            if (figuresChanged) modifications.Add("Figures");

            if (websiteUrlChanged) modifications.Add("WebsiteURL");

            if (personResonsibleChanged) modifications.Add("PersonResponsible");

            if (organisationSizeChanged) modifications.Add("OrganisationSize");

            if (lateReasonChanged) modifications.Add("LateReason");

            return new SubmissionChangeSummary
            {
                FiguresChanged = figuresChanged,
                PersonResonsibleChanged = personResonsibleChanged,
                OrganisationSizeChanged = organisationSizeChanged,
                WebsiteUrlChanged = websiteUrlChanged,
                IsPrevReportingStartYear = isPrevReportingStartYear,
                LateReasonChanged = lateReasonChanged,
                Modifications = string.Join(",", modifications)
            };
        }

        public async Task<Return> GetSubmissionByIdAsync(long returnId)
        {
            return await SubmissionService.SharedBusinessLogic.DataRepository.FirstOrDefaultAsync<Return>(r =>
                r.ReturnId == returnId);
        }

        public async Task<Return> GetReturnFromDatabaseAsync(long organisationId, int snapshotYear)
        {
            // ensure the year is reportable
            if (!IsValidSnapshotYear(snapshotYear)) return null;

            return await SubmissionService.SharedBusinessLogic.DataRepository
                .FirstOrDefaultByDescendingAsync<Return, long>(s => s.ReturnId,
                    s => s.Status == ReturnStatuses.Submitted && s.AccountingDate.Year == snapshotYear &&
                         s.OrganisationId == organisationId);
        }

        public async Task<ReturnViewModel> GetReturnViewModelAsync(long organisationId, int snapshotYear, long userId)
        {
            // get the user organisation
            var userOrganisationFromDatabase =
                await SubmissionService.SharedBusinessLogic.DataRepository.FirstOrDefaultAsync<UserOrganisation>(uo =>
                    uo.UserId == userId && uo.OrganisationId == organisationId);

            // throws an error when no user organisation was found
            if (userOrganisationFromDatabase == null)
                throw new AuthenticationException(
                    $"GetViewModelForSubmission: Failed to find the UserOrganisation for userId {userId} and organisationId {organisationId}");

            var result = new ReturnViewModel();

            // get the most recent submission for the given reporting year
            var returnFromDatabase = await GetReturnFromDatabaseAsync(organisationId, snapshotYear);

            // should provide late reason
            result.ShouldProvideLateReason =
                IsHistoricSnapshotYear(userOrganisationFromDatabase.Organisation.SectorType, snapshotYear);

            if (returnFromDatabase == null)
            {
                result.ReportInfo = await GetReportInfoModelWithDraftAsync(
                    userOrganisationFromDatabase.Organisation,
                    null,
                    snapshotYear,
                    userId);
                if (result.ReportInfo?.Draft?.ReturnViewModelContent != null)
                {
                    var tempDraft = result.ReportInfo.Draft;
                    result = result.ReportInfo.Draft.ReturnViewModelContent;
                    result.ReportInfo.Draft = tempDraft;
                }

                //Always create a new return if no previous return or latest last year 
                result.AccountingDate =
                    GetSnapshotDate(userOrganisationFromDatabase.Organisation.SectorType, snapshotYear);
                result.OrganisationId = organisationId;
                result.EncryptedOrganisationId = _sharedBusinessLogic.Obfuscator.Obfuscate(userOrganisationFromDatabase.Organisation.OrganisationId);
                result.SectorType = userOrganisationFromDatabase.Organisation.SectorType;
                return result;
            }

            // populate with return from db
            result.ReturnId = returnFromDatabase.ReturnId;
            result.OrganisationId = returnFromDatabase.OrganisationId;
            result.EncryptedOrganisationId = _sharedBusinessLogic.Obfuscator.Obfuscate(returnFromDatabase.Organisation.OrganisationId);
            result.DiffMeanBonusPercent = returnFromDatabase.DiffMeanBonusPercent;
            result.DiffMeanHourlyPayPercent = returnFromDatabase.DiffMeanHourlyPayPercent;
            result.DiffMedianBonusPercent = returnFromDatabase.DiffMedianBonusPercent;
            result.DiffMedianHourlyPercent = returnFromDatabase.DiffMedianHourlyPercent;
            result.FemaleLowerPayBand = returnFromDatabase.FemaleLowerPayBand;
            result.FemaleMedianBonusPayPercent = returnFromDatabase.FemaleMedianBonusPayPercent;
            result.FemaleMiddlePayBand = returnFromDatabase.FemaleMiddlePayBand;
            result.FemaleUpperPayBand = returnFromDatabase.FemaleUpperPayBand;
            result.FemaleUpperQuartilePayBand = returnFromDatabase.FemaleUpperQuartilePayBand;
            result.MaleLowerPayBand = returnFromDatabase.MaleLowerPayBand;
            result.MaleMedianBonusPayPercent = returnFromDatabase.MaleMedianBonusPayPercent;
            result.MaleMiddlePayBand = returnFromDatabase.MaleMiddlePayBand;
            result.MaleUpperPayBand = returnFromDatabase.MaleUpperPayBand;
            result.MaleUpperQuartilePayBand = returnFromDatabase.MaleUpperQuartilePayBand;
            result.JobTitle = returnFromDatabase.JobTitle;
            result.FirstName = returnFromDatabase.FirstName;
            result.LastName = returnFromDatabase.LastName;
            result.CompanyLinkToGPGInfo = returnFromDatabase.CompanyLinkToGPGInfo;
            result.AccountingDate = returnFromDatabase.AccountingDate;
            result.OrganisationSize = returnFromDatabase.OrganisationSize;
            result.SectorType = returnFromDatabase.Organisation.SectorType;
            result.FormatDecimals();
            result.Modified = returnFromDatabase.Modified;
            //result.LatestOrganisationName = returnFromDatabase.LatestOrganisation?.OrganisationName;
            result.OrganisationName = returnFromDatabase.Organisation.OrganisationName;
            result.LateReason = returnFromDatabase.LateReason;
            result.EHRCResponse = returnFromDatabase.EHRCResponse.ToString();

            // set the report info
            result.ReportInfo = await GetReportInfoModelWithDraftAsync(
                returnFromDatabase.Organisation,
                returnFromDatabase.Modified,
                snapshotYear,
                userId);
            if (result.ReportInfo?.Draft?.ReturnViewModelContent != null)
            {
                var temporaryDraft = result.ReportInfo.Draft;
                result = result.ReportInfo.Draft.ReturnViewModelContent;
                result.ReportInfo.Draft = temporaryDraft;
            }

            return result;
        }

        /// <summary>
        ///     This draft will only return the contents of the draft, if you need to know the user that is currently locking the
        ///     draft, or when was the draft last written, please use GetReportInfoModelWithDraft
        /// </summary>
        /// <param name="organisation"></param>
        /// <param name="returnModifiedDate"></param>
        /// <param name="snapshotYear"></param>
        public async Task<ReportInfoModel> GetReportInfoModelWithLockedDraftAsync(Organisation organisation,
            DateTime? returnModifiedDate,
            int snapshotYear)
        {
            var reportInfo = await GetReport_InfoAsync(organisation, returnModifiedDate, snapshotYear);
            reportInfo.Draft = await GetDraftIfAvailableAsync(organisation.OrganisationId, snapshotYear);
            return reportInfo;
        }

        public DateTime GetCurrentSnapshotDate(SectorTypes sector = SectorTypes.Private)
        {
            // Get the current reporting period for the sector
            return GetSnapshotDate(sector);
        }

        public bool ShouldUpdateLatestReturn(Organisation org, int snapshotYear)
        {
            return org.LatestReturn == null || org.LatestReturn.AccountingDate.Year <= snapshotYear;
        }

        public async Task<List<ReportInfoModel>> GetAllEditableReportsAsync(UserOrganisation userOrg,
            DateTime currentSnapshotDate)
        {
            var currentYear = currentSnapshotDate.Year;
            var startYear = currentYear - (SubmissionOptions.EditableReportCount - 1);
            if (startYear < _sharedBusinessLogic.SharedOptions.FirstReportingYear)
                startYear = _sharedBusinessLogic.SharedOptions.FirstReportingYear;

            var reportInfos = new List<ReportInfoModel>();
            for (var snapshotYear = startYear; snapshotYear <= currentYear; snapshotYear++)
            {
                var report = await GetReturnFromDatabaseAsync(userOrg.OrganisationId, snapshotYear);
                var reportInfo = report != null
                    ? await GetReportInfoModelWithLockedDraftAsync(report.Organisation, report.Modified, snapshotYear)
                    : await GetReportInfoModelWithLockedDraftAsync(userOrg.Organisation, null, snapshotYear);

                reportInfos.Add(reportInfo);
            }

            return reportInfos;
        }

        /// <summary>
        ///     Returns a complete draft with contents and access/locking information. If you require just basic draft contents
        ///     (perhaps to confirm the draft has some data or not) please use GetReportInfoModelWithLockedDraft
        /// </summary>
        /// <param name="organisation"></param>
        /// <param name="returnModifiedDate"></param>
        /// <param name="snapshotYear"></param>
        /// <param name="userRequestingDraft"></param>
        /// <returns></returns>
        private async Task<ReportInfoModel> GetReportInfoModelWithDraftAsync(Organisation organisation,
            DateTime? returnModifiedDate,
            int snapshotYear,
            long userRequestingDraft)
        {
            var reportInfo = await GetReport_InfoAsync(organisation, returnModifiedDate, snapshotYear);
            reportInfo.Draft = await GetDraftFileAsync(organisation.OrganisationId, snapshotYear, userRequestingDraft);
            return reportInfo;
        }

        private async Task<ReportInfoModel> GetReport_InfoAsync(Organisation organisation, DateTime? returnModifiedDate,
            int snapshotYear)
        {
            var snapshotDate = GetSnapshotDate(organisation.SectorType, snapshotYear);
            if (!IsValidSnapshotYear(snapshotDate.Year)) return null;

            var result = new ReportInfoModel
            {
                ReportingStartDate = snapshotDate,
                ReportModifiedDate = returnModifiedDate,
                ReportingRequirement =
                    await SubmissionService.ScopeBusinessLogic.GetLatestScopeStatusForSnapshotYearAsync(
                        organisation.OrganisationId, snapshotDate.Year)
            };

            return result;
        }

        // Wraps the enum SectorTypes.GetAccountingStartDate() method because we cant mock it
        // We cant change SectorTypes.GetAccountingStartDate() because it's used everywhere
        // But these virtual wrappers can be mocked and extended
        public virtual DateTime GetSnapshotDate(SectorTypes sector = SectorTypes.Private, int snapshotYear = 0)
        {
            // Get the reporting start date for the sector and reporting start year
            return SubmissionService.SharedBusinessLogic.GetAccountingStartDate(sector, snapshotYear);
        }

        #region Draft File

        public async Task<Draft> GetDraftIfAvailableAsync(long organisationId, int snapshotYear)
        {
            return await SubmissionService.DraftFileBusinessLogic.GetDraftIfAvailableAsync(organisationId,
                snapshotYear);
        }

        public async Task UpdateDraftFileAsync(long userRequestingTheUpdate, ReturnViewModel returnViewModel)
        {
            await SubmissionService.DraftFileBusinessLogic.UpdateAsync(returnViewModel,
                returnViewModel.ReportInfo.Draft, userRequestingTheUpdate);
        }

        public async Task<Draft> GetDraftFileAsync(long organisationId, int snapshotYear, long userIdRequestingDraft)
        {
            return await SubmissionService.DraftFileBusinessLogic.GetExistingOrNewAsync(organisationId, snapshotYear,
                userIdRequestingDraft);
        }

        public async Task KeepDraftFileLockedToUserAsync(ReturnViewModel returnViewModel, long userIdRequestingLock)
        {
            await SubmissionService.DraftFileBusinessLogic.KeepDraftFileLockedToUserAsync(
                returnViewModel.ReportInfo.Draft, userIdRequestingLock);
        }

        public async Task CommitDraftFileAsync(ReturnViewModel returnViewModel)
        {
            await SubmissionService.DraftFileBusinessLogic.CommitDraftAsync(returnViewModel.ReportInfo.Draft);
        }

        public async Task RollbackDraftFileAsync(ReturnViewModel returnViewModel)
        {
            await SubmissionService.DraftFileBusinessLogic.RollbackDraftAsync(returnViewModel.ReportInfo.Draft);
        }

        public async Task RestartDraftFileAsync(long organisationId, int snapshotYear, long userIdRequestingDraft)
        {
            await SubmissionService.DraftFileBusinessLogic.RestartDraftAsync(organisationId, snapshotYear,
                userIdRequestingDraft);
        }

        public async Task DiscardDraftFileAsync(ReturnViewModel returnViewModel)
        {
            await SubmissionService.DraftFileBusinessLogic.DiscardDraftAsync(returnViewModel.ReportInfo.Draft);
        }

        #endregion
    }
}