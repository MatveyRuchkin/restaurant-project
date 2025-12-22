<template>
  <div class="waiter-orders-page">
    <div class="container">
      <h1>Управление заказами</h1>

      <div v-if="loading" class="loading">
        <p>Загрузка заказов...</p>
      </div>

      <div v-else-if="error" class="error-message">
        {{ error }}
      </div>

      <div v-else>
        <!-- Фильтры -->
        <div class="filters">
          <div class="filter-group">
            <label for="statusFilter">Статус:</label>
            <select id="statusFilter" v-model="filters.status" @change="loadOrders" class="form-control">
              <option value="">Все</option>
              <option value="Pending">Ожидает</option>
              <option value="Processing">Готовится</option>
              <option value="Completed">Завершен</option>
              <option value="Cancelled">Отменен</option>
            </select>
          </div>
        </div>

        <!-- Список заказов -->
        <div v-if="orders.length === 0" class="empty-orders">
          <p>Заказы не найдены</p>
        </div>

        <div v-else class="orders-list">
          <OrderCard
            v-for="order in orders"
            :key="order.id"
            :order="order"
            :is-expanded="expandedOrderId === order.id"
            show-user
            @toggle="toggleOrder(order.id)"
          >
            <template #details="{ order }">
              <OrderDetails
                :order="order"
                :items="orderItems[order.id] || []"
                :loading="loadingItems[order.id]"
              >
                <template #actions="{ order }">
                  <!-- Изменение статуса -->
                  <div class="status-controls">
                    <label for="statusSelect">Изменить статус:</label>
                    <select 
                      id="statusSelect"
                      :value="order.status" 
                      @change="updateOrderStatus(order.id, $event.target.value)"
                      class="form-control status-select"
                      :disabled="updatingStatus[order.id]"
                    >
                      <option value="Pending">Ожидает</option>
                      <option value="Processing">Готовится</option>
                      <option value="Completed">Завершен</option>
                      <option value="Cancelled">Отменен</option>
                    </select>
                    <span v-if="updatingStatus[order.id]" class="updating-indicator">Обновление...</span>
                  </div>
                </template>
              </OrderDetails>
            </template>
          </OrderCard>
        </div>

        <!-- Пагинация -->
        <div v-if="pagination.totalPages > 1" class="pagination">
          <button 
            @click="changePage(pagination.page - 1)"
            :disabled="pagination.page === 1"
            class="btn btn-primary"
          >
            Назад
          </button>
          <span class="page-info">
            Страница {{ pagination.page }} из {{ pagination.totalPages }}
          </span>
          <button 
            @click="changePage(pagination.page + 1)"
            :disabled="pagination.page >= pagination.totalPages"
            class="btn btn-primary"
          >
            Вперед
          </button>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, onMounted } from 'vue'
import { ordersApi } from '@/api/orders'
import api from '@/api/index'
import OrderCard from '@/components/orders/OrderCard.vue'
import OrderDetails from '@/components/orders/OrderDetails.vue'

const orders = ref([])
const loading = ref(false)
const error = ref('')
const expandedOrderId = ref(null)
const orderItems = ref({})
const loadingItems = ref({})
const updatingStatus = ref({})

const filters = ref({
  status: ''
})

const pagination = ref({
  page: 1,
  pageSize: 20,
  totalPages: 1,
  totalCount: 0
})

const toggleOrder = async (orderId) => {
  if (expandedOrderId.value === orderId) {
    expandedOrderId.value = null
  } else {
    expandedOrderId.value = orderId
    // Загружаем элементы заказа, если еще не загружены
    if (!orderItems.value[orderId]) {
      await loadOrderItems(orderId)
    }
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
  loading.value = true
  error.value = ''
  
  try {
    const params = {
      page: pagination.value.page,
      pageSize: pagination.value.pageSize,
      sortBy: 'createdAt',
      order: 'desc'
    }
    
    if (filters.value.status) {
      params.status = filters.value.status
    }
    
    const response = await ordersApi.getAll(params)
    
    if (response.data) {
      orders.value = response.data
      pagination.value = {
        page: response.page || pagination.value.page,
        pageSize: response.pageSize || pagination.value.pageSize,
        totalPages: response.totalPages || 1,
        totalCount: response.totalCount || 0
      }
    } else {
      // Если ответ - массив напрямую
      orders.value = response
    }
  } catch (err) {
    console.error('Ошибка загрузки заказов:', err)
    error.value = err.response?.data?.message || 'Произошла ошибка при загрузке заказов'
  } finally {
    loading.value = false
  }
}

const updateOrderStatus = async (orderId, newStatus) => {
  updatingStatus.value[orderId] = true
  
  try {
    await ordersApi.update(orderId, { status: newStatus })
    
    // Обновляем статус в локальном списке
    const order = orders.value.find(o => o.id === orderId)
    if (order) {
      order.status = newStatus
    }
    
    // Показываем уведомление об успехе
    console.log(`Статус заказа ${orderId} изменен на ${newStatus}`)
  } catch (err) {
    console.error('Ошибка обновления статуса:', err)
    error.value = err.response?.data?.message || 'Произошла ошибка при обновлении статуса'
    
    // Перезагружаем заказы для синхронизации
    await loadOrders()
  } finally {
    updatingStatus.value[orderId] = false
  }
}

const changePage = (newPage) => {
  pagination.value.page = newPage
  loadOrders()
}

onMounted(() => {
  loadOrders()
})
</script>

<style scoped>
.waiter-orders-page {
  padding: 2rem;
  background: #F5F5F2;
  min-height: calc(100vh - 200px);
}

.container {
  max-width: 1200px;
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

.filters {
  background: #FFFFFF;
  padding: 1.5rem;
  border-radius: 8px;
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.05);
  margin-bottom: 2rem;
  border: 1px solid #E0E0DC;
}

.filter-group {
  display: flex;
  align-items: center;
  gap: 1rem;
}

.filter-group label {
  font-weight: 600;
  color: #2B2B2B;
  min-width: 100px;
}

.filter-group .form-control {
  max-width: 200px;
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
}

.orders-list {
  display: flex;
  flex-direction: column;
  gap: 1rem;
}

.order-card {
  background: #FFFFFF;
  border-radius: 8px;
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.05);
  border: 1px solid #E0E0DC;
  overflow: hidden;
  transition: box-shadow 0.3s;
}

