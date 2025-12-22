<template>
  <div class="register-page">
    <div class="register-container">
      <h1>Регистрация</h1>
      <form @submit.prevent="handleRegister" class="register-form">
        <div class="form-group">
          <label>Имя пользователя</label>
          <input 
            v-model="username" 
            type="text" 
            class="form-control" 
            required
            placeholder="Введите имя пользователя"
          />
        </div>
        <div class="form-group">
          <label>Пароль</label>
          <PasswordInput
            v-model="password"
            required
            placeholder="Введите пароль"
            @update:modelValue="validatePassword"
          />
          <div class="password-requirements">
            <div class="requirement" :class="{ valid: hasMinLength }">
              <span>{{ hasMinLength ? '✓' : '✗' }}</span> Минимум 8 символов
            </div>
            <div class="requirement" :class="{ valid: hasUpperCase }">
              <span>{{ hasUpperCase ? '✓' : '✗' }}</span> Заглавная буква
            </div>
            <div class="requirement" :class="{ valid: hasLowerCase }">
              <span>{{ hasLowerCase ? '✓' : '✗' }}</span> Строчная буква
            </div>
            <div class="requirement" :class="{ valid: hasNumber }">
              <span>{{ hasNumber ? '✓' : '✗' }}</span> Цифра
            </div>
            <div class="requirement" :class="{ valid: hasSpecialChar }">
              <span>{{ hasSpecialChar ? '✓' : '✗' }}</span> Специальный символ
            </div>
          </div>
        </div>
        <div class="form-group">
          <label>Подтверждение пароля</label>
          <PasswordInput
            v-model="confirmPassword"
            required
            placeholder="Повторите пароль"
            @update:modelValue="validatePasswordMatch"
          />
          <div v-if="passwordMismatch && confirmPassword" class="error-text">
            Пароли не совпадают
          </div>
        </div>
        <div v-if="error" class="error">{{ error }}</div>
        <div v-if="success" class="success">Регистрация успешна! Перенаправление...</div>
        <button 
          type="submit" 
          class="btn btn-primary btn-block" 
          :disabled="loading || !isPasswordValid || passwordMismatch"
        >
          {{ loading ? 'Регистрация...' : 'Зарегистрироваться' }}
        </button>
        <p class="login-link">
          Уже есть аккаунт? <router-link to="/login">Войти</router-link>
        </p>
      </form>
    </div>
  </div>
</template>

<script setup>
import { ref, computed } from 'vue'
import { useRouter } from 'vue-router'
import { useAuthStore } from '@/stores/auth'
import PasswordInput from '@/components/forms/PasswordInput.vue'

const router = useRouter()
const authStore = useAuthStore()

const username = ref('')
const password = ref('')
const confirmPassword = ref('')
const error = ref('')
const success = ref(false)
const loading = ref(false)

const hasMinLength = computed(() => password.value.length >= 8)
const hasUpperCase = computed(() => /[A-Z]/.test(password.value))
const hasLowerCase = computed(() => /[a-z]/.test(password.value))
const hasNumber = computed(() => /[0-9]/.test(password.value))
const hasSpecialChar = computed(() => /[^A-Za-z0-9]/.test(password.value))

const isPasswordValid = computed(() => {
  return hasMinLength.value && 
         hasUpperCase.value && 
         hasLowerCase.value && 
         hasNumber.value && 
         hasSpecialChar.value
})

const passwordMismatch = computed(() => {
  return confirmPassword.value && password.value !== confirmPassword.value
})

const validatePassword = () => {
  error.value = ''
}

const validatePasswordMatch = () => {
  error.value = ''
}

const handleRegister = async () => {
  error.value = ''
  success.value = false
  
  if (!isPasswordValid.value) {
    error.value = 'Пароль не соответствует требованиям'
    return
  }
  
  if (password.value !== confirmPassword.value) {
    error.value = 'Пароли не совпадают'
    return
  }
  
  loading.value = true
  
  try {
    const result = await authStore.register(username.value, password.value, confirmPassword.value)
    
    if (result.success) {
      success.value = true
      setTimeout(() => {
        router.push('/login')
      }, 2000)
    } else {
      error.value = result.message || 'Ошибка регистрации'
    }
  } catch (err) {
    error.value = 'Произошла ошибка при регистрации'
  } finally {
    loading.value = false
  }
}
</script>

<style scoped>
.register-page {
  min-height: 100vh;
  display: flex;
  align-items: center;
  justify-content: center;
  background: #F5F5F2;
}

.register-container {
  background: #FFFFFF;
  padding: 2rem;
  border-radius: 8px;
  box-shadow: 0 4px 12px rgba(0, 0, 0, 0.1);
  width: 100%;
  max-width: 400px;
  border: 1px solid #E0E0DC;
}

.register-container h1 {
  text-align: center;
  margin-bottom: 2rem;
  color: #2F5D8C;
  font-weight: bold;
}

.form-group {
  margin-bottom: 1rem;
}

.form-group label {
  display: block;
  margin-bottom: 0.5rem;
  color: #2c3e50;
  font-weight: 500;
}

.btn-block {
  width: 100%;
  margin-top: 1rem;
}

.login-link {
  text-align: center;
  margin-top: 1rem;
  color: #7f8c8d;
}

.login-link a {
  color: #007bff;
  text-decoration: none;
}

.login-link a:hover {
  text-decoration: underline;
}

.success {
  color: #28a745;
  padding: 0.5rem;
  background-color: #d4edda;
  border-radius: 4px;
  margin: 1rem 0;
}


.password-requirements {
  margin-top: 0.5rem;
  font-size: 0.85rem;
}

.requirement {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  color: #dc3545;
  margin-bottom: 0.25rem;
}

.requirement.valid {
  color: #28a745;
}

.requirement span {
  font-weight: bold;
  width: 15px;
}

.error-text {
  color: #dc3545;
  font-size: 0.85rem;
  margin-top: 0.25rem;
}
</style>

