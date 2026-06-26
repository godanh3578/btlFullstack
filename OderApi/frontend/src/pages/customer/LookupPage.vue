<script setup>
import { ref, computed, watch, watchEffect } from 'vue'
import {
  orderLookup, LOOKUP_STATUS_FILTERS,
  formatMoney, formatDateTime, statusLabel, statusClass, paymentMethodLabel, maskStart,
  orderTotal, orderDebt, paymentStatusFor, canCancelOrder, cancelFromLookup,
  lookupOrder, lookupByPhone, setLookupMode, toggleLookupExpand, lookupStatusCount
} from '../../composables/useAppState'

const searchText = ref('')
const filteredResults = ref([])

watch(() => orderLookup.value.mode, () => { searchText.value = '' })

watchEffect(() => {
  let list = orderLookup.value.results.slice()
  if (orderLookup.value.statusFilter)
    list = list.filter(o => String(o.orderStatus || '').toLowerCase() === orderLookup.value.statusFilter.toLowerCase())
  if (searchText.value.trim()) {
    const kw = searchText.value.trim().toLowerCase()
    list = list.filter(o =>
      String(o.orderCode || '').toLowerCase().includes(kw) ||
      (o.items || []).some(it => String(it.productName || '').toLowerCase().includes(kw))
    )
  }
  filteredResults.value = list
})
</script>

