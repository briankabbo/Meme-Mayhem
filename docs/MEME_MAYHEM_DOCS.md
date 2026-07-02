# 🔥 Meme Mayhem — Project Documentation

## Project Overview
A web-based multiplayer meme reaction card game.
- **Frontend:** React + TypeScript + Tailwind + Framer Motion
- **Backend:** .NET 9 + SignalR
- **Database:** PostgreSQL (Supabase)
- **AI Prompts:** Groq (Llama3-8b-8192) — free tier
- **Meme Cards:** Hand-curated reaction images hosted on Supabase private storage
- **Reaction GIFs:** Giphy API — free key
- **Repo:** GitHub (public)
- **IDE:** Google IDE (browser-based, NOT Visual Studio)
- **Shell:** Windows PowerShell

---

## Game Rules — Final v2.0

### Core Rules
| Rule | Value |
|---|---|
| Players | 3 to 10 (2 allowed, 3+ recommended) |
| Join method | Private room code |
| Rounds | Host picks |
| Theme | Host picks per game |
| Hand size | Always 5 cards |
| Turn timer | 15 seconds |
| Voting timer | None |
| Turn order | Random each round |
| Turn order visibility | Hidden (current only shown) |
| Self voting | Not allowed |
| Haha | 1 point |
| Lmao | 5 points |
| Meh | 0 points |
| Card after round | Draw 1 → back to 5 |
| Draw timing | After round results shown |
| Deck exhausted | Reshuffle discards |
| Disconnect grace | 30 seconds |
| Disconnect action | Auto skip turn |
| Host leaves | Auto promote next player |
| Late joiners | Spectator mode |

### Round Lifecycle
```
1. ROUND START
   └── Turn order randomized (hidden)
   └── AI prompt generated & shown (stays all round)
   └── Everyone has 5 cards

2. TURN (repeats for each player sequentially)
   └── Current player notified privately
   └── 15s timer starts
   └── Player picks 1 card → revealed to all
   └── Everyone else votes (Haha / Lmao / Meh)
   └── Turn ends when all (n-1) votes cast
   └── If 15s expires → player auto skipped
   └── If player disconnects → 30s grace → auto skipped

3. ROUND END
   └── All points tallied
   └── Results shown on scoreboard
   └── 1 new card dealt → back to 5
   └── If deck empty → reshuffle discards
   └── Next round starts OR game over

4. GAME OVER
   └── Final scoreboard shown
   └── Winner announced
   └── Play again option
```

### Themes
- 💀 Dark Humor
- 💼 Office Safe
- 📱 Gen Z
- 🔥 Pure Chaos
- 🌸 Wholesome

---

## SignalR Event Map

| Client → Server | Server → Client |
|---|---|
| CreateRoom | RoomCreated |
| JoinRoom | PlayerJoined |
| SpectateRoom | SpectatorJoined |
| PickTheme | ThemeSelected |
| PickRounds | RoundsSelected |
| StartGame | GameStarted |
| | HandDealt (private) |
| | RoundStarted |
| | YourTurn (private) |
| | TurnStarted |
| SubmitCard | CardRevealed (broadcast) |
| | TurnTimerStarted |
| | TurnSkipped |
| SubmitVote | VoteReceived (broadcast) |
| | TurnEnded |
| | RoundEnded |
| | ScoreUpdated |
| | NewCardDealt (private) |
| | NextRoundStarted |
| | HostChanged |
| | PlayerDisconnected |
| | PlayerReconnected |
| | GameOver |

### Critical SignalR Event Ordering
`StartGame` must fire events in this exact order or UI breaks:
```
1. HandDealt  (private) ← cards must exist before UI transitions
2. RoundStarted         ← populates currentRound in state
3. GameStarted          ← triggers UI transition LAST
4. YourTurn   (private) ← notify first player
```

Between rounds (`EndRoundAsync`):
```
1. RoundEnded           ← show results
2. [5s delay]           ← results display time
3. Check IsGameOver     ← from results object, NOT re-fetched room
4. DrawNewCards         ← only if game continues
5. DealHandsToPlayers   ← new hands privately
6. RoundStarted         ← next round data
7. YourTurn             ← notify first player
```

---

## Project Structure

