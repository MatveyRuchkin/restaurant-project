<template>
  <div class="cart-page">
    <div class="container">
      <h1>Корзина</h1>
      <div v-if="cartStore.isEmpty" class="empty-cart">
        <p class="empty-cart-text">Корзина пуста</p>
        <router-link to="/menu" class="btn btn-primary btn-empty-cart">Перейти к меню</router-link>
      </div>
      <div v-else>
        <div class="cart-items">
          <CartItem
            v-for="(item, index) in cartStore.items"
            :key="`${item.dishId}-${item.notes || 'default'}-${index}`"
            :item="item"
            @increase="cartStore.updateQuantity(item.dishId, item.quantity + 1, item.notes)"
            @decrease="cartStore.updateQuantity(item.dishId, item.quantity - 1, item.notes)"
            @remove="handleRemoveItem(item)"
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
          <router-link to="/checkout" class="btn btn-success btn-large">
            Оформить заказ
          </router-link>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
import { useCartStore } from '@/stores/cart'
import CartItem from '@/components/cart/CartItem.vue'
import SummaryRow from '@/components/cart/SummaryRow.vue'
import { formatCurrency } from '@/utils/formatters'

const cartStore = useCartStore()

const handleRemoveItem = (item) => {
  console.log('Удаление элемента:', item)
  const notes = item.notes || null
  console.log('Notes:', notes)
  cartStore.removeItem(item.dishId, notes)
  console.log('Корзина после удаления:', cartStore.items)
}
</script>

<style scoped>
.cart-page {
  padding: 2rem;
  background: #F5F5F2;
  min-height: calc(100vh - 200px);
}

.cart-page h1 {
  color: #2B2B2B;
}

.empty-cart {
  text-align: center;
  padding: 4rem 2rem;
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 2rem;
}

.empty-cart-text {
  font-size: 1.5rem;
  color: #7f8c8d;
  margin: 0;
}

.btn-empty-cart {
  padding: 1rem 2rem;
  font-size: 1.1rem;
}

.cart-items {
  margin-bottom: 2rem;
}

.cart-summary {
  background: #FFFFFF;
  padding: 2rem;
  border-radius: 8px;
  box-shadow: 0 2px 4px rgba(0, 0, 0, 0.1);
  border: 1px solid #E0E0DC;
}

.btn-large {
  width: 100%;
  margin-top: 0;
  padding: 1rem;
  font-size: 1.2rem;
}
</style>

