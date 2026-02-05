using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace AddressablesCatalogViewer
{
    /// <summary>
    /// Runtime utility to decode and inspect Addressables catalog data.
    /// Can be used to understand the dependency structure at runtime.
    /// </summary>
    public static class CatalogDecoder
    {
        #region Data Structures
        
        /// <summary>
        /// Represents a decoded catalog entry with all resolved references.
        /// </summary>
        public class CatalogEntry
        {
            public int Index { get; set; }
            
            // Raw indices from binary data
            public int InternalIdIndex { get; set; }
            public int ProviderIndex { get; set; }
            public int DependencyKeyIndex { get; set; }
            public int DependencyHash { get; set; }
            public int DataIndex { get; set; }
            public int PrimaryKeyIndex { get; set; }
            public int ResourceTypeIndex { get; set; }
            
            // Resolved values
            public string InternalId { get; set; }
            public string ProviderId { get; set; }
            public string PrimaryKey { get; set; }
            public string ResourceTypeName { get; set; }
            
            // Computed properties
            public bool IsBundle => ProviderId?.Contains("AssetBundleProvider") == true;
            public bool IsBundledAsset => ProviderId?.Contains("BundledAssetProvider") == true;
            public bool IsResource => ProviderId?.Contains("LegacyResourcesProvider") == true;
            public bool HasDependencies => DependencyKeyIndex >= 0;
            public bool IsRemote => InternalId?.StartsWith("http") == true;
            
            public override string ToString()
            {
                return $"[{Index}] {InternalId} (Provider: {ProviderId?.Split('.')[^1]}, DepKeyIdx: {DependencyKeyIndex})";
            }
        }

        /// <summary>
        /// Represents bundle loading options extracted from m_ExtraDataString.
        /// </summary>
        public class BundleRequestOptions
        {
            public string Hash { get; set; }
            public long Crc { get; set; }
            public int Timeout { get; set; }
            public bool ChunkedTransfer { get; set; }
            public int RedirectLimit { get; set; }
            public int RetryCount { get; set; }
            public string BundleName { get; set; }
            public int AssetLoadMode { get; set; }
            public long BundleSize { get; set; }
            public bool UseCrcForCachedBundles { get; set; }
            public bool UseUWRForLocalBundles { get; set; }
            public bool ClearOtherCachedVersionsWhenLoaded { get; set; }
            
            public override string ToString()
            {
                return $"Bundle: {BundleName}, Hash: {Hash}, Size: {BundleSize} bytes";
            }
        }

        /// <summary>
        /// Complete decoded catalog with all data accessible.
        /// </summary>
        public class DecodedCatalog
        {
            public string LocatorId { get; set; }
            public string[] InternalIds { get; set; }
            public string[] ProviderIds { get; set; }
            public List<CatalogEntry> Entries { get; set; }
            public List<BundleRequestOptions> BundleOptions { get; set; }
            public Dictionary<int, List<CatalogEntry>> DependencyGroups { get; set; }
            
            /// <summary>
            /// Get all entries that depend on a specific dependency key index.
            /// </summary>
            public List<CatalogEntry> GetEntriesByDependencyKey(int depKeyIndex)
            {
                if (DependencyGroups != null && DependencyGroups.TryGetValue(depKeyIndex, out var entries))
                    return entries;
                return new List<CatalogEntry>();
            }
            
            /// <summary>
            /// Get all bundle entries (entries that ARE bundles, not assets in bundles).
            /// </summary>
            public List<CatalogEntry> GetBundleEntries()
            {
                var result = new List<CatalogEntry>();
                foreach (var entry in Entries)
                {
                    if (entry.IsBundle)
                        result.Add(entry);
                }
                return result;
            }
            
            /// <summary>
            /// Get all asset entries (entries that are assets inside bundles).
            /// </summary>
            public List<CatalogEntry> GetAssetEntries()
            {
                var result = new List<CatalogEntry>();
                foreach (var entry in Entries)
                {
                    if (entry.IsBundledAsset)
                        result.Add(entry);
                }
                return result;
            }
            
            /// <summary>
            /// Find entries by internal ID path (partial match).
            /// </summary>
            public List<CatalogEntry> FindByInternalId(string searchTerm)
            {
                var result = new List<CatalogEntry>();
                string lowerSearch = searchTerm.ToLower();
                foreach (var entry in Entries)
                {
                    if (entry.InternalId?.ToLower().Contains(lowerSearch) == true)
                        result.Add(entry);
                }
                return result;
            }
        }
        
        #endregion

        #region Decoding Methods

        /// <summary>
        /// Decode a catalog JSON string into a fully parsed DecodedCatalog object.
        /// </summary>
        public static DecodedCatalog DecodeCatalogJson(string catalogJson)
        {
            // Parse the root JSON
            var rawCatalog = JsonUtility.FromJson<RawCatalogData>(catalogJson);
            
            var decoded = new DecodedCatalog
            {
                LocatorId = rawCatalog.m_LocatorId,
                InternalIds = rawCatalog.m_InternalIds,
                ProviderIds = rawCatalog.m_ProviderIds,
                Entries = new List<CatalogEntry>(),
                BundleOptions = new List<BundleRequestOptions>(),
                DependencyGroups = new Dictionary<int, List<CatalogEntry>>()
            };

            // Decode entries from binary
            DecodeEntries(rawCatalog, decoded);
            
            // Decode bundle options
            DecodeBundleOptions(rawCatalog.m_ExtraDataString, decoded);
            
            // Build dependency groups
            BuildDependencyGroups(decoded);
            
            return decoded;
        }

        private static void DecodeEntries(RawCatalogData rawCatalog, DecodedCatalog decoded)
        {
            if (string.IsNullOrEmpty(rawCatalog.m_EntryDataString))
                return;

            byte[] entryBytes = Convert.FromBase64String(rawCatalog.m_EntryDataString);
            
            // Each entry is 28 bytes (7 x int32)
            const int entrySize = 28;
            int numEntries = entryBytes.Length / entrySize;

            for (int i = 0; i < numEntries; i++)
            {
                int offset = i * entrySize;
                
                var entry = new CatalogEntry
                {
                    Index = i,
                    InternalIdIndex = BitConverter.ToInt32(entryBytes, offset),
                    ProviderIndex = BitConverter.ToInt32(entryBytes, offset + 4),
                    DependencyKeyIndex = BitConverter.ToInt32(entryBytes, offset + 8),
                    DependencyHash = BitConverter.ToInt32(entryBytes, offset + 12),
                    DataIndex = BitConverter.ToInt32(entryBytes, offset + 16),
                    PrimaryKeyIndex = BitConverter.ToInt32(entryBytes, offset + 20),
                    ResourceTypeIndex = BitConverter.ToInt32(entryBytes, offset + 24)
                };

                // Resolve InternalId
                if (entry.InternalIdIndex >= 0 && entry.InternalIdIndex < decoded.InternalIds.Length)
                {
                    entry.InternalId = decoded.InternalIds[entry.InternalIdIndex];
                }

                // Resolve ProviderId
                if (entry.ProviderIndex >= 0 && entry.ProviderIndex < decoded.ProviderIds.Length)
                {
                    entry.ProviderId = decoded.ProviderIds[entry.ProviderIndex];
                }

                // Resolve ResourceType
                if (rawCatalog.m_resourceTypes != null && 
                    entry.ResourceTypeIndex >= 0 && 
                    entry.ResourceTypeIndex < rawCatalog.m_resourceTypes.Length)
                {
                    entry.ResourceTypeName = rawCatalog.m_resourceTypes[entry.ResourceTypeIndex].m_ClassName;
                }

                decoded.Entries.Add(entry);
            }
        }

        private static void DecodeBundleOptions(string extraDataString, DecodedCatalog decoded)
        {
            if (string.IsNullOrEmpty(extraDataString))
                return;

            try
            {
                byte[] extraBytes = Convert.FromBase64String(extraDataString);
                
                // Extra data is UTF-16 LE encoded
                string text = Encoding.Unicode.GetString(extraBytes);
                
                // Find and parse JSON objects
                int depth = 0;
                int start = -1;
                
                for (int i = 0; i < text.Length; i++)
                {
                    if (text[i] == '{')
                    {
                        if (depth == 0) start = i;
                        depth++;
                    }
                    else if (text[i] == '}')
                    {
                        depth--;
                        if (depth == 0 && start >= 0)
                        {
                            string jsonObj = text.Substring(start, i - start + 1);
                            try
                            {
                                var rawOptions = JsonUtility.FromJson<RawBundleOptions>(jsonObj);
                                if (rawOptions != null && !string.IsNullOrEmpty(rawOptions.m_Hash))
                                {
                                    decoded.BundleOptions.Add(new BundleRequestOptions
                                    {
                                        Hash = rawOptions.m_Hash,
                                        Crc = rawOptions.m_Crc,
                                        Timeout = rawOptions.m_Timeout,
                                        ChunkedTransfer = rawOptions.m_ChunkedTransfer,
                                        RedirectLimit = rawOptions.m_RedirectLimit,
                                        RetryCount = rawOptions.m_RetryCount,
                                        BundleName = rawOptions.m_BundleName,
                                        AssetLoadMode = rawOptions.m_AssetLoadMode,
                                        BundleSize = rawOptions.m_BundleSize,
                                        UseCrcForCachedBundles = rawOptions.m_UseCrcForCachedBundles,
                                        UseUWRForLocalBundles = rawOptions.m_UseUWRForLocalBundles,
                                        ClearOtherCachedVersionsWhenLoaded = rawOptions.m_ClearOtherCachedVersionsWhenLoaded
                                    });
                                }
                            }
                            catch { }
                            start = -1;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Failed to decode bundle options: {e.Message}");
            }
        }

        private static void BuildDependencyGroups(DecodedCatalog decoded)
        {
            foreach (var entry in decoded.Entries)
            {
                int depKey = entry.DependencyKeyIndex;
                if (!decoded.DependencyGroups.ContainsKey(depKey))
                    decoded.DependencyGroups[depKey] = new List<CatalogEntry>();
                decoded.DependencyGroups[depKey].Add(entry);
            }
        }

        #endregion

        #region Utility Methods

        /// <summary>
        /// Print a summary of the decoded catalog to the console.
        /// </summary>
        public static void PrintCatalogSummary(DecodedCatalog catalog)
        {
            var sb = new StringBuilder();
            
            sb.AppendLine("═══════════════════════════════════════════════════════════════");
            sb.AppendLine("ADDRESSABLES CATALOG SUMMARY");
            sb.AppendLine("═══════════════════════════════════════════════════════════════");
            sb.AppendLine($"Locator ID: {catalog.LocatorId}");
            sb.AppendLine($"Internal IDs: {catalog.InternalIds?.Length ?? 0}");
            sb.AppendLine($"Providers: {catalog.ProviderIds?.Length ?? 0}");
            sb.AppendLine($"Total Entries: {catalog.Entries?.Count ?? 0}");
            sb.AppendLine($"Bundle Options: {catalog.BundleOptions?.Count ?? 0}");
            sb.AppendLine($"Dependency Groups: {catalog.DependencyGroups?.Count ?? 0}");
            
            sb.AppendLine();
            sb.AppendLine("PROVIDERS:");
            if (catalog.ProviderIds != null)
            {
                for (int i = 0; i < catalog.ProviderIds.Length; i++)
                {
                    string shortName = catalog.ProviderIds[i].Split('.')[^1];
                    sb.AppendLine($"  [{i}] {shortName}");
                }
            }
            
            sb.AppendLine();
            sb.AppendLine("BUNDLE ENTRIES:");
            var bundles = catalog.GetBundleEntries();
            foreach (var bundle in bundles)
            {
                string location = bundle.IsRemote ? "[REMOTE]" : "[LOCAL]";
                sb.AppendLine($"  {location} {bundle.InternalId}");
            }
            
            sb.AppendLine();
            sb.AppendLine("DEPENDENCY GROUPS:");
            foreach (var kvp in catalog.DependencyGroups)
            {
                sb.AppendLine($"  DepKeyIdx={kvp.Key}: {kvp.Value.Count} entries");
            }
            
            Debug.Log(sb.ToString());
        }

        /// <summary>
        /// Get the dependency chain for a specific entry.
        /// Returns the list of entries that must be loaded before this one.
        /// </summary>
        public static List<CatalogEntry> GetDependencyChain(DecodedCatalog catalog, CatalogEntry entry)
        {
            var chain = new List<CatalogEntry>();
            var visited = new HashSet<int>();
            
            void TraverseDeps(CatalogEntry current)
            {
                if (current == null || visited.Contains(current.Index))
                    return;
                    
                visited.Add(current.Index);
                
                if (current.DependencyKeyIndex >= 0)
                {
                    // Find entries that this depends on
                    foreach (var dep in catalog.Entries)
                    {
                        // This is simplified - actual dependency resolution is more complex
                        if (dep.IsBundle && dep.Index != current.Index)
                        {
                            if (!chain.Contains(dep))
                            {
                                chain.Add(dep);
                                TraverseDeps(dep);
                            }
                        }
                    }
                }
            }
            
            TraverseDeps(entry);
            return chain;
        }

        #endregion

        #region Raw JSON Data Classes (for JsonUtility)

        [Serializable]
        private class RawCatalogData
        {
            public string m_LocatorId;
            public string[] m_ProviderIds;
            public string[] m_InternalIds;
            public string m_KeyDataString;
            public string m_BucketDataString;
            public string m_EntryDataString;
            public string m_ExtraDataString;
            public RawResourceType[] m_resourceTypes;
        }

        [Serializable]
        private class RawResourceType
        {
            public string m_AssemblyName;
            public string m_ClassName;
        }

        [Serializable]
        private class RawBundleOptions
        {
            public string m_Hash;
            public long m_Crc;
            public int m_Timeout;
            public bool m_ChunkedTransfer;
            public int m_RedirectLimit;
            public int m_RetryCount;
            public string m_BundleName;
            public int m_AssetLoadMode;
            public long m_BundleSize;
            public bool m_UseCrcForCachedBundles;
            public bool m_UseUWRForLocalBundles;
            public bool m_ClearOtherCachedVersionsWhenLoaded;
        }

        #endregion
    }
}
