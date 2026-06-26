import { computed, nextTick, ref } from 'vue'
import api, { API_BASE, getStaffToken, setStaffToken } from '../api/client'
import {
  loadCustomerCart,
  loadCustomerUser,
  loadDemoCustomer,
  saveCustomerCart,
  saveCustomerUser,
  saveDemoCustomer,
  saveDemoPassword,
  verifyDemoPassword
} from '../utils/customerStorage'
import router from '../router/index.js'

// --- Constants ----------------------------------------------------------------

export const pagePaths = {
  shop: '/',
  cart: '/cart',
  lookup: '/lookup',
  myOrders: '/my-orders',
  account: '/account',
  dashboard: '/manage/dashboard',
  orders: '/manage/orders',
  customers: '/manage/customers',
  suppliers: '/manage/suppliers',
  payments: '/manage/payments',
  debts: '/manage/debts',
  integration: '/manage/integration',
  warehouse: '/manage/warehouse'
}

export const staffPages = ['dashboard', 'orders', 'customers', 'suppliers', 'payments', 'debts', 'returns', 'invoices', 'integration', 'warehouse']

export const staffPageTitles = {
  dashboard: 'T?ng quan b�n h�ng',
  orders: 'Qu?n l� don h�ng',
  customers: 'Qu?n l� kh�ch h�ng',
  suppliers: 'Qu?n l� nh� cung c?p',
  payments: 'Thanh to�n',
  debts: 'C�ng n?',
  returns: 'Ho�n h�ng',
  invoices: 'H�a don',
  integration: '�?ng b? kho'
}

export const paymentMethods = [
  { value: 'Cash', label: 'Ti?n m?t khi nh?n h�ng', note: 'Kh�ch tr? tr?c ti?p khi nh?n h�ng, don t?m ghi c�n ph?i thu.' },
  { value: 'BankTransfer', label: 'Chuy?n kho?n ng�n h�ng', note: 'X�c nh?n thanh to�n d? khi t?o don.' },
  { value: 'QR', label: 'Thanh to�n QR', note: 'Thanh to�n d? qua m� QR c?a c?a h�ng.' },
  { value: 'Wallet', label: 'V� RetailERP', note: 'Tr? s? du v�, thi?u bao nhi�u s? ghi c�ng n?.' },
  { value: 'Deposit', label: '?ng c?c gi? don', note: 'Kh�ch tr? tru?c m?t ph?n d? gi? h�ng.' }
]

export const memberTiers = [
  { code: 'DEFAULT', name: 'Thu?ng', minSpent: 0, rate: 0, className: 'basic', badge: 'TH' },
  { code: 'SILVER', name: 'B?c', minSpent: 2000000, rate: 2, className: 'silver', badge: 'B' },
  { code: 'GOLD', name: 'V�ng', minSpent: 5000000, rate: 5, className: 'gold', badge: 'V' },
  { code: 'PLATINUM', name: 'B?ch Kim', minSpent: 8000000, rate: 7, className: 'platinum', badge: 'BK' },
  { code: 'DIAMOND', name: 'Kim Cuong', minSpent: 10000000, rate: 10, className: 'diamond', badge: 'KC' }
]

export const vouchers = [
  { code: 'NONE', label: 'Kh�ng d�ng m�', type: 'none', value: 0, description: 'Thanh to�n theo gi� ni�m y?t.' },
  { code: 'NEW10', label: 'NEW10 - kh�ch m?i gi?m 10%', type: 'percent', value: 10, max: 150000, description: 'Uu d�i cho kh�ch h�ng m?i.' },
  { code: 'SALE50', label: 'SALE50 - gi?m 50.000d', type: 'fixed', value: 50000, minAmount: 300000, description: '�p d?ng cho don t? 300.000d.' },
  { code: 'VIP5', label: 'VIP5 - h?ng V�ng/Kim cuong', type: 'percent', value: 5, minTier: 'V�ng', description: 'M� ri�ng cho kh�ch h�ng th�n thi?t.' }
]

export const demoProducts = [
  { productId: 1, productCode: 'GD001', productName: 'N?i com di?n Sunhouse 1.8L', categoryName: 'Gia d?ng', sellingPrice: 650000, quantityAvailable: 12, stockStatus: 'InStock', manufacturerName: 'Sunhouse' },
  { productId: 2, productCode: 'GD002', productName: 'M�y xay sinh t? Philips', categoryName: 'Gia d?ng', sellingPrice: 890000, quantityAvailable: 8, stockStatus: 'InStock', manufacturerName: 'Philips' },
  { productId: 3, productCode: 'GD003', productName: 'Qu?t di?n Panasonic', categoryName: 'Gia d?ng', sellingPrice: 780000, quantityAvailable: 0, stockStatus: 'OutOfStock', manufacturerName: 'Panasonic' },
  { productId: 4, productCode: 'DT001', productName: 'Chu?t Logitech M331', categoryName: '�i?n t?', sellingPrice: 320000, quantityAvailable: 30, stockStatus: 'InStock', manufacturerName: 'Logitech' },
  { productId: 5, productCode: 'DT002', productName: 'B�n ph�m co AKKO', categoryName: '�i?n t?', sellingPrice: 1290000, quantityAvailable: 9, stockStatus: 'InStock', manufacturerName: 'AKKO' },
  { productId: 6, productCode: 'TP001', productName: 'G?o ST25 t�i 5kg', categoryName: 'Th?c ph?m', sellingPrice: 185000, quantityAvailable: 25, stockStatus: 'InStock', manufacturerName: 'ST25' },
  { productId: 7, productCode: 'TT001', productName: '�o thun cotton nam', categoryName: 'Th?i trang', sellingPrice: 150000, quantityAvailable: 18, stockStatus: 'InStock', manufacturerName: '�?i t�c Th?i trang' },
  { productId: 8, productCode: 'VP001', productName: 'B�t bi Thi�n Long h?p 20 c�y', categoryName: 'Van ph�ng ph?m', sellingPrice: 65000, quantityAvailable: 50, stockStatus: 'InStock', manufacturerName: 'Thi�n Long' }
]

export const LOOKUP_STATUS_FILTERS = [
  { value: 'WaitingPayment', label: 'Ch? thanh to�n' },
  { value: 'Pending', label: 'Ch? x�c nh?n' },
  { value: 'Processing', label: '�ang x? l�' },
  { value: 'Shipping', label: 'V?n chuy?n' },
  { value: 'WaitingDelivery', label: 'Ch? giao h�ng' },
  { value: 'Completed', label: 'Ho�n th�nh' },
  { value: 'Cancelled', label: '�� h?y' },
]

export const MY_ORDERS_TABS = [
  { key: 'all', label: 'T?t c?' },
  { key: 'pending', label: 'Ch? x? l�' },
  { key: 'completed', label: 'Ho�n th�nh' },
  { key: 'cancelled', label: '�� h?y' },
]

export const myOrdersTab = ref('all')

function matchesTab(order, tab) {
  const s = String(order?.orderStatus || '').toLowerCase()
  const ps = String(order?.paymentStatus || '').toLowerCase()
  if (tab === 'pending')
    return ['pending', 'confirmed', 'waitingpayment', 'awaitingconfirmation',
      'awaitingpaymentconfirmation', 'processing', 'waitingdelivery'].includes(s)
      || ['pendingpayment', 'awaitingpaymentconfirmation'].includes(ps)
  if (tab === 'completed') return s === 'completed'
  if (tab === 'cancelled') return s === 'cancelled'
  return true
}

export const filteredMyOrders = computed(() => {
  const tab = myOrdersTab.value
  if (tab === 'all') return myOrders.value
  return myOrders.value.filter(o => matchesTab(o, tab))
})

const LOCAL_ORDERS_KEY = 'retailerpLocalOrders'
const LOCAL_STOCK_RESERVES_KEY = 'retailerpLocalStockReserves'
const WALLET_STATE_KEY = 'retailerpWalletState'
const WISHLIST_KEY = 'retailerpWishlist'
const RECENTLY_VIEWED_KEY = 'retailerpRecentlyViewed'
const TOPUP_REQUESTS_KEY = 'retailerpTopUpRequests'
const ACTIVITY_LOG_KEY = 'retailerpActivityLog'
const REVIEWS_KEY = 'retailerpOrderReviews'

const API_ASSET_BASE = API_BASE || ''
const USER_SERVICE_API_BASE = (import.meta.env.VITE_USER_API_URL || 'http://127.0.0.1:8083/api').replace(/\/$/, '')
let demoIdCounter = -1

// --- Module-level singleton state ---------------------------------------------

export const products = ref([])
export const productLoading = ref(false)
export const productError = ref('')
export const searchText = ref('')
export const activeCategories = ref(new Set())
export const productSort = ref('popular')
export const catalogPage = ref(1)
export const productsPerPage = 12

export const cart = ref(loadCustomerCart())
export const currentUser = ref(loadCustomerUser())
export const myOrders = ref([])
export const myOrdersLoading = ref(false)
export const selectedProduct = ref(null)
export const walletTransactions = ref([])
export const customerPurchaseHistory = ref([])
export const customerDebts = ref([])
export const showTopUpModal = ref(false)
export const walletTopUpForm = ref({ amount: 200000, paymentMethod: 'BankTransfer' })
export const topUpRequests = ref([])
export const activityLogs = ref([])
export const wishlist = ref(readJsonStorage(WISHLIST_KEY, []))
export const recentlyViewed = ref(readJsonStorage(RECENTLY_VIEWED_KEY, []))

export const showAuthModal = ref(false)
export const authMode = ref('login')
export const authBusy = ref(false)
export const authError = ref('')
export const loginForm = ref({ phone: '', password: '' })
export const registerForm = ref({ fullName: '', phone: '', email: '', address: '', password: '', gender: 0, dateOfBirth: null })
export const forgotForm = ref({ phone: '', newPassword: '', confirmPassword: '', step: 1, busy: false, error: '', done: false })
export const showUserMenu = ref(false)

export const showStaffModal = ref(false)
export const staffUser = ref(null)
export const staffError = ref('')
export const staffBusy = ref(false)
export const staffTierSaving = ref({})
export const supplierSaving = ref(false)
export const supplierEditingId = ref(null)
export const supplierForm = ref({
  supplierCode: '', supplierName: '', contactPerson: '',
  phone: '', email: '', address: '', taxCode: '', note: '', status: 'Active'
})
export const debtPaying = ref({})
export const debtPayForms = ref({})
export const staffLoginForm = ref({ email: 'sales.user@khopro.local', password: 'Sales@123' })

export const checkout = ref({ voucher: 'NONE', paymentMethod: 'Cash', depositAmount: 0 })
export const checkoutShipping = ref({ fullName: '', phone: '', address: '' })
export const checkoutBusy = ref(false)
export const checkoutMessage = ref('')
export const showCheckoutPanel = ref(false)

export const orderLookup = ref({
  mode: 'phone', orderCode: '', phone: '', productSearch: '', loading: false,
  error: '', result: null, results: [], statusFilter: '', expandedId: null
})

export const accountForm = ref({ fullName: '', phone: '', email: '', address: '' })
export const accountProfile = ref({ gender: '', day: null, month: null, year: null })
export const accountEditing = ref({ email: false, phone: false })
export const accountMessage = ref('')
export const activeAccountTab = ref('profile')
export const avatarUrl = ref('')

export const showAddressModal = ref(false)
export const editingAddressIndex = ref(-1)
export const addressForm = ref({
  fullName: '', phone: '', province: '', street: '', type: 'Nh� Ri�ng', isDefault: false
})

export const staffData = ref({
  orders: [], customers: [], suppliers: [], payments: [],
  debts: [], outbox: [], auditLogs: [], returns: [], invoices: []
})
export const returnStatusFilter = ref('')
export const returnSearch = ref('')
export const invoiceSearch = ref('')
export const invoiceStatusFilter = ref('')
export const staffLoading = ref(false)
export const integrationHealth = ref({
  gateway: { status: 'Unknown', detail: 'Chua kiem tra' },
  orderApi: { status: 'Unknown', detail: 'Chua kiem tra' },
  rabbitmq: { status: 'Unknown', detail: 'Chua kiem tra' }
})

export const notice = ref({ type: '', message: '' })
export const passwordForm = ref({ currentPassword: '', newPassword: '', confirmPassword: '' })
export const passwordMessage = ref({ type: '', text: '' })

export const selectedCartItems = ref(new Set(loadCustomerCart().map(i => i.productId)))

// --- Reactive computed (using router.currentRoute for route access) -----------

export const activePage = computed(() => router.currentRoute.value.meta?.page || 'shop')
export const isStaffPage = computed(() => staffPages.includes(activePage.value))

export const currentOrderDetail = computed(() => {
  const id = String(router.currentRoute.value.params?.id || '')
  if (!id) return null
  return [...myOrders.value, ...staffData.value.orders].find(order =>
    String(order.orderId || order.id || '') === id ||
    String(order.orderCode || '') === id
  ) || null
})

export const birthDays = Array.from({ length: 31 }, (_, i) => i + 1)
export const birthMonths = Array.from({ length: 12 }, (_, i) => i + 1)
export const birthYears = Array.from({ length: 70 }, (_, i) => new Date().getFullYear() - i)

