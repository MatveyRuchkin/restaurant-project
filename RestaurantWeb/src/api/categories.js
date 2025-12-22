import api from './index'

export const categoriesApi = {
  getAll: async (params = {}) => {
    const response = await api.get('/Categories', { params })
    return response.data.data || response.data
  },
  
  getById: async (id) => {
    const response = await api.get(`/Categories/${id}`)
    return response.data
  },
  
  create: async (data) => {
    const response = await api.post('/Categories', data)
    return response.data
  },
  
  update: async (id, data) => {
    const response = await api.put(`/Categories/${id}`, data)
    return response.data
  },
  
  delete: async (id) => {
    const response = await api.delete(`/Categories/${id}`)
    return response.data
  }
}

