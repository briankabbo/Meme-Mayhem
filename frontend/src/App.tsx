// src/App.tsx
import { BrowserRouter, Routes, Route } from 'react-router-dom'
import { GameProvider } from './context/GameContext'
import Home from './pages/Home'
import Game from './pages/Game'
import Results from './pages/Results'

export default function App() {
  return (
    <GameProvider>
      <BrowserRouter>
        <Routes>
          <Route path="/"        element={<Home />} />
          <Route path="/game"    element={<Game />} />
          <Route path="/results" element={<Results />} />
        </Routes>
      </BrowserRouter>
    </GameProvider>
  )
}