import { motion, AnimatePresence } from 'framer-motion'
import type { Player } from '../../types/game'

interface Props {
  players: Player[]
  myPlayerId: string
  currentPlayerId?: string
}

export default function ScoreBoard({ players, myPlayerId, currentPlayerId }: Props) {
  const sorted = [...players]
    .filter(p => !p.isSpectator)
    .sort((a, b) => b.totalScore - a.totalScore)

  return (
    <div className="bg-white rounded-2xl border border-gray-100 shadow-sm p-4 h-full">
      <p className="text-xs font-body uppercase tracking-widest text-gray-400 mb-3">
        Leaderboard
      </p>

      <div className="space-y-1.5">
        <AnimatePresence>
          {sorted.map((player, index) => {
            const isMe = player.id === myPlayerId
            const isCurrent = player.id === currentPlayerId
            const isFirst = index === 0

            return (
              <motion.div
                key={player.id}
                layout
                initial={{ opacity: 0, x: -10 }}
                animate={{ opacity: 1, x: 0 }}
                className={`
                  flex items-center gap-2 px-3 py-2 rounded-xl transition-all
                  ${isMe
                    ? 'bg-red-50 border border-red-100'
                    : 'bg-gray-50'
                  }
                `}
              >
                {/* Rank */}
                <div className={`
                  w-6 h-6 rounded-full flex items-center justify-center
                  text-xs font-display font-bold flex-shrink-0
                  ${isFirst
                    ? 'bg-yellow-400 text-white'
                    : index === 1
                      ? 'bg-gray-300 text-white'
                      : index === 2
                        ? 'bg-orange-300 text-white'
                        : 'bg-gray-100 text-gray-400'
                  }
                `}>
                  {isFirst ? '👑' : index + 1}
                </div>

                {/* Name */}
                <div className="flex-1 min-w-0">
                  <p className="text-sm font-body font-semibold text-gray-700 truncate">
                    {player.nickname}
                    {isMe && (
                      <span className="ml-1 text-xs text-gray-400 font-normal">
                        you
                      </span>
                    )}
                  </p>
                </div>

                {/* Current turn indicator */}
                {isCurrent && (
                  <motion.div
                    animate={{ opacity: [1, 0.3, 1] }}
                    transition={{ duration: 1, repeat: Infinity }}
                    className="w-2 h-2 rounded-full bg-mayhem-primary flex-shrink-0"
                  />
                )}

                {/* Score */}
                <motion.span
                  key={player.totalScore}
                  initial={{ scale: 1.4, color: '#FF6B6B' }}
                  animate={{ scale: 1, color: '#1f2937' }}
                  className="font-display font-extrabold text-sm flex-shrink-0"
                >
                  {player.totalScore}
                </motion.span>
              </motion.div>
            )
          })}
        </AnimatePresence>
      </div>
    </div>
  )
}