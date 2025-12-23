<template>
  <div class="orders-page">
    <div class="page-header">
      <h2>Управление заказами</h2>
      <button @click="openGeneratorModal" class="btn btn-success">
        Сгенерировать заказы
      </button>
    </div>

    <div v-if="loading" class="loading">
      <p>Загрузка заказов...</p>
    </div>

    <div v-else-if="error" class="error-message">
      {{ error }}
    </div>

    <div v-else>
      <div class="filters">
        <div class="filter-group">
          <label for="statusFilter">Статус:</label>
          <select id="statusFilter" v-model="filters.status" class="form-control">
            <option value="">Все</option>
            <option value="Pending">Ожидает</option>
            <option value="Processing">Готовится</option>
            <option value="Completed">Завершен</option>
            <option value="Cancelled">Отменен</option>
          </select>
        </div>
        <div class="filter-group">
          <label for="usernameFilter">Пользователь:</label>
          <input id="usernameFilter" v-model="filters.username" type="text" placeholder="Имя пользователя..." class="form-control" />
        </div>
        <div class="filter-group">
          <label for="orderNumberFilter">Номер заказа:</label>
          <input id="orderNumberFilter" v-model="filters.orderNumber" type="text" placeholder="ORD..." class="form-control" />
        </div>
        <div class="filter-group">
          <label for="startDateFilter">Дата от:</label>
          <input id="startDateFilter" v-model="filters.startDate" type="date" class="form-control" />
        </div>
        <div class="filter-group">
          <label for="endDateFilter">Дата до:</label>
          <input id="endDateFilter" v-model="filters.endDate" type="date" class="form-control" />
        </div>
        <div class="filter-group price-range-group">
          <label>Сумма:</label>
          <div class="price-range-inputs">
            <input id="minTotalFilter" v-model.number="filters.minTotal" type="number" step="0.01" min="0" placeholder="Мин." class="form-control price-input" />
            <span class="price-range-separator">-</span>
            <input id="maxTotalFilter" v-model.number="filters.maxTotal" type="number" step="0.01" min="0" placeholder="Макс." class="form-control price-input" />
          </div>
        </div>
        <div class="filter-group">
          <button @click="clearFilters" class="btn btn-secondary btn-sm">Сбросить</button>
        </div>
      </div>

      <div v-if="filteredOrders.length === 0" class="empty-orders">
        <p>Заказы не найдены</p>
      </div>

      <div v-else class="orders-list">
        <OrderCard
          v-for="order in filteredOrders"
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

    <Modal 
      :is-open="showGeneratorModal" 
      title="Генератор заказов"
      large
      @close="closeGeneratorModal"
    >
      <div class="generator-form">
        <div class="form-group">
          <label for="ordersCount">Количество заказов:</label>
          <input 
            id="ordersCount"
            v-model.number="generatorForm.ordersCount" 
            type="number" 
            min="1" 
            max="100"
            class="form-control"
            :class="{ 'error': generatorErrors.ordersCount }"
          />
          <span v-if="generatorErrors.ordersCount" class="error-text">
            {{ generatorErrors.ordersCount }}
          </span>
        </div>

        <div class="form-group">
          <label>Пользователь:</label>
          <div class="info-text">
            Все заказы будут созданы для пользователя <strong>"generator"</strong>
          </div>
          <span v-if="generatorErrors.generatorUser" class="error-text">
            {{ generatorErrors.generatorUser }}
          </span>
        </div>

        <div class="form-group">
          <label for="targetTotal">Целевая сумма заказа (руб.): <span style="color: #B33A2B;">*</span></label>
          <input 
            id="targetTotal"
            v-model.number="generatorForm.targetTotal" 
            type="number" 
            min="0.01"
            step="0.01"
            class="form-control"
            :class="{ 'error': generatorErrors.targetTotal }"
            placeholder="Введите сумму заказа"
            required
          />
          <span v-if="generatorErrors.targetTotal" class="error-text">
            {{ generatorErrors.targetTotal }}
          </span>
          <small class="help-text">Обязательное поле. Блюда будут подобраны так, чтобы сумма заказа была близка к указанной.</small>
        </div>

        <div class="form-group">
          <label for="dishesPerOrder">Количество разновидностей блюд (опционально):</label>
          <input 
            id="dishesPerOrder"
            v-model.number="generatorForm.dishesPerOrder" 
            type="number" 
            min="1" 
            max="20"
            class="form-control"
            :class="{ 'error': generatorErrors.dishesPerOrder }"
            placeholder="Оставьте пустым для автоматического подбора"
          />
          <span v-if="generatorErrors.dishesPerOrder" class="error-text">
            {{ generatorErrors.dishesPerOrder }}
          </span>
          <small class="help-text">Если не указано, количество блюд будет подобрано автоматически для достижения целевой суммы.</small>
        </div>

        <div class="form-group">
          <label for="orderDate">Дата заказа: <span style="color: #6B6B6B;">({{ generatorForm.orderDate || 'не выбрана' }})</span></label>
          <input 
            id="orderDate"
            v-model="generatorForm.orderDate" 
            type="date" 
            class="form-control date-input"
            :class="{ 'error': generatorErrors.orderDate }"
            required
            style="display: block; width: 100%; padding: 0.5rem; border: 1px solid #E0E0DC; border-radius: 4px; font-size: 1rem;"
          />
          <span v-if="generatorErrors.orderDate" class="error-text">
            {{ generatorErrors.orderDate }}
          </span>
          <small class="help-text">Выберите дату для всех генерируемых заказов</small>
        </div>

        <div v-if="generatorStatus.message" 
             :class="['status-message', generatorStatus.type]">
          {{ generatorStatus.message }}
        </div>
      </div>

      <template #footer>
        <button 
          @click="closeGeneratorModal" 
          class="btn btn-secondary"
          :disabled="generating"
        >
          Отмена
        </button>
        <button 
          @click="generateOrders" 
          class="btn btn-primary"
          :disabled="generating"
        >
          <span v-if="generating">Генерация...</span>
          <span v-else>Сгенерировать</span>
        </button>
      </template>
    </Modal>
  </div>
