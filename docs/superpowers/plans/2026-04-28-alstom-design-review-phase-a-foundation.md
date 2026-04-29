# Alstom Design Review — Phase A: Foundation Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Reach the *Hello Alstom* milestone — a branded fork of the Unity Industry Viewer Template that opens an Alstom-prepped CAD asset on Mac, with CATIA-like navigation, box-select with depth, view cube + predefined views, and render-mode switching working end-to-end.

**Architecture:** Fork `Unity-Technologies/unity-industry-viewer-template` into `alstom-design-review`. Preserve upstream's `IndustryViewerTemplate/` Unity project structure for clean upstream merges. Add features as new feature folders under `Assets/Features/` with their own `.asmdef`. Asset prep happens Cloud-side via Asset Manager's *Prepare for 3D Data Streaming* action — no local Asset Transformer required.

**Tech Stack:** Unity 6000.3.5f2 (Industry seat), URP 17.3, UI Toolkit + App UI, Unity Cloud Identity / Asset Manager / Data Streaming, Input System, NGO + Vivox (deferred to Phase C).

**Reference spec:** `docs/superpowers/specs/2026-04-28-alstom-design-review-viewer-design.md`

**Phase A scope (spec §3.1 rows 1–4):**
- Repo fork + branding + 3 build profiles + sample asset prep + deep-link namespace
- CATIA-like navigation + box-select with depth-selection
- View cube widget + predefined views + Fit-to-selection
- Render modes (shaded / wireframe-on-shaded / lines / X-ray)

---

## Prerequisites checklist

Before starting Task 1, verify:

- [ ] Unity Hub installed and signed in with the Industry-seat account
- [ ] Unity Editor `6000.3.5f2` installed (download via Unity Hub if missing)
- [ ] Git installed (`git --version`)
- [ ] GitHub account with SSH key configured (or HTTPS auth working)
- [ ] Unity Cloud organization with Asset Manager available; you are at least Project Contributor
- [ ] One sample CAD file (CATPart / CATProduct / STEP / JT) — public train/automotive model from GrabCAD is fine. **Required size:** small enough to fit Asset Manager free-tier limits during dev (under ~500 MB). Anchor file: pick *one* model and stick with it for all of Phase A.
- [ ] HTC Vive Focus Vision **not** required during Phase A (deferred to Phase C); ADB setup deferred too

---

## File structure

By the end of Phase A, the repo will look like:

```
/Users/alaaelboudali/Documents/SideProj/alstom-design-review/
├── .git/
├── .gitignore                                # Unity standard + .DS_Store + Library/
├── README.md                                 # project overview + quickstart
├── docs/
│   └── superpowers/
│       ├── specs/2026-04-28-alstom-design-review-viewer-design.md  (existing)
│       └── plans/2026-04-28-alstom-design-review-phase-a-foundation.md (this file)
├── IndustryViewerTemplate/                   # Unity project, matches upstream
│   ├── Assets/
│   │   ├── Features/
│   │   │   ├── (template features — untouched)
│   │   │   ├── Alstom.Branding/              # NEW
│   │   │   │   ├── Alstom.Branding.asmdef
│   │   │   │   ├── UI/
│   │   │   │   │   ├── AlstomLogo.png
│   │   │   │   │   ├── AlstomSplash.png
│   │   │   │   │   └── AlstomTheme.tss
│   │   │   │   └── Scripts/
│   │   │   │       └── AlstomBrandingApplier.cs
│   │   │   ├── Pixyz.Render/                 # NEW
│   │   │   │   ├── Pixyz.Render.asmdef
│   │   │   │   ├── Tests/
│   │   │   │   │   └── Pixyz.Render.Tests.asmdef
│   │   │   │   ├── Scripts/
│   │   │   │   │   ├── RenderModeController.cs
│   │   │   │   │   ├── RenderMode.cs
│   │   │   │   │   ├── WireframeOnShadedFeature.cs   # URP renderer feature
│   │   │   │   │   ├── XRayFeature.cs                # URP renderer feature
│   │   │   │   │   └── LinesFeature.cs               # URP renderer feature
│   │   │   │   └── UI/
│   │   │   │       └── RenderModeDropdown.uxml
│   │   │   ├── Pixyz.ViewCube/               # NEW
│   │   │   │   ├── Pixyz.ViewCube.asmdef
│   │   │   │   ├── Tests/
│   │   │   │   │   └── Pixyz.ViewCube.Tests.asmdef
│   │   │   │   ├── Scripts/
│   │   │   │   │   ├── ViewCubeController.cs
│   │   │   │   │   ├── PredefinedViews.cs
│   │   │   │   │   └── FitToSelection.cs
│   │   │   │   └── UI/
│   │   │   │       ├── ViewCube.uxml
│   │   │   │       └── ViewCube.uss
│   │   │   └── Pixyz.Selection/              # NEW (box-select with depth)
│   │   │       ├── Pixyz.Selection.asmdef
│   │   │       ├── Tests/
│   │   │       │   └── Pixyz.Selection.Tests.asmdef
│   │   │       └── Scripts/
│   │   │           ├── BoxSelectController.cs
│   │   │           ├── DepthSelector.cs           # raycasts through occluders
│   │   │           └── CatiaLikeNavigation.cs
│   │   └── _AlstomSamples/                   # NEW
│   │       └── README.md                     # references Asset Manager dataset ID
│   ├── Packages/
│   │   └── manifest.json                     # MODIFIED (no new packages in Phase A)
│   └── ProjectSettings/
│       ├── ProjectVersion.txt                # untouched
│       └── (modified during Task 4 + 5)
└── upstream-tracking.md                      # how to merge upstream fixes
```

---

## Tasks

### Task 1: Fork the template repo and clone locally

**Files:**
- Create: `/Users/alaaelboudali/Documents/SideProj/alstom-design-review/` (full repo clone)

- [ ] **Step 1: Fork the upstream repo on GitHub**

Open https://github.com/Unity-Technologies/unity-industry-viewer-template in a browser. Click **Fork** → set owner to your account, name to `alstom-design-review`, set Description: *"Alstom design review viewer prototype, built on Unity Industry Viewer Template"*. Uncheck "Copy the main branch only" (we want all branches in case we need a different release tag).

- [ ] **Step 2: Clone the fork into the existing project dir**

```bash
cd /Users/alaaelboudali/Documents/SideProj/alstom-design-review

# The dir already contains docs/. Move them aside, clone, restore.
mv docs ../docs-tmp
git clone git@github.com:<your-username>/alstom-design-review.git .
mv ../docs-tmp/* docs/ 2>/dev/null || mkdir -p docs && mv ../docs-tmp/* docs/
rmdir ../docs-tmp
```

Expected: `git status` shows `docs/` as untracked.

- [ ] **Step 3: Add upstream remote**

```bash
git remote add upstream https://github.com/Unity-Technologies/unity-industry-viewer-template.git
git remote -v
```

