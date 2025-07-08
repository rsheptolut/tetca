using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace Tetca.Logic
{
    /// <summary>
    /// Handles the recording of work hours, breaks, and daily summaries.
    /// Provides functionality to log activities, save and load daily reports, and calculate work statistics.
    /// </summary>
    public class WorkRecorder : IDisposable
    {
        private WorkHoursDailyStorageModel dayModel;
        private DateTime lastFlushed;
        private readonly TimeSpan maxUnflushedTime;
        private readonly TimeSpan minBreak;
        private readonly TimeSpan latestGoToBedTime;
        private readonly ICurrentTime currentTime;
        private readonly ILogger<WorkRecorder> logger;
        private readonly JsonSerializerOptions jsonSerializerOptions;

        /// <summary>
        /// Initializes a new instance of the <see cref="WorkRecorder"/> class.
        /// </summary>
        /// <param name="minBreak">The minimum duration for a break to be considered valid.</param>
        /// <param name="latestGoToBedTime">The latest time considered as the end of the day.</param>
        /// <param name="currentTime">An instance of <see cref="ICurrentTime"/> to provide the current time.</param>
        /// <param name="logger">An instance of <see cref="ILogger{WorkRecorder}"/> for logging purposes.</param>
        public WorkRecorder(Settings settings, ICurrentTime currentTime, ILogger<WorkRecorder> logger)
        {
            this.minBreak = settings.MinBreak;
            this.latestGoToBedTime = settings.LatestGoToBedTimeForReporting;
            this.currentTime = currentTime;
            this.logger = logger;
            this.jsonSerializerOptions = new JsonSerializerOptions
            {
                AllowTrailingCommas = true,
                PropertyNameCaseInsensitive = true,
                ReadCommentHandling = JsonCommentHandling.Skip,
                WriteIndented = true
            };
            this.maxUnflushedTime = settings.MinBreak;
        }

        /// <summary>
        /// Gets the total work time for the current day, adjusted for an 8-hour workday norm.
        /// </summary>
        /// <returns>The total worked time for the day as a <see cref="TimeSpan"/>.</returns>
        public TimeSpan GetTotalNormie8hWorkedToday()
        {
            if (this.dayModel == null)
            {
                return TimeSpan.Zero;
            }

            return this.dayModel.Summary.TotalWorkedNormie8h;
        }

        /// <summary>
        /// Logs the current activity, updating the work and break durations as necessary.
        /// </summary>
        internal void LogActivity()
        {
            if (this.dayModel == null)
            {
                this.TryLoadDayFromFile();
            }

            // Remember last activity
            var lastActivity = this.dayModel.Activities.LastOrDefault();

            var now = this.currentTime.Now;

            if (lastActivity != null)
            {
                var breakDuration = now - lastActivity.End;

                if (breakDuration < this.minBreak)
                {
                    // Too small for a break, so just continue last acitivity
                    lastActivity.End = now;
                    lastActivity.BreakEnd = now;
                    this.dayModel.Summary.TotalWorked += breakDuration;
                }
                else
                {
                    // Proper break, so adding a new activity

                    // First clean up any previous activities that are too small (should be at most one though)

                    while (this.dayModel.Activities.LastOrDefault() is Activity act && act.WorkDuration < TimeSpan.FromSeconds(10))
                    {
                        this.dayModel.Activities.Remove(act);
                        this.dayModel.Summary.TotalWorked -= act.WorkDuration;
                        this.dayModel.Summary.TotalBreaks -= act.BreakDuration;
                    }

                    // Now adjust BreakEnd of the last activity and TotalBreaks
                    lastActivity = this.dayModel.Activities.LastOrDefault();
                    if (lastActivity != null)
                    {
                        var breakDurationAlreadyAccounted = lastActivity.BreakDuration;
                        lastActivity.BreakEnd = now;
                        breakDuration = now - lastActivity.End;
                        this.dayModel.Summary.TotalBreaks += breakDuration - breakDurationAlreadyAccounted;
                        lastActivity = null; // This will trigger adding a new activity entry
                    }
                }

                this.dayModel.Summary.End = now;
            }

            if (this.GetToday() > this.dayModel.Summary.Day)
            {
                // It's the new day! Let's switch the file

                if (this.dayModel.Activities.Count > 0)
                {
                    // Don't need to keep track of the last break, so just erase the break part there
                    var finalActivity = this.dayModel.Activities.Last();
                    this.dayModel.Summary.TotalBreaks -= finalActivity.BreakDuration;
                    finalActivity.BreakEnd = finalActivity.End;
                }

                this.SaveDayToFile();
                this.TryLoadDayFromFile();
            }

            if (lastActivity == null)
            {
                // This branch is for the first activity of the day, OR right after detecting a break
                var newActivity = new Activity
                {
                    Begin = now,
                    End = now,
                    BreakEnd = now,
                };
                this.dayModel.Activities.Add(newActivity);
                this.dayModel.Summary.TotalWorked += newActivity.WorkDuration;
                this.dayModel.Summary.End = now;
                this.SaveDayToFile();
            }
            else
            {
                if (now - this.lastFlushed > this.maxUnflushedTime && this.dayModel != null)
                {
                    this.SaveDayToFile();
                    this.lastFlushed = now;
                }
                else
                {
                    RecalcGrandTotals();
                }
            }
        }

        private DateTime GetToday()
        {
            var today = this.currentTime.Now.Date;

            if (this.currentTime.Now.TimeOfDay < this.latestGoToBedTime)
            {
                today = today.AddDays(-1);
            }

            return today;
        }

        private static string GetDayReportFileName(DateTime date)
        {
            var todayStr = date.ToString("yyyyMMdd");
            var dailyFileName = $"daily_{todayStr}.json";
            return dailyFileName;
        }

        public DateTime GetLastBreakEnded()
        {
            if (this.dayModel?.Activities?.Count > 0)
            {
                return this.dayModel.Activities.Last().Begin;
            }

            return this.currentTime.Now;
        }

        /// <summary>
        /// Attempts to load the current day's data from a file. If no file exists, initializes a new day model.
        /// </summary>
        public void TryLoadDayFromFile()
        {
            var reportsDirectory = GetReportsDirectory();
            string dailyFilePath = Path.Combine(reportsDirectory, GetDayReportFileName(this.GetToday()));

            this.dayModel = null;
            if (File.Exists(dailyFilePath))
            {
                try
                {
                    this.dayModel = this.ReadFromJsonFile<WorkHoursDailyStorageModel>(dailyFilePath);
                }
                catch (Exception ex)
                {
                    this.logger.LogError(ex, "Failed to load daily report file {dailyFilePath}", dailyFilePath);
                    File.Delete(dailyFilePath + ".bak");
                    File.Move(dailyFilePath, dailyFilePath + ".bak");
                }
            }
            
            if (this.dayModel == null)
            {
                this.dayModel = new WorkHoursDailyStorageModel();
                this.dayModel.Summary = new DaySummary
                {
                    Begin = this.currentTime.Now
                };
            }

            this.dayModel.Summary.Day = this.GetToday();
            this.ConsiderRefreshingSummaryReport();
        }

        /// <summary>
        /// Saves the current day's data to a file.
        /// </summary>
        private void SaveDayToFile()
        {
            if (this.dayModel == null)
            {
                return;
            }

            var reportsDirectory = GetReportsDirectory();
            if (!Directory.Exists(reportsDirectory))
            {
                Directory.CreateDirectory(reportsDirectory);
            }

            string dailyFilePath = Path.Combine(reportsDirectory, GetDayReportFileName(this.dayModel.Summary.Day));

            if (this.dayModel.Activities.Count > 0)
            {
                this.RecalcGrandTotals();
                this.WriteAsJsonFile(dailyFilePath, this.dayModel);
            }
        }

        private void RecalcGrandTotals()
        {
            var totalActivity = this.dayModel.Activities.Sum(a => a.WorkDuration.Ticks);
            var totalBreaks = this.dayModel.Activities.Sum(a => a.BreakDuration.Ticks);
            this.dayModel.Summary.RecalcGrandTotals(new TimeSpan(totalActivity), new TimeSpan(totalBreaks));
        }

        private static string GetReportsDirectory()
        {
            return Path.Combine(Environment.CurrentDirectory, "..", "reports");
        }

        private void ConsiderRefreshingSummaryReport()
        {
            var reportsDirectory = GetReportsDirectory();
            var summaryFileName = Path.Combine(reportsDirectory, "summary.json");
            var directory = new DirectoryInfo(reportsDirectory);
            if (!directory.Exists)
            {
                return;
            }

            var files = directory.GetFiles();
            var summaryFile = files.FirstOrDefault(f => f.Name == "summary.json");
            var lastUpdatedSummary = summaryFile?.LastWriteTime;
            var newFiles = files.Where(f => f.Name.StartsWith("daily_") && f.Extension == ".json" && (lastUpdatedSummary == null || f.LastWriteTime > lastUpdatedSummary)).ToList();
            if (newFiles.Count > 0)
            {
                var totalModel = new List<DaySummary>();

                if (summaryFile?.Exists == true)
                {
                    totalModel = this.ReadFromJsonFile<List<DaySummary>>(summaryFileName);
                }

                foreach (var f in newFiles)
                {
                    try
                    {
                        var model = this.ReadFromJsonFile<WorkHoursDailyStorageModel>(f.FullName);
                        var day = model.Summary.Day;
                        var existing = totalModel.FindIndex(d => d.Day == day);
                        if (existing >= 0)
                        {
                            totalModel[existing] = model.Summary;
                        }
                        else
                        {
                            totalModel.Add(model.Summary);
                        }
                    }
                    catch (Exception ex)
                    {
                        this.logger.LogError(ex, "Failed to load daily report file {dailyFilePath} to incorporate it into a summary.", f.FullName);
                    }
                }

                this.WriteAsJsonFile(summaryFileName, totalModel);
            }
        }

        private T ReadFromJsonFile<T>(string fileName)
        {
            return JsonSerializer.Deserialize<T>(File.ReadAllText(fileName), this.jsonSerializerOptions);
        }

        private void WriteAsJsonFile<T>(string fileName, T model)
        {
            var json = JsonSerializer.Serialize(model, this.jsonSerializerOptions);
            using (var fs = new FileStream(fileName + ".tmp", FileMode.Create, FileAccess.Write, FileShare.None))
            using (var sw = new StreamWriter(fs))
            {
                sw.Write(json);
                sw.Flush();
                fs.Flush(true); // Ensures data is flushed to disk, not just OS cache
            }
            try
            {
                File.Move(fileName, fileName + ".bak", true);

            }
            catch { }

            try
            {
                File.Move(fileName + ".tmp", fileName, true);
            }
            catch { }
        }

        /// <summary>
        /// Disposes of the <see cref="WorkRecorder"/> instance, ensuring any unsaved data is written to a file.
        /// </summary>
        public void Dispose()
        {
            this.SaveDayToFile();
            GC.SuppressFinalize(this);
        }

        internal string GetCurrentReportPath()
        {
            return Path.Combine(GetReportsDirectory(), GetDayReportFileName(this.GetToday()));
        }

        /// <summary>
        /// Represents the storage model for daily work hours, including activities and summary data.
        /// </summary>
        private class WorkHoursDailyStorageModel
        {
            /// <summary>
            /// Gets or sets the list of activities for the day.
            /// </summary>
            public List<Activity> Activities { get; set; } = [];

            /// <summary>
            /// Gets or sets the summary of the day's work and breaks.
            /// </summary>
            public DaySummary Summary { get; set; }
        }

        /// <summary>
        /// Represents a single work activity, including its start and end times, and break duration.
        /// </summary>
        private class Activity
        {
            /// <summary>
            /// Gets or sets the start time of the activity.
            /// </summary>
            public DateTime Begin { get; set; }

            /// <summary>
            /// Gets or sets the end time of the activity.
            /// </summary>
            public DateTime End { get; set; }

            /// <summary>
            /// Gets or sets the end time of the break following the activity.
            /// </summary>
            public DateTime BreakEnd { get; set; }

            /// <summary>
            /// Gets the duration of the work activity.
            /// </summary>
            public TimeSpan WorkDuration => this.End - this.Begin;

            /// <summary>
            /// Gets the duration of the break following the activity.
            /// </summary>
            public TimeSpan BreakDuration => this.BreakEnd - this.End;

            /// <summary>
            /// Returns a string representation of the activity.
            /// </summary>
            /// <returns>A string describing the activity.</returns>
            public override string ToString()
            {
                return "{ begin: " + this.Begin.ToString("HH:mm:ss") + ", end: " + this.End.ToString("HH:mm:ss") + ", duration: " + this.WorkDuration.ToString() + " }";
            }
        }

        /// <summary>
        /// Represents a summary of a day's work, including total durations and adjusted work time.
        /// </summary>
        private class DaySummary
        {
            /// <summary>
            /// Gets or sets the date of the summary.
            /// </summary>
            public DateTime Day { get; set; }

            /// <summary>
            /// Gets or sets the total duration of work and breaks for the day.
            /// </summary>
            public TimeSpan TotalDuration { get; set; }

            /// <summary>
            /// Gets or sets the total worked time adjusted for an 8-hour workday norm.
            /// </summary>
            public TimeSpan TotalWorkedNormie8h { get; set; }

            /// <summary>
            /// Gets or sets the total worked time for the day.
            /// </summary>
            public TimeSpan TotalWorked { get; set; }

            /// <summary>
            /// Gets or sets the total break time for the day.
            /// </summary>
            public TimeSpan TotalBreaks { get; set; }

            /// <summary>
            /// Gets or sets the start time of the day's work.
            /// </summary>
            public DateTime Begin { get; set; }

            /// <summary>
            /// Gets or sets the end time of the day's work.
            /// </summary>
            public DateTime End { get; set; }

            /// <summary>
            /// Recalculates the grand totals for the day, including total duration and adjusted work time.
            /// </summary>
            public void RecalcGrandTotals()
            {
                this.TotalDuration = this.TotalWorked + this.TotalBreaks;
                var freeBreakAllowance = this.TotalWorked / 7;
                var breakAllowanceUsedUp = this.TotalBreaks > freeBreakAllowance ? freeBreakAllowance : this.TotalBreaks;
                var workedCorrected = this.TotalWorked + breakAllowanceUsedUp;
                this.TotalWorkedNormie8h = workedCorrected;
            }

            /// <summary>
            /// Recalculates the grand totals for the day using the provided worked and break durations.
            /// </summary>
            /// <param name="totalWorked">The total worked time.</param>
            /// <param name="totalBreaks">The total break time.</param>
            internal void RecalcGrandTotals(TimeSpan totalWorked, TimeSpan totalBreaks)
            {
                this.TotalWorked = totalWorked;
                this.TotalBreaks = totalBreaks;
                this.RecalcGrandTotals();
            }
        }
    }
}
