<template>
  <div class="filter-group">
    <label v-if="label" :for="id">{{ label }}</label>
    <slot />
  </div>
</template>

<script setup>
defineProps({
  label: {
    type: String,
    default: ''
  },
  id: {
    type: String,
    default: ''
  }
})
</script>

<style scoped>
.filter-group {
  display: flex;
  flex-direction: column;
  gap: 0.5rem;
}

.filter-group label {
  font-weight: 600;
  color: #2B2B2B;
  font-size: 0.9rem;
}
</style>