Expected output includes both `origin` (your fork) and `upstream` (Unity's repo).

- [ ] **Step 4: Pin to known-good template release**

```bash
git fetch upstream
git checkout -b main upstream/main
git log --oneline -5
```

Verify the latest commit matches the spec's reference version (template 2.2.1). If a newer version is out, note it in `upstream-tracking.md` (created in Step 6) but stay on 2.2.1 for Phase A — upgrades happen between phases.

- [ ] **Step 5: Verify Unity opens the project cleanly**

Open Unity Hub → Add → Browse to `/Users/alaaelboudali/Documents/SideProj/alstom-design-review/IndustryViewerTemplate` → select Unity 6000.3.5f2 → Open. Wait for the import to finish (~5–15 minutes first time).

Expected: Editor opens with no compilation errors. Open scene `Assets/Scenes/Main.unity` — you should see the template's login UI.

If errors appear, check:
- All required packages in `Packages/manifest.json` resolved (Window → Package Manager)
- Scripting backend matches platform (Project Settings → Player → Configuration)

- [ ] **Step 6: Create upstream tracking doc and commit docs**

Create `/Users/alaaelboudali/Documents/SideProj/alstom-design-review/upstream-tracking.md`:

```markdown
# Upstream tracking

We track `Unity-Technologies/unity-industry-viewer-template` as the `upstream` git remote.

## Current pin

- Upstream version: 2.2.1
- Pinned commit: <fill in with `git rev-parse upstream/main`>
- Pin date: 2026-04-28

## Merge cadence

Merge upstream between phases (A → B, B → C). Resolve conflicts in feature folders we own (`Features/Alstom.*`, `Features/Pixyz.*`); accept upstream for everything else unless deliberately overridden.

## Merge command

```bash
git fetch upstream
git merge upstream/main
# Resolve conflicts; prefer upstream for template features.
```
```

- [ ] **Step 7: Commit**

```bash
cd /Users/alaaelboudali/Documents/SideProj/alstom-design-review
git add docs/ upstream-tracking.md
git commit -m "docs: add Phase A spec, plan, and upstream tracking doc"
git push origin main
```

Expected: push succeeds.

---

### Task 2: Create the Alstom.Branding feature folder structure

**Files:**
- Create: `IndustryViewerTemplate/Assets/Features/Alstom.Branding/Alstom.Branding.asmdef`
- Create: `IndustryViewerTemplate/Assets/Features/Alstom.Branding/UI/.gitkeep`
- Create: `IndustryViewerTemplate/Assets/Features/Alstom.Branding/Scripts/.gitkeep`

- [ ] **Step 1: Create the folder structure**

In Unity Editor, in the Project window, right-click `Assets/Features` → Create → Folder → name it `Alstom.Branding`. Inside it, create subfolders `UI` and `Scripts`.

- [ ] **Step 2: Create the assembly definition**

In `Assets/Features/Alstom.Branding/`, right-click → Create → Assembly Definition → name it `Alstom.Branding`. Open it in the Inspector.

Set:
- **Name:** `Alstom.Branding`
- **Assembly Definition References:** add (click +): `Unity.AppUI`, `Unity.AppUI.UI`, `Unity.Industry.Viewer.Identity`, `Unity.Industry.Viewer.Shared`
- **Auto Referenced:** unchecked
- **No Engine References:** unchecked

Click Apply.

- [ ] **Step 3: Verify the asmdef compiles**

Save the project (`Cmd+S`). The Editor reimports. Check the Console — no errors.

- [ ] **Step 4: Commit**

```bash
cd /Users/alaaelboudali/Documents/SideProj/alstom-design-review
git add IndustryViewerTemplate/Assets/Features/Alstom.Branding/
git commit -m "feat(branding): scaffold Alstom.Branding feature folder"
```

---

### Task 3: Apply Alstom branding (logo, splash, app name, theme color)

**Files:**
- Create: `IndustryViewerTemplate/Assets/Features/Alstom.Branding/UI/AlstomLogo.png` (256×64 PNG, transparent background)
- Create: `IndustryViewerTemplate/Assets/Features/Alstom.Branding/UI/AlstomSplash.png` (1920×1080 PNG)
- Modify: `IndustryViewerTemplate/Assets/UI/Background.uxml` — splash background
- Modify: `IndustryViewerTemplate/Assets/Features/Identity/UI/Identity.uxml` — logo
- Modify: `IndustryViewerTemplate/ProjectSettings/ProjectSettings.asset` — Product Name + Splash Screen

- [ ] **Step 1: Source Alstom-style logo and splash assets**

For prototype use, the official Alstom corporate logo is a registered trademark — use a **non-trademarked placeholder** (e.g. text "ALSTOM DESIGN REVIEW" set in Helvetica Bold, with an abstract industrial graphic). Save as `AlstomLogo.png` (256×64, transparent bg) and `AlstomSplash.png` (1920×1080).

Drop both files into `Assets/Features/Alstom.Branding/UI/`.

In the Project window, click each PNG → Inspector → Texture Type: `Sprite (2D and UI)` → Apply.

- [ ] **Step 2: Set Product Name and Company Name**

Project Settings → Player:
- **Product Name:** `Alstom Design Review`
- **Company Name:** `Alstom Design Review Prototype`

Save (`Cmd+S`).

- [ ] **Step 3: Set the splash screen**

Project Settings → Player → Splash Image:
- Uncheck **Show Unity Logo** (Industry seat permits this)
- Drag `AlstomSplash.png` into the **Background** slot under the Logo list (or use Background Image directly)
- Set Background Color to `#1A1A1A` (Alstom-style dark grey)

- [ ] **Step 4: Replace the login screen logo**

Open `Assets/Features/Identity/UI/Identity.uxml` in the UI Toolkit Builder (double-click). Find the `<Image>` element with class `unity-image` near the top — that's the Unity Industry Viewer Template logo placeholder.

Replace its `src` attribute to point to your sprite:
```xml
<ui:Image src="project://database/Assets/Features/Alstom.Branding/UI/AlstomLogo.png" />
```

Save the UXML file.

- [ ] **Step 5: Replace background**

Open `Assets/UI/Background.uxml`. Find the root `<VisualElement>` with the background-image inline style. Update:
```xml
<ui:VisualElement style="background-image: url(&apos;project://database/Assets/Features/Alstom.Branding/UI/AlstomSplash.png&apos;); flex-grow: 1;" />
```

Save.

- [ ] **Step 6: Verify branding by entering Play Mode**

Open `Assets/Scenes/Main.unity` → press Play. The login UI should show the Alstom logo and splash background instead of Unity Industry's defaults.

Take a screenshot and save to `docs/screenshots/branding-applied.png` for the Phase A demo log.

- [ ] **Step 7: Commit**

```bash
git add IndustryViewerTemplate/Assets/Features/Alstom.Branding/UI/AlstomLogo.png \
        IndustryViewerTemplate/Assets/Features/Alstom.Branding/UI/AlstomSplash.png \
        IndustryViewerTemplate/Assets/UI/Background.uxml \
        IndustryViewerTemplate/Assets/Features/Identity/UI/Identity.uxml \
        IndustryViewerTemplate/ProjectSettings/ProjectSettings.asset \
        docs/screenshots/branding-applied.png
git commit -m "feat(branding): apply Alstom logo, splash, and product name"
```

---

### Task 4: Change the deep-link auth namespace

**Files:**
- Modify: `IndustryViewerTemplate/ProjectSettings/ProjectSettings.asset` — Custom URL Scheme

The template warns explicitly: the default `com.unity.industry-viewer` namespace will collide with installs of the upstream template on the same machine. Required change before any build.

- [ ] **Step 1: Update Custom URL Scheme**

Project Settings → Player → Other Settings → Configuration → look for **Custom URL Scheme** entries (under Application Identifier). The template uses `com.unity.industry-viewer` — change to `com.alstom.design-review`.

Also update **Bundle Identifier** (Mac/iOS), **Package Name** (Android), and **Application ID** to `com.alstom.design-review`.

- [ ] **Step 2: Search the codebase for hard-coded `com.unity.industry-viewer` strings**

```bash
cd /Users/alaaelboudali/Documents/SideProj/alstom-design-review/IndustryViewerTemplate
grep -r "com.unity.industry-viewer" Assets/ --include="*.cs" --include="*.json" --include="*.asmdef"
```

Expected output: 0 results (the template stores this only in ProjectSettings). If any results appear, replace them inline with `com.alstom.design-review`.

- [ ] **Step 3: Verify deep linking initialization doesn't error**

Enter Play Mode in `Main.unity`. Check the Console for any deep-linking warnings. Expected: clean.

- [ ] **Step 4: Commit**

```bash
git add IndustryViewerTemplate/ProjectSettings/ProjectSettings.asset
git commit -m "feat(auth): change deep-link namespace to com.alstom.design-review"
```

---

### Task 5: Configure three build profiles

**Files:**
- Create: `IndustryViewerTemplate/Assets/Settings/BuildProfiles/Desktop-macOS.asset`
- Create: `IndustryViewerTemplate/Assets/Settings/BuildProfiles/Desktop-Win64.asset`
- Create: `IndustryViewerTemplate/Assets/Settings/BuildProfiles/Standalone-AndroidXR.asset`

Unity 6 has Build Profiles (File → Build Profiles), an evolution of the older Build Settings.

- [ ] **Step 1: Create the macOS Apple Silicon profile**

File → Build Profiles → click **Add Build Profile** → **macOS** → Create.

In the profile inspector:
- **Name:** `Desktop-macOS`
- **Architecture:** Apple Silicon
- **Target Platform:** macOS
- **Scenes In Build:** drag in `Assets/Scenes/Main.unity` and `Assets/Scenes/Streaming.unity` (in this order)

Save the profile asset to `Assets/Settings/BuildProfiles/Desktop-macOS.asset`.

- [ ] **Step 2: Create the Windows x64 profile**

File → Build Profiles → Add Build Profile → **Windows** → Create.
- **Name:** `Desktop-Win64`
- **Architecture:** x64
- **Scenes In Build:** Main, Streaming

Save to `Assets/Settings/BuildProfiles/Desktop-Win64.asset`.

- [ ] **Step 3: Create the Android XR profile (Vive Focus Vision target)**

First ensure the Android module is installed for Unity 6000.3.5f2 via Unity Hub → Installs → ⋯ → Add Modules.

File → Build Profiles → Add Build Profile → **Android** → Create.
- **Name:** `Standalone-AndroidXR`
- **Texture Compression:** ASTC
- **Target Architectures:** ARM64 only (uncheck ARMv7)
- **Scenes In Build:** `Main VR.unity`, `Streaming VR.unity` (the template's VR scene variants)

Open Project Settings → XR Plug-in Management → Android tab → enable **OpenXR** + **Android XR** providers.

Under Project Settings → XR Plug-in Management → OpenXR (Android tab):
- **Render Mode:** Single Pass Instanced
- **Interaction Profiles:** add **Khr Simple Controller Profile** (will refine for Vive controllers in Phase C)

Save profile to `Assets/Settings/BuildProfiles/Standalone-AndroidXR.asset`.

- [ ] **Step 4: Build and run the macOS profile to verify it works**

File → Build Profiles → select `Desktop-macOS` → click **Build And Run**. Save the .app bundle to `Builds/Desktop-macOS/AlstomDesignReview.app`.

Expected: the build completes (5–15 minutes first time), the .app launches, login UI appears with Alstom branding.

If macOS Gatekeeper blocks the app: System Settings → Privacy & Security → "Open Anyway" for the .app. (Code-signing is out of v1 scope — documented in spec §8.4.)

- [ ] **Step 5: Build the Windows profile (skip running for now)**

File → Build Profiles → select `Desktop-Win64` → **Build** (not Build And Run). Save to `Builds/Desktop-Win64/AlstomDesignReview.exe`. Verify the build completes; running on Windows is deferred until you have access to a Windows machine.

- [ ] **Step 6: Build the Android XR profile (skip running until headset available)**

File → Build Profiles → select `Standalone-AndroidXR` → Build. Save to `Builds/Standalone-AndroidXR/AlstomDesignReview.apk`. Verify the build completes — running on Vive Focus Vision is Phase C.

- [ ] **Step 7: Add Builds/ to .gitignore**

Edit `/Users/alaaelboudali/Documents/SideProj/alstom-design-review/.gitignore` (the template provides a base; we extend):

```
# Build artifacts
Builds/
*.app/
*.exe
*.apk
*.aab

# Unity caches
[Ll]ibrary/
[Tt]emp/
[Oo]bj/
[Bb]uild/
[Bb]uilds/
[Ll]ogs/
[Uu]ser[Ss]ettings/
```

- [ ] **Step 8: Commit**

```bash
git add IndustryViewerTemplate/Assets/Settings/BuildProfiles/ \
        IndustryViewerTemplate/ProjectSettings/ \
        .gitignore
git commit -m "feat(build): add Desktop-macOS, Desktop-Win64, Standalone-AndroidXR profiles"
```

---

### Task 6: Prepare and load a sample CAD asset via Asset Manager

**Files:** No code changes; this is a Cloud-side workflow.

- [ ] **Step 1: Create the Asset Manager organization and project**

Open https://cloud.unity.com → sign in with the Industry-seat account → navigate to **Asset Manager**. Create an organization if one doesn't exist (`alstom-prototype-org`). Inside it, create a project: `alstom-design-review-prototype`.

Note the **organization ID** and **project ID** from the URL — you'll paste these into the viewer's auth flow.

- [ ] **Step 2: Upload the sample CAD file**

Asset Manager web UI → Assets → **Upload** → select your sample CAD file (CATPart/STEP/JT). Wait for upload to complete (size-dependent; ~1–10 minutes).

The asset appears as a **Source dataset**.

- [ ] **Step 3: Trigger Cloud-side prep**

Click the asset → in the asset detail page, find the **Datasets** section → on the Source dataset, click **⋯** → **Prepare for 3D Data Streaming**.

Expected: a new **Streamable** dataset appears in *Processing* state. Transform takes 5–30 minutes depending on asset size and Cloud queue.

When done, the Streamable dataset shows status *Ready*. Note its **dataset ID**.

- [ ] **Step 4: Verify the asset loads in the viewer**

Build and run the macOS profile (or just enter Play Mode in `Main.unity`).

- Login with your Industry-seat account
- Pick the `alstom-prototype-org` organization
- Pick the `alstom-design-review-prototype` project
- The asset should appear in the asset browser
- Click it → it loads into the Streaming scene

Expected: hierarchy panel populated, mesh visible, you can orbit the camera.

If the asset doesn't appear, check:
- Asset Manager role — must be Project Contributor or higher
- Asset has a Streamable dataset (not just Source)
- Network — viewer needs internet for streaming

- [ ] **Step 5: Document the asset ID and a sample query**

Create `IndustryViewerTemplate/Assets/_AlstomSamples/README.md`:

```markdown
# Alstom Sample Assets

This folder is a marker; the actual asset data lives in Asset Manager.

## Phase A demo asset

- **Name:** <fill in>
- **Source format:** <CATPart / STEP / JT>
- **Asset Manager organization:** alstom-prototype-org
- **Asset Manager project:** alstom-design-review-prototype
- **Streamable dataset ID:** <fill in>
- **Triangle count (post-prep):** <fill in from streaming HUD>
- **Notes:** <any observations from the demo>
```

- [ ] **Step 6: Commit**

```bash
git add IndustryViewerTemplate/Assets/_AlstomSamples/README.md
git commit -m "docs(samples): record Phase A demo asset metadata"
```

**Verification milestone:** at the end of Task 6, you have *Hello Alstom* working — branded build, login, asset browse, asset loads. Take a screenshot and save to `docs/screenshots/hello-alstom.png`. Tasks 7–11 add the Pixyz-Review-style navigation polish.

---

### Task 7: Implement CATIA-like navigation (mouse-only mode)

The template ships ALT+mouse Standard navigation. Pixyz Review reviewers expect CATIA-like (no modifier required for rotate). We add a settings toggle and a new navigation mode.

**Files:**
- Create: `IndustryViewerTemplate/Assets/Features/Pixyz.Selection/Pixyz.Selection.asmdef`
- Create: `IndustryViewerTemplate/Assets/Features/Pixyz.Selection/Scripts/CatiaLikeNavigation.cs`
- Create: `IndustryViewerTemplate/Assets/Features/Pixyz.Selection/Tests/Pixyz.Selection.Tests.asmdef`
- Create: `IndustryViewerTemplate/Assets/Features/Pixyz.Selection/Tests/CatiaLikeNavigationTests.cs`
- Modify: `IndustryViewerTemplate/Assets/Features/Streaming/Scripts/Navigation/StandardCameraControl/InteractionController.cs` — add mode hook

- [ ] **Step 1: Scaffold the Pixyz.Selection feature folder**

In Unity Editor: right-click `Assets/Features` → Create → Folder → `Pixyz.Selection`. Inside, create `Scripts` and `Tests` subfolders. Create the asmdef:

`Assets/Features/Pixyz.Selection/Pixyz.Selection.asmdef`:
```json
{
    "name": "Pixyz.Selection",
    "rootNamespace": "Pixyz.Review.Selection",
    "references": [
        "Unity.InputSystem",
        "Unity.Industry.Viewer.Streaming",
        "Unity.Industry.Viewer.Shared",
        "Unity.AppUI",
        "Unity.AppUI.UI",
        "UnityEngine.UI"
    ],
    "autoReferenced": false,
    "noEngineReferences": false
}
```

`Assets/Features/Pixyz.Selection/Tests/Pixyz.Selection.Tests.asmdef`:
```json
{
    "name": "Pixyz.Selection.Tests",
    "rootNamespace": "Pixyz.Review.Selection.Tests",
    "references": [
        "Pixyz.Selection",
        "UnityEngine.TestRunner",
        "UnityEditor.TestRunner"
    ],
    "optionalUnityReferences": ["TestAssemblies"],
    "autoReferenced": false,
    "noEngineReferences": false,
    "defineConstraints": ["UNITY_INCLUDE_TESTS"]
}
```

- [ ] **Step 2: Write the failing test for CatiaLikeNavigation input mapping**

Create `Assets/Features/Pixyz.Selection/Tests/CatiaLikeNavigationTests.cs`:

```csharp
using NUnit.Framework;
using UnityEngine;
using Pixyz.Review.Selection;

namespace Pixyz.Review.Selection.Tests
{
    public class CatiaLikeNavigationTests
    {
        [Test]
        public void MapsMiddleAndLeftToRotate()
        {
            var input = new NavigationInput
            {
                MiddleButtonHeld = true,
                LeftButtonHeld = true,
                MouseDelta = new Vector2(10, 0)
            };
            var action = CatiaLikeNavigation.Resolve(input);
            Assert.AreEqual(NavigationAction.Rotate, action.Kind);
            Assert.AreEqual(new Vector2(10, 0), action.Delta);
        }

        [Test]
        public void MapsMiddleAloneToPan()
        {
            // Pixyz's "Center Click + Middle Click: Pan" reads as ambiguous in the doc
            // (same physical button labeled twice). We interpret it as: middle button
            // held with no other button = pan, which matches CATIA convention.
            var input = new NavigationInput
            {
                MiddleButtonHeld = true,
                LeftButtonHeld = false,
                RightButtonHeld = false,
                MouseDelta = new Vector2(5, 5)
            };
            var action = CatiaLikeNavigation.Resolve(input);
            Assert.AreEqual(NavigationAction.Pan, action.Kind);
        }

        [Test]
        public void MapsMiddleAndRightToZoom()
        {
            var input = new NavigationInput
            {
                MiddleButtonHeld = true,
                RightButtonHeld = true,
                MouseDelta = new Vector2(0, 10)
            };
            var action = CatiaLikeNavigation.Resolve(input);
            Assert.AreEqual(NavigationAction.Zoom, action.Kind);
        }

        [Test]
        public void LeftClickAloneIsSelect()
        {
            var input = new NavigationInput
            {
                LeftButtonClicked = true
            };
            var action = CatiaLikeNavigation.Resolve(input);
            Assert.AreEqual(NavigationAction.Select, action.Kind);
        }
    }
}
```

- [ ] **Step 3: Run the test — verify it fails**

In Unity: Window → General → Test Runner → switch to **EditMode** → Run All. Expected: 4 tests fail with "type or namespace `CatiaLikeNavigation` not found".

- [ ] **Step 4: Write the minimal implementation**

Create `Assets/Features/Pixyz.Selection/Scripts/CatiaLikeNavigation.cs`:

```csharp
using UnityEngine;

namespace Pixyz.Review.Selection
{
    public struct NavigationInput
    {
        public bool LeftButtonHeld;
        public bool RightButtonHeld;
        public bool MiddleButtonHeld;
        public bool LeftButtonClicked;
        public Vector2 MouseDelta;
    }

    public enum NavigationAction
    {
        None,
        Select,
        Rotate,
        Pan,
        Zoom
    }

    public struct NavigationResult
    {
        public NavigationAction Kind;
        public Vector2 Delta;
    }

    /// <summary>
    /// CATIA-like navigation: no modifier keys required.
    /// Mirrors Pixyz Review's default nav (per Pixyz Getting Started 2018.2 page 11).
    /// </summary>
    public static class CatiaLikeNavigation
    {
        public static NavigationResult Resolve(NavigationInput input)
        {
            if (input.LeftButtonClicked)
            {
                return new NavigationResult { Kind = NavigationAction.Select };
            }

            if (input.MiddleButtonHeld && input.LeftButtonHeld)
            {
                return new NavigationResult { Kind = NavigationAction.Rotate, Delta = input.MouseDelta };
            }

            if (input.MiddleButtonHeld && input.RightButtonHeld)
            {
                return new NavigationResult { Kind = NavigationAction.Zoom, Delta = input.MouseDelta };
            }

            // Middle alone (no other button held) = pan.
            // See test MapsMiddleAloneToPan for rationale.
            if (input.MiddleButtonHeld && !input.LeftButtonHeld && !input.RightButtonHeld)
            {
                return new NavigationResult { Kind = NavigationAction.Pan, Delta = input.MouseDelta };
            }

            return new NavigationResult { Kind = NavigationAction.None };
        }
    }
}
```

- [ ] **Step 5: Re-run the tests — verify they pass**

Test Runner → Run All. Expected: 4 tests pass.

- [ ] **Step 6: Hook the resolver into the runtime InteractionController**

Open `Assets/Features/Streaming/Scripts/Navigation/StandardCameraControl/InteractionController.cs`. Locate the `Update()` method that reads mouse input and applies camera deltas.

Add a settings field:

```csharp
[SerializeField] private bool useCatiaLikeNavigation = true;
```

Add a using:
```csharp
using Pixyz.Review.Selection;
```

Inside `Update()`, before the existing input branching, gather a `NavigationInput` from the current frame's mouse state and route through `CatiaLikeNavigation.Resolve(...)` when `useCatiaLikeNavigation` is true. Use the existing rotate/pan/zoom methods for the action handlers — do not duplicate camera math here.

First, **read the existing `InteractionController.cs`** to identify the template's actual rotate / pan / zoom / select internal methods or events. The template typically uses Cinemachine or a custom rig; the methods may be named `RotateAroundPivot`, `PanCamera`, `Zoom`, etc., or may dispatch via events. The exact names will be visible in the file.

Then route based on what you found:

```csharp
if (useCatiaLikeNavigation)
{
    var navInput = new NavigationInput
    {
        LeftButtonHeld = Mouse.current.leftButton.isPressed,
        RightButtonHeld = Mouse.current.rightButton.isPressed,
        MiddleButtonHeld = Mouse.current.middleButton.isPressed,
        LeftButtonClicked = Mouse.current.leftButton.wasPressedThisFrame,
        MouseDelta = Mouse.current.delta.ReadValue()
    };
    var result = CatiaLikeNavigation.Resolve(navInput);
    switch (result.Kind)
    {
        case NavigationAction.Rotate: /* call template's rotate-by-delta */ return;
        case NavigationAction.Pan:    /* call template's pan-by-delta */ return;
        case NavigationAction.Zoom:   /* call template's zoom-by-delta */ return;
        case NavigationAction.Select: /* call template's select-at-cursor */ return;
    }
}
// fall through to existing ALT+mouse Standard navigation as fallback
```

If the template's rotate/pan/zoom logic is private, expose it via a `private` → `internal` change and document the modification in `upstream-tracking.md`.

Add asmdef references in `Unity.Industry.Viewer.Streaming.asmdef` to include `Pixyz.Selection` (or invert the dependency by moving the resolver to a shared namespace — your call; the simpler path is referencing `Pixyz.Selection`).

- [ ] **Step 7: Manual verification in Play Mode**

Enter Play Mode on `Streaming.unity` with a loaded asset.
- Left-click on a part → it selects (highlight visible)
- Hold middle + left, drag → camera rotates
- Hold middle + right, drag → camera zooms
- Hold middle + middle, drag → camera pans

Take a screenshot for the demo log.

- [ ] **Step 8: Add a Settings entry to toggle modes**

In `InteractionController.cs`, expose the `useCatiaLikeNavigation` field through the existing template Preferences UI (`Features/Shared/...InAppSettings...`). Add a labeled checkbox: *"CATIA-like navigation (default)"* → unchecked falls back to Standard ALT+mouse.

- [ ] **Step 9: Commit**

```bash
git add IndustryViewerTemplate/Assets/Features/Pixyz.Selection/ \
        IndustryViewerTemplate/Assets/Features/Streaming/Scripts/Navigation/
git commit -m "feat(nav): add CATIA-like mouse navigation as default mode"
```

---

### Task 8: Implement box-select with depth-selection

Pixyz's box-select: left-drag = select visible parts inside box; right-drag = select all parts inside box including hidden behind front geometry.

**Files:**
- Create: `IndustryViewerTemplate/Assets/Features/Pixyz.Selection/Scripts/BoxSelectController.cs`
- Create: `IndustryViewerTemplate/Assets/Features/Pixyz.Selection/Scripts/DepthSelector.cs`
- Create: `IndustryViewerTemplate/Assets/Features/Pixyz.Selection/Tests/DepthSelectorTests.cs`

- [ ] **Step 1: Write the failing test for visibility-aware selection**

Create `Assets/Features/Pixyz.Selection/Tests/DepthSelectorTests.cs`:

```csharp
using NUnit.Framework;
using UnityEngine;
using Pixyz.Review.Selection;
using System.Collections.Generic;

namespace Pixyz.Review.Selection.Tests
{
    public class DepthSelectorTests
    {
        [Test]
        public void VisibleOnlyExcludesPartsBehindOccluders()
        {
            var camera = new GameObject("cam").AddComponent<Camera>();
            camera.transform.position = Vector3.zero;
            camera.transform.LookAt(Vector3.forward);

            var front = MakePart("front", new Vector3(0, 0, 5));
            var behind = MakePart("behind", new Vector3(0, 0, 10)); // occluded by front

            var box = new Rect(0, 0, Screen.width, Screen.height);
            var result = DepthSelector.SelectVisible(box, camera, new[] { front, behind });

            Assert.Contains(front, result);
            Assert.IsFalse(result.Contains(behind));

            Object.DestroyImmediate(camera.gameObject);
            Object.DestroyImmediate(front);
            Object.DestroyImmediate(behind);
        }

        [Test]
        public void DepthSelectIncludesPartsBehindOccluders()
        {
            var camera = new GameObject("cam").AddComponent<Camera>();
            camera.transform.position = Vector3.zero;
            camera.transform.LookAt(Vector3.forward);

            var front = MakePart("front", new Vector3(0, 0, 5));
            var behind = MakePart("behind", new Vector3(0, 0, 10));

            var box = new Rect(0, 0, Screen.width, Screen.height);
            var result = DepthSelector.SelectAll(box, camera, new[] { front, behind });

            Assert.Contains(front, result);
            Assert.Contains(behind, result);

            Object.DestroyImmediate(camera.gameObject);
            Object.DestroyImmediate(front);
            Object.DestroyImmediate(behind);
        }

        private GameObject MakePart(string name, Vector3 position)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.position = position;
            return go;
        }
    }
}
```

- [ ] **Step 2: Run the test — verify failure**

Test Runner → EditMode → Run All. Expected: 2 tests fail with "DepthSelector not found".

- [ ] **Step 3: Implement DepthSelector**

Create `Assets/Features/Pixyz.Selection/Scripts/DepthSelector.cs`:

```csharp
using System.Collections.Generic;
using UnityEngine;

namespace Pixyz.Review.Selection
{
    /// <summary>
    /// Box-select with two modes (Pixyz Getting Started 2018.2 p.11):
    ///   - SelectVisible: only parts whose AABB centroid is unoccluded from the camera
    ///   - SelectAll: all parts whose AABB intersects the box (depth-selection)
    /// </summary>
    public static class DepthSelector
    {
        public static List<GameObject> SelectVisible(Rect screenBox, Camera camera, IEnumerable<GameObject> candidates)
        {
            var hits = new List<GameObject>();
            foreach (var go in candidates)
            {
                if (!IsAabbCentroidInBox(go, camera, screenBox)) continue;
                if (IsOccluded(go, camera)) continue;
                hits.Add(go);
            }
            return hits;
        }

        public static List<GameObject> SelectAll(Rect screenBox, Camera camera, IEnumerable<GameObject> candidates)
        {
            var hits = new List<GameObject>();
            foreach (var go in candidates)
            {
                if (!IsAabbCentroidInBox(go, camera, screenBox)) continue;
                hits.Add(go);
            }
            return hits;
        }

        private static bool IsAabbCentroidInBox(GameObject go, Camera camera, Rect box)
        {
            var renderer = go.GetComponentInChildren<Renderer>();
            if (renderer == null) return false;
            var screen = camera.WorldToScreenPoint(renderer.bounds.center);
            if (screen.z < 0) return false; // behind camera
            return box.Contains(new Vector2(screen.x, screen.y));
        }

        private static bool IsOccluded(GameObject go, Camera camera)
        {
            var renderer = go.GetComponentInChildren<Renderer>();
            if (renderer == null) return true;
            var origin = camera.transform.position;
            var target = renderer.bounds.center;
            var direction = target - origin;
            var distance = direction.magnitude;
            if (Physics.Raycast(origin, direction.normalized, out var hit, distance))
            {
                // Allow self-hits (the part itself is what the ray hit)
                return !hit.collider.transform.IsChildOf(go.transform) && hit.collider.transform != go.transform;
            }
            return false;
        }
    }
}
```

- [ ] **Step 4: Run the tests — verify they pass**

Test Runner → Run All. Expected: 2 tests pass.

The test uses `GameObject.CreatePrimitive(PrimitiveType.Cube)` which auto-adds a BoxCollider — required for Physics.Raycast to detect occlusion. Real streamed parts may not have colliders by default; you'll handle that in Step 6.

- [ ] **Step 5: Build the BoxSelectController to drive selection from drag input**

Create `Assets/Features/Pixyz.Selection/Scripts/BoxSelectController.cs`:

```csharp
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Pixyz.Review.Selection
{
    /// <summary>
    /// Tracks left/right-mouse drag and emits a box-select operation on release.
    /// Left-drag = visible-only; right-drag = depth-select.
    /// </summary>
    public class BoxSelectController : MonoBehaviour
    {
        [SerializeField] private Camera viewerCamera;
        [SerializeField] private float minDragPixels = 6f;

        public System.Action<List<GameObject>> OnSelectionChanged;

        private Vector2? dragStart;
        private bool dragRightButton;

        private void Update()
        {
            if (Mouse.current == null) return;

            if (Mouse.current.leftButton.wasPressedThisFrame)  { dragStart = Mouse.current.position.ReadValue(); dragRightButton = false; }
            if (Mouse.current.rightButton.wasPressedThisFrame) { dragStart = Mouse.current.position.ReadValue(); dragRightButton = true; }

            bool released =
                Mouse.current.leftButton.wasReleasedThisFrame ||
                Mouse.current.rightButton.wasReleasedThisFrame;

            if (dragStart.HasValue && released)
            {
                var end = Mouse.current.position.ReadValue();
                var box = MakeRect(dragStart.Value, end);
                if (box.width >= minDragPixels && box.height >= minDragPixels)
                {
                    var candidates = FindStreamedParts();
                    var hits = dragRightButton
                        ? DepthSelector.SelectAll(box, viewerCamera, candidates)
                        : DepthSelector.SelectVisible(box, viewerCamera, candidates);
                    OnSelectionChanged?.Invoke(hits);
                }
                dragStart = null;
            }
        }

        private static Rect MakeRect(Vector2 a, Vector2 b)
        {
            var min = Vector2.Min(a, b);
            var max = Vector2.Max(a, b);
            return new Rect(min, max - min);
        }

        private IEnumerable<GameObject> FindStreamedParts()
        {
            // Phase A: enumerate active MeshRenderers under the streaming root.
            // Refined in Phase B once the streamed hierarchy resolver lands.
            var streamingRoot = GameObject.Find("StreamingRoot");
            if (streamingRoot == null) yield break;
            foreach (var renderer in streamingRoot.GetComponentsInChildren<MeshRenderer>())
            {
                yield return renderer.gameObject;
            }
        }
    }
}
```

- [ ] **Step 6: Add colliders to streamed parts**

Streamed meshes have no colliders by default — Physics.Raycast won't detect them. Add a runtime hook: when a streamed part appears in scene, attach a `MeshCollider` (convex=false) sized to its mesh.

In `BoxSelectController.cs`, add an Awake that walks the streaming root and adds colliders, OR (cleaner) hook into the template's `StreamingModelController` callback for "part loaded" and add colliders there. The template's `Features/Streaming/Scripts/StreamingModelController.cs` has a `OnPartLoaded` event — subscribe to it:

```csharp
private void OnEnable()
{
    StreamingModelController.OnPartLoaded += AddColliderIfMissing;
}

private void OnDisable()
{
    StreamingModelController.OnPartLoaded -= AddColliderIfMissing;
}

private void AddColliderIfMissing(GameObject part)
{
    if (part.GetComponent<Collider>() != null) return;
    var meshFilter = part.GetComponent<MeshFilter>();
    if (meshFilter == null || meshFilter.sharedMesh == null) return;
    var collider = part.AddComponent<MeshCollider>();
    collider.sharedMesh = meshFilter.sharedMesh;
    collider.convex = false; // accurate raycast against tessellated mesh
}
```

If `StreamingModelController` doesn't expose `OnPartLoaded`, add it in a small modification (template has `[SerializeField] events`); document the change in `upstream-tracking.md` so it's flagged at next merge.

- [ ] **Step 7: Wire BoxSelectController into the Streaming scene**

Open `Streaming.unity`. Find the `Stream Tool Controller` GameObject. Add a child GameObject named `Box Select Controller`. Attach `BoxSelectController` component. Drag the active Camera into the `Viewer Camera` field.

Connect `OnSelectionChanged` to the existing template selection-handler — call `SelectionManager.Instance.ReplaceSelection(hits)` (or the equivalent template API; check `Features/Streaming/Scripts/InteractionController.cs` for the actual selection manager).

- [ ] **Step 8: Manual verification**

Play Mode in `Streaming.unity` with a multi-part asset:
- Left-drag a box around 3 visible parts → only those 3 highlight
- Right-drag a box covering the same area → all parts whose AABB falls in the box highlight, including ones behind the front shell

Screenshot for the demo log.

- [ ] **Step 9: Commit**

```bash
git add IndustryViewerTemplate/Assets/Features/Pixyz.Selection/ \
        IndustryViewerTemplate/Assets/Scenes/Streaming.unity \
        IndustryViewerTemplate/Assets/Features/Streaming/Scripts/StreamingModelController.cs
git commit -m "feat(selection): add box-select with depth-selection (right-drag)"
```

---

### Task 9: Build the View Cube widget and predefined views

The View Cube is a small interactive 3D widget overlaid on the viewer; clicking a face snaps the camera to that orientation. Predefined view buttons (front/back/L/R/top/bottom/iso) sit alongside.

**Files:**
- Create: `IndustryViewerTemplate/Assets/Features/Pixyz.ViewCube/Pixyz.ViewCube.asmdef`
- Create: `IndustryViewerTemplate/Assets/Features/Pixyz.ViewCube/Tests/Pixyz.ViewCube.Tests.asmdef`
- Create: `IndustryViewerTemplate/Assets/Features/Pixyz.ViewCube/Scripts/PredefinedViews.cs`
- Create: `IndustryViewerTemplate/Assets/Features/Pixyz.ViewCube/Tests/PredefinedViewsTests.cs`
- Create: `IndustryViewerTemplate/Assets/Features/Pixyz.ViewCube/Scripts/ViewCubeController.cs`
- Create: `IndustryViewerTemplate/Assets/Features/Pixyz.ViewCube/UI/ViewCube.uxml`
- Create: `IndustryViewerTemplate/Assets/Features/Pixyz.ViewCube/UI/ViewCube.uss`

- [ ] **Step 1: Scaffold the asmdefs**

`Assets/Features/Pixyz.ViewCube/Pixyz.ViewCube.asmdef`:
```json
{
    "name": "Pixyz.ViewCube",
    "rootNamespace": "Pixyz.Review.ViewCube",
    "references": [
        "Unity.Industry.Viewer.Streaming",
        "Unity.Industry.Viewer.Shared",
        "Unity.AppUI",
        "Unity.AppUI.UI"
    ],
    "autoReferenced": false
}
```

`Assets/Features/Pixyz.ViewCube/Tests/Pixyz.ViewCube.Tests.asmdef`:
```json
{
    "name": "Pixyz.ViewCube.Tests",
    "rootNamespace": "Pixyz.Review.ViewCube.Tests",
    "references": ["Pixyz.ViewCube", "UnityEngine.TestRunner", "UnityEditor.TestRunner"],
    "optionalUnityReferences": ["TestAssemblies"],
    "defineConstraints": ["UNITY_INCLUDE_TESTS"]
}
```

- [ ] **Step 2: Write the failing test for predefined-view orientations**

Create `Assets/Features/Pixyz.ViewCube/Tests/PredefinedViewsTests.cs`:

```csharp
using NUnit.Framework;
using UnityEngine;
using Pixyz.Review.ViewCube;

namespace Pixyz.Review.ViewCube.Tests
{
    public class PredefinedViewsTests
    {
        [Test]
        public void FrontViewLooksAlongPositiveZ()
        {
            var pose = PredefinedViews.PoseFor(StandardView.Front, Bounds.zero, distance: 10f);
            // Camera should look from -Z toward origin (i.e. +Z is "front")
            Assert.AreEqual(Vector3.forward, pose.forward, "Front view should look in +Z direction");
        }

        [Test]
        public void BackViewLooksAlongNegativeZ()
        {
            var pose = PredefinedViews.PoseFor(StandardView.Back, Bounds.zero, distance: 10f);
            Assert.AreEqual(Vector3.back, pose.forward);
        }

        [Test]
        public void TopViewLooksDown()
        {
            var pose = PredefinedViews.PoseFor(StandardView.Top, Bounds.zero, distance: 10f);
            Assert.AreEqual(Vector3.down, pose.forward);
        }

        [Test]
        public void IsoViewIsDiagonal()
        {
            var pose = PredefinedViews.PoseFor(StandardView.Iso, Bounds.zero, distance: 10f);
            // Iso = look from (1,1,1)-direction toward origin
            var expected = -new Vector3(1, 1, 1).normalized;
            Assert.That(Vector3.Angle(pose.forward, expected), Is.LessThan(0.01f));
        }

        [Test]
        public void DistanceScalesWithBoundsSize()
        {
            var smallBounds = new Bounds(Vector3.zero, new Vector3(1, 1, 1));
            var largeBounds = new Bounds(Vector3.zero, new Vector3(100, 100, 100));
            var smallPose = PredefinedViews.PoseFor(StandardView.Front, smallBounds, distance: 0f);
            var largePose = PredefinedViews.PoseFor(StandardView.Front, largeBounds, distance: 0f);
            Assert.That(largePose.position.magnitude, Is.GreaterThan(smallPose.position.magnitude));
        }
    }
}
```

- [ ] **Step 3: Run the test — verify failure**

Test Runner → Run All. Expected: 5 tests fail with `PredefinedViews` undefined.

- [ ] **Step 4: Implement PredefinedViews**

Create `Assets/Features/Pixyz.ViewCube/Scripts/PredefinedViews.cs`:

```csharp
using UnityEngine;

namespace Pixyz.Review.ViewCube
{
    public enum StandardView
    {
        Front, Back, Left, Right, Top, Bottom, Iso
    }

    public struct CameraPose
    {
        public Vector3 position;
        public Vector3 forward;
        public Vector3 up;
    }

    public static class PredefinedViews
    {
        /// <summary>
        /// Compute a camera pose looking at the given bounds from a standard direction,
        /// with distance auto-scaled to bounds size when input distance is 0.
        /// </summary>
        public static CameraPose PoseFor(StandardView view, Bounds bounds, float distance)
        {
            Vector3 dir = view switch
            {
                StandardView.Front  => Vector3.back,        // camera at -Z looks toward +Z
                StandardView.Back   => Vector3.forward,
                StandardView.Left   => Vector3.right,
                StandardView.Right  => Vector3.left,
                StandardView.Top    => Vector3.up,
                StandardView.Bottom => Vector3.down,
                StandardView.Iso    => new Vector3(1, 1, 1).normalized,
                _ => Vector3.back,
            };

            float effectiveDistance = distance > 0 ? distance : EstimateDistance(bounds);
            return new CameraPose
            {
                position = bounds.center + dir * effectiveDistance,
                forward = (-dir).normalized,
                up = (view == StandardView.Top || view == StandardView.Bottom)
                    ? Vector3.forward
                    : Vector3.up
            };
        }

        private static float EstimateDistance(Bounds bounds)
        {
            // Frame the bounds within a 60° vertical FOV with 1.2× padding.
            float radius = bounds.extents.magnitude;
            const float fovDegrees = 60f;
            float halfFovRad = (fovDegrees * 0.5f) * Mathf.Deg2Rad;
            return (radius * 1.2f) / Mathf.Tan(halfFovRad);
        }
    }
}
```

- [ ] **Step 5: Run the tests — verify they pass**

Test Runner → Run All. Expected: 5 tests pass.

- [ ] **Step 6: Build the ViewCubeController and UI**

Create `Assets/Features/Pixyz.ViewCube/UI/ViewCube.uxml`:

```xml
<ui:UXML xmlns:ui="UnityEngine.UIElements">
    <ui:VisualElement name="view-cube-root" class="view-cube">
        <ui:VisualElement class="row">
            <ui:Button name="btn-iso"    text="Iso" />
            <ui:Button name="btn-top"    text="Top" />
        </ui:VisualElement>
        <ui:VisualElement class="row">
            <ui:Button name="btn-left"   text="L" />
            <ui:Button name="btn-front"  text="F" />
            <ui:Button name="btn-right"  text="R" />
            <ui:Button name="btn-back"   text="B" />
        </ui:VisualElement>
        <ui:VisualElement class="row">
            <ui:Button name="btn-bottom" text="Bot" />
            <ui:Button name="btn-fit"    text="Fit" />
        </ui:VisualElement>
    </ui:VisualElement>
</ui:UXML>
```

Create `Assets/Features/Pixyz.ViewCube/UI/ViewCube.uss`:

```css
.view-cube {
    position: absolute;
    top: 12px;
    right: 12px;
    background-color: rgba(0, 0, 0, 0.6);
    border-radius: 6px;
    padding: 6px;
}

.view-cube .row {
    flex-direction: row;
    margin-bottom: 4px;
}

.view-cube Button {
    width: 36px;
    height: 28px;
    margin: 2px;
}
```

Create `Assets/Features/Pixyz.ViewCube/Scripts/ViewCubeController.cs`:

```csharp
using UnityEngine;
using UnityEngine.UIElements;

namespace Pixyz.Review.ViewCube
{
    public class ViewCubeController : MonoBehaviour
    {
        [SerializeField] private UIDocument uiDocument;
        [SerializeField] private Camera viewerCamera;
        [SerializeField] private Transform streamingRoot;
        [SerializeField] private float transitionSeconds = 0.35f;

        private CameraPose? targetPose;
        private float transitionElapsed;
        private CameraPose startPose;

        private void OnEnable()
        {
            var root = uiDocument.rootVisualElement;
            Hook(root, "btn-front",  StandardView.Front);
            Hook(root, "btn-back",   StandardView.Back);
            Hook(root, "btn-left",   StandardView.Left);
            Hook(root, "btn-right",  StandardView.Right);
            Hook(root, "btn-top",    StandardView.Top);
            Hook(root, "btn-bottom", StandardView.Bottom);
            Hook(root, "btn-iso",    StandardView.Iso);
            root.Q<Button>("btn-fit").clicked += FitToScene;
        }

        private void Hook(VisualElement root, string name, StandardView view)
        {
            root.Q<Button>(name).clicked += () => GoToView(view);
        }

        private void GoToView(StandardView view)
        {
            var bounds = ComputeBounds();
            startPose = new CameraPose
            {
                position = viewerCamera.transform.position,
                forward = viewerCamera.transform.forward,
                up = viewerCamera.transform.up
            };
            targetPose = PredefinedViews.PoseFor(view, bounds, distance: 0f);
            transitionElapsed = 0f;
        }

        private void FitToScene()
        {
            // Re-apply current direction with auto-distance to frame bounds.
            var bounds = ComputeBounds();
            var dir = -viewerCamera.transform.forward;
            startPose = new CameraPose
            {
                position = viewerCamera.transform.position,
                forward = viewerCamera.transform.forward,
                up = viewerCamera.transform.up
            };
            targetPose = new CameraPose
            {
                position = bounds.center + dir * EstimateDistance(bounds),
                forward = -dir,
                up = viewerCamera.transform.up
            };
            transitionElapsed = 0f;
        }

        private Bounds ComputeBounds()
        {
            var bounds = new Bounds(streamingRoot.position, Vector3.zero);
            foreach (var renderer in streamingRoot.GetComponentsInChildren<Renderer>())
            {
                bounds.Encapsulate(renderer.bounds);
            }
            return bounds;
        }

        private static float EstimateDistance(Bounds bounds)
        {
            float radius = bounds.extents.magnitude;
            const float fovDegrees = 60f;
            float halfFovRad = (fovDegrees * 0.5f) * Mathf.Deg2Rad;
            return (radius * 1.2f) / Mathf.Tan(halfFovRad);
        }

        private void Update()
        {
            if (targetPose == null) return;

            transitionElapsed += Time.deltaTime;
            float t = Mathf.Clamp01(transitionElapsed / transitionSeconds);
            float eased = t * t * (3f - 2f * t); // smoothstep

            viewerCamera.transform.position = Vector3.Lerp(startPose.position, targetPose.Value.position, eased);
            var rot = Quaternion.LookRotation(
                Vector3.Slerp(startPose.forward, targetPose.Value.forward, eased),
                Vector3.Slerp(startPose.up, targetPose.Value.up, eased)
            );
            viewerCamera.transform.rotation = rot;

            if (t >= 1f) targetPose = null;
        }
    }
}
```

- [ ] **Step 7: Wire the View Cube into the Streaming scene**

Open `Streaming.unity`. Add a child GameObject under the existing UIDocument `View Cube` → attach `UIDocument` → set Source Asset to `ViewCube.uxml` → attach `ViewCubeController` → drag the viewer camera and the streaming root into the inspector slots.

The View Cube renders top-right by USS. Confirm it's visible above the existing HUD by adjusting `top` if there's overlap.

- [ ] **Step 8: Manual verification in Play Mode**

Play `Streaming.unity` with the sample asset:
- Click Front → camera animates to front view, asset framed
- Click Iso → camera animates to isometric
- Click Fit → camera reframes asset

Screenshot.

- [ ] **Step 9: Commit**

```bash
git add IndustryViewerTemplate/Assets/Features/Pixyz.ViewCube/ \
        IndustryViewerTemplate/Assets/Scenes/Streaming.unity
git commit -m "feat(viewcube): add view cube widget with predefined views and Fit"
```

---

### Task 10: Implement render modes (shaded / wireframe-on-shaded / lines / X-ray)

Render mode = a URP custom renderer feature that overlays or replaces the standard rendering.

**Files:**
- Create: `IndustryViewerTemplate/Assets/Features/Pixyz.Render/Pixyz.Render.asmdef`
- Create: `IndustryViewerTemplate/Assets/Features/Pixyz.Render/Scripts/RenderMode.cs`
- Create: `IndustryViewerTemplate/Assets/Features/Pixyz.Render/Scripts/RenderModeController.cs`
- Create: `IndustryViewerTemplate/Assets/Features/Pixyz.Render/Scripts/WireframeOnShadedFeature.cs`
- Create: `IndustryViewerTemplate/Assets/Features/Pixyz.Render/Scripts/XRayFeature.cs`
- Create: `IndustryViewerTemplate/Assets/Features/Pixyz.Render/Scripts/LinesFeature.cs`
- Create: `IndustryViewerTemplate/Assets/Features/Pixyz.Render/UI/RenderModeDropdown.uxml`

- [ ] **Step 1: Scaffold the asmdef**

```json
{
    "name": "Pixyz.Render",
    "rootNamespace": "Pixyz.Review.Render",
    "references": [
        "Unity.RenderPipelines.Universal.Runtime",
        "Unity.Industry.Viewer.Streaming",
        "Unity.AppUI",
        "Unity.AppUI.UI"
    ],
    "autoReferenced": false
}
```

- [ ] **Step 2: Define the RenderMode enum and controller skeleton**

`Assets/Features/Pixyz.Render/Scripts/RenderMode.cs`:

```csharp
namespace Pixyz.Review.Render
{
    public enum RenderMode
    {
        Shaded,
        WireframeOnShaded,
        Lines,
        XRay
    }
}
```

`Assets/Features/Pixyz.Render/Scripts/RenderModeController.cs`:

```csharp
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UIElements;

namespace Pixyz.Review.Render
{
    /// <summary>
    /// Toggles URP renderer features based on the selected render mode.
    /// Each mode enables/disables specific ScriptableRendererFeatures by name.
    /// </summary>
    public class RenderModeController : MonoBehaviour
    {
        [SerializeField] private UIDocument uiDocument;
        [SerializeField] private string wireframeFeatureName = "WireframeOnShaded";
        [SerializeField] private string xRayFeatureName = "XRay";
        [SerializeField] private string linesFeatureName = "Lines";

        private RenderMode currentMode = RenderMode.Shaded;

        private void OnEnable()
        {
            var root = uiDocument.rootVisualElement;
            var dropdown = root.Q<DropdownField>("render-mode-dropdown");
            dropdown.choices = new System.Collections.Generic.List<string>
            {
                "Shaded", "Wireframe", "Lines", "X-Ray"
            };
            dropdown.value = "Shaded";
            dropdown.RegisterValueChangedCallback(evt => SetMode(ParseMode(evt.newValue)));
        }

        public void SetMode(RenderMode mode)
        {
            currentMode = mode;
            EnableFeature(wireframeFeatureName, mode == RenderMode.WireframeOnShaded);
            EnableFeature(xRayFeatureName,      mode == RenderMode.XRay);
            EnableFeature(linesFeatureName,     mode == RenderMode.Lines);
            // Shaded = none of the above features active.
        }

        private static RenderMode ParseMode(string label) => label switch
        {
            "Wireframe" => RenderMode.WireframeOnShaded,
            "Lines"     => RenderMode.Lines,
            "X-Ray"     => RenderMode.XRay,
            _           => RenderMode.Shaded
        };

        private void EnableFeature(string featureName, bool enabled)
        {
            var pipeline = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
            if (pipeline == null) return;
            var rendererData = GetRendererData(pipeline);
            if (rendererData == null) return;
            foreach (var feature in rendererData.rendererFeatures)
            {
                if (feature == null) continue;
                if (feature.name == featureName)
                {
                    feature.SetActive(enabled);
                }
            }
        }

        private static ScriptableRendererData GetRendererData(UniversalRenderPipelineAsset pipeline)
        {
            // URP exposes default renderer via reflection on `m_RendererDataList`.
            var field = typeof(UniversalRenderPipelineAsset).GetField(
                "m_RendererDataList",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            var list = field?.GetValue(pipeline) as ScriptableRendererData[];
            return list != null && list.Length > 0 ? list[0] : null;
        }
    }
}
```

- [ ] **Step 3: Implement the WireframeOnShaded URP renderer feature**

Wireframe-on-shaded = standard shaded pass + a second pass with the same geometry rendered as wireframe lines on top.

Create `Assets/Features/Pixyz.Render/Scripts/WireframeOnShadedFeature.cs`:

```csharp
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Pixyz.Review.Render
{
    public class WireframeOnShadedFeature : ScriptableRendererFeature
    {
        [System.Serializable]
        public class Settings
        {
            public Material wireframeMaterial;
            public LayerMask layerMask = ~0;
            public RenderPassEvent renderPassEvent = RenderPassEvent.AfterRenderingOpaques;
        }

        public Settings settings = new Settings();
        private WireframePass pass;

        public override void Create()
        {
            pass = new WireframePass(settings);
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (settings.wireframeMaterial == null) return;
            renderer.EnqueuePass(pass);
        }

        private class WireframePass : ScriptableRenderPass
        {
            private readonly Settings settings;
            private readonly ShaderTagId shaderTagId = new ShaderTagId("UniversalForward");

            public WireframePass(Settings s)
            {
                settings = s;
                renderPassEvent = s.renderPassEvent;
            }

            public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
            {
                var cmd = CommandBufferPool.Get("WireframeOnShaded");
                var sortingSettings = new SortingSettings(renderingData.cameraData.camera);
                var drawingSettings = new DrawingSettings(shaderTagId, sortingSettings)
                {
                    overrideMaterial = settings.wireframeMaterial,
                    overrideMaterialPassIndex = 0
                };
                var filterSettings = new FilteringSettings(RenderQueueRange.opaque, settings.layerMask);

                cmd.SetGlobalFloat("_WireframeWidth", 1.5f);
                context.ExecuteCommandBuffer(cmd);
                cmd.Clear();

                context.DrawRenderers(renderingData.cullResults, ref drawingSettings, ref filterSettings);
                CommandBufferPool.Release(cmd);
            }
        }
    }
}
```

- [ ] **Step 4: Create the wireframe shader**

Create `Assets/Features/Pixyz.Render/Shaders/WireframeOnShaded.shader`:

```hlsl
Shader "Pixyz/WireframeOnShaded"
{
    Properties
    {
        _LineColor("Line Color", Color) = (0,0,0,1)
        _LineWidth("Line Width", Float) = 1.0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }

        Pass
        {
            ZWrite Off
            ZTest LEqual
            Blend SrcAlpha OneMinusSrcAlpha
            Cull Back

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma require geometry
            #pragma geometry geom

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            float4 _LineColor;
            float _LineWidth;

            struct Attributes { float4 positionOS : POSITION; };
            struct Varyings { float4 positionCS : SV_POSITION; float3 bary : TEXCOORD0; };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.bary = float3(0,0,0);
                return OUT;
            }

            [maxvertexcount(3)]
            void geom(triangle Varyings input[3], inout TriangleStream<Varyings> stream)
            {
                Varyings v0 = input[0]; v0.bary = float3(1,0,0);
                Varyings v1 = input[1]; v1.bary = float3(0,1,0);
                Varyings v2 = input[2]; v2.bary = float3(0,0,1);
                stream.Append(v0);
                stream.Append(v1);
                stream.Append(v2);
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float3 d = fwidth(IN.bary);
                float3 a = smoothstep(float3(0,0,0), d * _LineWidth, IN.bary);
                float edge = min(min(a.x, a.y), a.z);
                return half4(_LineColor.rgb, 1 - edge);
            }
            ENDHLSL
        }
    }
}
```

Create a material `Assets/Features/Pixyz.Render/Materials/WireframeOverlay.mat` using this shader. Set Line Color to black, Line Width to 1.0.

- [ ] **Step 5: Implement X-Ray feature (depth-tested transparency)**

Create `Assets/Features/Pixyz.Render/Scripts/XRayFeature.cs`:

```csharp
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Pixyz.Review.Render
{
    public class XRayFeature : ScriptableRendererFeature
    {
        [System.Serializable]
        public class Settings
        {
            public Material xRayMaterial;
            public LayerMask layerMask = ~0;
        }

        public Settings settings = new Settings();
        private XRayPass pass;

        public override void Create() { pass = new XRayPass(settings); }
        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (settings.xRayMaterial != null) renderer.EnqueuePass(pass);
        }

        private class XRayPass : ScriptableRenderPass
        {
            private readonly Settings settings;
            private readonly ShaderTagId tag = new ShaderTagId("UniversalForward");
            public XRayPass(Settings s) { settings = s; renderPassEvent = RenderPassEvent.AfterRenderingOpaques; }

            public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
            {
                var sort = new SortingSettings(renderingData.cameraData.camera);
                var draw = new DrawingSettings(tag, sort)
                {
                    overrideMaterial = settings.xRayMaterial,
                    overrideMaterialPassIndex = 0
                };
                var filter = new FilteringSettings(RenderQueueRange.opaque, settings.layerMask);
                context.DrawRenderers(renderingData.cullResults, ref draw, ref filter);
            }
        }
    }
}
```

X-Ray shader (`Assets/Features/Pixyz.Render/Shaders/XRay.shader`):

```hlsl
Shader "Pixyz/XRay"
{
    Properties
    {
        _Color("Color", Color) = (0.4, 0.7, 1.0, 0.3)
        _RimPower("Rim Power", Range(0.5, 8)) = 2.0
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "RenderPipeline"="UniversalPipeline" }
        Pass
        {
            ZWrite Off
            Blend SrcAlpha One
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            float4 _Color;
            float _RimPower;

            struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; };
            struct Varyings { float4 positionCS : SV_POSITION; float3 normalWS : TEXCOORD0; float3 viewDirWS : TEXCOORD1; };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.normalWS = TransformObjectToWorldNormal(IN.normalOS);
                float3 posWS = TransformObjectToWorld(IN.positionOS.xyz);
                OUT.viewDirWS = GetCameraPositionWS() - posWS;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float NdotV = saturate(dot(normalize(IN.normalWS), normalize(IN.viewDirWS)));
                float rim = pow(1.0 - NdotV, _RimPower);
                return half4(_Color.rgb, _Color.a * rim);
            }
            ENDHLSL
        }
    }
}
```

Create material `XRayOverlay.mat` using this shader.

- [ ] **Step 6: Implement Lines feature (edge-only)**

Lines mode = render only the wireframe, suppressing the shaded pass. Create `LinesFeature.cs` similar to `WireframeOnShadedFeature.cs` but configure the renderer to skip the default opaque pass while it's active. The simplest approach: same shader as wireframe but with the underlying material color forced to background color.

`Assets/Features/Pixyz.Render/Scripts/LinesFeature.cs`:

```csharp
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Pixyz.Review.Render
{
    public class LinesFeature : ScriptableRendererFeature
    {
        [System.Serializable]
        public class Settings { public Material linesMaterial; public LayerMask layerMask = ~0; }
        public Settings settings = new Settings();
        private LinesPass pass;

        public override void Create() { pass = new LinesPass(settings); }
        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (settings.linesMaterial != null) renderer.EnqueuePass(pass);
        }

        private class LinesPass : ScriptableRenderPass
        {
            private readonly Settings settings;
            private readonly ShaderTagId tag = new ShaderTagId("UniversalForward");
            public LinesPass(Settings s) { settings = s; renderPassEvent = RenderPassEvent.BeforeRenderingOpaques; }

            public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
            {
                var cmd = CommandBufferPool.Get("LinesMode");
                cmd.ClearRenderTarget(true, true, Color.white); // background = white in Lines mode
                context.ExecuteCommandBuffer(cmd);
                cmd.Clear();
                CommandBufferPool.Release(cmd);

                var sort = new SortingSettings(renderingData.cameraData.camera);
                var draw = new DrawingSettings(tag, sort)
                {
                    overrideMaterial = settings.linesMaterial,
                    overrideMaterialPassIndex = 0
                };
                var filter = new FilteringSettings(RenderQueueRange.opaque, settings.layerMask);
                context.DrawRenderers(renderingData.cullResults, ref draw, ref filter);
            }
        }
    }
}
```

- [ ] **Step 7: Add the three features to the URP renderer asset**

Open the URP renderer asset that the project uses (Project Settings → Graphics → check current RP Asset → expand to find renderer). Click **Add Renderer Feature** three times → add `WireframeOnShadedFeature`, `XRayFeature`, `LinesFeature`. For each, drag the corresponding material into the slot. Set Line Color, X-Ray color, Lines color to taste.

Set all three features to **disabled** by default. The `RenderModeController` enables them based on the selected mode.

Set the `name` field on each feature to match `wireframeFeatureName`, `xRayFeatureName`, `linesFeatureName` in `RenderModeController` (e.g. `"WireframeOnShaded"`, `"XRay"`, `"Lines"`).

- [ ] **Step 8: Add the dropdown UI and wire the controller**

Create `Assets/Features/Pixyz.Render/UI/RenderModeDropdown.uxml`:

```xml
<ui:UXML xmlns:ui="UnityEngine.UIElements">
    <ui:VisualElement name="render-mode-root" style="position: absolute; top: 12px; left: 240px;">
        <ui:DropdownField name="render-mode-dropdown" label="Render" />
    </ui:VisualElement>
</ui:UXML>
```

Open `Streaming.unity`. Add a child GameObject `Render Mode UI` → attach UIDocument with `RenderModeDropdown.uxml` → attach `RenderModeController` → drag the UIDocument reference.

- [ ] **Step 9: Manual verification in Play Mode**

Play `Streaming.unity` with the asset.
- Default: Shaded (full color)
- Switch to Wireframe → black wireframe overlay on shaded
- Switch to Lines → white background with edges only
- Switch to X-Ray → translucent rim-lit see-through

Screenshot each mode for the demo log. Save to `docs/screenshots/render-mode-{shaded,wireframe,lines,xray}.png`.

- [ ] **Step 10: Commit**

```bash
git add IndustryViewerTemplate/Assets/Features/Pixyz.Render/ \
        IndustryViewerTemplate/Assets/Scenes/Streaming.unity \
        IndustryViewerTemplate/Assets/Settings/ \
        docs/screenshots/render-mode-*.png
git commit -m "feat(render): add wireframe-on-shaded, lines, and x-ray render modes"
```

---

### Task 11: Phase A integration smoke test + demo recording

**Files:**
- Create: `docs/PHASE_A_DEMO.md`
- Create: `docs/screenshots/phase-a-demo-recording.mp4` (optional)

- [ ] **Step 1: Walk through the full *Hello Alstom* demo flow**

Build and run the macOS profile (`Desktop-macOS`). From a cold start:

1. Launch the .app
2. Login with Industry seat
3. Pick the `alstom-design-review-prototype` project
4. Click the prepared Alstom asset
5. Wait for streaming to load
6. Verify hierarchy panel populated, properties panel shows fields when a part is clicked
7. Use mouse-only navigation (left-click select, middle+left rotate, etc.)
8. Left-drag a box around 3 parts → verify they highlight
9. Right-drag a box → verify depth-selection includes occluded parts
10. Click each view-cube button → camera animates to that view
11. Click Fit → camera reframes
12. Switch render modes through the dropdown → verify each mode visually distinct

Note any issues in `docs/PHASE_A_DEMO.md`:

```markdown
# Phase A — Hello Alstom demo

## Build info
- Date: <date of build>
- Commit: <short hash>
- Asset: <demo asset name>

## Walkthrough

1. App launches with Alstom branding ✅
2. Login flow works ✅
3. Asset Manager browse ✅
4. Asset streams ✅
5. Hierarchy panel populated ✅
6. CATIA-like navigation ✅
7. Box-select (visible-only) ✅
8. Box-select (depth) ✅
9. View cube + predefined views ✅
10. Fit-to-scene ✅
11. Render modes (4 modes) ✅

## Known issues
- <fill in>

## Performance
- Asset triangle count: <fill in>
- Streaming time: <fill in>
- FPS at idle: <fill in>
- FPS during box-select drag: <fill in>
```

- [ ] **Step 2: Optional — record a 90-second demo video**

Use macOS QuickTime (Cmd+Shift+5) or OBS to capture the demo flow. Save to `docs/screenshots/phase-a-demo-recording.mp4`. Useful for sharing with stakeholders without needing them to install the build.

- [ ] **Step 3: Commit and push**

```bash
git add docs/PHASE_A_DEMO.md
git commit -m "docs: add Phase A walkthrough log"
git push origin main
```

- [ ] **Step 4: Tag the Phase A release**

```bash
git tag -a phase-a-foundation -m "Phase A complete: Hello Alstom milestone"
git push origin phase-a-foundation
```

---

## Phase A self-review checklist

After all tasks complete, verify against the spec (§3.1 rows 1–4):

- [ ] Repo forked with `upstream` remote configured
- [ ] Alstom branding visible in login + splash + product name
- [ ] All 3 build profiles produce artifacts (Mac built+ran; Win and Android built only)
- [ ] Sample CAD asset prepped via Asset Manager *Prepare for 3D Data Streaming*
- [ ] Deep-link namespace changed to `com.alstom.design-review`
- [ ] CATIA-like mouse navigation default; Standard ALT+mouse available in Settings
- [ ] Box-select works (left-drag visible, right-drag depth)
- [ ] View cube widget renders top-right with all 7 standard views + Fit
- [ ] All 4 render modes work and are visually distinct
- [ ] All 11 unit/edit-mode tests pass (Test Runner → Run All → green)
- [ ] `docs/PHASE_A_DEMO.md` walkthrough complete

If any box can't be checked, fix before declaring Phase A done.

---

## After Phase A — handoff to Phase B

Phase A's demo asset, learnings, and any spec corrections feed back into Phase B planning. Open this plan again at `docs/superpowers/specs/2026-04-28-alstom-design-review-viewer-design.md` and:

1. Confirm timeline assumptions held (was item #1 really 1.5 wk? adjust Phase B estimates accordingly)
2. Note any template upstream changes that landed during Phase A — merge or defer
3. If asset prep latency was a real bottleneck, decide whether to invest in a small lib of pre-prepped assets before starting Phase B
4. Run brainstorming + writing-plans skills again to produce **Plan B — Desktop parity** (spec §3.1 rows 5–11)

Phase B begins with hierarchy panel parity and ends with the measurement engine — the single hardest engineering piece in the whole project. Budget realistically.
