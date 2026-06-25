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
        players: state.players.filter((p: Player) => p.id !== action.payload)
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
        currentRound: {
          ...state.currentRound,
          cardPlays: [
            ...state.currentRound.cardPlays,
            action.payload
          ]
        }
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
        players: state.players.map((p: Player) => ({
          ...p,
          isHost: p.id === action.payload
        }))
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

export function GameProvider({ children }: { children: ReactNode }) {
  const [state, dispatch] = useReducer(gameReducer, initialState)
  const [connection, setConnection] = useState<signalR.HubConnection | null>(null)

  useSignalR({ state, dispatch, connection, setConnection })

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