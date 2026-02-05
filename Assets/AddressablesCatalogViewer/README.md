# Addressables Catalog Viewer (Corrected Version)

A Unity Editor tool that properly decodes and visualizes Addressables catalog files using Unity's built-in `ContentCatalogData.CreateLocator()` method.

## Why This Approach?

**The previous version was WRONG** because it attempted to manually parse the binary-encoded fields (`m_EntryDataString`, `m_BucketDataString`, `m_KeyDataString`, `m_ExtraDataString`) with incorrect assumptions about their format.

### The Correct Approach

Unity's Addressables catalog format is complex and has evolved through multiple versions:
- Json "v1", "v2", "v3"
- Binary "Binv1", "Binv1 v1.1", "Binv2"

Instead of manually parsing, we should:

1. **Inside Unity**: Use `ContentCatalogData.CreateLocator()` - Unity's own deserialization
2. **Outside Unity**: Use proven libraries like:
   - [AddressablesTools](https://github.com/nesrak1/AddressablesTools) (C#)
   - [AddressablesToolsPy](https://github.com/anosu/AddressablesToolsPy) (Python)

## How the Catalog Actually Works

### Key Insight: DependencyKey

Each `IResourceLocation` has a `DependencyKey` property (not `DependencyKeyIndex`!). This key can be looked up in the `Resources` dictionary to find the actual dependency locations.

```
Asset Location (e.g., "MyPrefab.prefab")
├── ProviderId: "BundledAssetProvider"
├── InternalId: "Assets/Prefabs/MyPrefab.prefab"
└── Dependencies: IList<IResourceLocation>
    └── Bundle Location
        ├── ProviderId: "AssetBundleProvider"
        ├── InternalId: "https://cdn.com/bundle_a.bundle"
        └── Dependencies: IList<IResourceLocation>
            ├── Bundle B Location (if Bundle A depends on B)
            └── Bundle C Location (etc.)
```

### Correct Dependency Resolution

From AddressablesTools documentation:

```csharp
// Find all keys that contain the substring we're searching for
// and have resource locations with ProviderId of "BundledAssetProvider"
var assetLocations = catalog.Resources["Assets/MyAsset.prefab"];

// Get the DependencyKey
var depKey = assetLocations[0].DependencyKey;

// Look up the dependency in the Resources dictionary
var bundleLocations = catalog.Resources[depKey];

// The first item is always the bundle containing the asset
// Additional items are dependencies of that bundle
```

## Installation

1. Copy the `AddressablesCatalogViewerV2` folder into your Unity project's `Assets` folder
2. Ensure you have the Addressables package installed
3. Open via `Window > Addressables > Catalog Viewer (Correct)`

## Usage

### Method 1: Load External Catalog
1. Click "Browse" to select a `catalog.json` file
2. Click "Load Catalog"

### Method 2: Load Current Project Catalog
1. Build your Addressables first (Groups window > Build)
2. Click "Load Current Project Catalog"

### Viewing Dependencies
1. Toggle "Assets" section on
2. Click on any asset to select it
3. Toggle "Dependency Tree" to see the full dependency chain

## Features

- **Bundles View**: Shows all AssetBundles with local/remote indicators
- **Assets View**: Shows all bundled assets with their types
- **Dependency Tree**: Interactive dependency chain visualization
- **Search**: Filter by address or internal ID
- **Export**: Generate a text report of the entire catalog

## External Tools

If you need to parse catalogs outside of Unity (e.g., for modding tools):

### AddressablesTools (C# - Recommended)
```csharp
// NuGet: AssetsTools.NET.Addressables
using AddressablesTools;

var catalog = AddressablesJsonParser.FromString(File.ReadAllText("catalog.json"));

// Access resources
foreach (var kvp in catalog.Resources)
{
    string key = kvp.Key.ToString();
    var locations = kvp.Value;
    
    foreach (var loc in locations)
    {
        Console.WriteLine($"Key: {key}");
        Console.WriteLine($"  InternalId: {loc.InternalId}");
        Console.WriteLine($"  Provider: {loc.ProviderId}");
        Console.WriteLine($"  DependencyKey: {loc.DependencyKey}");
    }
}
```

### AddressablesToolsPy (Python)
```python
from AddressablesTools import parse

catalog = parse(Path("catalog.json").read_text())

# Find dependencies
asset_locs = catalog.Resources["Assets/MyAsset.prefab"]
dep_key = asset_locs[0].DependencyKey
bundle_loc = catalog.Resources[dep_key][0]

print(f"Asset depends on bundle: {bundle_loc.InternalId}")
```

## Catalog Binary Format (For Reference)

The catalog has these base64-encoded binary sections:

| Field | Purpose |
|-------|---------|
| m_KeyDataString | Keys/addresses - variable-length strings and other key types |
| m_BucketDataString | Hash bucket → entry mappings |
| m_EntryDataString | Location entries (NOT 28-byte fixed format in all versions!) |
| m_ExtraDataString | Serialized objects like AssetBundleRequestOptions |

**DO NOT attempt to manually parse these** unless you're prepared to handle all format versions. Use the tools mentioned above.

## Common Mistakes (What NOT to Do)

❌ Assuming m_EntryDataString is always 28 bytes per entry
❌ Assuming DepKeyIdx directly points to another entry
❌ Ignoring catalog version differences
❌ Not handling the serialization format for m_ExtraDataString

## License

MIT License - Use freely in your projects.

## Credits

- [nesrak1/AddressablesTools](https://github.com/nesrak1/AddressablesTools) - Reverse engineering reference
- [anosu/AddressablesToolsPy](https://github.com/anosu/AddressablesToolsPy) - Python port
