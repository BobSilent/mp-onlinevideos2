using System;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime.Caching;
using System.Text;

namespace OnlineVideos
{
    public class WebCache
    {
        #region Singleton
        private WebCache() { }
        private static readonly Lazy<WebCache> _instance = new Lazy<WebCache>(() => new WebCache());
        public static WebCache Instance => _instance.Value;
        #endregion

        // Named cache instance — keeps OnlineVideos entries isolated from MemoryCache.Default.
        private readonly MemoryCache _cache = new MemoryCache("OnlineVideos");

        // Shared HttpClient for the common (no-cookie, no-proxy) path.
        // A single instance per process reuses TCP connections across all site-util calls.
        private static readonly HttpClient _httpClient = new HttpClient(
            new HttpClientHandler
            {
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate,
                AllowAutoRedirect = true,
                UseCookies = false, // cookies passed per-request via Cookie header when needed via fallback path
            })
        {
            Timeout = TimeSpan.FromSeconds(30)
        };

        // Separate client used by GetRedirectedUrl — must not follow redirects.
        private static readonly HttpClient _noRedirectClient = new HttpClient(
            new HttpClientHandler
            {
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate,
                AllowAutoRedirect = false,
                UseCookies = false
            })
        {
            Timeout = TimeSpan.FromSeconds(15)
        };

        public string this[string url]
        {
            get
            {
                if (!string.IsNullOrEmpty(url) && OnlineVideoSettings.Instance.CacheTimeout > 0) // only use cache if a timeout > 0 was set
                {
                    return _cache.Get(url) as string;
                }

                return null;
            }
            set
            {
                if (OnlineVideoSettings.Instance.CacheTimeout > 0) // only use cache if a timeout > 0 was set
                {
                    var policy = new CacheItemPolicy
                    {
                        AbsoluteExpiration = DateTimeOffset.Now.AddMinutes(OnlineVideoSettings.Instance.CacheTimeout)
                    };
                    _cache.Set(url, value, policy);
                }
            }
        }

        public T GetWebData<T>(string url, string postData = null, CookieContainer cookies = null, string referer = null, IWebProxy proxy = null, bool forceUTF8 = false, bool allowUnsafeHeader = false, string userAgent = null, Encoding encoding = null, NameValueCollection headers = null, bool cache = true)
        {
            string webData = GetWebData(url, postData, cookies, referer, proxy, forceUTF8, allowUnsafeHeader, userAgent, encoding, headers, cache);
            if (typeof(T) == typeof(string))
            {
                return (T)(object)webData;
            }
            if (typeof(T) == typeof(Newtonsoft.Json.Linq.JToken))
            {
                return (T)(object)Newtonsoft.Json.Linq.JToken.Parse(webData);
            }
            if (typeof(T) == typeof(Newtonsoft.Json.Linq.JObject))
            {
                return (T)(object)Newtonsoft.Json.Linq.JObject.Parse(webData);
            }
            if (typeof(T) == typeof(RssToolkit.Rss.RssDocument))
            {
                return (T)(object)RssToolkit.Rss.RssDocument.Load(webData);
            }
            if (typeof(T) == typeof(System.Xml.XmlDocument))
            {
                var xmlDoc = new System.Xml.XmlDocument();
                xmlDoc.LoadXml(webData);
                return (T)(object)xmlDoc;
            }
            if (typeof(T) == typeof(System.Xml.Linq.XDocument))
            {
                return (T)(object)System.Xml.Linq.XDocument.Parse(webData);
            }
            if (typeof(T) == typeof(HtmlAgilityPack.HtmlDocument))
            {
                HtmlAgilityPack.HtmlDocument htmlDoc = new HtmlAgilityPack.HtmlDocument();
                htmlDoc.LoadHtml(webData);
                return (T)(object)htmlDoc;
            }

            return default(T);
        }

        public string GetWebData(string url, string postData = null, CookieContainer cookies = null, string referer = null, IWebProxy proxy = null, bool forceUTF8 = false, bool allowUnsafeHeader = false, string userAgent = null, Encoding encoding = null, NameValueCollection headers = null, bool cache = true)
        {
            return GetWebData(new Uri(url), postData, cookies, referer, proxy, forceUTF8, allowUnsafeHeader, userAgent, encoding, headers, cache);
        }

        private void SetRequestProperties(HttpWebRequest request, NameValueCollection headers, bool isPost)
        {
            //Set some defaults. If new values are passed in the headers property they will be overwritten
            request.UserAgent = OnlineVideoSettings.Instance.UserAgent;
            request.Accept = "*/*";
            if (isPost)
            {
                request.ContentType = "application/x-www-form-urlencoded";
            }

            foreach (var headerName in headers.AllKeys)
            {
                switch (headerName.ToLowerInvariant())
                {
                    case "accept":
                        request.Accept = headers[headerName];
                        break;
                    case "user-agent":
                        request.UserAgent = headers[headerName];
                        break;
                    case "referer":
                        request.Referer = headers[headerName];
                        break;
                    case "content-type":
                        request.ContentType = headers[headerName];
                        break;
                    default:
                        request.Headers.Set(headerName, headers[headerName]);
                        break;
                }
            }
        }

