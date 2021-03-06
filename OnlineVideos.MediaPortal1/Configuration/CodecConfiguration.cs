﻿using System;
using System.IO;
using Microsoft.Win32;
using System.Security.AccessControl;
using System.Diagnostics;
using System.Collections;
using DShowNET.Helper;
using DirectShowLib;

namespace OnlineVideos.MediaPortal1
{
    /// <summary>
    /// http://msdn.microsoft.com/en-us/library/dd377513(VS.85).aspx
    /// </summary>
    public class CodecConfiguration
    {
        public const string MediaSubType_FLV = "{F2FAC0F1-3852-4670-AAC0-9051D400AC54}";
        public const string MediaSubType_AVI = "{E436EB88-524F-11CE-9F53-0020AF0BA770}";

        public struct Codec
        {
            public string CLSID;
            public string Name;
            public string[] FileTypes;
            public string CodecFile;
            public string Version;
            public bool IsInstalled;

            public override string ToString()
            {
                return string.Format("{0} | {1} | {2}", Name, Version, CodecFile);
            }
        }

        public Codec FLV_Splitter = new Codec() { FileTypes = new string[] { ".flv" } };
        public Codec AVI_Splitter = new Codec() { FileTypes = new string[] { ".avi" } };
        public Codec MPC_HC_MP4Splitter = new Codec() { CLSID = "{61F47056-E400-43D3-AF1E-AB7DFFD4C4AD}", Name = "MPC-HC MP4 Splitter", FileTypes = new string[] { ".mp4", ".m4v", ".mov" } };
        public Codec HaaliMediaSplitter = new Codec() { CLSID = "{55DA30FC-F16B-49FC-BAA5-AE59FC65F82D}", Name = "Haali Media Splitter", FileTypes = new string[] { ".mp4", ".m4v", ".mov" } };
        public Codec WM_ASFReader = new Codec() { CLSID = "{187463A0-5BB7-11D3-ACBE-0080C75E246E}", Name = "WM ASF Reader", FileTypes = new string[] { ".wmv" } };

        #region Singleton
        private static CodecConfiguration _Instance = new CodecConfiguration();
        public static CodecConfiguration Instance
        {
            get
            {
                if (_Instance == null) _Instance = new CodecConfiguration();
                return _Instance;
            }
        }
        #endregion

        private CodecConfiguration()
        {
            // get FLV splitter from registry by MediaSubType
            Merit highestMerit = Merit.DoNotUse;
            ArrayList list = FilterHelper.GetFilters(MediaType.Stream, new Guid(MediaSubType_FLV));
            if (list != null)
            {
                foreach(Filter filter in Filters.LegacyFilters)
                {
                    if (list.Contains(filter.Name))
                    {
                        Merit m = GetMerit(filter.CLSID.ToString());
                        if (m > highestMerit)
                        {
                            FLV_Splitter.CLSID = filter.CLSID.ToString("B");
                            FLV_Splitter.Name = filter.Name;
                            highestMerit = m;                            
                        }
                    }
                }
            }
            // get AVI splitter from registry by MediaSubType
            highestMerit = Merit.DoNotUse;
            list = FilterHelper.GetFilters(MediaType.Stream, new Guid(MediaSubType_AVI));
            if (list != null)
            {
                foreach (Filter filter in Filters.LegacyFilters)
                {
                    if (list.Contains(filter.Name))
                    {
                        Merit m = GetMerit(filter.CLSID.ToString());
                        if (m > highestMerit)
                        {
                            AVI_Splitter.CLSID = filter.CLSID.ToString("B");
                            AVI_Splitter.Name = filter.Name;
                            highestMerit = m;
                        }
                    }
                }
            }
            CheckCodec(ref FLV_Splitter);
            CheckCodec(ref AVI_Splitter);
            CheckCodec(ref MPC_HC_MP4Splitter);
            CheckCodec(ref HaaliMediaSplitter);
            CheckCodec(ref WM_ASFReader);
        }

        public static void CheckCodec(ref Codec codec)
        {
            try
            {
                RegistryKey key = Registry.ClassesRoot.OpenSubKey(@"CLSID\" + codec.CLSID + @"\InprocServer32", RegistryKeyPermissionCheck.ReadSubTree, RegistryRights.ReadKey);
                if (key != null)
                {
                    codec.CodecFile = key.GetValue("", null).ToString();
                    if (!Path.IsPathRooted(codec.CodecFile))
                    {
                        string systemPath = Environment.GetFolderPath(Environment.SpecialFolder.System);
                        codec.CodecFile = Path.Combine(systemPath, codec.CodecFile);
                    }
                    if (System.IO.File.Exists(codec.CodecFile))
                    {                        
                        FileVersionInfo info = FileVersionInfo.GetVersionInfo(codec.CodecFile);
                        codec.Version = string.Format("{0}.{1}.{2}.{3}", info.ProductMajorPart, info.ProductMinorPart, info.ProductBuildPart, info.ProductPrivatePart);
                        codec.IsInstalled = true;
                    }
                }
            }
            catch (Exception) { }
        }

        private static Merit GetMerit(string clsid)
        {
            try
            {
                RegistryKey filterKey =
                  Registry.ClassesRoot.OpenSubKey(@"CLSID\{083863F1-70DE-11d0-BD40-00A0C911CE86}\Instance\{" + clsid + @"}");
                if (filterKey == null)
                {
                    Log.Instance.Debug("Could not get merit value for clsid {0}, key not found!", clsid);
                    return Merit.DoNotUse;
                }
                Byte[] filterData = (Byte[])filterKey.GetValue("FilterData", 0x0);
                if (filterData == null || filterData.Length < 8)
                {
                    return Merit.DoNotUse;
                }
                Byte[] merit = new Byte[4];
                //merit is 2nd DWORD, reverse byte order
                Array.Copy(filterData, 4, merit, 0, 4);
                uint dwMerit = ReverseByteArrayToDWORD(merit);                
                return (Merit)dwMerit;
            }
            catch (Exception e)
            {
                Log.Instance.Debug("Could not get merit value for clsid {0}. Error: {1}", clsid, e.Message);
                return Merit.DoNotUse;
            }
        }

        private static uint ReverseByteArrayToDWORD(Byte[] ba)
        {
            return (uint)(((uint)ba[3] << 24) | ((uint)ba[2] << 16) | ((uint)ba[1] << 8) | ba[0]);
        }
    }
}
