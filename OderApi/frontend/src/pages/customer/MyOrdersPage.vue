<script setup>
import { ref, onMounted, onUnmounted } from 'vue'
import {
  currentUser, myOrders, myOrdersLoading,
  formatMoney, formatDateTime, statusLabel, statusClass, paymentMethodLabel,
  orderTotal, orderDebt, paymentStatusFor,
  isDepositHolding, canCancelOrder, cancelOrder, payRemainingOrder, openOrderDetail,
  reorderItems, openPage, canReviewOrder, hasReviewed,
  MY_ORDERS_TABS, myOrdersTab, filteredMyOrders, loadMyOrders
} from '../../composables/useAppState'

let pollTimer = null
onMounted(() => {
  pollTimer = setInterval(() => { if (currentUser.value) loadMyOrders() }, 8000)
})
onUnmounted(() => clearInterval(pollTimer))
import ReviewModal from '../../components/ReviewModal.vue'

const reviewingOrder = ref(null)

function openReview(order) { reviewingOrder.value = order }
function closeReview() { reviewingOrder.value = null }
function onReviewDone() { reviewingOrder.value = null }

function showReorderBtn(order) {
  const s = String(order?.orderStatus || '').toLowerCase()
  return s === 'cancelled' || hasReviewed(order)
}

function tabCount(key) {
  if (key === 'all') return myOrders.value.length
  const checks = {
    pending: o => { const s = String(o.orderStatus||'').toLowerCase(); const ps = String(o.paymentStatus||'').toLowerCase(); return ['pending','confirmed','waitingpayment','awaitingconfirmation','awaitingpaymentconfirmation','processing','waitingdelivery'].includes(s) || ['pendingpayment','awaitingpaymentconfirmation'].includes(ps) },
    completed: o => String(o.orderStatus||'').toLowerCase() === 'completed',
    cancelled: o => String(o.orderStatus||'').toLowerCase() === 'cancelled',
  }
  return checks[key] ? myOrders.value.filter(checks[key]).length : 0
}
</script>

