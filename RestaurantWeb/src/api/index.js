import axios from 'axios'
import { handleApiError } from '@/utils/errorHandler'

const baseURL = import.meta.env.VITE_API_BASE_URL || 'http://localhost:8080/api'

if (import.meta.env.DEV) {
  console.log('API Base URL:', baseURL)
}

const api = axios.create({
  baseURL: baseURL,
  headers: {
    'Content-Type': 'application/json'
  },
  timeout: 10000
})

/**
 * Проверяет, истек ли JWT токен
 * @param {string} token - JWT токен
 * @returns {boolean} true если токен истек
 */
export function isTokenExpired(token) {
  try {
    const parts = token.split('.')
    if (parts.length !== 3) return true
    
    const payload = JSON.parse(atob(parts[1]))
    const exp = payload.exp * 1000
    return exp < Date.now()
  } catch {
    return true
  }
}

/**
 * Проверяет токен и очищает данные при истечении
 * @returns {boolean} true если токен истек
 */
function checkTokenAndClear() {
  const token = localStorage.getItem('token')
  if (token && isTokenExpired(token)) {
    localStorage.removeItem('token')
    localStorage.removeItem('user')
    // Очищаем store асинхронно если он доступен
    import('@/stores/auth').then(({ useAuthStore }) => {
      const authStore = useAuthStore()
      authStore.logout()
    }).catch(() => {
      // Store может быть недоступен, это нормально
    })
    return true
  }
  return false
}

api.interceptors.request.use(
  (config) => {
    const token = localStorage.getItem('token')
    
    if (token) {
      // Проверяем токен перед каждым запросом
      if (checkTokenAndClear()) {
        return Promise.reject(new Error('Токен истек'))
      }
      config.headers.Authorization = `Bearer ${token}`
    }
    return config
  },
  (error) => {
    return Promise.reject(error)
  }
)

api.interceptors.response.use(
  (response) => response,
  (error) => {
    // Обрабатываем ошибку централизованно
    const errorInfo = handleApiError(error)
    
    // Автоматический logout при 401
    if (errorInfo.type === 'unauthorized') {
      checkTokenAndClear()
      if (typeof window !== 'undefined' && !window.location.pathname.includes('/login')) {
        window.location.href = '/login?expired=true'
      }
    }
    
    // Логирование в режиме разработки
    if (import.meta.env.DEV) {
      console.error('API Error:', {
        message: errorInfo.message,
        type: errorInfo.type,
        status: errorInfo.status,
        originalError: error
      })
    }
    
    // Добавляем обработанную информацию об ошибке к объекту ошибки
    error.errorInfo = errorInfo
    
    return Promise.reject(error)
  }
)

export default api

