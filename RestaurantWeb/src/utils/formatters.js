export const formatCurrency = (value) => {
  return new Intl.NumberFormat('ru-RU', {
    style: 'currency',
    currency: 'RUB'
  }).format(value)
}

export const formatDate = (dateString) => {
  const date = new Date(dateString)
  return new Intl.DateTimeFormat('ru-RU', {
    year: 'numeric',
    month: 'long',
    day: 'numeric',
    hour: '2-digit',
    minute: '2-digit'
  }).format(date)
}

export const formatDateShort = (dateString) => {
  const date = new Date(dateString)
  return new Intl.DateTimeFormat('ru-RU', {
    year: 'numeric',
    month: 'short',
    day: 'numeric',
    hour: '2-digit',
    minute: '2-digit'
  }).format(date)
}

/**
 * Форматирует номер заказа в читаемый вид
 * Формат: ORD + HHmmss + - + XXXXX (префикс + время с секундами + дефис + 5 символов из GUID)
 * Пример: ORD143045-A1B2C
 * 
 * @param {string|Guid} orderId - ID заказа (GUID)
 * @param {string|Date} orderDate - Дата заказа
 * @returns {string} Отформатированный номер заказа
 */
export const formatOrderNumber = (orderId, orderDate) => {
  if (!orderId || !orderDate) {
    // Если данных нет, возвращаем короткую версию GUID с префиксом
    return orderId ? `ORD-${orderId.toString().substring(0, 8)}` : 'N/A'
  }
  
  try {
    // Берем первые 5 символов GUID без дефисов и переводим в верхний регистр
    const guidPart = orderId.toString().replace(/-/g, '').substring(0, 5).toUpperCase()
    
    // Парсим дату (ожидается ISO строка с UTC)
    const date = new Date(orderDate)
    
    // Форматируем время: HHmmss (используем UTC время)
    const hours = String(date.getUTCHours()).padStart(2, '0')
    const minutes = String(date.getUTCMinutes()).padStart(2, '0')
    const seconds = String(date.getUTCSeconds()).padStart(2, '0')
    const timePart = `${hours}${minutes}${seconds}`
    
    return `ORD${timePart}-${guidPart}`
  } catch (error) {
    console.error('Ошибка форматирования номера заказа:', error)
    // В случае ошибки возвращаем короткую версию GUID с префиксом
    return orderId ? `ORD-${orderId.toString().substring(0, 8)}` : 'N/A'
  }
}

