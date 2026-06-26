<script setup>
import {
  showTopUpModal, walletTopUpForm,
  formatMoney, closeTopUpModal, topUpWallet
} from '../composables/useAppState'
</script>

<template>
  <div v-if="showTopUpModal" class="modal-backdrop" @click.self="closeTopUpModal">
    <section class="modal-card topup-modal">
      <button class="modal-close" type="button" @click="closeTopUpModal">×</button>
      <span class="eyebrow">Nạp ví RetailERP</span>
      <h2>Gửi yêu cầu nạp tiền</h2>
      <div class="topup-form">
        <label>
          Số tiền cần nạp
          <input v-model.number="walletTopUpForm.amount" type="number" min="10000" step="10000" placeholder="Nhập số tiền cần nạp" />
        </label>
        <label>
          Hình thức thanh toán
          <select v-model="walletTopUpForm.paymentMethod" disabled>
            <option value="BankTransfer">Chuyển khoản</option>
          </select>
        </label>
        <div class="topup-bank-box">
          <span>Thanh toán chuyển khoản</span>
          <b>{{ formatMoney(walletTopUpForm.amount) }}</b>
          <small>Nhân viên sẽ kiểm tra giao dịch và duyệt trước khi cộng tiền vào ví.</small>
        </div>
        <button class="primary-btn full" type="button" @click="topUpWallet">Gửi yêu cầu</button>
      </div>
    </section>
  </div>
</template>
