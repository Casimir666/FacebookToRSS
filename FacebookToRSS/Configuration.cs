using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace FacebookToRSS
{
    public class Configuration
    {
        private static string _configurationFile;

        public string SenderAddress { get; set; }
        public string Recipients { get; set; }
        public string ProxyHost { get; set; }
        public int ProxyPort { get; set; }
        public string SmtpServer { get; set; }
        public int SmtpPort { get; set; }
        public string SenderPassword { get; set; }
        public DateTime LastFacebookMessageDate { get; set; }
        public string LastFacebookMessageSha256 { get; set; }
        public TimeSpan RefreshDelay { get; set; }
        public string FacebookUser { get; set; }

        /// <summary>
        /// XPath for the Html elements that contains all posts
        /// </summary>
        public string PostsContainerXPath { get; set; }
        /// <summary>
        /// XPath to extract timestamp from a single post
        /// </summary>
        public string TimestampXPath { get; set; }
        /// <summary>
        /// Element ignored in post container element
        /// </summary>
        public string IgnoredClassname { get; set; }

        public static Configuration Default;

        static Configuration()
        {
            _configurationFile = Path.GetFullPath("Configuration.json");

            Logger.LogMessage($"Loading configuration from {_configurationFile}");
            if (File.Exists(_configurationFile))
            {
                Default = JsonConvert.DeserializeObject<Configuration>(File.ReadAllText(_configurationFile));
            }
        }

        public static async Task SaveAsync(CancellationToken cancellationToken = default)
        {
            await File.WriteAllTextAsync(_configurationFile, JsonConvert.SerializeObject(Default, Formatting.Indented), cancellationToken);
        }
    }
}
