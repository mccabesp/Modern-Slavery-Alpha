using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using ModernSlavery.Core.Extensions;

namespace ModernSlavery.Core.Entities
{
    [Serializable]
    [DebuggerDisplay("({Organisation}),({User})")]
    public partial class UserOrganisation
    {
        public string GetReviewCode()
        {
            return Encryption.EncryptQuerystring(UserId + ":" + OrganisationId + ":" +
                                                 VirtualDateTime.Now.ToSmallDateTime());
        }

        public IEnumerable<UserOrganisation> GetAssociatedUsers()
        {
            return Organisation.UserOrganisations.Where(uo =>
                uo.OrganisationId == OrganisationId
                && uo.UserId != UserId
                && uo.PINConfirmedDate != null
                && uo.User.Status == UserStatuses.Active);
        }
    }
}