```
Meme-Mayhem/
├── backend/
│   ├── MemeMayhem.sln
│   ├── MemeMayhem.API/
│   │   ├── Hubs/
│   │   │   └── GameHub.cs
│   │   ├── Properties/
│   │   │   └── launchSettings.json  ← port 5235
│   │   ├── Program.cs
│   │   └── appsettings.Development.json  ← secrets (gitignored)
│   ├── MemeMayhem.Core/
│   │   ├── DTOs/
│   │   │   ├── CardPlayDto.cs
│   │   │   ├── CardPlayWithRoundDto.cs
│   │   │   ├── MemeCardDto.cs
│   │   │   ├── PlayerDto.cs
│   │   │   ├── PlayerScoreDto.cs
│   │   │   ├── RoomDto.cs
│   │   │   ├── RoundDto.cs
│   │   │   ├── RoundResultDto.cs
│   │   │   └── VoteDto.cs
│   │   ├── Entities/
│   │   │   ├── CardPlay.cs
│   │   │   ├── MemeCard.cs          ← uses StoragePath not ImageUrl
│   │   │   ├── Player.cs
│   │   │   ├── PlayerCard.cs
│   │   │   ├── ReactionGif.cs
│   │   │   ├── Room.cs
│   │   │   ├── Round.cs
│   │   │   ├── RoundScore.cs
│   │   │   └── Vote.cs
│   │   ├── Enums/
│   │   │   ├── CardSource.cs        ← includes Custom
│   │   │   ├── RoomStatus.cs
│   │   │   ├── RoundStatus.cs
│   │   │   └── VoteType.cs
│   │   └── Interfaces/
│   │       ├── IAIPromptService.cs
│   │       ├── IGameService.cs
│   │       ├── IGiphyService.cs
│   │       ├── IMemeCardService.cs
│   │       └── IRoomService.cs
│   └── MemeMayhem.Infrastructure/
│       ├── Data/
│       │   ├── DbSeeder.cs          ← seeds meme cards from JSON
│       │   ├── MemeMayhemDbContext.cs
│       │   └── Configurations/
│       │       ├── CardPlayConfiguration.cs
│       │       ├── PlayerConfiguration.cs
│       │       ├── RoomConfiguration.cs
│       │       ├── RoundConfiguration.cs
│       │       └── VoteConfiguration.cs
│       │   └── Seeds/
│       │       └── meme_cards.json  ← 25 curated reaction images
│       ├── Migrations/
│       └── Services/
│           ├── AIPromptService.cs
│           ├── GameService.cs
│           ├── GiphyService.cs
│           ├── MemeCardService.cs
│           ├── RoomService.cs
│           ├── StartupSyncService.cs
│           └── SupabaseStorageService.cs  ← signed URL generation
└── frontend/
    └── src/
        ├── components/
        │   ├── game/
        │   │   ├── CardHand.tsx
        │   │   ├── CardReveal.tsx    ← spotlight mode with arrow nav
        │   │   ├── GameBoard.tsx
        │   │   ├── RoundResults.tsx
        │   │   ├── ScoreBoard.tsx    ← leaderboard style
        │   │   ├── TurnTimer.tsx
        │   │   └── VotePanel.tsx     ← horizontal reaction bar
        │   └── lobby/
        │       └── WaitingRoom.tsx
        ├── context/
        │   └── GameContext.tsx
        ├── hooks/
        │   ├── useGame.ts
        │   └── useSignalR.ts
        ├── pages/
        │   ├── Game.tsx
        │   ├── Home.tsx
        │   └── Results.tsx
        ├── types/
        │   └── game.ts
        └── App.tsx
```

---

## Database Schema (PostgreSQL — Supabase)

### Tables
- **Rooms** — game session, join code, host, theme, status
- **Players** — nickname, connectionId, isHost, isSpectator, totalScore
- **MemeCards** — curated reaction images with StoragePath (Supabase bucket path)
- **PlayerCards** — player's current hand (isPlayed flag)
- **ReactionGifs** — Giphy GIF pool per vote type
- **Rounds** — prompt, turnOrder (JSON), currentTurnIndex
- **CardPlays** — one card submission per turn
- **Votes** — one vote per voter per cardPlay
- **RoundScores** — points earned per player per round

### Connection
```
Host: aws-1-ap-south-1.pooler.supabase.com
Port: 5432 (session pooler — NOT 6543 transaction pooler)
Database: postgres
Username: postgres.toobszwgcbuswbbuayts
SSL: required
```

> ⚠️ Use port 5432 (session pooler) for DDL operations.
> Port 6543 (transaction pooler) causes timeout on CREATE TABLE.

### DB Reset Command (for testing)
Run in Supabase SQL Editor to wipe game data and reseed:
```sql
DELETE FROM "PlayerCards";
DELETE FROM "CardPlays";
DELETE FROM "Votes";
DELETE FROM "RoundScores";
DELETE FROM "Rounds";
DELETE FROM "Rooms";
DELETE FROM "Players";
DELETE FROM "MemeCards";
```

