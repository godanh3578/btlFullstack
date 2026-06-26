<script setup>
import { onMounted, watch } from 'vue'
import { useRoute } from 'vue-router'
import {
  currentUser, avatarUrl, activeAccountTab, accountForm, accountProfile, accountEditing,
  accountMessage, passwordForm, passwordMessage,
  parsedAddresses, accountUsername, maskedAccountEmail, maskedAccountPhone,
  walletBalance, currentMemberTier, nextMemberTier, tierProgressPercent,
  customerPurchaseHistory, customerDebts,
  walletTransactions, currentUserTopUpRequests, topUpRequests,
  addressForm, editingAddressIndex, birthDays, birthMonths, birthYears,
  memberTiers, topUpPaymentMethodLabel, showAddressModal,
  formatMoney, formatDateTime, statusLabel,
  saveAccount, changePassword, requestAccountDeletion, toggleAccountEdit,
  openTopUpModal, openAddressModal, editAddress, deleteAddress, submitAddress, closeAddressModal,
  handleFileChange
} from '../../composables/useAppState'

const route = useRoute()

onMounted(() => {
  if (route.query.tab) activeAccountTab.value = route.query.tab
})

watch(() => route.query.tab, (tab) => {
  if (tab) activeAccountTab.value = tab
})
</script>

