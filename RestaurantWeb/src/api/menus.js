import api from './index'

export const menusApi = {
  getAll: async (params = {}) => {
    const response = await api.get('/Menus', { params })
    return response.data.data || response.data
  },
  
  getById: async (id) => {
    const response = await api.get(`/Menus/${id}`)
    return response.data
  },
  
  create: async (data) => {
    const response = await api.post('/Menus', data)
    return response.data
  },
  
  update: async (id, data) => {
    const response = await api.put(`/Menus/${id}`, data)
    return response.data
  },
  
  delete: async (id) => {
    const response = await api.delete(`/Menus/${id}`)
    return response.data
  }
}

