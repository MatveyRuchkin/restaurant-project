<template>
  <div v-if="menus.length > 0" class="menu-selector">
    <button
      v-for="menu in menus"
      :key="menu.id"
      @click="$emit('select', menu.id)"
      :class="['menu-btn', { active: selectedMenuId === menu.id }]"
    >
      {{ menu.name }}
    </button>
  </div>
  <div v-else class="empty-menu">
    <p>Меню не найдены. Обратитесь к администратору.</p>
  </div>
</template>

<script setup>
defineProps({
  menus: {
    type: Array,
    default: () => []
  },
  selectedMenuId: {
    type: [String, Number],
    default: null
  }
})

defineEmits(['select'])
</script>

<style scoped>
.menu-selector {
  display: flex;
  gap: 1rem;
  margin-bottom: 2rem;
  flex-wrap: wrap;
  padding-bottom: 1.5rem;
  border-bottom: 2px solid #e0e0e0;
}

.menu-btn {
  padding: 0.75rem 1.5rem;
  border: 1px solid #E0E0DC;
  background: #FFFFFF;
  color: #2B2B2B;
  border-radius: 8px;
  cursor: pointer;
  font-size: 1rem;
  font-weight: 500;
  transition: all 0.3s;
}

.menu-btn:hover {
  background: #F5F5F2;
  transform: translateY(-1px);
  box-shadow: 0 2px 4px rgba(0, 0, 0, 0.1);
  border-color: #E3B23C;
}

.menu-btn.active {
  background: #2F5D8C;
  color: white;
  box-shadow: 0 2px 6px rgba(0, 0, 0, 0.15);
  border-color: #254A70;
  font-weight: 600;
}

.empty-menu {
  text-align: center;
  padding: 2rem;
  color: #6B6B6B;
}

@media (max-width: 768px) {
  .menu-selector {
    flex-direction: column;
  }

  .menu-btn {
    width: 100%;
  }
}
</style>

