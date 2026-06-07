import { useEffect } from 'react'
import { useNavigate } from 'react-router-dom'
import { useGame } from '../hooks/useGame'
import WaitingRoom from '../components/lobby/WaitingRoom'
import GameBoard from '../game/GameBoard'

export default function Game() {
  const { state } = useGame()
  const navigate = useNavigate()

  //Redirect to results when game ends
  useEffect(() => {
    if (state.roomStatus === 'Ended') {
      navigate('/results')
    }
  }, [state.roomStatus, navigate])

  if (state.roomStatus === 'Lobby') return <WaitingRoom />
  if (state.roomStatus === 'Active') return <GameBoard />

  return <div>Game Ended</div>
}