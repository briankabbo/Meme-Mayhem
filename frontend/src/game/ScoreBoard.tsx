import { motion, AnimatePresence } from 'framer-motion'
import type { Player } from '../../types/game'
import { Trophy } from 'lucide-react'

interface Props {
  players: Player[]
  myPlayerId: string
}

export default function ScoreBoard({ players, myPlayerId }: Props) {
  const sorted = [...players]
    .filter(p => !p.isSpectator)
    .sort((a, b) => b.totalScore - a.totalScore)

  return (
    <div className="bg-white rounded-2xl shadow-sm border border-gray-100 p-4">
      <div className="flex items-center gap-2 mb-3">
        <Trophy className="w-4 h-4 text-yellow-400" />
        <span className="font-display font-bold text-gray-700 text-sm">
          Scoreboard
        </span>
      </div>

      <div className="space-y-2">
        <AnimatePresence>
          {sorted.map((player, index) => (
            <motion.div
              key={player.id}
              layout
              initial={{ opacity: 0, x: -20 }}
              animate={{ opacity: 1, x: 0   }}
              className={`
                flex items-center gap-3 p-2 rounded-xl
                ${player.id === myPlayerId
                  ? 'bg-red-50 border border-red-100'
                  : 'bg-mayhem-surface'
                }
              `}
            >
              {/* Rank */}
              <span className={`
                w-6 h-6 rounded-full flex items-center justify-center
                font-display font-bold text-xs
                ${index === 0 ? 'bg-yellow-400 text-white' :
                  index === 1 ? 'bg-gray-300 text-white'   :
                  index === 2 ? 'bg-orange-400 text-white' :
                  'bg-gray-100 text-gray-500'}
              `}>
                {index + 1}
              </span>

              {/* Name */}
              <span className="flex-1 font-body text-sm font-semibold text-gray-700 truncate">
                {player.nickname}
                {player.id === myPlayerId && (
                  <span className="ml-1 text-mayhem-secondary text-xs font-normal">
                    (you)
                  </span>
                )}
              </span>

              {/* Score */}
              <motion.span
                key={player.totalScore}
                initial={{ scale: 1.3 }}
                animate={{ scale: 1   }}
                className="font-display font-extrabold text-mayhem-primary text-sm"
              >
                {player.totalScore}
              </motion.span>
            </motion.div>
          ))}
        </AnimatePresence>
      </div>
    </div>
  )
}