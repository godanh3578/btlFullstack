<script setup>
import {
  showStaffModal, staffLoginForm, staffBusy, staffError,
  loginStaff
} from '../composables/useAppState'
</script>

<template>
  <div v-if="showStaffModal" class="modal-backdrop">
    <section class="modal-card small-modal">
      <button class="modal-close" type="button" @click="showStaffModal = false">×</button>
      <span class="eyebrow">Nhân viên</span>
      <h2>Đăng nhập quản trị</h2>
      <div class="form-grid staff-login-grid">
        <label>Email nhan vien<input v-model="staffLoginForm.email" type="email" /></label>
        <label>Mat khau<input v-model="staffLoginForm.password" type="password" /></label>
        <label>Tên đăng nhập<input v-model="staffLoginForm.username" type="text" /></label>
        <label>
          Vai trò
          <select v-model="staffLoginForm.role">
            <option value="Sales">Sales</option>
            <option value="Admin">Admin</option>
            <option value="Warehouse">Warehouse</option>
          </select>
        </label>
        <button class="primary-btn full" type="button" :disabled="staffBusy" @click="loginStaff">
          {{ staffBusy ? 'Đang đăng nhập...' : 'Đăng nhập nhân viên' }}
        </button>
      </div>
      <p v-if="staffError" class="error-text">{{ staffError }}</p>
    </section>
  </div>
</template>
