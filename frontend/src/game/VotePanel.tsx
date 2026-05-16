import { motion } from 'framer-motion'
import type { CardPlay, VoteType } from '../../types/game'
import { useGame } from '../hooks/useGame'

interface Props {
  cardPlay: CardPlay
  myPlayerId: string
}

const VOTE_OPTIONS: { type: VoteType; emoji: string; label: string; points: string; color: string }[] = [
  { type: 'Haha', emoji: '😂', label: 'Haha',  points: '+1', color: 'bg-yellow-50 border-yellow-200 hover:bg-yellow-100' },
  { type: 'Lmao', emoji: '💀', label: 'Lmao',  points: '+5', color: 'bg-red-50 border-red-200 hover:bg-red-100'         },
  { type: 'Meh',  emoji: '😐', label: 'Meh',   points: '+0', color: 'bg-gray-50 border-gray-200 hover:bg-gray-100'      },
]

export default function VotePanel({ cardPlay, myPlayerId }: Props) {
  const { submitVote } = useGame()

  const isOwnCard   = cardPlay.playerId === myPlayerId
  const myVote      = cardPlay.votes.find(v => v.voterId === myPlayerId)
  const hasVoted    = !!myVote

  const voteCount = (type: VoteType) =>
    cardPlay.votes.filter(v => v.voteType === type).length

  return (
    <div className="flex gap-2 mt-2">
      {VOTE_OPTIONS.map(opt => {
        const count      = voteCount(opt.type)
        const isMyVote   = myVote?.voteType === opt.type
        const canVote    = !isOwnCard && !hasVoted

        return (
          <motion.button
            key={opt.type}
            whileHover={canVote ? { scale: 1.05 } : {}}
            whileTap={canVote   ? { scale: 0.95 } : {}}
            onClick={() => canVote && submitVote(cardPlay.id, opt.type)}
            className={`
              flex-1 flex flex-col items-center py-2 px-1 rounded-xl
              border-2 transition-all duration-200
              ${isMyVote
                ? 'border-mayhem-primary bg-red-50 shadow-sm'
                : canVote
                ? `${opt.color} cursor-pointer`
                : 'border-gray-100 bg-gray-50 cursor-default opacity-60'
              }
            `}
          >
            <span className="text-xl">{opt.emoji}</span>
            <span className="text-xs font-body font-semibold text-gray-600">
              {opt.label}
            </span>
            <div className="flex items-center gap-1 mt-1">
              {count > 0 && (
                <motion.span
                  key={count}
                  initial={{ scale: 1.5 }}
                  animate={{ scale: 1 }}
                  className="text-xs font-bold text-gray-500 font-body"
                >
                  {count}
                </motion.span>
              )}
              <span className="text-xs text-gray-400 font-body">
                {opt.points}
              </span>
            </div>
          </motion.button>
        )
      })}
    </div>
  )
}