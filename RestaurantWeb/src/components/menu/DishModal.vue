<template>
  <div v-if="isOpen" class="modal-overlay" @click="closeModal">
    <div class="modal-content" @click.stop>
      <button class="modal-close" @click="closeModal">×</button>
      
      <div v-if="loading" class="modal-loading">
        <p>Загрузка...</p>
      </div>
      
      <div v-else-if="dish">
        <h2 class="modal-title">{{ dish.name }}</h2>
        
        <div class="modal-body">
          <div class="modal-section">
            <p v-if="dish.description" class="dish-description-full">
              {{ dish.description }}
            </p>
          </div>
          
          <div class="modal-section">
            <h3 class="section-title">Ингредиенты:</h3>
            <div v-if="visibleIngredients.length > 0" class="ingredients-list">
              <div 
                v-for="ingredient in visibleIngredients" 
                :key="ingredient.id"
                class="ingredient-item"
              >
                <span class="ingredient-name">{{ ingredient.ingredientName }}</span>
                <span v-if="ingredient.quantity" class="ingredient-quantity">
                  {{ ingredient.quantity }}
                </span>
                <button 
                  @click="removeIngredient(ingredient.id)"
                  class="btn-remove-ingredient"
                  title="Удалить ингредиент"
                >
                  ×
                </button>
              </div>
            </div>
            <p v-else class="no-ingredients">Ингредиенты не указаны</p>
            
            <div v-if="removedIngredients.length > 0" class="removed-ingredients">
              <h4 class="removed-title">Удалено из состава:</h4>
              <div class="removed-list">
                <div 
                  v-for="ingredient in removedIngredients" 
                  :key="ingredient.id"
                  class="removed-item-wrapper"
                >
                  <span class="removed-item">{{ ingredient.ingredientName }}</span>
                  <button 
                    @click="restoreIngredient(ingredient.id)"
                    class="btn-restore-ingredient"
                    title="Вернуть ингредиент"
                  >
                    ↺
                  </button>
                </div>
              </div>
            </div>
          </div>
          
          <div class="modal-section price-section">
            <div class="price-display">
              <span class="price-label">Цена:</span>
              <span class="price-value">{{ formatCurrency(dish.price) }}</span>
            </div>
          </div>
        </div>
        
        <div class="modal-footer">
          <div class="modal-cart-controls">
            <template v-if="cartQuantity > 0">
              <button 
                @click="decreaseQuantity" 
                class="btn-quantity btn-quantity-large"
              >
                −
              </button>
              <span class="quantity-display-large">
                {{ cartQuantity }}
              </span>
              <button 
                @click="increaseQuantity" 
                class="btn-quantity btn-quantity-large btn-add"
              >
                +
              </button>
            </template>
            <button 
              v-else
              @click="increaseQuantity" 
              class="btn btn-success btn-large"
            >
              Добавить в корзину
            </button>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, computed, watch } from 'vue'
import { dishIngredientsApi } from '@/api/dishIngredients'
import { useCartStore } from '@/stores/cart'
import { useAuthStore } from '@/stores/auth'

const cartStore = useCartStore()
const authStore = useAuthStore()

const emit = defineEmits(['close', 'add-to-cart', 'require-auth'])

const props = defineProps({
  isOpen: {
    type: Boolean,
    default: false
  },
  dish: {
    type: Object,
    default: null
  }
})


const ingredients = ref([])
const removedIngredientIds = ref(new Set())
const loading = ref(false)

const loadIngredients = async () => {
  if (!props.dish || !props.dish.id) return
  
  loading.value = true
  try {
    const response = await dishIngredientsApi.getByDishId(props.dish.id)
    ingredients.value = Array.isArray(response)
      ? response
      : (response?.data || [])
  } catch (error) {
    console.error('Ошибка загрузки ингредиентов:', error)
    ingredients.value = []
  } finally {
    loading.value = false
  }
}

const closeModal = () => {
  emit('close')
}

const visibleIngredients = computed(() => {
  return ingredients.value.filter(ing => !removedIngredientIds.value.has(ing.id))
})

const removedIngredients = computed(() => {
  return ingredients.value.filter(ing => removedIngredientIds.value.has(ing.id))
})

const removeIngredient = (ingredientId) => {
  removedIngredientIds.value.add(ingredientId)
}

const restoreIngredient = (ingredientId) => {
  removedIngredientIds.value.delete(ingredientId)
}

const cartQuantity = computed(() => {
  if (!props.dish || !props.dish.id) return 0
  
  // Формируем строку с удаленными ингредиентами для поиска
  const removedNames = removedIngredients.value.map(ing => ing.ingredientName).join(', ')
  const notes = removedNames ? `Удалено: ${removedNames}` : null
  
  // Ищем количество этого блюда с такими же notes
  const item = cartStore.items.find(item => 
    item.dishId === props.dish.id && item.notes === notes
  )
  
  return item ? item.quantity : 0
})

