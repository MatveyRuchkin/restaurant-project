/**
 * Утилиты для валидации форм
 */

/**
 * Валидация обязательного поля
 * @param {any} value - Значение
 * @param {string} fieldName - Название поля
 * @returns {string|null} Сообщение об ошибке или null
 */
export const validateRequired = (value, fieldName = 'Поле') => {
  if (value === null || value === undefined || value === '') {
    return `${fieldName} обязательно`
  }
  if (typeof value === 'string' && !value.trim()) {
    return `${fieldName} не может быть пустым`
  }
  return null
}

/**
 * Валидация email
 * @param {string} email - Email адрес
 * @returns {string|null} Сообщение об ошибке или null
 */
export const validateEmail = (email) => {
  if (!email) return 'Email обязателен'
  const regex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/
  if (!regex.test(email)) return 'Неверный формат email'
  return null
}

/**
 * Валидация пароля
 * @param {string} password - Пароль
 * @returns {string|null} Сообщение об ошибке или null
 */
export const validatePassword = (password) => {
  if (!password) return 'Пароль обязателен'
  if (password.length < 8) return 'Минимум 8 символов'
  if (!/[A-Z]/.test(password)) return 'Нужна заглавная буква'
  if (!/[a-z]/.test(password)) return 'Нужна строчная буква'
  if (!/[0-9]/.test(password)) return 'Нужна цифра'
  if (!/[^A-Za-z0-9]/.test(password)) return 'Нужен специальный символ'
  return null
}

/**
 * Валидация числа
 * @param {number} value - Значение
 * @param {number|null} min - Минимальное значение
 * @param {number|null} max - Максимальное значение
 * @param {string} fieldName - Название поля
 * @returns {string|null} Сообщение об ошибке или null
 */
export const validateNumber = (value, min = null, max = null, fieldName = 'Значение') => {
  if (value === null || value === undefined || value === '') {
    return `${fieldName} обязательно`
  }
  const num = Number(value)
  if (isNaN(num)) return `${fieldName} должно быть числом`
  if (min !== null && num < min) return `${fieldName} должно быть не менее ${min}`
  if (max !== null && num > max) return `${fieldName} должно быть не более ${max}`
  return null
}

/**
 * Валидация строки (длина)
 * @param {string} value - Значение
 * @param {number} minLength - Минимальная длина
 * @param {number} maxLength - Максимальная длина
 * @param {string} fieldName - Название поля
 * @returns {string|null} Сообщение об ошибке или null
 */
export const validateStringLength = (value, minLength = null, maxLength = null, fieldName = 'Поле') => {
  if (!value && minLength > 0) {
    return `${fieldName} обязательно`
  }
  if (value && minLength !== null && value.length < minLength) {
    return `${fieldName} должно содержать минимум ${minLength} символов`
  }
  if (value && maxLength !== null && value.length > maxLength) {
    return `${fieldName} должно содержать максимум ${maxLength} символов`
  }
  return null
}

/**
 * Валидация совпадения паролей
 * @param {string} password - Пароль
 * @param {string} confirmPassword - Подтверждение пароля
 * @returns {string|null} Сообщение об ошибке или null
 */
export const validatePasswordMatch = (password, confirmPassword) => {
  if (!confirmPassword) return 'Подтверждение пароля обязательно'
  if (password !== confirmPassword) return 'Пароли не совпадают'
  return null
}

