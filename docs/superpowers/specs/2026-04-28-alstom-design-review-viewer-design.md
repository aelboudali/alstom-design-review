# Alstom Design Review Viewer — Design Spec

**Date:** 2026-04-28
**Status:** Approved (high-level approval; remaining decisions taken by author with documented defaults)
**Engagement context:** Pre-production prototype to demonstrate a Pixyz Review-equivalent design-review tool to Alstom, built on Unity's Industry Viewer Template + OpenXR for VR
**Timeline:** 16 weeks, one focused engineer
**Primary dev platform:** macOS (Apple Silicon)
**Primary VR target:** HTC Vive Focus Vision (standalone, OpenXR via AndroidXR)

---

## 1. Goal and non-goals

### Goal

Deliver a working pre-production prototype that lets Alstom engineers conduct CAD design reviews — on desktop and in VR on Vive Focus Vision — with multi-user co-presence. The prototype must cover most of Pixyz Review's feature surface (per the 2018.2 Getting Started doc) so an Alstom audience evaluating "should we keep buying Pixyz Review or fund this approach" can see meaningful side-by-side capability.

### Non-goals

- Production deployment (no Alstom IT integration, no PLM hookup, no SSO with Alstom AD)
- Full Pixyz Review parity (PMI/FTA display, runtime Motion Group authoring, Magic Window, Powerwall, HoloLens, version-compare are explicitly out of v1 scope)
- B-rep accuracy in measurements (mesh-based runtime edge detection is the v1 approach; B-rep is a v2 path requiring Asset Transformer customization)
- Local-file CADPart drag-and-drop (the template hard-requires Asset Manager; matching Pixyz's file-from-disk workflow is out of scope)

---

## 2. Architecture summary

### 2.1 Repository strategy

Fork `Unity-Technologies/unity-industry-viewer-template` into a new repo `alstom-design-review`. Track upstream as a Git remote (`upstream`) and selectively merge upstream fixes; do **not** vendor as a submodule. The template's per-feature `.asmdef` boundaries are the seam for additions.

| Disposition | Template feature folder |
|---|---|
| Keep & extend | `Assets`, `Identity`, `Streaming`, `Collaboration`, `Multiplay`, `Vivox`, `Shared`, `Localization` |
| Keep but slim | `DeepLinking` (single-tenant), `VR` (rebuilt for Vive Focus Vision) |
| Disable / unused | mobile AR features in `Streaming/Navigation/Mobile AR` (kept compilable, not polished or tested) |

New feature folders (each with its own `.asmdef`):

- `Features/Pixyz.MotionGroups/` — data model, Editor authoring window, runtime binder, VR grab integration
- `Features/Pixyz.Measurement/` — edge-graph extraction, primitive recognition, measurement compute, panel UI, VR measure tool
- `Features/Pixyz.Sectioning/` — extends template's section cut with axis dropdown, Show-Plane volume detection, Align-Camera
- `Features/Pixyz.Explode/` — exploded view per-axis with per-MotionGroup sensitivity
- `Features/Pixyz.Render/` — wireframe-on-shaded, lines, X-ray render modes (URP custom passes)
- `Features/Pixyz.Materials/` — Standard / PBR / Color / UnlitTexture preset shaders + palette + apply-via-property
- `Features/Pixyz.Snapshot/` — capture, persist to Asset Manager, restore camera/visibility/section state
- `Features/Pixyz.ViewCube/` — view cube widget + predefined views
- `Features/Alstom.Branding/` — logos, colors, splash, all override assets in one folder

Namespace new code as `Pixyz.Review.<Feature>`. Template namespaces (`Unity.Industry.Viewer.<Feature>`) untouched so upstream merges don't conflict.

### 2.2 Build profiles

| Profile | Target | Primary use |
|---|---|---|
| `Desktop-macOS-AppleSilicon` | .app, ARM64 | Primary dev box; Mac reviewer machines |
| `Desktop-Win64` | .exe, x64 | Windows reviewer machines |
| `Standalone-AndroidXR` | .apk, ARM64-v8a | Vive Focus Vision standalone |

PCVR streaming mode is **not** part of the build matrix (Vive Streaming Hub / SteamVR are Windows-only and incompatible with Mac dev). For very heavy assemblies that exceed Vive Focus Vision's standalone perf budget, a side Windows machine streaming via SteamVR is the documented out-of-band option, not a shipped feature.

Auth namespace: change `com.unity.industry-viewer` → `com.alstom.design-review` per template's `build-and-publish.md`.

### 2.3 Asset-prep pipeline

Cloud-side preparation is the default, locally-run Asset Transformer SDK is a documented fallback:

1. Engineer drops CATIA/STEP/JT files into `Assets/_AlstomSamples/Sources/` (or directly upload via Asset Manager web UI)
2. Asset Manager Source dataset created via SDK upload from the Editor
3. Trigger **Prepare for 3D Data Streaming** action on the Source dataset (Editor button or Asset Manager web UI) — Cloud transforms server-side; preserves hierarchy + metadata
4. Streamable dataset published with stable dataset ID
5. **Sidecar authoring step (custom):** open Editor scene, drag the streamed dataset's hierarchy in, run **Motion Group Authoring Tool** (`Pixyz.MotionGroups.Editor.AuthoringWindow`) to create Motion Groups + constraints + pivots + flags. Tool serializes a `MotionGroupSet.asset` ScriptableObject keyed by Asset Manager dataset ID
6. **v1 storage: sidecar is Git-tracked** in the project repo at `Assets/_AlstomSamples/MotionGroups/<datasetId>.asset`. No Asset Manager round-trip during dev.
7. **v2 storage (deferred, when runtime authoring lands): sidecar uploaded to Asset Manager** as a metadata attachment so multiple reviewers can edit collaboratively. Same data shape; serialized as JSON for the attachment payload.

At runtime: when the streaming dataset loads, viewer fetches the matching `MotionGroupSet.asset` by dataset ID (from local Resources in v1, from Asset Manager attachment in v2). If absent, asset still loads — just no articulations.

### 2.4 Data flow (state-only multiplayer)

No model bytes flow over the network. Each participant streams the asset independently from Asset Manager. Only state coordinates synchronize over NGO Distributed Authority:

- Selection (per-participant highlight)
- Cutting plane transform + enabled flag
- Exploded view factor + axis flags
- Per-Motion-Group transform deltas (only `grabbable` groups; static groups don't sync)
- Active measurements (created → broadcast → all participants render leader-line + value)
- Camera transforms (opt-in stream per participant for "See view of")

Voice via Vivox (template default).

---

## 3. Feature roadmap

### 3.1 Tier 1 — Desktop core (weeks 1–10)

| # | Feature | Effort | Notes |
|---|---|---|---|
| 1 | Repo fork; branding swap (Alstom logo in `Assets/UI/Background.uxml` and `Features/Identity/UI/Identity.uxml`; app name; splash screen; primary App UI theme color); 3 build profiles wired (Mac/Win/AndroidXR); one Alstom sample asset prepped via Cloud-side transform; deep-link namespace changed to `com.alstom.design-review` | 1.5 wk | "Hello Alstom" milestone — asset opens, hierarchy renders, login works |
| 2 | CATIA-like navigation rebind (mouse-only mode default; Standard ALT+mouse available in Settings); box-select with depth-selection (right-drag through-wall) | 0.5 wk | Pixyz reviewers expect CATIA-like default |
| 3 | View cube widget + predefined views (front/back/L/R/top/bottom/iso) + Fit-to-selection | 1 wk | UI Toolkit overlay anchored top-right of viewer |
| 4 | Render modes: shaded / wireframe-on-shaded / lines / X-ray; toolbar dropdown | 1 wk | URP custom passes; X-ray = depth-tested transparency |
| 5 | Hierarchy panel parity: Simple/Advanced mode switch; contextual menu (Show/Hide/Invert/Find-in-tree/Fit/Add Light); search with case-sensitive toggle | 1 wk | Extends template's `Streaming` panel |
| 6 | Properties panel parity: surface PartNumber, Revision, Definition, Nomenclature, Source, Author, Description as named fields plus generic key-value list | 0.5 wk | Data already in streamed metadata; UI work only |
| 7 | Cutting plane extension: axis dropdown (X/Y/Z/Camera); Show-Plane filled-vs-hollow detection; Align-Camera-to-section; edge color + thickness; lock-to-camera; invert-visibility | 1.5 wk | New `Pixyz.Sectioning` feature |
| 8 | Exploded view: per-axis (X/Y/Z) factor sliders; single-axis or planar; per-MotionGroup sensitivity flag respected | 1 wk | New `Pixyz.Explode` feature |
| 9 | Material editor: Standard/PBR/Color/UnlitTexture presets; basic palette (yellow/orange/red/cyan/blue/green/magenta/grey); apply via property | 1 wk | URP-compatible PBR |
| 10 | Snapshots: capture viewport with current camera/visibility/section state; name + thumbnail; persist to Asset Manager; restore on click | 1 wk | Uses `com.unity.modules.screencapture` |
| 11 | **Measurement engine** (Section 4) | **2.5 wk** | Single hardest engineering piece |

### 3.2 Tier 2 — Motion Groups, VR, multi-user (weeks 11–16)

| # | Feature | Effort | Notes |
|---|---|---|---|
| 12 | **Motion Groups data model + Editor authoring tool** (Section 5) | 2 wk | EditorWindow + ScriptableObject sidecar |
| 13 | VR scene ported to Vive Focus Vision: validate `AndroidXR-OpenXR` runtime; rebind hand-tracking gestures; controller bindings for Vive controllers | 1 wk | Highest-risk porting work; budget contingency |
| 14 | VR radial menu (Teleport / Visualization sub / Selection-Laser / Measurement sub / Snapshot / Exit-VR) anchored to non-dominant controller; selection laser on dominant | 1 wk | Matches Pixyz's immersive menu |
| 15 | In-VR tools: grab-cutting-plane; touchpad-slide explode/scale; distance/angle/radius measure-in-VR; snapshot from VR | 1 wk | Reuses Tier 1 measurement + sectioning |
| 16 | **Motion Group grab-with-constraints in VR**: collision highlight; trigger-grab; constraint-respecting motion; kinematic-anchor flag honored | 1 wk | The "wow" demo moment |
| 17 | Distributed multi-user co-presence: avatars + name labels + Vivox voice + Motion Group state sync over NGO Distributed Authority | 1 wk | Template's Multiplay+Vivox; Motion Group state is the new sync target |
| 18 | "See view of" — switch your camera to another participant's POV (drop-down in Collab panel) | 0.5 wk | NGO RPC; net-new but high-value demo moment |

### 3.3 Out of v1 scope (documented as v2 / stretch)

| Feature | Reason deferred |
|---|---|
| PMI/FTA 3D display | Requires Asset Transformer prep customization to extract PMI nodes; high risk for prototype timeline. v2 priority #1. |
| Runtime Motion Group authoring UI | Editor authoring is sufficient for curated demos; data model already supports runtime authoring. v2 polish item. |
| Version compare / model diff | Pixyz Review doesn't have it either; pure leapfrog feature for v2 differentiation. |
| Magic Window / Powerwall / HoloLens / Vision Pro | Vive Focus Vision covers stated VR target. |
| Mobile AR placement | Template has it free, but design-review use case doesn't need it. |
| Local-file CADPart drag-and-drop | Asset Manager dependency is hard; matching Pixyz's local-file ingest needs custom Asset Transformer SDK driver. |
| Clearance-min between part hulls | Useful measurement but separate algorithm + per-part hull caching; out of 16-week budget. |
| B-rep-accurate measurements | Requires Asset Transformer customization to retain B-rep edges; v2 path. |

### 3.4 Demo-readiness checkpoints

- **End of week 4** — *Hello Alstom*: branded build, Alstom asset loads on Mac, navigation + hide + screenshot work. Internal-only.
- **End of week 10** — *Desktop parity demo*: hierarchy + properties + measurements + cut + explode + snapshots + materials + render modes. First-pass demo to friendly Alstom contact.
- **End of week 14** — *VR demo*: Vive Focus Vision standalone; walk around the asset; open a door (Motion Group grab); measure something in VR. Solo VR.
- **End of week 16** — *Full prototype demo*: two reviewers in same session, one on Mac desktop, one in Vive Focus Vision; voice + avatars + shared Motion Group state. The pitch demo.

---

## 4. Measurement engine (key system #1)

Goal: Pixyz Review's primitive-recognition feel — fly-over highlights edges/circles; snap green-dot; fall back orange-dot at surface point — on **tessellated mesh data**, since B-rep is stripped by the Cloud streaming pipeline.

### 4.1 Per-mesh edge graph

Computed once, asynchronously, on streamed mesh load. Cached by streamed mesh ID.

1. **Sharp-edge extraction:** for each triangle pair sharing an edge, compute dihedral angle. If > θ_sharp (default 30°, configurable per asset) mark as sharp.
2. **Polyline chaining:** chain sharp edges that meet at vertices of degree 2 (continuous edge loops/strips).
3. **Circle fitting:** for each closed polyline of N≥8 vertices, run **Pratt circle fit** (more numerically stable than Kåsa). If RMSE < ε_circle (default 0.5% of mesh bbox diagonal) → register as `Circle{center, radius, axis}`.
4. **Plane fitting:** for each near-flat patch (cluster of triangles with normals within θ_plane) register as `Plane{point, normal}`.
5. Spatial hash keyed by 3D position for O(log N) cursor → nearest-primitive query.

### 4.2 Recognition flow

User workflow mirrors Pixyz Review (PDF page 23): user first **selects the primitive type** they want to measure (Polyline / Circle / Point) from the Measurement panel toolbar; then fly-over snaps within that type. This narrows the search space and makes intent explicit.

- User selects target primitive type → enters fly-over mode
- Raycast (mouse or VR laser) hits triangle → look up its mesh's edge graph
- Within the selected type only, find nearest primitive within snap radius (screen-space for desktop, 3D for VR)
- If found: **green dot** at primitive's snap point (line midpoint, circle center, plane centroid, etc.)
- If not: **orange dot** at raw surface intersection (always available as a Point primitive regardless of selected type)

### 4.3 Primitive types

| Primitive | Fields |
|---|---|
| Point | position, optional normal |
| Line | 2 points, derived direction |
| Polyline | ordered list of points |
| Circle | center, radius, axis (normal) |
| Plane | point, normal |

### 4.4 Operations

| Operation | Inputs | Output |
|---|---|---|
| Distance | 2× Point | scalar mm |
| Distance | Point + Line/Plane/Circle | perpendicular scalar mm |
| Center distance | 2× Circle | scalar mm |
| Angle | 3× Point | scalar deg |
| Angle | 2× Line/Plane | scalar deg |
| Plane inclination | 2× Plane | scalar deg |
| Radius / Diameter / Perimeter / Area | 1× Circle or Polyline | scalars |
| Convert | 1× primitive | derived primitive (Point→Line via normal, Circle→Plane via axis, Polyline→Plane via fit, etc.) |

### 4.5 Persistence

Each measurement is a `Measurement` record (primitive refs + computed values + visibility flag) stored per session. "Attach to Snapshot" copies into a Snapshot's measurement set. Measurements panel shows all with show/hide checkboxes, click-to-isolate, delete.

### 4.6 VR variant

Identical recognition pipeline. UI is the radial menu's Measurement submenu (Distance / Angle / Radius). Result rendered as 3D billboard with leader line at the primitive.

### 4.7 Known limitations (to disclose to Alstom)

- **Curved surface measurement:** a cylinder's sweep has no sharp edges → engine only finds circles at end-caps. Mitigation: increase Asset Transformer tessellation density at prep time for measurement-critical assets; document file-size vs primitive-count trade-off.
- **Sub-millimeter accuracy:** tessellation chord error caps measurement accuracy. Default Asset Transformer presets give ~0.2mm chord; "Maximum Quality" preset for tighter (with larger streamed dataset).
- These limitations are the cost of skipping B-rep. Frame explicitly: *for design review (clearance, fit) it's invisible; for manufacturing verification, revisit with B-rep retention*.

### 4.8 Calibration task (week 11)

Defaults (θ_sharp=30°, ε_circle=0.5%-bbox) are starting points. Calibration against a representative Alstom CAD asset is a v1 implementation task, not a finalized parameter set. Expected outcome: per-asset-class config presets (e.g., "machined-mechanical", "weldment", "AEC").

---

## 5. Motion Groups (key system #2)

A Motion Group is a logical sub-assembly of parts grouped for animation/interaction with constrained DOF. Mirrors Pixyz Review's Motion Group concept (per the 2018.2 Getting Started doc, pages 19–22).

### 5.1 Data model (sidecar ScriptableObject)

```
MotionGroupSet
  ├─ assetManagerDatasetId : string
  └─ groups : List<MotionGroup>

MotionGroup
  ├─ id : Guid
  ├─ name : string
  ├─ parentGroupId : Guid?              # nullable, for nested groups
  ├─ memberPartPaths : List<string>     # stable hierarchy paths in streamed dataset
  ├─ pivot : Pose                       # millimeters / degrees
  ├─ constraints : ConstraintSet        # 6 AxisConstraints (TX/TY/TZ/RX/RY/RZ)
  ├─ flags
  │   ├─ grabbable : bool = true
  │   ├─ responsiveToLaser : bool = true
  │   ├─ kinematicAnchor : bool = false
  │   └─ rotationCenterOnGrabber : bool = false
  ├─ vizFlags
  │   ├─ cuttingPlaneSensitive : bool = true
  │   ├─ scaleSensitive : bool = true
  │   ├─ explodedViewSensitive : bool = true
  │   └─ setAsExplodeCenter : bool = false
  └─ overrideMaterial : MaterialRef?    # nullable

AxisConstraint
  ├─ mode : { Locked, Free, Constrained }
  ├─ min, max : float                   # only when Constrained
  └─ unit : { Millimeters, Degrees }
```

Member parts are referenced by **hierarchy path strings** (e.g. `root/CATIA_Asset/Bogie/Door.1/Handle`), not by GameObject reference, so the sidecar survives reimport.

### 5.2 Why a sidecar (not Asset Manager metadata)

- Editable in Editor with custom inspector — no Cloud round-trip per change
- Diffs cleanly in Git for review
- Decoupled from Cloud schema changes
- Tier 2+ runtime authoring writes the same shape back via Asset Manager attachment API as JSON
- **Sidecar is the source of truth.** Asset Manager metadata for the streamed dataset is read-only from the viewer's perspective.

### 5.3 Editor authoring tool (`Pixyz.MotionGroups.Editor.AuthoringWindow`)

1. Drag a streamed dataset's root scene object into the window
2. Browse hierarchy + multi-select parts in Scene view → "Create Motion Group from Selection"
3. Per-group inspector:
   - Pivot gizmo with `MovePivotOnly` toggle (mirrors Pixyz)
   - Six AxisConstraint dropdowns (Locked / Free / Constrained); when Constrained, min/max numeric input + unit toggle
   - Flag checkboxes
   - Material override picker
4. "Test in Play Mode" button — spawns asset, binds groups, lets you grab in Editor with mouse to validate constraints before VR

### 5.4 Runtime resolver (`Pixyz.MotionGroups.Runtime.MotionGroupBinder`)

- On asset load: fetch matching `MotionGroupSet` by dataset ID. If absent, asset still loads with no articulations.
- For each group: resolve `memberPartPaths` against streamed hierarchy → spawn `MotionGroupController` MonoBehaviour wrapping the member transforms; attach `XRGrabInteractable` with constraint-applying motion filter.
- Selection hook: clicking any member part selects the whole group (consistent with Pixyz).
- Sectioning, Exploded View, Scale all consult the group's `vizFlags` before applying transformations.

### 5.5 Networking

In multi-user sessions, only `grabbable` groups sync transforms over NGO Distributed Authority. Static groups never sync (saves bandwidth on dense assemblies). Authority follows the grabber: when participant A grabs group X, A becomes authoritative for X until release.

### 5.6 Risks / mitigations

- **Hierarchy path stability across asset re-prep:** if Asset Transformer assigns different IDs on re-prep, paths break. Mitigation: separate `RenameMap.asset` the prep operator can apply post-prep; plus name-based fallback resolver. Fallback behavior on conflict: if a part name is ambiguous (multiple matches), the resolver logs a warning, leaves that group disabled at runtime, and surfaces the conflict in the Editor authoring tool's "Issues" panel for the operator to resolve manually. No silent fuzzy-matching.
- **Nested groups (parent/child):** require topological-sort load order; trivial but must be explicit.

---

## 6. UI architecture

### 6.1 Desktop UI

App UI dockable panels (`com.unity.dt.app-ui`), matching Pixyz's mental model:

- Toolbar across top (left → right slots): Import (greyed v1), Save, Reset, Materials, Color-by-part, Boost (greyed v1), Fit-to-view, Camera-mode (Orbit/Fly), Backface-culling, Show/Hide, Fullscreen, Screenshot, View-cube, Enter-VR, Render-mode dropdown
- Product Structure panel (left) with Simple/Advanced mode toggle
- Properties panel (bottom-left) with named CAD fields + generic K/V list
- Right-panel tabs: Visualization (Cutting Plane / Exploded View / VR System) / Measurement / Collab / Tracking (placeholder tab, deferred — Pixyz uses this for VR-tracking config like Vive lighthouse calibration; Vive Focus Vision is standalone with no external tracking, so the tab ships empty in v1 with a "configured automatically" note)
- Viewer center, with HUD overlay top-left (occurrences + triangles + FPS)

Each panel is a UI Toolkit document loaded by its feature controller.

### 6.2 VR UI — radial menu

Anchored to non-dominant controller; surfaces when CLIP/menu-button is held. Six segments:

- Teleport
- Visualization → sub-radial: Cut / Explode / Scale
- Selection-laser
- Measurement → sub-radial: Distance / Angle / Radius
- Snapshot
- Exit-VR

Dominant controller carries the selection laser + grab trigger.

### 6.3 Localization

EN + FR shipped (Unity Localization, template-supported per-feature `Localization/` folders). Alstom is French — FR is required.

### 6.4 Layout persistence

Save Layout / Reset Workspace via the template's existing `SaveLayoutController.cs`. User-defined workspaces stored as named ScriptableObjects per project.

---

## 7. Multi-user

### 7.1 Networking model

- NGO Distributed Authority for state sync (template default)
- Vivox for voice (template default)
- Each participant streams the asset independently from Asset Manager — no model bytes over NGO (matches Pixyz's IP-protection guarantee)

### 7.2 Synced state

- Selection (per-participant highlight)
- Cutting plane transform + enabled flag
- Exploded view factor + axis flags
- Per-Motion-Group transform deltas (only `grabbable` groups)
- Active measurements
- Camera transforms (opt-in stream for "See view of")

### 7.3 "See view of"

Drop-down in Collab panel listing connected participants. Selecting one binds your camera to RPC-streamed pose updates from theirs until "Release".

### 7.4 Authority

Default Distributed Authority. When a participant grabs a Motion Group, they become authoritative for that group's transform until release. Sectioning / exploded-view changes broadcast from whoever changes them; last-write-wins.

---

## 8. Build, test, deployment

### 8.1 Build matrix (recap)

| Profile | Target |
|---|---|
| `Desktop-macOS-AppleSilicon` | .app, ARM64 |
| `Desktop-Win64` | .exe, x64 |
| `Standalone-AndroidXR` | .apk, ARM64-v8a (Vive Focus Vision) |

### 8.2 CI

GitHub Actions matrix using `game-ci` action. Three jobs, one per profile. Asset prep is **not** in CI; manual Editor step against Asset Manager.

### 8.3 Test plan

**Edit-mode unit tests** (`Pixyz.*.Tests`):

- `AxisConstraint` math: clamp/free/locked behavior across boundary cases
- Edge-graph extraction on procedurally generated mesh (cube, cylinder, sphere) → expected edge/circle counts
- Pratt circle fit: synthetic 2D points + noise → radius within tolerance
- `MotionGroupSet` serialization round-trip

**Play-mode integration tests** (Unity Test Framework):

- Asset load via Asset Manager mock → hierarchy renders → known part path resolves
- Cutting plane axis swap updates clipping uniform
- Snapshot capture → restore returns identical camera + visibility state

**Manual VR test checklist** (no automation possible on headset):

- Vive Focus Vision boot → Asset Manager login (QR-pair-with-desktop flow)
- Hand-tracking gesture map: pinch-to-grab, point-laser, palm-up-for-menu
- All Tier 2 items #14–18 walked through with written checklist per build

**Network test:** two-instance local + one Vive Focus Vision; validate state sync + voice + "See view of".

### 8.4 Deployment

Vive Focus Vision: sideload APK via ADB (works on Mac). No app-store distribution in v1.

Desktop builds: zipped artifact per build, distributed to test reviewers manually. No code-signing in v1 (will trip macOS Gatekeeper; documented in test plan).

---

## 9. Risk register

| # | Risk | Likelihood | Impact | Mitigation |
|---|---|---|---|---|
| R1 | Vive Focus Vision OpenXR runtime quirks (template untested on it) | High | 1–2 wk slip | Budget 1 wk porting (Tier 2 #13); fallback to Quest 3 dev headset for early VR work |
| R2 | Edge-detection accuracy on real Alstom CAD insufficient | Medium | Measurements feel inferior to Pixyz | Calibration task v1 against representative asset; document tessellation density vs accuracy trade-off; v2 path is Asset Transformer customization |
| R3 | Cloud-side prep latency / quota during dev | Medium | Slow iteration | Build small lib (5–10) of pre-prepped sample assets early; use them for daily dev; only re-prep when validating end-to-end |
| R4 | Hierarchy path instability across asset re-prep | Medium | Motion Group sidecar breaks | RenameMap file applied at load; name-based fallback resolver; document re-prep workflow |
| R5 | Vive controller binding map differs from Quest | Low | Hand-tracking gestures need rebind | XR Interaction Toolkit handles per-device action maps; allocate buffer in Tier 2 #13 |
| R6 | Asset Manager auth flow on Vive Focus Vision (no native browser for SSO redirect) | Medium | Reviewer can't log in on headset | QR-code-pair-with-desktop flow; or device-code flow if Cloud Identity supports it; validate week 11 |
| R7 | Demo asset not representative of real Alstom complexity | Low | Demo doesn't land | Source public train/automotive sample early (GrabCAD); request Alstom-friendly model from author by week 4 |

---

## 10. Open decisions to revisit before v2

- **PMI/FTA strategy:** customize Asset Transformer to retain PMI annotations vs. parse from a separate exported file vs. drop the feature. Decide week 14 based on what Alstom's PMI workflow actually looks like.
- **B-rep retention:** if measurement accuracy lands as a dealbreaker in v1 demos, the v2 path is custom Asset Transformer prep that emits B-rep edge graphs alongside tessellated mesh; runtime consumes both. ~3–4 week effort.
- **Authoring location:** runtime Motion Group authoring UI vs. keep Editor-only. Decide based on whether reviewers want to create groups themselves or accept a "preparator" role does it upfront.
- **Hosting model:** Asset Manager (Cloud-only) vs. introduce a self-hosted streaming server for customers with offline / on-prem requirements. v3 question, not v2.

---

## 11. Implementation plan structure

16 weeks is too long for a single monolithic implementation plan — verification cadence breaks down past about 6 weeks. The implementation plan should split into **three phases**, each with its own demo-ready checkpoint:

- **Plan A — Foundation (weeks 1–4):** items #1–#4 from §3.1. Outcome: *Hello Alstom* milestone. Re-plan after this checkpoint based on what was learned about Asset Manager prep latency, Mac dev velocity, and template integration friction.
- **Plan B — Desktop parity (weeks 5–10):** items #5–#11 from §3.1. Outcome: *Desktop parity demo*. The hardest engineering (measurement engine) lands here.
- **Plan C — VR + multi-user (weeks 11–16):** items #12–#18 from §3.2. Outcome: *Full prototype demo*.

Plan A is what the writing-plans skill produces in this session. Plans B and C are written at their respective checkpoints, after Plan A's lessons reshape estimates and unknowns.

## 12. Prerequisites for the implementing developer

Before starting implementation:

- [ ] Unity 6.0+ (`6000.3.5f2` per template) installed on macOS Apple Silicon
- [ ] Unity Industry seat active; Cloud Identity SSO configured for the dev account
- [ ] Asset Manager organization + project created (any name; `alstom-design-review-prototype` recommended)
- [ ] Asset Transformer Toolkit accessible from a Windows side machine (for any prep that fails Cloud-side) or Asset Transformer SDK on Mac (Apple Silicon native, Sonoma 14+ confirmed supported)
- [ ] Vive Focus Vision headset + USB-C cable + ADB working from Mac
- [ ] Sample Alstom-representative CAD asset (CATPart/CATProduct or STEP) — public train/automotive model from GrabCAD acceptable for v1
- [ ] Git remote for `alstom-design-review` repo (GitHub or GitLab)
- [ ] One Windows machine accessible for Win64 build validation (CI handles cross-build, but smoke-test once)
