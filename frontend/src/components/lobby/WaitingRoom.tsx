import { motion, AnimatePresence } from 'framer-motion'
import { useGame } from '../../hooks/useGame'
import { Crown, Users, Copy, Check, Wifi, WifiOff } from 'lucide-react'
import { useState } from 'react'

const THEME_LABELS: Record<string, string> = {
  dark:      '💀 Dark Humor',
  office:    '💼 Office Safe',
  genz:      '📱 Gen Z',
  chaos:     '🔥 Pure Chaos',
  wholesome: '🌸 Wholesome',
}

export default function WaitingRoom() {
  const { state, startGame } = useGame()
  const [copied, setCopied] = useState(false)

  const handleCopy = () => {
    if (!state.roomCode) return
    navigator.clipboard.writeText(state.roomCode)
    setCopied(true)
    setTimeout(() => setCopied(false), 2000)
  }

  const activePlayers = state.players.filter(p => !p.isSpectator)
  const canStart = state.isHost && activePlayers.length >= 2

  return (
    <div className="min-h-screen bg-gradient-to-br from-mayhem-surface via-white to-blue-50 flex flex-col items-center justify-center p-4">

      {/* Header */}
      <motion.div
        initial={{ y: -20, opacity: 0 }}
        animate={{ y: 0,   opacity: 1 }}
        className="text-center mb-8"
      >
        <h1 className="font-display text-4xl font-extrabold text-gray-800 mb-2">
          Waiting Room
        </h1>
        <p className="text-gray-500 font-body">
          Share the code with your friends
        </p>
      </motion.div>

      <div className="w-full max-w-md space-y-4">

        {/* Room Code Card */}
        <motion.div
          initial={{ opacity: 0, y: 20 }}
          animate={{ opacity: 1, y: 0 }}
          transition={{ delay: 0.1 }}
          className="bg-white rounded-3xl shadow-lg p-6 text-center"
        >
          <p className="text-sm font-semibold text-gray-400 font-body mb-2 uppercase tracking-widest">
            Room Code
          </p>
          <div className="flex items-center justify-center gap-4">
            <span className="font-display text-5xl font-extrabold text-mayhem-primary tracking-widest">
              {state.roomCode}
            </span>
            <button
              onClick={handleCopy}
              className="p-3 rounded-xl bg-mayhem-surface hover:bg-gray-100 transition-colors"
            >
              {copied
                ? <Check className="w-5 h-5 text-green-500" />
                : <Copy className="w-5 h-5 text-gray-400" />
              }
            </button>
          </div>
          <p className="text-xs text-gray-400 font-body mt-3">
            {THEME_LABELS[state.players[0]?.nickname] || '🔥 Pure Chaos'} •{' '}
            {state.totalRounds || 5} rounds
          </p>
        </motion.div>

        {/* Players Card */}
        <motion.div
          initial={{ opacity: 0, y: 20 }}
          animate={{ opacity: 1, y: 0 }}
          transition={{ delay: 0.2 }}
          className="bg-white rounded-3xl shadow-lg p-6"
        >
          <div className="flex items-center justify-between mb-4">
            <div className="flex items-center gap-2">
              <Users className="w-5 h-5 text-mayhem-secondary" />
              <span className="font-display font-bold text-gray-700">
                Players
              </span>
            </div>
            <span className="text-sm font-body text-gray-400">
              {activePlayers.length} / 10
            </span>
          </div>

          {/* Player List */}
          <div className="space-y-2">
            <AnimatePresence>
              {state.players.map((player, index) => (
                <motion.div
                  key={player.id}
                  initial={{ opacity: 0, x: -20 }}
                  animate={{ opacity: 1, x: 0  }}
                  exit={{    opacity: 0, x: 20  }}
                  transition={{ delay: index * 0.05 }}
                  className="flex items-center justify-between p-3 rounded-xl bg-mayhem-surface"
                >
                  <div className="flex items-center gap-3">
                    {/* Avatar */}
                    <div className={`
                      w-9 h-9 rounded-full flex items-center justify-center
                      font-display font-bold text-white text-sm
                      ${getAvatarColor(index)}
                    `}>
                      {player.nickname[0].toUpperCase()}
                    </div>

                    {/* Name */}
                    <div>
                      <span className="font-body font-semibold text-gray-700">
                        {player.nickname}
                      </span>
                      {player.id === state.playerId && (
                        <span className="ml-2 text-xs text-mayhem-secondary font-body">
                          (you)
                        </span>
                      )}
                    </div>
                  </div>

                  {/* Badges */}
                  <div className="flex items-center gap-2">
                    {player.isHost && (
                      <Crown className="w-4 h-4 text-yellow-400" />
                    )}
                    {player.isSpectator && (
                      <span className="text-xs bg-gray-100 text-gray-400 px-2 py-1 rounded-full font-body">
                        Spectator
                      </span>
                    )}
                    {player.isConnected
                      ? <Wifi    className="w-4 h-4 text-green-400" />
                      : <WifiOff className="w-4 h-4 text-red-400" />
                    }
                  </div>
                </motion.div>
              ))}
            </AnimatePresence>

            {/* Empty slots */}
            {activePlayers.length < 3 && (
              <motion.p
                initial={{ opacity: 0 }}
                animate={{ opacity: 1 }}
                className="text-center text-sm text-gray-400 font-body py-2"
              >
                Waiting for more players... (min 3 recommended)
              </motion.p>
            )}
          </div>
        </motion.div>

        {/* Start Button — host only */}
        {state.isHost && (
          <motion.div
            initial={{ opacity: 0, y: 20 }}
            animate={{ opacity: 1, y: 0 }}
            transition={{ delay: 0.3 }}
          >
            <button
              onClick={startGame}
              disabled={!canStart}
              className={`
                w-full py-4 rounded-2xl font-display font-bold text-white
                text-xl transition-all duration-200
                ${canStart
                  ? 'bg-mayhem-primary hover:bg-red-500 hover:scale-[1.02] active:scale-[0.98] shadow-lg'
                  : 'bg-gray-200 text-gray-400 cursor-not-allowed'
                }
              `}
            >
              {canStart ? 'Start Game 🔥' : 'Need at least 2 players'}
            </button>
          </motion.div>
        )}

        {/* Waiting message — non-host */}
        {!state.isHost && (
          <motion.div
            initial={{ opacity: 0 }}
            animate={{ opacity: 1 }}
            transition={{ delay: 0.3 }}
            className="text-center"
          >
            <div className="inline-flex items-center gap-2 bg-white rounded-2xl px-6 py-3 shadow">
              <motion.div
                animate={{ rotate: 360 }}
                transition={{ duration: 2, repeat: Infinity, ease: 'linear' }}
                className="w-4 h-4 border-2 border-mayhem-primary border-t-transparent rounded-full"
              />
              <span className="font-body text-gray-500">
                Waiting for host to start...
              </span>
            </div>
          </motion.div>
        )}

      </div>
    </div>
  )
}

// Avatar colors cycling
function getAvatarColor(index: number): string {
  const colors = [
    'bg-mayhem-primary',
    'bg-mayhem-secondary',
    'bg-mayhem-purple',
    'bg-yellow-400',
    'bg-green-400',
    'bg-blue-400',
    'bg-pink-400',
    'bg-orange-400',
    'bg-indigo-400',
    'bg-teal-400',
  ]
  return colors[index % colors.length]
}