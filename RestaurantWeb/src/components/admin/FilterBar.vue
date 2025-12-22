<template>
  <div class="filters">
    <slot />
    <div v-if="showReset" class="filter-group">
      <button @click="$emit('reset')" class="btn btn-secondary btn-sm">Сбросить</button>
    </div>
  </div>
</template>

<script setup>
defineProps({
  showReset: {
    type: Boolean,
    default: true
  }
})

defineEmits(['reset'])
</script>

<style scoped>
.filters {
  background: #FFFFFF;
  padding: 1.5rem;
  border-radius: 8px;
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.05);
  margin-bottom: 2rem;
  border: 1px solid #E0E0DC;
  display: flex;
  flex-wrap: wrap;
  gap: 1rem;
  align-items: flex-end;
}

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

.filter-group .form-control {
  min-width: 200px;
}
</style>

