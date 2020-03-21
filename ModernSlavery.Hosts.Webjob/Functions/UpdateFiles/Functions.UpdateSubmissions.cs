﻿using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using ModernSlavery.SharedKernel;

namespace ModernSlavery.WebJob
{
    public partial class Functions
    {

        public async Task UpdateSubmissions([TimerTrigger(typeof(EveryWorkingHourSchedule), RunOnStartup = true)]
            TimerInfo timer,
            ILogger log)
        {
            try
            {
                string filePath = Path.Combine(_CommonBusinessLogic.GlobalOptions.DownloadsPath, Filenames.OrganisationSubmissions);

                //Dont execute on startup if file already exists
                if (!StartedJobs.Contains(nameof(UpdateSubmissions))
                    && await _CommonBusinessLogic.FileRepository.GetAnyFileExistsAsync(
                        _CommonBusinessLogic.GlobalOptions.DownloadsPath,
                        $"{Path.GetFileNameWithoutExtension(Filenames.OrganisationSubmissions)}*{Path.GetExtension(Filenames.OrganisationSubmissions)}")
                )
                {
                    return;
                }

                await UpdateSubmissionsAsync(filePath);

                log.LogDebug($"Executed {nameof(UpdateSubmissions)}:successfully");
            }
            catch (Exception ex)
            {
                string message = $"Failed {nameof(UpdateSubmissions)}:{ex.Message}";

                //Send Email to GEO reporting errors
                await _Messenger.SendGeoMessageAsync("GPG - WEBJOBS ERROR", message);
                //Rethrow the error
                throw;
            }
            finally
            {
                StartedJobs.Add(nameof(UpdateSubmissions));
            }
        }

        public async Task UpdateSubmissionsAsync(string filePath)
        {
            if (RunningJobs.Contains(nameof(UpdateSubmissions)))
            {
                return;
            }

            RunningJobs.Add(nameof(UpdateSubmissions));
            try
            {
                await WriteRecordsPerYearAsync(
                        filePath,
                        year => Task.FromResult(_SubmissionBusinessLogic.GetSubmissionsFileModelByYear(year).ToList()))
                    .ConfigureAwait(false);
            }
            finally
            {
                RunningJobs.Remove(nameof(UpdateSubmissions));
            }
        }

    }
}