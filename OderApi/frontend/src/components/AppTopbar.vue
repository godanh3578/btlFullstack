<script setup>
import { ref, computed, onMounted, onUnmounted } from 'vue'
import { Icon } from '@iconify/vue'
import { ShoppingBasket } from '@lucide/vue'
import {
  currentUser, showUserMenu, searchText, cartCount, activePage,
  openPage, openAuth, logoutCustomer,
  products, productId, productName, productCategory, productPrice,
  formatMoney, activityLogs
} from '../composables/useAppState'

const showSuggestions = ref(false)
const showNotifications = ref(false)

const suggestions = computed(() => {
  if (!searchText.value || searchText.value.length < 2) return []
  const q = searchText.value.toLowerCase()
  return products.value
    .filter(p =>
      productName(p).toLowerCase().includes(q) ||
      productCategory(p).toLowerCase().includes(q)
    )
    .slice(0, 6)
})

function selectSuggestion(product) {
  searchText.value = productName(product)
  showSuggestions.value = false
  openPage('shop')
}

function doSearch() {
  showSuggestions.value = false
  openPage('shop')
}

const recentNotifs = computed(() => activityLogs.value.slice(0, 5))

function timeAgo(iso) {
  if (!iso) return ''
  const diff = Math.floor((Date.now() - new Date(iso)) / 60000)
  if (diff < 1) return 'Vừa xong'
  if (diff < 60) return `${diff} phút trước`
  if (diff < 1440) return `${Math.floor(diff / 60)} giờ trước`
  return `${Math.floor(diff / 1440)} ngày trước`
}

// Flash sale countdown to next 14:00
const countdown = ref('')
let countdownTimer = null

function updateCountdown() {
  const now = new Date()
  const target = new Date()
  target.setHours(14, 0, 0, 0)
  if (now >= target) target.setDate(target.getDate() + 1)
  const diff = target - now
  const h = Math.floor(diff / 3600000).toString().padStart(2, '0')
  const m = Math.floor((diff % 3600000) / 60000).toString().padStart(2, '0')
  const s = Math.floor((diff % 60000) / 1000).toString().padStart(2, '0')
  countdown.value = `${h}:${m}:${s}`
}

onMounted(() => { updateCountdown(); countdownTimer = setInterval(updateCountdown, 1000) })
onUnmounted(() => clearInterval(countdownTimer))

const NOTIF_ICONS = {
  'order.created': 'mdi:cart-check',
  'order.cancelled': 'mdi:cart-remove',
  'wallet.requested': 'mdi:wallet-plus',
  'wallet.approved': 'mdi:wallet-check',
  'wallet.rejected': 'mdi:wallet-remove',
}

const NOTIF_FALLBACK = {
  'order.created': 'Đặt hàng thành công',
  'order.cancelled': 'Đã hủy đơn hàng',
  'wallet.requested': 'Yêu cầu nạp ví',
  'wallet.approved': 'Nạp ví thành công',
  'wallet.rejected': 'Yêu cầu nạp ví bị từ chối',
}

function notifNote(notif) {
  const note = notif.note || ''
  // fix old English-only entries (e.g. "Cancelled", "Created") stored before i18n
  if (/^[A-Z][a-z]+$/.test(note.trim())) return NOTIF_FALLBACK[notif.action] || note
  return note
}
</script>

