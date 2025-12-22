<template>
  <header class="header">
    <div class="container">
      <nav class="nav">
        <router-link to="/" class="logo-container">
          <img src="/logo.png" class="header-logo"></img>
          <span class="logo">Los Pollos Hermanos</span>
        </router-link>
        <div class="nav-links">
          <router-link to="/" class="nav-link">Главная</router-link>
          <router-link to="/menu" class="nav-link">Меню</router-link>
          <router-link v-if="authStore.isAdmin" to="/admin" class="nav-link">Админ</router-link>
          <router-link 
            v-if="authStore.isWaiter && !authStore.isAdmin" 
            to="/waiter" 
            class="nav-link"
          >
            Сотрудник
          </router-link>
          <router-link 
            v-if="authStore.isAuthenticated" 
            to="/orders" 
            class="nav-link username-link"
          >
            {{ authStore.user?.username }}
          </router-link>
          <button 
            v-if="authStore.isAuthenticated" 
            @click="handleLogout" 
            class="btn btn-danger"
          >
            Выйти
          </button>
          <router-link v-else to="/login" class="btn btn-primary">Войти</router-link>
        </div>
      </nav>
    </div>
  </header>
</template>

<script setup>
import { useAuthStore } from '@/stores/auth'
import { useCartStore } from '@/stores/cart'
import { useRouter } from 'vue-router'

const authStore = useAuthStore()
const cartStore = useCartStore()
const router = useRouter()

const handleLogout = () => {
  authStore.logout()
  router.push('/')
}
</script>

<style scoped>
.header {
  background: #2F5D8C;
  color: white;
  padding: 1rem 0;
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.1);
  border-bottom: 2px solid #254A70;
}

.logo-container {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  text-decoration: none;
  transition: opacity 0.2s;
}

.logo-container:hover {
  opacity: 0.9;
}

.header-logo {
  width: 50px;
  height: 50px;
  object-fit: contain;
}

.nav {
  display: flex;
  justify-content: space-between;
  align-items: center;
}

.logo {
  font-size: 1.5rem;
  font-weight: normal;
  color: white;
  text-decoration: none;
  font-family: 'Bebas Neue', sans-serif;
  letter-spacing: 2px;
  margin: 0;
}

.nav-links {
  display: flex;
  gap: 1rem;
  align-items: center;
}

.nav-link {
  color: white;
  text-decoration: none;
  padding: 0.5rem 1rem;
  border-radius: 4px;
  transition: background-color 0.3s;
}

.nav-link:hover {
  background-color: rgba(255, 255, 255, 0.15);
  transform: translateY(-2px);
}

.nav-link.router-link-active {
  background-color: rgba(255, 255, 255, 0.25);
  font-weight: 600;
}

.username-link {
  font-weight: 600;
  cursor: pointer;
}

.btn-danger {
  background: #E3B23C;
  color: #2B2B2B;
  border: none;
  box-shadow: 0 2px 4px rgba(0, 0, 0, 0.1);
  font-weight: 600;
}

.btn-danger:hover {
  background: #C99A2E;
  transform: translateY(-1px);
  box-shadow: 0 3px 6px rgba(0, 0, 0, 0.15);
}
</style>