</template>

<script setup>
import { ref, computed, onMounted, watch } from 'vue'
import { ordersApi } from '@/api/orders'
import { usersApi } from '@/api/users'
import { dishesApi } from '@/api/dishes'
import api from '@/api/index'
import OrderCard from '@/components/orders/OrderCard.vue'
import OrderDetails from '@/components/orders/OrderDetails.vue'
import Modal from '@/components/forms/Modal.vue'
import { formatOrderNumber } from '@/utils/formatters'

const orders = ref([])
const users = ref([])
const dishes = ref([])
const loading = ref(false)
const error = ref('')
const expandedOrderId = ref(null)
const orderItems = ref({})
const loadingItems = ref({})
const updatingStatus = ref({})

const showGeneratorModal = ref(false)
const generating = ref(false)

const generatorForm = ref({
  ordersCount: 1,
  targetTotal: null,
  dishesPerOrder: null,
  orderDate: new Date().toISOString().split('T')[0]
})

const generatorUserId = ref(null)

const generatorErrors = ref({})
const generatorStatus = ref({
  type: '',
  message: ''
})


const filters = ref({
  status: '',
  username: '',
  orderNumber: '',
  startDate: '',
  endDate: '',
  minTotal: null,
  maxTotal: null
})

const pagination = ref({
  page: 1,
  pageSize: 20,
  totalPages: 1,
  totalCount: 0
})

// Фильтрация заказов по номеру заказа на клиенте
const filteredOrders = computed(() => {
  if (!filters.value.orderNumber || filters.value.orderNumber.trim() === '') {
    return orders.value
  }
  
  const searchTerm = filters.value.orderNumber.trim().toUpperCase()
  
  return orders.value.filter(order => {
    const orderNumber = formatOrderNumber(order.id, order.createdAt || order.orderDate)
    return orderNumber.toUpperCase().includes(searchTerm)
  })
})

