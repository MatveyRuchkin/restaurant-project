<template>
  <div class="statistics-page">
    <h2>Статистика</h2>
    
    <div v-if="loading" class="loading">Загрузка статистики...</div>
    <div v-else-if="error" class="error-message">
      <p><strong>Ошибка:</strong> {{ error }}</p>
      <p v-if="error.includes('401') || error.includes('авторизац')" class="error-hint">
        Проверьте, что вы вошли как администратор и ваш токен не истёк.
      </p>
      <p v-else-if="error.includes('403')" class="error-hint">
        У вас нет прав доступа к статистике. Требуется роль администратора.
      </p>
      <p v-else class="error-hint">
        Проверьте консоль браузера (F12) для получения дополнительной информации.
      </p>
    </div>
    <div v-else>
      <!-- Общая статистика -->
      <!-- Первый ряд: 3 карточки -->
      <div class="stats-cards-row">
        <div class="stat-card">
          <h3>Общая выручка</h3>
          <p class="stat-value">{{ formatCurrency(overview.totalRevenue) }}</p>
        </div>
        <div class="stat-card">
          <h3>Выручка за месяц</h3>
          <p class="stat-value">{{ formatCurrency(monthlyStats.revenue) }}</p>
          <p v-if="monthlyStats.revenueChange !== null" class="stat-comparison" :class="getComparisonClass(monthlyStats.revenueChange)">
            {{ formatComparison(monthlyStats.revenueChange) }} к предыдущему месяцу
          </p>
        </div>
        <div class="stat-card">
          <h3>Средний чек</h3>
          <p class="stat-value">{{ formatCurrency(overview.averageOrderValue) }}</p>
        </div>
      </div>
      
      <!-- Второй ряд: 4 карточки -->
      <div class="stats-cards-row stats-cards-row-bottom">
        <div class="stat-card">
          <h3>Всего заказов</h3>
          <p class="stat-value">{{ overview.totalOrders }}</p>
        </div>
        <div class="stat-card">
          <h3>Заказов за месяц</h3>
          <p class="stat-value">{{ monthlyStats.orders }}</p>
          <p v-if="monthlyStats.ordersChange !== null" class="stat-comparison" :class="getComparisonClass(monthlyStats.ordersChange)">
            {{ formatComparison(monthlyStats.ordersChange) }} к предыдущему месяцу
          </p>
        </div>
        <div class="stat-card">
          <h3>Заказов за сегодня</h3>
          <p class="stat-value">{{ todayStats.orders }}</p>
          <p v-if="todayStats.ordersChange !== null" class="stat-comparison" :class="getComparisonClass(todayStats.ordersChange)">
            {{ formatComparison(todayStats.ordersChange) }} к вчера
          </p>
        </div>
        <div class="stat-card">
          <h3>Всего пользователей</h3>
          <p class="stat-value">{{ overview.totalUsers }}</p>
          <p v-if="usersStats.change !== null" class="stat-comparison" :class="getComparisonClass(usersStats.change)">
            {{ formatComparison(usersStats.change) }} к предыдущему месяцу
          </p>
        </div>
      </div>

      <!-- График выручки по датам -->
      <div class="section chart-section">
        <div class="chart-header">
          <h3>Выручка по датам</h3>
          <div class="chart-controls">
            <select v-model="revenuePeriod" @change="loadRevenueChart" class="form-control">
              <option value="day">По дням</option>
              <option value="week">По неделям</option>
              <option value="month">По месяцам</option>
              <option value="year">По годам</option>
            </select>
          </div>
        </div>
        <div class="chart-filters">
          <div class="filter-group">
            <label for="revenueYear">Год:</label>
            <select 
              id="revenueYear"
              v-model="revenueDateFilter.year" 
              @change="loadRevenueChart" 
              class="form-control"
            >
              <option :value="null">Все годы</option>
              <option v-for="year in availableYears" :key="year" :value="year">
                {{ year }}
              </option>
            </select>
          </div>
          <div class="filter-group">
            <label for="revenueStartDate">С:</label>
            <input 
              id="revenueStartDate"
              v-model="revenueDateFilter.startDate" 
              type="date" 
              class="form-control"
              @change="loadRevenueChart"
            />
          </div>
          <div class="filter-group">
            <label for="revenueEndDate">По:</label>
            <input 
              id="revenueEndDate"
              v-model="revenueDateFilter.endDate" 
              type="date" 
              class="form-control"
              @change="loadRevenueChart"
            />
          </div>
          <button 
            @click="resetRevenueFilters" 
            class="btn btn-secondary btn-sm"
            style="margin-top: 1.5rem;"
          >
            Сбросить фильтры
          </button>
        </div>
        <div v-if="revenueChartLoading" class="loading">Загрузка графика...</div>
        <div v-else class="chart-container">
          <canvas ref="revenueChartCanvas"></canvas>
        </div>
      </div>

      <!-- Выручка по категориям -->
      <div class="section chart-section">
        <h3>Выручка по категориям</h3>
        <div v-if="categoryRevenueLoading" class="loading">Загрузка...</div>
        <div v-else-if="categoryRevenue.length > 0" class="chart-container">
          <canvas ref="categoryChartCanvas"></canvas>
        </div>
        <div v-else class="empty">Нет данных</div>
      </div>

      <!-- Топ блюд -->
      <div class="section chart-section">
        <h3>Топ-10 блюд</h3>
        <div v-if="topDishesLoading" class="loading">Загрузка...</div>
        <div v-else-if="topDishes.length > 0" class="chart-container">
          <canvas ref="topDishesChartCanvas"></canvas>
        </div>
        <div v-else class="empty">Нет данных</div>
      </div>

    </div>
  </div>
