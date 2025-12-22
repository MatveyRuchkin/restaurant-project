<template>
  <div class="dishes-page">
    <div class="page-header">
      <h2>Управление блюдами</h2>
      <button @click="showCreateModal = true" class="btn btn-primary">Добавить блюдо</button>
    </div>

    <div v-if="loading" class="loading">Загрузка...</div>
    <div v-else-if="error" class="error">{{ error }}</div>
    <div v-else>
      <!-- Фильтры -->
      <div class="filters">
        <div class="filter-group">
          <label for="categoryFilter">Категория:</label>
          <select id="categoryFilter" v-model="filters.categoryId" class="form-control">
            <option value="">Все</option>
            <option v-for="cat in categories" :key="cat.id" :value="cat.id">{{ cat.name }}</option>
          </select>
        </div>
        <div class="filter-group">
          <label for="searchFilter">Поиск:</label>
          <input id="searchFilter" v-model="filters.search" type="text" placeholder="Название или описание..." class="form-control" />
        </div>
        <div class="filter-group price-range-group">
          <label>Цена:</label>
          <div class="price-range-inputs">
            <input id="minPriceFilter" v-model.number="filters.minPrice" type="number" step="0.01" min="0" placeholder="Мин." class="form-control price-input" />
            <span class="price-range-separator">-</span>
            <input id="maxPriceFilter" v-model.number="filters.maxPrice" type="number" step="0.01" min="0" placeholder="Макс." class="form-control price-input" />
          </div>
        </div>
        <div class="filter-group">
          <button @click="applyFilters" class="btn btn-primary btn-sm">Применить</button>
          <button @click="clearFilters" class="btn btn-secondary btn-sm">Сбросить</button>
        </div>
      </div>
      
      <div class="dishes-grid">
        <div v-for="dish in dishes" :key="dish.id" class="dish-card">
          <div class="dish-info">
            <h3>{{ dish.name }}</h3>
            <p class="dish-description">{{ dish.description || 'Нет описания' }}</p>
            <div class="dish-meta">
              <span class="dish-category">{{ dish.categoryName }}</span>
              <span class="dish-price">{{ formatCurrency(dish.price) }}</span>
            </div>
          </div>
          <div class="dish-actions">
            <button @click="editDish(dish)" class="btn btn-primary btn-sm">Редактировать</button>
            <button @click="deleteDish(dish.id)" class="btn btn-danger btn-sm">Удалить</button>
          </div>
        </div>
      </div>
    </div>

    <!-- Модальное окно создания/редактирования -->
    <div v-if="showCreateModal || editingDish" class="modal-overlay" @click="closeModal">
      <div class="modal-content" @click.stop>
        <h3>{{ editingDish ? 'Редактировать блюдо' : 'Создать блюдо' }}</h3>
        <form @submit.prevent="saveDish">
          <div class="form-group">
            <label>Название *</label>
            <input v-model="dishForm.name" type="text" class="form-control" required />
          </div>
          <div class="form-group">
            <label>Описание</label>
            <textarea v-model="dishForm.description" class="form-control" rows="3"></textarea>
          </div>
          <div class="form-group">
            <label>Цена *</label>
            <input v-model.number="dishForm.price" type="number" step="0.01" min="0.01" class="form-control" required />
          </div>
          <div class="form-group">
            <label>Категория *</label>
            <select v-model="dishForm.categoryId" class="form-control" required>
              <option value="">Выберите категорию</option>
              <option v-for="cat in categories" :key="cat.id" :value="cat.id">{{ cat.name }}</option>
            </select>
          </div>
          <div class="form-group">
            <label>Ингредиенты</label>
            <div class="ingredients-list">
              <div v-for="(ing, index) in dishForm.ingredients" :key="index" class="ingredient-item">
                <select v-model="ing.ingredientId" class="form-control">
                  <option value="">Выберите ингредиент</option>
                  <option v-for="ingredient in ingredients" :key="ingredient.id" :value="ingredient.id">{{ ingredient.name }}</option>
                </select>
                <button type="button" @click="removeIngredient(index)" class="btn btn-danger btn-sm">Удалить</button>
              </div>
              <button type="button" @click="addIngredient" class="btn btn-secondary btn-sm">Добавить ингредиент</button>
            </div>
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
import { ref, onMounted } from 'vue'
import { dishesApi } from '@/api/dishes'
import { categoriesApi } from '@/api/categories'
import { ingredientsApi } from '@/api/ingredients'
import { dishIngredientsApi } from '@/api/dishIngredients'
import { useApi } from '@/composables/useApi'
import { useErrorHandler } from '@/composables/useErrorHandler'
import { formatCurrency } from '@/utils/formatters'

