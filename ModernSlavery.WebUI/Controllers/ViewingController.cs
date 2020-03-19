﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mime;
using System.Threading.Tasks;
using System.Web;
using ModernSlavery.BusinessLogic;
using ModernSlavery.BusinessLogic.Models.Submit;
using ModernSlavery.Core.Classes.ErrorMessages;
using ModernSlavery.Extensions;
using ModernSlavery.WebUI.Models;
using ModernSlavery.WebUI.Models.Search;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;
using ModernSlavery.BusinessLogic.View;
using ModernSlavery.WebUI.Shared.Controllers;
using ModernSlavery.WebUI.Shared.Classes;
using ModernSlavery.Entities;
using ModernSlavery.SharedKernel;
using ModernSlavery.WebUI.Presenters;
using ModernSlavery.WebUI.Shared.Interfaces;
using ModernSlavery.WebUI.Shared.Models.HttpResultModels;

namespace ModernSlavery.WebUI.Controllers
{
    [Route("viewing")]
    public class ViewingController : BaseController
    {

        #region Constructors

        public ViewingController(
            IViewingService viewingService,
            IViewingPresenter viewingPresenter,
            ISearchPresenter searchPresenter,
            IComparePresenter comparePresenter,
            ILogger<ViewingController> logger, IWebService webService, ICommonBusinessLogic commonBusinessLogic) : base(logger, webService, commonBusinessLogic)
        {
            ViewingService = viewingService;
            ViewingPresenter = viewingPresenter;
            SearchPresenter = searchPresenter;
            ComparePresenter = comparePresenter;
        }

        #endregion

        #region Dependencies
        public IViewingService ViewingService { get; }
        public IViewingPresenter ViewingPresenter { get; }
        public ISearchPresenter SearchPresenter { get; }
        public IComparePresenter ComparePresenter { get; }

        #endregion

        private string DecodeEmployerIdentifier(string employerIdentifier, out ActionResult actionResult)
        {
            string result = string.Empty;
            actionResult = new EmptyResult();

            if (string.IsNullOrWhiteSpace(employerIdentifier))
            {
                actionResult = new HttpBadRequestResult("Missing employer identifier");
            }

            try
            {
                result = HttpUtility.UrlDecode(Encryption.DecryptQuerystring(employerIdentifier));
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, $"Cannot decrypt return id from '{employerIdentifier}'");
                actionResult = View("CustomError", WebService.ErrorViewModelFactory.Create(400));
            }

            return result;
        }

        #region Initialisation

        /// <summary>
        ///     This action is only used to warm up this controller on initialisation
        /// </summary>
        [HttpGet("Init")]
        public IActionResult Init()
        {
            if (!CommonBusinessLogic.GlobalOptions.IsProduction())
            {
                Logger.LogInformation("Viewing Controller Initialised");
            }

            return new EmptyResult();
        }

        [HttpGet("~/")]
        public IActionResult Index()
        {
            //Clear the default back url of the employer hub pages
            EmployerBackUrl = null;
            ReportBackUrl = null;
            
            if (WebService.FeatureSwitchOptions.IsEnabled("ReportingStepByStep"))
            {
                return View("Launchpad/PrototypeIndex");
            }
            else
            {
                return View("Launchpad/Index");
            }
        }

        [HttpGet]
        public async Task<IActionResult> Redirect()
        {
            await TrackPageViewAsync();

            return RedirectToActionPermanent("Index");
        }

        #endregion

        #region Search

        /// <summary>
        /// </summary>
        /// <param name="searchQuery"></param>
        [NoCache]
        [HttpGet("~/search-results")]
        [HttpGet("search-results")]
        public async Task<IActionResult> SearchResults([FromQuery] SearchResultsQuery searchQuery)
        {
            //Ensure search service is enabled
            if (ViewingService.SearchBusinessLogic.EmployerSearchRepository.Disabled) return View("CustomError", WebService.ErrorViewModelFactory.Create(1151,new {featureName="Search Service" }));

            //When never searched in this session
            if (string.IsNullOrWhiteSpace(SearchPresenter.LastSearchParameters))
            {
                //If no compare employers in session then load employers from the cookie
                if (ComparePresenter.BasketItemCount == 0)
                {
                    ComparePresenter.LoadComparedEmployersFromCookie();
                }
            }

            //Clear the default back url of the employer hub pages
            EmployerBackUrl = null;
            ReportBackUrl = null;

            // ensure parameters are valid
            if (!searchQuery.TryValidateSearchParams(out HttpStatusViewResult result))
            {
                return result;
            }

            // generate result view model
            var searchParams = AutoMapper.Map<EmployerSearchParameters>(searchQuery);
            SearchViewModel model = await ViewingPresenter.SearchAsync(searchParams);

            ViewBag.ReturnUrl = SearchPresenter.GetLastSearchUrl();

            ViewBag.BasketViewModel = new CompareBasketViewModel {
                CanAddEmployers = false, CanViewCompare = ComparePresenter.BasketItemCount > 1, CanClearCompare = true
            };

            return View("Finder/SearchResults", model);
        }

