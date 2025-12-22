<template>
  <div class="menu-page">
    <div class="menu-layout">
      <!-- Левая часть - меню -->
      <div class="menu-content">
        <div class="container">
          <h1>Меню</h1>
          
          <div v-if="loading" class="loading">
            <p>Загрузка меню...</p>
          </div>
          
          <div v-else-if="error" class="error">
            <p>{{ error }}</p>
          </div>
          
          <div v-else>
            <!-- Переключатель меню -->
            <MenuSelector
              :menus="menus"
              :selected-menu-id="selectedMenuId"
              @select="selectMenu"
            />
            
            <!-- Блюда по категориям -->
            <div v-if="groupedDishes.length > 0" class="dishes-content">
              <CategorySection
                v-for="category in groupedDishes"
                :key="category.id"
                :category="category"
              >
                <DishCard
                  v-for="dish in category.dishes"
                  :key="dish.id"
                  :dish="dish"
                  @click="openDishModal(dish)"
                  @add="increaseQuantity(dish)"
                  @increase="increaseQuantity(dish)"
                  @decrease="decreaseQuantity(dish)"
                />
              </CategorySection>
            </div>
            
            <div v-else-if="selectedMenuId && !loading" class="empty-menu">
              <p>В этом меню пока нет блюд</p>
              <p class="empty-menu-hint">Попробуйте выбрать другое меню</p>
            </div>
            
            <div v-else-if="!selectedMenuId && menus.length === 0" class="empty-menu">
              <p>Меню не найдены</p>
            </div>
            
            <div v-else-if="!selectedMenuId" class="empty-menu">
              <p>Выберите меню для просмотра блюд</p>
            </div>
            
            <!-- Модальное окно блюда -->
            <DishModal
              :is-open="isModalOpen"
              :dish="selectedDish"
              @close="closeDishModal"
              @require-auth="handleRequireAuth"
            />
          </div>
        </div>
      </div>
      
      <!-- Правая часть - корзина -->
      <CartDrawer />
    </div>
    
    <!-- Модальное окно авторизации -->
    <AuthModal 
      v-if="showAuthModal" 
      :isLogin="true" 
      @close="closeAuthModal" 
    />
  </div>
</template>

<script setup>
import { ref, computed, onMounted, watch } from 'vue'
import { useRouter } from 'vue-router'
import { menusApi } from '@/api/menus'
import { menuDishesApi } from '@/api/menuDishes'
import { dishesApi } from '@/api/dishes'
import { categoriesApi } from '@/api/categories'
import { useCartStore } from '@/stores/cart'
import { useAuthStore } from '@/stores/auth'
import DishModal from '@/components/menu/DishModal.vue'
import MenuSelector from '@/components/menu/MenuSelector.vue'
import CategorySection from '@/components/menu/CategorySection.vue'
import DishCard from '@/components/menu/DishCard.vue'
import CartDrawer from '@/components/cart/CartDrawer.vue'
import AuthModal from '@/components/auth/AuthModal.vue'

const cartStore = useCartStore()
const authStore = useAuthStore()
const router = useRouter()

const showAuthModal = ref(false)
const pendingDish = ref(null)

const isModalOpen = ref(false)
const selectedDish = ref(null)

const menus = ref([])
const selectedMenuId = ref(null)
const menuDishes = ref([]) // Связи меню-блюдо
const dishes = ref([]) // Все блюда
const categories = ref([])
const loading = ref(false)
const error = ref('')

// Блюда выбранного меню, сгруппированные по категориям
const groupedDishes = computed(() => {
  if (!selectedMenuId.value || menuDishes.value.length === 0) {
    return []
  }
  
  // Получаем ID блюд из выбранного меню
  const dishIdsInMenu = new Set(menuDishes.value.map(md => md.dishId))
  
  // Фильтруем блюда, которые есть в выбранном меню
  const dishesInMenu = dishes.value.filter(dish => dishIdsInMenu.has(dish.id))
  
  // Группируем по категориям
  const grouped = {}
  
  categories.value.forEach(category => {
    grouped[category.name] = {
      id: category.id,
      name: category.name,
      dishes: []
    }
  })
  
  dishesInMenu.forEach(dish => {
    const categoryName = dish.categoryName || 'Без категории'
    
    if (!grouped[categoryName]) {
      grouped[categoryName] = {
        id: categoryName,
        name: categoryName,
        dishes: []
      }
    }
    
    // Вычисляем количество в корзине для этого блюда
    const cartQuantity = cartStore.items
      .filter(item => item.dishId === dish.id)
      .reduce((sum, item) => sum + item.quantity, 0)
    
    grouped[categoryName].dishes.push({
      id: dish.id,
      name: dish.name,
      description: dish.description,
      price: dish.price,
      categoryName: categoryName,
      cartQuantity: cartQuantity
    })
  })
  
  // Фильтруем пустые категории и возвращаем массив
  return Object.values(grouped).filter(cat => cat.dishes.length > 0)
})

