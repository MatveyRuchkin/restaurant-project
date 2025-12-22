<template>
  <div class="auth-modal-overlay" @click.self="closeModal">
    <!-- Фотка справа в воздухе -->
    <div class="auth-image-container">
      <img src="/heisenberg.png" alt="Heisenberg" class="auth-image" />
    </div>
    
    <!-- Модальное окно по центру -->
    <div class="auth-modal-content">
      <div class="auth-form-container">
        <button class="auth-close-btn" @click="closeModal">×</button>
        
        <!-- Форма входа -->
        <div v-if="isLogin" class="auth-form">
          <h1>Вход</h1>
          <form @submit.prevent="handleLogin">
            <div class="form-group">
              <label>Имя пользователя</label>
              <input 
                v-model="loginForm.username" 
                type="text" 
                class="form-control" 
                required
                placeholder="Введите имя пользователя"
              />
            </div>
            <div class="form-group">
              <label>Пароль</label>
              <PasswordInput
                v-model="loginForm.password"
                required
                placeholder="Введите пароль"
              />
            </div>
            <div v-if="loginError" class="error">
              <strong>Ошибка:</strong> {{ loginError }}
            </div>
            <button type="submit" class="btn btn-primary btn-block" :disabled="loginLoading">
              {{ loginLoading ? 'Вход...' : 'Войти' }}
            </button>
            <p class="auth-link">
              Нет аккаунта? <a href="#" @click.prevent="switchToRegister">Зарегистрироваться</a>
            </p>
          </form>
        </div>
        
        <!-- Форма регистрации -->
        <div v-else class="auth-form">
          <h1>Регистрация</h1>
          <form @submit.prevent="handleRegister">
            <div class="form-group">
              <label>Имя пользователя</label>
              <input 
                v-model="registerForm.username" 
                type="text" 
                class="form-control" 
                required
                placeholder="Введите имя пользователя"
              />
            </div>
            <div class="form-group">
              <label>Пароль</label>
              <PasswordInput
                v-model="registerForm.password"
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
                v-model="registerForm.confirmPassword"
                required
                placeholder="Повторите пароль"
                @update:modelValue="validatePasswordMatch"
              />
              <div v-if="passwordMismatch && registerForm.confirmPassword" class="error-text">
                Пароли не совпадают
              </div>
            </div>
            <div v-if="registerError" class="error">{{ registerError }}</div>
            <div v-if="registerSuccess" class="success">Регистрация успешна! Перенаправление...</div>
            <button 
              type="submit" 
              class="btn btn-primary btn-block" 
              :disabled="registerLoading || !isPasswordValid || passwordMismatch"
            >
              {{ registerLoading ? 'Регистрация...' : 'Зарегистрироваться' }}
            </button>
            <p class="auth-link">
              Уже есть аккаунт? <a href="#" @click.prevent="switchToLogin">Войти</a>
            </p>
          </form>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, computed } from 'vue'
import { useRouter, useRoute } from 'vue-router'
import { useAuthStore } from '@/stores/auth'
import PasswordInput from '@/components/forms/PasswordInput.vue'

const props = defineProps({
  isLogin: {
    type: Boolean,
    default: true
  }
})

const emit = defineEmits(['close'])

const router = useRouter()
const route = useRoute()
const authStore = useAuthStore()

// Форма входа
const loginForm = ref({
  username: '',
  password: ''
})
const loginError = ref('')
const loginLoading = ref(false)

// Форма регистрации
const registerForm = ref({
  username: '',
  password: '',
  confirmPassword: ''
})
const registerError = ref('')
const registerSuccess = ref(false)
const registerLoading = ref(false)

// Валидация пароля
const hasMinLength = computed(() => registerForm.value.password.length >= 8)
const hasUpperCase = computed(() => /[A-Z]/.test(registerForm.value.password))
const hasLowerCase = computed(() => /[a-z]/.test(registerForm.value.password))
const hasNumber = computed(() => /[0-9]/.test(registerForm.value.password))
const hasSpecialChar = computed(() => /[^A-Za-z0-9]/.test(registerForm.value.password))

const isPasswordValid = computed(() => {
  return hasMinLength.value && 
         hasUpperCase.value && 
         hasLowerCase.value && 
         hasNumber.value && 
         hasSpecialChar.value
})

const passwordMismatch = computed(() => {
  return registerForm.value.confirmPassword && registerForm.value.password !== registerForm.value.confirmPassword
})

const validatePassword = () => {
  registerError.value = ''
}

const validatePasswordMatch = () => {
  registerError.value = ''
}

const closeModal = () => {
  emit('close')
}

const switchToLogin = () => {
  router.push('/login')
}

const switchToRegister = () => {
  router.push('/register')
}

