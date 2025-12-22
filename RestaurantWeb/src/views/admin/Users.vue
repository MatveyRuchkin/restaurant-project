<template>
  <div class="users-page">
    <div class="page-header">
      <h2>Управление пользователями</h2>
      <button @click="showCreateModal = true" class="btn btn-primary">Добавить пользователя</button>
    </div>

    <div v-if="loading" class="loading">Загрузка...</div>
    <div v-else-if="error" class="error">{{ error }}</div>
    <div v-else>
      <!-- Фильтры -->
      <div class="filters">
        <div class="filter-group">
          <label for="searchFilter">Поиск по имени:</label>
          <input id="searchFilter" v-model="filters.search" type="text" placeholder="Имя пользователя..." class="form-control" />
        </div>
        <div class="filter-group">
          <label for="roleFilter">Роль:</label>
          <select id="roleFilter" v-model="filters.roleId" class="form-control">
            <option value="">Все</option>
            <option v-for="role in roles" :key="role.id" :value="role.id">{{ role.name }}</option>
          </select>
        </div>
        <div class="filter-group">
          <button @click="applyFilters" class="btn btn-primary btn-sm">Применить</button>
          <button @click="clearFilters" class="btn btn-secondary btn-sm">Сбросить</button>
        </div>
      </div>
      
      <div class="users-table">
        <table>
          <thead>
            <tr>
              <th>Имя пользователя</th>
              <th>Роль</th>
              <th>Действия</th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="user in users" :key="user.id">
              <td>{{ user.username }}</td>
              <td>
                <span class="role-badge" :class="getRoleClass(user.roleName)">{{ user.roleName }}</span>
              </td>
              <td>
                <button @click="editUser(user)" class="btn btn-primary btn-sm">Изменить роль</button>
                <button @click="deleteUser(user.id)" class="btn btn-danger btn-sm">Удалить</button>
              </td>
            </tr>
          </tbody>
        </table>
      </div>
    </div>

    <!-- Модальное окно создания/редактирования -->
    <div v-if="showCreateModal || editingUser" class="modal-overlay" @click="closeModal">
      <div class="modal-content" @click.stop>
        <h3>{{ editingUser ? 'Изменить роль пользователя' : 'Создать пользователя' }}</h3>
        <form @submit.prevent="saveUser">
          <div class="form-group">
            <label>Имя пользователя *</label>
            <input v-model="userForm.username" type="text" class="form-control" required :disabled="!!editingUser" />
          </div>
          <div v-if="!editingUser" class="form-group">
            <label>Пароль *</label>
            <input v-model="userForm.password" type="password" class="form-control" required />
          </div>
          <div class="form-group">
            <label>Роль *</label>
            <select v-model="userForm.roleId" class="form-control" required>
              <option value="">Выберите роль</option>
              <option v-for="role in roles" :key="role.id" :value="role.id">{{ role.name }}</option>
            </select>
          </div>
          <div class="modal-actions">
            <button type="submit" class="btn btn-success" :disabled="saving">{{ saving ? 'Сохранение...' : 'Сохранить' }}</button>
            <button type="button" @click="closeModal" class="btn btn-secondary">Отмена</button>
          </div>
        </form>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, onMounted } from 'vue'
import { usersApi } from '@/api/users'
import { rolesApi } from '@/api/roles'
import { ROLES } from '@/constants'

const users = ref([])
const roles = ref([])
const loading = ref(false)
const error = ref('')
const showCreateModal = ref(false)
const editingUser = ref(null)
const saving = ref(false)

const userForm = ref({
  username: '',
  password: '',
  roleId: ''
})

const filters = ref({
  roleId: '',
  search: ''
})

const loadUsers = async () => {
  loading.value = true
  error.value = ''
  try {
    const params = { pageSize: 1000 }
    if (filters.value.roleId) {
      params.roleId = filters.value.roleId
    }
    if (filters.value.search) {
      params.search = filters.value.search
    }
    const response = await usersApi.getAll(params)
    users.value = response.data || response
  } catch (err) {
    error.value = 'Ошибка загрузки пользователей'
    console.error(err)
  } finally {
    loading.value = false
  }
}

