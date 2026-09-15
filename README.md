# hevy-mcp

An MCP server that computes strength analytics from your Hevy training log and hands an AI assistant the numbers.

An AI assistant can read the Hevy API, but it cannot reliably fit a regression across 273 workouts and 5000 sets from raw JSON. Ask it to judge your progress directly and it may produce a plausible figure it never computed. The server runs that arithmetic in C# and returns figures you can check against your log.

```
you    "am I making progress on Dumbbell Row?"
client  get_exercise_progress("Dumbbell Row")
server  20 sessions, e1RM 19.0 -> 36.4 kg, +2.11 kg/month, +1.92 over the last 3 months
client  "Yes, and the pace held steady through the summer."
```

## Requirements

| | |
|---|---|
| Hevy Pro | Hevy issues API keys to Pro subscribers only |
| .NET 10 SDK | I build and run it on 10.0.301 |
| An MCP client | Claude Code, Codex, or another client that speaks MCP over stdio |

## Setup

Clone and build:

```bash
git clone https://github.com/jaronskijakub/hevy-mcp.git
cd hevy-mcp
dotnet build
```

Generate an API key at [hevy.com/settings?developer](https://hevy.com/settings?developer). Store it with .NET user secrets:

```bash
dotnet user-secrets set "Hevy:ApiKey" "<your-key>" --project src/HevyMcp.Server
```

The key lands in `~/.microsoft/usersecrets/<UserSecretsId>/secrets.json`, outside the repository, so git never sees it. Only the `UserSecretsId` sits in the `.csproj`, and anyone reading it learns nothing about your account.

`Program.cs` loads user secrets in every environment, including Production:

```csharp
builder.Configuration.AddUserSecrets<Program>();
```

Publish the server:

```bash
dotnet publish src/HevyMcp.Server -c Release -o publish
```

Register it with Claude Code:

```bash
claude mcp add hevy --scope user -- dotnet "$(pwd)/publish/HevyMcp.Server.dll"
```

Or register it with Codex:

```bash
codex mcp add hevy -- dotnet "$(pwd)/publish/HevyMcp.Server.dll"
```

Check the connection with the matching client:

```bash
claude mcp list
codex mcp list
```

Then restart the client and ask whether the hevy server is alive. It should call `hevy_ping` and get 42 back. Codex CLI, the Codex IDE extension, and the ChatGPT desktop app share the same Codex MCP configuration.

While you develop, point the client at `dotnet run --project src/HevyMcp.Server`. MSBuild writes to stdout on the first build of a session and corrupts the protocol stream, so publish before you register the server for daily use.

## Tools

| Tool | Returns |
|---|---|
| `get_exercise_catalog` | Exercises you have logged, with session counts and the last date you trained them |
| `get_exercise_progress` | Two estimated-1RM slopes in kg per month, plus the first and last estimate |
| `get_routines_for_exercise` | Saved routines containing an exercise, including every exercise in each routine |
| `get_exercise_alternatives` | Substitutes that hit the same muscle with different equipment and can exclude today's routine |
| `hevy_ping` | Health check |

### get_exercise_catalog

Hevy ships around 460 exercise templates. You have touched maybe 100 of them. This tool returns yours, most trained first, filtered by an optional substring.

```
get_exercise_catalog("row")
```

```json
[
  { "name": "Seated Cable Row V-Grip", "sessions": 83, "lastPerformed": "2026-08-24T09:12:00+00:00" },
  { "name": "Iso-Lateral Row",         "sessions": 71, "lastPerformed": "2026-08-22T10:24:23+00:00" },
  { "name": "Dumbbell Row",            "sessions": 20, "lastPerformed": "2026-08-22T10:24:23+00:00" }
]
```

`get_exercise_progress` matches titles character for character, so the assistant calls the catalog first whenever it lacks the exact name. Without it, the model guesses `"Row"` and gets back an error it cannot act on.

### get_exercise_progress

```
get_exercise_progress("Chest Fly (Machine)")
```

```json
{
  "exercise": "Chest Fly (Machine)",
  "sessions": 96,
  "from": "2024-10-16T17:31:00+00:00",
  "to": "2026-08-21T14:02:00+00:00",
  "firstE1Rm": 40,
  "lastE1Rm": 65,
  "kgPerMonth": 0.77,
  "recentSessions": 11,
  "recentKgPerMonth": 0.05
}
```

Read the two slopes together. A healthy `kgPerMonth` next to a flat `recentKgPerMonth` means 22 months of growth are propping up a lift that stopped moving in May.

The tool description tells the model to report these two windows and no others, because a model holding one number that spans two years will describe months it never measured.

### get_exercise_alternatives

```
get_routines_for_exercise("Chest Fly (Machine)")
get_exercise_alternatives("Chest Fly (Machine)", "<confirmed-routine-id>")
```

```json
{
  "match": "Primary",
  "exercises": [
    { "name": "Bench Press (Dumbbell)",  "muscleGroup": "chest", "equipment": "dumbbell", "sessions": 12 },
    { "name": "Chest Fly (Dumbbell)",    "muscleGroup": "chest", "equipment": "dumbbell", "sessions": 0 },
    { "name": "Chest Fly (Band)",        "muscleGroup": "chest", "equipment": "band",     "sessions": 0 }
  ]
}
```

The assistant first finds the saved routines containing the exercise and asks which one you are doing today. Even a single match needs confirmation because you may be training outside that plan. Once confirmed, it passes the routine id to `get_exercise_alternatives`, and the server excludes every exercise in that routine, including exercises you have not performed yet. If no routine matches or you are training outside a saved routine, the assistant omits the id and makes clear that today's plan was not taken into account.

Among the remaining candidates, the server selects the same primary muscle, different equipment, and the same exercise type. It sorts exercises you already train to the top, and `sessions: 0` marks the ones you have never tried. If the routine excludes every suitable candidate, the tool returns `None` with an empty list instead of restoring a planned exercise.

`match` reports how the server found them:

| Value | Meaning |
|---|---|
| `Primary` | Same primary muscle group |
| `Secondary` | Your muscle appears among the candidate's secondary groups, so the substitute is weaker |
| `None` | Nothing fits, and the model should say so |

## Examples

**"What do I train?"**

The assistant calls the catalog with no filter and gets your whole logged history back, sorted by frequency. It can spot that you did Face Pull once in February and dropped it.

**"The Chest Fly machine is taken, what else can I do?"**

The assistant calls `get_routines_for_exercise("Chest Fly (Machine)")`, asks you to confirm today's routine, then calls `get_exercise_alternatives` with its id. Its answer excludes exercises already scheduled for that workout:

> Exercises you have done before (you know these best):
>
> - Bench Press (Dumbbell), 12 sessions
> - Incline Bench Press (Dumbbell), 9 sessions
> - Incline Bench Press (Barbell), 2 sessions
>
> If you want something closer to the fly movement (chest isolation rather than pressing):
>
> - Chest Fly (Dumbbell), flyes with dumbbells on a bench
> - Chest Fly (Band), flyes with a band if the machines and dumbbells are busy
>
> Bench presses recruit more triceps and shoulders, so reach for the dumbbells if you want the chest isolation that Chest Fly gives you.

I never asked for that split. The `sessions` field drives it, and the tool description states that 0 means you have never logged the exercise. The model added the closing note about triceps from its own training.

I asked in Polish. The tool descriptions stay in English and the model bridges the two.

**"Compare my three rowing variations."**

The assistant calls the catalog once and `get_exercise_progress` three times, then lays the slopes side by side:

| Exercise | Sessions | e1RM | All time | Last 3 months |
|---|---|---|---|---|
| Seated Cable Row V-Grip | 83 | 33.3 -> 80.0 | +1.04 | +4.89 |
| Iso-Lateral Row | 71 | 33.3 -> 69.7 | +0.71 | +7.18 |
| Dumbbell Row | 20 | 19.0 -> 36.4 | +2.11 | +1.92 |

Two of the three sped up over the summer. You lose that with one all-time slope.

## How it works

```
Tools/      MCP surface. Knows both Hevy and Analysis.
Analysis/   e1RM, regression, substitution rules. Imports nothing from Hevy.
Hevy/       API client, response models, in-memory caches.
```

`Analysis` defines its own input records (`CompletedSet`, `E1RmPoint`, `ExerciseProfile`) and imports nothing from `Hevy`, so a rename in the Hevy API cannot reach the maths. The tools translate between the two sides.

**Estimated 1RM.** Epley, `weight x (1 + reps / 30)`, applied to the best working set of each session. The calculation skips warmups and sets with no recorded weight.

**Trend.** Least squares over `(days since first session, e1RM)`, reported per 30 days. The fit weights each session by its distance from the middle of the period, so one bad Tuesday shifts the line a little while a slump in the closing weeks pulls it down.

**Caching.** The exercise catalog costs 5 requests, your workout history costs about 28, and saved routines may span several pages. All three load once per process behind a `SemaphoreSlim`, so parallel tool calls trigger one fetch per data source. Restart the server after changing a saved routine in Hevy.

## Known limitations

- **Bodyweight exercises.** Hevy records added weight only, so a weighted pull-up reads as 8.75 kg. The offset stays constant, so the trend holds. Treat the absolute figure as meaningless.
- **Duplicate titles.** A custom exercise sharing a name with a built-in one loses to whichever Hevy returns first.
- **Short recent windows.** Machine plates jump 5 kg at a time, so a three-month window holding four sessions produces a noisy slope. `recentSessions` exposes the sample size.
- **Exact title matching.** `"Dumbbell Rows"` fails where `"Dumbbell Row"` succeeds. The catalog tool covers this, and no fuzzy fallback exists yet.
- **No unit tests.** I checked the e1RM and regression code by hand against known inputs. No test suite covers them yet.
- **Captive dependency.** `ExerciseCatalog` is a singleton holding a transient `HttpClient`, which defeats handler rotation. This process lives for hours, so it costs nothing today.

## Roadmap

- Deploy the server with Streamable HTTP transport and authentication so you can use it from Claude Mobile or another remote MCP client at the gym without keeping a local agent session running on a computer.
- `get_training_gaps`: exercises you have dropped, ranked by how long they have been missing

## Disclaimer

I am not affiliated with Hevy. This project calls the public Hevy API and ships no Hevy code or data. Hevy is a trademark of its owners.

## License

MIT