<template>
  <div class="app-header-wrap">
    <!-- Info bar -->
    <div class="shopee-topbar">
      <div class="shopee-topbar-inner">
        <div class="stopbar-left">
          <span class="topbar-static">Kênh Đối Tác</span>
          <span class="sbsep">|</span>
          <span class="topbar-static">Ứng Dụng</span>
        </div>
        <div class="stopbar-center">
          <span class="topbar-promo">⚡ Flash Sale -30% &bull; Freeship 500K &bull; Voucher 50K &bull; Deal 12h–14h</span>
          <span class="flash-countdown">{{ countdown }}</span>
        </div>
        <div class="stopbar-right">
          <span class="topbar-icon-link"><Icon icon="mdi:help-circle-outline" />Hỗ Trợ</span>
          <span class="sbsep">|</span>
          <template v-if="!currentUser">
            <button class="topbar-link" type="button" @click="openAuth('register')">Đăng Ký</button>
            <span class="sbsep">|</span>
            <button class="topbar-link" type="button" @click="openAuth('login')">Đăng Nhập</button>
          </template>
          <template v-else>
            <!-- Notification bell -->
            <div class="topbar-bell-wrap" v-click-outside="() => showNotifications = false">
              <button class="topbar-link topbar-bell-btn" type="button" @click="showNotifications = !showNotifications">
                <Icon icon="mdi:bell-outline" />
                <span v-if="recentNotifs.length > 0" class="bell-badge">{{ Math.min(recentNotifs.length, 9) }}</span>
              </button>
              <div v-if="showNotifications" class="bell-dropdown">
                <div class="bell-dropdown-head">Thông báo</div>
                <div v-if="recentNotifs.length === 0" class="bell-empty">
                  <Icon icon="mdi:bell-off-outline" style="font-size:28px;opacity:.4" />
                  <span>Không có thông báo</span>
                </div>
                <div v-else>
                  <div v-for="notif in recentNotifs" :key="notif.id" class="bell-item">
                    <div class="bell-item-icon">
                      <Icon :icon="NOTIF_ICONS[notif.action] || 'mdi:bell'" />
                    </div>
                    <div class="bell-item-body">
                      <p>{{ notifNote(notif) }}</p>
                      <small>{{ timeAgo(notif.createdAt) }}</small>
                    </div>
                  </div>
                </div>
              </div>
            </div>
            <span class="sbsep">|</span>
            <!-- User menu -->
            <div class="topbar-userblock" v-click-outside="() => showUserMenu = false" @mouseenter="showUserMenu = true" @mouseleave="showUserMenu = false">
              <button class="topbar-link topbar-user-btn" type="button">
                <Icon icon="mdi:account-circle-outline" />
                {{ currentUser.fullName }}
                <Icon icon="mdi:chevron-down" />
              </button>
              <div v-if="showUserMenu" class="user-dropdown">
                <div class="user-dropdown-card">
                  <button class="udrop-item" type="button" @click="openPage('account', 'profile'); showUserMenu = false">Tài Khoản Của Tôi</button>
                  <button class="udrop-item" type="button" @click="openPage('account', 'wallet'); showUserMenu = false">Ví RetailERP</button>
                  <button class="udrop-item" type="button" @click="openPage('myOrders'); showUserMenu = false">Đơn Mua</button>
                  <button class="udrop-item danger" type="button" @click="logoutCustomer(); showUserMenu = false">Đăng Xuất</button>
                </div>
              </div>
            </div>
          </template>
        </div>
      </div>
    </div>

    <!-- Main header -->
    <header class="shopee-header">
      <div class="shopee-header-inner">
        <!-- Logo -->
        <button class="shopee-brand" type="button" @click="openPage('shop')">
          <svg class="shopee-logo-img" viewBox="0 0 80 80" fill="none" xmlns="http://www.w3.org/2000/svg">
            <line x1="43" y1="10" x2="63" y2="22" stroke="white" stroke-width="8" stroke-linecap="round"/>
            <line x1="65" y1="27" x2="65" y2="53" stroke="white" stroke-width="8" stroke-linecap="round"/>
            <line x1="63" y1="58" x2="43" y2="70" stroke="white" stroke-width="8" stroke-linecap="round"/>
            <line x1="37" y1="70" x2="17" y2="58" stroke="white" stroke-width="8" stroke-linecap="round"/>
            <line x1="15" y1="53" x2="15" y2="27" stroke="white" stroke-width="8" stroke-linecap="round"/>
            <line x1="17" y1="22" x2="37" y2="10" stroke="white" stroke-width="8" stroke-linecap="round"/>
            <line x1="40" y1="2" x2="40" y2="9" stroke="#c9a84c" stroke-width="2.5" stroke-linecap="round"/>
            <circle cx="40" cy="9" r="4" fill="#c9a84c"/>
            <line x1="2" y1="40" x2="9" y2="40" stroke="white" stroke-width="2.5" stroke-linecap="round"/>
            <circle cx="9" cy="40" r="4" fill="white"/>
            <line x1="71" y1="40" x2="78" y2="40" stroke="white" stroke-width="2.5" stroke-linecap="round"/>
            <circle cx="71" cy="40" r="4" fill="white"/>
            <line x1="40" y1="26" x2="40" y2="50" stroke="#c9a84c" stroke-width="6" stroke-linecap="round"/>
            <line x1="27" y1="18" x2="40" y2="26" stroke="#c9a84c" stroke-width="6" stroke-linecap="round"/>
            <line x1="53" y1="18" x2="40" y2="26" stroke="#c9a84c" stroke-width="6" stroke-linecap="round"/>
          </svg>
          <span class="shopee-brand-text">
            <b>ORIVEX</b>
            <small>Order &amp; Sales</small>
          </span>
        </button>

        <!-- Search with autocomplete -->
        <div class="shopee-search-wrap" v-click-outside="() => showSuggestions = false">
          <div class="shopee-search">
            <input
              :value="searchText"
              @input="searchText = $event.target.value; showSuggestions = true"
              @compositionend="searchText = $event.target.value; showSuggestions = true"
              type="search"
              placeholder="Tìm sản phẩm trong kho..."
              autocomplete="off"
              @focus="showSuggestions = true"
              @keyup.enter="doSearch"
            />
            <button class="shopee-search-btn" type="button" @click="doSearch">
              <Icon icon="mdi:magnify" style="font-size:20px;" />
            </button>
          </div>
          <!-- Suggestions dropdown -->
          <div v-if="showSuggestions && suggestions.length > 0" class="search-suggestions">
            <div
              v-for="product in suggestions"
              :key="productId(product)"
              class="search-suggestion-item"
              @mousedown.prevent="selectSuggestion(product)"
            >
              <Icon icon="mdi:magnify" class="sug-icon" />
              <div class="sug-info">
                <span class="sug-name">{{ productName(product) }}</span>
                <small class="sug-cat">{{ productCategory(product) }}</small>
              </div>
              <span class="sug-price">{{ formatMoney(productPrice(product)) }}</span>
            </div>
          </div>
        </div>

        <!-- Cart -->
        <button class="shopee-cart-btn" type="button" @click="openPage('cart')">
          <div class="shopee-cart-icon">
            <ShoppingBasket :size="30" :stroke-width="1.8" aria-hidden="true" />
            <span v-if="cartCount > 0" class="shopee-cart-badge">{{ cartCount > 99 ? '99+' : cartCount }}</span>
          </div>
          <span class="shopee-cart-label">Giỏ hàng</span>
        </button>
      </div>

      <!-- Nav strip -->
      <nav class="shopee-nav-strip">
        <div class="shopee-nav-inner">
          <button type="button" :class="{ active: activePage === 'shop' }" @click="openPage('shop')">Trang chủ</button>
          <button type="button" :class="{ active: activePage === 'lookup' }" @click="openPage('lookup')">Tra cứu đơn</button>
          <button type="button" :class="{ active: activePage === 'myOrders' }" @click="openPage('myOrders')">Đơn mua</button>
          <button v-if="currentUser" type="button" :class="{ active: activePage === 'account' }" @click="openPage('account')">Tài khoản</button>
        </div>
      </nav>
    </header>
  </div>
</template>