const increaseQuantity = () => {
  if (!authStore.isAuthenticated) {
    // Формируем строку с удаленными ингредиентами
    const removedNames = removedIngredients.value.map(ing => ing.ingredientName).join(', ')
    const notes = removedNames ? `Удалено: ${removedNames}` : null
    
    // Передаем блюдо с информацией об удаленных ингредиентах
    const dishWithNotes = {
      ...props.dish,
      notes: notes
    }
    
    // Эмитим событие для открытия модального окна авторизации
    emit('require-auth', dishWithNotes)
    return
  }
  
  // Формируем строку с удаленными ингредиентами
  const removedNames = removedIngredients.value.map(ing => ing.ingredientName).join(', ')
  const notes = removedNames ? `Удалено: ${removedNames}` : null
  
  // Передаем блюдо с информацией об удаленных ингредиентах
  const dishWithNotes = {
    ...props.dish,
    notes: notes
  }
  
  cartStore.addItem(dishWithNotes)
}

const decreaseQuantity = () => {
  if (!authStore.isAuthenticated) {
    return
  }
  
  // Формируем строку с удаленными ингредиентами
  const removedNames = removedIngredients.value.map(ing => ing.ingredientName).join(', ')
  const notes = removedNames ? `Удалено: ${removedNames}` : null
  
  const item = cartStore.items.find(item => 
    item.dishId === props.dish.id && item.notes === notes
  )
  
  if (item) {
    if (item.quantity > 1) {
      cartStore.updateQuantity(item.dishId, item.quantity - 1, item.notes)
    } else {
      cartStore.removeItem(item.dishId, item.notes)
    }
  }
}

const formatCurrency = (value) => {
  return new Intl.NumberFormat('ru-RU', {
    style: 'currency',
    currency: 'RUB'
  }).format(value)
}

// Загружаем ингредиенты при открытии модального окна
watch(() => props.isOpen, (newValue) => {
  if (newValue && props.dish) {
    loadIngredients()
    removedIngredientIds.value.clear() // Сбрасываем удаленные ингредиенты при открытии
  } else {
    ingredients.value = []
    removedIngredientIds.value.clear()
  }
})
</script>

<style scoped>
.modal-overlay {
  position: fixed;
  top: 0;
  left: 0;
  right: 0;
  bottom: 0;
  background-color: rgba(0, 0, 0, 0.6);
  display: flex;
  align-items: center;
  justify-content: center;
  z-index: 1000;
  padding: 1rem;
  animation: fadeIn 0.2s ease-out;
}

@keyframes fadeIn {
  from {
    opacity: 0;
  }
  to {
    opacity: 1;
  }
}

.modal-content {
  background: #FFFFFF;
  border-radius: 12px;
  max-width: 600px;
  width: 100%;
  max-height: 90vh;
  overflow-y: auto;
  position: relative;
  box-shadow: 0 10px 40px rgba(0, 0, 0, 0.2);
  border: 1px solid #E0E0DC;
  animation: slideUp 0.3s ease-out;
}

@keyframes slideUp {
  from {
    transform: translateY(20px);
    opacity: 0;
  }
  to {
    transform: translateY(0);
    opacity: 1;
  }
}

.modal-close {
  position: absolute;
  top: 1rem;
  right: 1rem;
  background: none;
  border: none;
  font-size: 2rem;
  color: #6B6B6B;
  cursor: pointer;
  width: 40px;
  height: 40px;
  display: flex;
  align-items: center;
  justify-content: center;
  border-radius: 50%;
  transition: all 0.2s;
  z-index: 10;
}

.modal-close:hover {
  background-color: #F5F5F2;
  color: #2B2B2B;
}

.modal-title {
  font-size: 2rem;
  color: #2B2B2B;
  margin: 2rem 2rem 1rem 2rem;
  padding-right: 3rem;
  font-weight: bold;
}

.modal-body {
  padding: 0 2rem;
}

.modal-section {
  margin-bottom: 1.5rem;
}

.dish-description-full {
  color: #6B6B6B;
  line-height: 1.6;
  font-size: 1.1rem;
}

.section-title {
  font-size: 1.3rem;
  color: #2F5D8C;
  margin-bottom: 1rem;
  font-weight: 600;
}

.ingredients-list {
  display: flex;
  flex-direction: column;
  gap: 0.75rem;
}

.ingredient-item {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 0.75rem;
  background: #F5F5F2;
  border-radius: 6px;
  border-left: 2px solid #E3B23C;
  gap: 0.5rem;
}

