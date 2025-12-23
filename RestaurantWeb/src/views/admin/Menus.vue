<template>
  <div class="menus-page">
    <div class="page-header">
      <h2>Управление меню</h2>
      <button @click="showCreateModal = true" class="btn btn-primary">Добавить меню</button>
    </div>

    <div v-if="loading" class="loading">Загрузка...</div>
    <div v-else-if="error" class="error">{{ error }}</div>
    <div v-else>
      <!-- Фильтры -->
      <div class="filters">
        <div class="filter-group">
          <label for="searchFilter">Поиск:</label>
          <input id="searchFilter" v-model="filters.search" type="text" placeholder="Название меню..." class="form-control" />
        </div>
        <div class="filter-group">
          <button @click="clearFilters" class="btn btn-secondary btn-sm">Сбросить</button>
        </div>
      </div>
      
      <div class="menus-list">
        <div v-for="menu in menus" :key="menu.id" class="menu-card">
          <div class="menu-header">
            <h3>{{ menu.name }}</h3>
            <div class="menu-actions">
              <button @click="editMenu(menu)" class="btn btn-primary btn-sm">Редактировать</button>
              <button @click="manageDishes(menu)" class="btn btn-success btn-sm">Управление блюдами</button>
              <button @click="deleteMenu(menu.id)" class="btn btn-danger btn-sm">Удалить</button>
            </div>
          </div>
        </div>
      </div>
    </div>

    <!-- Модальное окно создания/редактирования -->
    <div v-if="showCreateModal || editingMenu" class="modal-overlay" @click="closeModal">
      <div class="modal-content" @click.stop>
        <h3>{{ editingMenu ? 'Редактировать меню' : 'Создать меню' }}</h3>
        <form @submit.prevent="saveMenu">
          <div class="form-group">
            <label>Название *</label>
            <input v-model="menuForm.name" type="text" class="form-control" required />
          </div>
          <div class="modal-actions">
            <button type="submit" class="btn btn-success" :disabled="saving">{{ saving ? 'Сохранение...' : 'Сохранить' }}</button>
            <button type="button" @click="closeModal" class="btn btn-secondary">Отмена</button>
          </div>
        </form>
      </div>
    </div>

    <!-- Модальное окно управления блюдами -->
    <div v-if="managingMenu" class="modal-overlay" @click="closeDishModal">
      <div class="modal-content large" @click.stop>
        <h3>Блюда в меню: {{ managingMenu.name }}</h3>
        <div class="dish-management">
          <div class="add-dish-section">
            <select v-model="selectedDishId" class="form-control">
              <option value="">Выберите блюдо</option>
              <option v-for="dish in availableDishes" :key="dish.id" :value="dish.id">{{ dish.name }}</option>
            </select>
            <button @click="addDishToMenu" class="btn btn-primary" :disabled="!selectedDishId">Добавить</button>
          </div>
          <div class="menu-dishes-list">
            <div v-for="md in menuDishes" :key="md.id" class="menu-dish-item">
              <span>{{ md.dishName }}</span>
              <button @click="removeDishFromMenu(md.id)" class="btn btn-danger btn-sm">Удалить</button>
            </div>
          </div>
        </div>
        <div class="modal-actions">
          <button @click="closeDishModal" class="btn btn-secondary">Закрыть</button>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, onMounted, watch } from 'vue'
import { menusApi } from '@/api/menus'
import { dishesApi } from '@/api/dishes'
import { menuDishesApi } from '@/api/menuDishes'

const menus = ref([])
const availableDishes = ref([])
const menuDishes = ref([])
const loading = ref(false)
const error = ref('')
const showCreateModal = ref(false)
const editingMenu = ref(null)
const managingMenu = ref(null)
const saving = ref(false)
const selectedDishId = ref('')

const menuForm = ref({ name: '' })

const filters = ref({
  search: ''
})

const clearFilters = () => {
  filters.value = { search: '' }
  loadMenus()
}

const loadMenus = async () => {
  loading.value = true
  error.value = ''
  try {
    const params = {}
    if (filters.value.search) {
      params.search = filters.value.search
    }
    params.pageSize = 1000
    
    const data = await menusApi.getAll(params)
    menus.value = Array.isArray(data) ? data : (data.data || [])
  } catch (err) {
    error.value = 'Ошибка загрузки меню'
    console.error(err)
  } finally {
    loading.value = false
  }
}

const loadDishes = async () => {
  try {
    const data = await dishesApi.getAll()
    availableDishes.value = Array.isArray(data) ? data : (data.data || [])
  } catch (err) {
    console.error('Ошибка загрузки блюд:', err)
  }
}

const loadMenuDishes = async (menuId) => {
  try {
    const data = await menuDishesApi.getByMenuId(menuId)
    menuDishes.value = Array.isArray(data) ? data : (data.data || [])
  } catch (err) {
    console.error('Ошибка загрузки блюд меню:', err)
  }
}