</template>

<script setup>
import { ref, onMounted, onBeforeUnmount, nextTick } from 'vue'
import { statisticsApi } from '@/api/statistics'
import {
  Chart,
  CategoryScale,
  LinearScale,
  PointElement,
  LineElement,
  BarElement,
  ArcElement,
  Title,
  Tooltip,
  Legend,
  BarController,
  DoughnutController,
  LineController
} from 'chart.js'

// Регистрация компонентов Chart.js
Chart.register(
  CategoryScale,
  LinearScale,
  PointElement,
  LineElement,
  BarElement,
  ArcElement,
  Title,
  Tooltip,
  Legend,
  BarController,
  DoughnutController,
  LineController
)

console.log('Chart.js загружен:', Chart !== undefined)

const loading = ref(false)
const error = ref('')
const overview = ref({
  totalRevenue: 0,
  totalOrders: 0,
  averageOrderValue: 0,
  totalUsers: 0,
  totalDishes: 0,
  statusStatistics: []
})

const monthlyStats = ref({
  revenue: 0,
  revenueChange: null,
  orders: 0,
  ordersChange: null
})

const todayStats = ref({
  orders: 0,
  ordersChange: null
})

const usersStats = ref({
  change: null
})

const topDishes = ref([])
const topDishesLoading = ref(false)
const categoryRevenue = ref([])
const categoryRevenueLoading = ref(false)

// Графики
const revenueChartCanvas = ref(null)
const categoryChartCanvas = ref(null)
const topDishesChartCanvas = ref(null)
const revenueChartLoading = ref(false)
const revenueData = ref([])
const revenuePeriod = ref('day')
const revenueDateFilter = ref({
  startDate: null,
  endDate: null,
  year: null
})

let revenueChart = null
let categoryChart = null
let topDishesChart = null

const formatCurrency = (value) => {
  return new Intl.NumberFormat('ru-RU', {
    style: 'currency',
    currency: 'RUB'
  }).format(value)
}

const formatDate = (dateString) => {
  const date = new Date(dateString)
  return new Intl.DateTimeFormat('ru-RU', {
    year: 'numeric',
    month: 'short',
    day: 'numeric',
    hour: '2-digit',
    minute: '2-digit'
  }).format(date)
}

const getStatusText = (status) => {
  const statusMap = {
    'Pending': 'Ожидает',
    'Processing': 'Готовится',
    'Completed': 'Завершен',
    'Cancelled': 'Отменен'
  }
  return statusMap[status] || status
}

const getStatusClass = (status) => {
  return `status-${status.toLowerCase()}`
}

// Форматирование сравнения
const formatComparison = (change) => {
  if (change === null || change === undefined) return ''
  const sign = change > 0 ? '+' : ''
  return `${sign}${change.toFixed(1)}%`
}

// Класс для сравнения (положительное/отрицательное)
const getComparisonClass = (change) => {
  if (change === null || change === undefined) return ''
  return change >= 0 ? 'comparison-positive' : 'comparison-negative'
}

// Получение дат для текущего месяца (UTC)
const getCurrentMonthDates = () => {
  const now = new Date()
  // Используем UTC время
  const year = now.getUTCFullYear()
  const month = now.getUTCMonth()
  
  // Начало месяца в UTC (1 число, 00:00:00)
  const startOfMonth = new Date(Date.UTC(year, month, 1, 0, 0, 0, 0))
  // Конец месяца в UTC (последний день, 23:59:59.999)
  const endOfMonth = new Date(Date.UTC(year, month + 1, 0, 23, 59, 59, 999))
  
  return {
    start: startOfMonth.toISOString(),
    end: endOfMonth.toISOString()
  }
}

// Получение дат для предыдущего месяца (UTC)
const getPreviousMonthDates = () => {
  const now = new Date()
  // Используем UTC время
  const year = now.getUTCFullYear()
  const month = now.getUTCMonth()
  
  // Начало предыдущего месяца в UTC (1 число, 00:00:00)
  const startOfPrevMonth = new Date(Date.UTC(year, month - 1, 1, 0, 0, 0, 0))
  // Конец предыдущего месяца в UTC (последний день, 23:59:59.999)
  const endOfPrevMonth = new Date(Date.UTC(year, month, 0, 23, 59, 59, 999))
  
  return {
    start: startOfPrevMonth.toISOString(),
    end: endOfPrevMonth.toISOString()
  }
}