<template>
  <main class="account-page">
    <aside class="account-sidebar">
      <div class="account-side-group">
        <button class="account-side-row active" type="button">
          <span class="side-icon user-icon" style="width: 36px; height: 36px; border-radius: 50%; overflow: hidden; display: inline-flex; align-items: center; justify-content: center; background: #f0f0f0; flex-shrink: 0;">
            <img v-if="avatarUrl" :src="avatarUrl" style="width: 100%; height: 100%; object-fit: cover;" />
            <span v-else style="font-size: 18px;">&#9817;</span>
          </span>
          <b>Tài Khoản Của Tôi</b>
        </button>
        <button :class="['account-sub', { active: activeAccountTab === 'profile' }]" type="button" @click="activeAccountTab = 'profile'">Hồ Sơ</button>
        <button :class="['account-sub', { active: activeAccountTab === 'wallet' }]" type="button" @click="activeAccountTab = 'wallet'">Ví RetailERP</button>
        <button :class="['account-sub', { active: activeAccountTab === 'address' }]" type="button" @click="activeAccountTab = 'address'">Địa Chỉ</button>
        <button :class="['account-sub', { active: activeAccountTab === 'password' }]" type="button" @click="activeAccountTab = 'password'">Đổi Mật Khẩu</button>
        <button :class="['account-sub', { active: activeAccountTab === 'privacy' }]" type="button" @click="activeAccountTab = 'privacy'">Những Thiết Lập Riêng Tư</button>
      </div>
    </aside>

    <section class="account-profile-card">
      <header v-if="activeAccountTab === 'profile'" class="profile-heading">
        <h1>Hồ Sơ Của Tôi</h1>
        <p>Quản lý thông tin hồ sơ để bảo mật tài khoản</p>
      </header>
      <header v-else-if="activeAccountTab === 'address'" class="profile-heading" style="display: flex; justify-content: space-between; align-items: flex-start;">
        <div>
          <h1>Địa Chỉ Của Tôi</h1>
          <p>Quản lý địa chỉ nhận hàng</p>
        </div>
        <button class="primary-btn" type="button" @click="openAddressModal" style="background-color: #ee4d2d; color: #fff; border: none; padding: 10px 20px; border-radius: 4px; font-size: 14px; font-weight: 500; cursor: pointer; box-shadow: 0 4px 10px rgba(238, 77, 45, 0.3);">+ Thêm địa chỉ mới</button>
      </header>
      <header v-else-if="activeAccountTab === 'password'" class="profile-heading">
        <h1>Đổi Mật Khẩu</h1>
        <p>Bảo mật tài khoản bằng cách thay đổi mật khẩu thường xuyên</p>
      </header>
      <header v-else-if="activeAccountTab === 'privacy'" class="profile-heading">
        <h1>Những thiết lập riêng tư</h1>
      </header>

      <div v-if="activeAccountTab === 'profile'" class="profile-content">
        <form class="profile-form" @submit.prevent="saveAccount">
          <div class="profile-row readonly">
            <label>Tên đăng nhập</label>
            <div class="profile-value">{{ accountUsername }}</div>
          </div>

          <div class="profile-row">
            <label for="profile-name">Tên</label>
            <input id="profile-name" v-model="accountForm.fullName" type="text" />
          </div>

          <div class="profile-row">
            <label for="profile-email">Email</label>
            <div v-if="accountEditing.email" class="profile-edit-line">
              <input id="profile-email" v-model="accountForm.email" type="email" placeholder="Nhập email" />
              <button type="button" @click="toggleAccountEdit('email')">Xong</button>
            </div>
            <div v-else class="profile-value">
              <span>{{ maskedAccountEmail }}</span>
              <button type="button" @click="toggleAccountEdit('email')">Thay Đổi</button>
            </div>
          </div>

          <div class="profile-row">
            <label for="profile-phone">Số điện thoại</label>
            <div v-if="accountEditing.phone" class="profile-edit-line">
              <input id="profile-phone" v-model="accountForm.phone" type="text" placeholder="Nhập số điện thoại" />
              <button type="button" @click="toggleAccountEdit('phone')">Xong</button>
            </div>
            <div v-else class="profile-value">
              <span>{{ maskedAccountPhone }}</span>
              <button type="button" @click="toggleAccountEdit('phone')">Thay Đổi</button>
            </div>
          </div>

          <div class="profile-row">
            <label>Giới tính</label>
            <div class="radio-line">
              <label><input v-model="accountProfile.gender" type="radio" value="Nam" /> Nam</label>
              <label><input v-model="accountProfile.gender" type="radio" value="Nữ" /> Nữ</label>
              <label><input v-model="accountProfile.gender" type="radio" value="Khác" /> Khác</label>
            </div>
          </div>

          <div class="profile-row">
            <label>Ngày sinh</label>
            <div class="birthday-line">
              <select v-model="accountProfile.day">
                <option value="">Ngày</option>
                <option v-for="day in birthDays" :key="day" :value="day">{{ day }}</option>
              </select>
              <select v-model="accountProfile.month">
                <option value="">Tháng</option>
                <option v-for="month in birthMonths" :key="month" :value="month">Tháng {{ month }}</option>
              </select>
              <select v-model="accountProfile.year">
                <option value="">Năm</option>
                <option v-for="year in birthYears" :key="year" :value="year">{{ year }}</option>
              </select>
            </div>
          </div>

          <div class="profile-actions">
            <button class="profile-save-btn" type="submit">Lưu</button>
            <p v-if="accountMessage">{{ accountMessage }}</p>
          </div>
        </form>

        <aside class="avatar-panel">
          <div class="avatar-preview" style="position: relative; overflow: hidden; width: 150px; height: 150px;">
            <img v-if="avatarUrl" :src="avatarUrl" alt="Ảnh đại diện" style="width: 100%; height: 100%; object-fit: cover; border-radius: 50%;" />
            <span v-else style="font-size: 48px; color: #ccc;">{{ (currentUser?.fullName || '?')[0].toUpperCase() }}</span>
          </div>
          <input
            type="file"
            id="avatar-file-input"
            @change="handleFileChange"
            accept="image/*"
            style="display: none;"
          />
          <label for="avatar-file-input" style="min-width: 142px; min-height: 54px; border: 1px solid #d1d5db; background: #fff; color: #374151; font-size: 18px; display: inline-flex; align-items: center; justify-content: center; cursor: pointer;">Chọn ảnh </label>
        </aside>

      </div>

      <div v-if="activeAccountTab === 'wallet'" class="account-summary-grid">
        <section class="profile-tier-summary">
          <article>
            <span>Tổng chi tiêu</span>
            <b>{{ formatMoney(currentUser?.totalSpent || 0) }}</b>
            <small>Hệ thống dùng số này để phân hạng thành viên.</small>
          </article>
          <article :class="['tier-summary-card', currentMemberTier.className]">
            <span>Hạng hiện tại</span>
            <b>{{ currentMemberTier.name }}</b>
            <small>Ưu đãi tự động {{ currentMemberTier.rate }}% khi thanh toán.</small>
          </article>
          <article>
            <span>Mốc kế tiếp</span>
            <b v-if="nextMemberTier">{{ nextMemberTier.name }}</b>
            <b v-else>Cao nhất</b>
            <small v-if="nextMemberTier">Còn {{ formatMoney(Math.max(0, nextMemberTier.minSpent - Number(currentUser?.totalSpent || 0))) }} để lên hạng.</small>
            <small v-else>Bạn đang ở hạng cao nhất.</small>
          </article>
          <div class="profile-tier-progress">
            <i :style="{ width: tierProgressPercent + '%' }"></i>
          </div>
        </section>
        <article class="wallet-card">
          <span>Ví RetailERP</span>
          <b>{{ formatMoney(walletBalance) }}</b>
          <div class="wallet-topup">
            <button type="button" @click="openTopUpModal">Gửi yêu cầu nạp</button>
          </div>
          <p v-if="currentUserTopUpRequests.some(request => request.status === 'pending')" class="wallet-pending-note">
            Có {{ currentUserTopUpRequests.filter(request => request.status === 'pending').length }} yêu cầu nạp đang chờ nhân viên duyệt.
          </p>
        </article>
        <article class="debt-card">
          <span>Công nợ hiện tại</span>
          <b>{{ formatMoney(currentUser?.currentDebt || 0) }}</b>
          <p>Công nợ phát sinh khi thanh toán một phần hoặc ví không đủ số dư.</p>
        </article>
      </div>

      <div v-if="activeAccountTab === 'wallet' && walletTransactions.length" class="wallet-history">
        <h3>Lịch sử giao dịch ví</h3>
        <div v-for="transaction in walletTransactions.slice(0, 4)" :key="transaction.id" class="wallet-transaction">
          <span>{{ transaction.note }}</span>
          <b :class="{ minus: transaction.amount < 0 }">{{ formatMoney(transaction.amount) }}</b>
          <small>{{ formatDateTime(transaction.createdAt) }}</small>
        </div>
      </div>

      <div v-if="activeAccountTab === 'wallet' && currentUserTopUpRequests.length" class="wallet-history">
        <h3>Yêu cầu nạp tiền</h3>
        <div v-for="request in currentUserTopUpRequests.slice(0, 4)" :key="request.id" class="wallet-transaction">
          <span>{{ request.id }}</span>
          <b>{{ formatMoney(request.amount) }}</b>
          <small>{{ topUpPaymentMethodLabel(request.paymentMethod) }} · {{ request.status === 'pending' ? 'Chờ duyệt' : request.status === 'approved' ? 'Đã duyệt' : 'Từ chối' }} · {{ formatDateTime(request.createdAt) }}</small>
        </div>
      </div>

      <div v-if="activeAccountTab === 'wallet' && !walletTransactions.length" class="wallet-history">
        <h3>Lịch sử giao dịch ví</h3>
        <p>Chưa có giao dịch ví.</p>
      </div>

      <div v-else-if="activeAccountTab === 'address'" class="profile-content" style="grid-template-columns: 1fr; gap: 0; padding-top: 16px;">
        <div class="address-list">
          <div v-if="parsedAddresses.length === 0" class="empty-address" style="text-align: center; padding: 40px; color: #888;">
            Bạn chưa có địa chỉ nào.
          </div>
          <div v-for="(addr, index) in parsedAddresses" :key="index" class="address-item" style="border-bottom: 1px solid #eee; padding: 16px 0; display: flex; justify-content: space-between; width: 100%;">
            <div class="address-info" style="flex: 1;">
              <div style="margin-bottom: 4px;">
                <b style="font-size: 1rem;">{{ addr.fullName }}</b> <span style="color: #666; margin-left: 8px;">| {{ addr.phone }}</span>
              </div>
              <p style="color: #555; margin: 4px 0; font-size: 0.9rem;">{{ addr.street }}</p>
              <p style="color: #555; margin: 4px 0; font-size: 0.9rem;">{{ addr.province }}</p>
              <span v-if="addr.isDefault" class="status-pill active" style="margin-top: 8px; font-size: 0.75rem; border: 1px solid #ee4d2d; color: #ee4d2d; background: transparent;">Mặc định</span>
              <span v-if="addr.type" class="status-pill" style="margin-top: 8px; font-size: 0.75rem; margin-left: 8px; border: 1px solid #888; color: #888; background: transparent;">{{ addr.type }}</span>
            </div>
            <div class="address-actions" style="display: flex; flex-direction: column; align-items: flex-end; justify-content: center; gap: 8px;">
              <div style="display: flex; gap: 16px;">
                <button type="button" @click="editAddress(index)" style="color: #0056b3; background: none; border: none; cursor: pointer;">Cập nhật</button>
                <button type="button" @click="deleteAddress(index)" style="color: #0056b3; background: none; border: none; cursor: pointer;">Xóa</button>
              </div>
              <button v-if="!addr.isDefault" type="button" @click="addressForm = { ...addr, isDefault: true }; editingAddressIndex = index; submitAddress()" style="background: none; border: 1px solid #ccc; padding: 4px 8px; cursor: pointer; border-radius: 4px; font-size: 0.8rem;">Thiết lập mặc định</button>
            </div>
          </div>
        </div>
      </div>

      <div v-else-if="activeAccountTab === 'password'" class="profile-content">
        <form class="profile-form" @submit.prevent="changePassword">
          <div class="profile-row">
            <label>Mật khẩu hiện tại</label>
            <input v-model="passwordForm.currentPassword" type="password" placeholder="Nhập mật khẩu hiện tại" />
          </div>
          <div class="profile-row">
            <label>Mật khẩu mới</label>
            <input v-model="passwordForm.newPassword" type="password" placeholder="Mật khẩu từ 4 ký tự" />
          </div>
          <div class="profile-row">
            <label>Xác nhận mật khẩu</label>
            <input v-model="passwordForm.confirmPassword" type="password" placeholder="Nhập lại mật khẩu mới" />
          </div>
          <div class="profile-actions">
            <button class="profile-save-btn" type="submit">Đổi Mật Khẩu</button>
            <p v-if="passwordMessage.text" :class="passwordMessage.type === 'error' ? 'error-text' : 'success-text'">{{ passwordMessage.text }}</p>
          </div>
        </form>
      </div>

      <div v-else-if="activeAccountTab === 'privacy'" class="profile-content" style="padding-top: 16px;">
        <div style="display: flex; justify-content: space-between; align-items: center; padding: 24px 0; border-top: 1px solid #eee;">
          <span style="font-size: 1rem; color: #333;">Yêu cầu xóa tài khoản</span>
          <button type="button" @click="requestAccountDeletion" style="background: #ee4d2d; color: #fff; border: none; padding: 8px 24px; border-radius: 4px; font-size: 0.9rem; cursor: pointer;">Xóa bỏ</button>
        </div>
      </div>
    </section>

    <!-- Address Modal (inline in AccountPage) -->
    <div v-if="showAddressModal" class="modal-backdrop" @click.self="closeAddressModal">
      <section class="modal-card" style="max-width: 600px;">
        <h2 style="font-size: 1.5rem; font-weight: 500; margin-bottom: 24px;">Địa chỉ mới</h2>
        <form @submit.prevent="submitAddress" style="display: flex; flex-direction: column; gap: 16px;">
          <div style="display: grid; grid-template-columns: 1fr 1fr; gap: 16px;">
            <input v-model="addressForm.fullName" type="text" placeholder="Họ và tên" required style="padding: 12px; border: 1px solid #ddd; border-radius: 4px; font-size: 1rem;" />
            <input v-model="addressForm.phone" type="text" placeholder="Số điện thoại" required style="padding: 12px; border: 1px solid #ddd; border-radius: 4px; font-size: 1rem;" />
          </div>

          <select v-model="addressForm.province" required style="padding: 12px; border: 1px solid #ddd; border-radius: 4px; font-size: 1rem; width: 100%; appearance: none; background: #fff url('data:image/svg+xml;utf8,<svg fill=%22black%22 height=%2224%22 viewBox=%220 0 24 24%22 width=%2224%22 xmlns=%22http://www.w3.org/2000/svg%22><path d=%22M7 10l5 5 5-5z%22/><path d=%22M0 0h24v24H0z%22 fill=%22none%22/></svg>') no-repeat right 8px center;">
            <option value="" disabled>Tỉnh/Thành Phố, Quận/Huyện</option>
            <option value="Hà Nội">Hà Nội</option>
            <option value="Hồ Chí Minh">Hồ Chí Minh</option>
            <option value="Đà Nẵng">Đà Nẵng</option>
            <option value="Hải Phòng">Hải Phòng</option>
            <option value="Cần Thơ">Cần Thơ</option>
            <option value="Bình Dương">Bình Dương</option>
            <option value="Đồng Nai">Đồng Nai</option>
          </select>

          <textarea v-model="addressForm.street" placeholder="Địa chỉ cụ thể" rows="3" required style="padding: 12px; border: 1px solid #ddd; border-radius: 4px; font-size: 1rem; width: 100%; resize: vertical;"></textarea>

          <div style="background: #f8f9fa; border: 1px dashed #ddd; border-radius: 4px; padding: 40px 0; text-align: center; position: relative; overflow: hidden;">
            <div style="position: absolute; top: 0; left: 0; right: 0; bottom: 0; opacity: 0.05; background-image: repeating-linear-gradient(45deg, transparent, transparent 10px, #000 10px, #000 20px);"></div>
            <button type="button" style="background: #fff; border: 1px solid #eee; padding: 8px 16px; border-radius: 4px; display: inline-flex; align-items: center; gap: 8px; cursor: pointer; position: relative; z-index: 1; color: #666; font-size: 0.9rem;">
              <span style="font-size: 1.2rem;">+</span> Thêm vị trí
            </button>
          </div>

          <div>
            <label style="display: block; margin-bottom: 8px; color: #555;">Loại địa chỉ:</label>
            <div style="display: flex; gap: 16px;">
              <button type="button" :style="{ padding: '8px 16px', border: addressForm.type === 'Nhà Riêng' ? '1px solid #ee4d2d' : '1px solid #ddd', background: '#fff', color: addressForm.type === 'Nhà Riêng' ? '#ee4d2d' : '#333', borderRadius: '4px', cursor: 'pointer' }" @click="addressForm.type = 'Nhà Riêng'">Nhà Riêng</button>
              <button type="button" :style="{ padding: '8px 16px', border: addressForm.type === 'Văn Phòng' ? '1px solid #ee4d2d' : '1px solid #ddd', background: '#fff', color: addressForm.type === 'Văn Phòng' ? '#ee4d2d' : '#333', borderRadius: '4px', cursor: 'pointer' }" @click="addressForm.type = 'Văn Phòng'">Văn Phòng</button>
            </div>
          </div>

          <label style="display: flex; align-items: center; gap: 8px; cursor: pointer; margin-top: 8px; color: #555;">
            <input type="checkbox" v-model="addressForm.isDefault" :disabled="parsedAddresses.length === 0" style="width: 18px; height: 18px; accent-color: #ee4d2d;" />
            Đặt làm địa chỉ mặc định
          </label>

          <div style="display: flex; justify-content: flex-end; gap: 16px; margin-top: 16px;">
            <button type="button" @click="closeAddressModal" style="padding: 10px 24px; background: none; border: none; cursor: pointer; font-size: 1rem; color: #333;">Trở Lại</button>
            <button type="submit" style="padding: 10px 24px; background: #ee4d2d; color: #fff; border: none; border-radius: 4px; cursor: pointer; font-size: 1rem; min-width: 120px;">Hoàn thành</button>
          </div>
        </form>
      </section>
    </div>
  </main>
</template>
