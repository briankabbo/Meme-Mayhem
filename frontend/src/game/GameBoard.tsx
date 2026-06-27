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
  const {
    currentRound, hand, players, playerId,
    selectedCardId, isMyTurn, roundResults
  } = state

  if (!currentRound) {
    return (
      <div className="min-h-screen flex items-center justify-center bg-gray-50">
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

  const latestCardPlay: CardPlay | null =
    currentRound.cardPlays.length > 0
      ? currentRound.cardPlays[currentRound.cardPlays.length - 1]
      : null

  const activePlayers = players.filter((p: Player) => !p.isSpectator)

  return (
    <div className="min-h-screen bg-gray-50 flex flex-col">

      {/* Top Bar */}
      <div className="sticky top-0 z-10 bg-white border-b border-gray-100 px-4 py-3">
        <div className="flex items-center justify-between max-w-screen-xl mx-auto gap-4">

          {/* Round info */}
          <div className="flex items-center gap-2 flex-shrink-0">
            <span className="font-display font-bold text-gray-800 text-sm">
              Round {currentRound.roundNumber}
            </span>
            <span className="text-gray-300">/</span>
            <span className="text-gray-400 font-body text-sm">
              {state.totalRounds}
            </span>
            <span className="ml-1 text-xs text-gray-300 font-body">
              · Turn {currentRound.currentTurnIndex + 1} of {currentRound.totalTurns}
            </span>
          </div>

          {/* Prompt — centered */}
          <p className="flex-1 text-center font-display font-bold text-gray-800
                        text-sm leading-snug line-clamp-2 px-4">
            {currentRound.promptText}
          </p>

          {/* Right — timer + player count */}
          <div className="flex items-center gap-3 flex-shrink-0">
            {isMyTurn && <TurnTimer seconds={15} />}
            <div className="flex items-center gap-1 text-gray-400">
              <Users className="w-4 h-4" />
              <span className="text-sm font-body">
                {activePlayers.length}
              </span>
            </div>
          </div>

        </div>
      </div>

      {/* Main Content */}
      <div className="flex-1 grid grid-cols-[220px_1fr] gap-4
                max-w-screen-xl mx-auto w-full px-4 py-4 min-h-0">

        {/* LEFT — Leaderboard */}
        <aside>
          <ScoreBoard
            players={players}
            myPlayerId={playerId ?? ''}
            currentPlayerId={currentRound.currentPlayerId}
          />
        </aside>

        {/* CENTER — Main game */}
        <main className="flex flex-col gap-3 min-w-0">

          {/* Turn indicator */}
          <AnimatePresence mode="wait">
            {isMyTurn ? (
              <motion.div
                key="my-turn"
                initial={{ opacity: 0, y: -8 }}
                animate={{ opacity: 1, y: 0 }}
                exit={{ opacity: 0, y: -8 }}
                className="flex items-center justify-center gap-2
                     bg-mayhem-primary/10 border border-mayhem-primary/20
                     rounded-xl py-2 px-4"
              >
                <motion.span
                  animate={{ scale: [1, 1.2, 1] }}
                  transition={{ duration: 1, repeat: Infinity }}
                >
                  🎯
                </motion.span>
                <span className="font-display font-bold text-mayhem-primary text-sm">
                  Your Turn — pick a card!
                </span>
              </motion.div>
            ) : currentPlayer ? (
              <motion.div
                key="other-turn"
                initial={{ opacity: 0 }}
                animate={{ opacity: 1 }}
                exit={{ opacity: 0 }}
                className="text-center py-2"
              >
                <span className="text-sm font-body text-gray-400">
                  <span className="font-semibold text-gray-600">
                    {currentPlayer.nickname}
                  </span>
                  {' '}is picking a card...
                </span>
              </motion.div>
            ) : null}
          </AnimatePresence>

          {/* Spotlight Card Reveal — constrained height */}
          <div className="bg-white rounded-2xl border border-gray-100 shadow-sm p-4"
            style={{ maxHeight: '420px', overflow: 'hidden' }}>
            <CardReveal
              cardPlays={currentRound.cardPlays}
              myPlayerId={playerId ?? ''}
              totalPlayers={activePlayers.length}
            />
          </div>

          {/* Vote Reaction Bar */}
          <div className="bg-white rounded-2xl border border-gray-100 shadow-sm px-4 py-3">
            <VotePanel
              cardPlay={latestCardPlay}
              myPlayerId={playerId ?? ''}
            />
          </div>

          {/* Card Hand — always visible, pinned */}
          <motion.div
            animate={isMyTurn ? {
              boxShadow: [
                '0 0 0 0 rgba(255,107,107,0)',
                '0 0 0 4px rgba(255,107,107,0.3)',
                '0 0 0 0 rgba(255,107,107,0)',
              ]
            } : { boxShadow: '0 0 0 0 rgba(255,107,107,0)' }}
            transition={{ duration: 1.5, repeat: isMyTurn ? Infinity : 0 }}
            className="bg-white rounded-2xl border border-gray-100 shadow-sm px-4 py-3 mt-auto"
          >
            <p className="font-display font-bold text-gray-700 text-base mb-2">
              Your Hand
            </p>
            <CardHand
              cards={hand}
              isMyTurn={isMyTurn}
              selectedCardId={selectedCardId}
            />
          </motion.div>

        </main>
      </div>

      {/* Round Results Overlay */}
      <AnimatePresence>
        {roundResults && (
          <motion.div
            initial={{ opacity: 0 }}
            animate={{ opacity: 1 }}
            exit={{ opacity: 0 }}
            className="fixed inset-0 bg-black/40 backdrop-blur-sm z-50
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