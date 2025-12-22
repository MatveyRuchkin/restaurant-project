import { defineStore } from 'pinia'
import { authApi } from '@/api/auth'
import { ROLES } from '@/constants'
import { handleApiError } from '@/utils/errorHandler'

export const useAuthStore = defineStore('auth', {
  state: () => ({
    user: JSON.parse(localStorage.getItem('user')) || null,
    token: localStorage.getItem('token') || null,
    isAuthenticated: !!localStorage.getItem('token')
  }),
  
  getters: {
    isAdmin: (state) => {
      return state.user?.role === ROLES.ADMIN
    },
    isWaiter: (state) => {
      const role = state.user?.role
      return role === ROLES.WAITER || role === ROLES.ADMIN
    }
  },
  
  actions: {
    decodeToken(token) {
      try {
        const tokenParts = token.split('.')
        if (tokenParts.length !== 3) {
          throw new Error('Неверный формат токена')
        }
        
        const payload = JSON.parse(atob(tokenParts[1]))
        
        const extractedUserId = payload.userId 
          || payload['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier'] 
          || payload.sub
        
        const extractedRole = payload['http://schemas.microsoft.com/ws/2008/06/identity/claims/role'] 
                              || payload['role']
                              || ROLES.USER
        
        const extractedUsername = payload['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name'] 
                                 || payload['name'] 
                                 || payload['unique_name'] 
                                 || payload['sub']
        
        return {
          username: extractedUsername,
          role: extractedRole,
          userId: extractedUserId
        }
      } catch (error) {
        console.error('Ошибка декодирования токена:', error)
        return null
      }
    },
    
    initFromToken() {
      const token = localStorage.getItem('token')
      if (token && !this.user) {
        const userData = this.decodeToken(token)
        if (userData) {
          this.user = userData
          localStorage.setItem('user', JSON.stringify(this.user))
        }
      } else if (token && this.user) {
        const userData = this.decodeToken(token)
        if (userData && userData.role !== this.user.role) {
          this.user = userData
          localStorage.setItem('user', JSON.stringify(this.user))
        }
      }
    },
    
    async login(username, password) {
      try {
        const response = await authApi.login(username, password)
        
        if (!response || !response.token) {
          return { 
            success: false, 
            message: 'Неверный ответ от сервера' 
          }
        }
        
        this.token = response.token
        this.isAuthenticated = true
        
        const userData = this.decodeToken(response.token)
        if (userData) {
          this.user = {
            username: userData.username || username,
            role: userData.role,
            userId: userData.userId
          }
        } else {
          this.user = {
            username: username,
            role: ROLES.USER,
            userId: null
          }
        }
        
        localStorage.setItem('token', response.token)
        localStorage.setItem('user', JSON.stringify(this.user))
        
        // Загрузка корзины после успешного входа (не блокирует процесс входа при ошибке)
        try {
          const { useCartStore } = await import('@/stores/cart')
          const cartStore = useCartStore()
          await cartStore.loadCart()
        } catch (error) {
          console.error('Ошибка загрузки корзины после входа:', error)
        }
        
        return { success: true }
      } catch (error) {
        const errorInfo = handleApiError(error)
        
        // Специальная обработка для 401 - более понятное сообщение для пользователя
        if (errorInfo.type === 'unauthorized') {
          return {
            success: false,
            message: 'Неверный логин или пароль'
          }
        }
        
        return { 
          success: false, 
          message: errorInfo.message
        }
      }
    },
    
    async register(username, password, confirmPassword) {
      try {
        await authApi.register(username, password, confirmPassword)
        return { success: true }
      } catch (error) {
        const errorInfo = handleApiError(error)
        return { 
          success: false, 
          message: errorInfo.message
        }
      }
    },
    
    logout() {
      authApi.logout()
      this.user = null
      this.token = null
      this.isAuthenticated = false
      
      // Очищаем корзину при выходе
      import('@/stores/cart').then(({ useCartStore }) => {
        const cartStore = useCartStore()
        cartStore.items = []
        cartStore.lastSync = null
      }).catch(err => {
        console.error('Ошибка очистки корзины при выходе:', err)
      })
    }
  }
})

