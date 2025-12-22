<template>
  <div class="password-input-wrapper">
    <input 
      :value="modelValue"
      @input="$emit('update:modelValue', $event.target.value)"
      :type="showPassword ? 'text' : 'password'"
      class="form-control" 
      :required="required"
      :placeholder="placeholder"
    />
    <button 
      type="button" 
      class="password-toggle"
      @click="showPassword = !showPassword"
      :aria-label="showPassword ? 'Скрыть пароль' : 'Показать пароль'"
    >
      <i v-if="showPassword" class="fas fa-eye eye-icon"></i>
      <i v-else class="fas fa-eye-slash eye-icon"></i>
    </button>
  </div>
</template>

<script setup>
import { ref } from 'vue'

defineProps({
  modelValue: {
    type: String,
    default: ''
  },
  required: {
    type: Boolean,
    default: false
  },
  placeholder: {
    type: String,
    default: 'Введите пароль'
  }
})

defineEmits(['update:modelValue'])

const showPassword = ref(false)
</script>

<style scoped>
.password-input-wrapper {
  position: relative;
  display: flex;
  align-items: center;
}

.password-input-wrapper .form-control {
  padding-right: 40px;
}

.password-toggle {
  position: absolute;
  right: 10px;
  background: none;
  border: none;
  cursor: pointer;
  font-size: 1.2rem;
  padding: 0;
  width: 30px;
  height: 30px;
  display: flex;
  align-items: center;
  justify-content: center;
}

.password-toggle:hover {
  opacity: 0.7;
}

.eye-icon {
  display: inline-block;
  font-size: 1.1rem;
  color: #6B6B6B;
  transition: color 0.2s;
}

.password-toggle:hover .eye-icon {
  color: #2F5D8C;
}
</style>