        [NoCache]
        [HttpGet("~/search-results-js")]
        [HttpGet("search-results-js")]
        public async Task<IActionResult> SearchResultsJs([FromQuery] SearchResultsQuery searchQuery)
        {
            //Clear the default back url of the employer hub pages
            EmployerBackUrl = null;
            ReportBackUrl = null;


            // ensure parameters are valid
            if (!searchQuery.TryValidateSearchParams(out HttpStatusViewResult result))
            {
                return result;
            }

            // generate result view model
            var searchParams = AutoMapper.Map<EmployerSearchParameters>(searchQuery);
            SearchViewModel model = await ViewingPresenter.SearchAsync(searchParams);

            ViewBag.ReturnUrl = SearchPresenter.GetLastSearchUrl();

            return PartialView("Finder/Parts/MainContent", model);
        }

        [ResponseCache(CacheProfileName = "SuggestEmployerNameJs")]
        [HttpGet("suggest-employer-name-js")]
        public async Task<IActionResult> SuggestEmployerNameJs(string search)
        {
            if (string.IsNullOrEmpty(search))
            {
                return Json(new {ErrorCode = HttpStatusCode.BadRequest, ErrorMessage = "Cannot search for a null or empty value"});
            }

            List<SuggestEmployerResult> matches = await ViewingPresenter.SuggestEmployerNameAsync(search.Trim());

            return Json(new {Matches = matches});
        }

        [ResponseCache(CacheProfileName = "SuggestSicCodeJs")]
        [HttpGet("suggest-sic-code-js")]
        public async Task<JsonResult> SuggestSicCodeJsAsync(string search)
        {
            if (string.IsNullOrEmpty(search))
            {
                return Json(new {ErrorCode = HttpStatusCode.BadRequest, ErrorMessage = "Cannot search for a null or empty value"});
            }

            List<SicCodeSearchResult> matches = await ViewingPresenter.GetListOfSicCodeSuggestionsAsync(search.Trim());

            return Json(new {Matches = matches});
        }

        #endregion

        #region Downloads

        [HttpGet("download")]
        public async Task<IActionResult> Download()
        {
            var model = new DownloadViewModel {Downloads = new List<DownloadViewModel.Download>()};

            const string filePattern = "GPGData_????-????.csv";
            foreach (string file in await CommonBusinessLogic.FileRepository.GetFilesAsync(CommonBusinessLogic.GlobalOptions.DownloadsLocation, filePattern))
            {
                var download = new DownloadViewModel.Download {
                    Title = Path.GetFileNameWithoutExtension(file).AfterFirst("GPGData_"),
                    Count = await CommonBusinessLogic.FileRepository.GetMetaDataAsync(file, "RecordCount"),
                    Extension = Path.GetExtension(file).TrimI("."),
                    Size = Numeric.FormatFileSize(await CommonBusinessLogic.FileRepository.GetFileSizeAsync(file))
                };

                download.Url = Url.Action("DownloadData", new {year = download.Title.BeforeFirst("-")});
                model.Downloads.Add(download);
            }

            //Sort downloads by descending year
            model.Downloads = model.Downloads.OrderByDescending(d => d.Title).ToList();

            //Return the view with the model
            return View("Download", model);
        }

