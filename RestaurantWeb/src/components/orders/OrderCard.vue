<template>
  <div 
    class="order-card"
    :class="{ expanded: isExpanded }"
  >
    <div class="order-header" @click.stop="handleToggle">
      <div class="order-info">
        <div class="order-number">
          <strong>Заказ №{{ formatOrderNumber(order.id, order.createdAt || order.orderDate) }}</strong>
        </div>
        <div v-if="showUser && order.username" class="order-user">
          Клиент: {{ order.username }}
        </div>
        <div class="order-date">
          {{ formatDate(order.createdAt || order.orderDate) }}
        </div>
        <StatusBadge :status="order.status" />
      </div>
      <div class="order-total">
        {{ formatCurrency(order.total) }}
      </div>
      <div class="expand-icon">
        {{ isExpanded ? '▼' : '▶' }}
      </div>
    </div>

    <div v-if="isExpanded" class="order-details">
      <slot name="details" :order="order" />
    </div>
  </div>
</template>

<script setup>
import StatusBadge from './StatusBadge.vue'
import { formatCurrency, formatDate, formatOrderNumber } from '@/utils/formatters'

const props = defineProps({
  order: {
    type: Object,
    required: true
  },
  isExpanded: {
    type: Boolean,
    default: false
  },
  showUser: {
    type: Boolean,
    default: false
  }
})

const emit = defineEmits(['toggle'])

const handleToggle = (event) => {
  if (event) {
    event.preventDefault()
    event.stopPropagation()
  }
  emit('toggle')
}
</script>

<style scoped>
.order-card {
  background: #FFFFFF;
  border-radius: 8px;
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.05);
  border: 1px solid #E0E0DC;
  overflow: hidden;
  transition: box-shadow 0.3s;
}

.order-card:hover {
  box-shadow: 0 4px 12px rgba(0, 0, 0, 0.1);
}

.order-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 1.5rem;
  cursor: pointer;
  transition: background-color 0.2s;
}

.order-header:hover {
  background-color: #F9F9F9;
}

.order-info {
  display: flex;
  flex-direction: column;
  gap: 0.5rem;
  flex: 1;
}

.order-number {
  font-size: 1.2rem;
  color: #2B2B2B;
}

.order-user {
  font-size: 0.95rem;
  color: #6B6B6B;
  font-weight: 500;
}

.order-date {
  font-size: 0.9rem;
  color: #6B6B6B;
}

.order-total {
  font-size: 1.5rem;
  font-weight: bold;
  color: #2F5D8C;
  margin: 0 1rem;
}

.expand-icon {
  font-size: 0.9rem;
  color: #6B6B6B;
  transition: transform 0.3s;
}

.order-details {
  padding: 1.5rem;
  border-top: 1px solid #E0E0DC;
  background-color: #F9F9F9;
}

@media (max-width: 768px) {
  .order-header {
    flex-direction: column;
    align-items: flex-start;
    gap: 1rem;
  }

  .order-total {
    margin: 0;
    align-self: flex-end;
  }

  .expand-icon {
    position: absolute;
    top: 1.5rem;
    right: 1.5rem;
  }
}
</style>

