using System;
using System.Collections.Generic;
using System.Linq;
using ModernSlavery.Entities;
using ModernSlavery.Extensions;
using ModernSlavery.SharedKernel;

namespace ModernSlavery.Core.Models
{
    [Serializable]
    public class EmployerRecord
    {
        public Dictionary<string, string> References = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        public long OrganisationId { get; set; }
        public string DUNSNumber { get; set; }
        public string EmployerReference { get; set; }
        public string CompanyNumber { get; set; }
        public DateTime? DateOfCessation { get; set; }

        public string OrganisationName { get; set; }
        public SectorTypes SectorType { get; set; }

        public string NameSource { get; set; }
        public long ActiveAddressId { get; set; }

        public string Address1 { get; set; }
        public string Address2 { get; set; }
        public string Address3 { get; set; }
        public string City { get; set; }
        public string County { get; set; }
        public string Country { get; set; }
        public string PostCode { get; set; }
        public string PoBox { get; set; }
        public string AddressSource { get; set; }

        public bool? IsUkAddress { get; set; }

        public string SicCodeIds { get; set; }
        public string SicSource { get; set; }

        //Public Sector
        public string EmailDomains { get; set; }
        public string RegistrationStatus { get; set; }

        public string SicSectors { get; set; }


        public string GetFullAddress()
        {
            var list = new List<string>();
            list.Add(Address1);
            list.Add(Address2);
            list.Add(Address3);
            list.Add(City);
            list.Add(County);
            list.Add(Country);
            list.Add(PostCode);
            list.Add(PoBox);
            return list.ToDelimitedString(", ");
        }

        public List<string> GetAddressList()
        {
            var list = new List<string>();
            if (!string.IsNullOrWhiteSpace(Address1)) list.Add(Address1);

            if (!string.IsNullOrWhiteSpace(Address2)) list.Add(Address2);

            if (!string.IsNullOrWhiteSpace(Address3)) list.Add(Address3);

            if (!string.IsNullOrWhiteSpace(City)) list.Add(City);

            if (!string.IsNullOrWhiteSpace(County)) list.Add(County);

            if (!string.IsNullOrWhiteSpace(Country)) list.Add(Country);

            if (!string.IsNullOrWhiteSpace(PoBox)) list.Add(PoBox);

            return list;
        }

        public bool HasAnyAddress()
        {
            return !string.IsNullOrWhiteSpace(Address1)
                   || !string.IsNullOrWhiteSpace(Address2)
                   || !string.IsNullOrWhiteSpace(Address3)
                   || !string.IsNullOrWhiteSpace(City)
                   || !string.IsNullOrWhiteSpace(County)
                   || !string.IsNullOrWhiteSpace(Country)
                   || !string.IsNullOrWhiteSpace(PostCode)
                   || !string.IsNullOrWhiteSpace(PoBox);
        }

        public bool IsValidAddress()
        {
            var isUK = Country.IsUK();
            if (isUK)
                return !string.IsNullOrWhiteSpace(Address1)
                       || !string.IsNullOrWhiteSpace(Address2)
                       || !string.IsNullOrWhiteSpace(Address3)
                       || !string.IsNullOrWhiteSpace(City)
                       || !string.IsNullOrWhiteSpace(PostCode)
                       || !string.IsNullOrWhiteSpace(PoBox);

            return !string.IsNullOrWhiteSpace(Country)
                   && (!string.IsNullOrWhiteSpace(Address1)
                       || !string.IsNullOrWhiteSpace(Address2)
                       || !string.IsNullOrWhiteSpace(Address3)
                       || !string.IsNullOrWhiteSpace(City)
                       || !string.IsNullOrWhiteSpace(County)
                       || !string.IsNullOrWhiteSpace(PostCode)
                       || !string.IsNullOrWhiteSpace(PoBox));
        }

        public SortedSet<int> GetSicCodes()
        {
            var codes = new SortedSet<int>();
            foreach (var sicCode in SicCodeIds.SplitI()) codes.Add(sicCode.ToInt32());

            return codes;
        }

        public AddressModel GetAddressModel()
        {
            return new AddressModel
            {
                Address1 = Address1,
                Address2 = Address2,
                Address3 = Address3,
                City = City,
                County = County,
                Country = Country,
                PostCode = PostCode,
                PoBox = PoBox
            };
        }

        public bool IsAuthorised(string emailAddress)
        {
            if (!emailAddress.IsEmailAddress()) throw new ArgumentException("Bad email address");

            if (string.IsNullOrWhiteSpace(EmailDomains)) return false;

            var emailDomains = EmailDomains.SplitI(";")
                .Select(ep => ep.ContainsI("*@") ? ep : ep.Contains('@') ? "*" + ep : "*@" + ep)
                .ToList();
            return emailDomains.Count > 0 && emailAddress.LikeAny(emailDomains);
        }

        public static EmployerRecord Create(Organisation org, long userId = 0)
        {
            OrganisationAddress address = null;
            if (userId > 0) address = org.UserOrganisations.FirstOrDefault(uo => uo.UserId == userId)?.Address;

            if (address == null) address = org.LatestAddress;

            if (address == null)
                return new EmployerRecord
                {
                    OrganisationId = org.OrganisationId,
                    SectorType = org.SectorType,
                    OrganisationName = org.OrganisationName,
                    NameSource = org.GetName()?.Source,
                    EmployerReference = org.EmployerReference,
                    DateOfCessation = org.DateOfCessation,
                    DUNSNumber = org.DUNSNumber,
                    CompanyNumber = org.CompanyNumber,
                    SicSectors = org.GetSicSectorsString(null, ",<br/>"),
                    SicCodeIds = org.GetSicCodeIdsString(),
                    SicSource = org.GetSicSource(),
                    RegistrationStatus = org.GetRegistrationStatus(),
                    References = org.OrganisationReferences.ToDictionary(
                        r => r.ReferenceName,
                        r => r.ReferenceValue,
                        StringComparer.OrdinalIgnoreCase)
                };

            return new EmployerRecord
            {
                OrganisationId = org.OrganisationId,
                SectorType = org.SectorType,
                OrganisationName = org.OrganisationName,
                NameSource = org.GetName()?.Source,
                EmployerReference = org.EmployerReference,
                DateOfCessation = org.DateOfCessation,
                DUNSNumber = org.DUNSNumber,
                CompanyNumber = org.CompanyNumber,
                SicSectors = org.GetSicSectorsString(null, ",<br/>"),
                SicCodeIds = org.GetSicCodeIdsString(),
                SicSource = org.GetSicSource(),
                ActiveAddressId = address.AddressId,
                AddressSource = address.Source,
                Address1 = address.Address1,
                Address2 = address.Address2,
                Address3 = address.Address3,
                City = address.TownCity,
                County = address.County,
                Country = address.Country,
                PostCode = address.PostCode,
                PoBox = address.PoBox,
                IsUkAddress = address.IsUkAddress,
                RegistrationStatus = org.GetRegistrationStatus(),
                References = org.OrganisationReferences.ToDictionary(
                    r => r.ReferenceName,
                    r => r.ReferenceValue,
                    StringComparer.OrdinalIgnoreCase)
            };
        }
    }
}