// Получение дат для сегодня (UTC)
const getTodayDates = () => {
  const today = new Date()
  // Используем UTC время
  const year = today.getUTCFullYear()
  const month = today.getUTCMonth()
  const day = today.getUTCDate()
  
  // Начало дня в UTC (00:00:00)
  const startOfDay = new Date(Date.UTC(year, month, day, 0, 0, 0, 0))
  // Конец дня в UTC (23:59:59.999)
  const endOfDay = new Date(Date.UTC(year, month, day, 23, 59, 59, 999))
  
  return {
    start: startOfDay.toISOString(),
    end: endOfDay.toISOString()
  }
}

// Получение дат для предыдущего дня (вчера) (UTC)
const getYesterdayDates = () => {
  const today = new Date()
  // Используем UTC время
  const yesterday = new Date(today)
  yesterday.setUTCDate(yesterday.getUTCDate() - 1)
  
  const year = yesterday.getUTCFullYear()
  const month = yesterday.getUTCMonth()
  const day = yesterday.getUTCDate()
  
  // Начало дня в UTC (00:00:00)
  const startOfDay = new Date(Date.UTC(year, month, day, 0, 0, 0, 0))
  // Конец дня в UTC (23:59:59.999)
  const endOfDay = new Date(Date.UTC(year, month, day, 23, 59, 59, 999))
  
  return {
    start: startOfDay.toISOString(),
    end: endOfDay.toISOString()
  }
}

// Расчет процента изменения
const calculateChange = (current, previous) => {
  if (!previous || previous === 0) return null
  return ((current - previous) / previous) * 100
}

// Загрузка месячной статистики
const loadMonthlyStatistics = async () => {
  try {
    const currentMonth = getCurrentMonthDates()
    const previousMonth = getPreviousMonthDates()
    
    // Загружаем данные для текущего месяца
    const [currentMonthRevenue, currentMonthOverview] = await Promise.all([
      statisticsApi.getRevenueByDate(currentMonth.start, currentMonth.end, 'day').catch(() => []),
      statisticsApi.getOverview(currentMonth.start, currentMonth.end).catch(() => ({ totalOrders: 0 }))
    ])
    
    // Загружаем данные для предыдущего месяца
    const [previousMonthRevenue, previousMonthOverview] = await Promise.all([
      statisticsApi.getRevenueByDate(previousMonth.start, previousMonth.end, 'day').catch(() => []),
      statisticsApi.getOverview(previousMonth.start, previousMonth.end).catch(() => ({ totalOrders: 0 }))
    ])
    
    // Считаем выручку за текущий месяц
    const currentRevenue = Array.isArray(currentMonthRevenue) 
      ? currentMonthRevenue.reduce((sum, item) => sum + (item.revenue || 0), 0)
      : 0
    
    // Считаем выручку за предыдущий месяц
    const previousRevenue = Array.isArray(previousMonthRevenue)
      ? previousMonthRevenue.reduce((sum, item) => sum + (item.revenue || 0), 0)
      : 0
    
    // Заказы за текущий месяц
    const currentOrders = currentMonthOverview?.totalOrders || 0
    const previousOrders = previousMonthOverview?.totalOrders || 0
    
    monthlyStats.value = {
      revenue: currentRevenue,
      revenueChange: calculateChange(currentRevenue, previousRevenue),
      orders: currentOrders,
      ordersChange: calculateChange(currentOrders, previousOrders)
    }
  } catch (err) {
    console.error('Ошибка загрузки месячной статистики:', err)
    monthlyStats.value = {
      revenue: 0,
      revenueChange: null,
      orders: 0,
      ordersChange: null
    }
  }
}

// Загрузка статистики за сегодня
const loadTodayStatistics = async () => {
  try {
    const today = getTodayDates()
    const yesterday = getYesterdayDates()
    
    // Загружаем данные за сегодня и вчера
    const [todayOverview, yesterdayOverview] = await Promise.all([
      statisticsApi.getOverview(today.start, today.end).catch(() => ({ totalOrders: 0 })),
      statisticsApi.getOverview(yesterday.start, yesterday.end).catch(() => ({ totalOrders: 0 }))
    ])
    
    const todayOrders = todayOverview?.totalOrders || 0
    const yesterdayOrders = yesterdayOverview?.totalOrders || 0
    
    todayStats.value = {
      orders: todayOrders,
      ordersChange: calculateChange(todayOrders, yesterdayOrders)
    }
  } catch (err) {
    console.error('Ошибка загрузки статистики за сегодня:', err)
    todayStats.value = {
      orders: 0,
      ordersChange: null
    }
  }
}

const loadStatistics = async () => {
  loading.value = true
  error.value = ''
  
  try {
    const previousMonth = getPreviousMonthDates()
    
    const [currentOverview, previousMonthOverview] = await Promise.all([
      statisticsApi.getOverview(),
      statisticsApi.getOverview(previousMonth.start, previousMonth.end).catch(() => ({ totalUsers: 0 }))
    ])
    
    overview.value = currentOverview
    
    // Сравнение пользователей с предыдущим месяцем
    const currentUsers = currentOverview?.totalUsers || 0
    const previousUsers = previousMonthOverview?.totalUsers || 0
    usersStats.value = {
      change: calculateChange(currentUsers, previousUsers)
    }
  } catch (err) {
    const errorMessage = err.response?.data?.message || err.message || 'Ошибка загрузки статистики'
    error.value = errorMessage
    console.error('Ошибка загрузки статистики:', {
      message: errorMessage,
      status: err.response?.status,
      data: err.response?.data,
      error: err
    })
    usersStats.value = {
      change: null
    }
  } finally {
    loading.value = false
  }
}


