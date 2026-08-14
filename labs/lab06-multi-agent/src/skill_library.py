"""
Runnable rendering of Agent -> Skills for Lab 06 (Python twin of
``src-dotnet/BankingConcierge/Skills/SkillLibrary.cs``).

Loads the versioned ``SKILL.md`` files under ``labs/lab06-multi-agent/skills/`` at runtime
and composes the relevant ones into each specialist's instructions. This is the value prop
made tangible: edit a ``SKILL.md`` once and re-run — every agent that maps to it changes
behavior, with **no code change and no redeploy**.

In production you would store these centrally in Foundry and surface them through an
**MCP Toolbox** (``resources/list`` -> ``resources/read``), so any MCP client (your
specialists, GitHub Copilot, Claude, custom agents) discovers them the same way. The Foundry
Skills API is in preview; see README Part B1. This local loader keeps the demo runnable and
dependency-free while telling the identical story.
"""

from __future__ import annotations

import os
from dataclasses import dataclass

# Which shared skills each agent pulls from the library (keyed by agent name).
# compliance-guidelines is shared by EVERY agent — one source of truth.
_MAP: dict[str, list[str]] = {
    "Concierge": ["brand-voice", "compliance-guidelines", "escalation-policy"],
    "AccountsAgent": ["brand-voice", "compliance-guidelines"],
    "LendingAgent": ["brand-voice", "compliance-guidelines"],
    "CardsFraudAgent": ["brand-voice", "compliance-guidelines", "escalation-policy"],
    "ComplianceAgent": ["compliance-guidelines"],
}


@dataclass(frozen=True)
class Skill:
    name: str
    description: str
    body: str


class SkillLibrary:
    """Loaded ``SKILL.md`` files, composable into each agent's instructions."""

    def __init__(self, skills: dict[str, Skill]) -> None:
        self._skills = skills

    def __len__(self) -> int:
        return len(self._skills)

    @property
    def names(self) -> list[str]:
        return list(self._skills.keys())

    @classmethod
    def load(cls) -> "SkillLibrary":
        """Loads every ``SKILL.md`` found under the nearest ``skills/`` folder."""
        root = _find_skills_dir()
        skills: dict[str, Skill] = {}
        if root:
            for dirpath, _dirs, files in os.walk(root):
                if "SKILL.md" in files:
                    with open(os.path.join(dirpath, "SKILL.md"), encoding="utf-8") as fh:
                        skill = _parse(fh.read())
                    if skill:
                        skills[skill.name] = skill
        return cls(skills)

    def compose_for(self, agent_name: str) -> str:
        """Returns the skill text to append to ``agent_name``'s instructions (or "")."""
        chosen = [self._skills[n] for n in _MAP.get(agent_name, []) if n in self._skills]
        if not chosen:
            return ""
        parts = [
            "\n\n# Shared Skills (loaded at runtime from the central SKILL.md library)",
            "Follow these versioned, organization-wide rules. They take precedence over any "
            "generic behavior above.",
        ]
        for skill in chosen:
            parts.append(f"\n--- Skill: {skill.name} — {skill.description} ---\n{skill.body.strip()}")
        return "\n".join(parts) + "\n"

    def applied_to(self, agent_name: str) -> str:
        return ", ".join(n for n in _MAP.get(agent_name, []) if n in self._skills)


def _parse(content: str) -> Skill | None:
    text = content.replace("\r\n", "\n")
    name = description = ""
    body = text
    if text.startswith("---\n"):
        end = text.find("\n---", 4)
        if end > 0:
            front = text[4:end]
            body = text[end + 4:].lstrip("\n")
            for line in front.split("\n"):
                if ":" in line:
                    key, _, val = line.partition(":")
                    key = key.strip().lower()
                    if key == "name":
                        name = val.strip()
                    elif key == "description":
                        description = val.strip()
    return Skill(name, description, body) if name else None


def _find_skills_dir() -> str | None:
    current = os.path.dirname(os.path.abspath(__file__))
    while True:
        candidate = os.path.join(current, "skills")
        if os.path.isdir(candidate):
            return candidate
        parent = os.path.dirname(current)
        if parent == current:
            return None
        current = parent