        public string GetWebData(Uri uri, string postData = null, CookieContainer cookies = null, string referer = null, IWebProxy proxy = null, bool forceUTF8 = false, bool allowUnsafeHeader = false, string userAgent = null, Encoding encoding = null, NameValueCollection headers = null, bool cache = true)
        {
            // do not use the cache when doing a POST
            if (postData != null)
            {
                cache = false;
            }

            if (headers == null)
            {
                headers = new NameValueCollection();
            }
            if (referer != null)
            {
                headers.Add("referer", referer);
            }

            if (userAgent != null)
            {
                headers.Add("user-agent", userAgent);
            }

            // build a CRC of the url and all headers + proxy + cookies for caching
            string requestCRC = Helpers.EncryptionUtils.CalculateCRC32(
                $"{uri}"
                + (headers != null ? string.Join("&", Array.ConvertAll(headers.AllKeys, k => $"{k}={headers[k]}")) : "")
                + (proxy != null ? proxy.GetProxy(uri).AbsoluteUri : "")
                + (cookies != null ? cookies.GetCookieHeader(uri) : ""));

            // try cache first
            string cachedData = cache ? Instance[requestCRC] : null;
            var canUseHttpClient = cookies == null && proxy == null && !allowUnsafeHeader;
            Log.Debug($"GetWebData{(canUseHttpClient ? "New" : "Old")}-{(postData != null ? "POST" : "GET")}{(cachedData != null ? " (cached)" : "")}: '{uri.AbsoluteUri}'");
            if (cachedData != null)
            {
                return cachedData;
            }

            // Use the shared HttpClient for the common path (no cookies, no proxy, no unsafe-header hacks).
            // Fall back to HttpWebRequest when any of those are required.
            if (canUseHttpClient)
            {
                return GetWebDataViaHttpClient(uri, postData, headers, encoding, forceUTF8, cache, requestCRC);
            }

            // --- HttpWebRequest fallback (cookies / proxy / allowUnsafeHeader) ---
            HttpWebResponse response = null;
            try
            {
                if (allowUnsafeHeader)
                {
                    Helpers.DotNetFrameworkHelper.SetAllowUnsafeHeaderParsing(true);
                }

                HttpWebRequest request = WebRequest.Create(uri) as HttpWebRequest;
                if (request == null)
                {
                    return "";
                }

                request.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
                if (cookies != null)
                {
                    request.CookieContainer = cookies;
                }

                if (proxy != null)
                {
                    request.Proxy = proxy;
                }

                SetRequestProperties(request, headers, postData != null);

                if (postData != null)
                {
                    byte[] data = encoding != null ? encoding.GetBytes(postData) : Encoding.UTF8.GetBytes(postData);
                    request.Method = "POST";
                    request.ContentLength = data.Length;
                    request.ProtocolVersion = HttpVersion.Version10;
                    Stream requestStream = request.GetRequestStream();
                    requestStream.Write(data, 0, data.Length);
                    requestStream.Close();
                }

                try
                {
                    response = (HttpWebResponse)request.GetResponse();
                }
                catch (WebException webEx)
                {
                    Log.Debug(webEx.Message);
                    response = (HttpWebResponse)webEx.Response;
                }
                Stream responseStream = response.GetResponseStream();

                Encoding responseEncoding = Encoding.UTF8;
                if (!forceUTF8 && encoding == null && response.CharacterSet != null && !string.IsNullOrEmpty(response.CharacterSet.Trim()))
                {
                    responseEncoding = Encoding.GetEncoding(response.CharacterSet.Trim(new char[] { ' ', '"' }));
                }

                if (encoding != null)
                {
                    responseEncoding = encoding;
                }

                if (forceUTF8)
                {
                    responseEncoding = Encoding.UTF8;
                }

                using (StreamReader reader = new StreamReader(responseStream, responseEncoding, true))
                {
                    string str = reader.ReadToEnd().Trim();
                    if (cache && response.StatusCode == HttpStatusCode.OK && str.Length > 500)
                    {
                        Instance[requestCRC] = str;
                    }

                    return str;
                }
            }
            finally
            {
                (response as IDisposable)?.Dispose();

                if (allowUnsafeHeader)
                {
                    Helpers.DotNetFrameworkHelper.SetAllowUnsafeHeaderParsing(false);
                }
            }
        }

