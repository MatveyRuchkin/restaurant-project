<template>
  <div class="ingredients-page">
    <div class="page-header">
      <h2>Управление ингредиентами</h2>
      <button @click="showCreateModal = true" class="btn btn-primary">Добавить ингредиент</button>
    </div>

    <div v-if="loading" class="loading">Загрузка...</div>
    <div v-else-if="error" class="error">{{ error }}</div>
    <div v-else>
      <!-- Фильтры -->
      <div class="filters">
        <div class="filter-group">
          <label for="searchFilter">Поиск:</label>
          <input id="searchFilter" v-model="filters.search" type="text" placeholder="Название ингредиента..." class="form-control" />
        </div>
        <div class="filter-group">
          <button @click="clearFilters" class="btn btn-secondary btn-sm">Сбросить</button>
        </div>
      </div>
      
      <div class="ingredients-grid">
        <div v-for="ingredient in ingredients" :key="ingredient.id" class="ingredient-card">
          <h3>{{ ingredient.name }}</h3>
          <div class="ingredient-actions">
            <button @click="editIngredient(ingredient)" class="btn btn-primary btn-sm">Редактировать</button>
            <button @click="deleteIngredient(ingredient.id)" class="btn btn-danger btn-sm">Удалить</button>
          </div>
        </div>
      </div>
    </div>

    <!-- Модальное окно создания/редактирования -->
    <div v-if="showCreateModal || editingIngredient" class="modal-overlay" @click="closeModal">
      <div class="modal-content" @click.stop>
        <h3>{{ editingIngredient ? 'Редактировать ингредиент' : 'Создать ингредиент' }}</h3>
        <form @submit.prevent="saveIngredient">
          <div class="form-group">
            <label>Название *</label>
            <input v-model="ingredientForm.name" type="text" class="form-control" required />
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
import { ingredientsApi } from '@/api/ingredients'

const ingredients = ref([])
const loading = ref(false)
const error = ref('')
const showCreateModal = ref(false)
const editingIngredient = ref(null)
const saving = ref(false)

const ingredientForm = ref({ name: '' })

const filters = ref({
  search: ''
})

const clearFilters = () => {
  filters.value = { search: '' }
  loadIngredients()
}

const loadIngredients = async () => {
  loading.value = true
  error.value = ''
  try {
    const params = {}
    if (filters.value.search) {
      params.search = filters.value.search
    }
    params.pageSize = 1000
    
    const data = await ingredientsApi.getAll(params)
    ingredients.value = Array.isArray(data) ? data : (data.data || [])
  } catch (err) {
    error.value = 'Ошибка загрузки ингредиентов'
    console.error(err)
  } finally {
    loading.value = false
  }
}

const editIngredient = (ingredient) => {
  editingIngredient.value = ingredient
  ingredientForm.value = { name: ingredient.name }
}

const deleteIngredient = async (id) => {
  if (!confirm('Вы уверены, что хотите удалить этот ингредиент?')) return
  
  try {
    await ingredientsApi.delete(id)
    await loadIngredients()
  } catch (err) {
    error.value = 'Ошибка удаления ингредиента'
    console.error(err)
  }
}

const saveIngredient = async () => {
  saving.value = true
  try {
    if (editingIngredient.value) {
      await ingredientsApi.update(editingIngredient.value.id, ingredientForm.value)
    } else {
      await ingredientsApi.create(ingredientForm.value)
    }
    await loadIngredients()
    closeModal()
  } catch (err) {
    error.value = err.response?.data?.message || 'Ошибка сохранения ингредиента'
    console.error(err)
  } finally {
    saving.value = false
  }
}

const closeModal = () => {
  showCreateModal.value = false
  editingIngredient.value = null
  ingredientForm.value = { name: '' }
}

// Debounce функция для текстовых полей
let searchTimeout = null
const debouncedLoadIngredients = () => {
  if (searchTimeout) {
    clearTimeout(searchTimeout)
  }
  searchTimeout = setTimeout(() => {
    loadIngredients()
  }, 500)
}

// Debounced реакция для текстового поиска
watch(() => filters.value.search, () => {
  debouncedLoadIngredients()
})

onMounted(() => {
  loadIngredients()
})
</script>

<style scoped>
.ingredients-page {
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

.ingredients-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(320px, 1fr));
  gap: 1.5rem;
}

.ingredient-card {
  background: #FFFFFF;
  border-radius: 8px;
  padding: 1.5rem;
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.05);
  border: 1px solid #E0E0DC;
  display: flex;
  flex-direction: column;
  gap: 1rem;
}

.ingredient-card h3 {
  color: #2B2B2B;
  margin: 0;
  word-wrap: break-word;
  word-break: break-word;
  line-height: 1.4;
}

.ingredient-actions {
  display: flex;
  gap: 0.5rem;
  flex-wrap: wrap;
}

.ingredient-actions .btn {
  white-space: nowrap;
  flex: 1;
  min-width: 120px;
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

