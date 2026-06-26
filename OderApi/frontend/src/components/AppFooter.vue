<script setup>
import { openPage } from '../composables/useAppState'
</script>

<template>
  <footer class="site-footer">
    <div class="footer-inner">
      <div class="footer-brand">
        <span class="footer-logo">ORIVEX</span>
        <p>Order &amp; Sales — Nhóm 2</p>
      </div>
      <div class="footer-links">
        <span @click="openPage('shop')" class="footer-link">Cửa hàng</span>
        <span @click="openPage('lookup')" class="footer-link">Tra cứu đơn</span>
        <span @click="openPage('account')" class="footer-link">Tài khoản</span>
      </div>
      <div class="footer-info">
        <p>Hỗ trợ: <a href="mailto:support@orivex.local">support@orivex.local</a></p>
        <p class="footer-copy">&copy; 2026 ORIVEX — Nhóm 2</p>
      </div>
    </div>
  </footer>
</template>
