import { AlertCircle, WifiOff, Users, SearchX } from 'lucide-react'

type ErrorKind = 'room-not-found' | 'room-full' | 'connection-lost' | 'generic'

interface Props {
  message: string
  onRetry?: () => void
  onBack?: () => void
  retryLabel?: string
  backLabel?: string
}

function classifyError(message: string): ErrorKind {
  const lower = message.toLowerCase()
  if (lower.includes('room not found') || lower.includes('could not reconnect'))
    return 'room-not-found'
  if (lower.includes('room is full') || lower.includes('max 10'))
    return 'room-full'
  if (lower.includes('not connected') || lower.includes('connection'))
    return 'connection-lost'
  return 'generic'
}

const ERROR_CONFIG: Record<ErrorKind, {
  icon: typeof AlertCircle
  title: string
  defaultRetry: string
  defaultBack: string
}> = {
  'room-not-found': {
    icon: SearchX,
    title: 'Room not found',
    defaultRetry: 'Try another code',
    defaultBack: 'Back to home',
  },
  'room-full': {
    icon: Users,
    title: 'Room is full',
    defaultRetry: 'Try again',
    defaultBack: 'Back to home',
  },
  'connection-lost': {
    icon: WifiOff,
    title: 'Connection lost',
    defaultRetry: 'Reconnect',
    defaultBack: 'Back to home',
  },
  generic: {
    icon: AlertCircle,
    title: 'Something went wrong',
    defaultRetry: 'Try again',
    defaultBack: 'Back to home',
  },
}

export default function ErrorState({
  message,
  onRetry,
  onBack,
  retryLabel,
  backLabel,
}: Props) {
  const kind = classifyError(message)
  const config = ERROR_CONFIG[kind]
  const Icon = config.icon

  return (
    <div className="flex flex-col items-center justify-center text-center px-6 py-10">
      <div className="w-14 h-14 rounded-2xl bg-red-50 flex items-center justify-center mb-4">
        <Icon className="w-7 h-7 text-red-400" />
      </div>
      <h2 className="font-display font-bold text-gray-800 text-lg mb-2">
        {config.title}
      </h2>
      <p className="text-sm text-gray-500 font-body max-w-xs mb-6">
        {message}
      </p>
      <div className="flex flex-col sm:flex-row gap-3 w-full max-w-xs">
        {onRetry && (
          <button
            onClick={onRetry}
            className="flex-1 py-3 px-4 rounded-xl bg-mayhem-primary text-white
                       font-body font-semibold text-sm hover:bg-red-500 transition-colors"
          >
            {retryLabel ?? config.defaultRetry}
          </button>
        )}
        {onBack && (
          <button
            onClick={onBack}
            className="flex-1 py-3 px-4 rounded-xl bg-gray-100 text-gray-600
                       font-body font-semibold text-sm hover:bg-gray-200 transition-colors"
          >
            {backLabel ?? config.defaultBack}
          </button>
        )}
      </div>
    </div>
  )
}