const handleLogin = async () => {
  loginError.value = ''
  loginLoading.value = true
  
  try {
    const result = await authStore.login(loginForm.value.username, loginForm.value.password)
    
    if (result.success) {
      // Если мы на странице меню, не делаем редирект - просто закрываем модальное окно
      // Логика добавления блюда обрабатывается в Menu.vue через watch
      if (route.path === '/menu') {
        closeModal()
      } else {
        const redirect = route.query.redirect || '/'
        router.push(redirect)
        closeModal()
      }
    } else {
      loginError.value = result.message || 'Ошибка входа'
      console.error('Ошибка входа:', result)
    }
  } catch (err) {
    console.error('Ошибка при входе:', err)
    loginError.value = err.message || 'Произошла ошибка при входе. Проверьте консоль для деталей.'
  } finally {
    loginLoading.value = false
  }
}

const handleRegister = async () => {
  registerError.value = ''
  registerSuccess.value = false
  
  if (!isPasswordValid.value) {
    registerError.value = 'Пароль не соответствует требованиям'
    return
  }
  
  if (registerForm.value.password !== registerForm.value.confirmPassword) {
    registerError.value = 'Пароли не совпадают'
    return
  }
  
  registerLoading.value = true
  
  try {
    const result = await authStore.register(
      registerForm.value.username, 
      registerForm.value.password, 
      registerForm.value.confirmPassword
    )
    
    if (result.success) {
      registerSuccess.value = true
      setTimeout(() => {
        router.push('/login')
      }, 2000)
    } else {
      registerError.value = result.message || 'Ошибка регистрации'
    }
  } catch (err) {
    registerError.value = 'Произошла ошибка при регистрации'
  } finally {
    registerLoading.value = false
  }
}
</script>

<style scoped>
.auth-modal-overlay {
  position: fixed;
  top: 0;
  left: 0;
  right: 0;
  bottom: 0;
  background: rgba(245, 245, 242, 0.6);
  backdrop-filter: blur(4px);
  -webkit-backdrop-filter: blur(4px);
  z-index: 1000;
  display: flex;
  align-items: center;
  justify-content: center;
  padding: 2rem;
}

.auth-modal-content {
  width: 100%;
  max-width: 450px;
  background: rgba(255, 255, 255, 0.95);
  backdrop-filter: blur(20px);
  -webkit-backdrop-filter: blur(20px);
  border-radius: 16px;
  box-shadow: 0 8px 32px rgba(0, 0, 0, 0.2);
  position: relative;
  z-index: 1001;
}

.auth-image-container {
  position: fixed;
  right: 5%;
  top: 50%;
  transform: translateY(-50%) rotate(-8deg);
  z-index: 1002;
  pointer-events: none;
}

.auth-image {
  max-width: 500px;
  width: auto;
  height: auto;
  object-fit: contain;
  filter: drop-shadow(0 8px 16px rgba(0, 0, 0, 0.2));
}

.auth-form-container {
  padding: 3rem;
  position: relative;
}

.auth-close-btn {
  position: absolute;
  top: 1rem;
  right: 1rem;
  background: none;
  border: none;
  font-size: 2rem;
  cursor: pointer;
  color: #6B6B6B;
  line-height: 1;
  padding: 0;
  width: 40px;
  height: 40px;
  display: flex;
  align-items: center;
  justify-content: center;
  border-radius: 50%;
  transition: all 0.2s;
  z-index: 10;
}

.auth-close-btn:hover {
  background: rgba(0, 0, 0, 0.05);
  color: #2B2B2B;
}

.auth-form h1 {
  text-align: center;
  margin-bottom: 2rem;
  color: #2F5D8C;
  font-weight: bold;
  font-size: 2rem;
}

.form-group {
  margin-bottom: 1.5rem;
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

.auth-link {
  text-align: center;
  margin-top: 1.5rem;
  color: #7f8c8d;
}

.auth-link a {
  color: #2F5D8C;
  text-decoration: none;
  font-weight: 500;
}

.auth-link a:hover {
  text-decoration: underline;
}

.error {
  color: #dc3545;
  padding: 0.75rem;
  background-color: #f8d7da;
  border-radius: 4px;
  margin: 1rem 0;
  font-size: 0.9rem;
}

.success {
  color: #28a745;
  padding: 0.75rem;
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

@media (max-width: 1200px) {
  .auth-image-container {
    right: 2%;
    transform: translateY(-50%) rotate(-8deg);
  }
  
  .auth-image {
    max-width: 350px;
  }
}

@media (max-width: 968px) {
  .auth-image-container {
    display: none;
  }
  
  .auth-form-container {
    padding: 2rem;
  }
}

@media (max-width: 768px) {
  .auth-modal-overlay {
    padding: 1rem;
  }
  
  .auth-form-container {
    padding: 1.5rem;
  }
  
  .auth-form h1 {
    font-size: 1.5rem;
  }
}
</style>

