<template>
  <span class="order-status" :class="statusClass">
    {{ statusText }}
  </span>
</template>

<script setup>
import { computed } from 'vue'
import { getStatusText, getStatusClass } from '@/utils/statusHelpers'

const props = defineProps({
  status: {
    type: String,
    required: true
  }
})

const statusText = computed(() => getStatusText(props.status))
const statusClass = computed(() => getStatusClass(props.status))
</script>

<style scoped>
.order-status {
  display: inline-block;
  padding: 0.25rem 0.75rem;
  border-radius: 4px;
  font-size: 0.85rem;
  font-weight: 600;
  width: fit-content;
}

.status-pending {
  background-color: #FFF4E6;
  color: #E3B23C;
}

.status-processing {
  background-color: #E6F2FF;
  color: #2F5D8C;
}

.status-completed {
  background-color: #E6F7E6;
  color: #2F5D8C;
}

.status-cancelled {
  background-color: #FFE0E0;
  color: #B33A2B;
}
</style>

