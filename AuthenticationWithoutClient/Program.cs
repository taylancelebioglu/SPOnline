using Microsoft.SharePoint.Client;
using Newtonsoft.Json;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security;
using System.Threading.Tasks;

namespace AuthenticationWithoutClient
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                const string USERNAME = "taylancelebioglu@ogoodigital.com";
                const string PWD = "ZxCv123(";
                const string WEB = "https://ogoodigital.sharepoint.com/sites/heroo";


                var t = ExecuteCall(WEB, USERNAME, PWD);
                t.Wait();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
            Console.ReadLine();
        }

        /// <summary>
        /// Return Form Digest information
        /// </summary>
        /// <param name="handler"></param>
        /// <param name="webUrl"></param>
        /// <returns></returns>
        private static async Task<Models.FormDigestInfo.Rootobject> GetFormDigest(HttpClientHandler handler, string webUrl)
        {
            //Creating REST url to get Form Digest
            const string RESTURL = "{0}/_api/contextinfo";
            string restUrl = string.Format(RESTURL, webUrl);

            //Adding headers
            var client = new HttpClient(handler);
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Add("Accept", "application/json;odata=nometadata");

            //Perform call
            HttpResponseMessage response = await client.PostAsync(restUrl, null).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            //Reading string data
            string jsonData = await response.Content.ReadAsStringAsync();

            //Creating FormDigest object
            Models.FormDigestInfo.Rootobject res = JsonConvert.DeserializeObject<Models.FormDigestInfo.Rootobject>(jsonData);
            return res;
        }

        /// <summary>
        /// Upload a document
        /// </summary>
        /// <param name="webUrl"></param>
        /// <param name="loginName"></param>
        /// <param name="pwd"></param>
        /// <param name="document"></param>
        /// <param name="folderServerRelativeUrl"></param>
        /// <param name="fileName"></param>
        /// <returns></returns>
        private static async Task ExecuteCall(string webUrl, string loginName, string pwd)
        {
            try
            {
                //Creating credentials
                var passWord = new SecureString();
                foreach (var c in pwd) passWord.AppendChar(c);
                SharePointOnlineCredentials credential = new SharePointOnlineCredentials(loginName, passWord);

                //Creating REST url
                const string RESTURL = "{0}/_api/web/lists/GetByTitle('Stories')";
                string rESTUrl = string.Format(RESTURL, webUrl);

                //Creating handler
                using (var handler = new HttpClientHandler() { Credentials = credential })
                {
                    //Getting authentication cookies
                    Uri uri = new Uri(webUrl);
                    string cookie = credential.GetAuthenticationCookie(uri);
                    handler.CookieContainer.SetCookies(uri, cookie);

                    //Getting form digest
                    var tFormDigest = GetFormDigest(handler, webUrl);
                    tFormDigest.Wait();

                    //Creating HTTP Client
                    using (var client = new HttpClient(handler))
                    {
                        client.DefaultRequestHeaders.Accept.Clear();
                        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                        client.DefaultRequestHeaders.Add("Accept", "application/json;odata=nometadata");
                        client.DefaultRequestHeaders.Add("binaryStringRequestBody", "true");
                        client.DefaultRequestHeaders.Add("X-RequestDigest", tFormDigest.Result.FormDigestValue);
                        client.MaxResponseContentBufferSize = 2147483647;


                        HttpResponseMessage response = await client.GetAsync(rESTUrl).ConfigureAwait(false);

                        string msg = await response.Content.ReadAsStringAsync();

                        //Ensure 200 (Ok)
                        response.EnsureSuccessStatusCode();
                    }
                }
            }
            catch (Exception ex)
            {

            }
        }
    }

    public class Models
    {
        public class FormDigestInfo
        {
            public class Rootobject
            {
                public int FormDigestTimeoutSeconds { get; set; }
                public string FormDigestValue { get; set; }
                public string LibraryVersion { get; set; }
                public string SiteFullUrl { get; set; }
                public string[] SupportedSchemaVersions { get; set; }
                public string WebFullUrl { get; set; }
            }
        }
    }
}
