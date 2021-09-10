using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HtmlAgilityPack;

namespace FacebookToRSS
{
    class FacebookDaemon
    {
        public async Task RunAsync(CancellationToken cancellationToken)
        {
            var sender = new MailSender();

            Logger.LogMessage("Facebook daemon is running, press Ctrl+C to quit...");
            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    Logger.LogMessage("Loading Facebook messages...");
                    var web = new HtmlWeb();
                    HtmlDocument document;
                    var lastDateUpdated = false;
                    var previousLastMessageDate = Configuration.Default?.LastFacebookMessageDate ?? DateTime.MinValue;

                    try
                    {
                        document = await web.LoadFromWebAsync($"https://www.facebook.com/pg/{Configuration.Default.FacebookUser}/posts", cancellationToken);
                    }
                    catch (Exception ex)
                    {
                        Logger.LogMessage($"Fail to load facebook posts: {ex.Message}");
                        return;
                    }

                    var postContainer = document.DocumentNode.SelectSingleNode("//div[@class='_1xnd']");
                    if (postContainer == null || postContainer.ChildNodes.Count == 0)
                        throw new FacebookException("Cannot find div element for posts (class name _1xnd not found).");

                    foreach (var post in postContainer.ChildNodes.Reverse())
                    {
                        if (post.Attributes["class"]?.Value == "clearfix uiMorePager stat_elem _52jv")
                            continue;

                        var html = post.OuterHtml.Replace($"/{Configuration.Default.FacebookUser}/posts",
                            $"https://facebook.com/{Configuration.Default.FacebookUser}/posts");
                        var postDate = post.SelectSingleNode(".//span[@class='timestampContent']")?.InnerText?.Trim();

                        if (postDate == null)
                            throw new FacebookException("Fail to extract post date (class timestampContent not found)");

                        if (!DateTime.TryParseExact(postDate, "d MMMM, HH:mm", new System.Globalization.CultureInfo("fr-FR"), System.Globalization.DateTimeStyles.None, out DateTime postDateTime) &&
                            !DateTime.TryParseExact(postDate, "d MMMM", new System.Globalization.CultureInfo("fr-FR"), System.Globalization.DateTimeStyles.None, out postDateTime))
                            throw new FacebookException($"Date format invalid for post: {postDate}");

                        if (postDateTime < previousLastMessageDate)
                            continue;

                        try
                        {
                            await sender.SendAsync($"Message Facebook du {postDate}",  $"<html><body>{html}</body></html>", cancellationToken);
                            await Task.Delay(4000);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Cannot send mail: {ex.Message}");
                        }

                        Logger.LogMessage($"Message from {postDate} sent.");

                        if (!lastDateUpdated)
                        {
                            Configuration.Default.LastFacebookMessageDate = postDateTime;
                            Configuration.Default.Save();
                            lastDateUpdated = true;
                        }
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
                    await sender.SendAsync($"Erreur inattendue!", $"Le script de surveillance Facebook s'est arrêté avec l'erreur suivante : {ex.Message}\n{ex.StackTrace}", cancellationToken);
                }
                catch (Exception ex2)
                {
                    Logger.LogMessage($"Fail to send crash report mail: {ex2.Message}");
                }
            }
        }
    }
}
