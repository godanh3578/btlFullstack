<script setup>
import {
  showAuthModal, authMode, authBusy, authError,
  loginForm, registerForm, forgotForm,
  closeAuth, openAuth, loginCustomer, registerCustomer, forgotStep1, forgotStep2
} from '../composables/useAppState'
</script>

<template>
  <div v-if="showAuthModal" class="modal-backdrop">
    <section class="modal-card auth-modal">
      <button class="modal-close" type="button" @click="closeAuth">×</button>

      <!-- Đăng nhập -->
      <template v-if="authMode === 'login'">
        <span class="eyebrow">Khách hàng</span>
        <h2>Đăng nhập</h2>
        <div class="form-grid">
          <label>Số điện thoại<input v-model="loginForm.phone" type="text" placeholder="0912 345 678" @keyup.enter="loginCustomer" /></label>
          <label>
            Mật khẩu
            <input v-model="loginForm.password" type="password" placeholder="••••••••" @keyup.enter="loginCustomer" />
          </label>
          <p v-if="authError" class="error-text">{{ authError }}</p>
          <button class="primary-btn full" type="button" :disabled="authBusy" @click="loginCustomer">
            {{ authBusy ? 'Đang đăng nhập...' : 'Đăng nhập' }}
          </button>
          <div class="auth-links">
            <button class="link-btn" type="button" @click="openAuth('forgot')">Quên mật khẩu?</button>
            <button class="link-btn" type="button" @click="authMode = 'register'; authError = ''">Chưa có tài khoản? Đăng ký</button>
          </div>
        </div>
      </template>

      <!-- Đăng ký -->
      <template v-else-if="authMode === 'register'">
        <span class="eyebrow">Khách hàng</span>
        <h2>Đăng ký tài khoản</h2>
        <div class="form-grid">
          <label>Họ tên<input v-model="registerForm.fullName" type="text" autocomplete="name" placeholder="Nguyễn Văn A" /></label>
          <label>Số điện thoại<input v-model="registerForm.phone" type="tel" autocomplete="tel" placeholder="0912 345 678" /></label>
          <label>Email<input v-model="registerForm.email" type="email" autocomplete="email" placeholder="email@example.com" /></label>
          <label>Địa chỉ<input v-model="registerForm.address" type="text" autocomplete="street-address" placeholder="123 Đường ABC, Quận 1" /></label>
          <label>
            Mật khẩu
            <input v-model="registerForm.password" type="password" autocomplete="new-password" placeholder="Tối thiểu 8 ký tự" />
            <small class="field-hint">Cần có chữ hoa, chữ thường, số và ký tự đặc biệt</small>
          </label>
          <p v-if="authError" class="error-text">{{ authError }}</p>
          <button class="primary-btn full" type="button" :disabled="authBusy" @click="registerCustomer">
            {{ authBusy ? 'Đang đăng ký...' : 'Đăng ký' }}
          </button>
          <button class="link-btn" type="button" @click="authMode = 'login'; authError = ''">Đã có tài khoản? Đăng nhập</button>
        </div>
      </template>

      <!-- Quên mật khẩu -->
      <template v-else-if="authMode === 'forgot'">
        <button class="forgot-back-btn" type="button" @click="openAuth('login')">← Quay lại</button>
        <span class="eyebrow">Khôi phục</span>
        <h2>Quên mật khẩu</h2>

        <div v-if="forgotForm.done" class="forgot-success">
          <div class="forgot-success-icon">✓</div>
          <p class="forgot-success-title">Đặt lại mật khẩu thành công!</p>
          <p class="forgot-success-sub">Bạn có thể đăng nhập với mật khẩu mới ngay bây giờ.</p>
          <button class="primary-btn full" type="button" @click="openAuth('login')">Đăng nhập ngay</button>
        </div>

        <template v-else>
          <div class="forgot-steps">
            <div :class="['forgot-step', { active: forgotForm.step >= 1, done: forgotForm.step > 1 }]">
              <span class="step-dot">{{ forgotForm.step > 1 ? '✓' : '1' }}</span>
              <span>Xác minh SĐT</span>
            </div>
            <div class="step-line"></div>
            <div :class="['forgot-step', { active: forgotForm.step >= 2 }]">
              <span class="step-dot">2</span>
              <span>Mật khẩu mới</span>
            </div>
          </div>

          <div v-if="forgotForm.step === 1" class="form-grid">
            <p class="forgot-desc">Nhập số điện thoại đã đăng ký tài khoản. Hệ thống sẽ cho phép bạn đặt mật khẩu mới.</p>
            <label>
              Số điện thoại
              <input v-model="forgotForm.phone" type="text" placeholder="0912 345 678" @keyup.enter="forgotStep1" />
            </label>
            <p v-if="forgotForm.error" class="error-text">{{ forgotForm.error }}</p>
            <button class="primary-btn full" type="button" :disabled="forgotForm.busy" @click="forgotStep1">
              {{ forgotForm.busy ? 'Đang kiểm tra...' : 'Tiếp tục' }}
            </button>
          </div>

          <div v-else class="form-grid">
            <p class="forgot-desc">
              SĐT <b>{{ forgotForm.phone }}</b> đã được xác minh. Hãy đặt mật khẩu mới.
            </p>
            <label>
              Mật khẩu mới
              <input v-model="forgotForm.newPassword" type="password" placeholder="Tối thiểu 8 ký tự" @keyup.enter="forgotStep2" />
              <small class="field-hint">Cần có chữ hoa, chữ thường, số và ký tự đặc biệt</small>
            </label>
            <label>
              Xác nhận mật khẩu
              <input v-model="forgotForm.confirmPassword" type="password" placeholder="Nhập lại mật khẩu" @keyup.enter="forgotStep2" />
            </label>
            <p v-if="forgotForm.error" class="error-text">{{ forgotForm.error }}</p>
            <button class="primary-btn full" type="button" @click="forgotStep2">Đặt lại mật khẩu</button>
            <button class="link-btn" type="button" @click="forgotForm.step = 1; forgotForm.error = ''">← Đổi số điện thoại</button>
          </div>
        </template>
      </template>
    </section>
  </div>
</template>
