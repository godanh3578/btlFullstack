<script setup>
import { watch, onMounted, onUnmounted } from 'vue'
import api, { getStaffToken, setStaffToken } from './api/client'

import AppTopbar from './components/AppTopbar.vue'
import AppFooter from './components/AppFooter.vue'
import AuthModal from './components/AuthModal.vue'
import StaffLoginModal from './components/StaffLoginModal.vue'
import ProductDetailModal from './components/ProductDetailModal.vue'
import TopUpModal from './components/TopUpModal.vue'
import BackToTop from './components/BackToTop.vue'
import MobileNav from './components/MobileNav.vue'

import {
  notice, currentUser, staffUser, activePage,
  accountProfile, activityLogs,
  parseStaffToken, setCurrentCustomer, logoutCustomer, showNotice,
  loadWalletState, initCheckoutShipping, loadProducts, loadMyOrders,
  loadTopUpRequests, loadIntegrationHealth, loadStaffData,
  backendAssetUrl, avatarUrl, initProfileData,
  readJsonStorage
} from './composables/useAppState'

const ACTIVITY_LOG_KEY = 'retailerpActivityLog'

watch(() => currentUser.value, (newVal) => {
  if (newVal) initProfileData()
}, { immediate: true })

onMounted(async () => {
  const token = getStaffToken()
  const parsed = token ? parseStaffToken(token) : null
  if (parsed) staffUser.value = parsed
  else setStaffToken('')

  if (currentUser.value?.customerId) {
    try {
      const res = await api.get(`/api/Customers/${currentUser.value.customerId}/profile`)
      setCurrentCustomer(res.data)
      if (res.data?.dateOfBirth) {
        const parts = res.data.dateOfBirth.split('T')[0].split('-')
        accountProfile.value.year = parseInt(parts[0])
        accountProfile.value.month = parseInt(parts[1])
        accountProfile.value.day = parseInt(parts[2])
      }
      if (res.data?.avatarUrl) {
        avatarUrl.value = backendAssetUrl(res.data.avatarUrl)
      }
    } catch (err) {
      if ([403, 404].includes(err.response?.status)) {
        logoutCustomer()
        showNotice(err.response?.data?.message || 'Tài khoản không còn hoạt động, vui lòng đăng nhập lại.', 'bad')
        return
      }
    }
    loadWalletState(currentUser.value)
    initCheckoutShipping()
  }

  activityLogs.value = readJsonStorage(ACTIVITY_LOG_KEY, [])
  await loadTopUpRequests()
  await loadIntegrationHealth()
  await loadProducts()
  if (currentUser.value) await loadMyOrders()
  if (activePage.value === 'orderDetail' && staffUser.value) await loadStaffData()

  // Heartbeat: kiểm tra tài khoản có bị khóa không mỗi 60 giây
  const checkAccountActive = async () => {
    const email = currentUser.value?.email
    if (!email || currentUser.value?.isLocalOnly) return
    try {
      const res = await fetch(`/api/auth/check-active?email=${encodeURIComponent(email)}`)
      if (res.ok) {
        const data = await res.json()
        if (!data.isActive) {
          logoutCustomer()
          showNotice('Tài khoản của bạn đã bị khóa. Vui lòng liên hệ hỗ trợ.', 'bad')
        }
      }
    } catch { /* bỏ qua nếu Machine3 offline */ }
  }

  const heartbeatTimer = setInterval(checkAccountActive, 60_000)
  onUnmounted(() => clearInterval(heartbeatTimer))
})
</script>

<template>
  <div class="app-shell">
    <div v-if="notice.message" :class="['toast', notice.type]">
      {{ notice.message }}
    </div>

    <AppTopbar />
    <RouterView v-slot="{ Component }">
      <Transition name="page" mode="out-in">
        <component :is="Component" :key="$route.path" />
      </Transition>
    </RouterView>
    <AppFooter />

    <AuthModal />
    <StaffLoginModal />
    <ProductDetailModal />
    <TopUpModal />
    <BackToTop />
    <MobileNav />
  </div>
</template>

<style src="./App.css"></style>
