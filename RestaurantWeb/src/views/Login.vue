<template>
  <div class="login-page">
    <div class="login-container">
      <h1>Вход</h1>
      <form @submit.prevent="handleLogin" class="login-form">
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
          />
        </div>
        <div v-if="error" class="error">
          <strong>Ошибка:</strong> {{ error }}
          <br>
          <small>Проверьте консоль браузера (F12) для деталей</small>
        </div>
        <button type="submit" class="btn btn-primary btn-block" :disabled="loading">
          {{ loading ? 'Вход...' : 'Войти' }}
        </button>
        <p class="register-link">
          Нет аккаунта? <router-link to="/register">Зарегистрироваться</router-link>
        </p>
      </form>
    </div>
  </div>
</template>

<script setup>
import { ref } from 'vue'
import { useRouter, useRoute } from 'vue-router'
import { useAuthStore } from '@/stores/auth'
import PasswordInput from '@/components/forms/PasswordInput.vue'

const router = useRouter()
const route = useRoute()
const authStore = useAuthStore()

const username = ref('')
const password = ref('')
const error = ref('')
const loading = ref(false)

const handleLogin = async () => {
  error.value = ''
  loading.value = true
  
  try {
    const result = await authStore.login(username.value, password.value)
    
    if (result.success) {
      const redirect = route.query.redirect || '/'
      router.push(redirect)
    } else {
      error.value = result.message || 'Ошибка входа'
      console.error('Ошибка входа:', result)
    }
  } catch (err) {
    console.error('Ошибка при входе:', err)
    error.value = err.message || 'Произошла ошибка при входе. Проверьте консоль для деталей.'
  } finally {
    loading.value = false
  }
}
</script>

<style scoped>
.login-page {
  min-height: 100vh;
  display: flex;
  align-items: center;
  justify-content: center;
  background: #F5F5F2;
}

.login-container {
  background: #FFFFFF;
  padding: 2rem;
  border-radius: 8px;
  box-shadow: 0 4px 12px rgba(0, 0, 0, 0.1);
  width: 100%;
  max-width: 400px;
  border: 1px solid #E0E0DC;
}

.login-container h1 {
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

.register-link {
  text-align: center;
  margin-top: 1rem;
  color: #7f8c8d;
}

.register-link a {
  color: #007bff;
  text-decoration: none;
}

.register-link a:hover {
  text-decoration: underline;
}

</style>

