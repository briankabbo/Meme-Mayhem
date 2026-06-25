import { useEffect, useRef, useCallback } from 'react'
import * as signalR from '@microsoft/signalr'
import type { GameAction, GameState } from '../../types/game'

const HUB_URL = 'http://localhost:5235/hubs/game'

type UseSignalRArgs = {
  state: GameState
  dispatch: React.Dispatch<GameAction>
  connection: signalR.HubConnection | null
  setConnection: (conn: signalR.HubConnection | null) => void
}

export function useSignalR({ state, dispatch, connection, setConnection }: UseSignalRArgs) {
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

    conn.on('PlayerJoined', (data) =>
      dispatch({ type: 'PLAYER_JOINED', payload: data }))
    conn.on('PlayerDisconnected', (data) =>
      dispatch({ type: 'PLAYER_LEFT', payload: data.playerId }))
    conn.on('HostChanged', (data) =>
      dispatch({ type: 'HOST_CHANGED', payload: data.newHostId }))
    conn.on('GameStarted', () => dispatch({ type: 'GAME_STARTED' }))
    conn.on('HandDealt', (data) =>
      dispatch({ type: 'HAND_DEALT', payload: data }))
    conn.on('RoundStarted', (data) =>
      dispatch({ type: 'ROUND_STARTED', payload: data }))
    conn.on('YourTurn', () => dispatch({ type: 'YOUR_TURN' }))
    conn.on('CardRevealed', (data) =>
      dispatch({ type: 'CARD_REVEALED', payload: data }))
    conn.on('VoteReceived', (data) =>
      dispatch({ type: 'VOTE_RECEIVED', payload: data }))
    conn.on('RoundEnded', (data) =>
      dispatch({ type: 'ROUND_ENDED', payload: data }))
    conn.on('NewCardDealt', (data) =>
      dispatch({ type: 'NEW_CARD_DEALT', payload: data }))
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