// Получение доступных годов из всех данных (независимо от фильтрации)
const availableYears = ref([])

// Загрузка всех доступных годов при инициализации
const loadAvailableYears = async () => {
  try {
    // Загружаем данные без фильтров для получения всех годов (UTC)
    const now = new Date()
    const defaultStart = new Date(Date.UTC(now.getUTCFullYear() - 10, 0, 1, 0, 0, 0, 0)) // 10 лет назад
    const defaultEnd = new Date(Date.UTC(now.getUTCFullYear() + 10, 11, 31, 23, 59, 59, 999)) // 10 лет вперед
    
    const data = await statisticsApi.getRevenueByDate(
      defaultStart.toISOString(), 
      defaultEnd.toISOString(), 
      'day'
    )
    
    const yearsSet = new Set()
    if (Array.isArray(data)) {
      data.forEach(item => {
        try {
          if (item.date) {
            const date = new Date(item.date + 'T00:00:00Z')
            if (!isNaN(date.getTime())) {
              yearsSet.add(date.getUTCFullYear())
            } else if (item.date.includes('-')) {
              // Для формата "YYYY-MM" или "YYYY"
              const year = parseInt(item.date.split('-')[0])
              if (!isNaN(year)) {
                yearsSet.add(year)
              }
            } else {
              // Для формата "YYYY" (год)
              const year = parseInt(item.date)
              if (!isNaN(year) && year > 1900 && year < 2100) {
                yearsSet.add(year)
              }
            }
          }
        } catch (e) {
          console.warn('Ошибка парсинга даты:', item.date)
        }
      })
    }
    availableYears.value = Array.from(yearsSet).sort((a, b) => b - a)
  } catch (err) {
    console.error('Ошибка загрузки доступных годов:', err)
  }
}

const loadRevenueChart = async () => {
  revenueChartLoading.value = true
  try {
    let startDateStr = null
    let endDateStr = null
    
    // Если указан год, используем его для фильтрации (UTC)
    if (revenueDateFilter.value.year) {
      const year = revenueDateFilter.value.year
      const yearStart = new Date(Date.UTC(year, 0, 1, 0, 0, 0, 0))
      const yearEnd = new Date(Date.UTC(year, 11, 31, 23, 59, 59, 999))
      
      if (revenueDateFilter.value.startDate) {
        const customStart = new Date(revenueDateFilter.value.startDate + 'T00:00:00Z')
        startDateStr = customStart > yearStart ? customStart.toISOString() : yearStart.toISOString()
      } else {
        startDateStr = yearStart.toISOString()
      }
      
      if (revenueDateFilter.value.endDate) {
        const customEnd = new Date(revenueDateFilter.value.endDate + 'T23:59:59Z')
        endDateStr = customEnd < yearEnd ? customEnd.toISOString() : yearEnd.toISOString()
      } else {
        endDateStr = yearEnd.toISOString()
      }
    } else {
      if (revenueDateFilter.value.startDate) {
        const startDate = new Date(revenueDateFilter.value.startDate + 'T00:00:00Z')
        startDateStr = startDate.toISOString()
      } else {
        const now = new Date()
        const defaultStart = new Date(Date.UTC(now.getUTCFullYear() - 2, 0, 1, 0, 0, 0, 0))
        startDateStr = defaultStart.toISOString()
      }
      
      if (revenueDateFilter.value.endDate) {
        const endDate = new Date(revenueDateFilter.value.endDate + 'T23:59:59Z')
        endDateStr = endDate.toISOString()
      } else {
        const now = new Date()
        const defaultEnd = new Date(Date.UTC(now.getUTCFullYear() + 1, 11, 31, 23, 59, 59, 999))
        endDateStr = defaultEnd.toISOString()
      }
    }
    
    console.log('Загрузка графика выручки:', { startDateStr, endDateStr, period: revenuePeriod.value })
    const data = await statisticsApi.getRevenueByDate(startDateStr, endDateStr, revenuePeriod.value)
    console.log('Данные получены от API:', data)
    console.log('Тип данных:', typeof data, 'Является массивом:', Array.isArray(data))
    
    if (!data) {
      console.warn('API вернул null или undefined')
      revenueData.value = []
    } else if (!Array.isArray(data)) {
      console.warn('API вернул не массив:', typeof data, data)
      revenueData.value = []
    } else {
      // Проверяем структуру данных
      if (data.length > 0) {
        console.log('Пример первого элемента данных:', data[0])
        console.log('Поля первого элемента:', Object.keys(data[0]))
      }
      
      revenueData.value = data.map(item => {
        // Нормализуем данные - проверяем оба варианта имен полей
        const normalizedItem = {
          date: item.date || item.Date,
          revenue: item.revenue !== undefined ? item.revenue : (item.Revenue !== undefined ? item.Revenue : 0),
          ordersCount: item.ordersCount || item.OrdersCount || 0
        }
        return normalizedItem
      })
      
      // Сортируем данные по дате
      revenueData.value.sort((a, b) => {
        try {
          let dateA, dateB
          
          if (revenuePeriod.value === 'month') {
            const [yearA, monthA] = (a.date || '').split('-')
            const [yearB, monthB] = (b.date || '').split('-')
            if (!yearA || !yearB) return 0
            dateA = new Date(Date.UTC(parseInt(yearA), parseInt(monthA) - 1, 1, 0, 0, 0, 0))
            dateB = new Date(Date.UTC(parseInt(yearB), parseInt(monthB) - 1, 1, 0, 0, 0, 0))
          } else if (revenuePeriod.value === 'year') {
            const yearA = parseInt(a.date)
            const yearB = parseInt(b.date)
            if (isNaN(yearA) || isNaN(yearB)) return 0
            dateA = new Date(Date.UTC(yearA, 0, 1, 0, 0, 0, 0))
            dateB = new Date(Date.UTC(yearB, 0, 1, 0, 0, 0, 0))
          } else if (revenuePeriod.value === 'week') {
            dateA = new Date((a.date || '') + 'T00:00:00Z')
            dateB = new Date((b.date || '') + 'T00:00:00Z')
          } else {
            dateA = new Date((a.date || '') + 'T00:00:00Z')
            dateB = new Date((b.date || '') + 'T00:00:00Z')
          }
          
          if (isNaN(dateA.getTime()) || isNaN(dateB.getTime())) {
            console.warn('Некорректная дата при сортировке:', a.date, b.date)
            return 0
          }
          
          return dateA - dateB
        } catch (e) {
          console.warn('Ошибка сортировки дат:', e, a, b)
          return 0
        }
      })
    }
    
    console.log('Revenue data loaded:', revenueData.value.length, 'items')
    console.log('Первые 3 элемента после обработки:', revenueData.value.slice(0, 3))
    if (revenueData.value.length === 0) {
      console.warn('Нет данных для графика выручки')
    }
    
    await nextTick()
    setTimeout(() => {
      createRevenueChart()
    }, 100)
  } catch (err) {
    console.error('Ошибка загрузки графика выручки:', {
      message: err.response?.data?.message || err.message,
      status: err.response?.status,
      statusText: err.response?.statusText,
      data: err.response?.data,
      fullError: err
    })
    revenueData.value = []
  } finally {
    revenueChartLoading.value = false
  }
}

