import api from './index'

export const authApi = {
  login: async (username, password) => {
    const response = await api.post('/Auth/login', { username, password })
    return response.data
  },
  
  register: async (username, password, confirmPassword) => {
    const response = await api.post('/Auth/register', { 
      username, 
      password, 
      confirmPassword 
    })
    return response.data
  },
  
  logout: () => {
    localStorage.removeItem('token')
    localStorage.removeItem('user')
  }
}

