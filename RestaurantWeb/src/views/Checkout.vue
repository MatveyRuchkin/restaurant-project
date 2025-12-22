<template>
  <div class="checkout-page">
    <div class="container">
      <h1>Оформление заказа</h1>
      
      <div v-if="loading" class="loading">
        <p>Оформление заказа...</p>
      </div>
      
      <div v-else-if="error && !showErrorModal" class="error">
        {{ error }}
      </div>
      
      <div v-else-if="orderSuccess" class="success-message">
        <h2>Заказ успешно оформлен!</h2>
        <p>Номер заказа: <strong>{{ formattedOrderNumber }}</strong></p>
        <p>Сумма заказа: <strong>{{ formatCurrency(orderTotal) }}</strong></p>
        <div class="success-actions">
          <router-link to="/menu" class="btn btn-primary">Вернуться в меню</router-link>
          <router-link to="/" class="btn btn-secondary">На главную</router-link>
        </div>
      </div>
      
      <div v-else class="checkout-content">
        <div class="checkout-section">
          <h2>Ваш заказ</h2>
          <div class="order-items">
            <div 
              v-for="item in cartStore.items" 
              :key="`${item.dishId}-${item.notes || 'default'}`"
              class="order-item"
            >
              <div class="order-item-info">
                <h3>{{ item.dishName }}</h3>
                <p v-if="item.notes" class="order-item-notes">{{ item.notes }}</p>
                <p class="order-item-quantity">Количество: {{ item.quantity }}</p>
              </div>
              <div class="order-item-price">
                {{ formatCurrency(item.subtotal || (item.dishPrice || item.price) * item.quantity) }}
              </div>
            </div>
          </div>
          
          <div class="order-summary">
            <SummaryRow
              label="Всего товаров:"
              :value="cartStore.totalItems.toString()"
            />
            <SummaryRow
              label="Общая сумма:"
              :value="formatCurrency(cartStore.totalPrice)"
              is-total
            />
          </div>
        </div>
        
        <div class="checkout-section">
          <h2>Информация о заказе</h2>
          <form @submit.prevent="submitOrder" class="checkout-form">
            <div class="form-group">
              <label for="notes">Комментарий к заказу (необязательно)</label>
              <textarea
                id="notes"
                v-model="orderNotes"
                class="form-control"
                rows="4"
                placeholder="Укажите особые пожелания и т.д."
              ></textarea>
            </div>
            
            <div class="form-actions">
              <button 
                type="submit" 
                class="btn btn-success btn-large"
                :disabled="cartStore.isEmpty || submitting"
              >
                {{ submitting ? 'Оформление...' : 'Подтвердить заказ' }}
              </button>
              <router-link to="/cart" class="btn btn-secondary">
                Вернуться в корзину
              </router-link>
            </div>
          </form>
        </div>
      </div>
    </div>
    
    <!-- Модальное окно для ошибки о лимите 100 блюд -->
    <Modal
      :is-open="showErrorModal"
      title="Превышен лимит заказа"
      @close="closeErrorModal"
    >
      <p>{{ error }}</p>
      <template #footer>
        <button @click="closeErrorModal" class="btn btn-primary">Понятно</button>
      </template>
    </Modal>
  </div>
</template>