const loadMenus = async () => {
  try {
    console.log('Загрузка меню...')
    const menusResponse = await menusApi.getAll({ pageSize: 100 })
    console.log('Ответ API меню:', menusResponse)
    
    let allMenus = Array.isArray(menusResponse) 
      ? menusResponse 
      : (menusResponse?.data || [])
    
    console.log('Обработанные меню:', allMenus)
    
    if (!Array.isArray(allMenus) || allMenus.length === 0) {
      console.warn('Меню не найдены или пустой массив')
      menus.value = []
      error.value = 'Меню не найдены. Обратитесь к администратору.'
      return
    }
    
    // Сортируем меню: "Основное меню" всегда первое
    menus.value = allMenus.sort((a, b) => {
      const aName = a.name?.toLowerCase() || ''
      const bName = b.name?.toLowerCase() || ''
      
      // "Основное меню" всегда первое
      if (aName.includes('основн')) return -1
      if (bName.includes('основн')) return 1
      
      // Остальные по алфавиту
      return aName.localeCompare(bName)
    })
    
    console.log('Отсортированные меню:', menus.value)
    
    // Автоматически выбираем "Основное меню" или первое меню
    if (menus.value.length > 0 && !selectedMenuId.value) {
      const mainMenu = menus.value.find(m => 
        m.name?.toLowerCase().includes('основн')
      )
      const menuToSelect = mainMenu || menus.value[0]
      console.log('Выбираем меню:', menuToSelect)
      if (menuToSelect && menuToSelect.id) {
        selectMenu(menuToSelect.id)
      } else {
        console.error('Меню не имеет ID:', menuToSelect)
        error.value = 'Ошибка: меню не имеет идентификатора'
      }
    }
  } catch (err) {
    console.error('Ошибка загрузки меню:', err)
    console.error('Детали ошибки:', {
      message: err.message,
      response: err.response?.data,
      status: err.response?.status
    })
    error.value = err.response?.data?.message || err.message || 'Ошибка загрузки списка меню'
  }
}

const selectMenu = async (menuId) => {
  if (!menuId) {
    console.error('Попытка выбрать меню без ID')
    return
  }
  
  selectedMenuId.value = menuId
  loading.value = true
  error.value = ''
  
  try {
    console.log('Выбор меню с ID:', menuId)
    
    // Загружаем связи меню-блюдо для выбранного меню
    const menuDishesResponse = await menuDishesApi.getByMenuId(menuId)
    console.log('Ответ API блюд меню:', menuDishesResponse)
    
    menuDishes.value = Array.isArray(menuDishesResponse)
      ? menuDishesResponse
      : (menuDishesResponse?.data || [])
    
    console.log('Загружено блюд в меню:', menuDishes.value.length)
    
    // Если блюда еще не загружены, загружаем их
    if (dishes.value.length === 0) {
      await loadDishes()
    }
    
    console.log('Всего блюд загружено:', dishes.value.length)
  } catch (err) {
    console.error('Ошибка загрузки блюд меню:', err)
    console.error('Детали ошибки:', {
      message: err.message,
      response: err.response?.data,
      status: err.response?.status
    })
    error.value = err.response?.data?.message || err.message || 'Ошибка загрузки блюд выбранного меню'
  } finally {
    loading.value = false
  }
}

const loadDishes = async () => {
  try {
    console.log('Загрузка всех блюд...')
    const dishesResponse = await dishesApi.getAll({ pageSize: 1000 })
    console.log('Ответ API блюд:', dishesResponse)
    
    dishes.value = Array.isArray(dishesResponse)
      ? dishesResponse
      : (dishesResponse?.data || [])
    
    console.log('Загружено блюд:', dishes.value.length)
  } catch (err) {
    console.error('Ошибка загрузки блюд:', err)
    console.error('Детали ошибки:', {
      message: err.message,
      response: err.response?.data,
      status: err.response?.status
    })
    error.value = err.response?.data?.message || err.message || 'Ошибка загрузки блюд'
  }
}

const loadCategories = async () => {
  try {
    console.log('Загрузка категорий...')
    const categoriesResponse = await categoriesApi.getAll({ pageSize: 100 })
    console.log('Ответ API категорий:', categoriesResponse)
    
    categories.value = Array.isArray(categoriesResponse) 
      ? categoriesResponse 
      : (categoriesResponse?.data || [])
    
    console.log('Загружено категорий:', categories.value.length)
  } catch (err) {
    console.error('Ошибка загрузки категорий:', err)
    console.error('Детали ошибки:', {
      message: err.message,
      response: err.response?.data,
      status: err.response?.status
    })
  }
}

