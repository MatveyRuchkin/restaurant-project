import api from './index'

export const statisticsApi = {
  getOverview: async (startDate, endDate) => {
    const params = {}
    if (startDate) params.startDate = startDate
    if (endDate) params.endDate = endDate
    const response = await api.get('/Statistics/overview', { params })
    return response.data
  },
  
  getRevenueByDate: async (startDate, endDate, period = 'day') => {
    const params = {}
    // Всегда передаем period, даже если это значение по умолчанию
    params.period = period || 'day'
    if (startDate) params.startDate = startDate
    if (endDate) params.endDate = endDate
    const response = await api.get('/Statistics/revenue-by-date', { params })
    return response.data
  },
  
  getTopDishes: async (top = 10, startDate, endDate) => {
    const params = { top }
    if (startDate) params.startDate = startDate
    if (endDate) params.endDate = endDate
    const response = await api.get('/Statistics/top-dishes', { params })
    return response.data
  },
  
  getRevenueByCategory: async (startDate, endDate) => {
    const params = {}
    if (startDate) params.startDate = startDate
    if (endDate) params.endDate = endDate
    const response = await api.get('/Statistics/revenue-by-category', { params })
    return response.data
  },
  
  getRecentOrders: async (count = 10) => {
    const response = await api.get('/Statistics/recent-orders', { params: { count } })
    return response.data
  }
}

