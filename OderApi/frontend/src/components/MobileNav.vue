<script setup>
import { Icon } from '@iconify/vue'
import {
  activePage, cartCount, currentUser,
  openPage, openAuth
} from '../composables/useAppState'
</script>

<template>
  <nav class="mobile-nav" role="navigation" aria-label="Điều hướng chính">
    <button
      type="button"
      :class="['mobile-nav-item', { active: activePage === 'shop' }]"
      @click="openPage('shop')"
    >
      <Icon icon="mdi:home-outline" />
      <span>Trang chủ</span>
    </button>

    <button
      type="button"
      :class="['mobile-nav-item', { active: activePage === 'cart' }]"
      @click="openPage('cart')"
    >
      <span class="mobile-nav-icon-wrap">
        <Icon icon="mdi:cart-outline" />
        <span v-if="cartCount > 0" class="mobile-cart-badge">{{ cartCount > 9 ? '9+' : cartCount }}</span>
      </span>
      <span>Giỏ hàng</span>
    </button>

    <button
      type="button"
      :class="['mobile-nav-item', { active: activePage === 'lookup' }]"
      @click="openPage('lookup')"
    >
      <Icon icon="mdi:magnify" />
      <span>Tra cứu</span>
    </button>

    <button
      v-if="currentUser"
      type="button"
      :class="['mobile-nav-item', { active: activePage === 'account' }]"
      @click="openPage('account')"
    >
      <Icon icon="mdi:account-circle-outline" />
      <span>Tài khoản</span>
    </button>
    <button
      v-else
      type="button"
      class="mobile-nav-item"
      @click="openAuth('login')"
    >
      <Icon icon="mdi:login" />
      <span>Đăng nhập</span>
    </button>
  </nav>
</template>