export const walletBalance = computed(() => Number(currentUser.value?.walletBalance || 0))
export const currentMemberTier = computed(() => memberTierForUser(currentUser.value))
export const nextMemberTier = computed(() => memberTiers.find(t => t.minSpent > currentMemberTier.value.minSpent) || null)
export const currentUserTopUpRequests = computed(() => topUpRequests.value.filter(r => r.customerKey === walletKeyFor()))
export const pendingTopUpCount = computed(() => topUpRequests.value.filter(r => r.status === 'pending').length)

export const parsedAddresses = computed(() => {
  if (!accountForm.value.address) return []
  try {
    const arr = JSON.parse(accountForm.value.address)
    if (Array.isArray(arr)) return arr
    return []
  } catch {
    return [{ fullName: accountForm.value.fullName, phone: accountForm.value.phone, province: '', street: accountForm.value.address, type: 'Nh� Ri�ng', isDefault: true }]
  }
})

export const accountUsername = computed(() => {
  const phone = accountForm.value.phone || currentUser.value?.phone
  if (phone) return phone
  const name = accountForm.value.fullName || currentUser.value?.fullName || 'khachhang'
  return name.toLowerCase().replace(/\s+/g, '')
})

export const maskedAccountEmail = computed(() => maskMiddle(accountForm.value.email || currentUser.value?.email, '@'))
export const maskedAccountPhone = computed(() => maskTail(accountForm.value.phone || currentUser.value?.phone))

export const categories = computed(() => {
  const map = new Map()
  for (const p of products.value) {
    if (productBaseStock(p) <= 0) continue
    const cat = productCategory(p)
    map.set(cat, (map.get(cat) || 0) + 1)
  }
  return Array.from(map.entries())
    .map(([name, count], i) => ({ name, count, image: `/sarab/category-${(i % 6) + 1}.jpg` }))
    .sort((a, b) => a.name.localeCompare(b.name, 'vi'))
})

export const filteredProducts = computed(() => {
  const keyword = searchText.value.trim().toLowerCase()
  const filtered = products.value.filter(p => {
    if (productStock(p) <= 0) return false
    if (activeCategories.value.size > 0 && !activeCategories.value.has(productCategory(p))) return false
    return !keyword || productName(p).toLowerCase().includes(keyword) ||
      productCode(p).toLowerCase().includes(keyword) ||
      productCategory(p).toLowerCase().includes(keyword)
  })
  return filtered.sort((a, b) => {
    if (productSort.value === 'priceAsc') return productPrice(a) - productPrice(b)
    if (productSort.value === 'priceDesc') return productPrice(b) - productPrice(a)
    if (productSort.value === 'stockDesc') return productStock(b) - productStock(a)
    return productStock(b) - productStock(a) || productName(a).localeCompare(productName(b), 'vi')
  })
})

export const catalogPageCount = computed(() => Math.max(1, Math.ceil(filteredProducts.value.length / productsPerPage)))
export const pagedProducts = computed(() => {
  const page = Math.min(catalogPage.value, catalogPageCount.value)
  const start = (page - 1) * productsPerPage
  return filteredProducts.value.slice(start, start + productsPerPage)
})
export const catalogPages = computed(() => Array.from({ length: catalogPageCount.value }, (_, i) => i + 1))
export const featuredProducts = computed(() => [...products.value]
  .filter(p => productStock(p) > 0)
  .sort((a, b) => productPrice(b) - productPrice(a))
  .slice(0, 4))

export const selectedCart = computed(() => cart.value.filter(item => selectedCartItems.value.has(item.productId)))
export const allCartSelected = computed(() => cart.value.length > 0 && cart.value.every(item => selectedCartItems.value.has(item.productId)))
export const cartTotal = computed(() => selectedCart.value.reduce((sum, item) => sum + item.unitPrice * item.quantity, 0))
export const cartCount = computed(() => cart.value.length)

export const selectedVoucher = computed(() => {
  const v = vouchers.find(item => item.code === checkout.value.voucher) || vouchers[0]
  return voucherAvailable(v) ? v : vouchers[0]
})
export const voucherDiscountAmount = computed(() => {
  const v = selectedVoucher.value
  if (v.type === 'percent') {
    const amt = Math.round(cartTotal.value * v.value / 100)
    return v.max ? Math.min(amt, v.max) : amt
  }
  if (v.type === 'fixed') return Math.min(cartTotal.value, v.value)
  return 0
})
export const tierDiscountAmount = computed(() => {
  const base = Math.max(0, cartTotal.value - voucherDiscountAmount.value)
  return Math.round(base * currentMemberTier.value.rate / 100)
})
export const discountAmount = computed(() => Math.min(cartTotal.value, voucherDiscountAmount.value + tierDiscountAmount.value))
export const finalAmount = computed(() => Math.max(0, cartTotal.value - discountAmount.value))
export const selectedPaymentMethod = computed(() => paymentMethods.find(m => m.value === checkout.value.paymentMethod) || paymentMethods[0])
export const paidAmount = computed(() => {
  const amount = finalAmount.value
  const m = checkout.value.paymentMethod
  if (m === 'COD' || m === 'Cash') return 0
  if (m === 'BankTransfer' || m === 'QR') return 0
  if (m === 'Wallet') return walletBalance.value >= amount ? amount : 0
  if (m === 'Deposit') return Math.min(Math.max(Number(checkout.value.depositAmount || 0), 0), amount)
  return 0
})
export const debtAmount = computed(() => Math.max(0, finalAmount.value - paidAmount.value))
export const walletSufficient = computed(() => walletBalance.value >= finalAmount.value)
export const paymentStatusPreview = computed(() => {
  const m = checkout.value.paymentMethod
  if (m === 'COD' || m === 'Cash') return 'Unpaid'
  if (m === 'BankTransfer' || m === 'QR') return 'PendingPayment'
  if (m === 'Wallet') return walletSufficient.value ? 'Paid' : 'PendingPayment'
  if (m === 'Deposit') return paidAmount.value > 0 ? 'PartiallyPaid' : 'PendingPayment'
  return 'Unpaid'
})
export const orderStatusPreview = computed(() => {
  const m = checkout.value.paymentMethod
  if (m === 'BankTransfer' || m === 'QR') return 'WaitingPayment'
  if (m === 'Wallet' && !walletSufficient.value) return 'WaitingPayment'
  return 'AwaitingConfirmation'
})
export const relatedProducts = computed(() => {
  if (!selectedProduct.value) return []
  const cat = productCategory(selectedProduct.value)
  return products.value.filter(p => productId(p) !== productId(selectedProduct.value) && productStock(p) > 0 && productCategory(p) === cat).slice(0, 4)
})

export const staffOrdersForMetrics = computed(() => staffData.value.orders.length ? staffData.value.orders : myOrders.value)
export const staffDashboard = computed(() => {
  const orders = staffOrdersForMetrics.value
  const paidOrders = orders.filter(o => paymentStatusFor(o) === 'Paid')
  const cancelledOrders = orders.filter(o => String(o.orderStatus || '').toLowerCase() === 'cancelled')
  const pendingOrders = orders.filter(o => ['pending', 'debt', 'confirmed'].includes(String(o.orderStatus || '').toLowerCase()))
  return {
    totalOrders: orders.length,
    revenue: paidOrders.reduce((s, o) => s + orderTotal(o), 0),
    pending: pendingOrders.length,
    paid: paidOrders.length,
    cancelled: cancelledOrders.length,
    debt: orders.reduce((s, o) => s + orderDebt(o), 0),
    wallet: staffData.value.customers.reduce((s, c) => s + Number(c.walletBalance || 0), 0) || walletBalance.value,
    topCustomer: topCustomerName(orders)
  }
})

export const tierProgressPercent = computed(() => {
  if (!nextMemberTier.value) return 100
  const spent = Number(currentUser.value?.totalSpent || 0)
  const currentMin = currentMemberTier.value.minSpent
  const target = nextMemberTier.value.minSpent
  return Math.max(0, Math.min(100, Math.round(((spent - currentMin) / (target - currentMin)) * 100)))
})

export const filteredLookupResults = computed(() => {
  const { results, statusFilter, productSearch } = orderLookup.value
  let list = results
  if (statusFilter)
    list = list.filter(o => String(o.orderStatus || '').toLowerCase() === statusFilter.toLowerCase())
  if (productSearch.trim()) {
    const kw = productSearch.trim().toLowerCase()
    list = list.filter(o =>
      String(o.orderCode || '').toLowerCase().includes(kw) ||
      (o.items || []).some(it => String(it.productName || '').toLowerCase().includes(kw))
    )
  }
  return list
})

export const filteredReturns = computed(() => {
  let list = staffData.value.returns || []
  if (returnStatusFilter.value) list = list.filter(r => String(r.returnStatus || '').toLowerCase() === returnStatusFilter.value.toLowerCase())
  if (returnSearch.value) {
    const term = returnSearch.value.toLowerCase()
    list = list.filter(r => (r.returnCode || '').toLowerCase().includes(term) || (r.orderCode || '').toLowerCase().includes(term) || (r.customerName || '').toLowerCase().includes(term) || (r.customerPhone || '').includes(term))
  }
  return list
})

export const filteredInvoices = computed(() => {
  let list = staffData.value.invoices || []
  if (invoiceStatusFilter.value) list = list.filter(i => String(i.paymentStatus || '').toLowerCase() === invoiceStatusFilter.value.toLowerCase())
  if (invoiceSearch.value) {
    const term = invoiceSearch.value.toLowerCase()
    list = list.filter(i => (i.invoiceCode || '').toLowerCase().includes(term) || (i.orderCode || '').toLowerCase().includes(term) || (i.customerName || '').toLowerCase().includes(term))
  }
  return list
})

// --- Pure helper functions ----------------------------------------------------

export function productId(p) { return Number(p.productId || p.id || p.productStockCacheId) }
export function productName(p) { return p.productName || p.name || 'S?n ph?m' }
export function productCode(p) { return p.productCode || `SP${String(productId(p)).padStart(3, '0')}` }
export function productCategory(p) { return p.categoryName || p.category || 'Kh�c' }
export function productPrice(p) { return Number(p.sellingPrice ?? p.price ?? p.unitPrice ?? 0) }
export function productRawStock(p) { return Math.max(0, Number(p.sourceQuantityAvailable ?? p.quantityAvailable ?? p.stock ?? p.availableStock ?? 0)) }
export function productBaseStock(p) { return Math.max(0, Number(p.quantityAvailable ?? p.stock ?? p.availableStock ?? 0)) }
export function productStock(p) { return productBaseStock(p) }
export function productImage(p) {
  const img = p.productImage || p.image || p.imageUrl
  if (img) return backendAssetUrl(img)
  return `/sarab/menu-${((productId(p) - 1) % 6) + 1}.jpg`
}
export function backendAssetUrl(path) {
  if (!path) return ''
  if (/^(https?:|data:)/i.test(path)) return path
  return `${API_ASSET_BASE}${path.startsWith('/') ? path : `/${path}`}`
}
export function cartQuantityFor(p) {
  const line = cart.value.find(item => Number(item.productId) === productId(p))
  return line ? Number(line.quantity || 0) : 0
}

export function customerId(c) { return Number(c?.customerId || c?.id || 0) }
export function customerName(c) { return c?.fullName || c?.name || 'Kh�ch h�ng' }
export function memberTierFor(totalSpent) {
  return [...memberTiers].reverse().find(t => Number(totalSpent || 0) >= t.minSpent) || memberTiers[0]
}
export function memberTierByName(name) {
  const n = String(name || '').trim().toLowerCase()
  return memberTiers.find(t => t.name.toLowerCase() === n || t.code.toLowerCase() === n) || null
}
export function memberTierForUser(user) { return memberTierFor(Number(user?.totalSpent || 0)) }
export function customerMemberTier(customer) { return memberTierForUser(customer).name }
export function tierRank(name) { return memberTiers.findIndex(t => t.name === name) }

export function voucherAvailable(voucher) {
  if (!voucher || voucher.code === 'NONE') return true
  if (voucher.minAmount && cartTotal.value < voucher.minAmount) return false
  if (voucher.minTier && tierRank(currentMemberTier.value.name) < tierRank(voucher.minTier)) return false
  return true
}

export function paymentMethodLabel(method) {
  return paymentMethods.find(m => m.value === method)?.label || method || 'Ti?n m?t khi nh?n h�ng'
}
export function topUpPaymentMethodLabel(method) {
  if (method === 'BankTransfer') return 'Chuy?n kho?n'
  return method || 'Chuy?n kho?n'
}
export function backendPaymentMethod(method) {
  if (method === 'EWallet') return 'Wallet'
  return method || 'Cash'
}

export function paymentStatusFor(order) {
  const raw = order?.paymentStatus || order?.PaymentStatus
  if (raw) return String(raw)
  const os = String(order?.orderStatus || '').toLowerCase()
  const m = String(order?.paymentMethod || '').toLowerCase()
  if (os === 'waitingpayment') return m === 'banktransfer' || m === 'qr' ? 'PendingPayment' : 'PendingPayment'
  if (os === 'awaitingpaymentconfirmation') return 'AwaitingPaymentConfirmation'
  const total = orderTotal(order)
  const paid = Number(order?.paidAmount || 0)
  if (total <= 0 || paid >= total) return 'Paid'
  return paid > 0 ? 'PartiallyPaid' : 'Unpaid'
}

