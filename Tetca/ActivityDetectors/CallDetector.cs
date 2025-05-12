using System.Collections.Generic;
using System;
using System.Linq;
using Microsoft.Management.Infrastructure;
using System.Diagnostics;
using System.Text.RegularExpressions;
using Microsoft.Management.Infrastructure.Options;
using Tetca.Logic;

namespace Tetca.ActivityDetectors
{
    /// <summary>
    /// Detects whether a call is currently in progress by monitoring specific processes (e.g., Zoom, Teams, Slack)
    /// and their network activity.
    /// </summary>
    public class CallDetector(ICurrentTime currentTime) : IActivityDetector
    {
        /// <summary>
        /// Gets or sets the last time activity was detected. This is updated whenever a call is detected.
        /// </summary>
        public DateTime LastActive { get; set; } = currentTime.Now;

        /// <summary>
        /// Gets or sets a value indicating whether a call is currently in progress.
        /// </summary>
        public bool IsActive { get; set; }

        /// <summary>
        /// Gets the description of the last detected activity. This is used for logging and debugging purposes.
        /// </summary>
        public string LastActivityDescription { get; set; } = "Call";

        /// <summary>
        /// Detects if a call is currently in progress by checking for specific processes and their network activity.
        /// Updates the <see cref="LastActive"/> property if a call is detected.
        /// </summary>
        /// <returns>True if a call is detected; otherwise, false.</returns>
        public bool Detect()
        {
            this.IsActive = this.GetCallInProgress();
            if (this.IsActive)
            {
                this.LastActive = currentTime.Now;
            }

            return this.IsActive;
        }

        /// <summary>
        /// Checks if any monitored processes (e.g., Zoom, Teams, Slack) are currently active and have network activity.
        /// </summary>
        /// <returns>True if any monitored process is detected with network activity; otherwise, false.</returns>
        private bool GetCallInProgress()
        {
            var processes = Process.GetProcesses();
            processes = processes.Where(p => Regex.IsMatch(p.ProcessName, "zoom$|teams$|slack$", RegexOptions.IgnoreCase)).ToArray();
            if (processes.Length > 0)
            {
                var pids = processes.Select(p => (uint)p.Id).ToHashSet();
                string query = "SELECT CreationTime, InstanceID, LocalAddress, LocalPort, OwningProcess FROM MSFT_NetUDPEndpoint";
                using var session = CimSession.Create("localhost", new DComSessionOptions());
                var queryInstances = session.QueryInstances(@"ROOT/StandardCimv2", "WQL", query).ToList();
                var ignoreAddresses = new HashSet<string>()
                    {
                        "127.0.0.1",
                        "::",
                    };
                queryInstances = queryInstances.Where(q => !ignoreAddresses.Contains((string)q.CimInstanceProperties["LocalAddress"].Value)).ToList();
                queryInstances = queryInstances.Where(q => pids.Contains((uint)q.CimInstanceProperties["OwningProcess"].Value)).ToList();
                if (queryInstances.Count > 0)
                {
                    return true;
                }
            }

            return false;
        }
    }
}