<script setup>
import { ref, computed, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { useCartStore } from '@/stores/cart'
import { useAuthStore } from '@/stores/auth'
import { ordersApi } from '@/api/orders'
import SummaryRow from '@/components/cart/SummaryRow.vue'
import { formatCurrency, formatOrderNumber } from '@/utils/formatters'
import Modal from '@/components/forms/Modal.vue'

const router = useRouter()
const cartStore = useCartStore()
const authStore = useAuthStore()

const loading = ref(false)
const submitting = ref(false)
const error = ref('')
const showErrorModal = ref(false)
const orderSuccess = ref(false)
const orderId = ref(null)
const orderDate = ref(null)
const orderTotal = ref(0)
const orderNotes = ref('')

const formattedOrderNumber = computed(() => {
  if (orderId.value && orderDate.value) {
    return formatOrderNumber(orderId.value, orderDate.value)
  }
  return orderId.value ? orderId.value.toString().substring(0, 8) : 'N/A'
})

onMounted(() => {
  if (cartStore.isEmpty) {
    router.push('/cart')
  }
})

const submitOrder = async () => {
  if (cartStore.isEmpty) {
    error.value = 'Корзина пуста'
    return
  }
  
  if (!authStore.isAuthenticated || !authStore.user?.userId) {
    error.value = 'Необходимо войти в систему'
    router.push('/login')
    return
  }
  
  submitting.value = true
  error.value = ''
  
  try {
    // Получаем userId из токена напрямую, если его нет в user
    let userId = authStore.user?.userId
    
    // Если userId не найден, пытаемся извлечь из токена
    if (!userId && authStore.token) {
      try {
        const tokenParts = authStore.token.split('.')
        if (tokenParts.length === 3) {
          const payload = JSON.parse(atob(tokenParts[1]))
          userId = payload.userId || payload['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier']
        }
      } catch (e) {
        console.error('Ошибка извлечения userId из токена:', e)
      }
    }
    
    if (!userId) {
      error.value = 'Не удалось определить пользователя. Пожалуйста, войдите заново.'
      router.push('/login')
      return
    }
    
    const orderData = {
      userId: userId,
      items: cartStore.items.map(item => ({
        dishId: item.dishId,
        quantity: item.quantity,
        notes: item.notes && item.notes.trim() !== '' ? item.notes.trim() : null
      })),
      notes: orderNotes.value && orderNotes.value.trim() !== '' ? orderNotes.value.trim() : null
    }
    
    console.log('Данные заказа:', JSON.stringify(orderData, null, 2))
    
    // Сохраняем сумму заказа перед очисткой корзины
    orderTotal.value = cartStore.totalPrice
    
    const response = await ordersApi.create(orderData)
    
    orderId.value = response.id
    orderDate.value = response.createdAt || response.orderDate || new Date().toISOString()
    orderSuccess.value = true
    
    // Очищаем корзину после успешного оформления
    await cartStore.clear()
    
  } catch (err) {
    console.error('Ошибка оформления заказа:', err)
    
    let errorMessage = 'Произошла ошибка при оформлении заказа. Попробуйте позже.'
    let shouldShowModal = false
    
    // Обрабатываем ошибки валидации
    if (err.response?.data) {
      const errorData = err.response.data
      
      // Если есть поле errors (ошибки валидации)
      if (errorData.errors && typeof errorData.errors === 'object') {
        // Собираем все сообщения об ошибках
        const errorMessages = []
        for (const key in errorData.errors) {
          if (Array.isArray(errorData.errors[key])) {
            errorMessages.push(...errorData.errors[key])
          } else {
            errorMessages.push(errorData.errors[key])
          }
        }
        
        if (errorMessages.length > 0) {
          errorMessage = errorMessages.join('. ')
          // Проверяем, связана ли ошибка с лимитом 100 блюд
          const errorText = errorMessage.toLowerCase()
          if (errorText.includes('100') || errorText.includes('лимит') || errorText.includes('превышен') || errorText.includes('количество должно быть')) {
            shouldShowModal = true
          }
        }
      } else if (errorData.message) {
        // Обычное сообщение об ошибке
        errorMessage = errorData.message
        const errorText = errorMessage.toLowerCase()
        if (errorText.includes('100') || errorText.includes('лимит') || errorText.includes('превышен')) {
          shouldShowModal = true
        }
      } else if (typeof errorData === 'string') {
        errorMessage = errorData
      }
    }
    
    error.value = errorMessage
    
    if (shouldShowModal) {
      showErrorModal.value = true
    }
  } finally {
    submitting.value = false
  }
}

const closeErrorModal = () => {
  showErrorModal.value = false
  error.value = ''
}
</script>

<style scoped>
.checkout-page {
  padding: 2rem;
  background: #F5F5F2;
  min-height: calc(100vh - 200px);
}

.checkout-page h1 {
  color: #2B2B2B;
  margin-bottom: 2rem;
  font-size: 2.5rem;
}

.checkout-content {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 2rem;
  margin-top: 2rem;
}

.checkout-section {
  background: #FFFFFF;
  padding: 2rem;
  border-radius: 8px;
  box-shadow: 0 2px 4px rgba(0, 0, 0, 0.1);
  border: 1px solid #E0E0DC;
}

.checkout-section h2 {
  color: #2F5D8C;
  margin-bottom: 1.5rem;
  font-size: 1.5rem;
  border-bottom: 2px solid #E3B23C;
  padding-bottom: 0.5rem;
}

.order-items {
  margin-bottom: 1.5rem;
}

.order-item {
  display: flex;
  justify-content: space-between;
  align-items: flex-start;
  padding: 1rem;
  background: #F5F5F2;
  border-radius: 6px;
  margin-bottom: 0.75rem;
  border: 1px solid #E0E0DC;
}

.order-item-info {
  flex: 1;
}

.order-item-info h3 {
  color: #2B2B2B;
  margin-bottom: 0.5rem;
  font-size: 1.1rem;
}

.order-item-notes {
  color: #6B6B6B;
  font-size: 0.9rem;
  margin-bottom: 0.25rem;
}

.order-item-quantity {
  color: #6B6B6B;
  font-size: 0.9rem;
}

.order-item-price {
  font-size: 1.2rem;
  font-weight: bold;
  color: #2F5D8C;
  margin-left: 1rem;
}

.order-summary {
  border-top: 2px solid #E3B23C;
  padding-top: 1rem;
  margin-top: 1rem;
}

.checkout-form {
  display: flex;
  flex-direction: column;
  gap: 1.5rem;
}

.form-group {
  display: flex;
  flex-direction: column;
  gap: 0.5rem;
}

.form-group label {
  color: #2B2B2B;
  font-weight: 500;
}

.form-control {
  width: 100%;
  padding: 0.75rem;
  border: 1px solid #E0E0DC;
  border-radius: 4px;
  font-size: 1rem;
  background: #FFFFFF;
  color: #2B2B2B;
  font-family: inherit;
}

.form-control:focus {
  outline: none;
  border-color: #E3B23C;
  box-shadow: 0 0 0 2px rgba(227, 178, 60, 0.2);
}

.form-actions {
  display: flex;
  flex-direction: column;
  gap: 1rem;
}

.btn-large {
  width: 100%;
  padding: 1rem;
  font-size: 1.2rem;
}

.btn-secondary {
  background: #D9D9D6;
  color: #6B6B6B;
  border: none;
  padding: 0.75rem 1.5rem;
  border-radius: 4px;
  cursor: pointer;
  font-size: 1rem;
  text-decoration: none;
  text-align: center;
  transition: all 0.3s;
}

.btn-secondary:hover {
  background: #C9C9C6;
}

.btn:disabled {
  opacity: 0.6;
  cursor: not-allowed;
}

.loading {
  text-align: center;
  padding: 4rem 2rem;
  color: #6B6B6B;
  font-size: 1.25rem;
}

.error {
  background-color: #F5F5F2;
  color: #2F5D8C;
  padding: 1rem;
  border-radius: 4px;
  margin: 2rem 0;
  border: 1px solid #E0E0DC;
}

.success-message {
  background: #FFFFFF;
  padding: 3rem;
  border-radius: 8px;
  box-shadow: 0 2px 4px rgba(0, 0, 0, 0.1);
  border: 1px solid #E0E0DC;
  text-align: center;
}

.success-message h2 {
  color: #2F5D8C;
  margin-bottom: 1.5rem;
}

.success-message p {
  color: #6B6B6B;
  margin-bottom: 1rem;
  font-size: 1.1rem;
}

.success-message strong {
  color: #2B2B2B;
  font-size: 1.2rem;
}

.success-actions {
  display: flex;
  gap: 1rem;
  justify-content: center;
  margin-top: 2rem;
}

@media (max-width: 968px) {
  .checkout-content {
    grid-template-columns: 1fr;
  }
}
</style>
