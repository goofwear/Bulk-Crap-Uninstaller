/*
    Copyright (c) 2017 Marcin Szeniak (https://github.com/Klocman/)
    Apache License Version 2.0
*/

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Klocman.IO;
using Klocman.Tools;

namespace UninstallTools.Factory
{
    public class WindowsFeatureFactory : IUninstallerFactory
    {
        public IEnumerable<ApplicationUninstallerEntry> GetUninstallerEntries()
        {
            if (Environment.OSVersion.Version < WindowsTools.Windows7)
                return Enumerable.Empty<ApplicationUninstallerEntry>();

            Exception error = null;
            var applicationUninstallers = new List<ApplicationUninstallerEntry>();
            var t = new Thread(() =>
            {
                try
                {
                    applicationUninstallers.AddRange(WmiQueries.GetWindowsFeatures()
                        .Where(x => x.Enabled)
                        .Select(WindowsFeatureToUninstallerEntry));
                }
                catch (Exception ex)
                {
                    error = ex;
                }
            });
            t.Start();

            t.Join(TimeSpan.FromSeconds(40));

            if (error != null)
                throw new IOException("Error while collecting Windows Features, try restarting your computer. If the error persists read the KB957310 article.", error);
            if (t.IsAlive)
            {
                t.Abort();
                throw new TimeoutException("WMI query has hung while collecting Windows Features, try restarting your computer. If the error persists read the KB957310 article.");
            }

            return applicationUninstallers;
        }

        private static ApplicationUninstallerEntry WindowsFeatureToUninstallerEntry(WindowsFeatureInfo info)
        {
            return new ApplicationUninstallerEntry
            {
                RawDisplayName = info.DisplayName,
                Comment = info.Description,
                UninstallString = DismTools.GetDismUninstallString(info.FeatureName, false),
                QuietUninstallString = DismTools.GetDismUninstallString(info.FeatureName, true),
                UninstallerKind = UninstallerType.WindowsFeature,
                Publisher = "Microsoft Corporation",
                IsValid = true,
                Is64Bit = ProcessTools.Is64BitProcess ? MachineType.X64 : MachineType.X86,
                RatingId = "WindowsFeature_" + info.FeatureName
            };
        }
    }
}