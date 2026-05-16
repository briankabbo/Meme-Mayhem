import { useCallback } from 'react'
import { useGameContext } from '../context/GameContext'
import { useSignalR } from './useSignalR'

export function useGame() {
  const { state, dispatch } = useGameContext()
  const connectionRef = useSignalR()

  const invoke = useCallback(async (
    method: string, ...args: unknown[]
  ) => {
    try {
      await connectionRef.current?.invoke(method, ...args)
    } catch (err) {
      dispatch({ type: 'SET_ERROR', payload: `${err}` })
    }
  }, [connectionRef, dispatch])

  return {
    state,
    dispatch,
    createRoom:  (nickname: string, theme: string, rounds: number) =>
      invoke('CreateRoom', nickname, theme, rounds),
    joinRoom:    (code: string, nickname: string) =>
      invoke('JoinRoom', code, nickname),
    startGame:   () =>
      invoke('StartGame', state.roomId),
    submitCard:  (cardId: string) => {
      dispatch({ type: 'SELECT_CARD', payload: cardId })
      invoke('SubmitCard', state.currentRound?.id, cardId)
    },
    submitVote:  (cardPlayId: string, voteType: string) =>
      invoke('SubmitVote', cardPlayId, voteType),
    clearError:  () =>
      dispatch({ type: 'CLEAR_ERROR' }),
  }
}