<script setup>
import { ref, computed } from 'vue'
import { currentUser, orderItems, productName, productImage, saveReview, formatDateTime } from '../composables/useAppState'

const props = defineProps({ order: Object })
const emit = defineEmits(['close', 'done'])

const STAR_LABELS = ['', 'Tệ', 'Không tốt', 'Bình thường', 'Tốt', 'Tuyệt vời']

const star = ref(0)
const hovered = ref(0)
const content = ref('')
const showName = ref(true)

const firstItem = computed(() => {
  const items = orderItems(props.order)
  return items[0] || null
})

const firstProduct = computed(() => {
  if (!firstItem.value) return null
  const item = firstItem.value
  return {
    name: item.productName || item.name || 'Sản phẩm',
    image: item.imageUrl || item.image || productImage(item) || ''
  }
})

function submit() {
  if (!star.value) return
  saveReview(props.order, {
    productStar: star.value,
    content: content.value.trim(),
    showName: showName.value
  })
  emit('done')
}
</script>

<template>
  <div class="modal-backdrop" @click.self="emit('close')">
    <section class="modal-card review-modal">
      <button class="modal-close" type="button" @click="emit('close')">×</button>
      <h2 class="review-modal-title">Đánh giá đơn hàng</h2>
      <p class="review-modal-sub">{{ order?.orderCode }} · {{ formatDateTime(order?.orderDate) }}</p>

      <div class="review-product-row" v-if="firstProduct">
        <img v-if="firstProduct.image" :src="firstProduct.image" class="review-product-img" alt="" />
        <div v-else class="review-product-img review-img-placeholder"></div>
        <span class="review-product-name">{{ firstProduct.name }}</span>
      </div>

      <div class="review-section-label">Chất lượng sản phẩm</div>

      <div class="review-stars">
        <button
          v-for="n in 5" :key="n"
          type="button"
          class="review-star-btn"
          :class="{ filled: n <= (hovered || star), hovered: n <= hovered }"
          @mouseenter="hovered = n"
          @mouseleave="hovered = 0"
          @click="star = n"
        >★</button>
        <span class="review-star-label">{{ STAR_LABELS[hovered || star] }}</span>
      </div>

      <textarea
        class="review-textarea"
        placeholder="Chia sẻ cảm nhận của bạn về sản phẩm này..."
        v-model="content"
        rows="4"
        maxlength="500"
      ></textarea>
      <div class="review-char-count">{{ content.length }}/500</div>

      <div class="review-options">
        <label class="review-checkbox-label">
          <input type="checkbox" v-model="showName" />
          <span>Hiển thị tên đăng nhập</span>
        </label>
        <p class="review-username-hint" v-if="showName">Tên hiển thị: <b>{{ currentUser?.username || currentUser?.email }}</b></p>
        <p class="review-username-hint" v-else>Đánh giá sẽ được hiển thị ẩn danh.</p>
      </div>

      <div class="review-modal-footer">
        <button type="button" class="btn-ghost" @click="emit('close')">Trở lại</button>
        <button type="button" class="primary-btn" :disabled="!star" @click="submit">Hoàn Thành</button>
      </div>
    </section>
  </div>
</template>
