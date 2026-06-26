import axios from 'axios'

export const API_BASE = import.meta.env.VITE_API_URL ?? ''

const api = axios.create({
  baseURL: API_BASE,
  headers: {
    'Content-Type': 'application/json',
    ClientId: 'frontend'
  }
})

let staffToken = localStorage.getItem('staffToken') || ''

export function setStaffToken(token) {
  staffToken = token || ''
  if (staffToken) {
    localStorage.setItem('staffToken', staffToken)
  } else {
    localStorage.removeItem('staffToken')
  }
}

export function getStaffToken() {
  return staffToken
}

api.interceptors.request.use((config) => {
  if (staffToken) {
    config.headers.Authorization = `Bearer ${staffToken}`
  }
  return config
})
export default api
