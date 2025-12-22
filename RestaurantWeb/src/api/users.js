import api from './index'

export const usersApi = {
  getAll: async (params = {}) => {
    const response = await api.get('/Users', { params })
    return response.data.data || response.data
  },
  
  getById: async (id) => {
    const response = await api.get(`/Users/${id}`)
    return response.data
  },
  
  create: async (data) => {
    const response = await api.post('/Users', data)
    return response.data
  },
  
  update: async (id, data) => {
    const response = await api.put(`/Users/${id}`, data)
    return response.data
  },
  
  delete: async (id) => {
    const response = await api.delete(`/Users/${id}`)
    return response.data
  }
}

