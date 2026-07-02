import { useEffect, useRef, useCallback } from 'react'
import * as signalR from '@microsoft/signalr'
import type { GameAction, GameState } from '../../types/game'

import type { ToastType } from '../components/ui/ToastProvider'

const HUB_URL = 'http://localhost:5235/hubs/game'

type UseSignalRArgs = {
  state: GameState
  dispatch: React.Dispatch<GameAction>
  connection: signalR.HubConnection | null
  setConnection: (conn: signalR.HubConnection | null) => void
  addToast: (message: string, type?: ToastType) => void
}

export function useSignalR({ state, dispatch, connection, setConnection, addToast }: UseSignalRArgs) {
  const connectingRef = useRef(false)
  const stateRef = useRef(state)

  useEffect(() => {
    stateRef.current = state
  }, [state])

  const connect = useCallback(async () => {
    if (connection || connectingRef.current) return

    connectingRef.current = true

    const conn = new signalR.HubConnectionBuilder()
      .withUrl(HUB_URL)
      .withAutomaticReconnect()
      .build()

    conn.on('RoomCreated', (data) => {
      dispatch({ type: 'ROOM_CREATED', payload: data })
    })

    conn.on('RoomJoined', (data) => {
      dispatch({ type: 'ROOM_JOINED', payload: data })
    })

    conn.on('PlayerJoined', (data) => {
      dispatch({ type: 'PLAYER_JOINED', payload: data })
      addToast(`${data.nickname} joined the room`, 'info')
    })
    conn.on('PlayerDisconnected', (data) => {
      dispatch({ type: 'PLAYER_LEFT', payload: data.playerId })
      addToast(`${data.nickname} disconnected`, 'warning')
    })
    conn.on('PlayerTimedOut', (data) => {
      addToast(`${data.nickname} timed out and was removed`, 'error')
      // Note: Backend might send another PlayerDisconnected, or we just handle it here.
      // But we will at least show the toast.
    })
    conn.on('HostChanged', (data) => {
      dispatch({ type: 'HOST_CHANGED', payload: data.newHostId })
      const { playerId } = stateRef.current
      if (playerId === data.newHostId) {
        addToast('You are the new host', 'success')
      } else {
        addToast(`${data.nickname} is now the host`, 'info')
      }
    })
    conn.on('GameStarted', () => dispatch({ type: 'GAME_STARTED' }))
    conn.on('HandDealt', (data) =>
      dispatch({ type: 'HAND_DEALT', payload: data }))
    conn.on('RoundStarted', (data) =>
      dispatch({ type: 'ROUND_STARTED', payload: data }))
    conn.on('YourTurn', () => dispatch({ type: 'YOUR_TURN' }))
    conn.on('CardRevealed', (data) =>
      dispatch({ type: 'CARD_REVEALED', payload: data }))
    conn.on('VoteTimerStarted', (data) =>
      dispatch({ type: 'VOTE_TIMER_STARTED', payload: data.seconds }))
    conn.on('TurnStarted', (data) =>
      dispatch({ type: 'TURN_STARTED', payload: data }))
    conn.on('TurnEnded', (data) =>
      dispatch({ type: 'TURN_ENDED', payload: data.turnIndex }))
    conn.on('TurnSkipped', (data) => {
      dispatch({ type: 'TURN_SKIPPED', payload: data.playerId })
      const { players } = stateRef.current
      const skippedPlayer = players.find(p => p.id === data.playerId)
      if (skippedPlayer) {
        addToast(`${skippedPlayer.nickname}'s turn was skipped (${data.reason})`, 'warning')
      }
    })
    conn.on('VoteReceived', (data) =>
      dispatch({ type: 'VOTE_RECEIVED', payload: data }))
    conn.on('RoundEnded', (data) =>
      dispatch({ type: 'ROUND_ENDED', payload: data }))
    conn.on('NewCardDealt', (hand) =>
      dispatch({ type: 'NEW_CARD_DEALT', payload: hand }))
    conn.on('GameStateSync', (data) =>
      dispatch({ type: 'GAME_STATE_SYNC', payload: data }))
    conn.on('PlayerReconnected', (data) => {
      dispatch({ type: 'PLAYER_RECONNECTED', payload: data.playerId })
      addToast(`${data.nickname} reconnected`, 'success')
    })

    conn.on('GameOver', (data) =>
      dispatch({ type: 'GAME_OVER', payload: data.finalScores }))
    conn.on('Error', (message) =>
      dispatch({ type: 'SET_ERROR', payload: message }))

    conn.onreconnecting(() =>
      dispatch({ type: 'SET_CONNECTED', payload: false }))

    conn.onreconnected(async () => {
      dispatch({ type: 'SET_CONNECTED', payload: true })
      const { roomCode, playerId } = stateRef.current
      if (roomCode && playerId) {
        try {
          await conn.invoke('Reconnect', roomCode, playerId)
        } catch (err) {
          console.error('Failed to reconnect player:', err)
        }
      }
    })

    conn.onclose(() => dispatch({ type: 'SET_CONNECTED', payload: false }))

    try {
      await conn.start()
      dispatch({ type: 'SET_CONNECTED', payload: true })
      setConnection(conn)
    } catch (err) {
      console.error('Failed to start SignalR connection:', err)
    } finally {
      connectingRef.current = false
    }
  }, [connection, dispatch, setConnection])

  useEffect(() => {
    connect()
  }, [connect])
}