const dishes = ref([])
const categories = ref([])
const ingredients = ref([])
const dishIngredients = ref([])
const showCreateModal = ref(false)
const editingDish = ref(null)

const { loading, execute } = useApi()
const { error, handleError, clearError } = useErrorHandler()

const filters = ref({
  categoryId: '',
  search: '',
  minPrice: null,
  maxPrice: null
})
const saving = ref(false)

const dishForm = ref({
  name: '',
  description: '',
  price: 0,
  categoryId: '',
  ingredients: []
})

const applyFilters = () => {
  loadDishes()
}

const clearFilters = () => {
  filters.value = {
    categoryId: '',
    search: '',
    minPrice: null,
    maxPrice: null
  }
  loadDishes()
}

const loadDishes = async () => {
  clearError()
  const params = {}
  if (filters.value.categoryId) {
    params.categoryId = filters.value.categoryId
  }
  if (filters.value.search) {
    params.search = filters.value.search
  }
  if (filters.value.minPrice !== null && filters.value.minPrice !== '') {
    params.minPrice = filters.value.minPrice
  }
  if (filters.value.maxPrice !== null && filters.value.maxPrice !== '') {
    params.maxPrice = filters.value.maxPrice
  }
  params.pageSize = 1000
  
  const { data, error: apiError } = await execute(() => dishesApi.getAll(params))
  if (data) {
    dishes.value = Array.isArray(data) ? data : (data.data || [])
  } else if (apiError) {
    handleError(apiError)
  }
}

const loadCategories = async () => {
  const { data } = await execute(() => categoriesApi.getAll())
  if (data) {
    categories.value = Array.isArray(data) ? data : (data.data || [])
  }
}

const loadIngredients = async () => {
  const { data } = await execute(() => ingredientsApi.getAll({ pageSize: 1000 }))
  if (data) {
    ingredients.value = Array.isArray(data) ? data : (data.data || [])
  }
}

const loadDishIngredients = async (dishId) => {
  const { data } = await execute(() => dishIngredientsApi.getByDishId(dishId))
  if (data) {
    dishIngredients.value = Array.isArray(data) ? data : []
  } else {
    dishIngredients.value = []
  }
}

const editDish = async (dish) => {
  editingDish.value = dish
  await loadDishIngredients(dish.id)
  
  const category = categories.value.find(cat => cat.name === dish.categoryName)
  
  dishForm.value = {
    name: dish.name,
    description: dish.description || '',
    price: dish.price,
    categoryId: category?.id || '',
    ingredients: dishIngredients.value.map(di => ({
      id: di.id,
      ingredientId: di.ingredientId,
      ingredientName: di.ingredientName
    }))
  }
}

const deleteDish = async (id) => {
  if (!confirm('Вы уверены, что хотите удалить это блюдо?')) return
  
  clearError()
  const { error: apiError } = await execute(() => dishesApi.delete(id))
  if (!apiError) {
    await loadDishes()
  } else {
    handleError(apiError)
  }
}