        private string GetWebDataViaHttpClient(Uri uri, string postData, NameValueCollection headers, Encoding encoding, bool forceUTF8, bool cache, string requestCRC)
        {
            using (var request = new HttpRequestMessage(postData != null ? HttpMethod.Post : HttpMethod.Get, uri))
            {
                // Default headers
                request.Headers.TryAddWithoutValidation("User-Agent", OnlineVideoSettings.Instance.UserAgent);
                request.Headers.TryAddWithoutValidation("Accept", "*/*");

                // Caller-supplied headers (referer, user-agent override, custom headers)
                foreach (var key in headers.AllKeys)
                {
                    switch (key.ToLowerInvariant())
                    {
                        case "user-agent":
                            request.Headers.Remove("User-Agent");
                            request.Headers.TryAddWithoutValidation("User-Agent", headers[key]);
                            break;
                        case "accept":
                            request.Headers.Remove("Accept");
                            request.Headers.TryAddWithoutValidation("Accept", headers[key]);
                            break;
                        default:
                            request.Headers.TryAddWithoutValidation(key, headers[key]);
                            break;
                    }
                }

                if (postData != null)
                {
                    byte[] data = encoding != null ? encoding.GetBytes(postData) : Encoding.UTF8.GetBytes(postData);
                    request.Content = new ByteArrayContent(data);
                    request.Content.Headers.TryAddWithoutValidation("Content-Type", "application/x-www-form-urlencoded");
                }

                HttpResponseMessage response;
                try
                {
                    response = _httpClient.SendAsync(request).GetAwaiter().GetResult();
                }
                catch (Exception ex)
                {
                    Log.Debug("HttpClient error: {0}", ex.Message);
                    return "";
                }

                using (response)
                {
                    // Read raw bytes so we can apply encoding rules identical to the HttpWebRequest path.
                    byte[] bytes = response.Content.ReadAsByteArrayAsync().GetAwaiter().GetResult();

                    Encoding responseEncoding = Encoding.UTF8;
                    if (!forceUTF8 && encoding == null)
                    {
                        // Try charset from Content-Type header
                        var charset = response.Content.Headers.ContentType?.CharSet?.Trim(new char[] { ' ', '"' });
                        if (!string.IsNullOrEmpty(charset))
                        {
                            try { responseEncoding = Encoding.GetEncoding(charset); }
                            catch { /* unknown charset — stay UTF-8 */ }
                        }
                    }
                    if (encoding != null)
                    {
                        responseEncoding = encoding;
                    }

                    if (forceUTF8)
                    {
                        responseEncoding = Encoding.UTF8;
                    }

                    string str = responseEncoding.GetString(bytes).Trim();
                    if (cache && response.StatusCode == HttpStatusCode.OK && str.Length > 500)
                    {
                        Instance[requestCRC] = str;
                    }

                    return str;
                }
            }
        }

        public string GetRedirectedUrl(string url, CookieContainer cc = null, NameValueCollection headers = null, bool allowAutoRedirect = true)
        {
            if (headers == null)
            {
                headers = new NameValueCollection();
            }

            // Common path: no cookies — use the shared no-redirect HttpClient.
            if (cc == null)
            {
                try
                {
                    using (var request = new HttpRequestMessage(HttpMethod.Head, url))
                    {
                        request.Headers.TryAddWithoutValidation("User-Agent", OnlineVideoSettings.Instance.UserAgent);
                        foreach (var key in headers.AllKeys)
                        {
                            request.Headers.TryAddWithoutValidation(key, headers[key]);
                        }

                        // _noRedirectClient has AllowAutoRedirect=false so we always get the immediate response.
                        using (var response = _noRedirectClient.SendAsync(request).GetAwaiter().GetResult())
                        {
                            if (!allowAutoRedirect)
                            {
                                return response.Headers.Location?.ToString() ?? url;
                            }

                            // If the server returned a redirect, Location is the new URL.
                            var location = response.Headers.Location;
                            if (location != null)
                            {
                                return location.IsAbsoluteUri ? location.AbsoluteUri : new Uri(new Uri(url), location).AbsoluteUri;
                            }

                            return url; // no redirect — original URL is the final URL
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log.Warn(ex.ToString());
                    return url;
                }
            }

            // Fallback: CookieContainer present — use HttpWebRequest to preserve session.
            HttpWebRequest hwRequest = WebRequest.Create(url) as HttpWebRequest;

            string GetFinalUrl(WebResponse response)
            {
                if (response == null)
                {
                    return url;
                }

                if (!allowAutoRedirect)
                {
                    return response.Headers["location"];
                }

                if (hwRequest.RequestUri.Equals(((HttpWebResponse)response).ResponseUri))
                {
                    return url;
                }

                return ((HttpWebResponse)response).ResponseUri.OriginalString;
            }

            try
            {
                if (hwRequest == null)
                {
                    return url;
                }

                SetRequestProperties(hwRequest, headers, false);
                hwRequest.AllowAutoRedirect = allowAutoRedirect;
                hwRequest.CookieContainer = cc;
                hwRequest.Timeout = 15000;
                var result = hwRequest.BeginGetResponse((ar) => hwRequest.Abort(), null);
                result.AsyncWaitHandle.WaitOne();
                using (var hwResponse = hwRequest.EndGetResponse(result))
                {
                    return GetFinalUrl(hwResponse);
                }
            }
            catch (WebException ex)
            {
                return GetFinalUrl(ex.Response);
            }
            catch (Exception ex)
            {
                Log.Warn(ex.ToString());
            }
            return url;
        }
    }
}
