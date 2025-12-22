import api from './index'

export const ordersApi = {
  create: async (orderData) => {
    const response = await api.post('/Orders', orderData)
    return response.data
  },
  
  getById: async (id) => {
    const response = await api.get(`/Orders/${id}`)
    return response.data
  },
  
  getAll: async (params = {}) => {
    const response = await api.get('/Orders', { params })
    return response.data
  },
  
  update: async (id, updateData) => {
    const response = await api.put(`/Orders/${id}`, updateData)
    // API возвращает 204 NoContent, поэтому response.data может быть пустым
    return response.data || { success: true }
  },
  
  getMyOrders: async (page = 1, pageSize = 100) => {
    const response = await api.get('/Orders/MyOrders', { 
      params: { page, pageSize } 
    })
    // API возвращает PagedResult с полем data/Data, извлекаем массив
    const result = response.data
    if (result && typeof result === 'object' && !Array.isArray(result)) {
      return result.data || result.Data || []
    }
    return Array.isArray(result) ? result : []
  }
}

