<script setup>
import { Icon } from '@iconify/vue'
import { ref, computed, onUnmounted } from 'vue'
import {
  products, productLoading, productError, searchText, activeCategories,
  productSort, catalogPage, catalogPageCount, catalogPages,
  filteredProducts, categories, featuredProducts, pagedProducts, cartCount,
  productId, productName, productCode, productCategory, productPrice, productStock, productImage,
  formatMoney, addToCart, openPage, openProductDetail, scrollToCatalog,
  wishlist, recentlyViewed, toggleWishlist, isWishlisted, currentUser
} from '../../composables/useAppState'

const MAX_CATEGORIES_VISIBLE = 10
const showAllCategories = ref(false)

const visibleCategories = computed(() =>
  showAllCategories.value ? categories.value : categories.value.slice(0, MAX_CATEGORIES_VISIBLE)
)
const hasMoreCategories = computed(() => categories.value.length > MAX_CATEGORIES_VISIBLE)

function toggleCategory(name) {
  const s = new Set(activeCategories.value)
  s.has(name) ? s.delete(name) : s.add(name)
  activeCategories.value = s
  catalogPage.value = 1
  scrollToCatalog()
}

function clearCategories() {
  activeCategories.value = new Set()
  catalogPage.value = 1
}

const promoBanners = [
  '/banners/27eb383b-62ac-4552-a68e-d3a7ac2d371b.png',
  '/banners/ChatGPT Image 22_17_42 17 thg 6, 2026.png',
  '/banners/ChatGPT Image 22_17_51 17 thg 6, 2026.png',
  '/banners/ChatGPT Image 22_18_00 17 thg 6, 2026.png',
  '/banners/ChatGPT Image 22_18_42 17 thg 6, 2026.png',
]

const activeBanner = ref(0)
let bannerTimer = null

function startBannerTimer() {
  clearInterval(bannerTimer)
  bannerTimer = setInterval(() => {
    activeBanner.value = (activeBanner.value + 1) % promoBanners.length
  }, 7000)
}
startBannerTimer()
onUnmounted(() => clearInterval(bannerTimer))

function goToBanner(i) {
  activeBanner.value = i
  startBannerTimer()
}

function prevBanner() {
  activeBanner.value = (activeBanner.value - 1 + promoBanners.length) % promoBanners.length
  startBannerTimer()
}

function nextBanner() {
  activeBanner.value = (activeBanner.value + 1) % promoBanners.length
  startBannerTimer()
}

function _hash(s) {
  let h = 0
  const str = String(s || '')
  for (let i = 0; i < str.length; i++) h = (h * 31 + str.charCodeAt(i)) >>> 0
  return h
}
function productRating(product) {
  const h = _hash(productId(product) || productName(product))
  return (3.5 + (h % 16) / 10).toFixed(1)
}
function productReviewCount(product) {
  const h = _hash(String(productId(product)) + 'r')
  return 20 + (h % 230)
}
</script>

