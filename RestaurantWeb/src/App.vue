<template>
  <div id="app">
    <Header v-if="showHeader" />
    <main class="main-content">
      <router-view />
    </main>
    <Footer v-if="showFooter" />
    
    <!-- Модальное окно входа/регистрации -->
    <AuthModal v-if="showAuthModal" :isLogin="isLoginMode" @close="closeAuthModal" />
  </div>
</template>

<script setup>
import { computed, onMounted } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useAuthStore } from './stores/auth'
import { useAuth } from './composables/useAuth'
import Header from './components/common/Header.vue'
import Footer from './components/common/Footer.vue'
import AuthModal from './components/auth/AuthModal.vue'

const route = useRoute()
const router = useRouter()
const authStore = useAuthStore()
const { checkTokenAndRedirect } = useAuth()

const showHeader = computed(() => route.meta.showHeader !== false)
const showFooter = computed(() => route.meta.showFooter !== false)

const showAuthModal = computed(() => {
  return route.name === 'Login' || route.name === 'Register'
})

const isLoginMode = computed(() => route.name === 'Login')

const closeAuthModal = () => {
  router.push('/')
}

onMounted(async () => {
  // Проверяем токен сразу при монтировании (без периодической проверки)
  checkTokenAndRedirect(route.path)
  
  // Загружаем корзину если пользователь авторизован
  if (authStore.isAuthenticated) {
    try {
      const { useCartStore } = await import('./stores/cart')
      const cartStore = useCartStore()
      await cartStore.loadCart()
    } catch (error) {
      console.error('Ошибка загрузки корзины при монтировании:', error)
    }
  }
})
</script>

<style>
#app {
  min-height: 100vh;
  display: flex;
  flex-direction: column;
  background: #F5F5F2;
}

.main-content {
  flex: 1;
}
</style>

