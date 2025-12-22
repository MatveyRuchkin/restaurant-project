<template>
  <div class="order-item">
    <div class="item-info">
      <span class="item-name">{{ item.dishName }}</span>
      <span class="item-quantity">× {{ item.quantity }}</span>
      <span v-if="item.notes" class="item-notes">{{ item.notes }}</span>
    </div>
    <div class="item-price">
      {{ formatCurrency(item.price * item.quantity) }}
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
</script>

<style scoped>
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
</style>