<template>
  <main class="page lookup-page">

    <div class="lookup-fullbg">
      <!-- Decorative elements -->
      <div class="lookup-deco lookup-deco-tl"></div>
      <div class="lookup-deco lookup-deco-br"></div>

      <div class="lookup-center-wrap">
        <!-- Title above card -->
        <p class="lookup-hero-sub">KIỂM TRA TRẠNG THÁI ĐƠN HÀNG CỦA BẠN</p>
        <h1 class="lookup-hero-title">Tra cứu đơn hàng</h1>

        <!-- Main card -->
        <div class="lookup-card-wrap">

          <!-- Tab chọn hình thức tìm -->
          <div class="lookup-mode-tabs">
            <button
              :class="['lookup-mode-tab', { active: orderLookup.mode === 'phone' }]"
              type="button"
              @click="setLookupMode('phone')"
            >
              <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M22 16.92v3a2 2 0 0 1-2.18 2 19.79 19.79 0 0 1-8.63-3.07A19.5 19.5 0 0 1 4.26 12a19.79 19.79 0 0 1-3-8.59A2 2 0 0 1 3.19 1.5h3a2 2 0 0 1 2 1.72c.127.96.361 1.903.7 2.81a2 2 0 0 1-.45 2.11L7.91 9.4a16 16 0 0 0 6.29 6.29l1.46-1.46a2 2 0 0 1 2.11-.45c.907.339 1.85.573 2.81.7A2 2 0 0 1 22 16.92z"/></svg>
              Tìm theo SĐT
            </button>
            <button
              :class="['lookup-mode-tab', { active: orderLookup.mode === 'code' }]"
              type="button"
              @click="setLookupMode('code')"
            >
              <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><rect x="5" y="2" width="14" height="20" rx="2"/><line x1="9" y1="9" x2="15" y2="9"/><line x1="9" y1="13" x2="15" y2="13"/><line x1="9" y1="17" x2="12" y2="17"/></svg>
              Tìm theo mã đơn
            </button>
          </div>

          <!-- LUỒNG 1: Tìm theo SĐT -->
          <div v-if="orderLookup.mode === 'phone'" class="lookup-card">
            <div class="lookup-card-header">
              <div class="lookup-card-icon">
                <svg width="22" height="22" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M22 16.92v3a2 2 0 0 1-2.18 2 19.79 19.79 0 0 1-8.63-3.07A19.5 19.5 0 0 1 4.26 12a19.79 19.79 0 0 1-3-8.59A2 2 0 0 1 3.19 1.5h3a2 2 0 0 1 2 1.72c.127.96.361 1.903.7 2.81a2 2 0 0 1-.45 2.11L7.91 9.4a16 16 0 0 0 6.29 6.29l1.46-1.46a2 2 0 0 1 2.11-.45c.907.339 1.85.573 2.81.7A2 2 0 0 1 22 16.92z"/></svg>
              </div>
              <div>
                <div class="lookup-card-htitle">Tra cứu đơn hàng</div>
                <div class="lookup-card-hsub">Nhập thông tin để kiểm tra trạng thái đơn hàng của bạn</div>
              </div>
            </div>
            <div class="lookup-form-area">
              <label class="lookup-label">SỐ ĐIỆN THOẠI ĐẶT HÀNG</label>
              <div class="lookup-input-wrap">
                <svg class="lookup-input-icon" width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M22 16.92v3a2 2 0 0 1-2.18 2 19.79 19.79 0 0 1-8.63-3.07A19.5 19.5 0 0 1 4.26 12a19.79 19.79 0 0 1-3-8.59A2 2 0 0 1 3.19 1.5h3a2 2 0 0 1 2 1.72c.127.96.361 1.903.7 2.81a2 2 0 0 1-.45 2.11L7.91 9.4a16 16 0 0 0 6.29 6.29l1.46-1.46a2 2 0 0 1 2.11-.45c.907.339 1.85.573 2.81.7A2 2 0 0 1 22 16.92z"/></svg>
                <input
                  class="lookup-input"
                  v-model="orderLookup.phone"
                  type="tel"
                  placeholder="Nhập số điện thoại của bạn..."
                  @keyup.enter="lookupByPhone"
                />
              </div>
              <button
                class="lookup-submit-btn"
                type="button"
                :disabled="orderLookup.loading"
                @click="lookupByPhone"
              >
                <span v-if="orderLookup.loading" class="btn-spinner"></span>
                <svg v-else width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5"><circle cx="11" cy="11" r="8"/><line x1="21" y1="21" x2="16.65" y2="16.65"/></svg>
                {{ orderLookup.loading ? 'Đang tìm...' : 'Tìm kiếm đơn hàng' }}
              </button>
              <p class="lookup-security-note">
                <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M12 22s8-4 8-10V5l-8-3-8 3v7c0 6 8 10 8 10z"/></svg>
                Thông tin của bạn được bảo mật tuyệt đối.
              </p>
            </div>
            <div v-if="orderLookup.error" class="lookup-error">
              <span class="error-icon">⚠️</span> {{ orderLookup.error }}
            </div>
            <div class="lookup-card-footer">
              <div class="lookup-support-item">
                <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="#e9b020" stroke-width="2"><path d="M3 18v-6a9 9 0 0 1 18 0v6"/><path d="M21 19a2 2 0 0 1-2 2h-1a2 2 0 0 1-2-2v-3a2 2 0 0 1 2-2h3zM3 19a2 2 0 0 0 2 2h1a2 2 0 0 0 2-2v-3a2 2 0 0 0-2-2H3z"/></svg>
                <div><b>Cần hỗ trợ?</b><span>Hotline 1900 xxxx</span></div>
              </div>
              <div class="lookup-support-divider"></div>
              <div class="lookup-support-item">
                <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="#e9b020" stroke-width="2"><path d="M21 15a2 2 0 0 1-2 2H7l-4 4V5a2 2 0 0 1 2-2h14a2 2 0 0 1 2 2z"/></svg>
                <div><b>Chat với nhân viên</b><span>Phản hồi nhanh chóng</span></div>
              </div>
            </div>
          </div>

          <!-- LUỒNG 2: Tìm theo mã đơn -->
          <div v-else class="lookup-card">
            <div class="lookup-card-header">
              <div class="lookup-card-icon">
                <svg width="22" height="22" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><rect x="5" y="2" width="14" height="20" rx="2"/><line x1="9" y1="9" x2="15" y2="9"/><line x1="9" y1="13" x2="15" y2="13"/><line x1="9" y1="17" x2="12" y2="17"/></svg>
              </div>
              <div>
                <div class="lookup-card-htitle">Tra cứu đơn hàng</div>
                <div class="lookup-card-hsub">Nhập mã đơn để kiểm tra trạng thái đơn hàng</div>
              </div>
            </div>
            <div class="lookup-form-area">
              <label class="lookup-label">MÃ ĐƠN HÀNG <span class="label-required">*</span></label>
              <div class="lookup-input-wrap">
                <svg class="lookup-input-icon" width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><rect x="5" y="2" width="14" height="20" rx="2"/><line x1="9" y1="9" x2="15" y2="9"/></svg>
                <input
                  class="lookup-input"
                  v-model="orderLookup.orderCode"
                  type="text"
                  placeholder="VD: ORD000001"
                  @input="orderLookup.orderCode = orderLookup.orderCode.toUpperCase()"
                  @keyup.enter="lookupOrder"
                />
              </div>
              <label class="lookup-label" style="margin-top:14px">
                SỐ ĐIỆN THOẠI
                <span class="label-optional">(tùy chọn)</span>
              </label>
              <div class="lookup-input-wrap">
                <svg class="lookup-input-icon" width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M22 16.92v3a2 2 0 0 1-2.18 2 19.79 19.79 0 0 1-8.63-3.07A19.5 19.5 0 0 1 4.26 12a19.79 19.79 0 0 1-3-8.59A2 2 0 0 1 3.19 1.5h3a2 2 0 0 1 2 1.72c.127.96.361 1.903.7 2.81a2 2 0 0 1-.45 2.11L7.91 9.4a16 16 0 0 0 6.29 6.29l1.46-1.46a2 2 0 0 1 2.11-.45c.907.339 1.85.573 2.81.7A2 2 0 0 1 22 16.92z"/></svg>
                <input
                  class="lookup-input"
                  v-model="orderLookup.phone"
                  type="tel"
                  placeholder="0912 345 678"
                  @keyup.enter="lookupOrder"
                />
              </div>
              <button
                class="lookup-submit-btn"
                type="button"
                :disabled="orderLookup.loading"
                style="margin-top:16px"
                @click="lookupOrder"
              >
                <span v-if="orderLookup.loading" class="btn-spinner"></span>
                <svg v-else width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5"><circle cx="11" cy="11" r="8"/><line x1="21" y1="21" x2="16.65" y2="16.65"/></svg>
                {{ orderLookup.loading ? 'Đang tra cứu...' : 'Tìm kiếm đơn hàng' }}
              </button>
              <p class="lookup-security-note">
                <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M12 22s8-4 8-10V5l-8-3-8 3v7c0 6 8 10 8 10z"/></svg>
                Thông tin của bạn được bảo mật tuyệt đối.
              </p>
            </div>
            <div v-if="orderLookup.error" class="lookup-error">
              <span class="error-icon">⚠️</span> {{ orderLookup.error }}
            </div>
            <div class="lookup-card-footer">
              <div class="lookup-support-item">
                <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="#e9b020" stroke-width="2"><path d="M3 18v-6a9 9 0 0 1 18 0v6"/><path d="M21 19a2 2 0 0 1-2 2h-1a2 2 0 0 1-2-2v-3a2 2 0 0 1 2-2h3zM3 19a2 2 0 0 0 2 2h1a2 2 0 0 0 2-2v-3a2 2 0 0 0-2-2H3z"/></svg>
                <div><b>Cần hỗ trợ?</b><span>Hotline 1900 xxxx</span></div>
              </div>
              <div class="lookup-support-divider"></div>
              <div class="lookup-support-item">
                <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="#e9b020" stroke-width="2"><path d="M21 15a2 2 0 0 1-2 2H7l-4 4V5a2 2 0 0 1 2-2h14a2 2 0 0 1 2 2z"/></svg>
                <div><b>Chat với nhân viên</b><span>Phản hồi nhanh chóng</span></div>
              </div>
            </div>
          </div>

          <!-- KẾT QUẢ: Mã đơn — hiện 1 đơn -->
          <div v-if="orderLookup.mode === 'code' && orderLookup.result" class="lookup-result-card">
            <div class="lrc-header">
              <div class="lrc-code-row">
                <span class="lrc-badge">📦</span>
                <span class="lrc-code">{{ orderLookup.result.orderCode }}</span>
                <span :class="['status-pill', statusClass(orderLookup.result.orderStatus)]">
                  {{ statusLabel(orderLookup.result.orderStatus) }}
                </span>
              </div>
              <button
                v-if="canCancelOrder(orderLookup.result)"
                type="button"
                class="danger-btn-sm"
                @click="cancelFromLookup(orderLookup.result)"
              >Hủy đơn</button>
            </div>
            <div class="lrc-body">
              <div class="lrc-facts">
                <div class="lrc-fact"><span>Ngày đặt</span><b>{{ formatDateTime(orderLookup.result.orderDate) }}</b></div>
                <div class="lrc-fact"><span>Khách hàng</span><b>{{ orderLookup.result.customerName || '—' }}</b></div>
                <div class="lrc-fact"><span>Tổng tiền</span><b class="lrc-price">{{ formatMoney(orderTotal(orderLookup.result)) }}</b></div>
                <div class="lrc-fact"><span>Đã trả</span><b>{{ formatMoney(orderLookup.result.paidAmount) }}</b></div>
                <div v-if="orderDebt(orderLookup.result) > 0" class="lrc-fact">
                  <span>Còn nợ</span><b class="text-red">{{ formatMoney(orderDebt(orderLookup.result)) }}</b>
                </div>
                <div class="lrc-fact">
                  <span>Thanh toán</span>
                  <span :class="['status-pill', statusClass(paymentStatusFor(orderLookup.result))]">
                    {{ statusLabel(paymentStatusFor(orderLookup.result)) }}
                  </span>
                </div>
                <div class="lrc-fact"><span>Hình thức</span><b>{{ paymentMethodLabel(orderLookup.result.paymentMethod) }}</b></div>
              </div>
              <div v-if="orderLookup.result.items?.length" class="lrc-items">
                <p class="lrc-items-title">Chi tiết sản phẩm</p>
                <div v-for="item in orderLookup.result.items" :key="item.orderDetailId" class="lrc-item-row">
                  <span class="lrc-item-name">{{ item.productName }}</span>
                  <span class="lrc-item-qty">x{{ item.quantity }}</span>
                  <span class="lrc-item-price">{{ formatMoney(item.subTotal) }}</span>
                </div>
              </div>
            </div>
          </div>

          <!-- KẾT QUẢ: SĐT — danh sách đơn + filter -->
          <template v-if="orderLookup.mode === 'phone' && orderLookup.results.length">
            <div class="lookup-results-toolbar">
              <div class="lookup-results-meta">
                <span class="lookup-count">
                  📋 <b>{{ orderLookup.results.length }}</b> đơn · SĐT <b>{{ maskStart(orderLookup.phone) }}</b>
                </span>
              </div>
              <div class="lookup-search-filter">
                <div class="lookup-inline-search">
                  <span class="lis-icon">🔍</span>
                  <input
                    class="lis-input"
                    :value="searchText"
                    @input="searchText = $event.target.value"
                    type="text"
                    placeholder="Lọc theo tên sản phẩm hoặc mã đơn..."
                  />
                  <button v-if="searchText" class="lis-clear" type="button" @click="searchText = ''">×</button>
                </div>
              </div>
              <div class="lookup-filter-tabs">
                <button
                  :class="['filter-chip', { active: !orderLookup.statusFilter }]"
                  type="button"
                  @click="orderLookup.statusFilter = ''"
                >Tất cả <span class="chip-count">{{ orderLookup.results.length }}</span></button>
                <button
                  v-for="f in LOOKUP_STATUS_FILTERS"
                  :key="f.value"
                  :class="['filter-chip', { active: orderLookup.statusFilter === f.value }]"
                  type="button"
                  @click="orderLookup.statusFilter = f.value"
                  v-show="lookupStatusCount(f.value) > 0"
                >{{ f.label }} <span class="chip-count">{{ lookupStatusCount(f.value) }}</span></button>
              </div>
            </div>
            <p v-if="!filteredResults.length" class="soft-alert">Không có đơn nào khớp với bộ lọc hiện tại.</p>
            <div v-for="order in filteredResults" :key="order.orderId" class="lookup-order-row">
              <div class="lookup-row-summary" @click="toggleLookupExpand(order.orderId)">
                <div class="lor-left">
                  <span class="lor-code">{{ order.orderCode }}</span>
                  <span class="lor-date">{{ formatDateTime(order.orderDate) }}</span>
                </div>
                <div class="lor-mid">
                  <span class="lor-preview">
                    {{ order.items?.[0]?.productName || '—' }}
                    {{ order.items?.length > 1 ? ` +${order.items.length - 1} SP` : '' }}
                  </span>
                </div>
                <div class="lor-right">
                  <span class="lor-amount">{{ formatMoney(orderTotal(order)) }}</span>
                  <span :class="['status-pill', statusClass(order.orderStatus)]">{{ statusLabel(order.orderStatus) }}</span>
                </div>
                <span class="lookup-chevron">{{ orderLookup.expandedId === order.orderId ? '▲' : '▼' }}</span>
              </div>
              <div v-if="orderLookup.expandedId === order.orderId" class="lookup-row-detail">
                <div class="lrc-facts compact">
                  <div class="lrc-fact"><span>Ngày đặt</span><b>{{ formatDateTime(order.orderDate) }}</b></div>
                  <div class="lrc-fact"><span>Tổng tiền</span><b class="lrc-price">{{ formatMoney(orderTotal(order)) }}</b></div>
                  <div class="lrc-fact"><span>Đã trả</span><b>{{ formatMoney(order.paidAmount) }}</b></div>
                  <div v-if="orderDebt(order) > 0" class="lrc-fact"><span>Còn nợ</span><b class="text-red">{{ formatMoney(orderDebt(order)) }}</b></div>
                  <div class="lrc-fact">
                    <span>Thanh toán</span>
                    <span :class="['status-pill', statusClass(paymentStatusFor(order))]">{{ statusLabel(paymentStatusFor(order)) }}</span>
                  </div>
                  <div class="lrc-fact"><span>Hình thức</span><b>{{ paymentMethodLabel(order.paymentMethod) }}</b></div>
                </div>
                <div v-if="order.items?.length" class="lrc-items">
                  <p class="lrc-items-title">Sản phẩm trong đơn</p>
                  <div v-for="item in order.items" :key="item.orderDetailId" class="lrc-item-row">
                    <span class="lrc-item-name">{{ item.productName }}</span>
                    <span class="lrc-item-qty">x{{ item.quantity }}</span>
                    <span class="lrc-item-price">{{ formatMoney(item.subTotal) }}</span>
                  </div>
                </div>
                <button v-if="canCancelOrder(order)" type="button" class="danger-btn-sm" style="margin-top:10px" @click="cancelFromLookup(order)">Hủy đơn này</button>
              </div>
            </div>
          </template>

        </div>
      </div>
    </div>
  </main>
</template>
