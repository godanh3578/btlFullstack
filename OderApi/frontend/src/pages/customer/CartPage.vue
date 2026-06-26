<script setup>
import {
  cart, selectedCart, selectedCartItems, allCartSelected, cartTotal, cartCount,
  showCheckoutPanel, checkout, checkoutShipping, checkoutBusy, checkoutMessage,
  vouchers, paymentMethods, currentUser, currentMemberTier, walletBalance,
  selectedVoucher, selectedPaymentMethod, voucherDiscountAmount, tierDiscountAmount,
  discountAmount, finalAmount, paidAmount, debtAmount, paymentStatusPreview,
  formatMoney, statusLabel, voucherAvailable, paymentMethodLabel,
  toggleAllCart, toggleCartItem, updateCartQuantity, removeFromCart,
  openCheckoutPanel, submitCheckout, openAuth, openPage
} from '../../composables/useAppState'
</script>

<template>
  <main :class="['page', showCheckoutPanel && cart.length > 0 ? 'two-column-page' : 'single-cart-page']">
    <section class="cart-panel">
      <div class="page-title">
        <span>Giỏ hàng</span>
        <h1>Kiểm tra sản phẩm trước khi thanh toán</h1>
      </div>

      <div v-if="cart.length === 0" class="empty-state compact">
        <svg width="80" height="80" viewBox="0 0 80 80" fill="none">
          <circle cx="40" cy="40" r="38" fill="#f8fafc" stroke="#e2e8f0" stroke-width="2"/>
          <path d="M22 28h4l5 22h18l4-16H29" stroke="#cbd5e1" stroke-width="2.5" stroke-linecap="round" stroke-linejoin="round"/>
          <circle cx="34" cy="54" r="2.5" fill="#94a3b8"/>
          <circle cx="46" cy="54" r="2.5" fill="#94a3b8"/>
          <path d="M26 34h28" stroke="#e2e8f0" stroke-width="1.5" stroke-linecap="round"/>
        </svg>
        <h3>Giỏ hàng đang trống</h3>
        <p>Chọn sản phẩm còn trong kho để bắt đầu đặt hàng.</p>
        <button class="primary-btn" type="button" @click="openPage('shop')">Mua hàng ngay</button>
      </div>

      <div v-else class="cart-list">
        <div class="cart-select-header">
          <label class="cart-checkbox-wrap">
            <input type="checkbox" :checked="allCartSelected" @change="toggleAllCart" class="cart-checkbox" />
            <span class="cart-cb-label">Chọn tất cả ({{ cart.length }} sản phẩm)</span>
          </label>
          <span class="cart-selected-count" v-if="selectedCart.length > 0 && !allCartSelected">
            Đã chọn {{ selectedCart.length }}/{{ cart.length }}
          </span>
        </div>

        <article v-for="item in cart" :key="item.productId"
          :class="['cart-line', { 'cart-line--unchecked': !selectedCartItems.has(item.productId) }]">
          <label class="cart-checkbox-wrap">
            <input type="checkbox"
              :checked="selectedCartItems.has(item.productId)"
              @change="toggleCartItem(item.productId)"
              class="cart-checkbox" />
          </label>
          <img :src="item.image" alt="" loading="lazy" />
          <div>
            <small>{{ item.productCode }} · {{ item.categoryName }}</small>
            <h3>{{ item.productName }}</h3>
            <p>Còn trong kho: {{ item.stock }}</p>
          </div>
          <div class="qty-box">
            <button type="button" @click="updateCartQuantity(item, item.quantity - 1)">-</button>
            <input :value="item.quantity" type="number" min="1" :max="item.stock" @input="updateCartQuantity(item, $event.target.value)" />
            <button type="button" @click="updateCartQuantity(item, item.quantity + 1)">+</button>
          </div>
          <b>{{ formatMoney(item.unitPrice * item.quantity) }}</b>
          <button class="remove-btn" type="button" @click="removeFromCart(item.productId)">Xóa</button>
        </article>

        <div class="cart-next-action">
          <span class="cart-subtotal" v-if="selectedCart.length > 0">
            Tổng chọn ({{ selectedCart.length }} sp): <b>{{ formatMoney(cartTotal) }}</b>
          </span>
          <button v-if="!showCheckoutPanel" class="primary-btn" type="button"
            :disabled="selectedCart.length === 0"
            @click="openCheckoutPanel">Mua hàng</button>
        </div>
      </div>
    </section>

    <aside v-if="showCheckoutPanel && cart.length > 0" class="checkout-panel">
      <h2>Thanh toán</h2>
      <p v-if="!currentUser" class="soft-alert">Bạn cần đăng nhập hoặc đăng ký trước khi đặt hàng.</p>

      <div class="checkout-fields">
        <label>
          Người nhận
          <input v-model="checkoutShipping.fullName" type="text" placeholder="Họ tên người nhận" :disabled="!currentUser" />
        </label>
        <label>
          Số điện thoại
          <input v-model="checkoutShipping.phone" type="text" placeholder="Số điện thoại" :disabled="!currentUser" />
        </label>
        <label>
          Địa chỉ nhận hàng
          <textarea v-model="checkoutShipping.address" rows="3" placeholder="Địa chỉ giao hàng" :disabled="!currentUser"></textarea>
        </label>
        <label>
          Mã giảm giá
          <select v-model="checkout.voucher" :disabled="!currentUser">
            <option v-for="voucher in vouchers" :key="voucher.code" :value="voucher.code" :disabled="!voucherAvailable(voucher)">
              {{ voucher.label }}
            </option>
          </select>
          <small>{{ selectedVoucher.description }}</small>
        </label>
        <label>
          Phương thức thanh toán
          <select v-model="checkout.paymentMethod" :disabled="!currentUser">
            <option v-for="method in paymentMethods" :key="method.value" :value="method.value">
              {{ method.label }}
            </option>
          </select>
          <small>{{ selectedPaymentMethod.note }}</small>
        </label>
        <label v-if="checkout.paymentMethod === 'Deposit'">
          Số tiền ứng cọc
          <input v-model.number="checkout.depositAmount" type="number" min="0" :max="finalAmount" :disabled="!currentUser" />
        </label>
        <div v-if="checkout.paymentMethod === 'QR'" style="text-align: center; margin: 16px 0; padding: 20px; background: #f8f9fa; border-radius: 12px; border: 1px dashed #ccc;">
          <p style="margin-bottom: 12px; font-weight: 600; color: #333; font-size: 15px;">Quét mã QR để thanh toán</p>
          <img :src="'https://api.qrserver.com/v1/create-qr-code/?size=200x200&data=ThanhToan_RetailERP_' + finalAmount" alt="QR Code thanh toán" style="width: 180px; height: 180px; display: block; margin: 0 auto; border-radius: 8px; border: 2px solid #e0e0e0;" />
          <p style="margin-top: 12px; color: #e74c3c; font-size: 18px; font-weight: 700;">{{ formatMoney(finalAmount) }}</p>
          <small style="color: #888;">Mã QR có giá trị trong 15 phút</small>
        </div>
      </div>

      <div class="money-box">
        <p><span>Tổng tiền hàng</span><b>{{ formatMoney(cartTotal) }}</b></p>
        <p><span>Voucher</span><b>- {{ formatMoney(voucherDiscountAmount) }}</b></p>
        <p><span>Ưu đãi hạng {{ currentMemberTier.name }}</span><b>- {{ formatMoney(tierDiscountAmount) }}</b></p>
        <p v-if="checkout.paymentMethod === 'Wallet'"><span>Số dư ví</span><b>{{ formatMoney(walletBalance) }}</b></p>
        <p><span>Đã thanh toán</span><b>{{ formatMoney(paidAmount) }}</b></p>
        <p v-if="debtAmount > 0"><span>Còn phải thu / công nợ</span><b>{{ formatMoney(debtAmount) }}</b></p>
        <p><span>Trạng thái thanh toán</span><b>{{ statusLabel(paymentStatusPreview) }}</b></p>
        <p class="final"><span>Cần thanh toán</span><b>{{ formatMoney(finalAmount) }}</b></p>
      </div>

      <p v-if="checkoutMessage" class="error-text">{{ checkoutMessage }}</p>
      <button class="primary-btn full" type="button" :disabled="checkoutBusy || selectedCart.length === 0" @click="currentUser ? submitCheckout() : openAuth('login')">
        {{ checkoutBusy ? 'Đang tạo đơn...' : (currentUser ? 'Xác nhận đặt hàng' : 'Đăng nhập để đặt hàng') }}
      </button>
    </aside>
  </main>
</template>
