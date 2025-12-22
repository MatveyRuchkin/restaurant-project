<template>
  <div class="page-header">
    <h2>{{ title }}</h2>
    <slot name="actions" />
  </div>
</template>

<script setup>
defineProps({
  title: {
    type: String,
    required: true
  }
})
</script>

<style scoped>
.page-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 2rem;
}

.page-header h2 {
  color: #2F5D8C;
  margin: 0;
}
</style>

