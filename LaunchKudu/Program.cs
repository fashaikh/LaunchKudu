using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace LaunchKudu
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine("usage: LaunchKudu.exe <siteName>");
                Console.WriteLine("usage: LaunchKudu.exe jitaccess");
                return;
            }

            try
            {
                if (string.Equals(args[0], "jitaccess", StringComparison.OrdinalIgnoreCase))
                {
                    GetJitAccess();
                }
                else
                {
                    Run(args[0]);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                Console.WriteLine(DateTime.Now.ToString("s") + ": " + "To get jitaccess, \"LaunchKudu.exe jitaccess\"");
                Console.WriteLine(DateTime.Now.ToString("s") + ": " + "Warning pin password not supported!");
            }
        }

        static void GetJitAccess()
        {
            const string JitAccessUri = "https://jitaccess.security.core.windows.net/WorkFlowTempAccess.aspx?View=Submit&WorkItemSource=IcM&WorkItemId=22748709&Justification=Investigation&ResourceType=ACIS&AccessLevel=PlatformServiceAdministrator&Scope=Antares";
            var chrome = Environment.ExpandEnvironmentVariables(@"%ProgramFiles(x86)%\Google\Chrome\Application\chrome.exe");
            Console.WriteLine(DateTime.Now.ToString("s") + ": \"{0}\" {1}", chrome, JitAccessUri);
            if (File.Exists(chrome))
            {
                var proc = Process.Start(chrome, JitAccessUri);
                proc.WaitForExit(5000);
            }
        }

        static void Run(string siteName)
        {
            Console.WriteLine(DateTime.Now.ToString("s") + ": " + "Get site info");
            var siteInfo = GetSiteInfo(siteName).Result;

            var inputFile = Path.GetTempFileName();
            var outputFile = Path.GetTempFileName();
            var approvers = Environment.ExpandEnvironmentVariables("%USERNAME%;antst");
            var commandText = string.Format("RunLimitedUserCommand \"GetWebSiteConfig {0} {1} {2} /WebSystemName:{3}\" \"{4}\" \"1075622\" \"\"",
                siteInfo.subscription.name, siteInfo.webspace.name, siteInfo.name, siteInfo.websystem_name, approvers);
            Console.WriteLine(DateTime.Now.ToString("s") + ": " + "Command {0}", commandText);
            File.WriteAllText(inputFile, commandText);

            var fileName = @"\\reddog\builds\branches\rd_wapd_stable_latest_amd64fre\RDTools\ACISApp\ACISApp.exe";
            var arguments = string.Format("-AgreeTerms -extension:\"Antares\" -Endpoint:\"Antares {0}\" -InputFile:{1} -OutFile:{2}", "- WAWSPRODSN1 Geomaster", inputFile, outputFile);
            if (siteInfo.name.StartsWith("mawscanary-"))
            {
                arguments = string.Format("-AgreeTerms -extension:\"Antares\" -Endpoint:\"Antares {0}\" -InputFile:{1} -OutFile:{2}", siteInfo.webspace.stamp.name, inputFile, outputFile);
            }

            Console.WriteLine(DateTime.Now.ToString("s") + ": " + "Run {0} {1}", fileName, arguments);
            var process = CreateProcess(fileName, arguments);
            var handler = new DataReceivedEventHandler((sender, e) =>
            {
                // Prepend line numbers to each line of the output.
                if (!String.IsNullOrEmpty(e.Data))
                {
                    Console.WriteLine(e.Data);
                }
            });

            process.OutputDataReceived += handler;
            process.ErrorDataReceived += handler;

            process.Start();

            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            while (!process.WaitForExit(30000))
            {
                Console.WriteLine(DateTime.Now.ToString("s") + ": " + "Waiting for ACISApp.exe ...");
            }

            process.WaitForExit();

            if (process.ExitCode != 0)
            {
                throw new InvalidOperationException("ACISApp.exe failed with " + process.ExitCode);
            }

            File.Delete(inputFile);

            string username = null;
            string password = null;
            foreach (var line in File.ReadLines(outputFile))
            {
                var parts = line.Split(':').Select(p => p.Trim()).ToArray();
                if (parts[0] == "PublishingUsername")
                {
                    username = parts[1];
                    if (!string.IsNullOrEmpty(password))
                    {
                        break;
                    }
                }
                else if (parts[0] == "PublishingPassword")
                {
                    password = parts[1];
                    if (!string.IsNullOrEmpty(username))
                    {
                        break;
                    }
                }
            }

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                Console.WriteLine(File.ReadAllText(outputFile));
                return;
            }

            File.Delete(outputFile);

            var hidden = "******";
            var scmUri = string.Format("https://{0}:{1}@{2}/basicauth",
                username, hidden, siteInfo.hostnames.First(h => h.hostname_type == 1).hostname);
            var chrome = Environment.ExpandEnvironmentVariables(@"%ProgramFiles(x86)%\Google\Chrome\Application\chrome.exe");
            Console.WriteLine(DateTime.Now.ToString("s") + ": \"{0}\" {1}", chrome, scmUri);
            if (File.Exists(chrome))
            {
                var proc = Process.Start(chrome, scmUri.Replace(hidden, password));
                proc.WaitForExit(5000);
            }
            else
            {
                Console.WriteLine(DateTime.Now.ToString("s") + ": Could not fine {0}!", chrome);
            }
        }

        static Process CreateProcess(string fileName, string arguments)
        {
            var psi = new ProcessStartInfo
            {
                FileName = fileName,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden,
                UseShellExecute = false,
                ErrorDialog = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                Arguments = arguments
            };

            return new Process()
            {
                StartInfo = psi
            };
        }

        static async Task<SiteInfo> GetSiteInfo(string siteName)
        {
            HttpClientHandler handler = new HttpClientHandler()
            {
                UseDefaultCredentials = true
            };

            using (var client = new HttpClient(handler))
            {
                using (var response = await client.GetAsync("https://observer/api/Sites/" + siteName))
                {
                    response.EnsureSuccessStatusCode();
                    var result = await response.Content.ReadAsAsync<SiteInfo[]>();
                    return result.First();
                }
            }
        }

        public class SiteInfo
        {
            public SubscriptionInfo subscription { get; set; }
            public WebSpaceInfo webspace { get; set; }
            public string name { get; set; }
            public string websystem_name { get; set; }
            public HostNameInfo[] hostnames { get; set; }
        }

        public class HostNameInfo
        {
            public string hostname { get; set; }
            public int hostname_type { get; set; }
        }

        public class SubscriptionInfo
        {
            public string name { get; set; }
        }

        public class WebSpaceInfo
        {
            public string name { get; set; }
            public StampInfo stamp { get; set; }
        }

        public class StampInfo
        {
            public string name { get; set; }
        }
    }
}
