using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using MediaPortal.GUI.Library;
using MediaPortal.Player;
using OnlineVideos.Downloading;
using OnlineVideos.Helpers;

namespace OnlineVideos.MediaPortal1
{
	public class LatestVideosManager
	{
		Thread workerThread;
		// Signalled by Stop() to cooperatively interrupt the worker loop and its sleeps.
		readonly ManualResetEventSlim _stopEvent = new ManualResetEventSlim(false);

		public void Start()
		{
			if (workerThread == null && PluginConfiguration.Instance.LatestVideosMaxItems > 0)
			{
				_stopEvent.Reset();
				workerThread = new Thread(Worker) { IsBackground = true, Name = "OVLatest" };
				workerThread.Start();
			}
		}

		public void Pause()
		{
			// Pause is handled cooperatively inside the worker via _stopEvent sleeps.
		}

		public void Stop()
		{
			_stopEvent.Set(); // wake the worker from any sleep and tell it to exit
		}

		private void Worker()
		{
			bool setOnce = false;
			uint currentRotationIndex = 0;
			DateTime lastDiscovery = DateTime.MinValue;
			List<KeyValuePair<string, VideoInfo>> latestVideos = new List<KeyValuePair<string, VideoInfo>>();
			try
			{
				while (!_stopEvent.IsSet)
				{
					int maxItems = (int)PluginConfiguration.Instance.LatestVideosMaxItems;
					if ((DateTime.Now - lastDiscovery).TotalMinutes > PluginConfiguration.Instance.LatestVideosOnlineDataRefresh)
					{
						Dictionary<string, List<VideoInfo>> latestVideosPerSite = DiscoverAllLatestVideos();
						lastDiscovery = DateTime.Now;
						currentRotationIndex = 0;
						setOnce = false;
						int previousLatestVideosCount = latestVideos.Count;
						latestVideos.Clear();
						// Flatten per-site lists directly — avoids Select IEnumerable allocation per site.
						foreach (var l in latestVideosPerSite)
						{
							foreach (var v in l.Value)
							{
								latestVideos.Add(new KeyValuePair<string, VideoInfo>(l.Key, v));
							}
						}
						Log.Instance.Info("LatestVideosManager found {0} videos from {1} SiteUtils.", latestVideos.Count, latestVideosPerSite.Count);
						int less = Math.Min(previousLatestVideosCount, maxItems) - Math.Min(latestVideos.Count, maxItems);
						while (less > 0)
						{
							// reset the GuiProperties in case we found less latest videos than before and than should be shown in rotation
							ResetLatestVideoGuiProperties(maxItems - less + 1);
							less--;
						}
						// SetProperty expects lowercase "true"/"false" — avoid ToString().ToLower() allocation.
						bool hasVideos = Math.Min(latestVideos.Count, maxItems) > 0;
						GUIPropertyManager.SetProperty("#OnlineVideos.LatestVideosEnabled", hasVideos ? "true" : "false");
						if (latestVideos.Count > 0)
						{
							if (PluginConfiguration.Instance.LatestVideosRandomize) latestVideos.Randomize();
							// ConvertAll avoids a separate Select+ToList allocation.
							ImageDownloader.DownloadImages<VideoInfo>(latestVideos.ConvertAll(v => v.Value));
						}
					}
					if (latestVideos.Count > 0 && (!setOnce || latestVideos.Count > maxItems)) // only needed ONCE if there are no more latestVideos than amount to be shown
					{
						for (int i = 1; i <= Math.Min(latestVideos.Count, maxItems); i++)
						{
							int num = (int)currentRotationIndex + i - 1;
							if (num >= latestVideos.Count) num = i - 1;
							SetLatestVideoGuiProperties(latestVideos[num], i);
						}
						setOnce = true;
						currentRotationIndex++;
						if (currentRotationIndex >= latestVideos.Count) currentRotationIndex = 0;
					}
					// Sleep for the rotation interval; returns early if Stop() is called.
					_stopEvent.Wait(TimeSpan.FromSeconds(PluginConfiguration.Instance.LatestVideosGuiDataRefresh));
					if (_stopEvent.IsSet) break;
					// Don't rotate during fullscreen playback; wake immediately on Stop().
					while (!_stopEvent.IsSet && g_Player.FullScreen)
					{
						_stopEvent.Wait(TimeSpan.FromSeconds(1));
					}
				}
			}
			catch (Exception ex)
			{
				Log.Instance.Warn("LatestVideos thread ended unexpected: {0}", ex.Message);
			}
			workerThread = null;
		}

		private Dictionary<string, List<VideoInfo>> DiscoverAllLatestVideos()
		{
			Log.Instance.Info("LatestVideosManager getting new data from SiteUtils.");
			Dictionary<string, List<VideoInfo>> latestVideos = new Dictionary<string, List<VideoInfo>>();
			foreach (var site in OnlineVideoSettings.Instance.LatestVideosSiteUtilsList)
			{
				if (site.LatestVideosCount > 0)
				{
					try
					{
						var l = site.GetLatestVideos();
						if (l != null && l.Count > 0)
						{
							latestVideos.Add(site.Settings.Name, l.Take((int)site.LatestVideosCount).ToList());
						}
					}
					catch (Exception ex)
					{
						Log.Instance.Warn("Error getting latest videos from '{0}': {1}", site.Settings.Name, ex.Message);
					}
				}
			}
			return latestVideos;
		}

		private void SetLatestVideoGuiProperties(KeyValuePair<string, VideoInfo> video, int index)
		{
			string prefix = $"#OnlineVideos.LatestVideo{index}.";
			GUIPropertyManager.SetProperty(prefix + "Site", video.Key);

			string siteIcon = SiteImageExistenceCache.GetImageForSite(video.Key, null, "Icon");
			if (string.IsNullOrEmpty(siteIcon)) siteIcon = SiteImageExistenceCache.GetImageForSite("OnlineVideos", type: "Icon");
			if (siteIcon == null) siteIcon = string.Empty;
			GUIPropertyManager.SetProperty(prefix + "SiteIcon", siteIcon);

			GUIPropertyManager.SetProperty(prefix + "Title",       video.Value.Title);
			GUIPropertyManager.SetProperty(prefix + "Aired",       video.Value.Airdate);
			GUIPropertyManager.SetProperty(prefix + "Duration",    video.Value.Length);
			GUIPropertyManager.SetProperty(prefix + "Thumb",       video.Value.ThumbnailImage);
			GUIPropertyManager.SetProperty(prefix + "Description", video.Value.Description);
		}

		private void ResetLatestVideoGuiProperties(int index)
		{
			string prefix = $"#OnlineVideos.LatestVideo{index}.";
			GUIPropertyManager.SetProperty(prefix + "Site",        string.Empty);
			GUIPropertyManager.SetProperty(prefix + "SiteIcon",    string.Empty);
			GUIPropertyManager.SetProperty(prefix + "Title",       string.Empty);
			GUIPropertyManager.SetProperty(prefix + "Aired",       string.Empty);
			GUIPropertyManager.SetProperty(prefix + "Duration",    string.Empty);
			GUIPropertyManager.SetProperty(prefix + "Thumb",       string.Empty);
			GUIPropertyManager.SetProperty(prefix + "Description", string.Empty);
		}
	}
}