const resetRevenueFilters = () => {
  revenueDateFilter.value = {
    startDate: null,
    endDate: null,
    year: null
  }
  loadRevenueChart()
}

const createRevenueChart = () => {
  if (!Chart) {
    console.error('Chart.js не загружен')
    return
  }
  
  if (!revenueChartCanvas.value) {
    console.warn('Revenue chart canvas not available')
    return
  }
  
  if (!revenueData.value || revenueData.value.length === 0) {
    console.warn('No revenue data available', revenueData.value)
    return
  }
  
  if (revenueChart) {
    revenueChart.destroy()
    revenueChart = null
  }
  
  try {
    const ctx = revenueChartCanvas.value.getContext('2d')
    if (!ctx) {
      console.error('Не удалось получить контекст canvas')
      return
    }
    
    console.log('Creating revenue chart with', revenueData.value.length, 'data points')
    
    const labels = revenueData.value.map((item, index) => {
      try {
        if (!item.date) {
          console.warn(`Элемент ${index} не имеет поля date:`, item)
          return 'Без даты'
        }
        
        if (revenuePeriod.value === 'month') {
          const parts = item.date.split('-')
          if (parts.length < 2) {
            console.warn('Некорректный формат даты для месяца:', item.date)
            return item.date
          }
          const [year, month] = parts
          const date = new Date(Date.UTC(parseInt(year), parseInt(month) - 1, 1, 0, 0, 0, 0))
          if (isNaN(date.getTime())) {
            console.warn('Некорректная дата:', item.date)
            return item.date
          }
          return date.toLocaleDateString('ru-RU', { month: 'long', year: 'numeric' })
        } else if (revenuePeriod.value === 'year') {
          const year = parseInt(item.date)
          if (isNaN(year)) {
            console.warn('Некорректный год:', item.date)
            return item.date
          }
          return year.toString()
        } else if (revenuePeriod.value === 'week') {
          const date = new Date(item.date + 'T00:00:00Z')
          if (isNaN(date.getTime())) {
            console.warn('Некорректная дата для недели:', item.date)
            return item.date
          }
          const weekEnd = new Date(date)
          weekEnd.setUTCDate(weekEnd.getUTCDate() + 6)
          return date.toLocaleDateString('ru-RU', { day: 'numeric', month: 'short' }) + 
                 ' - ' + 
                 weekEnd.toLocaleDateString('ru-RU', { day: 'numeric', month: 'short' })
        } else {
          // Для дня
          const date = new Date(item.date + 'T00:00:00Z')
          if (isNaN(date.getTime())) {
            console.warn('Некорректная дата для дня:', item.date)
            return item.date
          }
          return date.toLocaleDateString('ru-RU')
        }
      } catch (error) {
        console.warn(`Ошибка форматирования даты для элемента ${index}:`, item.date, error)
        return item.date || 'Ошибка'
      }
    })
    
    const dataValues = revenueData.value.map((item, index) => {
      // Данные уже нормализованы в loadRevenueChart
      const revenue = item.revenue
      const value = typeof revenue === 'number' ? revenue : parseFloat(revenue) || 0
      if (isNaN(value)) {
        console.warn(`Некорректное значение revenue для элемента ${index}:`, item, revenue)
        return 0
      }
      return value
    })
    
    console.log('Chart data prepared:', { 
      labels: labels.length, 
      data: dataValues.length,
      sampleData: revenueData.value.slice(0, 3),
      sampleValues: dataValues.slice(0, 3),
      period: revenuePeriod.value
    })
    
    // Проверяем, что есть данные для отображения
    if (dataValues.every(v => v === 0)) {
      console.warn('Все значения revenue равны 0, возможно проблема с данными')
    }
    
    // Проверяем, что labels и dataValues имеют одинаковую длину
    if (labels.length !== dataValues.length) {
      console.error('Несоответствие длины labels и dataValues:', labels.length, dataValues.length)
      console.error('Labels:', labels)
      console.error('DataValues:', dataValues)
      return
    }
    
    // Проверяем, что есть хотя бы один ненулевой элемент
    if (dataValues.length === 0) {
      console.warn('Нет данных для отображения графика')
      return
    }
    
    // Проверяем, что все значения валидны
    const invalidValues = dataValues.filter(v => !isFinite(v))
    if (invalidValues.length > 0) {
      console.error('Найдены некорректные значения:', invalidValues)
      return
    }
    
    // Проверяем, что все labels валидны
    const invalidLabels = labels.filter(l => !l || l === 'undefined' || l === 'null')
    if (invalidLabels.length > 0) {
      console.warn('Найдены некорректные labels:', invalidLabels)
    }
    
    console.log('Создание графика Chart.js...')
    
    try {
      revenueChart = new Chart(ctx, {
        type: 'line',
        data: {
          labels: labels,
          datasets: [{
            label: 'Выручка (₽)',
            data: dataValues,
            borderColor: '#2F5D8C',
            backgroundColor: 'rgba(47, 93, 140, 0.1)',
            tension: 0.4,
            fill: true
          }]
        },
        options: {
          responsive: true,
          maintainAspectRatio: false,
          plugins: {
            legend: {
              display: true,
              position: 'top'
            },
            tooltip: {
              callbacks: {
                label: function(context) {
                  return 'Выручка: ' + formatCurrency(context.parsed.y)
                }
              }
            }
          },
          scales: {
            y: {
              beginAtZero: true,
              ticks: {
                callback: function(value) {
                  return formatCurrency(value)
                }
              }
            }
          }
        }
      })
      console.log('График успешно создан!')
    } catch (chartError) {
      console.error('Ошибка при создании графика Chart.js:', chartError)
      throw chartError
    }
  } catch (error) {
    console.error('Ошибка создания графика выручки:', error)
    console.error('Стек ошибки:', error.stack)
  }
}

