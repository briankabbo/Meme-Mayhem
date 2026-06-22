import { motion, AnimatePresence } from 'framer-motion'
import { useGame } from '../hooks/useGame'
import CardHand from './CardHand'
import CardReveal from './CardReveal'
import ScoreBoard from './ScoreBoard'
import TurnTimer from './TurnTimer'
import RoundResults from './RoundResults'
import VotePanel from './VotePanel'
import { Users } from 'lucide-react'
import type { Player, CardPlay } from '../../types/game'

export default function GameBoard() {
  const { state } = useGame()
  const { currentRound, hand, players, playerId,
          selectedCardId, isMyTurn, roundResults } = state

  if (!currentRound) {
    return (
      <div className="min-h-screen flex items-center justify-center">
        <motion.div
          animate={{ opacity: [1, 0.4, 1] }}
          transition={{ duration: 1.5, repeat: Infinity }}
          className="text-gray-400 font-body"
        >
          Loading game...
        </motion.div>
      </div>
    )
  }

  const currentPlayer = players.find(
    (p: Player) => p.id === currentRound.currentPlayerId)

  // Most-recently revealed card for the vote sidebar
  const latestCardPlay: CardPlay | null =
    currentRound.cardPlays.length > 0
      ? currentRound.cardPlays[currentRound.cardPlays.length - 1]
      : null

  return (
    <div className="min-h-screen bg-gradient-to-br from-mayhem-surface via-white to-blue-50 flex flex-col">

      {/* Top Bar */}
      <div className="sticky top-0 z-10 bg-white/80 backdrop-blur-sm border-b border-gray-100 px-4 py-3">
        <div className="flex items-center justify-between max-w-screen-xl mx-auto">

          {/* Round info */}
          <div>
            <p className="font-display font-bold text-gray-800 text-sm">
              Round {currentRound.roundNumber}
              <span className="text-gray-400 font-body font-normal ml-1">
                / {state.totalRounds}
              </span>
            </p>
            <p className="text-xs text-gray-400 font-body">
              Turn {currentRound.currentTurnIndex + 1} of {currentRound.totalTurns}
            </p>
          </div>

          {/* Timer — only show on current player's turn */}
          {isMyTurn && (
            <TurnTimer seconds={15} />
          )}

          {/* Player count */}
          <div className="flex items-center gap-1 text-gray-400">
            <Users className="w-4 h-4" />
            <span className="text-sm font-body">
              {players.filter((p: Player) => !p.isSpectator).length}
            </span>
          </div>

        </div>
      </div>

      {/* 3-Column Layout */}
      <div className="flex-1 grid grid-cols-[240px_1fr_240px] gap-4 max-w-screen-xl mx-auto w-full px-4 py-4">

        {/* LEFT — Scoreboard sidebar */}
        <aside className="flex flex-col gap-4">
          <ScoreBoard
            players={players}
            myPlayerId={playerId ?? ''}
          />
        </aside>

        {/* CENTER — Main game area */}
        <main className="flex flex-col gap-4 min-w-0">

          {/* Prompt */}
          <motion.div
            key={currentRound.id}
            initial={{ opacity: 0, y: -10 }}
            animate={{ opacity: 1, y: 0   }}
            className="bg-white rounded-2xl shadow-sm border border-gray-100 p-5"
          >
            <p className="text-xs font-body text-gray-400 uppercase tracking-widest mb-2">
              Prompt
            </p>
            <p className="font-display font-bold text-gray-800 text-lg leading-snug">
              {currentRound.promptText}
            </p>
          </motion.div>

          {/* Current turn indicator */}
          <AnimatePresence mode="wait">
            {isMyTurn ? (
              <motion.div
                key="your-turn"
                initial={{ opacity: 0, scale: 0.95 }}
                animate={{ opacity: 1, scale: 1    }}
                exit={{    opacity: 0, scale: 0.95  }}
                className="bg-gradient-to-r from-mayhem-primary to-red-400
                           rounded-2xl p-4 text-center text-white"
              >
                <p className="font-display font-bold text-lg">
                  🎯 Your Turn!
                </p>
                <p className="font-body text-sm opacity-90">
                  Pick a meme card from your hand
                </p>
              </motion.div>
            ) : currentPlayer ? (
              <motion.div
                key="other-turn"
                initial={{ opacity: 0 }}
                animate={{ opacity: 1 }}
                exit={{    opacity: 0 }}
                className="bg-mayhem-surface rounded-2xl p-4 text-center"
              >
                <p className="font-body text-gray-500 text-sm">
                  <span className="font-semibold text-gray-700">
                    {currentPlayer.nickname}
                  </span>
                  {' '}is picking a card...
                </p>
              </motion.div>
            ) : null}
          </AnimatePresence>

          {/* Revealed Cards */}
          <div>
            <p className="font-display font-bold text-gray-700 text-sm mb-3">
              Played Cards
            </p>
            <CardReveal
              cardPlays={currentRound.cardPlays}
              myPlayerId={playerId ?? ''}
            />
          </div>

          {/* Card Hand — inline, no fixed bar */}
          <div className="mt-auto pt-4 bg-white/95 rounded-2xl border border-gray-100 px-4 py-3">
            <CardHand
              cards={hand}
              isMyTurn={isMyTurn}
              selectedCardId={selectedCardId}
            />
          </div>

        </main>

        {/* RIGHT — Vote sidebar */}
        <aside className="flex flex-col gap-4">
          {latestCardPlay && (
            <div className="bg-white rounded-2xl shadow-sm border border-gray-100 p-4">
              <p className="text-xs font-body text-gray-400 uppercase tracking-widest mb-3">
                Vote
              </p>
              <p className="font-body font-semibold text-gray-700 text-sm mb-3 truncate">
                {latestCardPlay.playerName}
              </p>
              <VotePanel
                cardPlay={latestCardPlay}
                myPlayerId={playerId ?? ''}
              />
            </div>
          )}
        </aside>

      </div>

      {/* Round Results Overlay */}
      <AnimatePresence>
        {roundResults && (
          <motion.div
            initial={{ opacity: 0 }}
            animate={{ opacity: 1 }}
            exit={{    opacity: 0 }}
            className="fixed inset-0 bg-black/50 backdrop-blur-sm z-50
                       flex items-center justify-center p-4"
          >
            <RoundResults
              results={roundResults}
              myPlayerId={playerId ?? ''}
            />
          </motion.div>
        )}
      </AnimatePresence>

    </div>
  )
}