const saveDish = async () => {
  clearError()
  saving.value = true
  
  if (!dishForm.value.name || !dishForm.value.name.trim()) {
    handleError(new Error('Название блюда обязательно'))
    saving.value = false
    return
  }
  
  if (!dishForm.value.price || dishForm.value.price <= 0) {
    handleError(new Error('Цена должна быть больше 0'))
    saving.value = false
    return
  }
  
  if (!dishForm.value.categoryId) {
    handleError(new Error('Необходимо выбрать категорию'))
    saving.value = false
    return
  }
  
  try {
    const dishData = {
      name: dishForm.value.name.trim(),
      description: dishForm.value.description?.trim() || '',
      price: dishForm.value.price,
      categoryId: dishForm.value.categoryId
    }
    
    let dishId
    if (editingDish.value) {
      const { error: updateError } = await execute(() => 
        dishesApi.update(editingDish.value.id, dishData)
      )
      if (updateError) {
        handleError(updateError)
        saving.value = false
        return
      }
      dishId = editingDish.value.id
      
      const { data: currentDishIngredients } = await execute(() => 
        dishIngredientsApi.getByDishId(dishId)
      )
      if (currentDishIngredients) {
        for (const di of currentDishIngredients) {
          await execute(() => dishIngredientsApi.delete(di.id))
        }
      }
    } else {
      const { data: created, error: createError } = await execute(() => 
        dishesApi.create(dishData)
      )
      if (createError) {
        handleError(createError)
        saving.value = false
        return
      }
      dishId = created?.id || created?.data?.id
    }
    
    for (const ing of dishForm.value.ingredients) {
      if (ing.ingredientId) {
        await execute(() => dishIngredientsApi.create({
          dishId: dishId,
          ingredientId: ing.ingredientId
        }))
      }
    }
    
    await loadDishes()
    closeModal()
  } catch (err) {
    handleError(err)
  } finally {
    saving.value = false
  }
}

const addIngredient = () => {
  dishForm.value.ingredients.push({
    id: null,
    ingredientId: '',
    ingredientName: ''
  })
}

const removeIngredient = (index) => {
  dishForm.value.ingredients.splice(index, 1)
}

const closeModal = () => {
  showCreateModal.value = false
  editingDish.value = null
  dishForm.value = {
    name: '',
    description: '',
    price: 0,
    categoryId: '',
    ingredients: []
  }
  dishIngredients.value = []
}

onMounted(() => {
  loadDishes()
  loadCategories()
  loadIngredients()
})
</script>

<style scoped>
.dishes-page {
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

.dishes-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(300px, 1fr));
  gap: 1.5rem;
}

.dish-card {
  background: #FFFFFF;
  border-radius: 8px;
  padding: 1.5rem;
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.05);
  border: 1px solid #E0E0DC;
  display: flex;
  flex-direction: column;
  justify-content: space-between;
}

.dish-info h3 {
  color: #2B2B2B;
  margin-bottom: 0.5rem;
  font-size: 1.3rem;
}

.dish-description {
  color: #6B6B6B;
  font-size: 0.9rem;
  margin-bottom: 1rem;
  min-height: 40px;
}

.dish-meta {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 1rem;
}

.dish-category {
  background: #E6F2FF;
  color: #2F5D8C;
  padding: 0.25rem 0.75rem;
  border-radius: 4px;
  font-size: 0.85rem;
  font-weight: 500;
}

.dish-price {
  font-size: 1.2rem;
  font-weight: bold;
  color: #2F5D8C;
}

.dish-actions {
  display: flex;
  gap: 0.5rem;
}

.btn-sm {
  padding: 0.4rem 0.8rem;
  font-size: 0.9rem;
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

.price-input {
  -moz-appearance: textfield;
}

.price-input::-webkit-outer-spin-button,
.price-input::-webkit-inner-spin-button {
  -webkit-appearance: none;
  margin: 0;
}

.ingredients-list {
  display: flex;
  flex-direction: column;
  gap: 0.5rem;
}

.ingredient-item {
  display: flex;
  gap: 0.5rem;
  align-items: center;
}

.ingredient-item select {
  flex: 1;
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
</style>
