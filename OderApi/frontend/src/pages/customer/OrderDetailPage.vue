<script setup>
import { computed } from 'vue'
import {
  currentOrderDetail,
  formatMoney, formatDateTime, statusLabel, paymentStatusFor,
  orderTotal, orderDebt, orderItems,
  isDepositHolding, canCancelOrder, payRemainingOrder, cancelOrder, openPage
} from '../../composables/useAppState'

const ORDER_STEPS = [
  { key: 'pending',   label: 'Chờ xử lý',    icon: '🕐' },
  { key: 'confirmed', label: 'Đã xác nhận',   icon: '✅' },
  { key: 'shipping',  label: 'Đang giao',     icon: '🚚' },
  { key: 'completed', label: 'Hoàn tất',      icon: '🎉' }
]

const orderStepIndex = computed(() => {
  const s = String(currentOrderDetail.value?.orderStatus || '').toLowerCase()
  if (s === 'cancelled') return -1
  const idx = ORDER_STEPS.findIndex(step => step.key === s)
  return idx === -1 ? 0 : idx
})
</script>

<template>
  <main class="page">
    <div class="page-title">
      <span>Đơn hàng</span>
      <h1>Chi tiết đơn hàng</h1>
    </div>
    <section v-if="!currentOrderDetail" class="panel">
      <div class="empty-state compact">
        <h3>Không tìm thấy đơn hàng</h3>
        <p>Hãy tải lại danh sách đơn hàng hoặc quay về trang lịch sử đơn.</p>
        <button class="primary-btn" type="button" @click="openPage('myOrders')">Quay lại</button>
      </div>
    </section>
    <section v-else class="panel order-detail-panel">
      <!-- Order tracking timeline -->
      <div v-if="orderStepIndex >= 0" class="order-timeline">
        <div
          v-for="(step, i) in ORDER_STEPS"
          :key="step.key"
          :class="['timeline-step', { done: i < orderStepIndex, active: i === orderStepIndex }]"
        >
          <div class="timeline-dot">{{ step.icon }}</div>
          <div class="timeline-connector" v-if="i < ORDER_STEPS.length - 1"></div>
          <div class="timeline-label">{{ step.label }}</div>
        </div>
      </div>
      <div v-else class="order-timeline cancelled-notice">
        <span>❌</span> Đơn hàng đã bị hủy
      </div>

      <div class="detail-facts">
        <p><span>Mã đơn</span><b>{{ currentOrderDetail.orderCode }}</b></p>
        <p><span>Khách hàng</span><b>{{ currentOrderDetail.customerName || currentOrderDetail.customerId }}</b></p>
        <p><span>Ngày đặt</span><b>{{ formatDateTime(currentOrderDetail.orderDate) }}</b></p>
        <p><span>Thanh toán</span><b>{{ statusLabel(paymentStatusFor(currentOrderDetail)) }}</b></p>
        <p><span>Trạng thái</span><b>{{ statusLabel(currentOrderDetail.orderStatus) }}</b></p>
        <p><span>Tổng tiền</span><b>{{ formatMoney(orderTotal(currentOrderDetail)) }}</b></p>
        <p><span>Đã trả</span><b>{{ formatMoney(currentOrderDetail.paidAmount) }}</b></p>
        <p><span>Công nợ</span><b>{{ formatMoney(orderDebt(currentOrderDetail)) }}</b></p>
      </div>

      <div v-if="isDepositHolding(currentOrderDetail)" class="detail-pay-remaining">
        <div class="detail-pay-remaining-info">
          <span class="dpr-label">Ứng cọc đang giữ</span>
          <span>Còn lại cần thanh toán: <b class="debt-text">{{ formatMoney(orderDebt(currentOrderDetail)) }}</b></span>
        </div>
        <div class="detail-pay-remaining-actions">
          <button type="button" class="holding-confirm-btn" @click="payRemainingOrder(currentOrderDetail)">Thanh toán phần còn lại</button>
          <button v-if="canCancelOrder(currentOrderDetail)" type="button" class="holding-cancel-btn" @click="cancelOrder(currentOrderDetail)">Hủy & Hoàn cọc</button>
        </div>
      </div>

      <div class="table-wrap">
        <table>
          <thead>
            <tr>
              <th>Sản phẩm</th>
              <th>Số lượng</th>
              <th>Đơn giá</th>
              <th>Giảm giá</th>
              <th>Thành tiền</th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="item in orderItems(currentOrderDetail)" :key="item.orderDetailId || item.orderItemId || item.productId">
              <td>{{ item.productName || item.name }}</td>
              <td>{{ item.quantity }}</td>
              <td>{{ formatMoney(item.unitPrice) }}</td>
              <td>{{ formatMoney(item.discountAmount || 0) }}</td>
              <td>{{ formatMoney(item.subTotal || item.lineTotal || (Number(item.unitPrice || 0) * Number(item.quantity || 0))) }}</td>
            </tr>
          </tbody>
        </table>
      </div>
    </section>
  </main>
</template>
