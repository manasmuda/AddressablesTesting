# Unity Addressables — Dependency & Bundle Behavior

**Terminology used in this document:**
- **AG** = Addressable Group
- **Bundle** = Asset Bundle (output of an AG)
- **SO** = Scriptable Object
- **Explicit asset** = directly added to an AG; has its own addressable entry in the catalog
- **Implicit asset** = pulled into a bundle as a dependency of an explicit asset; has no catalog entry of its own

---

## Core Rules

### Rule 1: Explicit vs Implicit Assets
- Every asset directly added to an AG becomes an **explicit asset** in its bundle.
- Any non-addressable asset that an explicit asset depends on becomes an **implicit asset** in that same bundle.
- Implicit assets have no catalog entry and cannot be loaded independently.

### Rule 2: Addressable Dependencies → External Bundle References
- If asset A depends on asset B, and asset B is addressable (in any AG), the build creates an **external bundle reference** from A's bundle to B's bundle.
- B's data is **not duplicated** into A's bundle.

### Rule 3: Non-Addressable Dependencies → Implicit Asset Duplication
- If asset A depends on asset B, and asset B is **not** addressable, B is embedded as an implicit asset in A's bundle.
- If multiple bundles depend on the same non-addressable asset B, each bundle gets its own copy — **no deduplication across bundles**.
- The build system does not deduplicate implicit assets by GUID across different bundles.

### Rule 4: Prefab Hierarchy Children → Inline Serialization (Not References)
- When a prefab or scene embeds another prefab as a **hierarchy child**, the child's full data is **serialized inline** into the parent bundle at build time.
- This happens regardless of whether the child prefab is addressable.
- Child prefabs never appear as implicit assets — they are baked directly into the parent's serialized content.
- As a consequence, all of the child prefab's own dependencies (SOs, images, etc.) are also baked into the parent bundle, causing duplication.

### Rule 5: Prefab Field References → External Bundle References
- When a prefab or script holds another prefab as a **serialized field reference** (not a hierarchy child), the dependency is treated like any other asset reference.
- If the referenced prefab is addressable, an external bundle reference is created — no duplication.
- This is the pattern that allows Addressables to share prefab data across bundles.

### Rule 6: Scene References Are Ignored by the Build System
- If an asset (prefab, SO, script) holds a reference to a scene, the Addressables build system **completely ignores that scene reference**.
- This is true even if Unity's AssetDatabase reports the scene as a dependency of that asset — the AssetDatabase relationship is editor-only and does not carry over to the build.
- The scene is never pulled in as an implicit asset, never creates an external bundle reference, and is never included in any bundle as a result of being referenced by another asset.
- If you need a scene at runtime, it must be made explicitly addressable in its own AG — holding a reference to it from another asset provides no build coverage.

---

## Scenarios

### Scenario 1: Shared Non-Addressable SO Between Two Prefab Bundles

**Setup:**
- AG1: Prefab1 (depends on SO1 + 4 images)
- AG2: Prefab2 (has Prefab1 as hierarchy child, depends on same SO1 + 4 images)

**Build Output:**
- Bundle1: Prefab1 (explicit), SO1 + 4 images (implicit)
- Bundle2: Prefab2 (explicit), SO1 + 4 images (implicit)
- Prefab1 is **not** present as an implicit asset in Bundle2

**Key Observations:**
- SO1 and the 4 images are duplicated across both bundles because they are non-addressable.
- Prefab1 does not appear as an implicit asset in Bundle2 — initial assumption was that this was because Prefab1 is addressable, but Scenario 2 revised this theory.

---

### Scenario 2: Addressable SO Referenced by Prefab with Non-Addressable Child Prefab

**Setup:**
- AG1: SO1 only (with 4 images as dependencies)
- AG2: Prefab2 (has Prefab1 as hierarchy child; Prefab1 depends on SO1)

**Build Output:**
- Bundle1: SO1 (explicit), 4 images (implicit)
- Bundle2: Prefab2 (explicit), **no implicit assets**, external reference → Bundle1 for SO1

**Key Observations:**
- Since SO1 is now addressable, Bundle2 references Bundle1 instead of duplicating SO1.
- Prefab1 is still not an implicit asset in Bundle2 — its data is serialized inline into Prefab2's bundle data.
- This revised the Scenario 1 theory: prefabs never appear as implicit assets regardless of their addressable status. Non-addressable prefab data is baked in via serialization.

---

### Scenario 3: Third Bundle Sharing One Image from an Addressable SO

**Setup:**
- AG1: SO1 (with Image1, Image2, Image3, Image4 as dependencies)
- AG2: Prefab2 (hierarchy child Prefab1, which depends on SO1) — same as Scenario 2
- AG3: Prefab3 (directly depends on Image1)

**Build Output:**
- Bundle1: SO1 (explicit), 4 images including Image1 (implicit) — unchanged
- Bundle2: Prefab2 (explicit), no implicit assets, external ref → Bundle1 — unchanged
- Bundle3: Prefab3 (explicit), Image1 (implicit) — **duplicated from Bundle1**

**Key Observations:**
- Image1 exists in both Bundle1 (as implicit under SO1) and Bundle3 (as implicit under Prefab3).
- Making Image1 addressable in its own AG would eliminate this duplication.

---

### Scenario 4: Shared Child Prefab Across Multiple Bundles

**Setup:**
- AG1: Base Prefab (depends on SO1 + 4 images)
- AG2: PrefabA (has Base Prefab as hierarchy child)
- AG3: PrefabB (has Base Prefab as hierarchy child)

