using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

namespace ReloadLibrary
{
    public class LoadPages
    {
        private ManualResetEvent resetEvent = new ManualResetEvent(false);

        public LoadPages()
        {
            DownloadPageAsync();
            Console.WriteLine("Downloading page...");
            resetEvent.WaitOne(); // Blocks until "set"
        }

        private async void DownloadPageAsync()
        {
#if DEBUG
            String str = "webSites.xml";

#else
            String str = @"C:\Users\root\Documents\Tools\Reload\webSites.xml";
#endif
            XmlReader rdr = XmlReader.Create(str, new XmlReaderSettings() { IgnoreWhitespace = true, IgnoreComments = true });

            XElement websites = XElement.Load(rdr);

            IEnumerable<XElement> webSites = websites.Descendants("website");
            HttpClient client = new HttpClient();
            client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (compatible; MSIE 10.0; Windows NT 6.2;WOW64; Trident / 6.0)");

            client.Timeout = new TimeSpan(0, 0, 30);
            HttpResponseMessage response = null;

            if (websites.Descendants("login").Count() > 0)
            {
                XElement loginElement = websites.Descendants("login").First();
                string url = loginElement.Attribute("url").Value;

                HttpRequestMessage message = new HttpRequestMessage(HttpMethod.Get, url);

                response = await client.SendAsync(message);
                HttpContent content = response.Content;
                string result = await content.ReadAsStringAsync();

                HtmlDocument doc = new HtmlDocument();
                doc.LoadHtml(result);
                String __RequestVerificationToken = doc.DocumentNode.Descendants("input").FirstOrDefault(x => x.Attributes["name"].Value == "__RequestVerificationToken").Attributes["value"].Value;

                string user = loginElement.Attribute("user").Value;
                string password = loginElement.Attribute("password").Value;
                string userField = loginElement.Attribute("userField").Value;
                string passwordField = loginElement.Attribute("passwordField").Value;

                // This is the postdata
                var postData = new List<KeyValuePair<string, string>>();
                postData.Add(new KeyValuePair<string, string>(userField, user));
                postData.Add(new KeyValuePair<string, string>(passwordField, password));
                postData.Add(new KeyValuePair<string, string>("__RequestVerificationToken", __RequestVerificationToken));
                content = new FormUrlEncodedContent(postData);
                HttpResponseMessage r = null;
                r = await client.PostAsync(url, content);
                HttpContent contentLogin = r.Content;

                result = await contentLogin.ReadAsStringAsync();
                if (result != null &&
   result.Length >= 50)
                {
                    Console.WriteLine(result.Substring(0, 10) + "...");
                }
            }

            HttpRequestMessage request = new HttpRequestMessage();
            foreach (XElement element in webSites)
            {
                foreach (XElement page in element.Descendants("pages").First().Descendants())
                {
                    HttpRequestMessage message = new HttpRequestMessage(HttpMethod.Get, page.Attribute("url").Value);

                    response = await client.SendAsync(message);
                    HttpContent content = response.Content;
                    string result = await content.ReadAsStringAsync();
                    if (result != null &&
       result.Length >= 50)
                    {
                        Console.WriteLine(result.Substring(0, 10) + "...");
                    }
                }


            }


            resetEvent.Set(); // Allow the program to exit
        }
    }
}
