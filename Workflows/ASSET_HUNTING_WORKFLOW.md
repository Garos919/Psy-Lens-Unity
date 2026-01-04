# Asset Hunting Workflow

## Contents
- [1. Target Style](#1-target-style-locked)
- [2. File Format Rule](#2-file-format-rule-mandatory)
- [3. Acceptable Licenses & Meaning](#3-acceptable-licenses--what-they-mean)
- [4. Mandatory Asset Documentation](#4-mandatory-asset-documentation)
- [5. Required Folder Structure](#5-required-folder-structure)
- [6. Asset Acceptance Checklist](#6-asset-acceptance-checklist)
- [7. Approved Asset Sources](#7-approved-asset-sources)
- [8. Asset Hunting Rule of Thumb](#8-asset-hunting-rule-of-thumb)
- [9. Search Keywords & Discovery Guidance](#9-search-keywords--discovery-guidance)

---

This document defines the **official workflow for hunting, downloading, documenting,
and submitting third-party 3D assets** for the project.

It is designed to:
- avoid licensing issues
- ensure correct attribution
- keep Unity imports consistent
- allow building an early asset library safely and incrementally

---

## 1. Target Style (Locked)

### Geometry
- Low poly meshes only
- Poly count is the primary acceptance criterion

### Textures
- Texture quality, resolution, and style do not matter
- Textures can be freely modified later
- Textures are never a rejection criterion

---

## 2. File Format Rule (Mandatory)

### Accepted Model Format
- .fbx only

### Not Accepted
- .obj
- .blend
- .dae
- .gltf / .glb
- Any other format

If an asset is not available as .fbx, it must be converted before submission.

---

## 3. Acceptable Licenses & What They Mean

### CC0 (Creative Commons Zero)
- Free for commercial use
- No attribution required
- Redistribution and modification allowed
- License file still required

Folder:
CC0/

---

### CC-BY (Creative Commons Attribution)
- Free for commercial use
- Attribution mandatory
- Redistribution and modification allowed

Mandatory documentation:
- Creator name / username
- Direct link to creator profile or asset page
- License clearly stated

Folder:
CC-BY/

---

### Royalty-Free (Commercial Use Allowed)
- Free or one-time payment
- Commercial use allowed
- Attribution may or may not be required

If attribution is required:
- Creator name required
- Direct link to creator profile required

Folder:
ROYALTY_FREE/

---

### Not Accepted Licenses
- Non-Commercial
- Editorial-only
- Share-Alike / Copyleft
- Missing or unclear licenses

---

## 4. Mandatory Asset Documentation

Each asset must include:
- model.fbx
- textures (if any)
- ASSET_INFO.md or LICENSE.txt

Required fields:
- License type
- Source URL

If attribution required:
- Creator name
- Creator profile link

---

## 5. Required Folder Structure

ASSETS_LIBRARY/
├── CC0/
├── CC-BY/
└── ROYALTY_FREE/

Each asset must be in its own folder under the correct license.

---

## 6. Asset Acceptance Checklist

Asset is accepted only if:
- Low poly mesh
- .fbx format
- Accepted license
- Commercial use allowed
- Attribution present if required
- Documentation file included
- Correct folder placement

---

## 7. Approved Asset Sources

Preferred:
- https://sketchfab.com
- https://polyhaven.com
- https://kenney.nl/assets
- https://opengameart.org
- https://itch.io/game-assets

Rules for itch.io:
- Always read license
- Verify commercial use
- Check attribution
- Record creator and link

---

## 8. Asset Hunting Rule of Thumb

If you cannot answer:
- Who made this?
- Under what license?
- Can we use it commercially?

Do not use the asset.

---

## 9. Search Keywords & Discovery Guidance

These keywords are **guidelines**, not rules. Experiment freely.

### Core
- low poly
- lowpoly
- psx
- ps1
- retro

### Horror / Atmosphere
- horror
- dark
- creepy
- abandoned
- liminal
- industrial
- occult
- surreal

### Environments / Props
- ruins
- basement
- hallway
- urban
- decay
- interior
- props

### Characters
- npc
- enemy
- cultist
- humanoid
- low poly character

### itch.io Tips
- psx horror
- low poly horror
- retro horror assets
- ps1 environment
- psx props

Keywords help discovery only. License and format rules still apply.
