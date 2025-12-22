<template>
  <div v-if="isVisible" class="cart-drawer">
    <div class="cart-drawer-header">
      <h2>Корзина</h2>
    </div>
      
      <div class="cart-drawer-content">
        <div v-if="cartStore.isEmpty" class="empty-cart">
          <p class="empty-cart-text">Корзина пуста</p>
        </div>
        
        <div v-else>
          <div class="cart-items">
            <CartItem
              v-for="(item, index) in cartStore.items"
              :key="`${item.id || item.dishId}-${item.notes || 'default'}-${index}`"
              :item="item"
              @increase="handleIncrease(item)"
              @decrease="handleDecrease(item)"
              @remove="handleRemove(item)"
            />
          </div>
          
          <div class="cart-summary">
            <SummaryRow
              label="Всего товаров:"
              :value="cartStore.totalItems.toString()"
            />
            <SummaryRow
              label="Общая сумма:"
              :value="formatCurrency(cartStore.totalPrice)"
              is-total
            />
            <router-link 
              to="/checkout" 
              class="btn btn-success btn-large"
            >
              Оформить заказ
            </router-link>
          </div>
        </div>
      </div>
  </div>
</template>

<script setup>
import { computed } from 'vue'
import { useRoute } from 'vue-router'
import { useCartStore } from '@/stores/cart'
import { useAuthStore } from '@/stores/auth'
import CartItem from './CartItem.vue'
import SummaryRow from './SummaryRow.vue'
import { formatCurrency } from '@/utils/formatters'

const cartStore = useCartStore()
const authStore = useAuthStore()
const route = useRoute()

// Корзина видна только для авторизованных пользователей
const isVisible = computed(() => {
  return authStore.isAuthenticated
})

const handleIncrease = async (item) => {
  await cartStore.updateQuantity(item.dishId, item.quantity + 1, item.notes)
}

const handleDecrease = async (item) => {
  await cartStore.updateQuantity(item.dishId, item.quantity - 1, item.notes)
}

const handleRemove = async (item) => {
  await cartStore.removeItem(item.dishId, item.notes)
}
</script>

<style scoped>
.cart-drawer {
  width: 500px;
  height: 100%;
  background: #F5F5F2;
  display: flex;
  flex-direction: column;
  border-left: 2px solid #E0E0DC;
  overflow: hidden;
  flex-shrink: 0;
}

@media (max-width: 1024px) {
  .cart-drawer {
    width: 400px;
  }
}

@media (max-width: 768px) {
  .cart-drawer {
    width: 100%;
    height: auto;
    border-left: none;
    border-top: 2px solid #E0E0DC;
  }
}

.cart-drawer-header {
  background: #2F5D8C;
  color: white;
  padding: 1.5rem;
  display: flex;
  justify-content: space-between;
  align-items: center;
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.1);
}

.cart-drawer-header h2 {
  margin: 0;
  font-size: 1.5rem;
  font-weight: 600;
}

.cart-drawer-content {
  flex: 1;
  overflow-y: auto;
  padding: 1.5rem;
  display: flex;
  flex-direction: column;
  min-height: 0;
}

.empty-cart {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  height: 100%;
  text-align: center;
  gap: 1.5rem;
}

.empty-cart-text {
  font-size: 1.25rem;
  color: #7f8c8d;
  margin: 0;
}

.cart-items {
  flex: 1;
  overflow-y: auto;
  margin-bottom: 1.5rem;
}

.cart-summary {
  background: #FFFFFF;
  padding: 1.5rem;
  border-radius: 8px;
  box-shadow: 0 2px 4px rgba(0, 0, 0, 0.1);
  border: 1px solid #E0E0DC;
  position: sticky;
  bottom: 0;
}

.btn-large {
  width: 100%;
  margin-top: 1rem;
  padding: 1rem;
  font-size: 1.2rem;
  text-align: center;
  text-decoration: none;
  display: block;
}

/* Скроллбар */
.cart-items::-webkit-scrollbar {
  width: 8px;
}

.cart-items::-webkit-scrollbar-track {
  background: #F5F5F2;
}

.cart-items::-webkit-scrollbar-thumb {
  background: #E0E0DC;
  border-radius: 4px;
}

.cart-items::-webkit-scrollbar-thumb:hover {
  background: #C0C0C0;
}
</style>