<template>
  <main class="page">
    <div class="page-title">
      <span>Lịch sử</span>
      <h1>Đơn mua của tôi</h1>
    </div>
    <div class="account-summary-grid">
      <article class="debt-card">
        <span>Công nợ hiện tại</span>
        <b>{{ formatMoney(currentUser?.currentDebt || 0) }}</b>
      </article>
    </div>

    <!-- Tab lọc trạng thái -->
    <div class="myorders-tabs">
      <button
        v-for="tab in MY_ORDERS_TABS"
        :key="tab.key"
        :class="['myorders-tab', { active: myOrdersTab === tab.key }]"
        type="button"
        @click="myOrdersTab = tab.key"
      >
        {{ tab.label }}
        <span v-if="tabCount(tab.key) > 0" class="myorders-tab-count">{{ tabCount(tab.key) }}</span>
      </button>
    </div>

    <section class="panel">
      <div v-if="myOrdersLoading" class="table-wrap">
        <table>
          <thead>
            <tr><th>Mã đơn</th><th>Ngày đặt</th><th>Tổng tiền</th><th>Đã trả</th><th>Công nợ</th><th>Thanh toán</th><th>Trạng thái</th><th>Thao tác</th></tr>
          </thead>
          <tbody>
            <tr v-for="i in 5" :key="i" class="skeleton-table-row">
              <td v-for="j in 8" :key="j"><div class="skeleton-line" :style="j === 8 ? 'width:60px' : ''"></div></td>
            </tr>
          </tbody>
        </table>
      </div>

      <div v-else-if="myOrders.length === 0" class="empty-state compact">
        <svg width="72" height="72" viewBox="0 0 72 72" fill="none">
          <rect x="12" y="20" width="48" height="40" rx="6" fill="#f1f5f9" stroke="#e2e8f0" stroke-width="2"/>
          <path d="M24 20V16a12 12 0 0 1 24 0v4" stroke="#cbd5e1" stroke-width="2.5" stroke-linecap="round"/>
          <line x1="24" y1="36" x2="48" y2="36" stroke="#cbd5e1" stroke-width="2" stroke-linecap="round"/>
          <line x1="24" y1="44" x2="40" y2="44" stroke="#cbd5e1" stroke-width="2" stroke-linecap="round"/>
        </svg>
        <h3>Chưa có đơn hàng</h3>
        <p>Các đơn đã đặt sẽ hiển thị tại đây.</p>
        <button class="primary-btn" type="button" @click="openPage('shop')">Mua hàng ngay</button>
      </div>

      <div v-else-if="filteredMyOrders.length === 0" class="empty-state compact">
        <h3>Không có đơn nào</h3>
        <p>Không có đơn hàng nào ở trạng thái này.</p>
      </div>

      <div v-else class="table-wrap">
        <table>
          <thead>
            <tr>
              <th>Mã đơn</th>
              <th>Ngày đặt</th>
              <th>Tổng tiền</th>
              <th>Đã trả</th>
              <th>Công nợ</th>
              <th>Thanh toán</th>
              <th>Trạng thái đơn</th>
              <th>Thao tác</th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="order in filteredMyOrders" :key="order.orderId || order.id">
              <td>{{ order.orderCode }}</td>
              <td>{{ formatDateTime(order.orderDate) }}</td>
              <td>{{ formatMoney(orderTotal(order)) }}</td>
              <td>{{ formatMoney(order.paidAmount) }}</td>
              <td>{{ formatMoney(orderDebt(order)) }}</td>
              <td>
                <span :class="['status-pill', statusClass(paymentStatusFor(order))]">{{ statusLabel(paymentStatusFor(order)) }}</span>
                <small>{{ paymentMethodLabel(order.paymentMethod) }}</small>
              </td>
              <td>
                <span :class="['status-pill', statusClass(order.orderStatus)]">{{ statusLabel(order.orderStatus) }}</span>
                <small v-if="order.approvedBy" class="order-approved-label">Duyệt bởi: {{ order.approvedBy }}</small>
              </td>
              <td class="table-actions icon-actions">
                <!-- Xem chi tiết -->
                <v-tooltip text="Xem chi tiết" location="top">
                  <template #activator="{ props }">
                    <v-btn v-bind="props" icon size="small" variant="tonal" @click="openOrderDetail(order)">
                      <v-icon icon="mdi-eye-outline" size="18" />
                    </v-btn>
                  </template>
                </v-tooltip>

                <!-- Đánh giá -->
                <v-tooltip v-if="canReviewOrder(order)" text="Đánh giá đơn hàng" location="top">
                  <template #activator="{ props }">
                    <v-btn v-bind="props" icon size="small" variant="tonal" color="warning" @click="openReview(order)">
                      <v-icon icon="mdi-star-outline" size="18" />
                    </v-btn>
                  </template>
                </v-tooltip>

                <!-- Mua lại -->
                <v-tooltip v-if="showReorderBtn(order)" text="Mua lại" location="top">
                  <template #activator="{ props }">
                    <v-btn v-bind="props" icon size="small" variant="tonal" color="success" @click="reorderItems(order)">
                      <v-icon icon="mdi-cart-plus" size="18" />
                    </v-btn>
                  </template>
                </v-tooltip>

                <!-- Trả đủ (deposit) -->
                <v-tooltip v-if="isDepositHolding(order)" text="Trả phần còn lại" location="top">
                  <template #activator="{ props }">
                    <v-btn v-bind="props" icon size="small" variant="tonal" color="info" @click="payRemainingOrder(order)">
                      <v-icon icon="mdi-cash-plus" size="18" />
                    </v-btn>
                  </template>
                </v-tooltip>

                <!-- Hủy đơn -->
                <v-tooltip v-if="canCancelOrder(order)" text="Hủy đơn" location="top">
                  <template #activator="{ props }">
                    <v-btn v-bind="props" icon size="small" variant="tonal" color="error" @click="cancelOrder(order)">
                      <v-icon icon="mdi-close-circle-outline" size="18" />
                    </v-btn>
                  </template>
                </v-tooltip>
              </td>
            </tr>
          </tbody>
        </table>
      </div>
    </section>

    <ReviewModal
      v-if="reviewingOrder"
      :order="reviewingOrder"
      @close="closeReview"
      @done="onReviewDone"
    />
  </main>
</template>
