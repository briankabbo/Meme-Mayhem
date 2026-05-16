import { useEffect, useState } from 'react'
import { motion } from 'framer-motion'

interface Props {
  seconds: number
  onExpire?: () => void
}

export default function TurnTimer({ seconds, onExpire }: Props) {
  const [timeLeft, setTimeLeft] = useState(seconds)

  useEffect(() => {
    setTimeLeft(seconds)
  }, [seconds])

  useEffect(() => {
    if (timeLeft <= 0) {
      onExpire?.()
      return
    }
    const timer = setTimeout(() => setTimeLeft(t => t - 1), 1000)
    return () => clearTimeout(timer)
  }, [timeLeft, onExpire])

  const percentage = (timeLeft / seconds) * 100

  const color = timeLeft > 10
    ? '#4ECDC4'
    : timeLeft > 5
    ? '#FFE66D'
    : '#FF6B6B'

  return (
    <div className="flex flex-col items-center gap-1">
      {/* Circle Timer */}
      <div className="relative w-16 h-16">
        <svg className="w-16 h-16 -rotate-90" viewBox="0 0 64 64">
          {/* Background circle */}
          <circle
            cx="32" cy="32" r="28"
            fill="none"
            stroke="#F1F5F9"
            strokeWidth="6"
          />
          {/* Progress circle */}
          <motion.circle
            cx="32" cy="32" r="28"
            fill="none"
            stroke={color}
            strokeWidth="6"
            strokeLinecap="round"
            strokeDasharray={`${2 * Math.PI * 28}`}
            strokeDashoffset={`${2 * Math.PI * 28 * (1 - percentage / 100)}`}
            transition={{ duration: 1, ease: 'linear' }}
          />
        </svg>
        {/* Number */}
        <div className="absolute inset-0 flex items-center justify-center">
          <motion.span
            key={timeLeft}
            initial={{ scale: 1.3, opacity: 0 }}
            animate={{ scale: 1,   opacity: 1 }}
            className="font-display font-extrabold text-xl"
            style={{ color }}
          >
            {timeLeft}
          </motion.span>
        </div>
      </div>
      <span className="text-xs text-gray-400 font-body">seconds</span>
    </div>
  )
}