using HtmlAgilityPack;
using Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace ServiceUtilities
{
    public static class Crawler
    {

        public static async Task Crawl(Configuration configuration, List<Page> VisitedPages, List<string> Robots)
        {
            List<string> SiteMaps = new List<string>();
            LoadRobots(configuration.URL, Robots, configuration.IsHttps, SiteMaps);

            if (configuration.LoadFromSiteMap == 1)
            {
                foreach (string sitemap in SiteMaps)
                {
                    if (sitemap.StartsWith("/"))

                        await Task.Run(() => CrawlSitemap(VisitedPages, configuration.URL + sitemap, configuration.URL, configuration.IsHttps));
                    else
                        await Task.Run(() => CrawlSitemap(VisitedPages, sitemap, configuration.URL, configuration.IsHttps));
                }
                await Task.Run(() => CrawlSitemap(VisitedPages, configuration.URL + "/sitemap.xml", configuration.URL, configuration.IsHttps));
            }
            else if (configuration.LoadFromSiteMap == 2)
            {
                await Task.Run(() => CrawlUrl(VisitedPages, configuration.URL, configuration.URL, 0, configuration.Depth, configuration.IsHttps, true, Robots));
            }
            else if (configuration.LoadFromSiteMap == 3)
            {
                foreach (string sitemap in SiteMaps)
                {
                    if (sitemap.StartsWith("/"))

                        await Task.Run(() => CrawlSitemap(VisitedPages, configuration.URL + sitemap, configuration.URL, configuration.IsHttps));
                    else
                        await Task.Run(() => CrawlSitemap(VisitedPages, sitemap, configuration.URL, configuration.IsHttps));
                }

                bool SiteMap = await HasSiteMap(configuration.URL + "/sitemap.xml", true);
                if (SiteMap)
                {
                    await Task.Run(() => CrawlSitemap(VisitedPages, configuration.URL + "/sitemap.xml",configuration.URL, configuration.IsHttps));
                }
                else
                    await Task.Run(() => CrawlUrl(VisitedPages, configuration.URL, configuration.URL, 0, configuration.Depth, configuration.IsHttps, true, Robots));
            }
            else if (configuration.LoadFromSiteMap == 4)
            {
                foreach (string sitemap in SiteMaps)
                {
                    if (sitemap.StartsWith("/"))

                        await Task.Run(() => CrawlSitemap(VisitedPages, configuration.URL + sitemap, configuration.URL, configuration.IsHttps));
                    else
                        await Task.Run(() => CrawlSitemap(VisitedPages, sitemap, configuration.URL, configuration.IsHttps));
                }
                bool SiteMap = await HasSiteMap(configuration.URL + "/sitemap.xml", true);
                if (SiteMap)
                    await Task.Run(() => CrawlSitemap(VisitedPages, configuration.URL + "/sitemap.xml", configuration.URL, configuration.IsHttps));
                await Task.Run(() => CrawlUrl(VisitedPages, configuration.URL, configuration.URL, 0, configuration.Depth, configuration.IsHttps, true, Robots));

            }

        }

        public static void LoadRobots(string URL, List<string> Robots, bool Ishttps, List<string> SiteMaps)
        {
            string crawlurl = "";
            URL = URL.Trim();
            if (URL.StartsWith("http://") || URL.StartsWith("https://"))
            {
                crawlurl = URL;
            }
            else
            {
                if (Ishttps)
                {
                    crawlurl = "https://" + URL;
                }
                else
                {
                    crawlurl = "http://" + URL;
                }
            }

            crawlurl += "/robots.txt";
            Console.WriteLine(crawlurl);
            var res = HttpGet(crawlurl, "text", HttpStatusCode.OK);
            if (!res.Item1)
            {
                return;
            }
            string[] lines = res.Item2.Split(
                new[] { "\r\n", "\r", "\n" },
                StringSplitOptions.None);

            foreach (string line in lines)
            {
                if (line.ToLower().Trim().StartsWith("sitemap"))
                {
                    string itm = line.ToLower().Trim().Substring(8);
                    if (!string.IsNullOrWhiteSpace(itm))
                        SiteMaps.Add(itm);
                }
                if (line.ToLower().StartsWith("disallow"))
                {
                    string itm = line.ToLower().Trim().Substring(9);

                    if (!string.IsNullOrWhiteSpace(itm))
                        Robots.Add(itm);
                }
            }
        }

        public static void CrawlSitemap(List<Page> VisitedPages, string URL,string BaseUrl, bool Ishttps)
        {
            try
            {
                if (VisitedPages.Any(i => i.Url.Equals(URL)))
                    return;

                List<Task> tasks = new List<Task>();
                URL = URL.Trim();

                string crawlurl = "";
                if (URL.StartsWith("http://") || URL.StartsWith("https://"))
                {
                    crawlurl = URL;
                }
                else
                {
                    if (Ishttps)
                    {
                        crawlurl = "https://" + URL;
                    }
                    else
                    {
                        crawlurl = "http://" + URL;
                    }
                }


                Console.WriteLine(crawlurl);

                var res = HttpGet(crawlurl, "xml", HttpStatusCode.OK);
                if (!res.Item1)
                {
                    return;
                }

                XmlDocument xmlDoc = new XmlDocument();
                xmlDoc.LoadXml(res.Item2);
                XmlNodeList nodess;
                if (xmlDoc.ChildNodes.Count > 1)
                    nodess = xmlDoc.ChildNodes[1].ChildNodes;
                else
                    nodess = xmlDoc.ChildNodes[0].ChildNodes;


                foreach (XmlNode itm in nodess)
                {

                    foreach (XmlNode ittm in itm.ChildNodes)
                    {
                        if (ittm.Name == "loc")
                        {
                            string href = ittm.InnerText;
                            if (itm.Name.ToLower().Equals("url"))
                            {
                                if (href.StartsWith("/"))
                                {
                                    if (ittm.InnerText.Length > 1)
                                    {
                                        href = BaseUrl + href;
                                    }
                                }
                                VisitedPages.Add(new Page() { Url = href, VisitedSiteMap = true });
                            }
                            else if (itm.Name.ToLower().Equals("sitemap"))
                            {
                                tasks.Add(Task.Run(() => CrawlSitemap(VisitedPages, href, BaseUrl, Ishttps)));
                            }

                        }
                    }
                }

                Task.WhenAll(tasks).Wait();

            }
            catch (Exception ep)
            {

            }
        }

        public static void CrawlUrl(List<Page> VisitedPages, string BaseURL, string URL, int Depth, int maxdepth, bool Ishttps, bool FirstTry, List<string> Robots)
        {
            try
            {
                if (maxdepth != 0 && maxdepth < Depth)
                    return;


                if (VisitedPages.Any(i => i.Url.Equals(URL)))
                    return;


                List<Task> tasks = new List<Task>();
                URL = URL.Trim();

                string crawlurl = string.Empty;

                if (URL.StartsWith("http://") || URL.StartsWith("https://"))
                {
                    crawlurl = URL;
                }
                else
                {
                    if (Ishttps)
                    {
                        crawlurl = "https://" + URL;
                    }
                    else
                    {
                        crawlurl = "http://" + URL;
                    }
                }

                Console.WriteLine(" => " + Task.CurrentId + " => " + crawlurl);




                var res = HttpGet(crawlurl, "text", HttpStatusCode.OK);
                if (!res.Item1)
                {
                    return;
                }

                try
                {
                   // 
                    Page CurrentPage = new Page() { Url = URL.Trim(), SubURLs = new List<string>() };
                    var followindex = CheckInRobots(res.Item2);

                    CurrentPage.HasForm = HasForm(res.Item2);

                    if (followindex.Item2)
                    {
                       // CurrentPage.HTML = res.Item2;
                    }
                    if (!VisitedPages.Any(i => i.Url.Equals(URL)))
                    {
                        VisitedPages.Add(CurrentPage);
                    }

                    HtmlDocument htmlDoc = new HtmlDocument();
                    htmlDoc.LoadHtml(res.Item2);
                    HtmlNodeCollection htmlNodes = htmlDoc.DocumentNode.SelectNodes("//body//@href");

                    var title = htmlDoc.DocumentNode.SelectSingleNode("//head/title");
                    if (title != null)
                    {
                        CurrentPage.Title = title.InnerText;
                    }

                    if (!followindex.Item1)
                        return;

                    if (htmlNodes != null && htmlNodes.Count > 0)
                    {
                        foreach (var node in htmlNodes)
                        {
                            if (node.HasAttributes)
                            {
                                string href = node.Attributes["href"].Value;

                                if (href.StartsWith("/"))
                                {
                                    if (href.Count() > 1)
                                        href = BaseURL + href;
                                    else
                                        continue;
                                }
                                if (!href.StartsWith("#"))
                                {
                                    CurrentPage.SubURLs.Add(href);
                                    if (!CheckInRobots(BaseURL, href, Robots))
                                    {
                                        if (!VisitedPages.Any(i => i.Url.Equals(href.Trim())) && href.Contains(BaseURL))
                                        {
                                            if (!(href.ToLower().EndsWith(".mp4") || href.ToLower().EndsWith(".pdf")
                                                || href.ToLower().EndsWith(".apk") || href.ToLower().EndsWith(".jpg")
                                                || href.ToLower().EndsWith(".png") || href.ToLower().EndsWith(".mp3")
                                                || href.ToLower().EndsWith(".pdfy") || href.ToLower().EndsWith(".jpeg")
                                                || href.ToLower().EndsWith(".rar") || href.ToLower().EndsWith(".zip")))
                                            {
                                                tasks.Add(Task.Run(() => CrawlUrl(VisitedPages, BaseURL, href, Depth + 1, maxdepth, false, true, Robots)));
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                    Task.WhenAll(tasks).Wait();

                }
                catch (Exception ep)
                {

                }

            }
            catch (AggregateException ep)
            {
                CrawlUrl(VisitedPages, BaseURL, URL, Depth + 1, maxdepth, !Ishttps, false, Robots);

                return;
            }
            catch (Exception ep)
            {
                return;
            }
        }
        private static bool CheckInRobots(string BaseURL, string URL, List<string> Robots)
        {
            foreach (string rob in Robots)
            {

                if (URL.Contains(BaseURL + (rob.EndsWith("*") ? rob.Substring(0, rob.Length - 2) : rob)))
                    return true;
            }
            return false;
        }
        private static Tuple<bool, bool> CheckInRobots(string HTML)
        {
            bool index = true, follow = true;
            HtmlDocument htmlDoc = new HtmlDocument();
            htmlDoc.LoadHtml(HTML);
            HtmlNodeCollection htmlNodes = htmlDoc.DocumentNode.SelectNodes("//head//@content");
            if (htmlNodes != null && htmlNodes.Count > 0)
            {
                foreach (var node in htmlNodes)
                {
                    if (node.HasAttributes)
                    {
                        try
                        {
                            if (node.Attributes["name"].Value.ToLower().Equals("robots"))
                            {
                                if (node.Attributes["content"].Value.ToLower().Contains("noindex"))
                                {
                                    index = false;
                                }

                                if (node.Attributes["content"].Value.ToLower().Contains("nofollow"))
                                {
                                    index = false;
                                }
                            }
                        }
                        catch { }

                    }
                }
            }

            return new Tuple<bool, bool>(follow, index);
        }

        private static Tuple<bool, string> HttpGet(string URL, string ContentType, HttpStatusCode httpStatusCode,bool Ishttps=false)
        {
            string crawlurl = string.Empty;

            if (URL.StartsWith("http://") || URL.StartsWith("https://"))
            {
                crawlurl = URL;
            }
            else
            {
                if (Ishttps)
                {
                    crawlurl = "https://" + URL;
                }
                else
                {
                    crawlurl = "http://" + URL;
                }
            }

            string Resault = string.Empty;
            bool Success = false;
            try
            {
                HttpWebRequest myHttpWebRequest = (HttpWebRequest)WebRequest.Create(crawlurl);
                HttpWebResponse myHttpWebResponse = (HttpWebResponse)myHttpWebRequest.GetResponse();
                if (myHttpWebResponse.StatusCode == httpStatusCode)
                {
                    if (myHttpWebResponse.ContentType.Contains(ContentType))
                    {
                        Stream receiveStream = myHttpWebResponse.GetResponseStream();
                        StreamReader readStream = new StreamReader(receiveStream,
                           myHttpWebResponse.CharacterSet.ToUpper().Trim() == "" ? Encoding.UTF8 : Encoding.GetEncoding(myHttpWebResponse.CharacterSet.ToUpper().Trim()));
                        Resault = readStream.ReadToEnd();
                        Success = true;
                    }
                }
                myHttpWebResponse.Close();
            }
            catch
            {

            }
            return new Tuple<bool, string>(Success, Resault);
        }
        private static bool HasForm(string HTML)
        {
            HtmlDocument htmlDoc = new HtmlDocument();
            htmlDoc.LoadHtml(HTML);
            HtmlNodeCollection htmlNodes = htmlDoc.DocumentNode.SelectNodes("//body//form");


            if (htmlNodes != null && htmlNodes.Count > 0)
                return true;
            else
                return false;
        }

        public static async Task<bool> HasSiteMap(string URL, bool Ishttps)
        {
            var Resault = false;
            URL = URL.Trim();

            try
            {
                string crawlurl = "";
                if (URL.StartsWith("http://") || URL.StartsWith("https://"))
                {
                    crawlurl = URL;
                }
                else
                {
                    if (Ishttps)
                    {
                        crawlurl = "https://" + URL;
                    }
                    else
                    {
                        crawlurl = "http://" + URL;
                    }
                }
                HttpWebRequest myHttpWebRequest = (HttpWebRequest)WebRequest.Create(crawlurl);
                HttpWebResponse myHttpWebResponse = (HttpWebResponse)myHttpWebRequest.GetResponse();
                if (myHttpWebResponse.ContentType.Contains("xml") && myHttpWebResponse.StatusCode == HttpStatusCode.OK)
                {
                    Resault = true;
                }
                myHttpWebResponse.Close();
            }
            catch
            {

            }

            return await Task.FromResult(Resault);

        }

        public static async Task<string> LoadTitle(string URL)
        {
            URL = URL.Trim();

            if (!URL.StartsWith("http://") && !URL.StartsWith("https://"))
            {
                URL = "http://" + URL;
            }

            string Title = "";
            try
            {
                var res = HttpGet(URL, "text/html", HttpStatusCode.OK);

                if (!res.Item1)
                {
                    return Title;
                }

                //Console.WriteLine(HTML);

                try
                {
                    HtmlDocument htmlDoc = new HtmlDocument();
                    htmlDoc.LoadHtml(res.Item2);
                    var title = htmlDoc.DocumentNode.SelectSingleNode("//head/title");
                    if (title != null)
                    {

                        Title = title.InnerText;

                    }
                }
                catch { }
            }
            catch (Exception ep)
            {

            }
            return await Task.FromResult(Title);
        }

        public static async Task<List<string>> PostForm(string URL, bool Ishttps = false)
        {
            string crawlurl = string.Empty;

            if (URL.StartsWith("http://") || URL.StartsWith("https://"))
            {
                crawlurl = URL;
            }
            else
            {
                if (Ishttps)
                {
                    crawlurl = "https://" + URL;
                }
                else
                {
                    crawlurl = "http://" + URL;
                }
            }
            var Values = LoadFormFromHtml(crawlurl);

            List<string> Urls = new List<string>();
            if (!string.IsNullOrWhiteSpace(Values.Item2))
            {
                crawlurl = crawlurl.Trim().EndsWith("/") ? crawlurl.Substring(0, crawlurl.Length - 2) : crawlurl;

                string action = Values.Item2.Trim().StartsWith("/") ? Values.Item2.Substring(1, Values.Item2.Length - 1) : Values.Item2;

                crawlurl = crawlurl + "/" + action;

                var Datas = new Dictionary<string, string>();
                foreach (var itm in Values.Item1)
                {
                    switch (itm.Value)
                    {
                        case "text":
                            Datas.Add(itm.Key, "");
                            break;
                        case "password":
                            Datas.Add(itm.Key, "");
                            break;
                        case "textarea":
                            Datas.Add(itm.Key, "");
                            break;
                        case "email":
                            Datas.Add(itm.Key, "kohestanimahdi@gmail.com");
                            break;
                        case "radiobutton":
                            Datas.Add(itm.Key, "1");
                            break;
                        case "checkbox":
                            Datas.Add(itm.Key, true.ToString());
                            break;

                    }

                }
                //var values = new Dictionary<string, string>
                //{
                //{ "username", "" },
                //{ "password", "" }
                //};

                HttpClient client = new HttpClient();
                var content = new FormUrlEncodedContent(Datas);

                var response = await client.PostAsync(crawlurl, content);

                var responseString = await response.Content.ReadAsStringAsync();

                HtmlDocument htmlDoc = new HtmlDocument();
                htmlDoc.LoadHtml(responseString);
                HtmlNodeCollection htmlNodes = htmlDoc.DocumentNode.SelectNodes("//body//@href");



                if (htmlNodes != null && htmlNodes.Count > 0)
                {
                    foreach (var node in htmlNodes)
                    {
                        if (node.HasAttributes)
                        {
                            string href = node.Attributes["href"].Value;
                            if (href.StartsWith("/"))
                            {
                                if (href.Count() > 1)
                                    href = crawlurl + href;
                                else
                                    continue;
                            }
                            if (!href.StartsWith("#"))
                            {
                                Urls.Add(href);
                            }
                        }
                    }

                }
            }

            return Urls;
        }

        public static Tuple<Dictionary<string, string>, string> LoadFormFromHtml(string URL)
        {
            
            var res = HttpGet(URL, "text", HttpStatusCode.OK);
            Dictionary<string, string> FormInputs = new Dictionary<string, string>();
            bool hasaction = true;
            string PostAction = "";
            if (res.Item1)
            {
                HtmlDocument htmlDoc = new HtmlDocument();
                htmlDoc.LoadHtml(res.Item2);
                HtmlNodeCollection htmlNodes = htmlDoc.DocumentNode.SelectNodes("//body//form");



                if (htmlNodes != null && htmlNodes.Count > 0)
                {
                    //foreach (var node in htmlNodes)
                    {
                        var node = htmlNodes[0];


                        try
                        {
                            if (node.HasAttributes)
                            {
                                PostAction = node.Attributes["action"].Value;

                            }
                        }
                        catch
                        {
                            hasaction = false;
                        }
                        if (hasaction)
                        {
                            var inputs = node.SelectNodes("//input");
                            if (inputs != null && inputs.Count() > 0)
                            {
                                foreach (var input in inputs)
                                {
                                    if (input.HasAttributes)
                                    {
                                        string type = input.Attributes["type"].Value.Trim().ToLower();
                                        if (!type.Equals("hidden"))
                                        {
                                            string name = input.Attributes["name"].Value.Trim().ToLower();
                                            FormInputs.Add(name, type);
                                        }
                                    }
                                }
                            }
                        }

                    }

                }
            }
            return new Tuple<Dictionary<string, string>, string>(FormInputs, PostAction);
        }
    }
}
