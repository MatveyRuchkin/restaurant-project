import api from './index'

export const menuDishesApi = {
  getAll: async (params = {}) => {
    const response = await api.get('/MenuDishes', { params })
    return response.data.data || response.data
  },
  
  getByMenuId: async (menuId) => {
    const response = await api.get('/MenuDishes', { params: { menuId, pageSize: 1000 } })
    return response.data.data || response.data
  },
  
  create: async (data) => {
    const response = await api.post('/MenuDishes', data)
    return response.data
  },
  
  delete: async (id) => {
    const response = await api.delete(`/MenuDishes/${id}`)
    return response.data
  }
}

