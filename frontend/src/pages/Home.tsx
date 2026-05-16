import { useState } from 'react'
import { useEffect } from 'react'
import { useNavigate } from 'react-router-dom'
import { motion, AnimatePresence } from 'framer-motion'
import { useGame } from '../hooks/useGame'
import { Flame, Plus, LogIn } from 'lucide-react'

type Tab = 'create' | 'join'

const THEMES = [
  { id: 'dark',      emoji: '💀', label: 'Dark Humor' },
  { id: 'office',    emoji: '💼', label: 'Office Safe' },
  { id: 'genz',      emoji: '📱', label: 'Gen Z' },
  { id: 'chaos',     emoji: '🔥', label: 'Pure Chaos' },
  { id: 'wholesome', emoji: '🌸', label: 'Wholesome' },
]

export default function Home() {
  const navigate = useNavigate()
  const { state, createRoom, joinRoom } = useGame()

  const [tab, setTab]           = useState<Tab>('create')
  const [nickname, setNickname] = useState('')
  const [theme, setTheme]       = useState('chaos')
  const [rounds, setRounds]     = useState(5)
  const [roomCode, setRoomCode] = useState('')
  const [loading, setLoading]   = useState(false)

  // Navigate to game when room is ready

useEffect(() => {
  if (state.roomId && state.roomStatus === 'Lobby') {
    navigate('/game')
  }
}, [state.roomId, state.roomStatus, navigate])

  const handleCreate = async () => {
    if (!nickname.trim()) return
    setLoading(true)
    await createRoom(nickname.trim(), theme, rounds)
    setLoading(false)
  }

  const handleJoin = async () => {
    if (!nickname.trim() || !roomCode.trim()) return
    setLoading(true)
    await joinRoom(roomCode.trim().toUpperCase(), nickname.trim())
    setLoading(false)
  }

  return (
    <div className="min-h-screen bg-gradient-to-br from-mayhem-surface via-white to-blue-50 flex flex-col items-center justify-center p-4">

      {/* Header */}
      <motion.div
        initial={{ y: -40, opacity: 0 }}
        animate={{ y: 0,   opacity: 1 }}
        transition={{ duration: 0.5 }}
        className="text-center mb-10"
      >
        <div className="flex items-center justify-center gap-3 mb-2">
          <Flame className="text-mayhem-primary w-10 h-10" />
          <h1 className="font-display text-5xl font-extrabold text-gray-800">
            Meme Mayhem
          </h1>
          <Flame className="text-mayhem-primary w-10 h-10" />
        </div>
        <p className="text-gray-500 text-lg font-body">
          May the best meme win
        </p>
      </motion.div>

      {/* Card */}
      <motion.div
        initial={{ y: 40, opacity: 0 }}
        animate={{ y: 0,  opacity: 1 }}
        transition={{ duration: 0.5, delay: 0.1 }}
        className="bg-white rounded-3xl shadow-xl w-full max-w-md p-8"
      >
        {/* Tabs */}
        <div className="flex bg-mayhem-surface rounded-2xl p-1 mb-8">
          {(['create', 'join'] as Tab[]).map(t => (
            <button
              key={t}
              onClick={() => setTab(t)}
              className={`
                flex-1 flex items-center justify-center gap-2 py-3 rounded-xl
                font-body font-semibold text-sm transition-all duration-200
                ${tab === t
                  ? 'bg-white shadow text-mayhem-primary'
                  : 'text-gray-400 hover:text-gray-600'
                }
              `}
            >
              {t === 'create'
                ? <><Plus className="w-4 h-4" /> Create Room</>
                : <><LogIn className="w-4 h-4" /> Join Room</>
              }
            </button>
          ))}
        </div>

        <AnimatePresence mode="wait">

          {/* CREATE ROOM */}
          {tab === 'create' && (
            <motion.div
              key="create"
              initial={{ opacity: 0, x: -20 }}
              animate={{ opacity: 1, x: 0 }}
              exit={{    opacity: 0, x: 20 }}
              transition={{ duration: 0.2 }}
              className="space-y-5"
            >
              {/* Nickname */}
              <div>
                <label className="block text-sm font-semibold text-gray-600 mb-2 font-body">
                  Your Nickname
                </label>
                <input
                  type="text"
                  placeholder="Enter your nickname..."
                  value={nickname}
                  onChange={e => setNickname(e.target.value)}
                  maxLength={20}
                  className="w-full px-4 py-3 rounded-xl border-2 border-gray-100 
                             focus:border-mayhem-primary focus:outline-none
                             font-body text-gray-700 transition-colors"
                />
              </div>

              {/* Theme */}
              <div>
                <label className="block text-sm font-semibold text-gray-600 mb-2 font-body">
                  Pick a Theme
                </label>
                <div className="grid grid-cols-5 gap-2">
                  {THEMES.map(t => (
                    <button
                      key={t.id}
                      onClick={() => setTheme(t.id)}
                      className={`
                        flex flex-col items-center p-2 rounded-xl border-2
                        transition-all duration-200 text-xs font-body
                        ${theme === t.id
                          ? 'border-mayhem-primary bg-red-50 text-mayhem-primary'
                          : 'border-gray-100 text-gray-500 hover:border-gray-300'
                        }
                      `}
                    >
                      <span className="text-2xl mb-1">{t.emoji}</span>
                      <span className="text-center leading-tight">{t.label}</span>
                    </button>
                  ))}
                </div>
              </div>

              {/* Rounds */}
              <div>
                <label className="block text-sm font-semibold text-gray-600 mb-2 font-body">
                  Number of Rounds
                  <span className="ml-2 text-mayhem-primary font-bold">
                    {rounds}
                  </span>
                </label>
                <input
                  type="range"
                  min={3}
                  max={15}
                  value={rounds}
                  onChange={e => setRounds(Number(e.target.value))}
                  className="w-full accent-mayhem-primary"
                />
                <div className="flex justify-between text-xs text-gray-400 font-body mt-1">
                  <span>3 rounds</span>
                  <span>15 rounds</span>
                </div>
              </div>

              {/* Error */}
              {state.error && (
                <p className="text-red-500 text-sm font-body text-center">
                  {state.error}
                </p>
              )}

              {/* Create Button */}
              <button
                onClick={handleCreate}
                disabled={!nickname.trim() || loading}
                className={`
                  w-full py-4 rounded-xl font-display font-bold text-white
                  text-lg transition-all duration-200
                  ${nickname.trim() && !loading
                    ? 'bg-mayhem-primary hover:bg-red-500 hover:scale-[1.02] active:scale-[0.98]'
                    : 'bg-gray-200 cursor-not-allowed text-gray-400'
                  }
                `}
              >
                {loading ? 'Creating...' : 'Create Room 🔥'}
              </button>

              <p className="text-center text-xs text-gray-400 font-body">
                Min 3 players recommended
              </p>
            </motion.div>
          )}

          {/* JOIN ROOM */}
          {tab === 'join' && (
            <motion.div
              key="join"
              initial={{ opacity: 0, x: 20 }}
              animate={{ opacity: 1, x: 0 }}
              exit={{    opacity: 0, x: -20 }}
              transition={{ duration: 0.2 }}
              className="space-y-5"
            >
              {/* Nickname */}
              <div>
                <label className="block text-sm font-semibold text-gray-600 mb-2 font-body">
                  Your Nickname
                </label>
                <input
                  type="text"
                  placeholder="Enter your nickname..."
                  value={nickname}
                  onChange={e => setNickname(e.target.value)}
                  maxLength={20}
                  className="w-full px-4 py-3 rounded-xl border-2 border-gray-100
                             focus:border-mayhem-primary focus:outline-none
                             font-body text-gray-700 transition-colors"
                />
              </div>

              {/* Room Code */}
              <div>
                <label className="block text-sm font-semibold text-gray-600 mb-2 font-body">
                  Room Code
                </label>
                <input
                  type="text"
                  placeholder="Enter 6-digit code..."
                  value={roomCode}
                  onChange={e => setRoomCode(e.target.value.toUpperCase())}
                  maxLength={6}
                  className="w-full px-4 py-3 rounded-xl border-2 border-gray-100
                             focus:border-mayhem-primary focus:outline-none
                             font-body text-gray-700 tracking-widest
                             text-center text-2xl font-bold uppercase
                             transition-colors"
                />
              </div>

              {/* Error */}
              {state.error && (
                <p className="text-red-500 text-sm font-body text-center">
                  {state.error}
                </p>
              )}

              {/* Join Button */}
              <button
                onClick={handleJoin}
                disabled={!nickname.trim() || !roomCode.trim() || loading}
                className={`
                  w-full py-4 rounded-xl font-display font-bold text-white
                  text-lg transition-all duration-200
                  ${nickname.trim() && roomCode.trim() && !loading
                    ? 'bg-mayhem-secondary hover:bg-teal-500 hover:scale-[1.02] active:scale-[0.98]'
                    : 'bg-gray-200 cursor-not-allowed text-gray-400'
                  }
                `}
              >
                {loading ? 'Joining...' : 'Join Room 🎮'}
              </button>

              <p className="text-center text-xs text-gray-400 font-body">
                Get the code from your host
              </p>
            </motion.div>
          )}

        </AnimatePresence>
      </motion.div>

      {/* Connection Status */}
      <motion.div
        initial={{ opacity: 0 }}
        animate={{ opacity: 1 }}
        transition={{ delay: 0.5 }}
        className="mt-6 flex items-center gap-2"
      >
        <div className={`w-2 h-2 rounded-full ${
          state.isConnected ? 'bg-green-400' : 'bg-red-400'
        }`} />
        <span className="text-xs text-gray-400 font-body">
          {state.isConnected ? 'Connected' : 'Connecting...'}
        </span>
      </motion.div>

    </div>
  )
}