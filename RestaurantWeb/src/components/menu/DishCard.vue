<template>
  <div 
    class="dish-card"
    @click="$emit('click')"
  >
    <div class="dish-card-body">
      <h3 class="dish-name">{{ dish.name }}</h3>
      <p v-if="dish.description" class="dish-description">{{ dish.description }}</p>
      <div class="dish-footer" @click.stop>
        <span class="dish-price">{{ formatCurrency(dish.price) }}</span>
        <div class="cart-controls">
          <template v-if="dish.cartQuantity > 0">
            <button 
              @click="$emit('decrease')" 
              class="btn-quantity"
            >
              −
            </button>
            <span class="quantity-display">
              {{ dish.cartQuantity }}
            </span>
            <button 
              @click="$emit('increase')" 
              class="btn-quantity btn-add"
            >
              +
            </button>
          </template>
          <button 
            v-else
            @click="$emit('add')" 
            class="btn btn-success"
          >
            В корзину
          </button>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
import { formatCurrency } from '@/utils/formatters'

defineProps({
  dish: {
    type: Object,
    required: true
  }
})

defineEmits(['click', 'add', 'increase', 'decrease'])
</script>

<style scoped>
.dish-card {
  background: #FFFFFF;
  border-radius: 8px;
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.1);
  transition: transform 0.2s, box-shadow 0.2s;
  overflow: hidden;
  cursor: pointer;
  border: 1px solid #E0E0DC;
}

.dish-card:hover {
  transform: translateY(-4px);
  box-shadow: 0 4px 12px rgba(0, 0, 0, 0.15);
  border-color: #E3B23C;
}

.dish-card-body {
  padding: 1.5rem;
  display: flex;
  flex-direction: column;
  height: 100%;
}

.dish-name {
  font-size: 1.5rem;
  color: #2B2B2B;
  margin-bottom: 0.5rem;
  font-weight: 600;
}

.dish-description {
  color: #6B6B6B;
  margin-bottom: 1rem;
  flex-grow: 1;
  line-height: 1.5;
}

.dish-footer {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-top: auto;
  padding-top: 1rem;
  border-top: 1px solid #E0E0DC;
}

.dish-price {
  font-size: 1.5rem;
  font-weight: bold;
  color: #2F5D8C;
}

.cart-controls {
  display: flex;
  align-items: center;
  gap: 0.5rem;
}

.btn-success {
  padding: 0.5rem 1rem;
  border: none;
  border-radius: 4px;
  cursor: pointer;
  font-size: 1rem;
  transition: all 0.3s;
  background-color: #28a745;
  color: white;
}

.btn-success:hover {
  background-color: #218838;
}

.btn-success:active {
  transform: scale(0.95);
}

.btn-quantity {
  width: 32px;
  height: 32px;
  border: 1px solid #E0E0DC;
  background: #FFFFFF;
  border-radius: 4px;
  cursor: pointer;
  font-size: 1.2rem;
  display: flex;
  align-items: center;
  justify-content: center;
  transition: all 0.2s;
  font-weight: bold;
  color: #2B2B2B;
}

.btn-quantity:hover {
  background-color: #F5F5F2;
  border-color: #E3B23C;
}

.btn-quantity.btn-add {
  background: #E3B23C;
  border-color: #C99A2E;
  color: #2B2B2B;
  font-weight: 600;
}

.btn-quantity.btn-add:hover {
  background: #C99A2E;
  border-color: #B88A26;
}

.quantity-display {
  min-width: 30px;
  text-align: center;
  font-weight: 600;
  color: #2c3e50;
  font-size: 1.1rem;
}
</style>

