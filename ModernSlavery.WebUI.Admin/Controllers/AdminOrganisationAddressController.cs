﻿using System;
using System.Linq;
using ModernSlavery.Core.Interfaces;
using ModernSlavery.Entities;
using ModernSlavery.Extensions;
using ModernSlavery.WebUI.Admin.Models;
using GovUkDesignSystem.Parsers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ModernSlavery.BusinessLogic;
using ModernSlavery.Core.Models.CompaniesHouse;
using ModernSlavery.WebUI.Shared.Classes;
using ModernSlavery.Entities.Enums;

namespace ModernSlavery.WebUI.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "GPGadmin")]
    [Route("admin")]
    public class AdminOrganisationAddressController : Controller
    {
        private readonly IDataRepository dataRepository;
        private readonly ICompaniesHouseAPI companiesHouseApi;
        private readonly AuditLogger auditLogger;
        private readonly ICustomLogger customLogger;
        private readonly IUpdateFromCompaniesHouseService UpdateFromCompaniesHouseService;

        public AdminOrganisationAddressController(
            IDataRepository dataRepository,
            ICompaniesHouseAPI companiesHouseApi,
            IUpdateFromCompaniesHouseService updateFromCompaniesHouseService,
            AuditLogger auditLogger, ICustomLogger customLogger)
        {
            this.dataRepository = dataRepository;
            this.companiesHouseApi = companiesHouseApi;
            this.UpdateFromCompaniesHouseService = updateFromCompaniesHouseService;
            this.auditLogger = auditLogger;
            this.customLogger = customLogger;
        }

        [HttpGet("organisation/{id}/address")]
        public IActionResult ViewAddressHistory(long id)
        {
            Organisation organisation = dataRepository.Get<Organisation>(id);

            return View("ViewOrganisationAddress", organisation);
        }

        [HttpGet("organisation/{id}/address/change")]
        public IActionResult ChangeAddressGet(long id)
        {
            Organisation organisation = dataRepository.Get<Organisation>(id);

            if (!string.IsNullOrWhiteSpace(organisation.CompanyNumber))
            {
                try
                {
                    CompaniesHouseCompany organisationFromCompaniesHouse =
                        companiesHouseApi.GetCompanyAsync(organisation.CompanyNumber).Result;

                    OrganisationAddress addressFromCompaniesHouse =
                        UpdateFromCompaniesHouseService.CreateOrganisationAddressFromCompaniesHouseAddress(
                        organisationFromCompaniesHouse.RegisteredOfficeAddress);

                    if (!organisation.GetAddress().AddressMatches(addressFromCompaniesHouse))
                    {
                        return OfferNewCompaniesHouseAddress(organisation, addressFromCompaniesHouse);
                    }

                }
                catch (Exception ex)
                {
                    // Use Manual Change page instead
                    customLogger.Warning("Error from Companies House API", ex);
                }
            }

            // In all other cases...
            // * Organisation doesn't have a Companies House number
            // * CoHo API returns an error
            // * CoHo address matches Organisation address
            // ... send to the Manual Change page
            return SendToManualChangePage(organisation);
        }

        private IActionResult OfferNewCompaniesHouseAddress(Organisation organisation, OrganisationAddress addressFromCompaniesHouse)
        {
            var viewModel = new ChangeOrganisationAddressViewModel {
                Organisation = organisation,
                Action = ManuallyChangeOrganisationAddressViewModelActions.OfferNewCompaniesHouseAddress
            };

            viewModel.PopulateFromOrganisationAddress(addressFromCompaniesHouse);

            return View("OfferNewCompaniesHouseAddress", viewModel);
        }

        private IActionResult SendToManualChangePage(Organisation organisation)
        {
            OrganisationAddress address = organisation.OrganisationAddresses.OrderByDescending(a => a.Created).First();

            var viewModel = new ChangeOrganisationAddressViewModel
            {
                Organisation = organisation,
                Action = ManuallyChangeOrganisationAddressViewModelActions.ManualChange
            };

            viewModel.PopulateFromOrganisationAddress(address);

            return View("ManuallyChangeOrganisationAddress", viewModel);
        }

        [ValidateAntiForgeryToken]
        [HttpPost("organisation/{id}/address/change")]
        public IActionResult ChangeAddressPost(long id, ChangeOrganisationAddressViewModel viewModel)
        {
            // We might need to change the value of Action before we go to the view
            // Apparently this is necessary
            // https://stackoverflow.com/questions/4837744/hiddenfor-not-getting-correct-value-from-view-model
            ModelState.Clear();

            Organisation organisation = dataRepository.Get<Organisation>(id);

            switch (viewModel.Action) {
                case ManuallyChangeOrganisationAddressViewModelActions.OfferNewCompaniesHouseAddress:
                    return OfferNewCompaniesHouseAction(viewModel, organisation);

                case ManuallyChangeOrganisationAddressViewModelActions.ManualChange:
                    return ManualChangeAction(viewModel, organisation);

                case ManuallyChangeOrganisationAddressViewModelActions.CheckChangesManual:
                case ManuallyChangeOrganisationAddressViewModelActions.CheckChangesCoHo:
                    return CheckChangesAction(viewModel, organisation);

                default:
                    throw new ArgumentException("Unknown action in AdminOrganisationAddressController.ChangeAddressPost");
            }
        }

        private IActionResult OfferNewCompaniesHouseAction(ChangeOrganisationAddressViewModel viewModel, Organisation organisation)
        {
            viewModel.ParseAndValidateParameters(Request, m => m.AcceptCompaniesHouseAddress);

            if (viewModel.HasAnyErrors())
            {
                viewModel.Organisation = organisation;
                viewModel.Action = ManuallyChangeOrganisationAddressViewModelActions.OfferNewCompaniesHouseAddress;
                return View("OfferNewCompaniesHouseAddress", viewModel);
            }

            if (viewModel.AcceptCompaniesHouseAddress == AcceptCompaniesHouseAddress.Reject)
            {
                return SendToManualChangePage(organisation);
            }

            viewModel.ParseAndValidateParameters(Request, m => m.Reason);

            if (viewModel.HasAnyErrors())
            {
                viewModel.Organisation = organisation;
                viewModel.Action = ManuallyChangeOrganisationAddressViewModelActions.OfferNewCompaniesHouseAddress;
                return View("OfferNewCompaniesHouseAddress", viewModel);
            }

            viewModel.Organisation = organisation;
            viewModel.Action = ManuallyChangeOrganisationAddressViewModelActions.CheckChangesCoHo;
            return View("ConfirmAddressChange", viewModel);
        }

        private IActionResult ManualChangeAction(ChangeOrganisationAddressViewModel viewModel, Organisation organisation)
        {
            viewModel.ParseAndValidateParameters(Request, m => m.PoBox);
            viewModel.ParseAndValidateParameters(Request, m => m.Address1);
            viewModel.ParseAndValidateParameters(Request, m => m.Address2);
            viewModel.ParseAndValidateParameters(Request, m => m.Address3);
            viewModel.ParseAndValidateParameters(Request, m => m.TownCity);
            viewModel.ParseAndValidateParameters(Request, m => m.County);
            viewModel.ParseAndValidateParameters(Request, m => m.Country);
            viewModel.ParseAndValidateParameters(Request, m => m.PostCode);
            viewModel.ParseAndValidateParameters(Request, m => m.IsUkAddress);
            viewModel.ParseAndValidateParameters(Request, m => m.Reason);

            if (viewModel.HasAnyErrors())
            {
                viewModel.Organisation = organisation;
                viewModel.Action = ManuallyChangeOrganisationAddressViewModelActions.ManualChange;
                return View("ManuallyChangeOrganisationAddress", viewModel);
            }

            viewModel.Organisation = organisation;
            viewModel.Action = ManuallyChangeOrganisationAddressViewModelActions.CheckChangesManual;
            return View("ConfirmAddressChange", viewModel);
        }

        private IActionResult CheckChangesAction(ChangeOrganisationAddressViewModel viewModel, Organisation organisation)
        {
            if (viewModel.Action == ManuallyChangeOrganisationAddressViewModelActions.CheckChangesManual)
            {
                OptOrganisationOutOfCompaniesHouseUpdates(organisation);
            }

            SaveChangesAndAuditAction(viewModel, organisation);

            return View("SuccessfullyChangedOrganisationAddress", organisation);
        }

        private void SaveChangesAndAuditAction(ChangeOrganisationAddressViewModel viewModel, Organisation organisation)
        {
            string oldAddressString = organisation.GetAddressString();

            RetireOldAddress(organisation);

            OrganisationAddress newOrganisationAddress = CreateOrganisationAddressFromViewModel(viewModel);
            AddNewAddressToOrganisation(newOrganisationAddress, organisation);

            dataRepository.SaveChangesAsync().Wait();

            auditLogger.AuditChangeToOrganisation(
                this,
                AuditedAction.AdminChangeOrganisationAddress,
                organisation,
                new
                {
                    Action = viewModel.Action,
                    OldAddress = oldAddressString,
                    NewAddress = newOrganisationAddress.GetAddressString(),
                    NewAddressId = newOrganisationAddress.AddressId,
                    Reason = viewModel.Reason
                });
        }

        private static void RetireOldAddress(Organisation organisation)
        {
            OrganisationAddress oldOrganisationAddress = organisation.GetAddress();
            oldOrganisationAddress.Status = AddressStatuses.Retired;
            oldOrganisationAddress.StatusDate = VirtualDateTime.Now;
            oldOrganisationAddress.Modified = VirtualDateTime.Now;
        }

        private OrganisationAddress CreateOrganisationAddressFromViewModel(ChangeOrganisationAddressViewModel viewModel)
        {
            var organisationAddress = new OrganisationAddress {
                PoBox = viewModel.PoBox,
                Address1 = viewModel.Address1,
                Address2 = viewModel.Address2,
                Address3 = viewModel.Address3,
                TownCity = viewModel.TownCity,
                County = viewModel.County,
                Country = viewModel.Country,
                PostCode = viewModel.PostCode,
                Status = AddressStatuses.Active,
                StatusDate = VirtualDateTime.Now,
                StatusDetails = viewModel.Reason,
                Modified = VirtualDateTime.Now,
                Created = VirtualDateTime.Now,
                Source = "Service Desk",
            };

            if (viewModel.IsUkAddress.HasValue)
            {
                organisationAddress.IsUkAddress = (viewModel.IsUkAddress == ManuallyChangeOrganisationAddressIsUkAddress.Yes);
            }

            return organisationAddress;
        }

        private void AddNewAddressToOrganisation(OrganisationAddress organisationAddress, Organisation organisation)
        {
            organisationAddress.OrganisationId = organisation.OrganisationId;
            organisation.OrganisationAddresses.Add(organisationAddress);
            organisation.LatestAddress = organisationAddress;

            dataRepository.Insert(organisationAddress);
        }

        private void OptOrganisationOutOfCompaniesHouseUpdates(Organisation organisation)
        {
            organisation.OptedOutFromCompaniesHouseUpdate = true;
            dataRepository.SaveChangesAsync().Wait();
        }

    }
}