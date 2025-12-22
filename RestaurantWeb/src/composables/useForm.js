import { ref, computed } from 'vue'

/**
 * Composable для работы с формами
 * Предоставляет состояние формы, валидацию и обработку ошибок
 */
export function useForm(initialValues = {}) {
  const form = ref({ ...initialValues })
  const errors = ref({})
  const touched = ref({})

  /**
   * Устанавливает значение поля
   * @param {string} field - Имя поля
   * @param {any} value - Значение
   */
  const setField = (field, value) => {
    form.value[field] = value
    touched.value[field] = true
    // Очищаем ошибку при изменении
    if (errors.value[field]) {
      delete errors.value[field]
    }
  }

  /**
   * Устанавливает ошибку для поля
   * @param {string} field - Имя поля
   * @param {string} message - Сообщение об ошибке
   */
  const setError = (field, message) => {
    errors.value[field] = message
  }

  /**
   * Валидирует форму по правилам
   * @param {Object} rules - Объект с правилами валидации
   * @returns {boolean} true если форма валидна
   */
  const validate = (rules) => {
    errors.value = {}
    let isValid = true

    for (const [field, value] of Object.entries(form.value)) {
      const rule = rules[field]
      if (rule) {
        const error = rule(value, form.value)
        if (error) {
          errors.value[field] = error
          isValid = false
        }
      }
    }

    return isValid
  }

  /**
   * Валидирует одно поле
   * @param {string} field - Имя поля
   * @param {Function} rule - Правило валидации
   * @returns {boolean} true если поле валидно
   */
  const validateField = (field, rule) => {
    touched.value[field] = true
    
    if (rule) {
      const error = rule(form.value[field], form.value)
      if (error) {
        errors.value[field] = error
        return false
      } else {
        delete errors.value[field]
        return true
      }
    }
    
    return true
  }

  /**
   * Сбрасывает форму к начальным значениям
   */
  const reset = () => {
    form.value = { ...initialValues }
    errors.value = {}
    touched.value = {}
  }

  /**
   * Проверяет, была ли форма изменена
   */
  const isDirty = computed(() => {
    return JSON.stringify(form.value) !== JSON.stringify(initialValues)
  })

  /**
   * Проверяет, есть ли ошибки в форме
   */
  const hasErrors = computed(() => {
    return Object.keys(errors.value).length > 0
  })

  return {
    form,
    errors,
    touched,
    setField,
    setError,
    validate,
    validateField,
    reset,
    isDirty,
    hasErrors
  }
}