export function orderItems(order) { return order?.items || order?.orderDetails || order?.details || [] }
export function orderTotal(order) { return Number(order?.finalAmount ?? order?.totalAmount ?? 0) }
export function orderDebt(order) { return Math.max(0, Number(order?.debtAmount ?? order?.remainingAmount ?? 0)) }

export function topCustomerName(orders) {
  const map = new Map()
  for (const o of orders) {
    const name = o.customerName || o.customerId || 'Kh�ch l?'
    map.set(name, (map.get(name) || 0) + orderTotal(o))
  }
  return [...map.entries()].sort((a, b) => b[1] - a[1])[0]?.[0] || 'Chua c�'
}

export function formatMoney(value) { return Number(value || 0).toLocaleString('vi-VN') + 'd' }
export function formatDateTime(value) { if (!value) return ''; return new Date(value).toLocaleString('vi-VN') }

export function maskMiddle(value, separator = '') {
  const text = String(value || '').trim()
  if (!text) return 'Chua c?p nh?t'
  if (separator && text.includes(separator)) {
    const [first, ...rest] = text.split(separator)
    const domain = rest.join(separator)
    const visible = first.slice(0, 2)
    return `${visible}${'*'.repeat(Math.max(4, first.length - 2))}${separator}${domain}`
  }
  if (text.length <= 4) return `${text[0] || ''}***`
  return `${text.slice(0, 2)}${'*'.repeat(Math.max(4, text.length - 4))}${text.slice(-2)}`
}
export function maskTail(value) {
  const text = String(value || '').trim()
  if (!text) return 'Chua c?p nh?t'
  if (text.length <= 4) return `${text[0] || ''}***`
  return `${'*'.repeat(Math.max(6, text.length - 2))}${text.slice(-2)}`
}
export function maskStart(value) {
  const text = String(value || '').trim()
  if (!text) return '***'
  if (text.length <= 4) return `***${text.slice(-1)}`
  return `***${text.slice(-3)}`
}

export function statusLabel(status) {
  const v = String(status || '').toLowerCase()
  const labels = {
    // order statuses
    waitingpayment: 'Ch? x? l�',
    awaitingpaymentconfirmation: 'Ch? x�c nh?n TT',
    awaitingconfirmation: 'Ch? x? l�',
    processing: 'Ch? x? l�',
    shipping: 'Ch? x? l�',
    waitingdelivery: 'Ch? x? l�',
    completed: 'Ho�n th�nh',
    cancelled: '�� h?y',
    returned: '�� h?y',
    // payment statuses
    pendingpayment: 'Chua thanh to�n',
    unpaid: 'Chua thanh to�n',
    partiallypaid: 'Thanh to�n m?t ph?n',
    paid: '�� thanh to�n',
    refunded: '�� ho�n ti?n',
    // misc
    pending: 'Ch? x? l�', confirmed: 'Ch? x? l�',
    partial: 'Thanh to�n m?t ph?n', debt: 'C�n c�ng n?',
    failed: 'Th?t b?i', active: '�ang ho?t d?ng',
    inactive: 'Ng?ng ho?t d?ng', outofstock: 'H?t h�ng',
    depositpending: 'Ch? x�c nh?n c?c',
  }
  return labels[v] || status || 'Kh�ng x�c d?nh'
}
export function statusClass(status) {
  const v = String(status || '').toLowerCase()
  if (['ok', 'healthy', 'paid', 'completed', 'active', 'processed', 'refunded'].includes(v)) return 'ok'
  if (['degraded', 'pending', 'confirmed', 'partial', 'debt', 'unpaid', 'unknown',
       'waitingpayment', 'pendingpayment', 'awaitingconfirmation', 'awaitingpaymentconfirmation',
       'partiallypaid', 'depositpending', 'shipping', 'processing', 'waitingdelivery'].includes(v)) return 'warn'
  if (['down', 'unhealthy', 'cancelled', 'failed', 'blocked', 'outofstock', 'returned'].includes(v)) return 'bad'
  return 'info'
}

export function showNotice(message, type = 'ok') {
  notice.value = { message, type }
  window.setTimeout(() => {
    if (notice.value.message === message) notice.value = { type: '', message: '' }
  }, 3200)
}

// --- Navigation ---------------------------------------------------------------

export function openPage(page, tab) {
  if (['cart', 'myOrders', 'account'].includes(page) && !currentUser.value && page !== 'cart') {
    openAuth('login')
    return
  }
  if (staffPages.includes(page) && !staffUser.value) {
    openStaffAuth()
    return
  }
  const target = { path: pagePaths[page] || '/' }
  if (tab) target.query = { tab }
  router.push(target)
}

export function scrollToCatalog() {
  document.getElementById('catalog')?.scrollIntoView({ behavior: 'smooth', block: 'start' })
}

// --- localStorage helpers -----------------------------------------------------

export function readJsonStorage(key, fallback) {
  try { const raw = localStorage.getItem(key); return raw ? JSON.parse(raw) : fallback } catch { return fallback }
}
export function writeJsonStorage(key, value) { localStorage.setItem(key, JSON.stringify(value)) }

function readLocalStockReserves() { return readJsonStorage(LOCAL_STOCK_RESERVES_KEY, {}) }
function localReservedStock(id) { return Math.max(0, Number(readLocalStockReserves()[String(id)] || 0)) }
function saveLocalStockReserves(reserves) { writeJsonStorage(LOCAL_STOCK_RESERVES_KEY, reserves) }

export function isLocalOrder(order) {
  const id = order?.orderId || order?.id
  return Boolean(order?.isLocalDemo || (id && String(id).startsWith('local-')))
}

function refreshProductsFromLocalReserves() {
  products.value = products.value.map(p => {
    const id = productId(p)
    const rawStock = productRawStock(p)
    const stock = Math.max(0, rawStock - localReservedStock(id))
    return { ...p, quantityAvailable: stock, stockStatus: stock <= 0 ? 'OutOfStock' : stock <= 5 ? 'LowStock' : 'InStock' }
  })
  syncCartStock()
}

export function adjustLocalStockReserves(items = [], direction = 1) {
  const reserves = readLocalStockReserves()
  for (const item of items) {
    const id = Number(item.productId)
    const qty = Math.max(0, Number(item.quantity || 0))
    if (!id || qty <= 0) continue
    const key = String(id)
    const next = Math.max(0, Number(reserves[key] || 0) + direction * qty)
    if (next > 0) reserves[key] = next; else delete reserves[key]
  }
  saveLocalStockReserves(reserves)
  refreshProductsFromLocalReserves()
}

// --- Wallet helpers -----------------------------------------------------------

export function walletKeyFor(user = currentUser.value) { return String(user?.customerId || user?.phone || 'guest') }

export function loadWalletState(user = currentUser.value) {
  if (!user) { walletTransactions.value = []; return }
  const states = readJsonStorage(WALLET_STATE_KEY, {})
  const key = walletKeyFor(user)
  const state = states[key] || {}
  // Balance lu�n l?y t? server (user.walletBalance) � kh�ng override b?ng localStorage
  walletTransactions.value = state.transactions || []
}

export function saveWalletState() {
  if (!currentUser.value) return
  const states = readJsonStorage(WALLET_STATE_KEY, {})
  // Ch? luu transactions � balance l?y t? DB khi login
  states[walletKeyFor()] = { transactions: walletTransactions.value }
  writeJsonStorage(WALLET_STATE_KEY, states)
  saveCustomerUser(currentUser.value)
}

function saveWalletStateForCustomer(key, balance, transactions) {
  const states = readJsonStorage(WALLET_STATE_KEY, {})
  states[String(key || 'guest')] = { balance: Number(balance || 0), transactions: transactions || [] }
  writeJsonStorage(WALLET_STATE_KEY, states)
}

export function addWalletTransaction(type, amount, note, orderCode = '') {
  walletTransactions.value.unshift({ id: Date.now(), type, amount: Number(amount || 0), note, orderCode, createdAt: new Date().toISOString() })
  walletTransactions.value = walletTransactions.value.slice(0, 12)
  saveWalletState()
}

export function addActivityLog(action, note, orderCode = '') {
  activityLogs.value.unshift({
    id: Date.now(), action, note, orderCode,
    actor: staffUser.value?.username || currentUser.value?.fullName || 'Kh�ch h�ng',
    createdAt: new Date().toISOString()
  })
  activityLogs.value = activityLogs.value.slice(0, 20)
  writeJsonStorage(ACTIVITY_LOG_KEY, activityLogs.value)
}

// --- Product helpers ----------------------------------------------------------

function responseList(data) {
  if (Array.isArray(data)) return data
  if (Array.isArray(data?.items)) return data.items
  if (Array.isArray(data?.products)) return data.products
  if (Array.isArray(data?.data)) return data.data
  if (Array.isArray(data?.value)) return data.value
  return []
}

function categoryNameMap(cats = []) {
  return new Map(cats.map(c => [String(c.id || c.categoryId || ''), c.name || c.categoryName || '']).filter(([id, name]) => id && name))
}

export function normalizeProduct(p) {
  const id = productId(p)
  const rawStock = productRawStock(p)
  const stock = Math.max(0, rawStock - localReservedStock(id))
  return {
    ...p, productId: id, productCode: productCode(p), productName: productName(p),
    categoryName: productCategory(p), sellingPrice: productPrice(p),
    sourceQuantityAvailable: rawStock, quantityAvailable: stock,
    manufacturerName: p.manufacturerName || p.supplierName || 'Nh�m kho',
    stockStatus: stock <= 0 ? 'OutOfStock' : stock <= 5 ? 'LowStock' : 'InStock'
  }
}

function normalizeWarehouseProduct(p, index, categoryMap = new Map()) {
  const numericId = Number(p.productId || p.productID || p.productStockCacheId || 0) || index + 1
  const categoryId = String(p.categoryId || p.categoryID || '')
  const warehouseCategory = categoryMap.get(categoryId)
  const warehouseCode = p.productCode || p.code || p.sku
  return normalizeProduct({
    ...p, productId: numericId,
    externalProductId: p.externalProductId || p.id || p.productExternalId || '',
    productCode: warehouseCode || `SP${String(numericId).padStart(3, '0')}`,
    productName: p.productName || p.name || `San pham ${numericId}`,
    categoryName: p.categoryName || p.category || warehouseCategory || 'Kho',
    sellingPrice: Number(p.sellingPrice ?? p.price ?? p.unitPrice ?? 0),
    quantityAvailable: Number(p.quantityAvailable ?? p.stock ?? p.availableStock ?? 0),
    sourceQuantityAvailable: Number(p.quantityAvailable ?? p.stock ?? p.availableStock ?? 0),
    productImage: p.productImage || p.image || p.imageUrl || ''
  })
}

export async function loadProducts() {
  productLoading.value = true
  productError.value = ''
  try {
    const [warehouseRes, categoryRes] = await Promise.all([
      api.get('/api/products'),
      api.get('/api/categories').catch(() => ({ data: [] }))
    ])
    const warehouseList = responseList(warehouseRes.data)
    const warehouseCategories = categoryNameMap(responseList(categoryRes.data))
    if (warehouseList.length) {
      products.value = warehouseList.map((p, i) => normalizeWarehouseProduct(p, i, warehouseCategories))
      syncCartStock()
      productLoading.value = false
      return
    }
  } catch {}
  try {
    const res = await api.get('/api/ProductStockCaches')
    const list = responseList(res.data)
    products.value = (list.length ? list : demoProducts).map(normalizeProduct)
  } catch {
    products.value = demoProducts.map(normalizeProduct)
    productError.value = 'Chua k?t n?i du?c Inventory Service, dang d�ng d? li?u demo.'
  } finally {
    syncCartStock()
    productLoading.value = false
  }
}

export function syncCartStock() {
  const synced = []
  for (const item of cart.value) {
    const externalId = String(item.externalProductId || '')
    const currentName = String(item.productName || '')
    const p = products.value.find(p => externalId && String(p.externalProductId || p.id || '') === externalId)
      || products.value.find(p => currentName && productName(p) === currentName)
      || products.value.find(p => productId(p) === Number(item.productId))
    if (!p) continue
    const stock = productBaseStock(p)
    if (stock <= 0) continue
    item.productId = productId(p)
    item.externalProductId = p.externalProductId || p.id || ''
    item.productName = productName(p)
    item.productCode = productCode(p)
    item.categoryName = productCategory(p)
    item.unitPrice = productPrice(p)
    item.stock = stock
    item.image = productImage(p)
    item.quantity = Math.max(1, Math.min(Number(item.quantity || 1), stock))
    synced.push(item)
  }
  cart.value = synced
  saveCustomerCart(cart.value)
}