        [HttpGet("download-data")]
        [HttpGet("download-data/{year:int=0}")]
        public async Task<IActionResult> DownloadData(int year = 0)
        {
            if (year == 0)
            {
                year = ViewingService.CommonBusinessLogic.GetAccountingStartDate(SectorTypes.Private).Year;
            }

            //Ensure we have a directory
            if (!await CommonBusinessLogic.FileRepository.GetDirectoryExistsAsync(CommonBusinessLogic.GlobalOptions.DownloadsLocation))
            {
                return new HttpNotFoundResult($"Directory '{CommonBusinessLogic.GlobalOptions.DownloadsLocation}' does not exist");
            }

            //Ensure we have a file
            string filePattern = $"GPGData_{year}-{year + 1}.csv";
            IEnumerable<string> files = await CommonBusinessLogic.FileRepository.GetFilesAsync(CommonBusinessLogic.GlobalOptions.DownloadsLocation, filePattern);
            string file = files.FirstOrDefault();
            if (file == null || !await CommonBusinessLogic.FileRepository.GetFileExistsAsync(file))
            {
                return new HttpNotFoundResult("Cannot find GPG data file for year: " + year);
            }
            //Get the public and private accounting dates for the specified year

            //TODO log download

            //Setup the HTTP response
            var contentDisposition = new ContentDisposition {
                FileName = $"UK Modern Slavery statement - {year} to {year + 1}.csv", Inline = false
            };
            HttpContext.SetResponseHeader("Content-Disposition", contentDisposition.ToString());

            //cache old files for 1 day
            DateTime lastWriteTime = await CommonBusinessLogic.FileRepository.GetLastWriteTimeAsync(file);
            if (lastWriteTime.AddMonths(12) < VirtualDateTime.Now)
            {
                Response.GetTypedHeaders().CacheControl = new CacheControlHeaderValue {MaxAge = TimeSpan.FromDays(1), Public = true};
            }

            /* No Longer required as AspNetCore has response buffering on by default
            Response.BufferOutput = true;
            */
            //Track the download 
            await TrackPageViewAsync(contentDisposition.FileName);

            //Return the data
            return Content(await CommonBusinessLogic.FileRepository.ReadAsync(file), "text/csv");
        }

        #endregion

        #region Employer details

        [HttpGet("employer-details")]
        [Obsolete("Please use method 'Employer' instead.")] //, true)]
        public async Task<IActionResult> EmployerDetails(string e = null, int y = 0, string id = null)
        {
            if (!string.IsNullOrWhiteSpace(id))
            {
                Organisation organisation;

                try
                {
                    CustomResult<Organisation> organisationLoadingOutcome =
                        await ViewingService.OrganisationBusinessLogic.GetOrganisationByEncryptedReturnIdAsync(id);

                    if (organisationLoadingOutcome.Failed)
                    {
                        return organisationLoadingOutcome.ErrorMessage.ToHttpStatusViewResult();
                    }

                    organisation = organisationLoadingOutcome.Result;
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "Cannot decrypt return id from query string");
                    return View("CustomError", WebService.ErrorViewModelFactory.Create(400));
                }

                string organisationIdEncrypted = organisation.GetEncryptedId();
                return RedirectToActionPermanent(nameof(Employer), new {employerIdentifier = organisationIdEncrypted});
            }

            if (string.IsNullOrWhiteSpace(e))
            {
                return new HttpBadRequestResult("EmployerDetails: \'e\' query parameter was null or white space");
            }

