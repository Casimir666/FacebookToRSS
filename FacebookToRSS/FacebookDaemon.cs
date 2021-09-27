using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using HtmlAgilityPack;

namespace FacebookToRSS
{
    class FacebookDaemon
    {
        private readonly CultureInfo _cultureInfo = new CultureInfo("fr-FR");

        public async Task RunAsync(CancellationToken cancellationToken)
        {
            var sender = new MailSender();

            Logger.LogMessage("Facebook daemon is running, press Ctrl+C to quit...");
            var web = new HtmlWeb();
            Logger.LogMessage($"Web browser user agent: {web.UserAgent}");
            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    HtmlDocument document;
                    var previousLastMessageDate = Configuration.Default?.LastFacebookMessageDate ?? DateTime.MinValue;

                    var postUrl = $"https://www.facebook.com/pg/{Configuration.Default.FacebookUser}/posts";
                    Logger.LogMessage($"Loading Facebook messages: {postUrl}");
                    try
                    {
                        document = await web.LoadFromWebAsync(postUrl, cancellationToken);
                        // Dump Html received for debugging
                        await File.WriteAllTextAsync(Path.GetFullPath("facebook.html"), document.DocumentNode.OuterHtml, cancellationToken);
                    }
                    catch (Exception ex)
                    {
                        Logger.LogMessage($"Fail to load facebook posts: {ex.Message}");
                        return;
                    }

                    var postContainer = document.DocumentNode.SelectSingleNode(Configuration.Default.PostsContainerXPath);
                    if (postContainer == null || postContainer.ChildNodes.Count == 0)
                        throw new FacebookException($"Cannot find posts html element (xpath: {Configuration.Default.PostsContainerXPath}).");

                    foreach (var post in postContainer.ChildNodes.Reverse())
                    {
                        if (post.Attributes["class"]?.Value == Configuration.Default.IgnoredClassname)
                            continue;

                        var postDate = post.SelectSingleNode(Configuration.Default.TimestampXPath)?.InnerText?.Trim();
                        var cleanedPost = Clean(post).OuterHtml;
                        var postSha256 = cleanedPost != null ? BitConverter.ToString(SHA256.Create().ComputeHash(Encoding.UTF8.GetBytes(cleanedPost))).Replace("-", "") : "";

                        Logger.LogMessage($"{postDate} {postSha256}");

                        if (postDate == null)
                            throw new FacebookException("Fail to extract post date (class timestampContent not found)");

                        var postDateTime = Utilities.ParseFacebookDate(postDate, _cultureInfo, DateTime.Now);

                        if (postDateTime <= previousLastMessageDate || postSha256 == Configuration.Default.LastFacebookMessageSha256)
                            continue;

                        try
                        {
#if !DEBUG
                            var html = post.OuterHtml.Replace($"/{Configuration.Default.FacebookUser}/posts", $"https://facebook.com/{Configuration.Default.FacebookUser}/posts");
                            await sender.SendAsync($"Message Facebook du {postDateTime.ToString("f", _cultureInfo)}", Configuration.Default.Recipients, $"<html><body>{html}</body></html>", cancellationToken);
                            Logger.LogMessage($"Message from {postDateTime.ToString("f", _cultureInfo)} sent.");
#endif
                            await Task.Delay(4000, cancellationToken);
                        }
                        catch (Exception ex)
                        {
                            Logger.LogMessage($"Cannot send mail: {ex.Message}");
                        }

                        Configuration.Default.LastFacebookMessageDate = postDateTime;
                        Configuration.Default.LastFacebookMessageSha256 = postSha256;
                        await Configuration.SaveAsync(cancellationToken);
                    }

                    Logger.LogMessage($"Done, waiting for {Configuration.Default.RefreshDelay}.");
                    await Task.Delay(Configuration.Default.RefreshDelay, cancellationToken);
                }
            }
            catch (TaskCanceledException)
            {
                Logger.LogMessage("FacebookToRSS is stopped.");
            }
            catch (Exception ex)
            {
                Logger.LogMessage("Unexpected error: " + ex.Message);
                Logger.LogMessage(ex.StackTrace);

                try
                {
                    await sender.SendAsync($"Erreur inattendue!", Configuration.Default?.Recipients.Split(";").First(), $"Le script de surveillance Facebook s'est arrêté avec l'erreur suivante : {ex.Message}\n{ex.StackTrace}", cancellationToken);
                }
                catch (Exception ex2)
                {
                    Logger.LogMessage($"Fail to send crash report mail: {ex2.Message}");
                }
            }
        }

        private HtmlNode Clean(HtmlNode clone)
        {
            var res = clone.Clone();

            void DoClean(HtmlNode node)
            {
                foreach (var nodeAttribute in node.Attributes.ToList())
                {
                    switch (nodeAttribute.Name)
                    {
                        case "href":
                        case "data-xt":
                        case "aria-describedby":
                        case "id":
                        case "value":
                        case "ajaxify":
                        case "onclick":
                            node.Attributes.Remove(nodeAttribute);
                            break;
                        default:
                            //Console.WriteLine($"{nodeAttribute.Name} : {nodeAttribute.Value}");
                            break;
                    }
                }

                foreach (var childNode in node.ChildNodes)
                {
                    DoClean(childNode);
                }
            }

            DoClean(res);
            return res;
        }
    }
}
