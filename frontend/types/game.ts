// src/types/game.ts

export type VoteType = 'Haha' | 'Lmao' | 'Meh'
export type RoomStatus = 'Lobby' | 'Active' | 'Ended'
export type RoundStatus = 'CardPicking' | 'Voting' | 'Results' | 'Completed'

export interface MemeCard {
  id: string
  label: string
  imageUrl: string
}

export interface Player {
  id: string
  nickname: string
  isHost: boolean
  isSpectator: boolean
  isConnected: boolean
  totalScore: number
}

export interface Vote {
  voterId: string
  voterName: string
  voteType: VoteType
  points: number
}

export interface CardPlay {
  id: string
  playerId: string
  playerName: string
  card: MemeCard
  turnIndex: number
  votes: Vote[]
}

export interface Round {
  id: string
  roundNumber: number
  promptText: string
  status: RoundStatus
  currentPlayerId: string
  currentTurnIndex: number
  totalTurns: number
  cardPlays: CardPlay[]
}

export interface PlayerScore {
  playerId: string
  playerName: string
  pointsEarned: number
  runningTotal: number
  rank: number
}

export interface RoundResult {
  roundId: string
  roundNumber: number
  totalRounds: number
  isGameOver: boolean
  scores: PlayerScore[]
}

export interface GameState {
  isConnected: boolean
  roomId: string | null
  roomCode: string | null
  roomStatus: RoomStatus
  playerId: string | null
  isHost: boolean
  isSpectator: boolean
  players: Player[]
  currentRound: Round | null
  hand: MemeCard[]
  selectedCardId: string | null
  roundResults: RoundResult | null
  isMyTurn: boolean
  totalRounds: number
  currentRoundNumber: number
  error: string | null
}

export type GameAction =
  | { type: 'SET_CONNECTED'; payload: boolean }
  | { type: 'ROOM_CREATED'; payload: { roomId: string; roomCode: string; playerId: string } }
  | { type: 'ROOM_JOINED'; payload: { roomId: string; playerId: string; isHost: boolean } }
  | { type: 'PLAYER_JOINED'; payload: Player }
  | { type: 'PLAYER_LEFT'; payload: string }
  | { type: 'GAME_STARTED' }
  | { type: 'HAND_DEALT'; payload: MemeCard[] }
  | { type: 'ROUND_STARTED'; payload: Round }
  | { type: 'YOUR_TURN' }
  | { type: 'CARD_REVEALED'; payload: CardPlay }
  | { type: 'VOTE_RECEIVED'; payload: { cardPlayId: string; vote: Vote } }
  | { type: 'TURN_ENDED'; payload: number }
  | { type: 'TURN_SKIPPED'; payload: string }
  | { type: 'ROUND_ENDED'; payload: RoundResult }
  | { type: 'NEW_CARD_DEALT'; payload: MemeCard[] }
  | { type: 'SELECT_CARD'; payload: string }
  | { type: 'GAME_OVER'; payload: PlayerScore[] }
  | { type: 'SET_ERROR'; payload: string }
  | { type: 'CLEAR_ERROR' }
  | { type: 'HOST_CHANGED'; payload: string }