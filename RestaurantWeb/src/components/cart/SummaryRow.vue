<template>
  <div class="summary-row" :class="{ total: isTotal }">
    <span>{{ label }}</span>
    <span>{{ value }}</span>
  </div>
</template>

<script setup>
defineProps({
  label: {
    type: String,
    required: true
  },
  value: {
    type: String,
    required: true
  },
  isTotal: {
    type: Boolean,
    default: false
  }
})
</script>

<style scoped>
.summary-row {
  display: flex;
  justify-content: space-between;
  padding: 0.5rem 0;
  color: #6B6B6B;
}

.summary-row.total {
  border-top: 2px solid #E3B23C;
  margin-top: 0.5rem;
  padding-top: 0.5rem;
  padding-bottom: 1rem;
  font-size: 1.3rem;
  font-weight: bold;
  color: #2B2B2B;
  margin-bottom: 0;
}
</style>