// Цвета для категорий (те же, что в круговой диаграмме)
const categoryColors = [
  '#2F5D8C',
  '#E3B23C',
  '#6B6B6B',
  '#254A70',
  '#C99A2E',
  '#8B8B8B',
  '#1A3A52',
  '#B88A1F'
]

// Функция для получения цвета категории
const getCategoryColor = (categoryName, categoryId) => {
  // Создаем маппинг категорий к цветам на основе данных из круговой диаграммы
  const categoryIndex = categoryRevenue.value.findIndex(cat => 
    cat.categoryName === categoryName || cat.categoryId === categoryId
  )
  
  if (categoryIndex >= 0) {
    return categoryColors[categoryIndex % categoryColors.length]
  }
  
  // Если категория не найдена в revenue, используем хеш для стабильного цвета
  let hash = 0
  const str = categoryName || categoryId?.toString() || ''
  for (let i = 0; i < str.length; i++) {
    hash = str.charCodeAt(i) + ((hash << 5) - hash)
  }
  return categoryColors[Math.abs(hash) % categoryColors.length]
}

const createCategoryChart = () => {
  if (!Chart) {
    console.error('Chart.js не загружен')
    return
  }
  
  if (!categoryChartCanvas.value) {
    console.warn('Category chart canvas not available')
    return
  }
  
  if (categoryRevenue.value.length === 0) {
    console.warn('No category revenue data available')
    return
  }
  
  if (categoryChart) {
    categoryChart.destroy()
  }
  
  try {
    const ctx = categoryChartCanvas.value.getContext('2d')
    console.log('Creating category chart with', categoryRevenue.value.length, 'categories')
    categoryChart = new Chart(ctx, {
    type: 'doughnut',
    data: {
      labels: categoryRevenue.value.map(cat => cat.categoryName),
      datasets: [{
        data: categoryRevenue.value.map(cat => cat.totalRevenue),
        backgroundColor: categoryRevenue.value.map((cat, index) => 
          categoryColors[index % categoryColors.length]
        )
      }]
    },
    options: {
      responsive: true,
      maintainAspectRatio: false,
      plugins: {
        legend: {
          display: true,
          position: 'right'
        },
        tooltip: {
          callbacks: {
            label: function(context) {
              const label = context.label || ''
              const value = formatCurrency(context.parsed)
              const total = categoryRevenue.value.reduce((sum, cat) => sum + cat.totalRevenue, 0)
              const percentage = ((context.parsed / total) * 100).toFixed(1)
              return `${label}: ${value} (${percentage}%)`
            }
          }
        }
      }
    }
  })
  } catch (error) {
    console.error('Ошибка создания графика категорий:', error)
  }
}

