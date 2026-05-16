import { motion, AnimatePresence } from 'framer-motion'
import type { MemeCard } from '../../types/game'
import { useGame } from '../hooks/useGame'

interface Props {
  cards: MemeCard[]
  isMyTurn: boolean
  selectedCardId: string | null
}

export default function CardHand({ cards, isMyTurn, selectedCardId }: Props) {
  const { submitCard } = useGame()

  return (
    <div className="w-full">
      <div className="flex items-center justify-between mb-3">
        <span className="font-display font-bold text-gray-700 text-sm">
          Your Hand
        </span>
        {isMyTurn && (
          <motion.span
            animate={{ opacity: [1, 0.5, 1] }}
            transition={{ duration: 1.5, repeat: Infinity }}
            className="text-xs font-body text-mayhem-primary font-semibold"
          >
            Pick a card!
          </motion.span>
        )}
      </div>

      <div className="flex gap-2 overflow-x-auto pb-2">
        <AnimatePresence>
          {cards.map((card, index) => {
            const isSelected = selectedCardId === card.id
            const isPlayed   = selectedCardId !== null && !isSelected

            return (
              <motion.div
                key={card.id}
                initial={{ opacity: 0, y: 30 }}
                animate={{ opacity: 1, y: 0  }}
                exit={{    opacity: 0, y: 30  }}
                transition={{ delay: index * 0.05 }}
                whileHover={isMyTurn && !selectedCardId
                  ? { y: -8, scale: 1.03 }
                  : {}
                }
                whileTap={isMyTurn && !selectedCardId
                  ? { scale: 0.97 }
                  : {}
                }
                onClick={() => {
                  if (isMyTurn && !selectedCardId)
                    submitCard(card.id)
                }}
                className={`
                  relative flex-shrink-0 w-28 rounded-2xl overflow-hidden
                  border-2 transition-all duration-200
                  ${isMyTurn && !selectedCardId
                    ? 'cursor-pointer'
                    : 'cursor-default'
                  }
                  ${isSelected
                    ? 'border-mayhem-primary shadow-lg shadow-red-100'
                    : isPlayed
                    ? 'border-gray-100 opacity-40'
                    : 'border-gray-100 shadow-sm'
                  }
                `}
              >
                {/* Meme Image */}
                <div className="aspect-square bg-gray-50">
                  <img
                    src={card.imageUrl}
                    alt={card.label}
                    className="w-full h-full object-cover"
                    loading="lazy"
                  />
                </div>

                {/* Label */}
                <div className="p-2 bg-white">
                  <p className="text-xs font-body text-gray-600 text-center leading-tight truncate">
                    {card.label}
                  </p>
                </div>

                {/* Selected overlay */}
                {isSelected && (
                  <motion.div
                    initial={{ opacity: 0 }}
                    animate={{ opacity: 1 }}
                    className="absolute inset-0 bg-mayhem-primary/10 
                               flex items-center justify-center"
                  >
                    <span className="text-2xl">✅</span>
                  </motion.div>
                )}

                {/* Your turn glow */}
                {isMyTurn && !selectedCardId && (
                  <motion.div
                    animate={{ opacity: [0, 0.15, 0] }}
                    transition={{ duration: 1.5, repeat: Infinity }}
                    className="absolute inset-0 bg-mayhem-primary rounded-2xl"
                  />
                )}
              </motion.div>
            )
          })}
        </AnimatePresence>
      </div>

      {/* Empty hand message */}
      {cards.length === 0 && (
        <div className="text-center py-4 text-gray-400 font-body text-sm">
          No cards in hand
        </div>
      )}
    </div>
  )
}