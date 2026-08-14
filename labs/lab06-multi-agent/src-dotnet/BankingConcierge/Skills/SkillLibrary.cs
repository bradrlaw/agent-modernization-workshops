using System.Text;

namespace BankingConcierge.Skills;

/// <summary>A versioned unit of shared behavior loaded from a <c>SKILL.md</c> file.</summary>
public sealed record Skill(string Name, string Description, string Body);

/// <summary>
/// The runnable rendering of <b>Agent → Skills</b> for Lab 06.
///
/// Loads the versioned <c>SKILL.md</c> files under <c>labs/lab06-multi-agent/skills/</c> at
/// runtime and composes the relevant ones into each specialist's instructions. This is the
/// value prop made tangible: edit a <c>SKILL.md</c> once and re-run — every agent that maps to
/// it changes behavior, with <b>no code change and no redeploy</b>.
///
/// In production you would store these centrally in Foundry and surface them through an
/// <b>MCP Toolbox</b> (<c>resources/list</c> → <c>resources/read</c>), so any MCP client
/// (your specialists, GitHub Copilot, Claude, custom agents) discovers them the same way.
/// The Foundry Skills API is in preview; see README Part B1. This local loader keeps the demo
/// runnable and dependency-free while telling the identical story.
/// </summary>
public sealed class SkillLibrary
{
    // Which shared skills each agent pulls from the library (keyed by agent name).
    // compliance-guidelines is shared by EVERY agent — one source of truth.
    private static readonly Dictionary<string, string[]> Map = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Concierge"] = ["brand-voice", "compliance-guidelines", "escalation-policy"],
        ["AccountsAgent"] = ["brand-voice", "compliance-guidelines"],
        ["LendingAgent"] = ["brand-voice", "compliance-guidelines"],
        ["CardsFraudAgent"] = ["brand-voice", "compliance-guidelines", "escalation-policy"],
        ["ComplianceAgent"] = ["compliance-guidelines"],
    };

    private readonly IReadOnlyDictionary<string, Skill> _skills;

    private SkillLibrary(IReadOnlyDictionary<string, Skill> skills) => _skills = skills;

    public int Count => _skills.Count;

    public IReadOnlyCollection<Skill> All => _skills.Values.ToList();

    /// <summary>Loads every <c>SKILL.md</c> found under the nearest <c>skills/</c> folder.</summary>
    public static SkillLibrary Load()
    {
        var dir = FindSkillsDir();
        var skills = new Dictionary<string, Skill>(StringComparer.OrdinalIgnoreCase);
        if (dir is not null)
        {
            foreach (var file in Directory.EnumerateFiles(dir, "SKILL.md", SearchOption.AllDirectories))
            {
                if (Parse(File.ReadAllText(file)) is { } skill)
                {
                    skills[skill.Name] = skill;
                }
            }
        }

        return new SkillLibrary(skills);
    }

    /// <summary>The skill text to append to <paramref name="agentName"/>'s instructions (or "").</summary>
    public string ComposeFor(string agentName)
    {
        if (!Map.TryGetValue(agentName, out var names))
        {
            return string.Empty;
        }

        var chosen = names.Where(_skills.ContainsKey).Select(n => _skills[n]).ToList();
        if (chosen.Count == 0)
        {
            return string.Empty;
        }

        var sb = new StringBuilder();
        sb.Append("\n\n# Shared Skills (loaded at runtime from the central SKILL.md library)\n");
        sb.Append("Follow these versioned, organization-wide rules. They take precedence over any "
            + "generic behavior above.\n");
        foreach (var s in chosen)
        {
            sb.Append($"\n--- Skill: {s.Name} \u2014 {s.Description} ---\n");
            sb.Append(s.Body.Trim());
            sb.Append('\n');
        }

        return sb.ToString();
    }

    /// <summary>Comma-separated names of the skills applied to <paramref name="agentName"/>.</summary>
    public string AppliedTo(string agentName) =>
        Map.TryGetValue(agentName, out var names)
            ? string.Join(", ", names.Where(_skills.ContainsKey))
            : string.Empty;

    private static Skill? Parse(string content)
    {
        var text = content.Replace("\r\n", "\n");
        string name = string.Empty, description = string.Empty, body = text;

        if (text.StartsWith("---\n", StringComparison.Ordinal))
        {
            var end = text.IndexOf("\n---", 4, StringComparison.Ordinal);
            if (end > 0)
            {
                var frontMatter = text[4..end];
                body = text[(end + 4)..].TrimStart('\n');
                foreach (var line in frontMatter.Split('\n'))
                {
                    var idx = line.IndexOf(':');
                    if (idx <= 0)
                    {
                        continue;
                    }

                    var key = line[..idx].Trim();
                    var val = line[(idx + 1)..].Trim();
                    if (key.Equals("name", StringComparison.OrdinalIgnoreCase))
                    {
                        name = val;
                    }
                    else if (key.Equals("description", StringComparison.OrdinalIgnoreCase))
                    {
                        description = val;
                    }
                }
            }
        }

        return string.IsNullOrWhiteSpace(name) ? null : new Skill(name, description, body);
    }

    private static string? FindSkillsDir()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null)
        {
            var candidate = Path.Combine(current.FullName, "skills");
            if (Directory.Exists(candidate)
                && Directory.EnumerateFiles(candidate, "SKILL.md", SearchOption.AllDirectories).Any())
            {
                return candidate;
            }

            current = current.Parent;
        }

        return null;
    }
}