const loadData = async () => {
  loading.value = true
  error.value = ''
  
  try {
    // Загружаем все данные параллельно
    await Promise.all([
      loadMenus(),
      loadCategories(),
      loadDishes()
    ])
  } catch (err) {
    console.error('Ошибка загрузки данных:', err)
    error.value = 'Ошибка загрузки меню. Попробуйте обновить страницу.'
    
    if (err.response?.status === 401) {
      error.value = 'Требуется авторизация. Пожалуйста, войдите в систему.'
    }
  } finally {
    loading.value = false
  }
}

const openDishModal = (dish) => {
  selectedDish.value = dish
  isModalOpen.value = true
}

const closeDishModal = () => {
  isModalOpen.value = false
  selectedDish.value = null
}

const increaseQuantity = async (dish) => {
  if (!authStore.isAuthenticated) {
    // Сохраняем блюдо для добавления после авторизации
    pendingDish.value = dish
    showAuthModal.value = true
    return
  }
  await cartStore.addItem(dish)
}

const handleRequireAuth = (dish) => {
  pendingDish.value = dish
  showAuthModal.value = true
}

const closeAuthModal = () => {
  showAuthModal.value = false
  pendingDish.value = null
}

// Обработка добавления блюда после авторизации
watch(() => authStore.isAuthenticated, async (isAuthenticated, wasAuthenticated) => {
  // Срабатывает только при переходе из неавторизованного в авторизованный
  if (isAuthenticated && !wasAuthenticated && pendingDish.value) {
    try {
      // Небольшая задержка для завершения процесса авторизации
      await new Promise(resolve => setTimeout(resolve, 100))
      await cartStore.addItem(pendingDish.value)
      showAuthModal.value = false
      pendingDish.value = null
    } catch (error) {
      console.error('Ошибка добавления блюда после авторизации:', error)
      // Оставляем модальное окно открытым при ошибке
    }
  }
})

const decreaseQuantity = (dish) => {
  // Находим все элементы с таким dishId и уменьшаем количество первого (без notes)
  // Или можно уменьшать общее количество - находим первый элемент без notes
  const item = cartStore.items.find(item => 
    item.dishId === dish.id && (item.notes === null || item.notes === undefined)
  )
  
  if (item) {
    if (item.quantity > 1) {
      cartStore.updateQuantity(item.dishId, item.quantity - 1, item.notes)
    } else {
      cartStore.removeItem(item.dishId, item.notes)
    }
  } else {
    // Если нет элемента без notes, берем первый найденный
    const firstItem = cartStore.items.find(item => item.dishId === dish.id)
    if (firstItem) {
      if (firstItem.quantity > 1) {
        cartStore.updateQuantity(firstItem.dishId, firstItem.quantity - 1, firstItem.notes)
      } else {
        cartStore.removeItem(firstItem.dishId, firstItem.notes)
      }
    }
  }
}


onMounted(() => {
  loadData()
})
</script>

<style scoped>
.menu-page {
  height: calc(100vh - 200px);
  background: #F5F5F2;
  overflow: hidden;
}

.menu-layout {
  display: flex;
  height: 100%;
  overflow: hidden;
}

.menu-content {
  flex: 1;
  padding: 2rem;
  overflow-y: auto;
  overflow-x: hidden;
  min-width: 0;
  height: 100%;
}

.container {
  max-width: 1200px;
  margin: 0 auto;
}

h1 {
  margin-bottom: 2rem;
  color: #2B2B2B;
  font-size: 2.5rem;
  font-weight: bold;
}

.loading {
  text-align: center;
  padding: 4rem 2rem;
  font-size: 1.25rem;
  color: #6B6B6B;
}

.error {
  background-color: #F5F5F2;
  color: #2F5D8C;
  padding: 1rem;
  border-radius: 4px;
  margin: 2rem 0;
  border: 1px solid #E0E0DC;
}

.empty-menu {
  text-align: center;
  padding: 4rem 2rem;
  color: #6B6B6B;
  font-size: 1.25rem;
}

.empty-menu-hint {
  font-size: 0.9rem;
  color: #9B9B9B;
  margin-top: 0.5rem;
}

.dishes-content {
  margin-top: 2rem;
}

@media (max-width: 768px) {
  .menu-layout {
    flex-direction: column;
  }
  
  .menu-content {
    padding: 1rem;
  }
}
</style>
