┌─────────────────────────────────────────────────────────┐
│                     GAME RULES                          │
├──────────────────────────┬──────────────────────────────┤
│  Players                 │  3 to 10 (2 allowed, 3+ rec) │
│  Join method             │  Private room code           │
│  Rounds                  │  Host picks                  │
│  Theme                   │  Host picks per game         │
│  Hand size               │  Always 5 cards              │
│  Turn timer              │  15 seconds                  │
│  Voting timer            │  None                        │
│  Turn order              │  Random each round           │
│  Turn order visibility   │  Hidden (current only shown) │
│  Voting while waiting    │  Allowed                     │
│  Mid-round scores        │  Visible (last player adv.)  │
│  Self voting             │  Not allowed                 │
│  Haha                    │  1 point                     │
│  Lmao                    │  5 points                    │
│  Meh                     │  0 points                    │
│  Card after round        │  Draw 1 → back to 5          │
│  Deck exhausted          │  Reshuffle discards          │
│  Disconnect grace        │  30 seconds                  │
│  Disconnect action       │  Auto skip turn              │
│  Host leaves             │  Auto promote next player    │
│  Late joiners            │  Spectator mode              │
│  Minimum fun players     │  3+                          │
└──────────────────────────┴──────────────────────────────┘

┌─────────────────────────────────────────────────────────┐
│                   ROUND LIFECYCLE                       │
├─────────────────────────────────────────────────────────┤
│                                                         │
│  1. ROUND START                                         │
│     └── Turn order randomized (hidden)                 │
│     └── AI prompt generated & shown (stays all round)  │
│     └── Everyone has 5 cards                           │
│                                                         │
│  2. TURN (repeats for each player)                      │
│     └── Current player notified privately              │
│     └── 15s timer starts                               │
│     └── Player picks 1 card → revealed to all         │
│     └── Everyone else votes (Haha / Lmao / Meh)       │
│     └── Turn ends when all (n-1) votes cast            │
│     └── If 15s expires → player auto skipped          │
│     └── If player disconnects → 30s grace             │
│         └── No reconnect → auto skipped               │
│                                                         │
│  3. ROUND END                                           │
│     └── All points tallied                             │
│     └── Results shown on scoreboard                    │
│     └── 1 new card dealt → back to 5                  │
│     └── If deck empty → reshuffle discards            │
│     └── Next round starts OR game over                 │
│                                                         │
│  4. GAME OVER (after all rounds)                        │
│     └── Final scoreboard shown                         │
│     └── Winner announced                               │
│     └── Play again option                              │
│                                                         │
└─────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────┐
│                   PLAYER STATES                         │
├─────────────────────────────────────────────────────────┤
│                                                         │
│  Connected ──→ Lobby ──→ Playing ──→ Spectator         │
│                  │                        ↑             │
│                  │    Disconnect          │             │
│                  └──→ Reconnecting ───────┘             │
│                         (30s grace)   if failed        │
│                                                         │
│  Host leaves ──→ Next player auto-promoted             │
│  Late join   ──→ Spectator (watch only)                │
│                                                         │
└─────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────┐
│                 GAME STATE MACHINE                      │
├─────────────────────────────────────────────────────────┤
│                                                         │
│  Lobby                                                  │
│  └──→ [Host starts] ──→ GameStarted                   │
│                                                         │
│  GameStarted                                            │
│  └──→ Deal 5 cards ──→ RoundStarted                   │
│                                                         │
│  RoundStarted                                           │
│  └──→ Randomize order ──→ TurnStarted                 │
│                                                         │
│  TurnStarted                                            │
│  ├──→ [Card submitted] ──→ CardRevealed               │
│  │    └──→ [All votes cast] ──→ TurnEnded             │
│  └──→ [15s expired / disconnect] ──→ TurnSkipped      │
│                                                         │
│  TurnEnded / TurnSkipped                                │
│  ├──→ [Players remaining] ──→ TurnStarted             │
│  └──→ [All players done] ──→ RoundEnded               │
│                                                         │
│  RoundEnded                                             │
│  ├──→ [Rounds remaining] ──→ RoundStarted             │
│  └──→ [All rounds done] ──→ GameOver                  │
│                                                         │
│  GameOver                                               │
│  └──→ [Play again] ──→ Lobby                          │
│                                                         │
└─────────────────────────────────────────────────────────┘