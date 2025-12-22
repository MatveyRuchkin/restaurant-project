import { ref } from 'vue'
import { handleApiError } from '@/utils/errorHandler'

/**
 * Composable для обработки ошибок в компонентах
 * Предоставляет удобные методы для работы с ошибками валидации
 */
export function useErrorHandler() {
  const error = ref(null)
  const errors = ref({}) // Для валидационных ошибок полей
  const errorInfo = ref(null)

  /**
   * Обрабатывает ошибку и извлекает информацию
   * @param {Error} err - Ошибка
   * @returns {Object} Информация об ошибке
   */
  const handleError = (err) => {
    const info = handleApiError(err)
    error.value = info.message
    errorInfo.value = info

    // Если есть ошибки валидации полей
    if (info.errors) {
      errors.value = info.errors
    } else {
      errors.value = {}
    }

    return info
  }

  /**
   * Очищает все ошибки
   */
  const clearError = () => {
    error.value = null
    errors.value = {}
    errorInfo.value = null
  }

  /**
   * Получает ошибку для конкретного поля
   * @param {string} fieldName - Имя поля
   * @returns {string|null} Сообщение об ошибке или null
   */
  const getFieldError = (fieldName) => {
    const fieldErrors = errors.value[fieldName]
    if (Array.isArray(fieldErrors) && fieldErrors.length > 0) {
      return fieldErrors[0]
    }
    if (typeof fieldErrors === 'string') {
      return fieldErrors
    }
    return null
  }

  /**
   * Устанавливает ошибку для поля
   * @param {string} fieldName - Имя поля
   * @param {string} message - Сообщение об ошибке
   */
  const setFieldError = (fieldName, message) => {
    if (!errors.value) {
      errors.value = {}
    }
    errors.value[fieldName] = message
  }

  /**
   * Очищает ошибку для конкретного поля
   * @param {string} fieldName - Имя поля
   */
  const clearFieldError = (fieldName) => {
    if (errors.value[fieldName]) {
      delete errors.value[fieldName]
    }
  }

  return {
    error,
    errors,
    errorInfo,
    handleError,
    clearError,
    getFieldError,
    setFieldError,
    clearFieldError
  }
}