const applyFilters = () => {
  loadUsers()
}

const clearFilters = () => {
  filters.value = { roleId: '', search: '' }
  loadUsers()
}

const loadRoles = async () => {
  try {
    const data = await rolesApi.getAll()
    roles.value = Array.isArray(data) ? data : (data.data || [])
  } catch (err) {
    console.error('Ошибка загрузки ролей:', err)
  }
}

const editUser = (user) => {
  editingUser.value = user
  const role = roles.value.find(r => r.name === user.roleName)
  userForm.value = {
    username: user.username,
    password: '',
    roleId: role?.id || ''
  }
}

const deleteUser = async (id) => {
  if (!confirm('Вы уверены, что хотите удалить этого пользователя?')) return
  
  try {
    await usersApi.delete(id)
    await loadUsers()
  } catch (err) {
    error.value = 'Ошибка удаления пользователя'
    console.error(err)
  }
}

const saveUser = async () => {
  saving.value = true
  try {
    if (editingUser.value) {
      await usersApi.update(editingUser.value.id, { roleId: userForm.value.roleId })
    } else {
      await usersApi.create(userForm.value)
    }
    await loadUsers()
    closeModal()
  } catch (err) {
    error.value = err.response?.data?.message || 'Ошибка сохранения пользователя'
    console.error(err)
  } finally {
    saving.value = false
  }
}

const closeModal = () => {
  showCreateModal.value = false
  editingUser.value = null
  userForm.value = {
    username: '',
    password: '',
    roleId: ''
  }
}

const getRoleClass = (roleName) => {
  const roleMap = {
    'Admin': 'role-admin',
    'Waiter': 'role-waiter',
    'User': 'role-user'
  }
  return roleMap[roleName] || ''
}

onMounted(() => {
  loadUsers()
  loadRoles()
})
</script>

<style scoped>
.users-page {
  padding: 2rem 0;
}

.page-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 2rem;
  position: sticky;
  top: 0;
  background: #F5F5F2;
  padding: 1rem 0;
  z-index: 100;
}

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

.users-table {
  background: #FFFFFF;
  border-radius: 8px;
  overflow: hidden;
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.05);
}

table {
  width: 100%;
  border-collapse: collapse;
}

thead {
  background: #2F5D8C;
  color: white;
}

th, td {
  padding: 1rem;
  text-align: left;
  border-bottom: 1px solid #E0E0DC;
}

tbody tr:hover {
  background: #F9F9F9;
}

.role-badge {
  padding: 0.25rem 0.75rem;
  border-radius: 4px;
  font-size: 0.85rem;
  font-weight: 600;
}

.role-admin {
  background: #FFE0E0;
  color: #B33A2B;
}

.role-waiter {
  background: #E6F2FF;
  color: #2F5D8C;
}

.role-user {
  background: #F5F5F2;
  color: #6B6B6B;
}

.modal-overlay {
  position: fixed;
  top: 0;
  left: 0;
  right: 0;
  bottom: 0;
  background: rgba(0, 0, 0, 0.5);
  display: flex;
  align-items: center;
  justify-content: center;
  z-index: 1000;
}

.modal-content {
  background: #FFFFFF;
  border-radius: 8px;
  padding: 2rem;
  width: 90%;
  max-width: 500px;
  max-height: 90vh;
  overflow-y: auto;
}

.modal-content h3 {
  color: #2F5D8C;
  margin-bottom: 1.5rem;
}

.form-group {
  margin-bottom: 1rem;
}

.form-group label {
  display: block;
  margin-bottom: 0.5rem;
  color: #2B2B2B;
  font-weight: 500;
}

.modal-actions {
  display: flex;
  gap: 1rem;
  margin-top: 1.5rem;
}

.btn-secondary {
  background: #6B6B6B;
  color: white;
}

.btn-secondary:hover {
  background: #555;
}
</style>

