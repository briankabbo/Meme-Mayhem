import { useState, useEffect } from 'react'
import { motion, AnimatePresence } from 'framer-motion'
import { ChevronLeft, ChevronRight } from 'lucide-react'
import type { CardPlay } from '../../types/game'

interface Props {
  cardPlays: CardPlay[]
  myPlayerId: string
  totalPlayers: number
}

export default function CardReveal({ cardPlays, myPlayerId, totalPlayers }: Props) {
  const [activeIndex, setActiveIndex] = useState(0)

  // Always show latest card when new one arrives
  useEffect(() => {
    if (cardPlays.length > 0)
      setActiveIndex(cardPlays.length - 1)
  }, [cardPlays.length])

  const canGoPrev = activeIndex > 0
  const canGoNext = activeIndex < cardPlays.length - 1
  const active = cardPlays[activeIndex] ?? null

  if (cardPlays.length === 0) {
    return (
      <div className="flex flex-col items-center justify-center py-16 text-gray-300">
        <motion.div
          animate={{ opacity: [1, 0.3, 1] }}
          transition={{ duration: 1.8, repeat: Infinity }}
          className="text-5xl mb-3"
        >
          🃏
        </motion.div>
        <p className="font-body text-sm text-gray-400">
          Waiting for first card...
        </p>
      </div>
    )
  }

  return (
    <div className="flex flex-col items-center gap-4">

      {/* Spotlight card */}
      <div className="flex items-center gap-3 w-full">

        {/* Prev arrow */}
        <button
          onClick={() => canGoPrev && setActiveIndex(i => i - 1)}
          className={`
            w-8 h-8 rounded-full flex items-center justify-center flex-shrink-0
            transition-all
            ${canGoPrev
              ? 'bg-gray-100 hover:bg-gray-200 text-gray-600 cursor-pointer'
              : 'bg-gray-50 text-gray-200 cursor-default'
            }
          `}
        >
          <ChevronLeft className="w-4 h-4" />
        </button>

        {/* Card */}
        <div className="flex-1 min-w-0">
          <AnimatePresence mode="wait">
            {active && (
              <motion.div
                key={active.id}
                initial={{ opacity: 0, scale: 0.92, y: 10 }}
                animate={{ opacity: 1, scale: 1, y: 0 }}
                exit={{ opacity: 0, scale: 0.92, y: -10 }}
                transition={{ type: 'spring', stiffness: 300, damping: 28 }}
                className="bg-white rounded-2xl border border-gray-100 shadow-md overflow-hidden"
              >
                {/* Player header */}
                <div className="flex items-center gap-2 px-4 py-2.5 border-b border-gray-50">
                  <div className={`
                    w-7 h-7 rounded-full flex items-center justify-center
                    text-white text-xs font-bold flex-shrink-0
                    ${active.playerId === myPlayerId
                      ? 'bg-mayhem-primary'
                      : 'bg-mayhem-secondary'
                    }
                  `}>
                    {active.playerName[0].toUpperCase()}
                  </div>
                  <span className="font-body font-semibold text-gray-700 text-sm">
                    {active.playerName}
                    {active.playerId === myPlayerId && (
                      <span className="ml-1 text-xs text-gray-400 font-normal">you</span>
                    )}
                  </span>
                  <span className="ml-auto text-xs text-gray-400 font-body">
                    {active.votes.length} vote{active.votes.length !== 1 ? 's' : ''}
                  </span>
                </div>

                {/* Card image — fixed height spotlight */}
                <div className="w-full aspect-[4/3] bg-gray-50">
                  <img
                    src={active.card.imageUrl}
                    alt={active.card.label}
                    className="w-full h-full object-contain"
                  />
                </div>

                {/* Vote emojis */}
                {active.votes.length > 0 && (
                  <div className="flex items-center gap-1 px-4 py-2 border-t border-gray-50 flex-wrap">
                    {active.votes.map(vote => (
                      <motion.span
                        key={vote.voterId}
                        initial={{ scale: 0, opacity: 0 }}
                        animate={{ scale: 1, opacity: 1 }}
                        className="text-lg"
                        title={`${vote.voterName}`}
                      >
                        {vote.voteType === 'Haha' ? '😂' :
                          vote.voteType === 'Lmao' ? '💀' : '😐'}
                      </motion.span>
                    ))}
                  </div>
                )}
              </motion.div>
            )}
          </AnimatePresence>
        </div>

        {/* Next arrow */}
        <button
          onClick={() => canGoNext && setActiveIndex(i => i + 1)}
          className={`
            w-8 h-8 rounded-full flex items-center justify-center flex-shrink-0
            transition-all
            ${canGoNext
              ? 'bg-gray-100 hover:bg-gray-200 text-gray-600 cursor-pointer'
              : 'bg-gray-50 text-gray-200 cursor-default'
            }
          `}
        >
          <ChevronRight className="w-4 h-4" />
        </button>
      </div>

      {/* Dot indicators */}
      <div className="flex items-center gap-1.5">
        {Array.from({ length: totalPlayers }).map((_, i) => (
          <button
            key={i}
            onClick={() => i < cardPlays.length && setActiveIndex(i)}
            className={`
              rounded-full transition-all
              ${i === activeIndex
                ? 'w-4 h-2 bg-mayhem-primary'
                : i < cardPlays.length
                  ? 'w-2 h-2 bg-gray-300 hover:bg-gray-400 cursor-pointer'
                  : 'w-2 h-2 bg-gray-100 cursor-default'
              }
            `}
          />
        ))}
      </div>

      {/* Card count */}
      <p className="text-xs text-gray-400 font-body">
        {cardPlays.length} of {totalPlayers} cards played
      </p>
    </div>
  )
}