export function addToCart(p, qty = 1) {
  const stock = productStock(p)
  if (stock <= 0) { showNotice('S?n ph?m n�y d� h?t h�ng trong kho.', 'bad'); return }
  const addQty = Math.max(1, Math.min(Number(qty) || 1, stock))
  const id = productId(p)
  const existing = cart.value.find(item => Number(item.productId) === id)
  if (existing) {
    const next = existing.quantity + addQty
    if (next > productBaseStock(p)) { showNotice('S? lu?ng mua kh�ng du?c vu?t qu� t?n kho.', 'bad'); return }
    existing.quantity = next
  } else {
    cart.value.push({
      productId: id, externalProductId: p.externalProductId || p.id || '',
      productCode: productCode(p), productName: productName(p), categoryName: productCategory(p),
      unitPrice: productPrice(p), quantity: addQty, stock: productBaseStock(p), image: productImage(p)
    })
    const s = new Set(selectedCartItems.value)
    s.add(id)
    selectedCartItems.value = s
  }
  saveCustomerCart(cart.value)
  showNotice(`�� th�m ${addQty} s?n ph?m v�o gi? h�ng.`)
}

export function updateCartQuantity(item, value) {
  const qty = Math.floor(Number(value || 1))
  const p = products.value.find(p => productId(p) === Number(item.productId))
  const limit = p ? productBaseStock(p) : Number(item.stock || 1)
  if (qty > limit) { item.quantity = limit; showNotice('S? lu?ng mua kh�ng du?c vu?t qu� t?n kho.', 'bad') }
  else item.quantity = Math.max(1, qty)
  saveCustomerCart(cart.value)
}

export function removeFromCart(id) {
  cart.value = cart.value.filter(item => Number(item.productId) !== Number(id))
  if (cart.value.length === 0) showCheckoutPanel.value = false
  saveCustomerCart(cart.value)
}

export function clearCart() {
  cart.value = []
  showCheckoutPanel.value = false
  saveCustomerCart([])
}

export function clearSelectedFromCart() {
  const selectedIds = selectedCartItems.value
  cart.value = cart.value.filter(item => !selectedIds.has(item.productId))
  selectedCartItems.value = new Set()
  if (cart.value.length === 0) showCheckoutPanel.value = false
  saveCustomerCart(cart.value)
}

export function toggleCartItem(productId) {
  const s = new Set(selectedCartItems.value)
  s.has(productId) ? s.delete(productId) : s.add(productId)
  selectedCartItems.value = s
}

export function toggleAllCart() {
  if (allCartSelected.value) selectedCartItems.value = new Set()
  else selectedCartItems.value = new Set(cart.value.map(i => i.productId))
}

export function openProductDetail(p) {
  selectedProduct.value = p
  const id = productId(p)
  const filtered = recentlyViewed.value.filter(x => productId(x) !== id)
  recentlyViewed.value = [p, ...filtered].slice(0, 8)
  writeJsonStorage(RECENTLY_VIEWED_KEY, recentlyViewed.value)
}
export function closeProductDetail() { selectedProduct.value = null }

export function toggleWishlist(product) {
  const id = productId(product)
  if (wishlist.value.includes(id)) {
    wishlist.value = wishlist.value.filter(i => i !== id)
    showNotice('Dang nhap thanh cong.')
  } else {
    wishlist.value = [...wishlist.value, id]
    showNotice('Dang nhap thanh cong.')
  }
  writeJsonStorage(WISHLIST_KEY, wishlist.value)
}
export function isWishlisted(product) { return wishlist.value.includes(productId(product)) }

export function reorderItems(order) {
  const items = orderItems(order)
  if (!items.length) { showNotice('�on h�ng kh�ng c� s?n ph?m.', 'bad'); return }
  let added = 0
  for (const item of items) {
    const product = products.value.find(p =>
      productId(p) === Number(item.productId) ||
      productCode(p) === item.productCode ||
      productName(p) === (item.productName || item.name)
    )
    if (product && productStock(product) > 0) { addToCart(product); added++ }
  }
  showNotice(added > 0 ? `�� th�m l?i ${added} s?n ph?m v�o gi? h�ng.` : 'S?n ph?m d� h?t h�ng, kh�ng th? d?t l?i.', added > 0 ? 'ok' : 'bad')
}

// --- Reviews ------------------------------------------------------------------

function reviewsFor(user) {
  const all = readJsonStorage(REVIEWS_KEY, {})
  const key = user?.customerId || user?.id || user?.email || 'guest'
  return all[key] || {}
}

export function hasReviewed(order) {
  if (!currentUser.value) return false
  const id = order?.orderId || order?.id
  return !!reviewsFor(currentUser.value)[id]
}

export function getReview(order) {
  if (!currentUser.value) return null
  const id = order?.orderId || order?.id
  return reviewsFor(currentUser.value)[id] || null
}

export function saveReview(order, reviewData) {
  if (!currentUser.value) return
  const id = order?.orderId || order?.id
  const all = readJsonStorage(REVIEWS_KEY, {})
  const key = currentUser.value?.customerId || currentUser.value?.id || currentUser.value?.email || 'guest'
  if (!all[key]) all[key] = {}
  all[key][id] = { ...reviewData, orderId: id, reviewedAt: new Date().toISOString() }
  writeJsonStorage(REVIEWS_KEY, all)
}

export function canReviewOrder(order) {
  const s = String(order?.orderStatus || '').toLowerCase()
  return (s === 'completed' || s === 'confirmed') && !hasReviewed(order)
}

// --- Checkout -----------------------------------------------------------------

export function initCheckoutShipping() {
  let defaultAddress = currentUser.value?.address || ''
  try {
    const arr = JSON.parse(defaultAddress)
    if (Array.isArray(arr) && arr.length > 0) {
      const def = arr.find(a => a.isDefault) || arr[0]
      defaultAddress = def.province ? `${def.street}, ${def.province}` : def.street
    }
  } catch {}
  checkoutShipping.value = {
    fullName: currentUser.value?.fullName || '',
    phone: currentUser.value?.phone || '',
    address: defaultAddress
  }
  if (!checkout.value.depositAmount) checkout.value.depositAmount = Math.round(finalAmount.value * 0.3)
}

export async function quickBuy(p, qty = 1) {
  const stock = productStock(p)
  if (stock <= 0) { showNotice('S?n ph?m n�y d� h?t h�ng trong kho.', 'bad'); return }
  if (!currentUser.value) { selectedProduct.value = null; openAuth('login'); return }
  const addQty = Math.max(1, Math.min(Number(qty) || 1, stock))
  const id = productId(p)
  const existing = cart.value.find(item => Number(item.productId) === id)
  if (existing) {
    existing.quantity = Math.min(existing.quantity + addQty, productBaseStock(p))
  } else {
    cart.value.push({
      productId: id, externalProductId: p.externalProductId || p.id || '',
      productCode: productCode(p), productName: productName(p), categoryName: productCategory(p),
      unitPrice: productPrice(p), quantity: addQty, stock: productBaseStock(p), image: productImage(p)
    })
  }
  selectedCartItems.value = new Set([id])
  saveCustomerCart(cart.value)
  selectedProduct.value = null
  await router.push(pagePaths.cart)
  initCheckoutShipping()
  checkoutMessage.value = ''
  showCheckoutPanel.value = true
}

export function openCheckoutPanel() {
  if (cart.value.length === 0) return
  if (!currentUser.value) { openAuth('login'); return }
  initCheckoutShipping()
  checkoutMessage.value = ''
  showCheckoutPanel.value = true
}

function resetCheckoutShipping() {
  checkoutShipping.value = { fullName: '', phone: '', address: '' }
  checkout.value = { voucher: 'NONE', paymentMethod: 'Cash', depositAmount: 0 }
  checkoutMessage.value = ''
}

function checkoutProfilePayload() {
  return {
    fullName: (checkoutShipping.value.fullName || currentUser.value?.fullName || '').trim(),
    phone: (checkoutShipping.value.phone || currentUser.value?.phone || '').trim(),
    email: currentUser.value?.email || '',
    address: checkoutShipping.value.address.trim()
  }
}

// --- Auth ---------------------------------------------------------------------

export function openAuth(mode = 'login') {
  authMode.value = mode
  authError.value = ''
  if (mode === 'forgot') forgotForm.value = { phone: '', newPassword: '', confirmPassword: '', step: 1, busy: false, error: '', done: false }
  showAuthModal.value = true
}

export function closeAuth() { showAuthModal.value = false; authError.value = '' }

function validatePasswordStrength(pw) {
  if (pw.length < 8) return 'M?t kh?u ph?i c� �t nh?t 8 k� t?.'
  if (!/[A-Z]/.test(pw)) return 'C?n �t nh?t 1 ch? hoa.'
  if (!/[a-z]/.test(pw)) return 'C?n �t nh?t 1 ch? thu?ng.'
  if (!/\d/.test(pw)) return 'C?n �t nh?t 1 ch? s?.'
  if (!/[^A-Za-z0-9]/.test(pw)) return 'C?n �t nh?t 1 k� t? d?c bi?t (!@#$...).'
  return null
}

export async function forgotStep1() {
  const phone = forgotForm.value.phone.trim()
  forgotForm.value.error = ''
  if (!phone) { forgotForm.value.error = 'Vui l�ng nh?p s? di?n tho?i.'; return }
  if (!/^[0-9]{9,11}$/.test(phone.replace(/\s/g, ''))) { forgotForm.value.error = 'S? di?n tho?i kh�ng h?p l? (9�11 ch? s?).'; return }
  forgotForm.value.busy = true
  try {
    const res = await api.get('/api/Customers/exists', { params: { phone } })
    if (!res.data?.phoneExists) { forgotForm.value.error = 'S? di?n tho?i n�y chua du?c dang k� t�i kho?n.'; return }
    forgotForm.value.step = 2
  } catch { forgotForm.value.error = 'Kh�ng th? ki?m tra. Vui l�ng th? l?i.' }
  finally { forgotForm.value.busy = false }
}

export function forgotStep2() {
  const { newPassword, confirmPassword } = forgotForm.value
  forgotForm.value.error = ''
  const err = validatePasswordStrength(newPassword)
  if (err) { forgotForm.value.error = err; return }
  if (newPassword !== confirmPassword) { forgotForm.value.error = 'M?t kh?u x�c nh?n kh�ng kh?p.'; return }
  saveDemoPassword(forgotForm.value.phone.trim(), newPassword)
  forgotForm.value.done = true
}

export function setCurrentCustomer(customer) {
  currentUser.value = {
    role: 'Customer', customerId: customerId(customer), fullName: customerName(customer),
    phone: customer.phone || '', email: customer.email || '', address: customer.address || '',
    gender: customer.gender || '', dateOfBirth: customer.dateOfBirth || null,
    currentDebt: Number(customer.currentDebt || 0), totalSpent: Number(customer.totalSpent || 0),
    membershipTier: customer.membershipTier || '',
    walletBalance: Number(customer.walletBalance ?? currentUser.value?.walletBalance ?? 500000)
  }
  saveCustomerUser(currentUser.value)
  saveDemoCustomer(currentUser.value)
  loadWalletState(currentUser.value)
  initCheckoutShipping()
}

function createLocalDemoCustomer(profile) {
  return {
    customerId: demoIdCounter--,
    fullName: profile.fullName,
    phone: profile.phone,
    email: profile.email || '',
    address: profile.address || '',
    gender: profile.gender,
    dateOfBirth: profile.dateOfBirth,
    currentDebt: 0,
    totalSpent: 0,
    membershipTier: 'Thu?ng',
    walletBalance: 500000
  }
}

function markLocalOnlyCustomer(customer) {
  return customer ? { ...customer, isLocalOnly: true } : customer
}

