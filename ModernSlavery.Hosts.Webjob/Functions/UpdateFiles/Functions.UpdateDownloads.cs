﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ModernSlavery.Core.Models;
using ModernSlavery.Entities;
using ModernSlavery.Entities.Enums;
using ModernSlavery.Extensions;
using Microsoft.Azure.WebJobs;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ModernSlavery.Core.Classes;

namespace ModernSlavery.WebJob
{
    public partial class Functions
    {

        //Update data for viewing service
        public async Task UpdateDownloadFiles([TimerTrigger("00:01:00:00", RunOnStartup = true)]
            TimerInfo timer,
            ILogger log)
        {
            try
            {
                await UpdateDownloadFilesAsync(log);
            }
            catch (Exception ex)
            {
                //Send Email to GEO reporting errors
                await _Messenger.SendGeoMessageAsync("GPG - WEBJOBS ERROR", ex.Message);
                //Rethrow the error
                throw;
            }
        }

        //Update GPG download file
        public async Task UpdateDownloadFilesAsync(ILogger log, string userEmail = null, bool force = false)
        {
            if (RunningJobs.Contains(nameof(UpdateDownloadFiles)))
            {
                return;
            }

            try
            {
                List<int> returnYears = _CommonBusinessLogic.DataRepository.GetAll<Return>()
                    .Where(r => r.Status == ReturnStatuses.Submitted)
                    .Select(r => r.AccountingDate.Year)
                    .Distinct()
                    .ToList();

                //Get the downloads location
                string downloadsLocation = _CommonBusinessLogic.GlobalOptions.DownloadsLocation;

                //Ensure we have a directory
                if (!await _CommonBusinessLogic.FileRepository.GetDirectoryExistsAsync(downloadsLocation))
                {
                    await _CommonBusinessLogic.FileRepository.CreateDirectoryAsync(downloadsLocation);
                }

                foreach (int year in returnYears)
                {
                    //If another server is already in process of creating a file then skip

                    string downloadFilePattern = $"GPGData_{year}-{year + 1}.csv";
                    IEnumerable<string> files = await _CommonBusinessLogic.FileRepository.GetFilesAsync(downloadsLocation, downloadFilePattern);
                    string oldDownloadFilePath = files.FirstOrDefault();

                    //Skip if the file already exists and is newer than 1 hour or older than 1 year
                    if (oldDownloadFilePath != null && !force)
                    {
                        DateTime lastWriteTime = await _CommonBusinessLogic.FileRepository.GetLastWriteTimeAsync(oldDownloadFilePath);
                        if (lastWriteTime.AddHours(1) >= VirtualDateTime.Now || lastWriteTime.AddYears(2) <= VirtualDateTime.Now)
                        {
                            continue;
                        }
                    }

                    List<Return> returns = await _CommonBusinessLogic.DataRepository.GetAll<Return>()
                        .Where(
                            r => r.AccountingDate.Year == year
                                 && r.Status == ReturnStatuses.Submitted
                                 && r.Organisation.Status == OrganisationStatuses.Active)
                        .ToListAsync();
                    returns.RemoveAll(r => r.Organisation.OrganisationName.StartsWithI(_CommonBusinessLogic.GlobalOptions.TestPrefix));

                    List<DownloadResult> downloadData = returns.ToList()
                        .Select(r => DownloadResult.Create(r))
                        .OrderBy(d => d.EmployerName)
                        .ToList();

                    string newFilePath =
                        _CommonBusinessLogic.FileRepository.GetFullPath(Path.Combine(downloadsLocation, $"GPGData_{year}-{year + 1}.csv"));
                    try
                    {
                        if (downloadData.Any())
                        {
                            await _CommonBusinessLogic.FileRepository.SaveCSVAsync(downloadData, newFilePath, oldDownloadFilePath);
                        }
                        else if (!string.IsNullOrWhiteSpace(oldDownloadFilePath))
                        {
                            await _CommonBusinessLogic.FileRepository.DeleteFileAsync(oldDownloadFilePath);
                        }
                    }
                    catch (Exception ex)
                    {
                        log.LogError(ex, ex.Message);
                    }
                }

                if (force && !string.IsNullOrWhiteSpace(userEmail))
                {
                    try
                    {
                        await _Messenger.SendMessageAsync(
                            "UpdateDownloadFiles complete",
                            userEmail,
                            "The update of the download files completed successfully.");
                    }
                    catch (Exception ex)
                    {
                        log.LogError(ex, ex.Message);
                    }
                }
            }
            finally
            {
                RunningJobs.Remove(nameof(UpdateDownloadFiles));
            }
        }

    }
}