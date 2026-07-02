import { useEffect } from 'react'
import { useNavigate } from 'react-router-dom'
import { useGame } from '../hooks/useGame'
import WaitingRoom from '../components/lobby/WaitingRoom'
import GameBoard from '../game/GameBoard'
import ConnectionLostBanner from '../components/ui/ConnectionLostBanner'

export default function Game() {
  const { state, reconnect } = useGame()
  const navigate = useNavigate()

  //Redirect to results when game ends
  useEffect(() => {
    if (state.roomStatus === 'Ended') {
      navigate('/results')
    }
  }, [state.roomStatus, navigate])

  const showConnectionLost =
    !state.isConnected && state.roomCode !== null

  if (state.roomStatus === 'Lobby') return (
    <>
      {showConnectionLost && (
        <ConnectionLostBanner
          onRetry={() => reconnect()}
          onBack={() => navigate('/')}
        />
      )}
      <WaitingRoom />
    </>
  )

  if (state.roomStatus === 'Active') return (
    <>
      {showConnectionLost && (
        <ConnectionLostBanner
          onRetry={() => reconnect()}
          onBack={() => navigate('/')}
        />
      )}
      <GameBoard />
    </>
  )

  return <div>Game Ended</div>
}