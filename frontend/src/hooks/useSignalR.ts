import { useEffect, useRef, useCallback } from 'react'
import * as signalR from '@microsoft/signalr'
import { useGameContext } from '../context/GameContext'

const HUB_URL = 'http://localhost:5000/hubs/game'

export function useSignalR() {
  const { dispatch } = useGameContext()
  const connectionRef = useRef<signalR.HubConnection | null>(null)

  const connect = useCallback(async () => {
    const connection = new signalR.HubConnectionBuilder()
      .withUrl(HUB_URL)
      .withAutomaticReconnect()
      .build()

    connection.on('RoomCreated',        d  => dispatch({ type: 'ROOM_CREATED',   payload: d }))
    connection.on('RoomJoined',         d  => dispatch({ type: 'ROOM_JOINED',    payload: d }))
    connection.on('PlayerJoined',       d  => dispatch({ type: 'PLAYER_JOINED',  payload: d }))
    connection.on('PlayerDisconnected', d  => dispatch({ type: 'PLAYER_LEFT',    payload: d.playerId }))
    connection.on('HostChanged',        d  => dispatch({ type: 'HOST_CHANGED',   payload: d.newHostId }))
    connection.on('GameStarted',        () => dispatch({ type: 'GAME_STARTED' }))
    connection.on('HandDealt',          d  => dispatch({ type: 'HAND_DEALT',     payload: d }))
    connection.on('RoundStarted',       d  => dispatch({ type: 'ROUND_STARTED',  payload: d }))
    connection.on('YourTurn',           () => dispatch({ type: 'YOUR_TURN' }))
    connection.on('CardRevealed',       d  => dispatch({ type: 'CARD_REVEALED',  payload: d }))
    connection.on('VoteReceived',       d  => dispatch({ type: 'VOTE_RECEIVED',  payload: d }))
    connection.on('TurnEnded',          d  => dispatch({ type: 'TURN_ENDED',     payload: d }))
    connection.on('TurnSkipped',        d  => dispatch({ type: 'TURN_SKIPPED',   payload: d.playerId }))
    connection.on('RoundEnded',         d  => dispatch({ type: 'ROUND_ENDED',    payload: d }))
    connection.on('NewCardDealt',       d  => dispatch({ type: 'NEW_CARD_DEALT', payload: d }))
    connection.on('GameOver',           d  => dispatch({ type: 'GAME_OVER',      payload: d.finalScores }))
    connection.on('Error',              d  => dispatch({ type: 'SET_ERROR',      payload: d }))

    connection.onreconnecting(() => dispatch({ type: 'SET_CONNECTED', payload: false }))
    connection.onreconnected(()  => dispatch({ type: 'SET_CONNECTED', payload: true  }))
    connection.onclose(()        => dispatch({ type: 'SET_CONNECTED', payload: false }))

    await connection.start()
    dispatch({ type: 'SET_CONNECTED', payload: true })
    connectionRef.current = connection
  }, [dispatch])

  useEffect(() => {
    connect()
    return () => { connectionRef.current?.stop() }
  }, [connect])

  return connectionRef
}