const editMenu = (menu) => {
  editingMenu.value = menu
  menuForm.value = { name: menu.name }
}

const manageDishes = async (menu) => {
  managingMenu.value = menu
  await loadMenuDishes(menu.id)
  await loadDishes()
}

const addDishToMenu = async () => {
  if (!selectedDishId.value || !managingMenu.value) return
  
  try {
    await menuDishesApi.create({
      menuId: managingMenu.value.id,
      dishId: selectedDishId.value
    })
    await loadMenuDishes(managingMenu.value.id)
    selectedDishId.value = ''
  } catch (err) {
    error.value = err.response?.data?.message || 'Ошибка добавления блюда'
    console.error(err)
  }
}

const removeDishFromMenu = async (menuDishId) => {
  if (!confirm('Удалить блюдо из меню?')) return
  
  try {
    await menuDishesApi.delete(menuDishId)
    await loadMenuDishes(managingMenu.value.id)
  } catch (err) {
    error.value = 'Ошибка удаления блюда'
    console.error(err)
  }
}

const deleteMenu = async (id) => {
  if (!confirm('Вы уверены, что хотите удалить это меню?')) return
  
  try {
    await menusApi.delete(id)
    await loadMenus()
  } catch (err) {
    error.value = 'Ошибка удаления меню'
    console.error(err)
  }
}

const saveMenu = async () => {
  saving.value = true
  try {
    if (editingMenu.value) {
      await menusApi.update(editingMenu.value.id, menuForm.value)
    } else {
      await menusApi.create(menuForm.value)
    }
    await loadMenus()
    closeModal()
  } catch (err) {
    error.value = err.response?.data?.message || 'Ошибка сохранения меню'
    console.error(err)
  } finally {
    saving.value = false
  }
}

const closeModal = () => {
  showCreateModal.value = false
  editingMenu.value = null
  menuForm.value = { name: '' }
}

const closeDishModal = () => {
  managingMenu.value = null
  menuDishes.value = []
  selectedDishId.value = ''
}

// Debounce функция для текстовых полей
let searchTimeout = null
const debouncedLoadMenus = () => {
  if (searchTimeout) {
    clearTimeout(searchTimeout)
  }
  searchTimeout = setTimeout(() => {
    loadMenus()
  }, 500)
}

// Debounced реакция для текстового поиска
watch(() => filters.value.search, () => {
  debouncedLoadMenus()
})

onMounted(() => {
  loadMenus()
})
</script>

<style scoped>
.menus-page {
  padding: 2rem 0;
}

.page-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 2rem;
  position: sticky;
  top: 0;
  background: #F5F5F2;
  padding: 1rem 0;
  z-index: 100;
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

.menus-list {
  display: flex;
  flex-direction: column;
  gap: 1rem;
}

.menu-card {
  background: #FFFFFF;
  border-radius: 8px;
  padding: 1.5rem;
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.05);
  border: 1px solid #E0E0DC;
}

.menu-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
}

.menu-header h3 {
  color: #2B2B2B;
  margin: 0;
}

.menu-actions {
  display: flex;
  gap: 0.5rem;
}

.modal-content.large {
  max-width: 700px;
}

.dish-management {
  margin: 1.5rem 0;
}

.add-dish-section {
  display: flex;
  gap: 1rem;
  margin-bottom: 1.5rem;
}

.add-dish-section select {
  flex: 1;
}

.menu-dishes-list {
  max-height: 400px;
  overflow-y: auto;
  border: 1px solid #E0E0DC;
  border-radius: 4px;
  padding: 1rem;
}

.menu-dish-item {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 0.75rem;
  border-bottom: 1px solid #E0E0DC;
}

.menu-dish-item:last-child {
  border-bottom: none;
}

.modal-overlay {
  position: fixed;
  top: 0;
  left: 0;
  right: 0;
  bottom: 0;
  background: rgba(0, 0, 0, 0.5);
  display: flex;
  align-items: center;
  justify-content: center;
  z-index: 1000;
}

.modal-content {
  background: #FFFFFF;
  border-radius: 8px;
  padding: 2rem;
  width: 90%;
  max-width: 500px;
  max-height: 90vh;
  overflow-y: auto;
}

.modal-content h3 {
  color: #2F5D8C;
  margin-bottom: 1.5rem;
}

.form-group {
  margin-bottom: 1rem;
}

.form-group label {
  display: block;
  margin-bottom: 0.5rem;
  color: #2B2B2B;
  font-weight: 500;
}

.modal-actions {
  display: flex;
  gap: 1rem;
  margin-top: 1.5rem;
}

.btn-secondary {
  background: #6B6B6B;
  color: white;
}

.btn-secondary:hover {
  background: #555;
}
</style>