---

## Supabase Storage — Meme Cards

### Bucket Setup
- **Bucket name:** `meme-cards`
- **Visibility:** Private (not public)
- **Files:** `reaction001.jpg` through `reaction025.jpg` (25 hand-curated reaction images)

### Storage Policy
A policy must exist on the `meme-cards` bucket or signed URL generation returns 400:
```
Policy name:       service_role_access
Allowed operation: ALL
Target roles:      (empty — defaults to all public roles)
Policy definition: true
```

### Signed URL Flow
```
GameService.GetPlayerHandAsync()
  → SupabaseStorageService.GetSignedUrlAsync(storagePath)
  → POST /storage/v1/object/sign/meme-cards/{filename}
  → Returns signed URL valid for 1 hour
  → Sent to frontend via HandDealt event
```

### Seed File Format
`MemeMayhem.Infrastructure/Data/Seeds/meme_cards.json`:
```json
[
  {
    "id": "reaction-001",
    "label": "Hide the Pain Harold",
    "storagePath": "reaction001.jpg"
  }
]
```

---

## Environment Config

### appsettings.Development.json (gitignored)
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=aws-1-ap-south-1.pooler.supabase.com;Port=5432;Database=postgres;Username=postgres.toobszwgcbuswbbuayts;Password=YOUR-PASSWORD;SSL Mode=Require;Trust Server Certificate=true"
  },
  "Groq": {
    "ApiKey": "gsk_your-key-here"
  },
  "Giphy": {
    "ApiKey": "your-giphy-key-here"
  },
  "Supabase": {
    "Url": "https://toobszwgcbuswbbuayts.supabase.co",
    "ServiceRoleKey": "your-service-role-key-here"
  }
}
```

---

## Key Technical Decisions

| Decision | Choice | Reason |
|---|---|---|
| AI Prompts | Groq (Llama3) | Free tier, fast inference |
| Meme images | Hand-curated + Supabase private storage | Full control, no external API dependency |
| Signed URLs | SupabaseStorageService | Private bucket security, 1hr expiry |
| Card seeding | DbSeeder + meme_cards.json | Predictable, version-controlled card deck |
| Reaction GIFs | Giphy API | Free key, large library |
| State management | React Context + useReducer | No extra packages |
| Animations | Framer Motion | Medium animation level |
| Styling | Tailwind v3 | Utility-first, fast |
| DB | PostgreSQL (Supabase) | Free, impressive on resume |
| Game over check | results.IsGameOver (not re-fetched room) | Avoids stale data bug at round 9/10 |

---

## Backend Port
```
http://localhost:5235  ← backend
http://localhost:5173  ← frontend
```

SignalR Hub URL: `http://localhost:5235/hubs/game`

---

## UI Design Decisions

### Layout
```
┌─────────────────────────────────────────────┐
│  Round 3/10    [PROMPT TEXT]         ⏱ 12s  │  ← top bar
├──────────┬──────────────────────────────────┤
│          │                                  │
│ LEADER   │   SPOTLIGHT CARD REVEAL          │
│ BOARD    │   [ONE BIG CARD + ARROWS]        │
│          │   < PlayerName >  >              │
│ #1 👑    │   😂 😂 💀                       │
│ #2 ●     │   ● ● ○ ○  (dot nav)            │
│ #3       │                                  │
│          ├──────────────────────────────────┤
│          │  😂 Haha  💀 Lmao  😐 Meh       │  ← vote bar
│          ├──────────────────────────────────┤
│          │  ✨ Your Turn — pick a card!     │  ← turn badge
│          │  [card][card][card][card][card]  │  ← hand (glows)
└──────────┴──────────────────────────────────┘
```

### Component Decisions
| Component | Design Choice |
|---|---|
| Background | Light — white/soft gray (`bg-gray-50`) |
| CardReveal | Spotlight — one card at a time, left/right arrows, dot nav |
| ScoreBoard | Leaderboard — crown on #1, animated dot on current player's turn |
| VotePanel | Horizontal reaction bar above hand — 3 tiles, locks after vote |
| Turn indicator | Floating badge + hand glow (red pulse when your turn) |
| Card image ratio | `aspect-[4/3]` with `object-contain`, max height 260px |

---

## Known Issues & Fixes

### 1. SignalR payload mismatch
Backend sends `code` but frontend expected `roomCode`.

**Fix in useSignalR.ts:**
```typescript
connection.on('RoomCreated', (d) => {
  dispatch({
    type: 'ROOM_CREATED',
    payload: {
      roomId:   d.roomId,
      roomCode: d.code,      // ← backend sends 'code'
      playerId: d.playerId
    }
  })
})
```