<template>
  <main class="shop-page">
    <!-- Promo Banner ảnh -->
    <section class="promo-banner-section">
      <div class="promo-banner">
        <img
          v-for="(src, i) in promoBanners"
          :key="src"
          :src="src"
          :class="['promo-banner-img', { active: activeBanner === i }]"
          alt=""
          loading="lazy"
        />
        <button class="promo-arrow promo-arrow--left" type="button" @click="prevBanner">&#8249;</button>
        <button class="promo-arrow promo-arrow--right" type="button" @click="nextBanner">&#8250;</button>
        <div class="promo-dots">
          <button
            v-for="(_, i) in promoBanners"
            :key="i"
            :class="['promo-dot', { active: activeBanner === i }]"
            type="button"
            @click="goToBanner(i)"
          ></button>
        </div>
      </div>
    </section>

    <section class="category-strip">
      <div class="category-strip-head">
        <div class="section-heading">
          <span>Danh mục</span>
          <h2>Chọn nhóm sản phẩm</h2>
        </div>
        <div v-if="activeCategories.size > 0" class="category-active-bar">
          <span class="category-active-count">{{ activeCategories.size }} đang chọn</span>
          <button class="ghost-btn small" type="button" @click="clearCategories">Bỏ tất cả</button>
        </div>
      </div>

      <div class="category-grid">
        <button
          v-for="category in visibleCategories"
          :key="category.name"
          :class="['category-card', { active: activeCategories.has(category.name) }]"
          type="button"
          @click="toggleCategory(category.name)"
        >
          <img :src="category.image" alt="" loading="lazy" />
          <b>{{ category.name }}</b>
          <span>{{ category.count }} sản phẩm</span>
          <span v-if="activeCategories.has(category.name)" class="cat-check">✓</span>
        </button>
      </div>

      <div v-if="hasMoreCategories" class="category-show-more">
        <button class="ghost-btn small" type="button" @click="showAllCategories = !showAllCategories">
          {{ showAllCategories ? 'Thu gọn' : `Xem thêm ${categories.length - MAX_CATEGORIES_VISIBLE} danh mục` }}
        </button>
      </div>
    </section>

    <section class="featured-section">
      <div class="section-heading">
        <span>Gợi ý hôm nay</span>
        <h2>Sản phẩm nổi bật</h2>
      </div>
      <div class="featured-grid">
        <article v-for="product in featuredProducts" :key="productId(product)" class="featured-card clickable-card" @click="openProductDetail(product)">
          <div class="card-img-wrap">
            <img :src="productImage(product)" alt="" loading="lazy" />
            <button class="wishlist-btn" type="button" :class="{ wishlisted: isWishlisted(product) }" @click.stop="toggleWishlist(product)" :aria-label="isWishlisted(product) ? 'Bỏ yêu thích' : 'Yêu thích'">♥</button>
          </div>
          <div>
            <small>{{ productCategory(product) }}</small>
            <h3>{{ productName(product) }}</h3>
            <div class="product-rating-row">
              <span class="stars-wrap">
                <span class="stars-bg">★★★★★</span>
                <span class="stars-fill" :style="`width:${productRating(product) / 5 * 100}%`">★★★★★</span>
              </span>
              <small class="review-count">{{ productRating(product) }} ({{ productReviewCount(product) }})</small>
            </div>
            <b>{{ formatMoney(productPrice(product)) }}</b>
            <button type="button" @click.stop="addToCart(product)">Thêm nhanh</button>
          </div>
        </article>
      </div>
    </section>

    <section id="catalog" class="catalog-section">
      <div class="catalog-head">
        <div class="section-heading">
          <span>Sản phẩm</span>
          <h2>Danh sách đang còn trong kho</h2>
        </div>
        <div class="catalog-tools">
          <template v-if="activeCategories.size > 0">
            <span
              v-for="cat in activeCategories"
              :key="cat"
              class="active-cat-tag"
            >
              {{ cat }}
              <button type="button" @click="toggleCategory(cat)">×</button>
            </span>
          </template>
          <select v-model="productSort">
            <option value="popular">Gợi ý</option>
            <option value="priceAsc">Giá thấp đến cao</option>
            <option value="priceDesc">Giá cao đến thấp</option>
            <option value="stockDesc">Tồn kho nhiều</option>
          </select>
        </div>
      </div>

      <p v-if="productError" class="soft-alert">{{ productError }}</p>

      <!-- Skeleton khi đang tải -->
      <div v-if="productLoading" class="product-grid">
        <div v-for="i in 8" :key="i" class="product-card skeleton-card">
          <div class="skeleton-img"></div>
          <div class="product-info">
            <div class="skeleton-line w-half"></div>
            <div class="skeleton-line"></div>
            <div class="skeleton-line w-three-quarter"></div>
            <div class="skeleton-row">
              <div class="skeleton-line w-third"></div>
              <div class="skeleton-btn"></div>
            </div>
          </div>
        </div>
      </div>

      <div v-else-if="filteredProducts.length === 0" class="empty-state">
        <img src="/sarab/off-img.jpg" alt="" />
        <h3>Không có sản phẩm trong kho</h3>
        <p>Thử đổi từ khóa tìm kiếm hoặc chọn danh mục khác.</p>
      </div>

      <div v-else class="product-grid">
        <article v-for="product in pagedProducts" :key="productId(product)" class="product-card clickable-card" @click="openProductDetail(product)">
          <div class="product-image">
            <img :src="productImage(product)" alt="" loading="lazy" />
            <span>{{ productStock(product) }} còn lại</span>
            <button class="wishlist-btn" type="button" :class="{ wishlisted: isWishlisted(product) }" @click.stop="toggleWishlist(product)" :aria-label="isWishlisted(product) ? 'Bỏ yêu thích' : 'Yêu thích'">♥</button>
          </div>
          <div class="product-info">
            <small>{{ productCode(product) }} · {{ productCategory(product) }}</small>
            <h3>{{ productName(product) }}</h3>
            <p>{{ product.manufacturerName }}</p>
            <div class="product-rating-row">
              <span class="stars-wrap">
                <span class="stars-bg">★★★★★</span>
                <span class="stars-fill" :style="`width:${productRating(product) / 5 * 100}%`">★★★★★</span>
              </span>
              <small class="review-count">{{ productRating(product) }} ({{ productReviewCount(product) }})</small>
            </div>
            <div class="product-row">
              <b>{{ formatMoney(productPrice(product)) }}</b>
              <button type="button" :disabled="productStock(product) <= 0" @click.stop="addToCart(product)">
                Thêm vào giỏ
              </button>
            </div>
          </div>
        </article>
      </div>

      <div v-if="catalogPageCount > 1" class="catalog-pagination" aria-label="Phân trang sản phẩm">
        <button
          v-for="page in catalogPages"
          :key="page"
          :class="{ active: catalogPage === page }"
          type="button"
          :aria-label="`Trang ${page}`"
          @click="catalogPage = page; scrollToCatalog()"
        ></button>
      </div>
    </section>

    <!-- Recently viewed -->
    <section v-if="currentUser && recentlyViewed.length > 0" class="recently-viewed-section">
      <div class="section-heading">
        <span>Lịch sử</span>
        <h2>Sản phẩm đã xem</h2>
      </div>
      <div class="recently-viewed-grid">
        <article v-for="product in recentlyViewed" :key="productId(product)" class="rv-card clickable-card" @click="openProductDetail(product)">
          <div class="rv-img-wrap">
            <img :src="productImage(product)" alt="" loading="lazy" />
          </div>
          <div class="rv-info">
            <small>{{ productCategory(product) }}</small>
            <p>{{ productName(product) }}</p>
            <b>{{ formatMoney(productPrice(product)) }}</b>
          </div>
        </article>
      </div>
    </section>
  </main>
</template>
