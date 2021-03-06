﻿using System.Collections.Generic;

namespace OnlineVideos.Sites.JSurf.Interfaces
{
    /// <summary>
    /// Information interface - getting categories, channels, etc
    /// </summary>
    public interface IInformationConnector
    {
        /// <summary>
        /// Load the catgeories - if no parentCategory is specified we'll load just the root categories
        /// </summary>
        /// <param name="parentCategory"></param>
        /// <returns></returns>
        List<Category> LoadCategories(Category parentCategory = null);

        /// <summary>
        /// Load all videos for the specified category - Load the videos for a category
        /// </summary>
        /// <param name="parentCategory"></param>
        /// <returns></returns>
        List<VideoInfo> LoadVideos(Category parentCategory);

        /// <summary>
        /// The type name of the entity to use as the BrowserUtilConnector
        /// </summary>
        string ConnectorEntityTypeName { get; }

        /// <summary>
        /// Should the main util sort the video results
        /// </summary>
        bool ShouldSortResults { get; }

        bool CanSearch { get; }

        List<SearchResultItem> DoSearch(string query);

    }
}
