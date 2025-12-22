import api from './index'

export const dishesApi = {
  getAll: async (params = {}) => {
    const response = await api.get('/Dishes', { params })
    return response.data.data || response.data
  },
  
  getById: async (id) => {
    const response = await api.get(`/Dishes/${id}`)
    return response.data
  },
  
  create: async (data) => {
    const response = await api.post('/Dishes', data)
    return response.data
  },
  
  update: async (id, data) => {
    const response = await api.put(`/Dishes/${id}`, data)
    return response.data
  },
  
  delete: async (id) => {
    const response = await api.delete(`/Dishes/${id}`)
    return response.data
  }
}