**Build Output:**
- Bundle1: Base Prefab (explicit), SO1 + 4 images (implicit)
- Bundle2: PrefabA (explicit), SO1 + 4 images (implicit), **no external reference to Bundle1**
- Bundle3: PrefabB (explicit), SO1 + 4 images (implicit), **no external reference to Bundle1**

**Key Observations:**
- SO1 and all 4 images are duplicated **3x** across all bundles.
- Even though Base Prefab is addressable, PrefabA and PrefabB do not create external references to Bundle1.
- The hierarchy child relationship causes full inline serialization of Base Prefab's data — including all its dependencies — into each parent bundle.
- This is a major source of hidden bundle bloat when base prefabs are reused via hierarchy nesting.

---

### Scenario 5: Scenes with Shared Child Prefab

**Setup:**
- AG1: SceneA (has Base Prefab as hierarchy child)
- AG2: SceneB (has Base Prefab as hierarchy child)

**Build Output:**
- Same result as Scenario 4 — SO and images duplicated across both scene bundles, no external references to the shared prefab.

**Key Observations:**
- Scenes follow the same inline serialization pattern as prefabs for hierarchy children.
- The duplication behavior is not specific to prefab-in-prefab nesting — any asset type that embeds a prefab as a hierarchy child will bake in that prefab's data.

---

### Scenario 6: Prefab Referenced via Script Field (Not Hierarchy Child)

**Setup:**
- AG1: Base Prefab (depends on SO + images)
- AG2: PrefabA (holds Base Prefab as a serialized script field reference)
- AG3: PrefabB (holds Base Prefab as a serialized script field reference)

**Build Output:**
- Bundle1: Base Prefab (explicit), SO + images (implicit)
- Bundle2: PrefabA (explicit), external reference → Bundle1
- Bundle3: PrefabB (explicit), external reference → Bundle1

**Key Observations:**
- SO and images exist only once, in Bundle1. No duplication.
- A field reference is treated as an asset dependency, not inline serialization — Addressables can resolve it via the catalog and create proper inter-bundle references.
- **This is the correct pattern for sharing prefabs across multiple addressable assets.**

**Comparison Table:**

| Dependency Method | Mechanism | Duplication |
|---|---|---|
| Prefab as hierarchy child | Inline serialization | Yes — full data baked into parent bundle |
| Prefab as script/SO field reference | External bundle reference | No — resolved via catalog at runtime |

---

### Scenario 7: Two SOs Referencing the Same Non-Addressable Images

**Setup:**
- AG1: SO1 (depends on 4 images)
- AG2: Prefab with SO2 (SO2 also depends on the same 4 images)

**Build Output:**
- Bundle1: SO1 (explicit), 4 images (implicit)
- Bundle2: Prefab (explicit), 4 images (implicit) — **duplicated**

**Key Observations:**
- The build system does **not** deduplicate implicit assets by asset GUID across bundles.
- Even when two SOs reference the exact same image files, each bundle gets its own copy.
- The only fix is to make the shared images addressable in their own AG.

---

### Scenario 8: Mixed Addressable and Non-Addressable Dependencies Across Two Groups

**Setup:**
- AG1: Image3, Prefab34 (→ Image3, Image4), Prefab341 (→ Image3, Image4, Image1)
- AG2: Image4, Prefab43 (→ Image3, Image4)
- Image1 is not in any AG (non-addressable)

**Build Output:**
- Bundle1: Image3 (explicit), Prefab34 (explicit), Prefab341 (explicit), Image1 (implicit), external ref → Bundle2 for Image4
- Bundle2: Image4 (explicit), Prefab43 (explicit), external ref → Bundle1 for Image3

**Key Observations:**
- Image3 and Image4 are both addressable → cross-bundle references, no duplication.
- Image1 is non-addressable → becomes an implicit asset in Bundle1 (the only bundle that needs it, via Prefab341).
- Multiple explicit assets within the same bundle (Image3, Prefab34, Prefab341) share their bundle's implicit assets without duplication within that bundle.

---

## Summary of Dependency Resolution Rules

| Asset Type | Addressable? | Result |
|---|---|---|
| SO / Texture / Image | No | Implicit asset — duplicated in every bundle that needs it |
| SO / Texture / Image | Yes | External bundle reference — no duplication |
| Prefab (hierarchy child) | No | Inline serialized into parent bundle — all its dependencies also baked in |
| Prefab (hierarchy child) | Yes | Still inline serialized — addressable status does not prevent baking |
| Prefab (field reference) | Yes | External bundle reference — no duplication |
| Scene child prefab | No / Yes | Same as prefab hierarchy child — inline serialized |

---

## Practical Recommendations

1. **Make shared non-prefab assets addressable.** Any SO, texture, or image used by multiple AGs should be placed in its own AG to convert implicit duplication into external references.

2. **Avoid hierarchy child prefabs for shared content.** If a prefab is used as a child in multiple other prefabs or scenes, its data (and all its dependencies) will be duplicated in every parent bundle — regardless of whether the child is addressable.

3. **Use field references for shared prefabs.** Holding a prefab reference in a script field (e.g., `public GameObject prefabRef`) allows Addressables to create external bundle references and eliminates duplication.

4. **Audit implicit assets regularly.** Use the Addressables Analyze tool (Duplicate Bundle Dependencies rule) to detect cases where the same non-addressable asset is being baked into multiple bundles.

5. **Implicit assets within a bundle are not duplicated.** Multiple explicit assets in the same AG share any implicit assets within that bundle — duplication only occurs across different bundles.
