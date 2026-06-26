<script setup>
import { ref, watch } from 'vue'
import {
  selectedProduct, relatedProducts,
  productId, productName, productCode, productCategory, productPrice, productStock, productImage,
  formatMoney, addToCart, closeProductDetail, quickBuy
} from '../composables/useAppState'

const qty = ref(1)

watch(selectedProduct, () => { qty.value = 1 })

function decrease() { if (qty.value > 1) qty.value-- }
function increase() { if (qty.value < productStock(selectedProduct.value)) qty.value++ }
function clamp(val) { qty.value = Math.max(1, Math.min(Number(val) || 1, productStock(selectedProduct.value))) }
</script>

<template>
  <div v-if="selectedProduct" class="modal-backdrop" @click.self="closeProductDetail">
    <section class="modal-card product-modal">
      <button class="modal-close" type="button" @click="closeProductDetail">×</button>
      <div class="product-detail-grid">
        <img :src="productImage(selectedProduct)" alt="" />
        <div>
          <span class="eyebrow">{{ productCode(selectedProduct) }} · {{ productCategory(selectedProduct) }}</span>
          <h2>{{ productName(selectedProduct) }}</h2>
          <p>{{ selectedProduct.manufacturerName }}</p>
          <b class="detail-price">{{ formatMoney(productPrice(selectedProduct)) }}</b>
          <div class="detail-facts">
            <span>Còn trong kho: <b>{{ productStock(selectedProduct) }}</b></span>
            <span>Trạng thái: <b>{{ productStock(selectedProduct) > 0 ? 'Còn hàng' : 'Hết hàng' }}</b></span>
          </div>

          <div class="qty-row">
            <span class="qty-label">Số Lượng</span>
            <div class="qty-control">
              <button type="button" class="qty-btn" :disabled="qty <= 1" @click="decrease">−</button>
              <input type="number" class="qty-input" :value="qty" min="1" :max="productStock(selectedProduct)" @change="clamp($event.target.value)" />
              <button type="button" class="qty-btn" :disabled="qty >= productStock(selectedProduct)" @click="increase">+</button>
            </div>
          </div>

          <div class="detail-btn-row">
            <button class="primary-btn" type="button" :disabled="productStock(selectedProduct) <= 0" @click="addToCart(selectedProduct, qty); closeProductDetail()">
              {{ productStock(selectedProduct) > 0 ? 'Thêm vào giỏ' : 'Hết hàng' }}
            </button>
            <button class="quick-buy-btn" type="button" :disabled="productStock(selectedProduct) <= 0" @click="quickBuy(selectedProduct, qty)">
              Mua ngay
            </button>
          </div>
        </div>
      </div>

      <div v-if="relatedProducts.length" class="related-products">
        <h3>Sản phẩm cùng danh mục</h3>
        <div>
          <button v-for="product in relatedProducts" :key="productId(product)" type="button" @click="selectedProduct = product">
            <img :src="productImage(product)" alt="" />
            <span>{{ productName(product) }}</span>
            <b>{{ formatMoney(productPrice(product)) }}</b>
          </button>
        </div>
      </div>
    </section>
  </div>
</template>
