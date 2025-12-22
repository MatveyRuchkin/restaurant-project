<template>
  <div class="cart-item">
    <div class="cart-item-info">
      <h3 class="cart-item-name">{{ item.dishName }}</h3>
      <p v-if="item.notes" class="cart-item-notes">
        {{ item.notes }}
      </p>
      <div class="cart-item-controls">
        <button 
          @click="$emit('decrease')"
          class="btn-quantity"
        >
          −
        </button>
        <span class="quantity">{{ item.quantity }}</span>
        <button 
          @click="$emit('increase')"
          class="btn-quantity"
        >
          +
        </button>
        <button 
          @click="$emit('remove')"
          class="btn-remove"
        >
          Удалить
        </button>
      </div>
    </div>
    <div class="cart-item-price">
      {{ formatCurrency(item.subtotal || (item.dishPrice || item.price) * item.quantity) }}
    </div>
  </div>
</template>

<script setup>
import { formatCurrency } from '@/utils/formatters'

defineProps({
  item: {
    type: Object,
    required: true
  }
})

defineEmits(['increase', 'decrease', 'remove'])
</script>

<style scoped>
.cart-item {
  display: flex;
  justify-content: space-between;
  align-items: flex-start;
  padding: 1.5rem;
  background: #FFFFFF;
  border-radius: 8px;
  box-shadow: 0 2px 4px rgba(0, 0, 0, 0.1);
  margin-bottom: 1rem;
  border: 1px solid #E0E0DC;
}

.cart-item-info {
  flex: 1;
}

.cart-item-name {
  font-size: 1.25rem;
  color: #2B2B2B;
  margin-bottom: 0.5rem;
  font-weight: 600;
  word-wrap: break-word;
  overflow-wrap: break-word;
}

.cart-item-notes {
  font-size: 0.9rem;
  color: #856404;
  background-color: #fff3cd;
  padding: 0.5rem;
  border-radius: 4px;
  margin-bottom: 1rem;
  border-left: 3px solid #ffc107;
}

.cart-item-controls {
  display: flex;
  align-items: center;
  gap: 0.5rem;
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
  color: #2B2B2B;
}

.btn-quantity:hover {
  background-color: #F5F5F2;
  border-color: #E3B23C;
}

.quantity {
  min-width: 40px;
  text-align: center;
  font-weight: 500;
  color: #2B2B2B;
}

.btn-remove {
  padding: 0.5rem 1rem;
  background-color: #2F5D8C;
  color: white;
  border: none;
  border-radius: 4px;
  cursor: pointer;
  font-size: 0.9rem;
  transition: all 0.2s;
}

.btn-remove:hover {
  background-color: #254A70;
}

.cart-item-price {
  font-size: 1.5rem;
  font-weight: bold;
  color: #2F5D8C;
  margin-left: 1rem;
}
</style>

