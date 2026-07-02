import {
  createContext,
  useContext,
  useReducer,
  type ReactNode,
  useState,
} from 'react'
import * as signalR from '@microsoft/signalr'
import type { GameState, GameAction, Player, CardPlay } from '../../types/game'
import { useSignalR } from '../hooks/useSignalR'

const initialState: GameState = {
  isConnected: false,
  roomId: null,
  roomCode: null,
  roomStatus: 'Lobby',
  playerId: null,
  isHost: false,
  isSpectator: false,
  players: [],
  currentRound: null,
  hand: [],
  selectedCardId: null,
  roundResults: null,
  isMyTurn: false,
  totalRounds: 0,
  currentRoundNumber: 0,
  error: null,
  theme: null,
  voteTimerSeconds: null,
  activeCardPlayId: null,
}

function gameReducer(state: GameState, action: GameAction): GameState {
  switch (action.type) {

    case 'SET_CONNECTED':
      return { ...state, isConnected: action.payload }

    case 'ROOM_CREATED':
      return {
        ...state,
        roomId: action.payload.roomId,
        roomCode: action.payload.code,
        playerId: action.payload.playerId,
        isHost: true,
        roomStatus: 'Lobby',
        players: action.payload.players,
        totalRounds: action.payload.totalRounds,
        theme: action.payload.theme
      }

    case 'ROOM_JOINED':
      return {
        ...state,
        roomId: action.payload.roomId,
        roomCode: action.payload.code,
        playerId: action.payload.playerId,
        isHost: action.payload.isHost,
        roomStatus: 'Lobby',
        players: action.payload.players,
        totalRounds: action.payload.totalRounds,
        theme: action.payload.theme
      }

    case 'PLAYER_JOINED':
      // Avoid duplicate players
      if (state.players.find((p: Player) => p.id === action.payload.id))
        return state
      return {
        ...state,
        players: [...state.players, action.payload]
      }

    case 'PLAYER_LEFT':
      return {
        ...state,
        players: state.players.map((p: Player) =>
          p.id === action.payload ? { ...p, isConnected: false } : p
        ),
      }

    case 'PLAYER_RECONNECTED':
      return {
        ...state,
        players: state.players.map((p: Player) =>
          p.id === action.payload ? { ...p, isConnected: true } : p
        ),
      }

    case 'GAME_STARTED':
      return { ...state, roomStatus: 'Active' }

    case 'HAND_DEALT':
      return { ...state, hand: action.payload }

    case 'ROUND_STARTED':
      return {
        ...state,
        roomStatus: 'Active',
        currentRound: {
          ...action.payload,
          cardPlays: action.payload.cardPlays ?? [],
        },
        currentRoundNumber: action.payload.roundNumber,
        roundResults: null,
        isMyTurn: false,
        selectedCardId: null,
        voteTimerSeconds: null,
        activeCardPlayId: null,
      }

    case 'YOUR_TURN':
      return { ...state, isMyTurn: true }

    case 'SELECT_CARD':
      return { ...state, selectedCardId: action.payload }

    case 'CARD_REVEALED':
      if (!state.currentRound) return state
      return {
        ...state,
        isMyTurn: false,
        activeCardPlayId: action.payload.id,
        currentRound: {
          ...state.currentRound,
          cardPlays: [
            ...state.currentRound.cardPlays,
            action.payload
          ]
        }
      }

    case 'VOTE_TIMER_STARTED':
      return {
        ...state,
        voteTimerSeconds: action.payload,
      }

    case 'VOTE_RECEIVED':
      if (!state.currentRound) return state
      return {
        ...state,
        currentRound: {
          ...state.currentRound,
          cardPlays: state.currentRound.cardPlays.map((cp: CardPlay) =>
            cp.id === action.payload.cardPlayId
              ? { ...cp, votes: [...cp.votes, action.payload.vote] }
              : cp
          )
        }
      }

    case 'ROUND_ENDED':
      return {
        ...state,
        roundResults: action.payload,
        isMyTurn: false,
        voteTimerSeconds: null,
      }

    case 'TURN_ENDED':
      return { ...state, voteTimerSeconds: null, activeCardPlayId: null }

    case 'TURN_STARTED':
      if (!state.currentRound) return state
      return {
        ...state,
        isMyTurn: false,
        selectedCardId: null,
        voteTimerSeconds: null,
        activeCardPlayId: null,
        currentRound: {
          ...state.currentRound,
          currentPlayerId: action.payload.currentPlayerId,
          currentTurnIndex: action.payload.turnIndex,
          totalTurns: action.payload.totalTurns,
        },
      }

    case 'TURN_SKIPPED':
      if (!state.currentRound) return state
      return {
        ...state,
        isMyTurn: false,
        selectedCardId: null,
        voteTimerSeconds: null,
        activeCardPlayId: null,
      }

    case 'NEW_CARD_DEALT':
      return { ...state, hand: action.payload }

    case 'GAME_OVER':
      return { ...state, roomStatus: 'Ended' }

    case 'SET_ERROR':
      return { ...state, error: action.payload }

    case 'CLEAR_ERROR':
      return { ...state, error: null }

    case 'HOST_CHANGED':
      return {
        ...state,
        isHost: state.playerId === action.payload,
        players: state.players.map(p =>
          p.id === action.payload
            ? { ...p, isHost: true }
            : { ...p, isHost: false }
        )
      }

    case 'GAME_STATE_SYNC':
      return {
        ...state,
        roomId: action.payload.roomId,
        roomCode: action.payload.roomCode,
        roomStatus: action.payload.roomStatus,
        playerId: action.payload.playerId,
        isHost: action.payload.isHost,
        isSpectator: action.payload.isSpectator,
        theme: action.payload.theme,
        totalRounds: action.payload.totalRounds,
        currentRoundNumber: action.payload.currentRoundNumber,
        players: action.payload.players,
        currentRound: action.payload.currentRound || null,
        hand: action.payload.hand,
        isMyTurn: action.payload.isMyTurn,
        voteTimerSeconds: action.payload.voteTimerSeconds ?? null,
        activeCardPlayId: action.payload.activeCardPlayId ?? null,
        roundResults: null,
        selectedCardId: null,
        error: null,
      }

    default:
      return state
  }
}

const GameContext = createContext<{
  state: GameState
  dispatch: React.Dispatch<GameAction>
  connection: signalR.HubConnection | null
  setConnection: (conn: signalR.HubConnection | null) => void
} | null>(null)

import { useToast } from '../components/ui/ToastProvider'

export function GameProvider({ children }: { children: ReactNode }) {
  const [state, dispatch] = useReducer(gameReducer, initialState)
  const [connection, setConnection] = useState<signalR.HubConnection | null>(null)
  const { addToast } = useToast()

  useSignalR({ state, dispatch, connection, setConnection, addToast })

  return (
    <GameContext.Provider value={{ state, dispatch, connection, setConnection }}>
      {children}
    </GameContext.Provider>
  )
}

export function useGameContext() {
  const context = useContext(GameContext)
  if (!context)
    throw new Error('useGameContext must be used within GameProvider')
  return context
}