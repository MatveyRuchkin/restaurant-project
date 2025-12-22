import { useAuthStore } from '@/stores/auth'
import { useRouter } from 'vue-router'
import { isTokenExpired } from '@/api'

/**
 * Composable для работы с аутентификацией
 * Централизует логику проверки токена и выхода
 */
export function useAuth() {
  const authStore = useAuthStore()
  const router = useRouter()

  /**
   * Проверяет токен и выполняет logout если токен истек
   * @returns {boolean} true если токен истек, false если валиден
   */
  const checkTokenAndLogout = () => {
    const token = localStorage.getItem('token')
    if (token && isTokenExpired(token)) {
      localStorage.removeItem('token')
      localStorage.removeItem('user')
      authStore.logout()
      return true // Токен истек
    }
    return false // Токен валиден
  }

  /**
   * Проверяет токен и перенаправляет на страницу входа если токен истек
   * @param {string} currentPath - Текущий путь
   * @returns {boolean} true если токен истек и был выполнен редирект
   */
  const checkTokenAndRedirect = (currentPath = '') => {
    if (checkTokenAndLogout()) {
      // Редирект на логин только если не находимся уже на странице логина/регистрации
      if (!currentPath.includes('/login') && !currentPath.includes('/register')) {
        // Используем replace вместо push, чтобы не создавать новую запись в истории браузера
        router.replace({ name: 'Login', query: { expired: 'true' } })
      }
      return true
    }
    return false
  }

  return {
    checkTokenAndLogout,
    checkTokenAndRedirect
  }
}
