<template>
  <div class="categories-page">
    <div class="page-header">
      <h2>Управление категориями</h2>
      <button @click="showCreateModal = true" class="btn btn-primary">Добавить категорию</button>
    </div>

    <div v-if="loading" class="loading">Загрузка...</div>
    <div v-else-if="error" class="error">{{ error }}</div>
    <div v-else>
      <!-- Фильтры -->
      <div class="filters">
        <div class="filter-group">
          <label for="searchFilter">Поиск:</label>
          <input id="searchFilter" v-model="filters.search" type="text" placeholder="Название категории..." class="form-control" />
        </div>
        <div class="filter-group">
          <button @click="clearFilters" class="btn btn-secondary btn-sm">Сбросить</button>
        </div>
      </div>
      
      <div class="categories-grid">
        <div v-for="category in categories" :key="category.id" class="category-card">
          <div class="category-info">
            <h3>{{ category.name }}</h3>
            <p v-if="category.notes" class="category-notes">{{ category.notes }}</p>
          </div>
          <div class="category-actions">
            <button @click="editCategory(category)" class="btn btn-primary btn-sm">Редактировать</button>
            <button @click="deleteCategory(category.id)" class="btn btn-danger btn-sm">Удалить</button>
          </div>
        </div>
      </div>
    </div>

    <!-- Модальное окно создания/редактирования -->
    <div v-if="showCreateModal || editingCategory" class="modal-overlay" @click="closeModal">
      <div class="modal-content" @click.stop>
        <h3>{{ editingCategory ? 'Редактировать категорию' : 'Создать категорию' }}</h3>
        <form @submit.prevent="saveCategory">
          <div class="form-group">
            <label>Название *</label>
            <input v-model="categoryForm.name" type="text" class="form-control" required />
          </div>
          <div class="form-group">
            <label>Примечания</label>
            <textarea v-model="categoryForm.notes" class="form-control" rows="3"></textarea>
          </div>
          <div class="modal-actions">
            <button type="submit" class="btn btn-success" :disabled="saving">{{ saving ? 'Сохранение...' : 'Сохранить' }}</button>
            <button type="button" @click="closeModal" class="btn btn-secondary">Отмена</button>
          </div>
        </form>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, onMounted, watch } from 'vue'
import { categoriesApi } from '@/api/categories'

const categories = ref([])
const loading = ref(false)
const error = ref('')
const showCreateModal = ref(false)
const editingCategory = ref(null)
const saving = ref(false)

const categoryForm = ref({
  name: '',
  notes: ''
})

const filters = ref({
  search: ''
})

const clearFilters = () => {
  filters.value = { search: '' }
  loadCategories()
}

const loadCategories = async () => {
  loading.value = true
  error.value = ''
  try {
    const params = {}
    if (filters.value.search) {
      params.search = filters.value.search
    }
    params.pageSize = 1000
    
    const data = await categoriesApi.getAll(params)
    categories.value = Array.isArray(data) ? data : (data.data || [])
  } catch (err) {
    error.value = 'Ошибка загрузки категорий'
    console.error(err)
  } finally {
    loading.value = false
  }
}

const editCategory = (category) => {
  editingCategory.value = category
  categoryForm.value = {
    name: category.name,
    notes: category.notes || ''
  }
}

const deleteCategory = async (id) => {
  if (!confirm('Вы уверены, что хотите удалить эту категорию?')) return
  
  try {
    await categoriesApi.delete(id)
    await loadCategories()
  } catch (err) {
    error.value = 'Ошибка удаления категории'
    console.error(err)
  }
}

const saveCategory = async () => {
  saving.value = true
  try {
    if (editingCategory.value) {
      await categoriesApi.update(editingCategory.value.id, categoryForm.value)
    } else {
      await categoriesApi.create(categoryForm.value)
    }
    await loadCategories()
    closeModal()
  } catch (err) {
    error.value = err.response?.data?.message || 'Ошибка сохранения категории'
    console.error(err)
  } finally {
    saving.value = false
  }
}

const closeModal = () => {
  showCreateModal.value = false
  editingCategory.value = null
  categoryForm.value = {
    name: '',
    notes: ''
  }
}

// Debounce функция для текстовых полей
let searchTimeout = null
const debouncedLoadCategories = () => {
  if (searchTimeout) {
    clearTimeout(searchTimeout)
  }
  searchTimeout = setTimeout(() => {
    loadCategories()
  }, 500)
}

// Debounced реакция для текстового поиска
watch(() => filters.value.search, () => {
  debouncedLoadCategories()
})

onMounted(() => {
  loadCategories()
})
</script>

<style scoped>
.categories-page {
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

.page-header h2 {
  color: #2F5D8C;
  margin: 0;
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

.categories-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(300px, 1fr));
  gap: 1.5rem;
}

.category-card {
  background: #FFFFFF;
  border-radius: 8px;
  padding: 1.5rem;
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.05);
  border: 1px solid #E0E0DC;
  display: flex;
  flex-direction: column;
  justify-content: space-between;
  gap: 1rem;
}

.category-info {
  flex: 1;
}

.category-info h3 {
  color: #2B2B2B;
  margin-bottom: 0.5rem;
  font-size: 1.3rem;
}

.category-notes {
  color: #6B6B6B;
  font-size: 0.9rem;
  margin: 0;
  font-style: italic;
}

.category-actions {
  display: flex;
  gap: 0.5rem;
  flex-shrink: 0;
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
