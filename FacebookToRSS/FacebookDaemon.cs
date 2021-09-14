using System;
using System.Globalization;
using System.IO;
using System.Linq;
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

                        if (postDate == null)
                            throw new FacebookException("Fail to extract post date (class timestampContent not found)");

                        if (!DateTime.TryParseExact(postDate, "d MMMM, HH:mm", _cultureInfo, DateTimeStyles.None, out DateTime postDateTime) &&
                            !DateTime.TryParseExact(postDate, "d MMMM", _cultureInfo, DateTimeStyles.None, out postDateTime))
                        {
                            if (!TimeSpan.TryParseExact(postDate, "%h\\ \\h", _cultureInfo, out TimeSpan postDuration) &&
                                !TimeSpan.TryParseExact(postDate, "%m\\ \\m", _cultureInfo, out postDuration))
                                throw new FacebookException($"Date format invalid for post: {postDate}");
                            postDateTime = DateTime.Now - postDuration;
                        }

                        if (postDateTime <= previousLastMessageDate)
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
    }
}
