# AGENTS.md instructions for /home/mingmarsherpa/RiderProjects/TodoView

## Skills
A skill is a set of local instructions to follow that is stored in a `SKILL.md` file. Below is the list of skills that can be used. Each entry includes a name, description, and file path so you can open the source for full instructions when using a specific skill.

### Available skills
- `skill-creator`: Guide for creating effective skills. Use when creating a new skill or updating an existing skill that extends Codex with specialized knowledge, workflows, or tool integrations. File: `/home/mingmarsherpa/.cache/JetBrains/Rider2025.3/aia/codex/skills/.system/skill-creator/SKILL.md`
- `skill-installer`: Install Codex skills into `$CODEX_HOME/skills` from a curated list or a GitHub repo path. Use when listing installable skills, installing a curated skill, or installing a skill from another repo, including private repos. File: `/home/mingmarsherpa/.cache/JetBrains/Rider2025.3/aia/codex/skills/.system/skill-installer/SKILL.md`

### How to use skills
- Discovery: The list above is the skills available in this session. Skill bodies live on disk at the listed paths.
- Trigger rules: If the user names a skill with `$SkillName` or plain text, or the task clearly matches a skill's description, use that skill for the turn. Multiple mentions mean use them all. Do not carry skills across turns unless re-mentioned.
- Missing or blocked: If a named skill is not in the list or the path cannot be read, say so briefly and continue with the best fallback.

### Skill workflow
1. After deciding to use a skill, open its `SKILL.md`. Read only enough to follow the workflow.
2. When `SKILL.md` references relative paths such as `scripts/foo.py`, resolve them relative to the skill directory listed above first, and only consider other paths if needed.
3. If `SKILL.md` points to extra folders such as `references/`, load only the specific files needed for the request instead of bulk-loading everything.
4. If `scripts/` exist, prefer running or patching them instead of retyping large code blocks.
5. If `assets/` or templates exist, reuse them instead of recreating from scratch.

### Coordination and sequencing
- If multiple skills apply, choose the minimal set that covers the request and state the order you will use them.
- Announce which skill or skills you are using and why in one short line. If you skip an obvious skill, say why.

### Context hygiene
- Keep context small: summarize long sections instead of pasting them, and only load extra files when needed.
- Avoid deep reference-chasing: prefer opening only files directly linked from `SKILL.md` unless blocked.
- When variants exist such as frameworks, providers, or domains, pick only the relevant reference files and note that choice.

### Safety and fallback
- If a skill cannot be applied cleanly due to missing files or unclear instructions, state the issue, pick the next-best approach, and continue.
