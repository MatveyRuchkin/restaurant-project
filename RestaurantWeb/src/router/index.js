import { createRouter, createWebHistory } from 'vue-router'
import { useAuthStore } from '@/stores/auth'
import { useAuth } from '@/composables/useAuth'

const routes = [
  {
    path: '/',
    name: 'Home',
    component: () => import('@/views/Home.vue')
  },
  {
    path: '/menu',
    name: 'Menu',
    component: () => import('@/views/Menu.vue')
  },
  {
    path: '/cart',
    name: 'Cart',
    component: () => import('@/views/Cart.vue'),
    meta: { requiresAuth: true }
  },
  {
    path: '/checkout',
    name: 'Checkout',
    component: () => import('@/views/Checkout.vue'),
    meta: { requiresAuth: true }
  },
  {
    path: '/orders',
    name: 'OrderHistory',
    component: () => import('@/views/OrderHistory.vue'),
    meta: { requiresAuth: true }
  },
  {
    path: '/waiter',
    name: 'WaiterOrders',
    component: () => import('@/views/WaiterOrders.vue'),
    meta: { requiresAuth: true, requiresWaiter: true }
  },
  {
    path: '/login',
    name: 'Login',
    component: () => import('@/views/Home.vue')
  },
  {
    path: '/register',
    name: 'Register',
    component: () => import('@/views/Home.vue')
  },
  {
    path: '/admin',
    component: () => import('@/views/admin/Dashboard.vue'),
    meta: { requiresAuth: true, requiresAdmin: true },
    children: [
      {
        path: '',
        redirect: '/admin/statistics'
      },
      {
        path: 'statistics',
        name: 'AdminStatistics',
        component: () => import('@/views/admin/Statistics.vue')
      },
      {
        path: 'dishes',
        name: 'AdminDishes',
        component: () => import('@/views/admin/Dishes.vue')
      },
      {
        path: 'orders',
        name: 'AdminOrders',
        component: () => import('@/views/admin/Orders.vue')
      },
      {
        path: 'categories',
        name: 'AdminCategories',
        component: () => import('@/views/admin/Categories.vue')
      },
      {
        path: 'menus',
        name: 'AdminMenus',
        component: () => import('@/views/admin/Menus.vue')
      },
      {
        path: 'ingredients',
        name: 'AdminIngredients',
        component: () => import('@/views/admin/Ingredients.vue')
      },
      {
        path: 'users',
        name: 'AdminUsers',
        component: () => import('@/views/admin/Users.vue')
      }
    ]
  }
]

const router = createRouter({
  history: createWebHistory(),
  routes
})

// Навигационный guard: проверка токена и прав доступа
router.beforeEach((to, from, next) => {
  const authStore = useAuthStore()
  const { checkTokenAndRedirect } = useAuth()
  
  // Проверка истечения токена (если истек, выполняется редирект на логин)
  if (checkTokenAndRedirect(to.path)) {
    return
  }
  
  // Проверка прав доступа к маршруту
  if (to.meta.requiresAuth && !authStore.isAuthenticated) {
    next({ name: 'Login', query: { redirect: to.fullPath } })
  } else if (to.meta.requiresAdmin && !authStore.isAdmin) {
    next({ name: 'Home' })
  } else if (to.meta.requiresWaiter && !authStore.isWaiter) {
    next({ name: 'Home' })
  } else {
    next()
  }
})

export default router

