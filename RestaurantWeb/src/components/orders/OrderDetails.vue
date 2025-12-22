<template>
  <div>
    <div v-if="order.notes" class="order-notes">
      <strong>Комментарий к заказу:</strong> {{ order.notes }}
    </div>
    
    <div v-if="loading" class="loading-items">
      Загрузка деталей...
    </div>
    
    <div v-else-if="items && items.length > 0" class="order-items">
      <h3>Состав заказа:</h3>
      <OrderItem 
        v-for="item in items" 
        :key="item.id" 
        :item="item"
      />
    </div>

    <slot name="actions" :order="order" />
  </div>
</template>

<script setup>
import OrderItem from './OrderItem.vue'

defineProps({
  order: {
    type: Object,
    required: true
  },
  items: {
    type: Array,
    default: () => []
  },
  loading: {
    type: Boolean,
    default: false
  }
})
</script>

<style scoped>
.order-notes {
  padding: 1rem;
  background: #FFFFFF;
  border-radius: 4px;
  margin-bottom: 1rem;
  color: #2B2B2B;
  border-left: 3px solid #E3B23C;
}

.order-notes strong {
  color: #2F5D8C;
}

.loading-items {
  text-align: center;
  padding: 1rem;
  color: #6B6B6B;
}

.order-items {
  margin-top: 1rem;
  margin-bottom: 1rem;
}

.order-items h3 {
  color: #2F5D8C;
  font-size: 1.1rem;
  margin-bottom: 1rem;
}
</style>