const createTopDishesChart = () => {
  if (!Chart) {
    console.error('Chart.js не загружен')
    return
  }
  
  if (!topDishesChartCanvas.value) {
    console.warn('Top dishes chart canvas not available')
    return
  }
  
  if (topDishes.value.length === 0) {
    console.warn('No top dishes data available')
    return
  }
  
  if (topDishesChart) {
    topDishesChart.destroy()
  }
  
  try {
    const ctx = topDishesChartCanvas.value.getContext('2d')
    console.log('Creating top dishes chart with', topDishes.value.length, 'dishes')
    
    // Получаем цвета для каждого блюда на основе его категории
    const dishColors = topDishes.value.map(dish => 
      getCategoryColor(dish.categoryName, dish.categoryId)
    )
    
    topDishesChart = new Chart(ctx, {
    type: 'bar',  
    data: {
      labels: topDishes.value.map(dish => dish.dishName),
      datasets: [{
        label: 'Выручка (₽)',
        data: topDishes.value.map(dish => dish.totalRevenue),
        backgroundColor: dishColors,
        borderRadius: 4
      }]
    },
    options: {
      responsive: true,
      maintainAspectRatio: false,
      indexAxis: 'y',
      plugins: {
        legend: {
          display: false
        },
        tooltip: {
          callbacks: {
            label: function(context) {
              const dish = topDishes.value[context.dataIndex]
              return [
                'Выручка: ' + formatCurrency(context.parsed.x),
                'Заказано: ' + dish.totalQuantity + ' раз'
              ]
            }
          }
        }
      },
      scales: {
        x: {
          beginAtZero: true,
          ticks: {
            callback: function(value) {
              return formatCurrency(value)
            }
          }
        }
      }
    }
  })
  } catch (error) {
    console.error('Ошибка создания графика топ блюд:', error)
  }
}

const loadTopDishes = async () => {
  topDishesLoading.value = true
  try {
    const data = await statisticsApi.getTopDishes(10)
    topDishes.value = Array.isArray(data) ? data : []
    console.log('Top dishes data loaded:', topDishes.value.length, 'dishes')
    await nextTick()
    // Добавляем небольшую задержку для гарантии, что canvas готов
    setTimeout(() => {
      createTopDishesChart()
    }, 100)
  } catch (err) {
    console.error('Ошибка загрузки топ блюд:', {
      message: err.response?.data?.message || err.message,
      status: err.response?.status,
      data: err.response?.data,
      error: err
    })
  } finally {
    topDishesLoading.value = false
  }
}

const loadCategoryRevenue = async () => {
  categoryRevenueLoading.value = true
  try {
    const data = await statisticsApi.getRevenueByCategory()
    categoryRevenue.value = Array.isArray(data) ? data : []
    console.log('Category revenue data loaded:', categoryRevenue.value.length, 'categories')
    await nextTick()
    // Добавляем небольшую задержку для гарантии, что canvas готов
    setTimeout(() => {
      createCategoryChart()
    }, 100)
  } catch (err) {
    console.error('Ошибка загрузки выручки по категориям:', {
      message: err.response?.data?.message || err.message,
      status: err.response?.status,
      data: err.response?.data,
      error: err
    })
  } finally {
    categoryRevenueLoading.value = false
  }
}

onMounted(async () => {
  await loadStatistics()
  await loadMonthlyStatistics()
  await loadTodayStatistics()
  await loadTopDishes()
  await loadCategoryRevenue()
  // Загружаем доступные годы перед загрузкой графика
  await loadAvailableYears()
  await loadRevenueChart()
})

onBeforeUnmount(() => {
  if (revenueChart) {
    revenueChart.destroy()
    revenueChart = null
  }
  if (categoryChart) {
    categoryChart.destroy()
    categoryChart = null
  }
  if (topDishesChart) {
    topDishesChart.destroy()
    topDishesChart = null
  }
})
</script>

<style scoped>
.statistics-page {
  padding: 2rem 0;
}

.stats-cards-row {
  display: grid;
  grid-template-columns: repeat(3, 1fr);
  gap: 1.5rem;
  margin-bottom: 1.5rem;
}

.stats-cards-row-bottom {
  grid-template-columns: repeat(4, 1fr);
}

@media (max-width: 1200px) {
  .stats-cards-row {
    grid-template-columns: repeat(2, 1fr);
  }
  
  .stats-cards-row-bottom {
    grid-template-columns: repeat(2, 1fr);
  }
}

@media (max-width: 768px) {
  .stats-cards-row,
  .stats-cards-row-bottom {
    grid-template-columns: 1fr;
  }
}

