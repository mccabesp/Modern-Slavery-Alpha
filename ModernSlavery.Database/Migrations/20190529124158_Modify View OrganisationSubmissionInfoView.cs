﻿using Microsoft.EntityFrameworkCore.Migrations;

namespace GenderPayGap.Database.Core21.Migrations
{
    public partial class ModifyViewOrganisationSubmissionInfoView : Migration
    {

        private const string viewName = "OrganisationSubmissionInfoView";

        protected override void Up(MigrationBuilder migrationBuilder)
        {
            const string viewModificationScript = " ALTER VIEW [dbo].["
                                                  + viewName
                                                  + "] "
                                                  + " AS "
                                                  + " SELECT ret.OrganisationId "
                                                  + " 	,CONVERT(DATE, ret.AccountingDate) AS LatestReturnAccountingDate "
                                                  + " 	,DATEADD(second, - 1, DATEADD(year, 1, ret.AccountingDate)) AS ReportingDeadline "
                                                  + " 	,CASE ret.[StatusId] "
                                                  + " 		WHEN 0 "
                                                  + " 			THEN 'Unknown' "
                                                  + " 		WHEN 1 "
                                                  + " 			THEN 'Draft' "
                                                  + " 		WHEN 2 "
                                                  + " 			THEN 'Suspended' "
                                                  + " 		WHEN 3 "
                                                  + " 			THEN 'Submitted' "
                                                  + " 		WHEN 4 "
                                                  + " 			THEN 'Retired' "
                                                  + " 		WHEN 5 "
                                                  + " 			THEN 'Deleted' "
                                                  + " 		ELSE 'Error' "
                                                  + " 		END + ' (' + CAST(ret.StatusId AS VARCHAR(2)) + ')' AS latestReturnStatus "
                                                  + " 	,ret.StatusDate AS latestReturnStatusDate "
                                                  + " 	,firstDates.dateFirstReportedInYear "
                                                  + " 	,ret.StatusDetails AS LatestReturnStatusDetails "
                                                  + " 	,CASE  "
                                                  + " 		WHEN ([Modified] > dateadd(second, - 1, dateadd(year, 1, ret.accountingdate))) "
                                                  + " 			THEN 'true' "
                                                  + " 		ELSE 'false' "
                                                  + " 		END AS ReportedLate "
                                                  + " 	,ret.LateReason AS LatestReturnLateReason "
                                                  + " 	,CASE retstat.[StatusId] "
                                                  + " 		WHEN 0 "
                                                  + " 			THEN 'Unknown' "
                                                  + " 		WHEN 1 "
                                                  + " 			THEN 'Draft' "
                                                  + " 		WHEN 2 "
                                                  + " 			THEN 'Suspended' "
                                                  + " 		WHEN 3 "
                                                  + " 			THEN 'Submitted' "
                                                  + " 		WHEN 4 "
                                                  + " 			THEN 'Retired' "
                                                  + " 		WHEN 5 "
                                                  + " 			THEN 'Deleted' "
                                                  + " 		ELSE 'Error' "
                                                  + " 		END + ' (' + CAST(retstat.StatusId AS VARCHAR(2)) + ')' AS StatusId "
                                                  + " 	,retstat.StatusDate "
                                                  + " 	,retstat.StatusDetails "
                                                  + " 	,ret.Modifications AS ReturnModifiedFields "
                                                  + " 	,CASE ret.[EHRCResponse] "
                                                  + " 		WHEN 1 "
                                                  + " 			THEN 'true' "
                                                  + " 		ELSE 'false' "
                                                  + " 		END AS EHRCResponse "
                                                  + " 	,ret.FirstName + ' ' + ret.LastName + ' [' + ret.JobTitle + ']' AS SubmittedBy "
                                                  + " 	,CASE  "
                                                  + " 		WHEN ( "
                                                  + " 				[MinEmployees] = 0 "
                                                  + " 				AND [MaxEmployees] = 0 "
                                                  + " 				) "
                                                  + " 			THEN 'Not provided' "
                                                  + " 		WHEN ( "
                                                  + " 				[MinEmployees] = 0 "
                                                  + " 				AND [MaxEmployees] = 249 "
                                                  + " 				) "
                                                  + " 			THEN 'Employees 0 to 249' "
                                                  + " 		WHEN ( "
                                                  + " 				[MinEmployees] = 250 "
                                                  + " 				AND [MaxEmployees] = 499 "
                                                  + " 				) "
                                                  + " 			THEN 'Employees 250 to 499' "
                                                  + " 		WHEN ( "
                                                  + " 				[MinEmployees] = 500 "
                                                  + " 				AND [MaxEmployees] = 999 "
                                                  + " 				) "
                                                  + " 			THEN 'Employees 500 to 999' "
                                                  + " 		WHEN ( "
                                                  + " 				[MinEmployees] = 1000 "
                                                  + " 				AND [MaxEmployees] = 4999 "
                                                  + " 				) "
                                                  + " 			THEN 'Employees 1,000 to 4,999' "
                                                  + " 		WHEN ( "
                                                  + " 				[MinEmployees] = 5000 "
                                                  + " 				AND [MaxEmployees] = 19999 "
                                                  + " 				) "
                                                  + " 			THEN 'Employees 5,000 to 19,999' "
                                                  + " 		WHEN ( "
                                                  + " 				[MinEmployees] = 20000 "
                                                  + " 				AND [MaxEmployees] = 2147483647 "
                                                  + " 				) "
                                                  + " 			THEN 'Employees 20,000 or more' "
                                                  + " 		ELSE 'Error' "
                                                  + " 		END AS OrganisationSize "
                                                  + " 	,ret.DiffMeanHourlyPayPercent "
                                                  + " 	,ret.DiffMedianHourlyPercent "
                                                  + " 	,ret.DiffMeanBonusPercent "
                                                  + " 	,ret.DiffMedianBonusPercent "
                                                  + " 	,ret.MaleMedianBonusPayPercent "
                                                  + " 	,ret.FemaleMedianBonusPayPercent "
                                                  + " 	,ret.MaleLowerPayBand "
                                                  + " 	,ret.FemaleLowerPayBand "
                                                  + " 	,ret.MaleMiddlePayBand "
                                                  + " 	,ret.FemaleMiddlePayBand "
                                                  + " 	,ret.MaleUpperPayBand "
                                                  + " 	,ret.FemaleUpperPayBand "
                                                  + " 	,ret.MaleUpperQuartilePayBand "
                                                  + " 	,ret.FemaleUpperQuartilePayBand "
                                                  + " 	,ret.CompanyLinkToGPGInfo "
                                                  + " FROM dbo.[Returns] AS ret "
                                                  + " LEFT OUTER JOIN dbo.ReturnStatus AS retstat ON retstat.ReturnId = ret.ReturnId "
                                                  + " LEFT OUTER JOIN ( "
                                                  + "     SELECT [OrganisationId] "
                                                  + "         ,[AccountingDate] "
                                                  + "         ,min([Modified]) AS dateFirstReportedInYear "
                                                  + "     FROM [dbo].[Returns] AS ret "
                                                  + "     JOIN [dbo].[ReturnStatus] AS retst ON retst.ReturnId = ret.ReturnId "
                                                  + "     GROUP BY [OrganisationId] "
                                                  + "         ,[AccountingDate] "
                                                  + " 	) AS firstDates ON firstDates.OrganisationId = ret.OrganisationId "
                                                  + " 	AND firstDates.AccountingDate = ret.AccountingDate ";

            migrationBuilder.Sql(viewModificationScript);

            const string selectPermissionsScript = " GRANT SELECT " + " 	ON OBJECT::dbo.[" + viewName + "] " + " 	TO [ReportsReaderDv]; ";

            migrationBuilder.Sql(selectPermissionsScript);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            const string viewModificationScript = " ALTER VIEW [dbo].["
                                                  + viewName
                                                  + "] "
                                                  + " AS "
                                                  + " SELECT ret.OrganisationId "
                                                  + " 	,CONVERT(DATE, ret.AccountingDate) AS LatestReturnAccountingDate "
                                                  + " 	,DATEADD(second, - 1, DATEADD(year, 1, ret.AccountingDate)) AS ReportingDeadline "
                                                  + " 	,CASE ret.[StatusId] "
                                                  + " 		WHEN 0 "
                                                  + " 			THEN 'Unknown' "
                                                  + " 		WHEN 1 "
                                                  + " 			THEN 'Draft' "
                                                  + " 		WHEN 2 "
                                                  + " 			THEN 'Suspended' "
                                                  + " 		WHEN 3 "
                                                  + " 			THEN 'Submitted' "
                                                  + " 		WHEN 4 "
                                                  + " 			THEN 'Retired' "
                                                  + " 		WHEN 5 "
                                                  + " 			THEN 'Deleted' "
                                                  + " 		ELSE 'Error' "
                                                  + " 		END + ' (' + CAST(ret.StatusId AS VARCHAR(2)) + ')' AS latestReturnStatus "
                                                  + " 	,ret.StatusDate AS latestReturnStatusDate "
                                                  + " 	,firstDates.dateFirstReportedInYear "
                                                  + " 	,ret.StatusDetails AS LatestReturnStatusDetails "
                                                  + " 	,CASE  "
                                                  + " 		WHEN ([Modified] > dateadd(second, - 1, dateadd(year, 1, ret.accountingdate))) "
                                                  + " 			THEN 'true' "
                                                  + " 		ELSE 'false' "
                                                  + " 		END AS ReportedLate "
                                                  + " 	,ret.LateReason AS LatestReturnLateReason "
                                                  + " 	,CASE retstat.[StatusId] "
                                                  + " 		WHEN 0 "
                                                  + " 			THEN 'Unknown' "
                                                  + " 		WHEN 1 "
                                                  + " 			THEN 'Draft' "
                                                  + " 		WHEN 2 "
                                                  + " 			THEN 'Suspended' "
                                                  + " 		WHEN 3 "
                                                  + " 			THEN 'Submitted' "
                                                  + " 		WHEN 4 "
                                                  + " 			THEN 'Retired' "
                                                  + " 		WHEN 5 "
                                                  + " 			THEN 'Deleted' "
                                                  + " 		ELSE 'Error' "
                                                  + " 		END + ' (' + CAST(retstat.StatusId AS VARCHAR(2)) + ')' AS StatusId "
                                                  + " 	,retstat.StatusDate "
                                                  + " 	,retstat.StatusDetails "
                                                  + " 	,ret.Modifications AS ReturnModifiedFields "
                                                  + " 	,CASE ret.[EHRCResponse] "
                                                  + " 		WHEN 1 "
                                                  + " 			THEN 'true' "
                                                  + " 		ELSE 'false' "
                                                  + " 		END AS EHRCResponse "
                                                  + " 	,ret.FirstName + ' ' + ret.LastName + ' [' + ret.JobTitle + ']' AS SubmittedBy "
                                                  + " 	,CASE  "
                                                  + " 		WHEN ( "
                                                  + " 				[MinEmployees] = 0 "
                                                  + " 				AND [MaxEmployees] = 0 "
                                                  + " 				) "
                                                  + " 			THEN 'Not provided' "
                                                  + " 		WHEN ( "
                                                  + " 				[MinEmployees] = 0 "
                                                  + " 				AND [MaxEmployees] = 249 "
                                                  + " 				) "
                                                  + " 			THEN 'Employees 0 to 249' "
                                                  + " 		WHEN ( "
                                                  + " 				[MinEmployees] = 250 "
                                                  + " 				AND [MaxEmployees] = 499 "
                                                  + " 				) "
                                                  + " 			THEN 'Employees 250 to 499' "
                                                  + " 		WHEN ( "
                                                  + " 				[MinEmployees] = 500 "
                                                  + " 				AND [MaxEmployees] = 999 "
                                                  + " 				) "
                                                  + " 			THEN 'Employees 500 to 999' "
                                                  + " 		WHEN ( "
                                                  + " 				[MinEmployees] = 1000 "
                                                  + " 				AND [MaxEmployees] = 4999 "
                                                  + " 				) "
                                                  + " 			THEN 'Employees 1,000 to 4,999' "
                                                  + " 		WHEN ( "
                                                  + " 				[MinEmployees] = 5000 "
                                                  + " 				AND [MaxEmployees] = 19999 "
                                                  + " 				) "
                                                  + " 			THEN 'Employees 5,000 to 19,999' "
                                                  + " 		WHEN ( "
                                                  + " 				[MinEmployees] = 20000 "
                                                  + " 				AND [MaxEmployees] = 2147483647 "
                                                  + " 				) "
                                                  + " 			THEN 'Employees 20,000 or more' "
                                                  + " 		ELSE 'Error' "
                                                  + " 		END AS OrganisationSize "
                                                  + " 	,ret.DiffMeanHourlyPayPercent "
                                                  + " 	,ret.DiffMedianHourlyPercent "
                                                  + " 	,ret.DiffMeanBonusPercent "
                                                  + " 	,ret.DiffMedianBonusPercent "
                                                  + " 	,ret.MaleMedianBonusPayPercent "
                                                  + " 	,ret.FemaleMedianBonusPayPercent "
                                                  + " 	,ret.MaleLowerPayBand "
                                                  + " 	,ret.FemaleLowerPayBand "
                                                  + " 	,ret.MaleMiddlePayBand "
                                                  + " 	,ret.FemaleMiddlePayBand "
                                                  + " 	,ret.MaleUpperPayBand "
                                                  + " 	,ret.FemaleUpperPayBand "
                                                  + " 	,ret.MaleUpperQuartilePayBand "
                                                  + " 	,ret.FemaleUpperQuartilePayBand "
                                                  + " FROM dbo.[Returns] AS ret "
                                                  + " LEFT OUTER JOIN dbo.ReturnStatus AS retstat ON retstat.ReturnId = ret.ReturnId "
                                                  + " LEFT OUTER JOIN ( "
                                                  + "     SELECT [OrganisationId] "
                                                  + "         ,[AccountingDate] "
                                                  + "         ,min([Modified]) AS dateFirstReportedInYear "
                                                  + "     FROM [dbo].[Returns] AS ret "
                                                  + "     JOIN [dbo].[ReturnStatus] AS retst ON retst.ReturnId = ret.ReturnId "
                                                  + "     GROUP BY [OrganisationId] "
                                                  + "         ,[AccountingDate] "
                                                  + " 	) AS firstDates ON firstDates.OrganisationId = ret.OrganisationId "
                                                  + " 	AND firstDates.AccountingDate = ret.AccountingDate ";

            migrationBuilder.Sql(viewModificationScript);

            const string selectPermissionsScript = " GRANT SELECT " + " 	ON OBJECT::dbo.[" + viewName + "] " + " 	TO [ReportsReaderDv]; ";

            migrationBuilder.Sql(selectPermissionsScript);
        }

    }
}