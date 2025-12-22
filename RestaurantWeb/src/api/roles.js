import api from './index'

export const rolesApi = {
  getAll: async (params = {}) => {
    const response = await api.get('/Roles', { params })
    return response.data.data || response.data
  },
  
  getById: async (id) => {
    const response = await api.get(`/Roles/${id}`)
    return response.data
  }
}

