import { useGame } from '../hooks/useGame'
import WaitingRoom from '../components/lobby/WaitingRoom'
import GameBoard from '../game/GameBoard'

export default function Game() {
  const { state } = useGame()

  if (state.roomStatus === 'Lobby')  return <WaitingRoom />
  if (state.roomStatus === 'Active') return <GameBoard />

  return <div>Game Ended</div>
}