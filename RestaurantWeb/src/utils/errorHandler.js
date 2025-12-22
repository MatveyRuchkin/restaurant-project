import axios from 'axios'

/**
 * Централизованная обработка ошибок API
 * @param {Error} error - Ошибка от axios или другого источника
 * @returns {Object} Объект с информацией об ошибке
 */
export function handleApiError(error) {
  // Если это ошибка отмены запроса (axios cancel)
  if (axios.isCancel && axios.isCancel(error)) {
    return {
      message: 'Запрос отменен',
      type: 'cancel',
      status: null
    }
  }

  // Если есть ответ от сервера
  if (error.response) {
    const status = error.response.status
    const data = error.response.data

    switch (status) {
      case 400:
        return {
          message: data?.message || 'Неверный запрос. Проверьте введенные данные.',
          type: 'validation',
          status: 400,
          errors: data?.errors || null // Для валидационных ошибок полей
        }
      
      case 401:
        return {
          message: 'Сессия истекла. Пожалуйста, войдите снова.',
          type: 'unauthorized',
          status: 401
        }
      
      case 403:
        return {
          message: data?.message || 'У вас нет прав для выполнения этого действия.',
          type: 'forbidden',
          status: 403
        }
      
      case 404:
        return {
          message: data?.message || 'Ресурс не найден.',
          type: 'notFound',
          status: 404
        }
      
      case 422:
        return {
          message: 'Ошибка валидации данных.',
          type: 'validation',
          status: 422,
          errors: data?.errors || null
        }
      
      case 500:
        return {
          message: data?.message || 'Ошибка сервера. Попробуйте позже.',
          type: 'server',
          status: 500
        }
      
      default:
        return {
          message: data?.message || `Ошибка ${status}`,
          type: 'unknown',
          status: status
        }
    }
  }

  // Если запрос отправлен, но ответа нет (сетевые проблемы)
  if (error.request) {
    return {
      message: 'Сервер недоступен. Проверьте подключение к интернету.',
      type: 'network',
      status: null
    }
  }

  // Другая ошибка (например, ошибка в коде)
  return {
    message: error.message || 'Произошла неизвестная ошибка',
    type: 'unknown',
    status: null
  }
}
