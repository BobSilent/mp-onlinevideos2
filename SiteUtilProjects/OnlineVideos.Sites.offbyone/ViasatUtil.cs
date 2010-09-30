﻿using System;
using System.Collections.Generic;
using System.Xml;

namespace OnlineVideos.Sites
{
    public class ViasatUtil : SiteUtilBase
    {
        enum ViasatChannel { Sport = 1, TV3, TV6, TV8 };

        public override int DiscoverDynamicCategories()
        {
            Settings.Categories.Clear();

            foreach (int i in Enum.GetValues(typeof(ViasatChannel)))
            {
                Category cat = new Category() { Name = ((ViasatChannel)i).ToString(), HasSubCategories = true, SubCategoriesDiscovered = false };
                Settings.Categories.Add(cat);
            }

            Settings.DynamicCategoriesDiscovered = true;
            return Settings.Categories.Count;
        }

        public override int DiscoverSubCategories(Category parentCategory)
        {
            Category channelCategory = parentCategory;
            while (channelCategory.ParentCategory != null) channelCategory = channelCategory.ParentCategory;
            ViasatChannel channel = (ViasatChannel)Enum.Parse(typeof(ViasatChannel), channelCategory.Name);            
            string id = parentCategory is RssLink ? ((RssLink)parentCategory).Url : "0";
            XmlDocument xDoc;
            if (channel != ViasatChannel.Sport)
            {
                xDoc = GetWebData<XmlDocument>("http://viastream.viasat.tv/siteMapData/se/" + ((int)channel).ToString() + "se/" + id);
            }
            else
            {
                xDoc = GetWebData<XmlDocument>("http://viastream.player.mtgnewmedia.se/xml/xmltoplayer.php?type=siteMapData&channel=" + ((int)channel).ToString() + "se&country=se&category=" + id);
            }            
            parentCategory.SubCategories = new List<Category>();
            foreach (XmlElement e in xDoc.SelectNodes("//siteMapNode"))
            {
                RssLink subCat = new RssLink() { Name = e.Attributes["title"].Value, Url = e.Attributes["id"].Value, ParentCategory = parentCategory };
                if (e.Attributes["children"].Value == "true") subCat.HasSubCategories = true;
                parentCategory.SubCategories.Add(subCat);
            }                
            parentCategory.SubCategoriesDiscovered = true;
            return parentCategory.SubCategories.Count;
        }

        public override List<VideoInfo> getVideoList(Category category)
        {
            List<VideoInfo> videos = new List<VideoInfo>();
            Category channelCategory = category;
            while (channelCategory.ParentCategory != null) channelCategory = channelCategory.ParentCategory;
            ViasatChannel channel = (ViasatChannel)Enum.Parse(typeof(ViasatChannel), channelCategory.Name);
            string doc = "";          
            if (channel != ViasatChannel.Sport)
            {
                doc = GetWebData("http://viastream.viasat.tv/Products/Category/" + ((RssLink)category).Url);
            }
            else
            {
                doc = GetWebData("http://viastream.player.mtgnewmedia.se/xml/xmltoplayer.php?type=Products&category=" + ((RssLink)category).Url);
            }
            XmlDocument xDoc = new XmlDocument();
            xDoc.LoadXml(doc);
            foreach (XmlElement e in xDoc.SelectNodes("//Product"))
            {
                VideoInfo video = new VideoInfo();
                video.Title = e.SelectSingleNode("Title").InnerText;
                video.VideoUrl = e.SelectSingleNode("ProductId").InnerText;
                video.Other = channel;
                videos.Add(video);
            }            
            return videos;
        }

        public override string getUrl(VideoInfo video)
        {
            ViasatChannel channel = (ViasatChannel)video.Other;
            string doc = "";          
            if (channel != ViasatChannel.Sport)
            {
                doc = GetWebData("http://viastream.viasat.tv/Products/" + video.VideoUrl);
            }
            else
            {
                doc = GetWebData("http://viastream.player.mtgnewmedia.se/xml/xmltoplayer.php?type=Products&clipid=" + video.VideoUrl);
            }

            doc = System.Text.RegularExpressions.Regex.Replace(doc, "&(?!amp;)", "&amp;");
            XmlDocument xDoc = new XmlDocument();
            xDoc.LoadXml(doc);

            string playstr = xDoc.SelectSingleNode("Products/Product/Videos/Video/Url").InnerText;

            XmlNode geo;
            if ((geo = xDoc.SelectSingleNode("Products/Product/Geoblock")) != null)
            {
                if (geo.InnerText == "true")
                {
                    xDoc.LoadXml(GetWebData(playstr));
                    playstr = xDoc.SelectSingleNode("GeoLock/Url").InnerText;

                    if (playstr.ToLower().StartsWith("rtmp"))
                    {
                        playstr = ReverseProxy.GetProxyUri(RTMP_LIB.RTMPRequestHandler.Instance,
                            string.Format("http://127.0.0.1/stream.flv?rtmpurl={0}&swfurl{1}=&swfhash={2}&swfsize={3}",
                            System.Web.HttpUtility.UrlEncode(playstr),
                            System.Web.HttpUtility.UrlEncode("http://flvplayer.viastream.viasat.tv/flvplayer/play/swf/player100920.swf"),
                            "9d14a8849c059734a62544c44bdc252fe6961c00f50739c4cd12fca42a503b41",
                            1326663));
                    }
                }
            }

            return playstr;
        }
    }
}
