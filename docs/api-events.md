┌─────────────────────────────────────────────────────────┐
│                  SIGNALR EVENT MAP                      │
├──────────────────────────┬──────────────────────────────┤
│  CLIENT → SERVER         │  SERVER → CLIENT             │
├──────────────────────────┼──────────────────────────────┤
│  CreateRoom              │  RoomCreated                 │
│  JoinRoom                │  PlayerJoined                │
│  SpectateRoom            │  SpectatorJoined             │
│  PickTheme               │  ThemeSelected               │
│  PickRounds              │  RoundsSelected              │
│  StartGame               │  GameStarted                 │
│                          │  HandDealt (private)         │
│                          │  RoundStarted                │
│                          │  YourTurn (private)          │
│                          │  TurnStarted (current player │
│                          │  shown, order hidden)        │
│  SubmitCard              │  CardRevealed (broadcast)    │
│                          │  TurnTimerStarted            │
│                          │  TurnSkipped (AFK/disconnect)│
│  SubmitVote              │  VoteReceived (broadcast)    │
│                          │  TurnEnded                   │
│                          │  RoundEnded                  │
│                          │  ScoreUpdated                │
│                          │  NewCardDealt (private)      │
│                          │  NextRoundStarted            │
│                          │  HostChanged                 │
│                          │  PlayerDisconnected          │
│                          │  PlayerReconnected           │
│                          │  GameOver                    │
└──────────────────────────┴──────────────────────────────┘