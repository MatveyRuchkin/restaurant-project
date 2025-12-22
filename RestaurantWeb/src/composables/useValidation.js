import { computed } from 'vue'

/**
 * Composable для валидации форм
 */
export function useValidation() {
  /**
   * Валидация email
   */
  const validateEmail = (email) => {
    if (!email) return 'Email обязателен'
    const regex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/
    if (!regex.test(email)) return 'Неверный формат email'
    return null
  }

  /**
   * Валидация пароля
   */
  const validatePassword = (password) => {
    if (!password) return 'Пароль обязателен'
    if (password.length < 8) return 'Минимум 8 символов'
    if (!/[A-Z]/.test(password)) return 'Нужна заглавная буква'
    if (!/[a-z]/.test(password)) return 'Нужна строчная буква'
    if (!/[0-9]/.test(password)) return 'Нужна цифра'
    if (!/[^A-Za-z0-9]/.test(password)) return 'Нужен специальный символ'
    return null
  }

  /**
   * Валидация обязательного поля
   */
  const validateRequired = (value, fieldName = 'Поле') => {
    if (value === null || value === undefined || value === '') {
      return `${fieldName} обязательно`
    }
    if (typeof value === 'string' && !value.trim()) {
      return `${fieldName} не может быть пустым`
    }
    return null
  }

  /**
   * Валидация числа
   */
  const validateNumber = (value, min = null, max = null, fieldName = 'Значение') => {
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
   */
  const validateStringLength = (value, min = null, max = null, fieldName = 'Поле') => {
    if (value === null || value === undefined) {
      return `${fieldName} обязательно`
    }
    const str = String(value)
    if (min !== null && str.length < min) {
      return `${fieldName} должно содержать минимум ${min} символов`
    }
    if (max !== null && str.length > max) {
      return `${fieldName} должно содержать максимум ${max} символов`
    }
    return null
  }

  /**
   * Создает computed для проверки пароля
   */
  const createPasswordValidators = (password) => {
    return {
      hasMinLength: computed(() => password.value.length >= 8),
      hasUpperCase: computed(() => /[A-Z]/.test(password.value)),
      hasLowerCase: computed(() => /[a-z]/.test(password.value)),
      hasNumber: computed(() => /[0-9]/.test(password.value)),
      hasSpecialChar: computed(() => /[^A-Za-z0-9]/.test(password.value)),
      isValid: computed(() => {
        return password.value.length >= 8 &&
               /[A-Z]/.test(password.value) &&
               /[a-z]/.test(password.value) &&
               /[0-9]/.test(password.value) &&
               /[^A-Za-z0-9]/.test(password.value)
      })
    }
  }

  return {
    validateEmail,
    validatePassword,
    validateRequired,
    validateNumber,
    validateStringLength,
    createPasswordValidators
  }
}