### 2. StartupSyncService scope error
`IHostedService` (singleton) can't use scoped `DbContext` directly.

**Fix:** Use `IServiceScopeFactory` instead.

### 3. Supabase transaction pooler timeout
Port 6543 doesn't support DDL (CREATE TABLE).

**Fix:** Use session pooler port 5432 for migrations.

### 4. Npgsql version incompatibility
Npgsql v10 requires .NET 10. Project uses .NET 9.

**Fix:** Install specific version:
```bash
dotnet add package Npgsql.EntityFrameworkCore.PostgreSQL --version 9.0.4
```

### 5. Tailwind install on Windows PowerShell
`./node_modules/.bin/tailwindcss` doesn't work on PowerShell.

**Fix:**
```powershell
npm install -D tailwindcss@3 postcss autoprefixer
npx tailwindcss init -p
```

### 6. VoteType enum inline comments
Comments on enum values cause CS parse errors.

**Fix:** Remove inline comments from enum values.

### 7. Supabase Storage 400 on signed URL
Two causes found:
- Missing storage policy on private bucket → add policy with `true` definition
- `StoragePath` in DB contained full Imgflip URLs instead of filenames → wipe MemeCards table and reseed

### 8. Cards not showing in hand (loading screen after StartGame)
`HandDealt` was sent after `GameStarted`, so UI transitioned before cards existed.

**Fix:** Reorder events in `StartGame` — HandDealt → RoundStarted → GameStarted.

### 9. Game stops at round 9 of 10
`EndRoundAsync` re-fetched stale room data to check game over. Room's `CurrentRound` hadn't been incremented yet.

**Fix:** Use `results.IsGameOver` directly from `EndRoundAsync` return value instead of re-querying the room.

### 10. Old Imgflip data in MemeCards table
`StoragePath` column contained full Imgflip URLs from before migration.

**Fix:** Wipe all tables via SQL editor and restart backend to reseed from `meme_cards.json`.

---

## PowerShell Commands (Windows specific)

```powershell
# Delete folder (not rmdir /s /q)
Remove-Item -Recurse -Force FolderName

# Delete file
Remove-Item filename.json

# Find in directory
ls node_modules\.bin | findstr tailwind

# EF migrations — run from backend folder with single quotes for spaces in path
cd 'C:\Users\NOC-02\Documents\Kabbo Essentials (Do Not Delete)\Meme-Mayhem\backend'

dotnet ef migrations add MigrationName `
  --project MemeMayhem.Infrastructure `
  --startup-project MemeMayhem.API

dotnet ef database update `
  --project MemeMayhem.Infrastructure `
  --startup-project MemeMayhem.API
```

---

## Git Workflow
```
main   ← stable, working version
dev    ← active development

# Daily workflow
git checkout dev
git add .
git commit -m "feat: description"
git push origin dev

# When stable
git checkout main
git merge dev
git push origin main
git checkout dev
```

### Commit prefixes
- `feat:` new feature
- `fix:` bug fix
- `refactor:` code cleanup
- `chore:` setup/config
- `docs:` documentation

---

## Run Commands

```powershell
# Backend
cd 'C:\Users\NOC-02\Documents\Kabbo Essentials (Do Not Delete)\Meme-Mayhem\backend\MemeMayhem.API'
dotnet run

# Frontend
cd 'C:\Users\NOC-02\Documents\Kabbo Essentials (Do Not Delete)\Meme-Mayhem\frontend'
npm run dev
```

---

## Current Status (as of this session)
- ✅ Full backend built and running
- ✅ PostgreSQL connected via Supabase
- ✅ All tables migrated including StoragePath column
- ✅ Frontend built — Home, WaitingRoom, GameBoard, Results
- ✅ SignalR connected and event ordering fixed
- ✅ Imgflip replaced with hand-curated images on Supabase private storage
- ✅ DbSeeder seeding 25 reaction cards from meme_cards.json
- ✅ SupabaseStorageService generating signed URLs per card deal
- ✅ Supabase storage policy configured
- ✅ Game loop tested end to end — all 10 rounds complete correctly
- ✅ Game over bug fixed (round 9/10 issue)
- ✅ UI redesigned — spotlight card, leaderboard, reaction bar, turn indicator
- ✅ Dark humor prompts expanded to 100 entries across 7 categories

## Next Steps
1. UI polish — card hand floating, spotlight card size, caption font size
2. Mobile responsiveness
3. Deploy to production
