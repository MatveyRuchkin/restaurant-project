import { ref } from 'vue'
import { handleApiError } from '@/utils/errorHandler'

/**
 * Composable для работы с API запросами
 * Предоставляет loading состояние и обработку ошибок
 */
export function useApi() {
  const loading = ref(false)
  const error = ref(null)
  const errorInfo = ref(null)

  /**
   * Выполняет API запрос с обработкой ошибок
   * @param {Function} apiCall - Функция, возвращающая Promise
   * @returns {Promise<{data: any, error: any, errorInfo: any}>}
   */
  const execute = async (apiCall) => {
    loading.value = true
    error.value = null
    errorInfo.value = null

    try {
      const result = await apiCall()
      return { 
        data: result, 
        error: null,
        errorInfo: null
      }
    } catch (err) {
      const info = handleApiError(err)
      error.value = info.message
      errorInfo.value = info
      
      // Логирование в режиме разработки
      if (import.meta.env.DEV) {
        console.error('API Error:', {
          message: info.message,
          type: info.type,
          status: info.status,
          originalError: err
        })
      }

      return { 
        data: null, 
        error: err,
        errorInfo: info
      }
    } finally {
      loading.value = false
    }
  }

  /**
   * Очищает состояние ошибки
   */
  const clearError = () => {
    error.value = null
    errorInfo.value = null
  }

  return {
    loading,
    error,
    errorInfo,
    execute,
    clearError
  }
}
