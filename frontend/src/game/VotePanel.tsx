import { motion } from 'framer-motion'
import type { CardPlay, VoteType } from '../../types/game'
import { useGame } from '../hooks/useGame'

interface Props {
  cardPlay: CardPlay | null
  myPlayerId: string
}

const VOTE_OPTIONS: {
  type: VoteType
  emoji: string
  label: string
  points: string
  activeColor: string
  hoverColor: string
}[] = [
    {
      type: 'Haha',
      emoji: '😂',
      label: 'Haha',
      points: '+1',
      activeColor: 'bg-yellow-400 border-yellow-400 text-white',
      hoverColor: 'hover:bg-yellow-50 hover:border-yellow-300',
    },
    {
      type: 'Lmao',
      emoji: '💀',
      label: 'Lmao',
      points: '+5',
      activeColor: 'bg-red-500 border-red-500 text-white',
      hoverColor: 'hover:bg-red-50 hover:border-red-300',
    },
    {
      type: 'Meh',
      emoji: '😐',
      label: 'Meh',
      points: '+0',
      activeColor: 'bg-gray-400 border-gray-400 text-white',
      hoverColor: 'hover:bg-gray-50 hover:border-gray-300',
    },
  ]

export default function VotePanel({ cardPlay, myPlayerId }: Props) {
  const { submitVote } = useGame()

  if (!cardPlay) return null

  const isOwnCard = cardPlay.playerId === myPlayerId
  const myVote = cardPlay.votes.find(v => v.voterId === myPlayerId)
  const hasVoted = !!myVote

  if (isOwnCard) {
    return (
      <div className="flex items-center justify-center gap-2 py-3 text-gray-400">
        <span className="text-sm font-body">Others are voting on your card...</span>
      </div>
    )
  }

  if (hasVoted) {
    return (
      <div className="flex items-center justify-center gap-2 py-3">
        <span className="text-lg">
          {myVote?.voteType === 'Haha' ? '😂' :
            myVote?.voteType === 'Lmao' ? '💀' : '😐'}
        </span>
        <span className="text-sm font-body text-gray-500">
          You voted <strong>{myVote?.voteType}</strong>
        </span>
      </div>
    )
  }

  return (
    <div className="flex items-center gap-2">
      <span className="text-xs text-gray-400 font-body mr-1 whitespace-nowrap">
        React:
      </span>
      {VOTE_OPTIONS.map(opt => (
        <motion.button
          key={opt.type}
          whileHover={{ scale: 1.05 }}
          whileTap={{ scale: 0.95 }}
          onClick={() => submitVote(cardPlay.id, opt.type)}
          className={`
            flex-1 flex flex-col items-center gap-0.5 py-2 px-3
            rounded-xl border-2 border-gray-200 bg-white
            transition-all duration-150 cursor-pointer
            ${opt.hoverColor}
          `}
        >
          <span className="text-xl">{opt.emoji}</span>
          <span className="text-xs font-body font-semibold text-gray-600">
            {opt.label}
          </span>
          <span className="text-xs text-gray-400 font-body">{opt.points}</span>
        </motion.button>
      ))}
    </div>
  )
}