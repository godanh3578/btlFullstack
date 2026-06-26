import { createRouter, createWebHistory } from 'vue-router'

const PAGE_TITLES = {
  shop: 'Trang chủ',
  cart: 'Giỏ hàng',
  lookup: 'Tra cứu đơn',
  myOrders: 'Đơn mua của tôi',
  account: 'Tài khoản',
  orderDetail: 'Chi tiết đơn hàng',
  dashboard: 'Tổng quan',
  orders: 'Quản lý đơn hàng',
  customers: 'Quản lý khách hàng',
  suppliers: 'Quản lý nhà cung cấp',
  payments: 'Thanh toán',
  debts: 'Công nợ',
  returns: 'Hoàn trả',
  invoices: 'Hóa đơn',
  integration: 'Tích hợp hệ thống',
  warehouse: 'Kho hàng'
}

const router = createRouter({
  history: createWebHistory(import.meta.env.BASE_URL),
  routes: [
    { path: '/', name: 'shop', component: () => import('../pages/customer/ShopPage.vue'), meta: { page: 'shop' } },
    { path: '/cart', name: 'cart', component: () => import('../pages/customer/CartPage.vue'), meta: { page: 'cart' } },
    { path: '/lookup', name: 'lookup', component: () => import('../pages/customer/LookupPage.vue'), meta: { page: 'lookup' } },
    { path: '/my-orders', name: 'myOrders', component: () => import('../pages/customer/MyOrdersPage.vue'), meta: { page: 'myOrders' } },
    { path: '/account', name: 'account', component: () => import('../pages/customer/AccountPage.vue'), meta: { page: 'account' } },
    { path: '/orders/:id', name: 'orderDetail', component: () => import('../pages/customer/OrderDetailPage.vue'), meta: { page: 'orderDetail' } },
    { path: '/manage/dashboard', name: 'dashboard', component: () => import('../pages/staff/StaffPage.vue'), meta: { page: 'dashboard', staff: true } },
    { path: '/manage/orders', name: 'orders', component: () => import('../pages/staff/StaffPage.vue'), meta: { page: 'orders', staff: true } },
    { path: '/manage/customers', name: 'customers', component: () => import('../pages/staff/StaffPage.vue'), meta: { page: 'customers', staff: true } },
    { path: '/manage/suppliers', name: 'suppliers', component: () => import('../pages/staff/StaffPage.vue'), meta: { page: 'suppliers', staff: true } },
    { path: '/manage/payments', name: 'payments', component: () => import('../pages/staff/StaffPage.vue'), meta: { page: 'payments', staff: true } },
    { path: '/manage/debts', name: 'debts', component: () => import('../pages/staff/StaffPage.vue'), meta: { page: 'debts', staff: true } },
    { path: '/manage/returns', name: 'returns', component: () => import('../pages/staff/StaffPage.vue'), meta: { page: 'returns', staff: true } },
    { path: '/manage/invoices', name: 'invoices', component: () => import('../pages/staff/StaffPage.vue'), meta: { page: 'invoices', staff: true } },
    { path: '/manage/integration', name: 'integration', component: () => import('../pages/staff/StaffPage.vue'), meta: { page: 'integration', staff: true } },
    { path: '/manage/warehouse', name: 'warehouse', component: () => import('../pages/staff/StaffPage.vue'), meta: { page: 'warehouse', staff: true } },
    { path: '/:pathMatch(.*)*', name: 'notFound', component: () => import('../pages/NotFoundPage.vue'), meta: { page: 'notFound' } }
  ]
})

router.afterEach((to) => {
  window.scrollTo({ top: 0, behavior: 'instant' })
  const page = to.meta?.page
  const title = PAGE_TITLES[page]
  document.title = title ? `${title} — ORIVEX` : 'ORIVEX'
})

export default router
