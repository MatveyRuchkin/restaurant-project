import { defineStore } from 'pinia'
import { cartApi } from '@/api/cart'

export const useCartStore = defineStore('cart', {
  state: () => ({
    items: [],
    isLoading: false,
    lastSync: null,
    isOpen: false
  }),
  
  getters: {
    totalItems: (state) => 
      state.items.reduce((sum, item) => sum + item.quantity, 0),
    
    totalPrice: (state) => 
      state.items.reduce((sum, item) => sum + item.subtotal, 0),
    
    isEmpty: (state) => state.items.length === 0,
    
    // Получение общего количества блюда в корзине (суммирует все варианты с разными примечаниями)
    getDishQuantity: (state) => (dishId) => {
      return state.items
        .filter(item => item.dishId === dishId)
        .reduce((sum, item) => sum + item.quantity, 0)
    }
  },
  
  actions: {
    async loadCart() {
      this.isLoading = true
      try {
        const cartData = await cartApi.get()
        this.items = cartData.items || []
        this.lastSync = new Date()
      } catch (error) {
        console.error('Ошибка загрузки корзины:', error)
        // При ошибке авторизации очищаем корзину
        if (error.response?.status === 401) {
          this.items = []
        }
      } finally {
        this.isLoading = false
      }
    },
    
    async addItem(dish) {
      try {
        const cartData = await cartApi.addItem(
          dish.id,
          1,
          dish.notes || null
        )
        this.items = cartData.items || []
        this.lastSync = new Date()
      } catch (error) {
        console.error('Ошибка добавления товара:', error)
        throw error
      }
    },
    
    async updateQuantity(dishId, quantity, notes = null) {
      const item = this.items.find(item => 
        item.dishId === dishId && item.notes === (notes || null)
      )
      
      if (!item) {
        console.warn('Элемент корзины не найден для обновления:', { dishId, notes })
        return
      }
      
      if (quantity <= 0) {
        await this.removeItem(dishId, notes)
        return
      }
      
      try {
        const cartData = await cartApi.updateItem(item.id, quantity, notes)
        this.items = cartData.items || []
        this.lastSync = new Date()
      } catch (error) {
        console.error('Ошибка обновления количества:', error)
        throw error
      }
    },
    
    async removeItem(dishId, notes = null) {
      const item = this.items.find(item => 
        item.dishId === dishId && item.notes === (notes || null)
      )
      
      if (!item) {
        console.warn('Элемент корзины не найден для удаления:', { dishId, notes })
        return
      }
      
      try {
        const cartData = await cartApi.removeItem(item.id)
        this.items = cartData.items || []
        this.lastSync = new Date()
      } catch (error) {
        console.error('Ошибка удаления товара:', error)
        throw error
      }
    },
    
    async clear() {
      try {
        await cartApi.clear()
        this.items = []
        this.lastSync = new Date()
      } catch (error) {
        console.error('Ошибка очистки корзины:', error)
        throw error
      }
    },
    
    openCart() {
      this.isOpen = true
    },
    
    closeCart() {
      this.isOpen = false
    },
    
    toggleCart() {
      this.isOpen = !this.isOpen
    }
  }
})

