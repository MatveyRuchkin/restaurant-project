<template>
  <div class="admin-dashboard">
    <div class="container">
      <h1>Панель администратора</h1>
      <nav class="admin-nav">
        <router-link to="/admin/statistics" class="admin-nav-link">Статистика</router-link>
        <router-link to="/admin/dishes" class="admin-nav-link">Блюда</router-link>
        <router-link to="/admin/menus" class="admin-nav-link">Меню</router-link>
        <router-link to="/admin/ingredients" class="admin-nav-link">Ингредиенты</router-link>
        <router-link to="/admin/categories" class="admin-nav-link">Категории</router-link>
        <router-link to="/admin/users" class="admin-nav-link">Пользователи</router-link>
        <router-link to="/admin/orders" class="admin-nav-link">Заказы</router-link>
      </nav>
      <router-view />
    </div>
  </div>
</template>

<style scoped>
.admin-dashboard {
  padding: 2rem;
  background: #F5F5F2;
  min-height: calc(100vh - 200px);
}

.admin-dashboard h1 {
  color: #2B2B2B;
}

.admin-nav {
  display: flex;
  gap: 1rem;
  margin-bottom: 2rem;
  border-bottom: 2px solid #E3B23C;
  padding-bottom: 1rem;
}

.admin-nav-link {
  padding: 0.5rem 1rem;
  text-decoration: none;
  color: #2B2B2B;
  border-radius: 4px;
  transition: all 0.3s;
  border: 1px solid transparent;
  font-weight: 500;
}

.admin-nav-link:hover {
  background: #F5F5F2;
  border-color: #E3B23C;
}

.admin-nav-link.router-link-active {
  background: #2F5D8C;
  color: white;
  border-color: #254A70;
  font-weight: 600;
}
</style>

