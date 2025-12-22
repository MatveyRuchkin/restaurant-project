import api from './index'

export const ingredientsApi = {
  getAll: async (params = {}) => {
    const response = await api.get('/Ingredients', { params })
    return response.data.data || response.data
  },
  
  getById: async (id) => {
    const response = await api.get(`/Ingredients/${id}`)
    return response.data
  },
  
  create: async (data) => {
    const response = await api.post('/Ingredients', data)
    return response.data
  },
  
  update: async (id, data) => {
    const response = await api.put(`/Ingredients/${id}`, data)
    return response.data
  },
  
  delete: async (id) => {
    const response = await api.delete(`/Ingredients/${id}`)
    return response.data
  }
}