.ingredient-name {
  font-weight: 500;
  color: #2B2B2B;
  flex: 1;
}

.ingredient-quantity {
  color: #6B6B6B;
  font-size: 0.9rem;
}

.btn-remove-ingredient {
  background-color: #2F5D8C;
  color: white;
  border: none;
  border-radius: 50%;
  width: 24px;
  height: 24px;
  display: flex;
  align-items: center;
  justify-content: center;
  cursor: pointer;
  font-size: 1.2rem;
  line-height: 1;
  transition: all 0.2s;
  flex-shrink: 0;
}

.btn-remove-ingredient:hover {
  background-color: #254A70;
  transform: scale(1.1);
}

.removed-ingredients {
  margin-top: 1rem;
  padding: 1rem;
  background-color: #F5F5F2;
  border-radius: 6px;
  border-left: 3px solid #2F5D8C;
}

.removed-title {
  font-size: 0.9rem;
  color: #2F5D8C;
  margin-bottom: 0.5rem;
  font-weight: 600;
}

.removed-list {
  display: flex;
  flex-wrap: wrap;
  gap: 0.5rem;
}

.removed-item-wrapper {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  background-color: #FFFFFF;
  color: #6B6B6B;
  padding: 0.25rem 0.75rem;
  border-radius: 4px;
  font-size: 0.85rem;
  font-weight: 500;
  border: 1px solid #E0E0DC;
}

.removed-item {
  flex: 1;
}

.btn-restore-ingredient {
  background-color: #2F5D8C;
  color: white;
  border: none;
  border-radius: 50%;
  width: 20px;
  height: 20px;
  display: flex;
  align-items: center;
  justify-content: center;
  cursor: pointer;
  font-size: 1rem;
  line-height: 1;
  transition: all 0.2s;
  flex-shrink: 0;
  padding: 0;
}

.btn-restore-ingredient:hover {
  background-color: #254A70;
  transform: scale(1.1);
}

.no-ingredients {
  color: #6B6B6B;
  font-style: italic;
  padding: 1rem;
  text-align: center;
}

.price-section {
  border-top: 2px solid #E0E0DC;
  padding-top: 1.5rem;
  margin-top: 1.5rem;
}

.price-display {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 1rem;
  background: #E3B23C;
  border-radius: 8px;
  color: #2B2B2B;
  border: 1px solid #C99A2E;
}

.price-label {
  font-size: 1.2rem;
  font-weight: 500;
}

.price-value {
  font-size: 2rem;
  font-weight: bold;
}

.modal-footer {
  padding: 1.5rem 2rem 2rem 2rem;
  border-top: 1px solid #E0E0DC;
  margin-top: 1rem;
}

.modal-cart-controls {
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 1rem;
}

.btn-quantity-large {
  width: 48px;
  height: 48px;
  font-size: 1.5rem;
  border: 2px solid #E0E0DC;
  background: #FFFFFF;
  color: #2B2B2B;
  border-radius: 8px;
  cursor: pointer;
  display: flex;
  align-items: center;
  justify-content: center;
  transition: all 0.2s;
  font-weight: bold;
}

.btn-quantity-large:hover {
  background-color: #F5F5F2;
  border-color: #E3B23C;
}

.btn-quantity-large.btn-add {
  background: #E3B23C;
  border-color: #C99A2E;
  color: #2B2B2B;
  font-weight: 600;
}

.btn-quantity-large.btn-add:hover {
  background: #C99A2E;
  border-color: #B88A26;
}

.quantity-display-large {
  font-size: 1.5rem;
  font-weight: bold;
  color: #2B2B2B;
  min-width: 60px;
  text-align: center;
}

.btn {
  padding: 0.75rem 2rem;
  border: none;
  border-radius: 6px;
  cursor: pointer;
  font-size: 1.1rem;
  font-weight: 500;
  transition: all 0.3s;
  width: 100%;
}

.btn-success {
  background-color: #28a745;
  color: white;
}

.btn-success:hover {
  background-color: #218838;
  transform: translateY(-2px);
  box-shadow: 0 4px 8px rgba(40, 167, 69, 0.3);
}

.btn-success:active {
  transform: translateY(0);
}

.modal-loading {
  padding: 4rem 2rem;
  text-align: center;
  color: #7f8c8d;
}

@media (max-width: 768px) {
  .modal-content {
    max-width: 95%;
    margin: 1rem;
  }
  
  .modal-title {
    font-size: 1.5rem;
    margin: 1.5rem 1.5rem 1rem 1.5rem;
  }
  
  .modal-body {
    padding: 0 1.5rem;
  }
  
  .modal-footer {
    padding: 1rem 1.5rem 1.5rem 1.5rem;
  }
  
  .price-value {
    font-size: 1.5rem;
  }
}
</style>

