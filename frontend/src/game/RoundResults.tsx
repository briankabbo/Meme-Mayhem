import { motion } from 'framer-motion'
import type { RoundResult } from '../../types/game'
import { Star, Trophy } from 'lucide-react'

interface Props {
  results: RoundResult
  myPlayerId: string
}

export default function RoundResults({ results, myPlayerId }: Props) {
  const myScore = results.scores.find(s => s.playerId === myPlayerId)

  return (
    <motion.div
      initial={{ opacity: 0, scale: 0.95 }}
      animate={{ opacity: 1, scale: 1    }}
      className="bg-white rounded-3xl shadow-xl p-6 w-full max-w-md mx-auto"
    >
      {/* Header */}
      <div className="text-center mb-6">
        <div className="text-4xl mb-2">
          {results.isGameOver ? '🏆' : '⭐'}
        </div>
        <h2 className="font-display font-extrabold text-2xl text-gray-800">
          {results.isGameOver ? 'Game Over!' : `Round ${results.roundNumber} Results`}
        </h2>
        {!results.isGameOver && (
          <p className="text-gray-400 font-body text-sm mt-1">
            Round {results.roundNumber} of {results.totalRounds}
          </p>
        )}
      </div>

      {/* My points this round */}
      {myScore && (
        <motion.div
          initial={{ scale: 0.8, opacity: 0 }}
          animate={{ scale: 1,   opacity: 1 }}
          transition={{ delay: 0.2 }}
          className="bg-gradient-to-r from-mayhem-primary to-red-400 
                     rounded-2xl p-4 text-center text-white mb-6"
        >
          <p className="font-body text-sm opacity-90">You earned</p>
          <p className="font-display font-extrabold text-4xl">
            +{myScore.pointsEarned}
          </p>
          <p className="font-body text-sm opacity-90">
            Total: {myScore.runningTotal} pts
          </p>
        </motion.div>
      )}

      {/* All scores */}
      <div className="space-y-2">
        {results.scores.map((score, index) => (
          <motion.div
            key={score.playerId}
            initial={{ opacity: 0, x: -20 }}
            animate={{ opacity: 1, x: 0   }}
            transition={{ delay: 0.1 + index * 0.05 }}
            className={`
              flex items-center gap-3 p-3 rounded-xl
              ${score.playerId === myPlayerId
                ? 'bg-red-50 border border-red-100'
                : 'bg-mayhem-surface'
              }
            `}
          >
            {/* Rank badge */}
            <span className={`
              w-7 h-7 rounded-full flex items-center justify-center
              font-display font-bold text-xs text-white flex-shrink-0
              ${score.rank === 1 ? 'bg-yellow-400' :
                score.rank === 2 ? 'bg-gray-400'   :
                score.rank === 3 ? 'bg-orange-400' :
                'bg-gray-200 text-gray-500'}
            `}>
              {score.rank === 1 ? <Trophy className="w-3 h-3" /> : score.rank}
            </span>

            {/* Name */}
            <span className="flex-1 font-body font-semibold text-gray-700 text-sm truncate">
              {score.playerName}
            </span>

            {/* Points earned this round */}
            <div className="flex items-center gap-1">
              <Star className="w-3 h-3 text-yellow-400" />
              <span className="text-xs font-body text-gray-400">
                +{score.pointsEarned}
              </span>
            </div>

            {/* Total */}
            <span className="font-display font-extrabold text-mayhem-primary text-sm">
              {score.runningTotal}
            </span>
          </motion.div>
        ))}
      </div>

      {/* Next round indicator */}
      {!results.isGameOver && (
        <motion.div
          initial={{ opacity: 0 }}
          animate={{ opacity: 1 }}
          transition={{ delay: 0.5 }}
          className="text-center mt-6"
        >
          <div className="inline-flex items-center gap-2 text-gray-400 font-body text-sm">
            <motion.div
              animate={{ rotate: 360 }}
              transition={{ duration: 2, repeat: Infinity, ease: 'linear' }}
              className="w-4 h-4 border-2 border-mayhem-secondary border-t-transparent rounded-full"
            />
            Next round starting soon...
          </div>
        </motion.div>
      )}
    </motion.div>
  )
}