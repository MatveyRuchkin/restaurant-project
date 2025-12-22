<template>
  <div class="order-history-page">
    <div class="container">
      <h1>Мои заказы</h1>

      <div v-if="loading" class="loading">
        <p>Загрузка заказов...</p>
      </div>

      <div v-else-if="error" class="error-message">
        <p>{{ error }}</p>
        <div class="error-actions">
          <button @click="loadOrders" class="btn btn-primary">Попробовать снова</button>
          <router-link to="/menu" class="btn btn-secondary">Вернуться в меню</router-link>
        </div>
      </div>

      <div v-else-if="orders.length === 0" class="empty-orders">
        <p>У вас пока нет заказов</p>
        <router-link to="/menu" class="btn btn-primary">Перейти к меню</router-link>
      </div>

      <div v-else class="orders-list">
        <OrderCard
          v-for="order in orders"
          :key="order.id"
          :order="order"
          :is-expanded="expandedOrderId === order.id"
          @toggle="toggleOrder(order.id)"
        >
          <template #details="{ order }">
            <OrderDetails
              :order="order"
              :items="orderItems[order.id] || []"
              :loading="loadingItems[order.id]"
            >
              <template #actions="{ order }">
                <div class="order-actions">
                  <button 
                    @click="repeatOrder(order.id)" 
                    class="btn btn-primary"
                    :disabled="repeatingOrderId === order.id"
                  >
                    <span v-if="repeatingOrderId === order.id">Добавление...</span>
                    <span v-else>Повторить заказ</span>
                  </button>
                </div>
              </template>
            </OrderDetails>
          </template>
        </OrderCard>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { useAuthStore } from '@/stores/auth'
import { useCartStore } from '@/stores/cart'
import { ordersApi } from '@/api/orders'
import { useApi } from '@/composables/useApi'
import { useErrorHandler } from '@/composables/useErrorHandler'
import api from '@/api/index'
import OrderCard from '@/components/orders/OrderCard.vue'
import OrderDetails from '@/components/orders/OrderDetails.vue'

const router = useRouter()
const authStore = useAuthStore()
const cartStore = useCartStore()
const { loading, execute } = useApi()
const { error, handleError, clearError } = useErrorHandler()

const orders = ref([])
const expandedOrderId = ref(null)
const orderItems = ref({})
const loadingItems = ref({})
const repeatingOrderId = ref(null)

const toggleOrder = async (orderId) => {
  try {
    if (expandedOrderId.value === orderId) {
      expandedOrderId.value = null
    } else {
      expandedOrderId.value = orderId
      if (!orderItems.value[orderId]) {
        await loadOrderItems(orderId)
      }
    }
  } catch (err) {
    console.error('Ошибка при раскрытии заказа:', err)
  }
}

const loadOrderItems = async (orderId) => {
  loadingItems.value[orderId] = true
  try {
    const response = await api.get(`/OrderItems/ByOrder/${orderId}`)
    orderItems.value[orderId] = response.data
  } catch (err) {
    console.error('Ошибка загрузки элементов заказа:', err)
    orderItems.value[orderId] = []
  } finally {
    loadingItems.value[orderId] = false
  }
}

const loadOrders = async () => {
  clearError()
  
  const { data, error: apiError } = await execute(() => 
    ordersApi.getMyOrders(1, 100)
  )
  
  if (data) {
    orders.value = Array.isArray(data) ? data : []
  } else if (apiError) {
    handleError(apiError)
  }
}

const repeatOrder = async (orderId) => {
  if (repeatingOrderId.value === orderId) return
  
  repeatingOrderId.value = orderId
  
  try {
    // Загружаем элементы заказа, если они еще не загружены
    if (!orderItems.value[orderId]) {
      await loadOrderItems(orderId)
    }
    
    const items = orderItems.value[orderId] || []
    
    if (items.length === 0) {
      alert('Не удалось загрузить элементы заказа')
      return
    }
    
    // Добавляем каждый элемент заказа в корзину с нужным количеством
    for (const item of items) {
      try {
        // Добавляем блюдо нужное количество раз
        for (let i = 0; i < item.quantity; i++) {
          await cartStore.addItem({
            id: item.dishId,
            dishId: item.dishId,
            price: item.price,
            notes: item.notes || null
          })
        }
      } catch (err) {
        console.error('Ошибка добавления блюда в корзину:', err)
        // Если ошибка авторизации, перенаправляем на логин
        if (err.response?.status === 401) {
          router.push({ name: 'Login', query: { redirect: '/orders' } })
          return
        }
      }
    }
    
    // Перенаправляем на страницу меню
    router.push('/menu')
  } catch (err) {
    console.error('Ошибка при повторении заказа:', err)
    alert('Произошла ошибка при добавлении заказа в корзину')
  } finally {
    repeatingOrderId.value = null
  }
}

onMounted(() => {
  if (!authStore.isAuthenticated) {
    router.push({ name: 'Login', query: { redirect: '/orders' } })
    return
  }
  loadOrders()
})
</script>

<style scoped>
.order-history-page {
  padding: 2rem;
  background: #F5F5F2;
  min-height: calc(100vh - 200px);
}

.container {
  max-width: 1000px;
  margin: 0 auto;
}

h1 {
  font-size: 2.5rem;
  color: #2F5D8C;
  margin-bottom: 2rem;
  text-align: center;
}

.loading, .error-message {
  text-align: center;
  padding: 3rem;
  background: #FFFFFF;
  border-radius: 8px;
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.05);
}

.error-message {
  color: #B33A2B;
  background-color: #FFF5F5;
  border: 1px solid #FFE0E0;
}

.error-message p {
  margin-bottom: 1rem;
}

.error-actions {
  display: flex;
  gap: 1rem;
  justify-content: center;
  margin-top: 1rem;
}

.empty-orders {
  text-align: center;
  padding: 4rem 2rem;
  background: #FFFFFF;
  border-radius: 8px;
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.05);
}

.empty-orders p {
  font-size: 1.2rem;
  color: #6B6B6B;
  margin-bottom: 1.5rem;
}

.orders-list {
  display: flex;
  flex-direction: column;
  gap: 1rem;
}

.order-actions {
  margin-top: 1rem;
  padding-top: 1rem;
  border-top: 1px solid #E0E0DC;
}

.order-actions .btn {
  width: 100%;
}

</style>

