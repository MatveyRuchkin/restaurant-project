import api from './index'

export const dishIngredientsApi = {
  getByDishId: async (dishId) => {
    const response = await api.get('/DishIngredients', { 
      params: { dishId, pageSize: 1000 } 
    })
    return response.data.data || response.data
  },
  
  getAll: async (params = {}) => {
    const response = await api.get('/DishIngredients', { params })
    return response.data.data || response.data
  },
  
  create: async (data) => {
    const response = await api.post('/DishIngredients', data)
    return response.data
  },
  
  update: async (id, data) => {
    const response = await api.put(`/DishIngredients/${id}`, data)
    return response.data
  },
  
  delete: async (id) => {
    const response = await api.delete(`/DishIngredients/${id}`)
    return response.data
  }
}

