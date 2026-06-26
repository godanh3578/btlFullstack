<script setup>
import { openPage } from '../composables/useAppState'
</script>

<template>
  <main class="not-found-page">
    <div class="not-found-inner">
      <div class="not-found-code">404</div>
      <div class="not-found-art">
        <svg viewBox="0 0 200 120" fill="none" xmlns="http://www.w3.org/2000/svg">
          <rect x="20" y="30" width="160" height="70" rx="8" fill="#1a2035" stroke="rgba(201,168,76,0.3)" stroke-width="1.5"/>
          <rect x="35" y="48" width="60" height="8" rx="4" fill="rgba(201,168,76,0.25)"/>
          <rect x="35" y="62" width="90" height="6" rx="3" fill="rgba(255,255,255,0.08)"/>
          <rect x="35" y="74" width="70" height="6" rx="3" fill="rgba(255,255,255,0.08)"/>
          <circle cx="150" cy="58" r="16" fill="rgba(201,168,76,0.12)" stroke="rgba(201,168,76,0.4)" stroke-width="1.5"/>
          <line x1="144" y1="52" x2="156" y2="64" stroke="rgba(201,168,76,0.7)" stroke-width="2.5" stroke-linecap="round"/>
          <line x1="156" y1="52" x2="144" y2="64" stroke="rgba(201,168,76,0.7)" stroke-width="2.5" stroke-linecap="round"/>
        </svg>
      </div>
      <h1>Trang không tồn tại</h1>
      <p>Đường dẫn bạn truy cập không hợp lệ hoặc đã bị xóa.</p>
      <div class="not-found-actions">
        <button class="primary-btn" type="button" @click="openPage('shop')">Về trang chủ</button>
        <button class="ghost-btn" type="button" @click="$router.go(-1)">Quay lại</button>
      </div>
    </div>
  </main>
</template>
