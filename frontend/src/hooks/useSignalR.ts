import { useEffect, useRef, useCallback } from 'react'
import * as signalR from '@microsoft/signalr'
import { useGameContext } from '../context/GameContext'

const HUB_URL = 'http://localhost:5235/hubs/game'

export function useSignalR() {
  const { state, dispatch, connection, setConnection } = useGameContext()
  const stateRef = useRef(state)

  useEffect(() => {
    stateRef.current = state
  }, [state])

  const connect = useCallback(async () => {
    if (connection) return

    console.log('Connecting to SignalR...')
    const conn = new signalR.HubConnectionBuilder()
      .withUrl(HUB_URL)
      .withAutomaticReconnect()
      .build()

    conn.on('RoomCreated', (data) => {
      console.log('RoomCreated event received:', data)
      dispatch({
        type: 'ROOM_CREATED',
        payload: data
      })
    })

    conn.on('RoomJoined', (data) => {
      console.log('RoomJoined event received:', data)
      dispatch({
        type: 'ROOM_JOINED',
        payload: data
      })
    })

    conn.on('PlayerJoined', d => dispatch({ type: 'PLAYER_JOINED', payload: d }))
    conn.on('PlayerDisconnected', d => dispatch({ type: 'PLAYER_LEFT', payload: d.playerId }))
    conn.on('HostChanged', d => dispatch({ type: 'HOST_CHANGED', payload: d.newHostId }))
    conn.on('GameStarted', () => dispatch({ type: 'GAME_STARTED' }))
    conn.on('HandDealt', d => dispatch({ type: 'HAND_DEALT', payload: d }))
    conn.on('RoundStarted', d => dispatch({ type: 'ROUND_STARTED', payload: d }))
    conn.on('YourTurn', () => dispatch({ type: 'YOUR_TURN' }))
    conn.on('CardRevealed', d => dispatch({ type: 'CARD_REVEALED', payload: d }))
    conn.on('VoteReceived', d => dispatch({ type: 'VOTE_RECEIVED', payload: d }))
    conn.on('TurnEnded', d => dispatch({ type: 'TURN_ENDED', payload: d }))
    conn.on('TurnSkipped', d => dispatch({ type: 'TURN_SKIPPED', payload: d.playerId }))
    conn.on('RoundEnded', d => dispatch({ type: 'ROUND_ENDED', payload: d }))
    conn.on('NewCardDealt', d => dispatch({ type: 'NEW_CARD_DEALT', payload: d }))
    conn.on('GameOver', d => dispatch({ type: 'GAME_OVER', payload: d.finalScores }))
    conn.on('Error', d => dispatch({ type: 'SET_ERROR', payload: d }))

    conn.onreconnecting(() => dispatch({ type: 'SET_CONNECTED', payload: false }))
    
    conn.onreconnected(async () => {
      dispatch({ type: 'SET_CONNECTED', payload: true })
      const { roomCode, playerId } = stateRef.current
      if (roomCode && playerId) {
        try {
          await conn.invoke('Reconnect', roomCode, playerId)
          console.log('Reconnected successfully to room:', roomCode)
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
    }
  }, [connection, dispatch, setConnection])

  useEffect(() => {
    connect()
  }, [connect])

  const connectionRef = useRef<signalR.HubConnection | null>(null)
  connectionRef.current = connection
  return connectionRef
}