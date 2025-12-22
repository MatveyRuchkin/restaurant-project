<template>
  <div class="category-section">
    <h2 class="category-title">{{ category.name }}</h2>
    <div class="dishes-grid">
      <slot />
    </div>
  </div>
</template>

<script setup>
defineProps({
  category: {
    type: Object,
    required: true
  }
})
</script>

<style scoped>
.category-section {
  margin-bottom: 3rem;
}

.category-title {
  font-size: 2rem;
  color: #2F5D8C;
  margin-bottom: 1.5rem;
  padding-bottom: 0.5rem;
  border-bottom: 2px solid #E3B23C;
  font-weight: bold;
}

.dishes-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(280px, 1fr));
  gap: 1.5rem;
}

@media (max-width: 768px) {
  .dishes-grid {
    grid-template-columns: 1fr;
  }
}
</style>

