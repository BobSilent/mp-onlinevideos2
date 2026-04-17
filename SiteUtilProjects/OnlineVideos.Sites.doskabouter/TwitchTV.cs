using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Web;
using Newtonsoft.Json.Linq;
using System.Text.RegularExpressions;
using OnlineVideos.Helpers;

namespace OnlineVideos.Sites
{
    /// <summary>
    /// Twitch API docs can be found here: https://dev.twitch.tv/docs/v5/
    /// </summary>
    public class TwitchTVUtil : SiteUtilBase, IWebViewSiteUtilBase
    {
        private const string ClientID     = "3jqzelqamssns2ybboe0ps2o6jo4tw";
        private const string ClientSecret = "mysecret";
        private const string BaseApiUrl   = "https://api.twitch.tv/helix";
        private const string GamesUrl     = "/games/top?first=40";
        private const string StreamsUrl   = "/streams?game_id={0}";
        private const string SearchUrl    = "/search/channels?query={0}&first=25&live_only=true";

        private string nextPageUrl;

        private NameValueCollection customHeader;

        public override int DiscoverDynamicCategories()
        {
            var token = GetToken();
            customHeader = new NameValueCollection();
            customHeader.Add("Client-Id", ClientID);
            customHeader.Add("Authorization", $"Bearer {token}");
            Settings.Categories.Clear();
            return ParseCategories(BaseApiUrl + GamesUrl);
        }

        private string GetToken()
        {
            string postData = $"client_id={ClientID}&client_secret={ClientSecret}&grant_type=client_credentials";
            var tokenDataJson = GetWebData<JToken>(@"https://id.twitch.tv/oauth2/token", postData: postData);
            return tokenDataJson["access_token"].ToString();
        }

        public override int DiscoverNextPageCategories(NextPageCategory category)
        {
            Settings.Categories.Remove(category);
            return ParseCategories(category.Url);
        }

        private int ParseCategories(string url)
        {
            var games = GetWebData<JObject>(url, headers: customHeader);
            foreach (var game in games["data"])
            {
                Settings.Categories.Add(CategoryFromJsonGameObject(game));
            }
            Settings.DynamicCategoriesDiscovered = Settings.Categories.Count > 0;

            var cursor = games["pagination"]?.Value<string>("cursor");
            if (!string.IsNullOrEmpty(cursor))
            {
                Settings.Categories.Add(new NextPageCategory() { Url = GetNextPageUrl(url, cursor) });
            }

            return Settings.Categories.Count;
        }

        public override List<VideoInfo> GetVideos(Category category)
        {
            return VideosFromApiUrl(((RssLink)category).Url);
        }

        public override List<VideoInfo> GetNextPageVideos()
        {
            return VideosFromApiUrl(nextPageUrl);
        }

        public override bool CanSearch
        {
            get { return true; }
        }

        public override List<SearchResultItem> Search(string query, string category = null)
        {
            return VideosFromApiUrl(BaseApiUrl + string.Format(SearchUrl, HttpUtility.UrlEncode(query))).ConvertAll<SearchResultItem>(i => i as SearchResultItem);
        }

        private List<VideoInfo> VideosFromApiUrl(string url)
        {
            List<VideoInfo> result = new List<VideoInfo>();

            var streams = GetWebData<JObject>(url, headers: customHeader);
            foreach (var stream in streams["data"])
            {
                result.Add(VideoFromJsonStreamObject(stream));
            }

            var cursor = streams["pagination"]?.Value<string>("cursor");
            nextPageUrl = GetNextPageUrl(url, cursor);

            HasNextPage = !string.IsNullOrEmpty(nextPageUrl);
            return result;
        }

        private Category CategoryFromJsonGameObject(JToken game)
        {
            return new RssLink()
            {
                Name = game.Value<string>("name"),
                Url = BaseApiUrl + string.Format(StreamsUrl, game.Value<string>("id")),
                Other = game
            };
        }

        private VideoInfo VideoFromJsonStreamObject(JToken stream)
        {
            return new VideoInfo()
            {
                Title       = stream.Value<string>("title"),
                Thumb       = stream.Value<string>("thumbnail_url").Replace("{width}", "200").Replace("{height}", "200"),
                Description = $"{stream.Value<string>("viewer_count")} Viewers for {stream.Value<string>("user_name")}",
                Airdate     = stream.Value<DateTime>("started_at").ToString("g", OnlineVideoSettings.Instance.Locale),
                VideoUrl    = $"https://player.twitch.tv/?channel={stream.Value<string>("user_login") ?? stream.Value<string>("broadcaster_login")}&parent=streamernews.example.com&muted=false"
            };
        }
        private string GetNextPageUrl(string url, string cursor)
        {
            if (string.IsNullOrEmpty(cursor)) return null;
            int p = url.IndexOf("&after", StringComparison.Ordinal);
            if (p >= 0)
            {
                url = url.Substring(0, p);
            }
            return url + "&after=" + cursor;
        }

        void IWebViewSiteUtilBase.StartPlayback()
        {
            System.Threading.Thread.Sleep(1000);
            webViewHelper.Execute(@"document.querySelectorAll('[data-a-target=""player-mute-unmute-button""]')[0].click()");
            webViewHelper.Execute(@"document.querySelectorAll('[data-a-target=""content-classification-gate-overlay-start-watching-button""]')[0].click()");
        }
    }

}
