import { motion } from 'framer-motion'
import ErrorState from './ErrorState'

interface Props {
  onRetry: () => void
  onBack: () => void
}

export default function ConnectionLostBanner({ onRetry, onBack }: Props) {
  return (
    <motion.div
      initial={{ opacity: 0 }}
      animate={{ opacity: 1 }}
      className="fixed inset-0 z-50 bg-white/90 backdrop-blur-sm
                 flex items-center justify-center p-4"
    >
      <div className="bg-white rounded-3xl shadow-xl border border-gray-100 w-full max-w-sm">
        <ErrorState
          message="Lost connection to the game server. Check your network and try reconnecting."
          onRetry={onRetry}
          onBack={onBack}
          retryLabel="Reconnect"
          backLabel="Leave game"
        />
      </div>
    </motion.div>
  )
}