function validateRegisterForm() {
  const fullName = registerForm.value.fullName.trim()
  const phone = registerForm.value.phone.trim()
  const email = registerForm.value.email.trim()
  const address = registerForm.value.address.trim()
  const password = registerForm.value.password
  if (fullName.length < 2 || !/^[\p{L}\s'.-]+$/u.test(fullName)) return 'Ho ten chi duoc gom chu cai, dau cach va toi thieu 2 ky tu.'
  if (!/^0\d{9}$/.test(phone)) return 'So dien thoai phai co 10 chu so va bat dau bang 0.'
  if (!/^[^\s@]+@[^\s@]+\.[^\s@]{2,}$/.test(email)) return 'Email khong dung dinh dang. Vi du: ten@example.com.'
  if (address.length < 5) return 'Dia chi phai co toi thieu 5 ky tu.'
  if (password.length < 8 || !/[A-Z]/.test(password) || !/[a-z]/.test(password) || !/\d/.test(password) || !/[^A-Za-z0-9]/.test(password)) return 'Mat khau phai tu 8 ky tu, co chu hoa, chu thuong, so va ky tu dac biet.'
  return ''
}

async function checkRegistrationAvailability() {
  const params = new URLSearchParams({ phone: registerForm.value.phone.trim(), email: registerForm.value.email.trim() })
  try {
    const res = await api.get(`/api/Customers/exists?${params.toString()}`)
    if (res.data?.phoneExists) return 'So dien thoai nay da duoc dang ky.'
    if (res.data?.emailExists) return 'Email nay da duoc dang ky.'
  } catch (error) {
    console.warn('Customer existence check skipped:', error.message)
  }
  return ''
}

async function syncCustomerToUserService() {
  try {
    await api.post('/api/auth/register', {
      email: registerForm.value.email.trim(), password: registerForm.value.password,
      name: registerForm.value.fullName.trim(), storeName: 'OderApi', phone: registerForm.value.phone.trim(), province: ''
    })
  } catch (error) {
    if (error.response?.status === 409) throw new Error(error.response?.data?.message || 'Email n�y d� du?c dang k� ? h? th?ng qu?n l� ngu?i d�ng.')
    // N3 down ho?c l?i m?ng ? ch? warn, kh�ng block dang k�
    console.warn('N3 sync skipped (N3 may be down):', error.message)
  }
}

async function syncCustomerToUserServiceFast() {
  const payload = {
    email: registerForm.value.email.trim(),
    password: registerForm.value.password,
    name: registerForm.value.fullName.trim(),
    storeName: 'OderApi',
    phone: registerForm.value.phone.trim(),
    province: ''
  }

  const directUrl = `${USER_SERVICE_API_BASE}/auth/register`

  try {
    const controller = new AbortController()
    const timer = setTimeout(() => controller.abort(), 3500)
    try {
      const response = await fetch(directUrl, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(payload),
        signal: controller.signal
      })

      const raw = await response.text()
      const data = raw ? JSON.parse(raw) : null
      if (!response.ok) {
        const error = new Error(data?.message || `${response.status} ${response.statusText}`)
        error.response = { status: response.status, data }
        throw error
      }
      return data
    } finally {
      clearTimeout(timer)
    }
  } catch (directError) {
    if (directError?.response?.status === 409) {
      throw new Error(directError.response?.data?.message || 'Email này đã được đăng ký ở hệ thống quản lý người dùng.')
    }

    try {
      await api.post('/api/auth/register', payload)
      return
    } catch (fallbackError) {
      if (fallbackError?.response?.status === 409) {
        throw new Error(fallbackError.response?.data?.message || 'Email này đã được đăng ký ở hệ thống quản lý người dùng.')
      }
      console.warn('N3 sync skipped after retries:', fallbackError.message)
    }
  }
}

export async function registerCustomer() {
  authBusy.value = true; authError.value = ''
  const validationMessage = validateRegisterForm()
  if (validationMessage) { authError.value = validationMessage; authBusy.value = false; return }
  try {
    const availabilityMessage = await checkRegistrationAvailability()
    if (availabilityMessage) { authError.value = availabilityMessage; return }

    const payload = {
      fullName: registerForm.value.fullName.trim(),
      phone: registerForm.value.phone.trim(),
      email: registerForm.value.email.trim(),
      address: registerForm.value.address.trim(),
      gender: registerForm.value.gender.toString(),
      dateOfBirth: registerForm.value.dateOfBirth ? new Date(registerForm.value.dateOfBirth).toISOString().split('T')[0] : null
    }

    await syncCustomerToUserServiceFast()

    let customer = null
    try {
      const res = await api.post('/api/Customers', payload)
      customer = res.data
    } catch (error) {
      console.warn('Customer API unavailable, falling back to local customer:', error.message)
      customer = markLocalOnlyCustomer(createLocalDemoCustomer(payload))
    }

    saveDemoPassword(registerForm.value.phone.trim(), registerForm.value.password)
    setCurrentCustomer(customer)
    await loadCustomerAccountData()
    registerForm.value = { fullName: '', phone: '', email: '', address: '', password: '', gender: 0 }
    closeAuth()
    showNotice(customer?.isLocalOnly ? 'Dang ky tam thoi tren may nay. He thong khach hang dang mat ket noi DB.' : 'Dang ky thanh cong.')
    await loadMyOrders()
  } catch (error) {
    authError.value = error.response?.data?.message || error.message || 'Dang ky that bai. Vui long thu lai.'
  } finally { authBusy.value = false }
}

export async function loginCustomer() {
  authBusy.value = true; authError.value = ''
  if (!loginForm.value.phone.trim()) { authError.value = 'Vui long nhap so dien thoai.'; authBusy.value = false; return }
  try {
    const res = await api.post('/api/Customers/Login', { phone: loginForm.value.phone.trim() })
    setCurrentCustomer(res.data)
    await loadCustomerAccountData()
    loadAvatar(res.data)
    loginForm.value = { phone: '', password: '' }
    closeAuth()
    showNotice('Dang nhap thanh cong.')
    await loadMyOrders()
  } catch (error) {
    const localCustomer = loadDemoCustomer(loginForm.value.phone.trim())
    if (localCustomer && verifyDemoPassword(loginForm.value.phone.trim(), loginForm.value.password)) {
      setCurrentCustomer(markLocalOnlyCustomer(localCustomer))
      await loadCustomerAccountData()
      loginForm.value = { phone: '', password: '' }
      closeAuth()
      showNotice('Dang nhap local thanh cong.')
      return
    }
    authError.value = error.response?.data?.message || 'Khong tim thay khach hang. Vui long dang ky.'
  } finally { authBusy.value = false }
}

export function logoutCustomer() {
  currentUser.value = null; avatarUrl.value = ''
  saveCustomerUser(null)
  myOrders.value = []; customerPurchaseHistory.value = []; customerDebts.value = []
  recentlyViewed.value = []; writeJsonStorage(RECENTLY_VIEWED_KEY, [])
  resetCheckoutShipping()
  showNotice('Dang nhap thanh cong.')
  if (['myOrders', 'account'].includes(activePage.value)) openPage('shop')
}

// --- Customer account ---------------------------------------------------------

export function avatarKey(user = currentUser.value) { return user ? `avatar_${user.customerId}` : null }

export function loadAvatar(user = currentUser.value) {
  if (user?.avatarUrl) { avatarUrl.value = backendAssetUrl(user.avatarUrl); return }
  const key = avatarKey(user)
  avatarUrl.value = key ? (localStorage.getItem(key) || '') : ''
}

export async function handleFileChange(event) {
  const file = event.target.files[0]
  if (!file) return
  const formData = new FormData()
  formData.append('file', file)
  try {
    const res = await api.post(`/api/Customers/${currentUser.value.customerId}/avatar`, formData, { headers: { 'Content-Type': 'multipart/form-data' } })
    avatarUrl.value = backendAssetUrl(res.data.avatarUrl)
    currentUser.value.avatarUrl = res.data.avatarUrl
    saveCustomerUser(currentUser.value)
    showNotice('Dang nhap thanh cong.')
  } catch { showNotice('Dang nhap thanh cong.') }
}

export async function loadCustomerAccountData() {
  if (!currentUser.value?.customerId || currentUser.value.isLocalOnly) { customerPurchaseHistory.value = []; customerDebts.value = []; return }
  const id = currentUser.value.customerId
  try {
    const [history, debts] = await Promise.all([
      api.get(`/api/Customers/${id}/purchase-history`).catch(() => ({ data: { orders: [] } })),
      api.get(`/api/Customers/${id}/debts`).catch(() => ({ data: { debts: [] } }))
    ])
    customerPurchaseHistory.value = history.data?.orders || []
    customerDebts.value = debts.data?.debts || []
  } catch { customerPurchaseHistory.value = []; customerDebts.value = [] }
}

export function initAccountForm() {
  accountForm.value = {
    fullName: currentUser.value?.fullName || '', phone: currentUser.value?.phone || '',
    email: currentUser.value?.email || '', address: currentUser.value?.address || '',
    gender: currentUser.value?.gender || '', dateOfBirth: currentUser.value?.dateOfBirth || ''
  }
  const dob = currentUser.value?.dateOfBirth ? new Date(currentUser.value.dateOfBirth) : null
  accountProfile.value = {
    gender: currentUser.value?.gender || '',
    day: dob ? dob.getDate() : '', month: dob ? dob.getMonth() + 1 : '', year: dob ? dob.getFullYear() : ''
  }
  accountEditing.value = { email: false, phone: false }
}

export function initProfileData() {
  if (!currentUser.value) return
  accountForm.value.fullName = currentUser.value.fullName || ''
  accountForm.value.phone = currentUser.value.phone || ''
  accountForm.value.email = currentUser.value.email || ''
  accountForm.value.address = currentUser.value.address || ''
  accountProfile.value.gender = currentUser.value.gender || ''
  if (currentUser.value.dateOfBirth) {
    const pureDate = currentUser.value.dateOfBirth.split('T')[0]
    const parts = pureDate.split('-')
    accountProfile.value.year = parseInt(parts[0])
    accountProfile.value.month = parseInt(parts[1])
    accountProfile.value.day = parseInt(parts[2])
  } else {
    accountProfile.value.year = ''; accountProfile.value.month = ''; accountProfile.value.day = ''
  }
}

export function toggleAccountEdit(field) { accountEditing.value[field] = !accountEditing.value[field] }

export async function saveAccount() {
  accountMessage.value = ''
  if (!currentUser.value?.customerId) return
  if (!accountForm.value.fullName.trim()) { accountMessage.value = 'Vui l�ng nh?p t�n kh�ch h�ng.'; return }
  if (!accountForm.value.phone.trim()) { accountMessage.value = 'Vui l�ng nh?p s? di?n tho?i.'; accountEditing.value.phone = true; return }
  try {
    const dobString = (accountProfile.value.year && accountProfile.value.month && accountProfile.value.day)
      ? `${accountProfile.value.year}-${String(accountProfile.value.month).padStart(2, '0')}-${String(accountProfile.value.day).padStart(2, '0')}`
      : null
    const res = await api.put(`/api/Customers/${currentUser.value.customerId}/profile`, {
      fullName: accountForm.value.fullName.trim(), phone: accountForm.value.phone.trim(),
      email: accountForm.value.email.trim(), address: accountForm.value.address.trim(),
      gender: accountProfile.value.gender, dateOfBirth: dobString
    })
    setCurrentCustomer(res.data)
    if (res.data?.dateOfBirth) {
      const parts = res.data.dateOfBirth.split('-')
      await nextTick()
      accountProfile.value.year = parseInt(parts[0])
      accountProfile.value.month = parseInt(parts[1])
      accountProfile.value.day = parseInt(parts[2])
    }
    accountEditing.value = { email: false, phone: false }
    accountMessage.value = '�� luu th�ng tin t�i kho?n.'
    showNotice('Dang nhap thanh cong.')
  } catch (error) { accountMessage.value = error.response?.data?.message || 'Kh�ng luu du?c th�ng tin.' }
}

export function changePassword() {
  passwordMessage.value = { type: '', text: '' }
  const phone = currentUser.value?.phone
  if (!phone) return
  if (!verifyDemoPassword(phone, passwordForm.value.currentPassword)) { passwordMessage.value = { type: 'error', text: 'M?t kh?u hi?n t?i kh�ng d�ng.' }; return }
  if (!passwordForm.value.newPassword) { passwordMessage.value = { type: 'error', text: 'Vui l�ng nh?p m?t kh?u m?i.' }; return }
  if (passwordForm.value.newPassword !== passwordForm.value.confirmPassword) { passwordMessage.value = { type: 'error', text: 'M?t kh?u x�c nh?n kh�ng kh?p.' }; return }
  saveDemoPassword(phone, passwordForm.value.newPassword)
  passwordForm.value = { currentPassword: '', newPassword: '', confirmPassword: '' }
  passwordMessage.value = { type: 'success', text: '�?i m?t kh?u th�nh c�ng.' }
  showNotice('Dang nhap thanh cong.')
}

export async function requestAccountDeletion() {
  if (!confirm('B?n c� ch?c ch?n mu?n y�u c?u x�a t�i kho?n n�y? H�nh d?ng n�y kh�ng th? ho�n t�c!')) return
  try {
    if (currentUser.value?.customerId) await api.delete(`/api/Customers/${currentUser.value.customerId}`)
    showNotice('Dang nhap thanh cong.')
    logoutCustomer()
  } catch (error) { showNotice(error.response?.data?.message || 'C� l?i x?y ra khi y�u c?u x�a t�i kho?n.') }
}

// --- Address helpers ----------------------------------------------------------

export function openAddressModal() {
  addressForm.value = { fullName: '', phone: '', province: '', street: '', type: 'Nh� Ri�ng', isDefault: parsedAddresses.value.length === 0 }
  editingAddressIndex.value = -1
  showAddressModal.value = true
}
export function closeAddressModal() { showAddressModal.value = false }

export function submitAddress() {
  let list = [...parsedAddresses.value]
  if (addressForm.value.isDefault) list.forEach(a => a.isDefault = false)
  if (editingAddressIndex.value >= 0) list[editingAddressIndex.value] = { ...addressForm.value }
  else list.push({ ...addressForm.value })
  if (list.length > 0 && !list.some(a => a.isDefault)) list[0].isDefault = true
  accountForm.value.address = JSON.stringify(list)
  saveAccount()
  closeAddressModal()
}

export function editAddress(index) {
  addressForm.value = { ...parsedAddresses.value[index] }
  editingAddressIndex.value = index
  showAddressModal.value = true
}

export function deleteAddress(index) {
  if (!confirm('B?n c� ch?c ch?n mu?n x�a d?a ch? n�y?')) return
  let list = [...parsedAddresses.value]
  list.splice(index, 1)
  if (list.length > 0 && !list.some(a => a.isDefault)) list[0].isDefault = true
  accountForm.value.address = JSON.stringify(list)
  saveAccount()
}

// --- Orders -------------------------------------------------------------------

export function loadLocalOrders(customer = currentUser.value) {
  if (!customer) return []
  const orders = readJsonStorage(LOCAL_ORDERS_KEY, [])
  const key = String(customer.customerId || customer.phone)
  return orders.filter(o => String(o.customerId) === key || String(o.customerPhone) === String(customer.phone))
}

function loadAllLocalOrders() { return readJsonStorage(LOCAL_ORDERS_KEY, []) }

function saveLocalOrders(orders) { writeJsonStorage(LOCAL_ORDERS_KEY, orders) }

function upsertLocalOrder(order) {
  const orders = readJsonStorage(LOCAL_ORDERS_KEY, [])
  const index = orders.findIndex(item => item.orderCode === order.orderCode)
  if (index >= 0) orders[index] = order; else orders.unshift(order)
  saveLocalOrders(orders.slice(0, 50))
}

function mergeOrders(remoteOrders, localOrders) {
  const map = new Map()
  for (const o of [...remoteOrders, ...localOrders]) map.set(o.orderCode || o.orderId || o.id, o)
  return [...map.values()].sort((a, b) => new Date(b.orderDate || 0) - new Date(a.orderDate || 0))
}

function createLocalOrder(customer) {
  const orderCode = `ORD-DEMO${Date.now().toString().slice(-6)}`
  return {
    orderId: `local-${Date.now()}`, orderCode,
    customerId: String(customer.customerId || customer.phone),
    customerName: customer.fullName, customerPhone: customer.phone,
    orderDate: new Date().toISOString(), totalAmount: cartTotal.value,
    discountAmount: discountAmount.value, finalAmount: finalAmount.value,
    paidAmount: paidAmount.value, debtAmount: debtAmount.value,
    paymentMethod: checkout.value.paymentMethod,
    paymentStatus: paymentStatusPreview.value, orderStatus: orderStatusPreview.value,
    isLocalDemo: true,
    items: selectedCart.value.map(item => ({
      productId: item.productId, productCode: item.productCode, productName: item.productName,
      categoryName: item.categoryName, quantity: item.quantity, unitPrice: item.unitPrice,
      subTotal: item.unitPrice * item.quantity
    }))
  }
}

function applyCheckoutSideEffects(order) {
  if (!currentUser.value) return
  if (checkout.value.paymentMethod === 'Wallet' && paidAmount.value > 0) {
    currentUser.value.walletBalance = Math.max(0, walletBalance.value - paidAmount.value)
    addWalletTransaction('pay', -paidAmount.value, 'Thanh to�n don h�ng b?ng v�', order.orderCode)
  }
  currentUser.value.currentDebt = Math.max(0, Number(currentUser.value.currentDebt || 0) + orderDebt(order))
  currentUser.value.totalSpent = Number(currentUser.value.totalSpent || 0) + orderTotal(order)
  saveCustomerUser(currentUser.value)
  saveDemoCustomer(currentUser.value)
  saveWalletState()
  addActivityLog('order.created', `�?t h�ng th�nh c�ng${order.orderCode ? ' - ' + order.orderCode : ''}`, order.orderCode)
}

export async function loadMyOrders() {
  if (!currentUser.value) return
  if (currentUser.value?.isLocalOnly) return
  myOrdersLoading.value = true
  try {
    const res = await api.get('/api/Orders', { params: { customerId: currentUser.value.customerId } })
    myOrders.value = mergeOrders(Array.isArray(res.data) ? res.data : [], loadLocalOrders())
  } catch { myOrders.value = loadLocalOrders() }
  finally { myOrdersLoading.value = false }
}

export function openOrderDetail(order) {
  const id = order?.orderId || order?.id || order?.orderCode
  if (id) router.push(`/orders/${id}`)
}

export function lookupStatusCount(status) {
  return orderLookup.value.results.filter(o => String(o.orderStatus || '').toLowerCase() === status.toLowerCase()).length
}

export function setLookupMode(mode) {
  orderLookup.value.mode = mode
  orderLookup.value.error = ''
  orderLookup.value.result = null
  orderLookup.value.results = []
  orderLookup.value.statusFilter = ''
  orderLookup.value.expandedId = null
  orderLookup.value.productSearch = ''
}

export function toggleLookupExpand(id) {
  orderLookup.value.expandedId = orderLookup.value.expandedId === id ? null : id
}

function validatePhone(phone) {
  const digits = phone.replace(/\D/g, '')
  if (!digits) return 'Vui l�ng nh?p s? di?n tho?i.'
  if (digits.length < 9 || digits.length > 11) return 'S? di?n tho?i ph?i c� 9�11 ch? s?.'
  return null
}

export async function lookupOrder() {
  const code = orderLookup.value.orderCode.trim().toUpperCase()
  const phone = orderLookup.value.phone.trim()
  orderLookup.value.error = ''; orderLookup.value.result = null
  if (!code) { orderLookup.value.error = 'Vui l�ng nh?p m� don h�ng.'; return }
  if (phone) {
    const phoneErr = validatePhone(phone)
    if (phoneErr) { orderLookup.value.error = phoneErr; return }
  }
  orderLookup.value.orderCode = code
  orderLookup.value.loading = true
  try {
    const params = phone ? { orderCode: code, phone } : { orderCode: code }
    const res = await api.get('/api/Orders/lookup', { params })
    orderLookup.value.result = res.data
  } catch (error) {
    const status = error.response?.status
    const msg = error.response?.data?.message || error.response?.data || error.message
    if (!error.response) orderLookup.value.error = 'Kh�ng k?t n?i du?c d?n server. Ki?m tra backend dang ch?y chua.'
    else if (status === 404) orderLookup.value.error = 'Kh�ng t�m th?y don h�ng. Ki?m tra l?i m� don ho?c t�m theo S�T.'
    else orderLookup.value.error = `L?i ${status ?? '?'}: ${msg || 'Kh�ng th? tra c?u l�c n�y.'}`
  } finally { orderLookup.value.loading = false }
}

export async function lookupByPhone() {
  const phone = orderLookup.value.phone.trim()
  orderLookup.value.error = ''; orderLookup.value.results = []; orderLookup.value.expandedId = null
  const phoneErr = validatePhone(phone)
  if (phoneErr) { orderLookup.value.error = phoneErr; return }
  orderLookup.value.loading = true
  try {
    const res = await api.get('/api/Orders/lookup', { params: { phone } })
    const data = Array.isArray(res.data) ? res.data : [res.data]
    orderLookup.value.results = data
    if (!data.length) orderLookup.value.error = 'Kh�ng t�m th?y don h�ng n�o v?i s? di?n tho?i n�y.'
  } catch (error) {
    const status = error.response?.status
    const msg = error.response?.data?.message || error.response?.data || error.message
    if (!error.response) orderLookup.value.error = 'Kh�ng k?t n?i du?c d?n server. Ki?m tra backend dang ch?y chua.'
    else if (status === 404 || status === 400) orderLookup.value.error = 'Kh�ng t�m th?y don h�ng v?i s? di?n tho?i n�y.'
    else orderLookup.value.error = `L?i ${status ?? '?'}: ${msg || 'Kh�ng th? tra c?u l�c n�y.'}`
  } finally { orderLookup.value.loading = false }
}

export async function cancelFromLookup(order) {
  if (!canCancelOrder(order)) { showNotice('�on n�y kh�ng th? h?y ? tr?ng th�i hi?n t?i.', 'bad'); return }
  if (!confirm(`H?y don ${order.orderCode}?`)) return
  const phone = orderLookup.value.phone.trim()
  try {
    await api.put(`/api/Orders/${order.orderId}/customer-cancel`, { phone })
    showNotice('Dang nhap thanh cong.')
    await lookupByPhone()
    if (currentUser.value) await loadMyOrders()
  } catch (error) { showNotice(error.response?.data?.message || 'Kh�ng h?y du?c don h�ng.', 'bad') }
}

export async function submitCheckout() {
  checkoutMessage.value = ''; checkoutBusy.value = true
  syncCartStock()
  if (selectedCart.value.length === 0) { checkoutMessage.value = 'Vui l�ng ch?n �t nh?t m?t s?n ph?m d? thanh to�n.'; checkoutBusy.value = false; return }
  if (!currentUser.value) { checkoutBusy.value = false; openAuth('login'); return }
  try {
    const customer = await ensureCheckoutCustomer()
    const isLocalCustomer = customer?.isLocalOnly || Number(customer?.customerId || 0) <= 0
    const payload = {
      customerId: isLocalCustomer ? 0 : customer.customerId,
      customerName: customer.fullName || checkoutShipping.value.fullName || '',
      customerPhone: customer.phone || checkoutShipping.value.phone || '',
      customerAddress: customer.address || checkoutShipping.value.address || '',
      discountAmount: discountAmount.value,
      paymentMethod: backendPaymentMethod(checkout.value.paymentMethod),
      paidAmount: paidAmount.value,
      items: selectedCart.value.map(item => ({ productId: item.productId, externalProductId: item.externalProductId || '', quantity: item.quantity }))
    }
    const res = await api.post('/api/Sales/Checkout', payload)
    const order = { ...(res.data?.data || res.data), paymentMethod: checkout.value.paymentMethod }
    applyCheckoutSideEffects(order)
    clearSelectedFromCart()
    checkout.value = { voucher: 'NONE', paymentMethod: 'Cash', depositAmount: 0 }
    await Promise.all([loadProducts(), loadMyOrders()])
    await loadCustomerAccountData()
    showNotice(`�?t h�ng th�nh c�ng ${order?.orderCode ? `- ${order.orderCode}` : ''}.`)
    openPage('myOrders')
  } catch (error) {
    if (!error.response) {
      const order = createLocalOrder(currentUser.value)
      upsertLocalOrder(order)
      adjustLocalStockReserves(order.items, 1)
      applyCheckoutSideEffects(order)
      clearSelectedFromCart()
      checkout.value = { voucher: 'NONE', paymentMethod: 'Cash', depositAmount: 0 }
      await Promise.all([loadProducts(), loadMyOrders()])
      await loadCustomerAccountData()
      showNotice(`�� t?o don demo local - ${order.orderCode}.`)
      openPage('myOrders')
      return
    }
    checkoutMessage.value = error.response?.data?.message || error.message || 'L?i khi t?o don h�ng.'
  } finally { checkoutBusy.value = false }
}

async function updateBackendCustomer(customer, profile) {
  if (customer?.isLocalOnly) {
    const updated = { ...customer, ...profile, isLocalOnly: true }
    setCurrentCustomer(updated)
    return currentUser.value
  }
  const id = customerId(customer)
  const phone = customer?.phone || profile.phone
  const res = await api.put(`/api/Customers/${id}/profile`, { ...profile, phone })
  setCurrentCustomer(res.data)
  return currentUser.value
}

async function findCustomerByPhone(phone) {
  if (!phone) return null
  try { const res = await api.post('/api/Customers/login', { phone }); return res.data }
  catch (error) {
    const localCustomer = loadDemoCustomer(phone)
    if (localCustomer) return localCustomer
    if (error.response?.status === 404) return null
    throw error
  }
}

async function ensureCheckoutCustomer() {
  const profile = checkoutProfilePayload()
  if (!profile.fullName) throw new Error('Vui l�ng nh?p h? t�n ngu?i nh?n.')
  if (!profile.phone) throw new Error('Vui l�ng nh?p s? di?n tho?i.')
  if (!profile.address) throw new Error('Vui l�ng nh?p d?a ch? nh?n h�ng.')
  if (currentUser.value?.customerId) {
    try { return await updateBackendCustomer(currentUser.value, profile) }
    catch (error) {
      if (!error.response) { setCurrentCustomer({ ...currentUser.value, ...profile }); return currentUser.value }
      if (![400, 404].includes(error.response?.status)) throw new Error(error.response?.data?.message || 'Kh�ng c?p nh?t du?c d?a ch? nh?n h�ng.')
    }
  }
  const existing = await findCustomerByPhone(profile.phone)
  if (existing) {
    try { return await updateBackendCustomer(existing, profile) }
    catch (error) {
      if (!error.response) { setCurrentCustomer({ ...existing, ...profile }); return currentUser.value }
      throw error
    }
  }
  try { const res = await api.post('/api/Customers', profile); setCurrentCustomer(res.data); return currentUser.value }
  catch (error) {
    if (!error.response) { setCurrentCustomer(createLocalDemoCustomer(profile)); return currentUser.value }
    throw error
  }
}

export function canCancelOrder(order) {
  const s = String(order?.orderStatus || '').toLowerCase()
  return !['cancelled', 'completed', 'shipping'].includes(s)
}

function markOrderCancelled(order) {
  const updated = { ...order, orderStatus: 'Cancelled', paymentStatus: Number(order.paidAmount || 0) > 0 ? 'Refunded' : paymentStatusFor(order) }
  if (currentUser.value && order.paymentMethod === 'Wallet' && Number(order.paidAmount || 0) > 0) {
    currentUser.value.walletBalance = walletBalance.value + Number(order.paidAmount || 0)
    addWalletTransaction('refund', Number(order.paidAmount || 0), 'Ho�n ti?n do h?y don', order.orderCode)
  }
  if (currentUser.value) {
    currentUser.value.currentDebt = Math.max(0, Number(currentUser.value.currentDebt || 0) - orderDebt(order))
    saveCustomerUser(currentUser.value); saveDemoCustomer(currentUser.value)
  }
  if (isLocalOrder(order)) adjustLocalStockReserves(order.items, -1)
  upsertLocalOrder(updated)
  addActivityLog('order.cancelled', 'H?y don h�ng', order.orderCode)
  return updated
}

export async function cancelOrder(order, staff = false) {
  if (!canCancelOrder(order)) { showNotice('�on n�y kh�ng th? h?y ? tr?ng th�i hi?n t?i.', 'bad'); return }
  const id = order.orderId || order.id
  try {
    if (id && !String(id).startsWith('local-') && !order.isLocalDemo) {
      if (staff) await api.put(`/api/Orders/${id}/cancel`)
      else await api.put(`/api/Orders/${id}/customer-cancel`, { phone: currentUser.value?.phone || order.customerPhone || '' })
      await loadProducts(); await loadStaffData(); await loadMyOrders()
      await recordAuditLog('order.cancelled', 'Order', order.orderCode || id, '', 'Cancelled')
      showNotice('Dang nhap local thanh cong.')
      return
    }
    throw new Error('local')
  } catch (error) {
    if (staff && error.message !== 'local' && error.response) { showNotice(error.response?.data?.message || 'Kh�ng h?y du?c don h�ng.', 'bad'); return }
    markOrderCancelled(order)
    await loadProducts(); await loadMyOrders()
    if (staff) await loadStaffData()
    showNotice('Dang nhap thanh cong.')
  }
}

export function isDepositHolding(order) {
  if (!order) return false
  const pm = String(order.paymentMethod || order.PaymentMethod || '')
  const ps = String(order.paymentStatus || order.PaymentStatus || '')
  const os = String(order.orderStatus || order.OrderStatus || '').toLowerCase()
  if (os === 'cancelled' || os === 'completed') return false
  return pm === 'Deposit' || ps === 'Partial'
}

export async function payRemainingOrder(order) {
  if (!confirm(`X�c nh?n d� thanh to�n d? s? ti?n c�n l?i cho don ${order.orderCode}?`)) return
  const id = order.orderId || order.id
  try {
    if (id && !String(id).startsWith('local-') && !order.isLocalDemo) {
      await api.put(`/api/Orders/${id}/status`, { status: 'Completed' })
    } else {
      const all = readJsonStorage(LOCAL_ORDERS_KEY, [])
      const idx = all.findIndex(o => o.orderCode === order.orderCode)
      if (idx >= 0) {
        all[idx] = { ...all[idx], orderStatus: 'Completed', paymentStatus: 'Paid', paidAmount: all[idx].finalAmount || all[idx].totalAmount, debtAmount: 0 }
        writeJsonStorage(LOCAL_ORDERS_KEY, all)
      }
    }
    await loadMyOrders()
    showNotice(`�on ${order.orderCode} d� thanh to�n d?.`)
  } catch (e) { showNotice(e.response?.data?.message || 'Kh�ng th? c?p nh?t don h�ng.', 'bad') }
}

// --- Wallet top-up ------------------------------------------------------------

function mapTopUpRequestFromApi(r) {
  return {
    id: r.requestCode || `TOPUP-${r.walletTopUpRequestId}`, backendId: r.walletTopUpRequestId,
    customerKey: String(r.customerId || ''), customerId: r.customerId || null,
    customerName: r.customerName || 'Kh�ch h�ng', customerPhone: r.customerPhone || '',
    amount: Number(r.amount || 0), paymentMethod: r.paymentMethod || 'BankTransfer',
    status: String(r.status || 'pending').toLowerCase(),
    createdAt: r.requestedAt || new Date().toISOString(),
    reviewedAt: r.reviewedAt || null, reviewedBy: r.reviewedBy || '',
    customerWalletBalance: r.customerWalletBalance
  }
}

export async function loadTopUpRequests() {
  const localRequests = readJsonStorage(TOPUP_REQUESTS_KEY, [])
  topUpRequests.value = localRequests
  try {
    const path = staffUser.value ? '/api/WalletTopUps'
      : currentUser.value?.customerId ? `/api/WalletTopUps/customer/${currentUser.value.customerId}` : ''
    if (!path) return
    const res = await api.get(path)
    if (Array.isArray(res.data)) topUpRequests.value = res.data.map(mapTopUpRequestFromApi)
  } catch { topUpRequests.value = localRequests }
}

function saveTopUpRequests() { writeJsonStorage(TOPUP_REQUESTS_KEY, topUpRequests.value) }

function updateStoredCustomerBalance(customerKey, amount) {
  const demoCustomer = loadDemoCustomer()
  if (demoCustomer && walletKeyFor(demoCustomer) === String(customerKey)) {
    const updatedDemo = { ...demoCustomer, walletBalance: Number(demoCustomer.walletBalance || 0) + Number(amount || 0) }
    saveDemoCustomer(updatedDemo)
    if (currentUser.value && walletKeyFor(currentUser.value) === String(customerKey)) {
      currentUser.value = { ...currentUser.value, walletBalance: updatedDemo.walletBalance }
      saveCustomerUser(currentUser.value)
    }
  }
}

async function submitTopUpRequest(amount) {
  if (currentUser.value?.customerId) {
    try {
      const res = await api.post('/api/WalletTopUps', {
        customerId: currentUser.value.customerId, amount: Number(amount || 0),
        paymentMethod: walletTopUpForm.value.paymentMethod, note: 'Kh�ch y�u c?u n?p v�'
      })
      const request = mapTopUpRequestFromApi(res.data)
      topUpRequests.value = [request, ...topUpRequests.value.filter(item => item.id !== request.id)]
      return request
    } catch {}
  }
  const request = {
    id: `TOPUP-${Date.now()}`, customerKey: walletKeyFor(),
    customerId: currentUser.value?.customerId || null,
    customerName: currentUser.value?.fullName || 'Kh�ch h�ng', customerPhone: currentUser.value?.phone || '',
    amount: Number(amount || 0), paymentMethod: walletTopUpForm.value.paymentMethod,
    status: 'pending', createdAt: new Date().toISOString(), reviewedAt: null, reviewedBy: ''
  }
  topUpRequests.value.unshift(request)
  saveTopUpRequests()
  return request
}

export function openTopUpModal() {
  if (!currentUser.value) { openAuth('login'); return }
  walletTopUpForm.value = { amount: 200000, paymentMethod: 'BankTransfer' }
  showTopUpModal.value = true
}
export function closeTopUpModal() { showTopUpModal.value = false }

export async function topUpWallet() {
  if (!currentUser.value) { openAuth('login'); return }
  const amount = Math.max(0, Number(walletTopUpForm.value.amount || 0))
  if (amount <= 0) { showNotice('Vui l�ng nh?p s? ti?n n?p v� h?p l?.', 'bad'); return }
  const request = await submitTopUpRequest(amount)
  showTopUpModal.value = false
  addActivityLog('wallet.requested', `Kh�ch y�u c?u n?p v� ${formatMoney(amount)}`, request.id)
  showNotice('Dang nhap thanh cong.')
}

export async function approveTopUpRequest(request) {
  if (!request || request.status !== 'pending') return
  if (request.backendId) {
    try {
      const res = await api.post(`/api/WalletTopUps/${request.backendId}/approve`, { reviewedBy: staffUser.value?.username || 'Nh�n vi�n' })
      const updated = mapTopUpRequestFromApi(res.data)
      topUpRequests.value = topUpRequests.value.map(item => item.id === request.id ? updated : item)
      if (currentUser.value?.customerId && Number(currentUser.value.customerId) === Number(updated.customerId) && updated.customerWalletBalance != null) {
        currentUser.value = { ...currentUser.value, walletBalance: Number(updated.customerWalletBalance || 0) }
        saveCustomerUser(currentUser.value)
      }
      addActivityLog('wallet.approved', `Duy?t n?p v� ${formatMoney(updated.amount)} cho ${updated.customerName}`)
      showNotice('Dang nhap local thanh cong.')
      return
    } catch (error) { showNotice(error.response?.data?.message || 'Kh�ng duy?t du?c y�u c?u n?p v�.', 'bad'); return }
  }
  const states = readJsonStorage(WALLET_STATE_KEY, {})
  const state = states[request.customerKey] || { balance: 500000, transactions: [] }
  const amount = Number(request.amount || 0)
  const nextBalance = Number(state.balance || 0) + amount
  const nextTransactions = [{ id: Date.now(), type: 'topup', amount, note: 'N?p v� d� du?c nh�n vi�n duy?t', orderCode: request.id, createdAt: new Date().toISOString() }, ...(state.transactions || [])].slice(0, 12)
  saveWalletStateForCustomer(request.customerKey, nextBalance, nextTransactions)
  topUpRequests.value = topUpRequests.value.map(item => item.id === request.id ? { ...item, status: 'approved', reviewedAt: new Date().toISOString(), reviewedBy: staffUser.value?.username || 'Nh�n vi�n' } : item)
  saveTopUpRequests()
  updateStoredCustomerBalance(request.customerKey, amount)
  if (currentUser.value && walletKeyFor() === request.customerKey) loadWalletState(currentUser.value)
  addActivityLog('wallet.approved', `Duy?t n?p v� ${formatMoney(amount)} cho ${request.customerName}`)
  showNotice('Dang nhap thanh cong.')
}

export async function rejectTopUpRequest(request) {
  if (!request || request.status !== 'pending') return
  if (request.backendId) {
    try {
      const res = await api.post(`/api/WalletTopUps/${request.backendId}/reject`, { reviewedBy: staffUser.value?.username || 'Nh�n vi�n' })
      const updated = mapTopUpRequestFromApi(res.data)
      topUpRequests.value = topUpRequests.value.map(item => item.id === request.id ? updated : item)
      addActivityLog('wallet.rejected', `T? ch?i n?p v� ${formatMoney(updated.amount)} cho ${updated.customerName}`)
      showNotice('Dang nhap local thanh cong.')
      return
    } catch (error) { showNotice(error.response?.data?.message || 'Kh�ng t? ch?i du?c y�u c?u n?p v�.', 'bad'); return }
  }
  topUpRequests.value = topUpRequests.value.map(item => item.id === request.id ? { ...item, status: 'rejected', reviewedAt: new Date().toISOString(), reviewedBy: staffUser.value?.username || 'Nh�n vi�n' } : item)
  saveTopUpRequests()
  addActivityLog('wallet.rejected', `T? ch?i n?p v� ${formatMoney(request.amount)} cho ${request.customerName}`)
  showNotice('Dang nhap thanh cong.')
}

// --- Staff --------------------------------------------------------------------

export function openStaffAuth() { staffError.value = ''; showStaffModal.value = true }

export function parseStaffToken(token) {
  try {
    const payload = JSON.parse(atob(token.split('.')[1]))
    return {
      username: payload.email || payload.unique_name || payload['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name'] || 'staff',
      role: payload.role || 'Sales'
    }
  } catch { return null }
}

export async function loginStaff() {
  staffBusy.value = true; staffError.value = ''
  try {
    const res = await api.post('/api/auth/login', staffLoginForm.value)
    const token = res.data.accessToken || res.data.token
    const tokenUser = token ? parseStaffToken(token) : null
    if (!token) throw new Error('Login response does not include a token.')
    setStaffToken(token)
    staffUser.value = {
      username: res.data.user?.email || res.data.user?.name || res.data.username || tokenUser?.username || staffLoginForm.value.email,
      role: res.data.user?.role || res.data.role || tokenUser?.role || 'Sales'
    }
    showStaffModal.value = false
    await loadStaffData()
    openPage('dashboard')
  } catch (error) {
    if (!error.response) staffError.value = 'Kh�ng k?t n?i du?c d?n m�y ch?. Ki?m tra Nh�m 3 (port 8083) v� API Gateway (port 7000) dang ch?y chua.'
    else staffError.value = error.response?.data?.message || `�ang nh?p th?t b?i (${error.response.status}).`
  }
  finally { staffBusy.value = false }
}

export function logoutStaff() {
  setStaffToken('')
  staffUser.value = null
  staffData.value = { orders: [], customers: [], suppliers: [], payments: [], debts: [], outbox: [], auditLogs: [] }
  if (isStaffPage.value) openPage('shop')
}

async function safeList(path) {
  try { const res = await api.get(path); return Array.isArray(res.data) ? res.data : [] } catch { return [] }
}

function mapHealthStatus(value) {
  const status = String(value || 'Unknown')
  if (status.toLowerCase() === 'healthy') return 'OK'
  if (status.toLowerCase() === 'degraded') return 'Degraded'
  if (status.toLowerCase() === 'unhealthy') return 'Down'
  return status
}

export async function loadIntegrationHealth() {
  const next = {
    gateway: { status: 'Down', detail: 'Khong goi duoc /health' },
    orderApi: { status: 'Down', detail: 'Khong goi duoc /api/OrderHealth' },
    rabbitmq: { status: 'Unknown', detail: 'Chua co du lieu tu OrderApi health' }
  }
  try {
    const gateway = await api.get('/health')
    next.gateway = { status: mapHealthStatus(gateway.data?.status), detail: gateway.data?.service || 'ApiGateway' }
  } catch {}
  try {
    const order = await api.get('/api/OrderHealth')
    const entries = order.data?.entries || {}
    const rabbit = entries.rabbitmq || entries.RabbitMQ || entries.rabbitMq
    next.orderApi = { status: mapHealthStatus(order.data?.status), detail: 'OrderApi /health' }
    next.rabbitmq = { status: mapHealthStatus(rabbit?.status || order.data?.status), detail: rabbit?.description || rabbit?.exception || 'RabbitMQ dependency' }
  } catch {}
  integrationHealth.value = next
}

export async function loadStaffData() {
  if (!staffUser.value) return
  staffLoading.value = true
  await loadIntegrationHealth()
  await loadTopUpRequests()
  const [orders, customers, suppliers, payments, debts, outbox, auditLogs, returns, invoices] = await Promise.all([
    safeList('/api/Orders'), safeList('/api/Customers'), safeList('/api/Suppliers'),
    safeList('/api/Payments'), safeList('/api/Debts'), safeList('/api/OutboxMessages'),
    safeList('/api/AuditLogs'), safeList('/api/Returns'), safeList('/api/SalesInvoices')
  ])
  staffData.value = { orders: mergeOrders(orders, loadAllLocalOrders()), customers, suppliers, payments, debts, outbox, auditLogs, returns, invoices }
  staffLoading.value = false
}

export async function recordAuditLog(action, entityName, entityId, oldValue = '', newValue = '') {
  addActivityLog(action, newValue || oldValue || entityName, entityId)
  if (!staffUser.value) return
  try {
    const res = await api.post('/api/AuditLogs', {
      action, entityName, entityId: String(entityId || '-'),
      oldValue: oldValue ? String(oldValue) : '', newValue: newValue ? String(newValue) : '',
      performedBy: staffUser.value.username || 'staff'
    })
    staffData.value.auditLogs = [res.data, ...staffData.value.auditLogs].slice(0, 80)
  } catch {}
}

export function resetSupplierForm() {
  supplierEditingId.value = null
  supplierForm.value = { supplierCode: '', supplierName: '', contactPerson: '', phone: '', email: '', address: '', taxCode: '', note: '', status: 'Active' }
}

export function editSupplier(supplier) {
  supplierEditingId.value = supplier.supplierId || supplier.id
  supplierForm.value = {
    supplierCode: supplier.supplierCode || '', supplierName: supplier.supplierName || supplier.name || '',
    contactPerson: supplier.contactPerson || supplier.contactName || '', phone: supplier.phone || '',
    email: supplier.email || '', address: supplier.address || '', taxCode: supplier.taxCode || '',
    note: supplier.note || '', status: supplier.status || 'Active'
  }
}

export async function saveSupplier() {
  if (!supplierForm.value.supplierName.trim() || !supplierForm.value.phone.trim()) { showNotice('Vui l�ng nh?p t�n nh� cung c?p v� s? di?n tho?i.', 'bad'); return }
  supplierSaving.value = true
  try {
    if (supplierEditingId.value) {
      await api.put(`/api/Suppliers/${supplierEditingId.value}`, {
        supplierName: supplierForm.value.supplierName, contactPerson: supplierForm.value.contactPerson,
        phone: supplierForm.value.phone, email: supplierForm.value.email, address: supplierForm.value.address,
        taxCode: supplierForm.value.taxCode, note: supplierForm.value.note, status: supplierForm.value.status
      })
      await recordAuditLog('supplier.updated', 'Supplier', supplierEditingId.value, '', supplierForm.value.supplierName)
      showNotice('Dang nhap thanh cong.')
    } else {
      await api.post('/api/Suppliers', {
        supplierCode: supplierForm.value.supplierCode, supplierName: supplierForm.value.supplierName,
        contactPerson: supplierForm.value.contactPerson, phone: supplierForm.value.phone,
        email: supplierForm.value.email, address: supplierForm.value.address,
        taxCode: supplierForm.value.taxCode, note: supplierForm.value.note
      })
      await recordAuditLog('supplier.created', 'Supplier', supplierForm.value.supplierCode || '-', '', supplierForm.value.supplierName)
      showNotice('Dang nhap thanh cong.')
    }
    resetSupplierForm()
    await loadStaffData()
  } catch (error) { showNotice(error.response?.data?.message || 'Kh�ng luu du?c nh� cung c?p.', 'bad') }
  finally { supplierSaving.value = false }
}

export async function deleteSupplier(supplier) {
  const id = supplier.supplierId || supplier.id
  if (!id || !confirm('X�a nh� cung c?p n�y?')) return
  try {
    await api.delete(`/api/Suppliers/${id}`)
    await recordAuditLog('supplier.deleted', 'Supplier', id, supplier.supplierName || supplier.name || '', '')
    if (supplierEditingId.value === id) resetSupplierForm()
    await loadStaffData()
    showNotice('Dang nhap thanh cong.')
  } catch (error) { showNotice(error.response?.data?.message || 'Kh�ng x�a du?c nh� cung c?p.', 'bad') }
}

export function debtRemaining(debt) { return Math.max(0, Number(debt?.remainingAmount ?? debt?.debtAmount - debt?.paidAmount ?? 0)) }
export function debtPayAmount(debt) { const id = debt.debtId || debt.id; return Number(debtPayForms.value[id]?.amount || 0) }
export function setDebtPayAmount(debt, amount) {
  const id = debt.debtId || debt.id
  debtPayForms.value = { ...debtPayForms.value, [id]: { amount, paymentMethod: debtPayForms.value[id]?.paymentMethod || 'Cash' } }
}

export async function payDebt(debt) {
  const id = debt.debtId || debt.id
  const amount = debtPayAmount(debt)
  if (!id || amount <= 0) { showNotice('Nh?p s? ti?n tr? n? h?p l?.', 'bad'); return }
  debtPaying.value = { ...debtPaying.value, [id]: true }
  try {
    await api.post(`/api/Debts/${id}/pay`, { amount, paymentMethod: debtPayForms.value[id]?.paymentMethod || 'Cash', note: 'Debt payment from staff UI' })
    await recordAuditLog('debt.paid', 'Debt', id, '', String(amount))
    const next = { ...debtPayForms.value }; delete next[id]; debtPayForms.value = next
    await loadStaffData()
    showNotice('Dang nhap thanh cong.')
  } catch (error) { showNotice(error.response?.data?.message || 'Kh�ng thanh to�n du?c c�ng n?.', 'bad') }
  finally { const next = { ...debtPaying.value }; delete next[id]; debtPaying.value = next }
}

export async function updateCustomerTier(customer, tierName) {
  const id = customerId(customer)
  if (!id || staffTierSaving.value[id]) return
  const previousTier = customer.membershipTier || ''
  customer.membershipTier = tierName
  staffTierSaving.value = { ...staffTierSaving.value, [id]: true }
  try {
    const res = await api.put(`/api/Customers/${id}`, {
      fullName: customerName(customer), phone: customer.phone || '', email: customer.email || '',
      address: customer.address || '', status: customer.status || 'Active', membershipTier: tierName
    })
    const updated = res.data || { ...customer, membershipTier: tierName }
    staffData.value.customers = staffData.value.customers.map(item => customerId(item) === id ? { ...item, ...updated } : item)
    if (currentUser.value && customerId(currentUser.value) === id) {
      currentUser.value = { ...currentUser.value, membershipTier: updated.membershipTier || tierName }
      saveCustomerUser(currentUser.value)
    }
    await recordAuditLog('customer.tier.updated', 'Customer', id, previousTier, tierName)
    showNotice(`�� c?p nh?t h?ng ${tierName} cho ${customerName(customer)}.`)
  } catch (error) {
    customer.membershipTier = previousTier
    staffData.value.customers = [...staffData.value.customers]
    showNotice(error.response?.data?.message || 'Kh�ng th? c?p nh?t h?ng th�nh vi�n.', 'bad')
  } finally { const next = { ...staffTierSaving.value }; delete next[id]; staffTierSaving.value = next }
}

export function isPendingPaymentOrder(order) {
  // BankTransfer/QR chua du?c x�c nh?n thanh to�n (ch? kh�ch b?m "T�i d� TT")
  const s = String(order?.orderStatus || '').toLowerCase()
  const ps = String(order?.paymentStatus || '').toLowerCase()
  const m = String(order?.paymentMethod || '').toLowerCase()
  return (s === 'waitingpayment' || ps === 'pendingpayment') &&
         (m === 'banktransfer' || m === 'qr') &&
         s !== 'awaitingpaymentconfirmation' && ps !== 'awaitingpaymentconfirmation' &&
         ps !== 'paid'
}

export function isAwaitingPaymentConfirmation(order) {
  // Kh�ch d� b?m "T�i d� TT", ch? nh�n vi�n x�c nh?n
  const s = String(order?.orderStatus || '').toLowerCase()
  const ps = String(order?.paymentStatus || '').toLowerCase()
  return s === 'awaitingpaymentconfirmation' || ps === 'awaitingpaymentconfirmation'
}

export async function markOrderPaid(order) {
  const id = order?.orderId || order?.id
  if (!id) return
  if (order.isLocalDemo || String(id).startsWith('local-')) {
    const updated = { ...order, paymentStatus: 'AwaitingPaymentConfirmation', orderStatus: 'AwaitingPaymentConfirmation' }
    upsertLocalOrder(updated)
    await loadMyOrders()
    showNotice('Dang nhap local thanh cong.')
      return
  }
  try {
    await api.put(`/api/Orders/${id}/status`, { status: 'AwaitingPaymentConfirmation' })
    await loadMyOrders()
    showNotice('Dang nhap thanh cong.')
  } catch {
    const updated = { ...order, paymentStatus: 'AwaitingPaymentConfirmation', orderStatus: 'AwaitingPaymentConfirmation' }
    upsertLocalOrder(updated)
    await loadMyOrders()
    showNotice('Dang nhap thanh cong.')
  }
}

export async function confirmPaymentReceived(order) {
  const id = order?.orderId || order?.id
  if (!id) return
  const total = orderTotal(order)
  if (order.isLocalDemo || String(id).startsWith('local-')) {
    const updated = { ...order, paymentStatus: 'Paid', paidAmount: total, debtAmount: 0, orderStatus: 'AwaitingConfirmation' }
    upsertLocalOrder(updated)
    await loadMyOrders(); await loadStaffData()
    showNotice('Dang nhap local thanh cong.')
      return
  }
  try {
    await api.put(`/api/Orders/${id}/status`, { status: 'AwaitingConfirmation' })
    await loadStaffData(); await loadMyOrders()
    showNotice('Dang nhap thanh cong.')
  } catch { showNotice('Kh�ng x�c nh?n du?c. Vui l�ng th? l?i.', 'bad') }
}

export async function approveOrder(order) {
  await updateOrderStatus(order, 'Processing')
  showNotice('Dang nhap thanh cong.')
}

export async function markOrderShipping(order) {
  await updateOrderStatus(order, 'Shipping')
}

export async function completeOrderDelivery(order) {
  const id = order?.orderId || order?.id
  const isCOD = ['cod', 'cash'].includes(String(order?.paymentMethod || '').toLowerCase())
  const total = orderTotal(order)
  if (order.isLocalDemo || String(id).startsWith('local-')) {
    const updated = {
      ...order, orderStatus: 'Completed',
      ...(isCOD ? { paymentStatus: 'Paid', paidAmount: total, debtAmount: 0 } : {})
    }
    upsertLocalOrder(updated)
    if (currentUser.value && isCOD) {
      currentUser.value.currentDebt = Math.max(0, Number(currentUser.value.currentDebt || 0) - orderDebt(order))
      saveCustomerUser(currentUser.value)
    }
    await loadMyOrders(); await loadStaffData()
    showNotice('Dang nhap local thanh cong.')
      return
  }
  await updateOrderStatus(order, 'Completed')
}

export async function updateOrderStatus(order, status) {
  const id = order.orderId || order.id
  if (!id) return
  if (String(id).startsWith('local-') || order.isLocalDemo) {
    const updated = { ...order, orderStatus: status }
    if (status === 'Completed') { updated.paymentStatus = 'Paid'; updated.paidAmount = orderTotal(order); updated.debtAmount = 0 }
    upsertLocalOrder(updated)
    await recordAuditLog('order.status.updated', 'Order', updated.orderCode, order.orderStatus || '', status)
    await loadMyOrders(); await loadStaffData()
    showNotice('Dang nhap local thanh cong.')
      return
  }
  try {
    await api.put(`/api/Orders/${id}/status`, { status })
    await recordAuditLog('order.status.updated', 'Order', order.orderCode || id, order.orderStatus || '', status)
    await loadStaffData(); await loadMyOrders()
    showNotice('Dang nhap thanh cong.')
  } catch (error) { showNotice(error.response?.data?.message || 'Kh�ng c?p nh?t du?c don h�ng.', 'bad') }
}

export async function updateReturnStatus(returnId, status) {
  try {
    await api.put(`/api/Returns/${returnId}/status`, { status })
    showNotice(`�� c?p nh?t tr?ng th�i phi?u ho�n h�ng.`)
    await loadStaffData()
  } catch { showNotice('Kh�ng th? c?p nh?t tr?ng th�i. Vui l�ng th? l?i.', 'bad') }
}

export async function createInvoiceForOrder(orderId, cId) {
  try {
    const inv = await api.post('/api/SalesInvoices', { orderId, customerId: cId })
    showNotice(`�� t?o h�a don ${inv.data?.invoiceCode || ''}.`)
    await loadStaffData()
  } catch { showNotice('Kh�ng th? t?o h�a don. Vui l�ng th? l?i.', 'bad') }
}




