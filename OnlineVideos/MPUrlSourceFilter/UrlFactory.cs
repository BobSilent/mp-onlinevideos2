using System;
using System.Collections;

namespace OnlineVideos.MPUrlSourceFilter
{
    /// <summary>
    /// Represents class for creating URL objects from specified string.
    /// </summary>
    internal static class UrlFactory
    {
        #region Private fields
        #endregion

        #region Constructors

        static UrlFactory()
        {
            SupportedProtocols = new Hashtable();

            SupportedProtocols.Add("HTTP", "HTTP");
            SupportedProtocols.Add("HTTPS", "HTTP");

            SupportedProtocols.Add("RTMP", "RTMP");
            SupportedProtocols.Add("RTMPT", "RTMP");
            SupportedProtocols.Add("RTMPE", "RTMP");
            SupportedProtocols.Add("RTMPTE", "RTMP");
            SupportedProtocols.Add("RTMPS", "RTMP");
            SupportedProtocols.Add("RTMPTS", "RTMP");

            SupportedProtocols.Add("RTSP", "RTSP");

            SupportedProtocols.Add("UDP", "UDP");
            SupportedProtocols.Add("RTP", "UDP");
        }

        #endregion

        #region Properties
        #endregion

        #region Methods

        public static SimpleUrl CreateUrl(String url)
        {
            // no special form of URL
            // in this case check URI scheme

            Uri uri = new Uri(url);
            String scheme = (String)SupportedProtocols[uri.Scheme.ToUpperInvariant()];

            switch (scheme)
            {
                case "HTTP":
                    return new HttpUrl(url);
                case "RTSP":
                    return new RtspUrl(url);
                case "RTMP":
                    return new RtmpUrl(url);
                case "UDP":
                    return new UdpRtpUrl(url);
                default:
                    return null;
            }
        }

        #endregion

        #region Constants

        public static readonly Hashtable SupportedProtocols = null;

        #endregion
    }
}
