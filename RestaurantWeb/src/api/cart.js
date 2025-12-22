import api from './index'

export const cartApi = {
  // Получить корзину
  get: async () => {
    const response = await api.get('/Cart')
    return response.data
  },
  
  // Добавить товар
  addItem: async (dishId, quantity = 1, notes = null) => {
    const response = await api.post('/Cart/items', {
      dishId,
      quantity,
      notes: notes && notes.trim() !== '' ? notes.trim() : null
    })
    return response.data
  },
  
  // Обновить количество товара
  updateItem: async (itemId, quantity, notes = null) => {
    const response = await api.put(`/Cart/items/${itemId}`, {
      quantity,
      notes: notes && notes.trim() !== '' ? notes.trim() : null
    })
    return response.data
  },
  
  // Удалить товар
  removeItem: async (itemId) => {
    const response = await api.delete(`/Cart/items/${itemId}`)
    return response.data
  },
  
  // Очистить корзину
  clear: async () => {
    await api.delete('/Cart')
  }
}

