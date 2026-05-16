import { motion, AnimatePresence } from 'framer-motion'
import type { CardPlay } from '../../types/game'
import VotePanel from './VotePanel'

interface Props {
  cardPlays: CardPlay[]
  myPlayerId: string
}

export default function CardReveal({
  cardPlays, myPlayerId
}: Props) {

  if (cardPlays.length === 0) {
    return (
      <div className="flex flex-col items-center justify-center py-12 text-gray-400">
        <motion.div
          animate={{ opacity: [1, 0.4, 1] }}
          transition={{ duration: 1.5, repeat: Infinity }}
          className="text-4xl mb-3"
        >
          🃏
        </motion.div>
        <p className="font-body text-sm">
          Waiting for first card...
        </p>
      </div>
    )
  }

  return (
    <div className="space-y-4">
      <AnimatePresence>
        {cardPlays.map((cardPlay, index) => (
          <motion.div
            key={cardPlay.id}
            initial={{ opacity: 0, y: 30, scale: 0.95 }}
            animate={{ opacity: 1, y: 0,  scale: 1    }}
            transition={{ delay: index * 0.05 }}
            className="bg-white rounded-2xl shadow-sm border border-gray-100 overflow-hidden"
          >
            {/* Player name header */}
            <div className="flex items-center gap-3 px-4 py-3 bg-mayhem-surface border-b border-gray-100">
              <div className={`
                w-8 h-8 rounded-full flex items-center justify-center
                font-display font-bold text-white text-sm
                ${cardPlay.playerId === myPlayerId
                  ? 'bg-mayhem-primary'
                  : 'bg-mayhem-secondary'
                }
              `}>
                {cardPlay.playerName[0].toUpperCase()}
              </div>
              <span className="font-body font-semibold text-gray-700 text-sm">
                {cardPlay.playerName}
                {cardPlay.playerId === myPlayerId && (
                  <span className="ml-2 text-mayhem-secondary text-xs">
                    (you)
                  </span>
                )}
              </span>
              {/* Vote progress */}
              <span className="ml-auto text-xs text-gray-400 font-body">
                {cardPlay.votes.length} votes
              </span>
            </div>

            {/* Card content */}
            <div className="flex gap-4 p-4">
              {/* Meme image */}
              <div className="flex-shrink-0 w-24 h-24 rounded-xl overflow-hidden bg-gray-50">
                <img
                  src={cardPlay.card.imageUrl}
                  alt={cardPlay.card.label}
                  className="w-full h-full object-cover"
                />
              </div>

              {/* Right side */}
              <div className="flex-1 min-w-0">
                <p className="font-body text-sm text-gray-600 mb-3 leading-tight">
                  {cardPlay.card.label}
                </p>

                {/* Voter names */}
                {cardPlay.votes.length > 0 && (
                  <div className="flex flex-wrap gap-1 mb-2">
                    {cardPlay.votes.map(vote => (
                      <span
                        key={vote.voterId}
                        className="text-xs bg-gray-100 text-gray-500 px-2 py-0.5 rounded-full font-body"
                      >
                        {vote.voterName} {
                          vote.voteType === 'Haha' ? '😂' :
                          vote.voteType === 'Lmao' ? '💀' : '😐'
                        }
                      </span>
                    ))}
                  </div>
                )}

                {/* Vote buttons */}
                <VotePanel
                  cardPlay={cardPlay}
                  myPlayerId={myPlayerId}
                />
              </div>
            </div>
          </motion.div>
        ))}
      </AnimatePresence>
    </div>
  )
}