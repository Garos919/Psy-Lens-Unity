# Asset Hunting Workflow

## Contents

* [1. Setup & Folder Structure](#1-setup--folder-structure)
* [2. Naming Rules](#2-naming-rules)
* [3. File Format Rules](#3-file-format-rules)
* [4. Asset Documentation](#4-asset-documentation)
* [5. Acceptable Licenses & Meaning](#5-acceptable-licenses--meaning)
* [6. Target Style](#6-target-style)
* [7. Asset Gathering & Sources](#7-asset-gathering--sources)
* [8. Asset Acceptance Checklist](#8-asset-acceptance-checklist)
* [9. Asset Hunting Rule of Thumb](#9-asset-hunting-rule-of-thumb)
* [10. Search Keywords & Discovery Guidance](#10-search-keywords--discovery-guidance)

---

This document defines the **official workflow for hunting, downloading, documenting, and submitting third-party 3D assets** for the project.

---

## 1. Setup & Folder Structure

Create the following folder structure:

```
Asset_Library/
├── CC0/
│   └── <contributor_id>/
├── CC-BY/
│   └── <contributor_id>/
└── ROYALTY_FREE/
    └── <contributor_id>/
```

Rules:

* Each asset must be placed under the correct license folder
* Each asset must live in its **own folder**, named after the asset
* Do **not** create additional subfolders inside asset folders at this stage
* Each contributor may add **up to 10 assets total**

Example (Nick Garos – `ngar`, asset: `building_1`):

```
Asset_Library/
└── CC0/
    └── ngar/
        └── building_1/
            ├── model.fbx
            ├── ASSET_INFO.md
            └── LICENSE.txt
```

---

## 2. Naming Rules

Contributor identification and asset naming follow these rules:

### Contributor Folder

Each contributor uses a single identifier in the format:

```
<first-initial><last-name-3-letters>
```

Example:

* Nick Garos → `ngar`

This identifier is used **only** for the contributor folder name under each license.

### Asset Folder

* Each asset must live in its **own folder**
* The asset folder must be named after the asset / model
* Use clear, descriptive names (e.g. `building_1`, `hallway_segment`, `streetlamp_old`)

No other naming schemes are used at this stage.

---

## 3. File Format Rules

### Accepted Model Format

* `.fbx` only

### Not Accepted

* `.obj`
* `.blend`
* `.dae`
* `.gltf` / `.glb`
* Any other format

If an asset is not available as `.fbx`, it must be converted before submission.

---

## 4. Asset Documentation

Each asset must include **both**:

* `ASSET_INFO.md`
* `LICENSE.txt`

`ASSET_INFO.md` must include:

* Source URL
* Asset page link
* Creator name
* Creator profile link

`LICENSE.txt` must include:

* Full license text
* License name and version

---

## 5. Acceptable Licenses & Meaning

### CC0 (Creative Commons Zero)

* Free for commercial use
* No attribution required
* Redistribution and modification allowed

Folder:

```
CC0/
```

---

### CC-BY (Creative Commons Attribution)

* Free for commercial use
* Attribution mandatory
* Redistribution and modification allowed

Folder:

```
CC-BY/
```

---

### Royalty-Free (Commercial Use Allowed)

* Free or one-time payment
* Commercial use allowed
* Attribution may or may not be required

Folder:

```
ROYALTY_FREE/
```

---

### Not Accepted Licenses

* Non-Commercial
* Editorial-only
* Share-Alike / Copyleft
* Missing or unclear licenses

---

## 6. Target Style

### Geometry

* Low poly meshes only
* Poly count is the primary acceptance criterion

### Textures

* Texture quality, resolution, and style do not matter
* Textures can be freely modified later
* Textures are never a rejection criterion

---

## 7. Asset Gathering & Sources

Approved sources:

* [https://sketchfab.com](https://sketchfab.com)
* [https://itch.io/game-assets](https://itch.io/game-assets)
* [https://opengameart.org](https://opengameart.org)
* [https://quaternius.com](https://quaternius.com)
* [https://poly.pizza](https://poly.pizza)

---

## 8. Asset Acceptance Checklist

An asset is accepted only if:

* Low poly mesh
* `.fbx` format
* Accepted license
* Commercial use allowed
* Documentation files included
* Correct folder placement

---

## 9. Asset Hunting Rule of Thumb

If you cannot answer:

* Who made this?
* Under what license?
* Can we use it commercially?

Do not use the asset.

---

## 10. Search Keywords & Discovery Guidance

These keywords are **guidelines**, not rules. Experiment freely.

### Core

* low poly
* lowpoly
* psx
* ps1
* retro

### Horror / Atmosphere

* horror
* dark
* creepy
* abandoned
* liminal
* industrial
* occult
* surreal

### Environments / Props

* ruins
* basement
* hallway
* urban
* decay
* interior
* props

### Characters

* npc
* enemy
* cultist
* humanoid
* low poly character

### itch.io Tips

* psx horror
* low poly horror
* retro horror assets
* ps1 environment
* psx props

Keywords help discovery only. License and format rules still apply.
