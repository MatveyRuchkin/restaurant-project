<template>
  <div class="home">
    <div class="container">
      <div class="logo-container">
        <img src="/logo.png" alt="Los Pollos Hermanos" class="main-logo" />
      </div>
      <h1>Добро пожаловать в Los Pollos Hermanos!</h1>
      <p class="subtitle">Лучшая курица в городе. Семейный рецепт с 1986 года.</p>
      <router-link to="/menu" class="btn btn-primary btn-large">
        Посмотреть меню
      </router-link>
    </div>
  </div>
</template>

<style scoped>
.home {
  padding: 2rem;
  text-align: center;
  height: calc(100vh - 200px);
  display: flex;
  align-items: center;
  justify-content: center;
  background: #F5F5F2;
  overflow: hidden;
}

.container {
  max-width: 800px;
  width: 100%;
}

.logo-container {
  margin-bottom: 2rem;
  display: flex;
  justify-content: center;
  align-items: center;
}

.main-logo {
  max-width: 400px;
  width: 100%;
  height: auto;
  filter: drop-shadow(0 4px 8px rgba(0, 0, 0, 0.3));
  animation: fadeInUp 0.8s ease-out;
  pointer-events: none;
}

@keyframes fadeInUp {
  from {
    opacity: 0;
    transform: translateY(20px);
  }
  to {
    opacity: 1;
    transform: translateY(0);
  }
}

.home h1 {
  font-size: 2.5rem;
  margin-bottom: 1rem;
  color: #2B2B2B;
  font-weight: bold;
  animation: fadeInUp 0.8s ease-out 0.2s both;
}

.subtitle {
  font-size: 1.25rem;
  color: #6B6B6B;
  margin-bottom: 2.5rem;
  font-weight: 500;
  animation: fadeInUp 0.8s ease-out 0.4s both;
}

.btn-large {
  padding: 1rem 2rem;
  font-size: 1.25rem;
  animation: fadeInUp 0.8s ease-out 0.6s both;
}

@media (max-width: 768px) {
  .home h1 {
    font-size: 2rem;
  }
  
  .main-logo {
    max-width: 300px;
  }
  
  .subtitle {
    font-size: 1.1rem;
  }
}
</style>

