# Feel AI Skills

If you don't like using AI, don't want to use it, or don't care for it, you can simply ignore this folder, or remove it, you won't miss it.
This is here because Unity / the Asset Store now recommends publishers add it.

This folder contains **AI Skills** for Feel: instruction files that teach AI coding agents (Unity AI, Claude Code, and other agents that support skills) how to use Feel correctly in your project.
When you ask an agent something like "add screen shake when the player gets hit" or "make this button bounce", a skill gives it the exact components, menu paths, APIs and verification steps for Feel 6.4, so it doesn't have to guess.

The skills are plain Markdown. They don't add any code, menus or runtime cost to your project or your builds.

## What's included

| Skill | Use it for |
|---|---|
| `feel-add-feedbacks` | Creating MMF Players, adding feedbacks, triggering them from code, UnityEvents, animation events or Timeline, pauses and loops, runtime changes, completion callbacks, automatic (multiple) target acquisition |
| `feel-screen-shake` | Camera, Cinemachine and UI shake, camera zoom, fade, flash, and screen-shake accessibility toggles |
| `feel-post-processing-effects` | Pulsing bloom, vignette, chromatic aberration, lens distortion and other volume effects (URP, HDRP, Built-in) |
| `feel-springs` | Bouncy, spring-driven motion and values (spring components, spring feedbacks, the spring API) |
| `feel-sound-and-playlist-setup` | Setting up MMSoundManager and the MMSM Playlist Manager: mixer routing, custom tracks, volume sliders, saved audio settings, music playlists |
| `feel-sound-manager` | Playing and controlling sounds at runtime: sound feedbacks, 3D sounds, sound IDs, track control, track pitch, fades |
| `feel-custom-feedback` | Writing your own feedback classes in C#, extending existing feedbacks, writing shakers |
| `feel-haptics` | Vibrations and gamepad rumble with Nice Vibrations |

Each skill lives in its own folder:

```
Skills/
├── README.md                     ← this file
└── feel-<capability>/
    ├── SKILL.md                  ← the skill itself
    └── references/               ← optional deeper material the skill links to
```

## How a skill is used

Agents don't read every skill up front. They read each skill's `description` (in the frontmatter at the top of `SKILL.md`) and load the full skill only when your request matches it. You don't need to name the skill or mention Feel in your request: "the camera should shake when I land" is enough.

Each `SKILL.md` follows the same structure, so you can read it yourself too:

- **When to use this skill**: example requests, plus pointers to the right skill for related requests.
- **Prerequisites**: requirements, and a check the agent runs to confirm Feel (and any optional package) is installed.
- **Quick start**: the most common task, end to end.
- **Workflows**: step-by-step procedures with defaults and C# examples.
- **Verification**: how the agent confirms its own work.
- **API quick reference**, **Common issues**, **Boundaries**.

## Using the skills with your agent

### Unity AI

These skills are registered in the Asset Store skill catalog, which Unity AI uses to find skills for the assets in your project. Keep this folder in your project; see Unity AI's documentation for how it currently discovers and enables catalogued skills.

### Claude Code

Claude Code loads project skills from `.claude/skills/` at your project root. Expose this folder there with a link, so the skills stay in sync when you update Feel. Run this from your Unity project's root folder.

Windows (PowerShell):

```powershell
New-Item -ItemType Junction -Path .claude\skills -Target Assets\Feel\Skills
```

macOS / Linux:

```bash
mkdir -p .claude && ln -s ../Assets/Feel/Skills .claude/skills
```

If you already have a `.claude/skills` folder, link or copy each `feel-*` folder into it individually instead. Start a new Claude Code session afterwards; the Feel skills appear in its list of available skills.

### Other agents

For agents that support the same `SKILL.md` format, point their skills directory at this folder (or link it in, as above). For agents without skill support, add a line like this to your project's `AGENTS.md` (or the equivalent instructions file):

> Feel skills are in `Assets/Feel/Skills/`. Before working with Feel, read the `description` of each `SKILL.md` there and follow the skill that matches the task.

## Getting the best results

- **Give the agent access to the Unity Editor.** Many workflows create GameObjects, add components and read the Console. Agents that can operate the Editor (for example through a Unity MCP integration) can complete and verify these tasks on their own. Agents that only edit files can still write scripts, but will ask you to do the Editor steps.
- **Have the optional packages you need installed.** Some feedbacks only exist when their package is present (Cinemachine, URP/HDRP, Post Processing, TextMesh Pro). The skills check for this and tell the agent what's missing, but they ask before installing anything.
- **Save your scene and use version control.** Some steps (Automatic Shaker Setup, for example) modify your scene, and audio settings steps can modify assets. The skills tell the agent to ask before changing project settings or shared assets.
- **Keep custom code outside `Assets/Feel/`.** The skills already tell agents to do this, so Feel updates don't overwrite your work.

## Updating

The skills are versioned with Feel: each `SKILL.md` lists the Feel version it was verified against (`asset-version`) and when (`last-verified`). When you update Feel, the new package replaces this folder with skills matching the new version. If you've edited a skill locally, copy your changes elsewhere before updating.

## Feedback

If a skill gives an agent wrong or outdated instructions, please report it through the support form at https://feel.moremountains.com/, with the skill name and what went wrong. The full Feel documentation is at https://feel-docs.moremountains.com/.
