import { motion } from 'framer-motion'
import { useGame } from '../hooks/useGame'
import { useNavigate } from 'react-router-dom'
import { Trophy, Star, Home, RotateCcw } from 'lucide-react'

export default function Results() {
  const { state } = useGame()
  const navigate = useNavigate()

  const sorted = [...state.players]
    .filter(p => !p.isSpectator)
    .sort((a, b) => b.totalScore - a.totalScore)

  const winner = sorted[0]

  const getRankEmoji = (index: number) => {
    if (index === 0) return '🥇'
    if (index === 1) return '🥈'
    if (index === 2) return '🥉'
    return `#${index + 1}`
  }

  const getConfettiColor = (index: number) => {
    const colors = [
      'bg-mayhem-primary',
      'bg-mayhem-secondary',
      'bg-mayhem-yellow',
      'bg-mayhem-purple',
      'bg-green-400',
      'bg-blue-400',
    ]
    return colors[index % colors.length]
  }

  return (
    <div className="min-h-screen bg-gradient-to-br from-mayhem-surface via-white to-blue-50 flex flex-col items-center justify-center p-4">

      {/* Confetti particles */}
      <div className="fixed inset-0 pointer-events-none overflow-hidden">
        {Array.from({ length: 20 }).map((_, i) => (
          <motion.div
            key={i}
            className={`absolute w-3 h-3 rounded-full ${getConfettiColor(i)}`}
            initial={{
              x: Math.random() * window.innerWidth,
              y: -20,
              opacity: 1,
              rotate: 0,
            }}
            animate={{
              y: window.innerHeight + 20,
              opacity: [1, 1, 0],
              rotate: Math.random() * 360,
              x: Math.random() * window.innerWidth,
            }}
            transition={{
              duration: Math.random() * 3 + 2,
              delay: Math.random() * 2,
              repeat: Infinity,
              ease: 'linear',
            }}
          />
        ))}
      </div>

      <div className="w-full max-w-md space-y-4 relative z-10">

        {/* Header */}
        <motion.div
          initial={{ opacity: 0, y: -30 }}
          animate={{ opacity: 1, y: 0 }}
          className="text-center"
        >
          <motion.div
            animate={{ rotate: [0, -10, 10, -10, 0] }}
            transition={{ duration: 0.5, delay: 0.3 }}
            className="text-7xl mb-4"
          >
            🏆
          </motion.div>
          <h1 className="font-display font-extrabold text-4xl text-gray-800 mb-2">
            Game Over!
          </h1>
          {winner && (
            <motion.p
              initial={{ opacity: 0 }}
              animate={{ opacity: 1 }}
              transition={{ delay: 0.4 }}
              className="text-gray-500 font-body text-lg"
            >
              <span className="font-bold text-mayhem-primary">
                {winner.nickname}
              </span>
              {' '}wins with{' '}
              <span className="font-bold text-mayhem-primary">
                {winner.totalScore} pts
              </span>
            </motion.p>
          )}
        </motion.div>

        {/* Winner Card */}
        {winner && (
          <motion.div
            initial={{ opacity: 0, scale: 0.8 }}
            animate={{ opacity: 1, scale: 1 }}
            transition={{ delay: 0.3, type: 'spring', stiffness: 200 }}
            className="bg-gradient-to-br from-yellow-400 to-orange-400
                       rounded-3xl p-6 text-center text-white shadow-xl"
          >
            <div className="text-5xl mb-3">👑</div>
            <p className="font-body text-sm opacity-90 mb-1">
              Champion
            </p>
            <p className="font-display font-extrabold text-3xl mb-2">
              {winner.nickname}
            </p>
            <div className="flex items-center justify-center gap-2">
              <Star className="w-5 h-5 fill-white" />
              <span className="font-display font-bold text-2xl">
                {winner.totalScore}
              </span>
              <span className="font-body opacity-90">points</span>
            </div>
          </motion.div>
        )}

        {/* Full Leaderboard */}
        <motion.div
          initial={{ opacity: 0, y: 20 }}
          animate={{ opacity: 1, y: 0 }}
          transition={{ delay: 0.4 }}
          className="bg-white rounded-3xl shadow-lg p-6"
        >
          <div className="flex items-center gap-2 mb-4">
            <Trophy className="w-5 h-5 text-yellow-400" />
            <h2 className="font-display font-bold text-gray-800">
              Final Standings
            </h2>
          </div>

          <div className="space-y-3">
            {sorted.map((player, index) => (
              <motion.div
                key={player.id}
                initial={{ opacity: 0, x: -20 }}
                animate={{ opacity: 1, x: 0 }}
                transition={{ delay: 0.4 + index * 0.08 }}
                className={`
                  flex items-center gap-4 p-3 rounded-2xl
                  ${player.id === state.playerId
                    ? 'bg-red-50 border-2 border-mayhem-primary'
                    : index === 0
                      ? 'bg-yellow-50 border-2 border-yellow-200'
                      : 'bg-mayhem-surface'
                  }
                `}
              >
                {/* Rank */}
                <span className="text-2xl w-8 text-center flex-shrink-0">
                  {getRankEmoji(index)}
                </span>

                {/* Avatar */}
                <div className={`
                  w-10 h-10 rounded-full flex items-center justify-center
                  font-display font-bold text-white flex-shrink-0
                  ${index === 0 ? 'bg-yellow-400' :
                    index === 1 ? 'bg-gray-400' :
                      index === 2 ? 'bg-orange-400' :
                        'bg-mayhem-secondary'
                  }
                `}>
                  {player.nickname[0].toUpperCase()}
                </div>

                {/* Name */}
                <div className="flex-1 min-w-0">
                  <p className="font-body font-semibold text-gray-700 truncate">
                    {player.nickname}
                    {player.id === state.playerId && (
                      <span className="ml-2 text-xs text-mayhem-secondary">
                        (you)
                      </span>
                    )}
                  </p>
                </div>

                {/* Score */}
                <div className="flex items-center gap-1 flex-shrink-0">
                  <Star className="w-4 h-4 text-yellow-400 fill-yellow-400" />
                  <span className="font-display font-extrabold text-gray-800 text-lg">
                    {player.totalScore}
                  </span>
                </div>
              </motion.div>
            ))}
          </div>
        </motion.div>

        {/* Action Buttons */}
        <motion.div
          initial={{ opacity: 0, y: 20 }}
          animate={{ opacity: 1, y: 0 }}
          transition={{ delay: 0.6 }}
          className="grid grid-cols-2 gap-3"
        >
          {/* Play Again — host only */}
          {state.isHost && (
            <button
              onClick={() => navigate('/game')}
              className="flex items-center justify-center gap-2 py-4 
                         rounded-2xl bg-mayhem-primary text-white
                         font-display font-bold text-sm
                         hover:bg-red-500 hover:scale-[1.02]
                         active:scale-[0.98] transition-all duration-200
                         shadow-lg col-span-2"
            >
              <RotateCcw className="w-4 h-4" />
              Play Again
            </button>
          )}

          {/* Home */}
          <button
            onClick={() => navigate('/')}
            className={`
              flex items-center justify-center gap-2 py-4
              rounded-2xl font-display font-bold text-sm
              transition-all duration-200
              ${state.isHost
                ? 'bg-mayhem-surface text-gray-600 hover:bg-gray-100'
                : 'bg-mayhem-primary text-white hover:bg-red-500 shadow-lg col-span-2'
              }
            `}
          >
            <Home className="w-4 h-4" />
            Back to Home
          </button>
        </motion.div>

        {/* Room code reminder */}
        <motion.p
          initial={{ opacity: 0 }}
          animate={{ opacity: 1 }}
          transition={{ delay: 0.7 }}
          className="text-center text-xs text-gray-400 font-body pb-4"
        >
          Room code: {' '}
          <span className="font-bold text-gray-600 tracking-widest">
            {state.roomCode}
          </span>
        </motion.p>

      </div>
    </div>
  )
}