.order-card:hover {
  box-shadow: 0 4px 12px rgba(0, 0, 0, 0.1);
}

.order-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 1.5rem;
  cursor: pointer;
  transition: background-color 0.2s;
}

.order-header:hover {
  background-color: #F9F9F9;
}

.order-info {
  display: flex;
  flex-direction: column;
  gap: 0.5rem;
  flex: 1;
}

.order-number {
  font-size: 1.2rem;
  color: #2B2B2B;
}

.order-user {
  font-size: 0.95rem;
  color: #6B6B6B;
  font-weight: 500;
}

.order-date {
  font-size: 0.9rem;
  color: #6B6B6B;
}

.order-status {
  display: inline-block;
  padding: 0.25rem 0.75rem;
  border-radius: 4px;
  font-size: 0.85rem;
  font-weight: 600;
  width: fit-content;
}

.status-pending {
  background-color: #FFF4E6;
  color: #E3B23C;
}

.status-processing {
  background-color: #E6F2FF;
  color: #2F5D8C;
}

.status-completed {
  background-color: #E6F7E6;
  color: #2F5D8C;
}

.status-cancelled {
  background-color: #FFE0E0;
  color: #B33A2B;
}

.order-total {
  font-size: 1.5rem;
  font-weight: bold;
  color: #2F5D8C;
  margin: 0 1rem;
}

.expand-icon {
  font-size: 0.9rem;
  color: #6B6B6B;
  transition: transform 0.3s;
}

.order-details {
  padding: 1.5rem;
  border-top: 1px solid #E0E0DC;
  background-color: #F9F9F9;
}

.order-notes {
  padding: 1rem;
  background: #FFFFFF;
  border-radius: 4px;
  margin-bottom: 1rem;
  color: #2B2B2B;
  border-left: 3px solid #E3B23C;
}

.order-notes strong {
  color: #2F5D8C;
}

.loading-items {
  text-align: center;
  padding: 1rem;
  color: #6B6B6B;
}

.order-items {
  margin-top: 1rem;
  margin-bottom: 1rem;
}

.order-items h3 {
  color: #2F5D8C;
  font-size: 1.1rem;
  margin-bottom: 1rem;
}

.order-item {
  display: flex;
  justify-content: space-between;
  align-items: flex-start;
  padding: 0.75rem 0;
  border-bottom: 1px dashed #E0E0DC;
}

.order-item:last-child {
  border-bottom: none;
}

.item-info {
  display: flex;
  flex-direction: column;
  gap: 0.25rem;
  flex: 1;
}

.item-name {
  font-weight: 600;
  color: #2B2B2B;
}

.item-quantity {
  font-size: 0.9rem;
  color: #6B6B6B;
}

.item-notes {
  font-size: 0.85rem;
  color: #6B6B6B;
  font-style: italic;
  margin-top: 0.25rem;
}

.item-price {
  font-weight: 600;
  color: #2F5D8C;
  font-size: 1.1rem;
}

.status-controls {
  margin-top: 1.5rem;
  padding-top: 1.5rem;
  border-top: 1px solid #E0E0DC;
  display: flex;
  align-items: center;
  gap: 1rem;
}

.status-controls label {
  font-weight: 600;
  color: #2B2B2B;
}

.status-select {
  max-width: 200px;
}

.updating-indicator {
  color: #6B6B6B;
  font-size: 0.9rem;
  font-style: italic;
}

.pagination {
  display: flex;
  justify-content: center;
  align-items: center;
  gap: 1rem;
  margin-top: 2rem;
  padding: 1.5rem;
  background: #FFFFFF;
  border-radius: 8px;
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.05);
}

.page-info {
  color: #2B2B2B;
  font-weight: 500;
}

@media (max-width: 768px) {
  .order-header {
    flex-direction: column;
    align-items: flex-start;
    gap: 1rem;
  }

  .order-total {
    margin: 0;
    align-self: flex-end;
  }

  .expand-icon {
    position: absolute;
    top: 1.5rem;
    right: 1.5rem;
  }

  .status-controls {
    flex-direction: column;
    align-items: flex-start;
  }
}
</style>

