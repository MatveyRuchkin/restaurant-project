// Роли пользователей
export const ROLES = {
  ADMIN: 'Admin',
  WAITER: 'Waiter',
  USER: 'User'
}

// Статусы заказов
export const ORDER_STATUSES = {
  PENDING: 'Pending',
  PROCESSING: 'Processing',
  COMPLETED: 'Completed',
  CANCELLED: 'Cancelled'
}

// Тексты статусов на русском
export const ORDER_STATUS_TEXTS = {
  [ORDER_STATUSES.PENDING]: 'Ожидает',
  [ORDER_STATUSES.PROCESSING]: 'Готовится',
  [ORDER_STATUSES.COMPLETED]: 'Завершен',
  [ORDER_STATUSES.CANCELLED]: 'Отменен'
}