.stat-card {
  background: white;
  padding: 1.5rem;
  border-radius: 8px;
  box-shadow: 0 2px 4px rgba(0, 0, 0, 0.1);
  border: 1px solid #E0E0DC;
  overflow: hidden;
  word-wrap: break-word;
  overflow-wrap: break-word;
}

.stat-card h3 {
  color: #6B6B6B;
  font-size: 0.9rem;
  margin-bottom: 0.5rem;
  text-transform: uppercase;
  font-weight: 600;
  word-wrap: break-word;
  overflow-wrap: break-word;
}

.stat-value {
  font-size: 2rem;
  font-weight: bold;
  color: #2F5D8C;
  margin: 0.5rem 0;
  word-wrap: break-word;
  overflow-wrap: break-word;
  hyphens: auto;
}

.stat-comparison {
  font-size: 0.85rem;
  margin-top: 0.5rem;
  font-weight: 500;
}

.comparison-positive {
  color: #2F5D8C;
}

.comparison-negative {
  color: #B33A2B;
}

.section {
  background: white;
  padding: 1.5rem;
  border-radius: 8px;
  box-shadow: 0 2px 4px rgba(0, 0, 0, 0.1);
  border: 1px solid #E0E0DC;
  margin-bottom: 2rem;
}

.chart-section {
  min-height: 400px;
}

.chart-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 1rem;
}

.chart-controls {
  display: flex;
  gap: 0.5rem;
}

.chart-controls .form-control {
  padding: 0.5rem;
  border: 1px solid #E0E0DC;
  border-radius: 4px;
  font-size: 0.9rem;
}

.chart-filters {
  display: flex;
  gap: 1rem;
  align-items: flex-end;
  margin-bottom: 1rem;
  flex-wrap: wrap;
  padding: 1rem;
  background: #F9F9F9;
  border-radius: 4px;
}

.filter-group {
  display: flex;
  flex-direction: column;
  gap: 0.25rem;
}

.filter-group label {
  font-size: 0.9rem;
  color: #6B6B6B;
  font-weight: 500;
}

.filter-group .form-control {
  min-width: 120px;
  padding: 0.5rem;
  border: 1px solid #E0E0DC;
  border-radius: 4px;
  font-size: 0.9rem;
}

.btn-sm {
  padding: 0.5rem 1rem;
  font-size: 0.9rem;
}

.chart-container {
  position: relative;
  height: 400px;
  width: 100%;
}

.section h3 {
  color: #2F5D8C;
  margin-bottom: 1rem;
  font-size: 1.3rem;
}

.top-dishes, .category-revenue, .recent-orders {
  display: flex;
  flex-direction: column;
  gap: 0.75rem;
}

.top-dish-item, .category-item, .order-item {
  padding: 1rem;
  background: #F9F9F9;
  border-radius: 4px;
  border-left: 3px solid #2F5D8C;
}

.top-dish-item {
  display: flex;
  align-items: center;
  gap: 1rem;
}

.dish-rank {
  font-weight: bold;
  color: #E3B23C;
  font-size: 1.2rem;
}

.dish-name {
  flex: 1;
  font-weight: 600;
  color: #2B2B2B;
}

.dish-stats {
  display: flex;
  flex-direction: column;
  gap: 0.25rem;
  font-size: 0.9rem;
  color: #6B6B6B;
}

.category-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 0.5rem;
}

.category-name {
  font-weight: 600;
  color: #2B2B2B;
}

.category-total {
  font-weight: bold;
  color: #2F5D8C;
  font-size: 1.1rem;
}

.category-details {
  font-size: 0.9rem;
  color: #6B6B6B;
}

.order-item {
  display: flex;
  justify-content: space-between;
  align-items: center;
}

.order-info {
  display: flex;
  flex-direction: column;
  gap: 0.25rem;
}

.order-id {
  font-weight: 600;
  color: #2B2B2B;
}

.order-user, .order-date {
  font-size: 0.9rem;
  color: #6B6B6B;
}

.order-meta {
  display: flex;
  flex-direction: column;
  align-items: flex-end;
  gap: 0.5rem;
}

.order-status {
  padding: 0.25rem 0.75rem;
  border-radius: 4px;
  font-size: 0.85rem;
  font-weight: 600;
}

.status-pending {
  background: #FFF4E6;
  color: #E3B23C;
}

.status-processing {
  background: #E6F2FF;
  color: #2F5D8C;
}

.status-completed {
  background: #E6F7E6;
  color: #2F5D8C;
}

.status-cancelled {
  background: #FFE0E0;
  color: #B33A2B;
}

.order-total {
  font-weight: bold;
  color: #2F5D8C;
  font-size: 1.1rem;
}

.empty {
  text-align: center;
  padding: 2rem;
  color: #6B6B6B;
}

.error-message {
  background: #FFF5F5;
  border: 1px solid #FFE0E0;
  border-radius: 8px;
  padding: 1.5rem;
  margin-bottom: 2rem;
  color: #B33A2B;
}

.error-message p {
  margin: 0.5rem 0;
}

.error-hint {
  font-size: 0.9rem;
  color: #6B6B6B;
  margin-top: 0.5rem;
  font-style: italic;
}
</style>

