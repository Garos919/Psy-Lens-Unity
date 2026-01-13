# Psy-Lens GitHub Governance (.github) and Branch Workflow

## Contents
- [1. Purpose](#1-purpose)
- [2. `.github` Folder](#2-github-folder)
  - [2.1 Folder Tree](#21-folder-tree)
  - [2.2 What each file does](#22-what-each-file-does)
- [3. Access Configuration (`psylens-access.json`)](#3-access-configuration-psylens-accessjson)
  - [3.1 Schema](#31-schema)
  - [3.2 Roles inside config](#32-roles-inside-config)
  - [3.3 `tag_map` (contributor tag → GitHub user)](#33-tag_map-contributor-tag--github-user)
- [4. Roles](#4-roles)
- [5. Branch Families](#5-branch-families)
- [6. Branch Naming Rules](#6-branch-naming-rules)
  - [6.1 `task/*` format](#61-task-format)
  - [6.2 `tag/*` format](#62-tag-format)
- [7. Permissions Matrix](#7-permissions-matrix)
- [8. Promotion Flow](#8-promotion-flow)
- [9. Practical Workflow Guide](#9-practical-workflow-guide)
  - [9.1 Work in `tag/*` (personal sandbox)](#91-work-in-tag-personal-sandbox)
  - [9.2 Move `tag/*` → `task/*` (scoped task branch)](#92-move-tag--task-scoped-task-branch)
  - [9.3 Merge `task/*` → `develop` (integration)](#93-merge-task--develop-integration)
  - [9.4 Merge `develop` → `main` (milestone/stable)](#94-merge-develop--main-milestonestable)
- [10. Automation Behavior](#10-automation-behavior)
  - [10.1 What happens on branch creation](#101-what-happens-on-branch-creation)
  - [10.2 What happens on push](#102-what-happens-on-push)
- [11. Maintenance](#11-maintenance)
  - [11.1 Add or update a contributor tag](#111-add-or-update-a-contributor-tag)
  - [11.2 Rename a GitHub username](#112-rename-a-github-username)
  - [11.3 If something gets deleted or reverted](#113-if-something-gets-deleted-or-reverted)

---

## 1. Purpose

This repository uses GitHub Actions to enforce:
- allowed branch name formats
- who is allowed to create branches
- who is allowed to push to each branch family

The goal is to keep the repo stable and prevent accidental or unauthorized pushes.

---

## 2. `.github` Folder

### 2.1 Folder Tree

    .github/
    ├── psylens-access.json
    └── workflows/
        ├── enforce-branch-names.yml
        └── push-guard.yml

### 2.2 What each file does

- `psylens-access.json`  
  The single source of truth for:
  - Director username
  - Coordinator usernames
  - contributor tag → GitHub username mapping

- `workflows/enforce-branch-names.yml`  
  Runs when a branch is created and deletes branches that:
  - do not match allowed naming rules
  - are created by someone not allowed for that branch type
  - (for `tag/*`) use an unmapped contributor tag

- `workflows/push-guard.yml`  
  Runs on pushes and enforces push permissions by reverting unauthorized commits.

---

## 3. Access Configuration (`psylens-access.json`)

### 3.1 Schema

    {
      "director": "GitHubUsername",
      "coordinators": ["GitHubUsername1", "GitHubUsername2"],
      "tag_map": {
        "contributorTag": "GitHubUsername"
      }
    }

### 3.2 Roles inside config

- `director`  
  Single repository director role.

- `coordinators`  
  Trusted maintainers.

### 3.3 `tag_map` (contributor tag → GitHub user)

`tag_map` connects a short contributor tag (used in `tag/*` branch names) to a GitHub username.

Example:

    "tag_map": {
      "ngar": "Garos919",
      "mvid": "IntrProgrammer"
    }

---

## 4. Roles

- **Director**  
  Full authority over protected branches.

- **Coordinator**  
  Maintainer role that can manage integration and task branches.

- **Contributor (tag owner)**  
  A mapped owner from `tag_map` who can work inside their own `tag/*` branch namespace.

---

## 5. Branch Families

Allowed branch families:
- `main`
- `develop`
- `task/*`
- `tag/*`

---

## 6. Branch Naming Rules

### 6.1 `task/*` format

Format:
- `task/NNN-slug`

Rules:
- `NNN` is exactly **3 digits** (e.g. `007`, `120`, `999`)
- `slug` uses **lowercase letters/numbers** and **hyphens**
- hyphen-separated words only

Examples:
- `task/012-fog-tuning`
- `task/120-ui-main-menu`
- `task/305-audio-mixer-pass`

### 6.2 `tag/*` format

Format:
- `tag/NNN-slug/<tag>`

Rules:
- `NNN-slug` follows the same format as `task/*`
- `<tag>` must exist in `psylens-access.json -> tag_map`

Examples:
- `tag/012-fog-tuning/ngar`
- `tag/305-audio-mixer-pass/mvid`

---

## 7. Permissions Matrix

| Branch | Represents | Who can create | Who can push |
|---|---|---|---|
| `main` | stable milestone / release baseline | Director | Director |
| `develop` | integration branch | Director, Coordinators | Director, Coordinators |
| `task/NNN-slug` | scoped task branch meant for merge | Director, Coordinators | Director, Coordinators |
| `tag/NNN-slug/<tag>` | personal sandbox for `<tag>` owner | Director, Coordinators, mapped `<tag>` owner | Director, Coordinators, mapped `<tag>` owner |

---

## 8. Promotion Flow

Official flow:

`tag/*  ->  task/*  ->  develop  ->  main`

Meaning:
- `tag/*` is where an individual iterates freely
- `task/*` is where work is organized into a clean task branch
- `develop` is where multiple tasks integrate together
- `main` is where stable milestones land

---

## 9. Practical Workflow Guide

### 9.1 Work in `tag/*` (personal sandbox)

Contributor creates and works in:
- `tag/NNN-slug/<their-tag>`

Purpose:
- personal iteration without affecting integration branches

### 9.2 Move `tag/*` → `task/*` (scoped task branch)

Coordinator/director creates:
- `task/NNN-slug`

Then changes from the `tag/*` branch are brought into the task branch (commonly via PR).

Purpose:
- clean task history and clear scope before integration

### 9.3 Merge `task/*` → `develop` (integration)

After review/acceptance:
- merge the `task/*` branch into `develop`

Purpose:
- combine multiple tasks into one buildable/testing branch

### 9.4 Merge `develop` → `main` (milestone/stable)

When the integration state is stable:
- director merges `develop` into `main`

Purpose:
- `main` remains stable and represents milestones/releases

---

## 10. Automation Behavior

### 10.1 What happens on branch creation

When someone creates a branch:
- if the name is invalid, the branch is deleted
- if the creator is not allowed for that branch type, the branch is deleted
- if it is a `tag/*` branch and the `<tag>` is not in `tag_map`, the branch is deleted

### 10.2 What happens on push

When someone pushes:
- if they are authorized for that branch type, the push stays
- if they are not authorized, commits are reverted and pushed back to the branch
- if revert conflicts, an issue is opened for manual intervention

---

## 11. Maintenance

### 11.1 Add or update a contributor tag

Edit `psylens-access.json` on `main`:

    "tag_map": {
      "ngar": "Garos919",
      "newtag": "NewGitHubUsername"
    }

### 11.2 Rename a GitHub username

Update the username wherever it appears in:
- `director`
- `coordinators`
- `tag_map`

### 11.3 If something gets deleted or reverted

- If a branch is deleted immediately after creation:
  - its name format is invalid, or
  - the creator is not authorized, or
  - (for `tag/*`) the `<tag>` is not mapped in `tag_map`

- If a push gets reverted:
  - the actor is not authorized to push to that branch family
  - check the Permissions Matrix and the current `psylens-access.json`