            return RedirectToActionPermanent(nameof(Employer), new {employerIdentifier = e});
        }

        [HttpGet("employer-{employerIdentifier}")]
        [Obsolete("Please use method 'Employer' instead.")] // , true)]
        public IActionResult EmployerDeprecated(string employerIdentifier)
        {
            string decodedEmployerIdentifier = DecodeEmployerIdentifier(employerIdentifier, out ActionResult actionResult);

            if (string.IsNullOrEmpty(decodedEmployerIdentifier))
            {
                return actionResult;
            }

            int employerIdentifierId = employerIdentifier.ToInt32();
            string shortUrlObfuscatedEmployerIdentifier = ViewingService.Obfuscator.Obfuscate(employerIdentifierId);

            return RedirectToActionPermanent(nameof(Employer), new {employerIdentifier = shortUrlObfuscatedEmployerIdentifier});
        }

        [NoCache]
        [HttpGet("~/Employer/{employerIdentifier}")]
        public IActionResult Employer(string employerIdentifier)
        {
            if (string.IsNullOrWhiteSpace(employerIdentifier))
            {
                return new HttpBadRequestResult("Missing employer identifier");
            }

            CustomResult<Organisation> organisationLoadingOutcome;

            try
            {
                organisationLoadingOutcome = ViewingService.OrganisationBusinessLogic.LoadInfoFromActiveEmployerIdentifier(employerIdentifier);

                if (organisationLoadingOutcome.Failed)
                {
                    return organisationLoadingOutcome.ErrorMessage.ToHttpStatusViewResult();
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, $"Cannot decrypt return employerIdentifier from '{employerIdentifier}'");
                return View("CustomError", WebService.ErrorViewModelFactory.Create(400));
            }

            //Clear the default back url of the report page
            ReportBackUrl = null;

            ViewBag.BasketViewModel = new CompareBasketViewModel {CanAddEmployers = true, CanViewCompare = true};

            return View(
                "EmployerDetails/Employer",
                new EmployerDetailsViewModel {
                    Organisation = organisationLoadingOutcome.Result,
                    LastSearchUrl = SearchPresenter.GetLastSearchUrl(),
                    EmployerBackUrl = EmployerBackUrl,
                    ComparedEmployers = ComparePresenter.ComparedEmployers.Value
                });
        }

        #endregion

        #region Reports

        [HttpGet("employer-{employerIdentifier}/report-{year}")]
        [NoCache]
        [Obsolete("ReportDeprecated is (unsurprisingly) deprecated, please use method 'Report' instead.")] // , true)]
        public IActionResult ReportDeprecated(string employerIdentifier, int year)
        {
            if (year < CommonBusinessLogic.GlobalOptions.FirstReportingYear || year > VirtualDateTime.Now.Year)
            {
                return new HttpBadRequestResult($"Invalid snapshot year {year}");
            }

            string decodedEmployerIdentifier = DecodeEmployerIdentifier(employerIdentifier, out ActionResult actionResult);

            if (string.IsNullOrEmpty(decodedEmployerIdentifier))
            {
                return actionResult;
            }

            int employerIdentifierId = decodedEmployerIdentifier.ToInt32();
            string shortUrlObfuscatedEmployerIdentifier = ViewingService.Obfuscator.Obfuscate(employerIdentifierId);

            return RedirectToActionPermanent(nameof(Report), new {employerIdentifier = shortUrlObfuscatedEmployerIdentifier, year});
        }


        [HttpGet("~/Employer/{employerIdentifier}/{year}")]
        public IActionResult Report(string employerIdentifier, int year)
        {
            if (string.IsNullOrWhiteSpace(employerIdentifier))
            {
                return new HttpBadRequestResult("Missing employer identifier");
            }

            if (year < CommonBusinessLogic.GlobalOptions.FirstReportingYear || year > VirtualDateTime.Now.Year)
            {
                return new HttpBadRequestResult($"Invalid snapshot year {year}");
            }

            #region Load organisation

            CustomResult<Organisation> organisationLoadingOutcome;

            try
            {
                organisationLoadingOutcome = ViewingService.OrganisationBusinessLogic.LoadInfoFromActiveEmployerIdentifier(employerIdentifier);

                if (organisationLoadingOutcome.Failed)
                {
                    return organisationLoadingOutcome.ErrorMessage.ToHttpStatusViewResult();
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, $"Cannot decrypt return employerIdentifier from '{employerIdentifier}'");
                return View("CustomError", WebService.ErrorViewModelFactory.Create(400));
            }

            Organisation foundOrganisation = organisationLoadingOutcome.Result;

            #endregion

            #region Load latest submission 

            ReturnViewModel model;

            try
            {
                CustomResult<Return> getLatestSubmissionLoadingOutcome =
                    ViewingService.SubmissionBusinessLogic.GetSubmissionByOrganisationAndYear(foundOrganisation, year);

                if (getLatestSubmissionLoadingOutcome.Failed)
                {
                    return getLatestSubmissionLoadingOutcome.ErrorMessage.ToHttpStatusViewResult();
                }

                model = ViewingService.SubmissionBusinessLogic.ConvertSubmissionReportToReturnViewModel(getLatestSubmissionLoadingOutcome.Result);
            }
            catch (Exception ex)
            {
                Logger.LogError(
                    ex,
                    $"Exception processing the return information for Organisation '{foundOrganisation.OrganisationId}:{foundOrganisation.OrganisationName}'");
                return View("CustomError", WebService.ErrorViewModelFactory.Create(400));
            }

            #endregion

            ViewBag.BasketViewModel = new CompareBasketViewModel {CanAddEmployers = true, CanViewCompare = true};

            return View("EmployerDetails/Report", model);
        }

        [HttpGet("add-search-results-to-compare")]
        public async Task<IActionResult> AddSearchResultsToCompare([FromQuery] SearchResultsQuery searchQuery)
        {
            if (!searchQuery.TryValidateSearchParams(out HttpStatusViewResult result))
            {
                return result;
            }

            // generate compare list
            var searchParams = AutoMapper.Map<EmployerSearchParameters>(searchQuery);

            // set maximum search size
            searchParams.Page = 1;
            searchParams.PageSize = ComparePresenter.MaxCompareBasketCount;
            SearchViewModel searchResultsModel = await ViewingPresenter.SearchAsync(searchParams);

            // add any new items to the compare list
            string[] resultIds = searchResultsModel.Employers.Results
                .Where(employer => ComparePresenter.BasketContains(employer.OrganisationIdEncrypted) == false)
                .Take(ComparePresenter.MaxCompareBasketCount - ComparePresenter.BasketItemCount)
                .Select(employer => employer.OrganisationIdEncrypted)
                .ToArray();

            ComparePresenter.AddRangeToBasket(resultIds);

            // save the results to the cookie
            ComparePresenter.SaveComparedEmployersToCookie(Request);

            return RedirectToAction(nameof(SearchResults), "Viewing", searchQuery);
        }

        #endregion

    }
}