const toggleOrder = async (orderId) => {
  if (expandedOrderId.value === orderId) {
    expandedOrderId.value = null
  } else {
    expandedOrderId.value = orderId
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

const loadUsers = async () => {
  try {
    const data = await usersApi.getAll({ pageSize: 1000 })
    users.value = Array.isArray(data) ? data : (data.data || [])
  } catch (err) {
    console.error('Ошибка загрузки пользователей:', err)
  }
}

const findUserIdByUsername = (username) => {
  if (!username || !users.value.length) return null
  const user = users.value.find(u => u.username.toLowerCase().includes(username.toLowerCase()))
  return user?.id || null
}

const clearFilters = () => {
  filters.value = {
    status: '',
    username: '',
    orderNumber: '',
    startDate: '',
    endDate: '',
    minTotal: null,
    maxTotal: null
  }
  pagination.value.page = 1
  loadOrders()
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
    if (filters.value.username) {
      const userId = findUserIdByUsername(filters.value.username)
      if (userId) {
        params.userId = userId
      } else {
        orders.value = []
        pagination.value = {
          page: 1,
          pageSize: pagination.value.pageSize,
          totalPages: 0,
          totalCount: 0
        }
        loading.value = false
        return
      }
    }
    if (filters.value.startDate) {
      params.startDate = filters.value.startDate
    }
    if (filters.value.endDate) {
      params.endDate = filters.value.endDate
    }
    if (filters.value.minTotal !== null && filters.value.minTotal !== '') {
      params.minTotal = filters.value.minTotal
    }
    if (filters.value.maxTotal !== null && filters.value.maxTotal !== '') {
      params.maxTotal = filters.value.maxTotal
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
    
    const order = orders.value.find(o => o.id === orderId)
    if (order) {
      order.status = newStatus
    }
    
    console.log(`Статус заказа ${orderId} изменен на ${newStatus}`)
  } catch (err) {
    console.error('Ошибка обновления статуса:', err)
    error.value = err.response?.data?.message || 'Произошла ошибка при обновлении статуса'
    await loadOrders()
  } finally {
    updatingStatus.value[orderId] = false
  }
}

const changePage = (newPage) => {
  pagination.value.page = newPage
  loadOrders()
}

const loadDishes = async () => {
  try {
    const data = await dishesApi.getAll({ pageSize: 1000 })
    dishes.value = Array.isArray(data) ? data : (data.data || [])
  } catch (err) {
    console.error('Ошибка загрузки блюд:', err)
  }
}

const findGeneratorUser = () => {
  const generatorUser = users.value.find(u => u.username.toLowerCase() === 'generator' && !u.isDeleted)
  if (generatorUser) {
    generatorUserId.value = generatorUser.id
    return true
  }
  return false
}

const validateGeneratorForm = () => {
  generatorErrors.value = {}
  let isValid = true

  if (!findGeneratorUser()) {
    generatorErrors.value.generatorUser = 'Пользователь "generator" не найден. Создайте пользователя с именем "generator"'
    isValid = false
  }

  if (!generatorForm.value.ordersCount || generatorForm.value.ordersCount < 1) {
    generatorErrors.value.ordersCount = 'Количество заказов должно быть не менее 1'
    isValid = false
  } else if (generatorForm.value.ordersCount > 100) {
    generatorErrors.value.ordersCount = 'Максимум 100 заказов за раз'
    isValid = false
  }

  // Целевая сумма - обязательное поле
  if (generatorForm.value.targetTotal === null || generatorForm.value.targetTotal === undefined) {
    generatorErrors.value.targetTotal = 'Укажите целевую сумму заказа'
    isValid = false
  } else if (generatorForm.value.targetTotal <= 0) {
    generatorErrors.value.targetTotal = 'Сумма должна быть больше нуля'
    isValid = false
  }

  // Количество блюд - опциональное, но если указано, должно быть валидным
  if (generatorForm.value.dishesPerOrder !== null && generatorForm.value.dishesPerOrder !== undefined) {
    if (generatorForm.value.dishesPerOrder < 1) {
      generatorErrors.value.dishesPerOrder = 'Количество блюд должно быть не менее 1'
      isValid = false
    } else if (generatorForm.value.dishesPerOrder > 20) {
      generatorErrors.value.dishesPerOrder = 'Максимум 20 блюд в заказе'
      isValid = false
    }
  }

  if (!generatorForm.value.orderDate) {
    generatorErrors.value.orderDate = 'Укажите дату заказа'
    isValid = false
  }

  // Проверка наличия блюд
  const availableDishesList = dishes.value.filter(d => !d.isDeleted && d.price > 0)
  if (availableDishesList.length === 0) {
    generatorErrors.value.targetTotal = 'Нет доступных блюд'
    isValid = false
  } else if (generatorForm.value.dishesPerOrder !== null && generatorForm.value.dishesPerOrder !== undefined) {
    if (availableDishesList.length < generatorForm.value.dishesPerOrder) {
      generatorErrors.value.dishesPerOrder = `Доступно только ${availableDishesList.length} блюд(а)`
      isValid = false
    }
  }

  return isValid
}


const getRandomDishes = (count) => {
  if (dishes.value.length === 0) return []
  
  const availableDishes = [...dishes.value]
  const selectedDishes = []
  
  for (let i = 0; i < count && availableDishes.length > 0; i++) {
    const randomIndex = Math.floor(Math.random() * availableDishes.length)
    const dish = availableDishes.splice(randomIndex, 1)[0]
    selectedDishes.push(dish)
  }
  
  return selectedDishes
}

const getDishesForTargetTotal = (targetTotal, maxDishes = null) => {
  if (dishes.value.length === 0) return []
  
  const availableDishes = dishes.value
    .filter(d => !d.isDeleted && d.price > 0)
  
  if (availableDishes.length === 0) return []
  
  // Для маленьких сумм используем только дешевые блюда
  // Фильтруем блюда: цена должна быть не больше целевой суммы (с небольшим запасом)
  const maxDishPrice = targetTotal * 1.5 // Блюдо может быть дороже суммы, но не более чем в 1.5 раза
  const suitableDishes = availableDishes.filter(d => d.price <= maxDishPrice)
  
  if (suitableDishes.length === 0) {
    // Если нет подходящих блюд, берем самое дешевое
    const cheapestDish = availableDishes.reduce((min, d) => d.price < min.price ? d : min, availableDishes[0])
    return [cheapestDish]
  }
  
  const tolerance = targetTotal * 0.15 // Уменьшили допуск до 15%
  const minTotal = targetTotal - tolerance
  const maxTotal = targetTotal + tolerance
  
  // Подбор блюд для достижения целевой суммы (до 200 попыток)
  const attempts = 200
  let bestCombination = []
  let bestDifference = Infinity
  let bestTotal = 0
  
  for (let attempt = 0; attempt < attempts; attempt++) {
    // Сортируем блюда по цене (от дешевых к дорогим) для более точного подбора
    const sortedDishes = [...suitableDishes].sort((a, b) => a.price - b.price)
    // Перемешиваем для разнообразия
    const shuffled = sortedDishes.sort(() => Math.random() - 0.5)
    
    const combination = []
    let total = 0
    
    for (const dish of shuffled) {
      // Если указано ограничение по количеству блюд, проверяем его
      if (maxDishes !== null && combination.length >= maxDishes) break
      
      const remaining = maxTotal - total
      if (remaining > 0 && dish.price > 0) {
        // Вычисляем оптимальное количество для достижения целевой суммы
        // Для маленьких сумм используем только 1-2 штуки
        const maxQty = targetTotal < 100 ? 2 : 10
        const optimalQty = Math.min(Math.floor(remaining / dish.price), maxQty)
        
        if (optimalQty > 0) {
          combination.push({ dish, quantity: optimalQty })
          total += dish.price * optimalQty
        }
      }
      
      // Если достигли целевой суммы в пределах допуска, возвращаем комбинацию
      if (total >= minTotal && total <= maxTotal) {
        return combination.map(item => item.dish)
      }
      
      // Если превысили максимальную сумму, прекращаем попытку
      if (total > maxTotal) {
        break
      }
    }
    
    // Сохраняем лучшую комбинацию (ближайшую к целевой сумме)
    const difference = Math.abs(total - targetTotal)
    if (difference < bestDifference && total > 0) {
      bestDifference = difference
      bestCombination = combination
      bestTotal = total
    }
  }
  
  // Если нашли комбинацию, возвращаем её (даже если не идеальная)
  if (bestCombination.length > 0) {
    return bestCombination.map(item => item.dish)
  }
  
  // Если не удалось подобрать, используем самое дешевое блюдо
  const cheapestDish = suitableDishes.reduce((min, d) => d.price < min.price ? d : min, suitableDishes[0])
  return [cheapestDish]
}

const generateOrders = async () => {
  generatorErrors.value = {}
  generatorStatus.value = { type: '', message: '' }

  if (!validateGeneratorForm()) {
    generatorStatus.value = {
      type: 'error',
      message: 'Исправьте ошибки в форме'
    }
    return
  }

  generating.value = true
  generatorStatus.value = { type: '', message: '' }

  try {
    const availableDishesList = dishes.value.filter(d => !d.isDeleted && d.price > 0)
    if (availableDishesList.length === 0) {
      throw new Error('Нет доступных блюд для генерации заказов')
    }

    // Проверка количества блюд только если указано ограничение
    if (generatorForm.value.dishesPerOrder !== null && generatorForm.value.dishesPerOrder !== undefined) {
      if (availableDishesList.length < generatorForm.value.dishesPerOrder) {
        throw new Error(`Недостаточно блюд. Доступно: ${availableDishesList.length}, требуется: ${generatorForm.value.dishesPerOrder}`)
      }
    }

    let successCount = 0
    let errorCount = 0

    if (!generatorUserId.value) {
      throw new Error('Пользователь "generator" не найден')
    }

    for (let i = 0; i < generatorForm.value.ordersCount; i++) {
      try {
        let selectedDishes = []
        let orderItems = []

        // Целевая сумма обязательна
        const targetTotal = generatorForm.value.targetTotal
        const maxDishes = generatorForm.value.dishesPerOrder !== null && generatorForm.value.dishesPerOrder !== undefined 
          ? generatorForm.value.dishesPerOrder 
          : null
        
        selectedDishes = getDishesForTargetTotal(targetTotal, maxDishes)
        
        if (selectedDishes.length === 0) {
          errorCount++
          continue
        }

        // Подбор количеств для достижения целевой суммы
        const tolerance = targetTotal * 0.15 // Допуск 15%
        const minTotal = targetTotal - tolerance
        const maxTotal = targetTotal + tolerance
        
        // Для маленьких сумм используем более строгий подход
        const isSmallAmount = targetTotal < 100
        
        // Начинаем с количества 1 для каждого блюда
        orderItems = selectedDishes.map(dish => ({
          dishId: dish.id,
          quantity: 1,
          price: dish.price,
          notes: null
        }))
        
        let currentTotal = orderItems.reduce((sum, item) => sum + item.price * item.quantity, 0)
        
        // Если сумма меньше минимальной, увеличиваем количества
        if (currentTotal < minTotal) {
          // Сортируем блюда по цене (от дешевых к дорогим) для более равномерного распределения
          const sortedItems = [...orderItems].sort((a, b) => a.price - b.price)
          
          for (const item of sortedItems) {
            if (currentTotal >= minTotal) break
            
            const needed = minTotal - currentTotal
            if (needed <= 0) break
            
            // Для маленьких сумм ограничиваем максимальное количество
            const maxQty = isSmallAmount ? 2 : 10
            const optimalQty = Math.min(
              Math.floor(needed / item.price) + 1,
              maxQty
            )
            
            if (optimalQty > item.quantity) {
              const addQty = optimalQty - item.quantity
              item.quantity = optimalQty
              currentTotal += item.price * addQty
            }
          }
          
          // Если все еще не достигли минимума, добавляем к самому дешевому блюду
          if (currentTotal < minTotal && orderItems.length > 0) {
            const cheapestItem = sortedItems[0]
            const needed = minTotal - currentTotal
            const canAdd = Math.min(
              Math.ceil(needed / cheapestItem.price),
              isSmallAmount ? 2 : 10
            )
            
            if (canAdd > 0) {
              cheapestItem.quantity += canAdd
              currentTotal += cheapestItem.price * canAdd
            }
          }
        }
        
        // Если сумма превысила максимум, пытаемся уменьшить количества
        if (currentTotal > maxTotal && orderItems.length > 1) {
          // Сортируем по убыванию цены для уменьшения дорогих блюд
          const sortedItems = [...orderItems].sort((a, b) => b.price - a.price)
          
          for (const item of sortedItems) {
            if (currentTotal <= maxTotal) break
            if (item.quantity <= 1) continue // Не уменьшаем ниже 1
            
            const excess = currentTotal - maxTotal
            const reduceQty = Math.min(
              Math.ceil(excess / item.price),
              item.quantity - 1
            )
            
            if (reduceQty > 0) {
              item.quantity -= reduceQty
              currentTotal -= item.price * reduceQty
            }
          }
        }
        
        // Если сумма все еще слишком большая, используем только самое дешевое блюдо
        if (currentTotal > maxTotal * 1.5 && orderItems.length > 1) {
          const cheapestDish = selectedDishes.reduce((min, d) => d.price < min.price ? d : min, selectedDishes[0])
          const qty = Math.min(Math.floor(targetTotal / cheapestDish.price), isSmallAmount ? 2 : 10)
          orderItems = [{
            dishId: cheapestDish.id,
            quantity: qty,
            price: cheapestDish.price,
            notes: null
          }]
          currentTotal = cheapestDish.price * qty
        }
        
        // Удаляем поле price перед отправкой (оно не нужно в API)
        orderItems = orderItems.map(({ price, ...item }) => item)

        // Создание даты заказа в UTC времени (начало дня - 00:00:00 UTC)
        if (!generatorForm.value.orderDate) {
          throw new Error('Дата заказа не выбрана')
        }
        
        const dateParts = generatorForm.value.orderDate.split('-')
        if (dateParts.length !== 3) {
          throw new Error('Неверный формат даты')
        }
        
        const year = parseInt(dateParts[0])
        const month = parseInt(dateParts[1]) - 1
        const day = parseInt(dateParts[2])
        
        // Используем UTC время
        const utcDate = new Date(Date.UTC(year, month, day, 0, 0, 0, 0))
        const orderDateValue = utcDate.toISOString()

        const orderData = {
          userId: generatorUserId.value,
          items: orderItems,
          notes: null, // Пустые примечания
          orderDate: orderDateValue
        }

        await ordersApi.create(orderData)
        successCount++

        // Задержка между запросами для снижения нагрузки на сервер
        if (i < generatorForm.value.ordersCount - 1) {
          await new Promise(resolve => setTimeout(resolve, 100))
        }
      } catch (err) {
        console.error(`Ошибка при создании заказа ${i + 1}:`, err)
        errorCount++
      }
    }

    if (successCount > 0) {
      generatorStatus.value = {
        type: 'success',
        message: `Успешно создано заказов: ${successCount}${errorCount > 0 ? `. Ошибок: ${errorCount}` : ''}`
      }
      
      await loadOrders()
      
      setTimeout(() => {
        closeGeneratorModal()
      }, 2000)
    } else {
      generatorStatus.value = {
        type: 'error',
        message: `Не удалось создать ни одного заказа. Ошибок: ${errorCount}`
      }
    }
  } catch (err) {
    console.error('Ошибка генерации заказов:', err)
    generatorStatus.value = {
      type: 'error',
      message: err.response?.data?.message || err.message || 'Произошла ошибка при генерации заказов'
    }
  } finally {
    generating.value = false
  }
}

const openGeneratorModal = () => {
  if (!generatorForm.value.orderDate) {
    generatorForm.value.orderDate = new Date().toISOString().split('T')[0]
  }
  showGeneratorModal.value = true
}

const closeGeneratorModal = () => {
  showGeneratorModal.value = false
  generatorForm.value = {
    ordersCount: 1,
    targetTotal: null,
    dishesPerOrder: null,
    orderDate: new Date().toISOString().split('T')[0]
  }
  generatorErrors.value = {}
  generatorStatus.value = { type: '', message: '' }
}

// Debounce функция для текстовых полей
let searchTimeout = null
const debouncedLoadOrders = () => {
  if (searchTimeout) {
    clearTimeout(searchTimeout)
  }
  searchTimeout = setTimeout(() => {
    pagination.value.page = 1
    loadOrders()
  }, 500)
}

// Реактивные фильтры - немедленная реакция для select и date
watch(() => filters.value.status, () => {
  pagination.value.page = 1
  loadOrders()
})

watch(() => filters.value.startDate, () => {
  pagination.value.page = 1
  loadOrders()
})

watch(() => filters.value.endDate, () => {
  pagination.value.page = 1
  loadOrders()
})

watch(() => filters.value.minTotal, () => {
  pagination.value.page = 1
  loadOrders()
})

watch(() => filters.value.maxTotal, () => {
  pagination.value.page = 1
  loadOrders()
})

// Debounced реакция для текстовых полей
watch(() => filters.value.username, () => {
  debouncedLoadOrders()
})

// Поиск по номеру заказа работает на клиенте через filteredOrders, не требует watch

onMounted(async () => {
  await loadUsers()
  await loadDishes()
  loadOrders()
})
</script>

<style scoped>
.orders-page {
  padding: 2rem 0;
}

.page-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 2rem;
}

h2 {
  color: #2F5D8C;
  margin: 0;
}

.page-header .btn-success {
  background: #28a745;
  color: white;
  border: none;
  padding: 0.75rem 1.5rem;
  border-radius: 4px;
  cursor: pointer;
  font-weight: 600;
  transition: background-color 0.2s;
  font-size: 1rem;
}

.page-header .btn-success:hover {
  background: #218838;
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
  display: flex;
  flex-wrap: wrap;
  gap: 1rem;
  align-items: flex-end;
}

.filter-group {
  display: flex;
  flex-direction: column;
  gap: 0.5rem;
}

.filter-group label {
  font-weight: 600;
  color: #2B2B2B;
  font-size: 0.9rem;
}

.filter-group .form-control {
  min-width: 200px;
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

.price-input {
  -moz-appearance: textfield;
}

.price-input::-webkit-outer-spin-button,
.price-input::-webkit-inner-spin-button {
  -webkit-appearance: none;
  margin: 0;
}

.price-range-group {
  min-width: 300px;
}

.price-range-inputs {
  display: flex;
  align-items: center;
  gap: 0.5rem;
}

.price-range-inputs .price-input {
  flex: 1;
  min-width: 100px;
}

.price-range-separator {
  color: #6B6B6B;
  font-weight: 500;
  flex-shrink: 0;
}

.btn-success {
  background: #28a745;
  color: white;
  border: none;
  padding: 0.5rem 1rem;
  border-radius: 4px;
  cursor: pointer;
  font-weight: 600;
  transition: background-color 0.2s;
}

.btn-success:hover {
  background: #218838;
}

.btn-success:disabled {
  background: #6c757d;
  cursor: not-allowed;
}

.generator-form {
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
  font-weight: 600;
  color: #2B2B2B;
  font-size: 0.9rem;
}

.form-group .form-control {
  padding: 0.5rem;
  border: 1px solid #E0E0DC;
  border-radius: 4px;
  font-size: 1rem;
  width: 100%;
  box-sizing: border-box;
}

.form-group .form-control.date-input {
  min-width: 200px;
  cursor: pointer;
  background-color: #FFFFFF !important;
  color: #2B2B2B !important;
  position: relative;
  z-index: 1;
}

.form-group .form-control.date-input::-webkit-calendar-picker-indicator {
  cursor: pointer;
  opacity: 1;
  z-index: 2;
  position: relative;
}

.form-group .form-control.date-input::-webkit-inner-spin-button,
.form-group .form-control.date-input::-webkit-calendar-picker-indicator {
  cursor: pointer;
}

.form-group .form-control.error {
  border-color: #B33A2B;
}

.help-text {
  display: block;
  margin-top: 0.25rem;
  font-size: 0.85rem;
  color: #6B6B6B;
  font-style: italic;
}

.error-text {
  color: #B33A2B;
  font-size: 0.85rem;
}

.status-message {
  padding: 1rem;
  border-radius: 4px;
  font-weight: 500;
  margin-top: 1rem;
}

.status-message.success {
  background-color: #E6F7E6;
  color: #2F5D8C;
  border: 1px solid #28a745;
}

.status-message.error {
  background-color: #FFF5F5;
  color: #B33A2B;
  border: 1px solid #FFE0E0;
}

.info-text {
  padding: 0.75rem;
  background-color: #E6F2FF;
  border: 1px solid #2F5D8C;
  border-radius: 4px;
  color: #2F5D8C;
  font-size: 0.9rem;
}

.info-text strong {
  font-weight: 600;
}
</style>
