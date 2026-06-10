import { Routes, Route } from 'react-router-dom'
import DashboardPage from './pages/DashboardPage'
import LoginPage from './pages/LoginPage'
import PlannerPage from './pages/PlannerPage'
import PreferencesPage from './pages/PreferencesPage'

export default function App() {
  return (
    <Routes>
      <Route path="/" element={<DashboardPage />} />
      <Route path="/login" element={<LoginPage />} />
      <Route path="/plan" element={<PlannerPage />} />
      <Route path="/trips/:tripId" element={<PlannerPage />} />
      <Route path="/preferences" element={<PreferencesPage />} />
    </Routes>
  )
}
