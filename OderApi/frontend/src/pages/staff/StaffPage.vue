<script setup>
import {
  staffUser, staffData, staffLoading, staffDashboard, activePage, activityLogs,
  products, topUpRequests, pendingTopUpCount,
  supplierForm, supplierEditingId, supplierSaving, staffTierSaving,
  debtPayForms, debtPaying, returnSearch, returnStatusFilter, filteredReturns,
  invoiceSearch, invoiceStatusFilter, filteredInvoices,
  integrationHealth, staffPageTitles, memberTiers,
  formatMoney, formatDateTime, statusLabel, statusClass, paymentMethodLabel, topUpPaymentMethodLabel,
  orderTotal, orderDebt, paymentStatusFor,
  customerName, customerId, customerMemberTier,
  debtRemaining, debtPayAmount, setDebtPayAmount,
  cancelOrder, canCancelOrder, updateOrderStatus, openOrderDetail,
  updateCustomerTier, saveSupplier, editSupplier, resetSupplierForm, deleteSupplier,
  payDebt, approveTopUpRequest, rejectTopUpRequest,
  loadStaffData, logoutStaff, openPage,
  updateReturnStatus, createInvoiceForOrder,
  completeOrderDelivery
} from '../../composables/useAppState'
</script>

<template>
  <main class="page staff-page">
    <aside class="staff-sidebar">
      <h2>Nhân viên</h2>
      <p v-if="staffUser">{{ staffUser.username }} · {{ staffUser.role }}</p>
      <button :class="{ active: activePage === 'dashboard' }" @click="openPage('dashboard')">Tổng quan</button>
      <button :class="{ active: activePage === 'orders' }" @click="openPage('orders')">Đơn hàng</button>
      <button :class="{ active: activePage === 'customers' }" @click="openPage('customers')">Khách hàng</button>
      <button :class="{ active: activePage === 'suppliers' }" @click="openPage('suppliers')">Nhà cung cấp</button>
      <button :class="{ active: activePage === 'payments' }" @click="openPage('payments')">Thanh toán</button>
      <button :class="{ active: activePage === 'debts' }" @click="openPage('debts')">Công nợ</button>
      <button :class="{ active: activePage === 'returns' }" @click="openPage('returns')">Hoàn hàng</button>
      <button :class="{ active: activePage === 'invoices' }" @click="openPage('invoices')">Hóa đơn</button>
      <button :class="{ active: activePage === 'integration' }" @click="openPage('integration')">Đồng bộ kho</button>
      <button class="ghost-btn small" type="button" @click="logoutStaff">Đăng xuất nhân viên</button>
    </aside>

    <section class="staff-content panel">
      <div class="panel-head">
        <div>
          <span class="eyebrow">Khu vực nhân viên</span>
          <h1>{{ staffPageTitles[activePage] || 'Quản lý' }}</h1>
        </div>
        <button class="ghost-btn small" type="button" :disabled="staffLoading" @click="loadStaffData">Tải lại</button>
      </div>

      <p v-if="staffLoading" class="soft-alert">Đang tải dữ liệu quản trị...</p>

      <!-- Dashboard -->
      <div v-if="activePage === 'dashboard'" class="dashboard-panel">
        <div class="dashboard-grid">
          <article><span>Tổng đơn hàng</span><b>{{ staffDashboard.totalOrders }}</b></article>
          <article><span>Doanh thu đã thanh toán</span><b>{{ formatMoney(staffDashboard.revenue) }}</b></article>
          <article><span>Đơn chờ xử lý</span><b>{{ staffDashboard.pending }}</b></article>
          <article><span>Đơn đã thanh toán</span><b>{{ staffDashboard.paid }}</b></article>
          <article><span>Đơn đã hủy</span><b>{{ staffDashboard.cancelled }}</b></article>
          <article><span>Tổng công nợ</span><b>{{ formatMoney(staffDashboard.debt) }}</b></article>
          <article><span>Số dư ví demo</span><b>{{ formatMoney(staffDashboard.wallet) }}</b></article>
          <article><span>Khách mua nhiều nhất</span><b>{{ staffDashboard.topCustomer }}</b></article>
        </div>

        <div class="activity-panel">
          <h3>Nhật ký giao dịch gần đây</h3>
          <p v-if="activityLogs.length === 0" class="soft-alert">Chưa có thao tác nào được ghi nhận.</p>
          <div v-for="log in activityLogs.slice(0, 8)" :key="log.id" class="activity-row">
            <b>{{ log.action }}</b>
            <span>{{ log.note }}</span>
            <small>{{ log.actor }} · {{ formatDateTime(log.createdAt) }}</small>
          </div>
        </div>
      </div>

      <div v-if="activePage === 'dashboard'" class="activity-panel">
        <div class="table-caption">
          <h3>Audit log Order/Sales trong DB</h3>
          <span>{{ staffData.auditLogs.length }} bản ghi</span>
        </div>
        <p v-if="staffData.auditLogs.length === 0" class="soft-alert">Chưa có audit log trong OrderDB.</p>
        <div v-for="log in staffData.auditLogs.slice(0, 10)" :key="log.auditLogId" class="activity-row">
          <b>{{ log.action }}</b>
          <span>{{ log.entityName }} #{{ log.entityId }} · {{ log.newValue || log.oldValue || '-' }}</span>
          <small>{{ log.performedBy }} · {{ formatDateTime(log.performedAt) }}</small>
        </div>
      </div>

      <!-- Orders -->
      <div v-if="activePage === 'orders'" class="table-wrap">
        <table>
          <thead><tr><th>Mã đơn</th><th>Khách</th><th>Tổng</th><th>Công nợ</th><th>Thanh toán</th><th>Trạng thái</th><th>Thao tác</th></tr></thead>
          <tbody>
            <tr v-for="order in staffData.orders" :key="order.orderId || order.id">
              <td>{{ order.orderCode }}</td>
              <td>{{ order.customerName || order.customerId }}</td>
              <td>{{ formatMoney(orderTotal(order)) }}</td>
              <td>{{ formatMoney(orderDebt(order)) }}</td>
              <td>
                <span :class="['status-pill', statusClass(paymentStatusFor(order))]">{{ statusLabel(paymentStatusFor(order)) }}</span>
                <small>{{ paymentMethodLabel(order.paymentMethod) }}</small>
              </td>
              <td><span :class="['status-pill', statusClass(order.orderStatus)]">{{ statusLabel(order.orderStatus) }}</span></td>
              <td class="table-actions">
                <button type="button" @click="openOrderDetail(order)">Xem</button>
                <!-- Hoàn thành đơn: đơn đang chờ xử lý -->
                <button
                  v-if="['pending','confirmed','awaitingconfirmation','awaitingpaymentconfirmation','waitingpayment'].includes(String(order.orderStatus||'').toLowerCase())"
                  class="complete-btn"
                  @click="completeOrderDelivery(order)"
                >✓ Hoàn thành đơn</button>
                <button v-if="canCancelOrder(order)" @click="cancelOrder(order, true)">✕ Hủy</button>
              </td>
            </tr>
          </tbody>
        </table>
      </div>

      <!-- Customers -->
      <div v-else-if="activePage === 'customers'" class="table-wrap">
        <table>
          <thead><tr><th>Mã KH</th><th>Họ tên</th><th>SĐT</th><th>Email</th><th>Hạng thành viên</th><th>Công nợ</th></tr></thead>
          <tbody>
            <tr v-for="customer in staffData.customers" :key="customer.customerId || customer.id">
              <td>{{ customer.customerCode }}</td>
              <td>{{ customerName(customer) }}</td>
              <td>{{ customer.phone }}</td>
              <td>{{ customer.email }}</td>
              <td>
                <select
                  class="tier-select"
                  :value="customerMemberTier(customer)"
                  :disabled="staffTierSaving[customerId(customer)]"
                  @change="updateCustomerTier(customer, $event.target.value)"
                >
                  <option v-for="tier in memberTiers" :key="tier.name" :value="tier.name">
                    {{ tier.name }} - {{ tier.rate }}%
                  </option>
                </select>
              </td>
              <td>{{ formatMoney(customer.currentDebt) }}</td>
            </tr>
          </tbody>
        </table>
      </div>

      <!-- Suppliers -->
      <div v-else-if="activePage === 'suppliers'" class="table-wrap">
        <form class="inline-admin-form" @submit.prevent="saveSupplier">
          <input v-model="supplierForm.supplierCode" type="text" placeholder="Mã NCC" :disabled="Boolean(supplierEditingId)" />
          <input v-model="supplierForm.supplierName" type="text" placeholder="Tên nhà cung cấp" />
          <input v-model="supplierForm.contactPerson" type="text" placeholder="Người liên hệ" />
          <input v-model="supplierForm.phone" type="text" placeholder="Số điện thoại" />
          <input v-model="supplierForm.email" type="email" placeholder="Email" />
          <input v-model="supplierForm.taxCode" type="text" placeholder="Mã số thuế" />
          <select v-if="supplierEditingId" v-model="supplierForm.status">
            <option value="Active">Đang hợp tác</option>
            <option value="Inactive">Ngừng hợp tác</option>
          </select>
          <textarea v-model="supplierForm.note" placeholder="Ghi chú"></textarea>
          <div class="table-actions">
            <button type="submit" :disabled="supplierSaving">{{ supplierEditingId ? 'Lưu NCC' : 'Thêm NCC' }}</button>
            <button v-if="supplierEditingId" type="button" @click="resetSupplierForm">Hủy</button>
          </div>
        </form>
        <table>
          <thead><tr><th>Mã NCC</th><th>Tên</th><th>Liên hệ</th><th>Điện thoại</th><th>Email</th><th>MST</th><th>Trạng thái</th><th>Ghi chú</th><th>Thao tác</th></tr></thead>
          <tbody>
            <tr v-for="supplier in staffData.suppliers" :key="supplier.supplierId || supplier.id">
              <td>{{ supplier.supplierCode }}</td>
              <td>{{ supplier.supplierName || supplier.name }}</td>
              <td>{{ supplier.contactPerson }}</td>
              <td>{{ supplier.phone }}</td>
              <td>{{ supplier.email }}</td>
              <td>{{ supplier.taxCode || '-' }}</td>
              <td><span :class="['status-pill', statusClass(supplier.status)]">{{ statusLabel(supplier.status) }}</span></td>
              <td>{{ supplier.note || '-' }}</td>
              <td class="table-actions">
                <button type="button" @click="editSupplier(supplier)">Sửa</button>
                <button type="button" @click="deleteSupplier(supplier)">Xóa</button>
              </td>
            </tr>
          </tbody>
        </table>
      </div>

      <!-- Payments -->
      <div v-else-if="activePage === 'payments'" class="payments-stack">
        <div class="table-wrap">
          <div class="table-caption">
            <h3>Yêu cầu nạp ví</h3>
            <span>{{ pendingTopUpCount }} chờ duyệt</span>
          </div>
          <table>
            <thead><tr><th>Mã yêu cầu</th><th>Khách</th><th>SĐT</th><th>Số tiền</th><th>Hình thức</th><th>Trạng thái</th><th>Ngày gửi</th><th>Thao tác</th></tr></thead>
            <tbody>
              <tr v-if="topUpRequests.length === 0">
                <td colspan="8">Chưa có yêu cầu nạp tiền.</td>
              </tr>
              <tr v-for="request in topUpRequests" :key="request.id">
                <td>{{ request.id }}</td>
                <td>{{ request.customerName }}</td>
                <td>{{ request.customerPhone }}</td>
                <td>{{ formatMoney(request.amount) }}</td>
                <td>{{ topUpPaymentMethodLabel(request.paymentMethod) }}</td>
                <td>
                  <span :class="['status-pill', request.status === 'approved' ? 'ok' : request.status === 'rejected' ? 'bad' : 'warn']">
                    {{ request.status === 'pending' ? 'Chờ duyệt' : request.status === 'approved' ? 'Đã duyệt' : 'Từ chối' }}
                  </span>
                </td>
                <td>{{ formatDateTime(request.createdAt) }}</td>
                <td class="table-actions">
                  <button v-if="request.status === 'pending'" type="button" @click="approveTopUpRequest(request)">✓ Duyệt</button>
                  <button v-if="request.status === 'pending'" type="button" @click="rejectTopUpRequest(request)">X Từ chối</button>
                  <small v-else>{{ request.reviewedBy || 'Nhân viên' }} · {{ formatDateTime(request.reviewedAt) }}</small>
                </td>
              </tr>
            </tbody>
          </table>
        </div>

        <div class="table-wrap">
          <div class="table-caption">
            <h3>Thanh toán đơn hàng</h3>
            <span>{{ staffData.payments.length }} giao dịch</span>
          </div>
          <table>
            <thead><tr><th>Mã TT</th><th>Đơn</th><th>Phương thức</th><th>Số tiền</th><th>Ngày</th></tr></thead>
            <tbody>
              <tr v-for="payment in staffData.payments" :key="payment.paymentId || payment.id">
                <td>{{ payment.paymentCode }}</td>
                <td>{{ payment.orderId }}</td>
                <td>{{ payment.paymentMethod }}</td>
                <td>{{ formatMoney(payment.amount) }}</td>
                <td>{{ formatDateTime(payment.paymentDate) }}</td>
              </tr>
            </tbody>
          </table>
        </div>
      </div>

      <!-- Debts -->
      <div v-else-if="activePage === 'debts'" class="table-wrap">
        <table>
          <thead><tr><th>Khách</th><th>Đơn</th><th>Số nợ</th><th>Đã trả</th><th>Còn lại</th><th>Hạn trả</th><th>Trạng thái</th><th>Thu nợ</th></tr></thead>
          <tbody>
            <tr v-for="debt in staffData.debts" :key="debt.debtId || debt.id">
              <td>{{ debt.customerName || debt.customerId }}</td>
              <td>{{ debt.orderId }}</td>
              <td>{{ formatMoney(debt.debtAmount) }}</td>
              <td>{{ formatMoney(debt.paidAmount) }}</td>
              <td>{{ formatMoney(debtRemaining(debt)) }}</td>
              <td>{{ formatDateTime(debt.dueDate) }}</td>
              <td><span :class="['status-pill', statusClass(debt.debtStatus)]">{{ statusLabel(debt.debtStatus) }}</span></td>
              <td class="debt-pay-cell">
                <input
                  type="number"
                  min="0"
                  :max="debtRemaining(debt)"
                  :value="debtPayAmount(debt)"
                  :disabled="debtRemaining(debt) <= 0 || debtPaying[debt.debtId || debt.id]"
                  @input="setDebtPayAmount(debt, $event.target.value)"
                />
                <button
                  type="button"
                  :disabled="debtRemaining(debt) <= 0 || debtPaying[debt.debtId || debt.id]"
                  @click="payDebt(debt)"
                >Thu</button>
              </td>
            </tr>
          </tbody>
        </table>
      </div>

      <!-- Returns -->
      <div v-else-if="activePage === 'returns'" class="table-wrap">
        <div class="table-toolbar">
          <input v-model="returnSearch" type="text" placeholder="Tìm mã phiếu, mã đơn, khách hàng..." class="search-input" />
          <select v-model="returnStatusFilter" class="filter-select">
            <option value="">Tất cả trạng thái</option>
            <option value="Pending">Chờ duyệt</option>
            <option value="Approved">Đã duyệt</option>
            <option value="Refunded">Đã hoàn tiền</option>
            <option value="Rejected">Từ chối</option>
          </select>
          <span class="table-count">{{ filteredReturns.length }} phiếu</span>
        </div>
        <table>
          <thead>
            <tr>
              <th>Mã phiếu</th><th>Mã đơn</th><th>Khách hàng</th><th>Ngày tạo</th>
              <th>Lý do</th><th>Hoàn tiền</th><th>Trạng thái</th><th>Thao tác</th>
            </tr>
          </thead>
          <tbody>
            <tr v-if="filteredReturns.length === 0">
              <td colspan="8" style="text-align:center;color:#94a3b8;padding:32px">Chưa có phiếu hoàn hàng nào.</td>
            </tr>
            <tr v-for="ret in filteredReturns" :key="ret.returnId">
              <td><b>{{ ret.returnCode }}</b></td>
              <td>{{ ret.orderCode }}</td>
              <td>
                <div>{{ ret.customerName }}</div>
                <small style="color:#64748b">{{ ret.customerPhone }}</small>
              </td>
              <td>{{ formatDateTime(ret.createdAt) }}</td>
              <td style="max-width:160px;white-space:normal">{{ ret.reason || '—' }}</td>
              <td>{{ formatMoney(ret.refundAmount) }}</td>
              <td>
                <span :class="['status-pill', ret.returnStatus === 'Approved' || ret.returnStatus === 'Refunded' ? 'status-confirmed' : ret.returnStatus === 'Rejected' ? 'status-cancelled' : 'status-pending']">
                  {{ ret.returnStatus === 'Pending' ? 'Chờ duyệt' : ret.returnStatus === 'Approved' ? 'Đã duyệt' : ret.returnStatus === 'Refunded' ? 'Đã hoàn tiền' : 'Từ chối' }}
                </span>
              </td>
              <td>
                <div style="display:flex;gap:6px;flex-wrap:wrap">
                  <button v-if="ret.returnStatus === 'Pending'" class="action-btn" type="button" @click="updateReturnStatus(ret.returnId, 'Approved')">Duyệt</button>
                  <button v-if="ret.returnStatus === 'Approved'" class="action-btn" type="button" @click="updateReturnStatus(ret.returnId, 'Refunded')">Đã hoàn tiền</button>
                  <button v-if="ret.returnStatus === 'Pending'" class="danger-btn-sm" type="button" @click="updateReturnStatus(ret.returnId, 'Rejected')">Từ chối</button>
                </div>
              </td>
            </tr>
          </tbody>
        </table>
      </div>

      <!-- Invoices -->
      <div v-else-if="activePage === 'invoices'" class="table-wrap">
        <div class="table-toolbar">
          <input v-model="invoiceSearch" type="text" placeholder="Tìm mã hóa đơn, mã đơn, khách hàng..." class="search-input" />
          <select v-model="invoiceStatusFilter" class="filter-select">
            <option value="">Tất cả</option>
            <option value="Unpaid">Chưa thanh toán</option>
            <option value="Partial">Thanh toán một phần</option>
            <option value="Paid">Đã thanh toán</option>
          </select>
          <span class="table-count">{{ filteredInvoices.length }} hóa đơn</span>
        </div>
        <table>
          <thead>
            <tr>
              <th>Mã hóa đơn</th><th>Mã đơn hàng</th><th>Khách hàng</th><th>Ngày xuất</th>
              <th>Tổng tiền</th><th>Giảm giá</th><th>Thành tiền</th><th>Thanh toán</th>
            </tr>
          </thead>
          <tbody>
            <tr v-if="filteredInvoices.length === 0">
              <td colspan="8" style="text-align:center;color:#94a3b8;padding:32px">Chưa có hóa đơn nào.</td>
            </tr>
            <tr v-for="inv in filteredInvoices" :key="inv.invoiceId">
              <td><b>{{ inv.invoiceCode }}</b></td>
              <td>{{ inv.orderCode }}</td>
              <td>
                <div>{{ inv.customerName }}</div>
                <small style="color:#64748b">{{ inv.customerPhone }}</small>
              </td>
              <td>{{ formatDateTime(inv.issuedDate) }}</td>
              <td>{{ formatMoney(inv.totalAmount) }}</td>
              <td>{{ formatMoney(inv.discountAmount) }}</td>
              <td><b>{{ formatMoney(inv.finalAmount) }}</b></td>
              <td>
                <span :class="['status-pill', inv.paymentStatus === 'Paid' ? 'status-confirmed' : inv.paymentStatus === 'Partial' ? 'status-shipping' : 'status-pending']">
                  {{ inv.paymentStatus === 'Paid' ? 'Đã TT' : inv.paymentStatus === 'Partial' ? 'Một phần' : 'Chưa TT' }}
                </span>
              </td>
            </tr>
          </tbody>
        </table>
        <div v-if="staffData.orders.length > 0" style="margin-top:16px">
          <p style="font-size:13px;color:#64748b;margin-bottom:8px">Tạo hóa đơn nhanh từ đơn hàng đã hoàn thành:</p>
          <div style="display:flex;flex-wrap:wrap;gap:8px">
            <button
              v-for="order in staffData.orders.filter(o => (o.orderStatus === 'Completed' || o.orderStatus === 2) && !staffData.invoices.find(i => i.orderId === (o.orderId || o.id)))"
              :key="order.orderId || order.id"
              class="action-btn"
              type="button"
              @click="createInvoiceForOrder(order.orderId || order.id, order.customerId)"
            >
              Tạo HĐ cho {{ order.orderCode }}
            </button>
          </div>
        </div>
      </div>

      <!-- Integration/Warehouse (default) -->
      <div v-else class="integration-grid">
        <article>
          <h3>Gateway health</h3>
          <p>{{ integrationHealth.gateway.detail }}</p>
          <b :class="['status-pill', statusClass(integrationHealth.gateway.status)]">{{ integrationHealth.gateway.status }}</b>
        </article>
        <article>
          <h3>OderApi health</h3>
          <p>{{ integrationHealth.orderApi.detail }}</p>
          <b :class="['status-pill', statusClass(integrationHealth.orderApi.status)]">{{ integrationHealth.orderApi.status }}</b>
        </article>
        <article>
          <h3>RabbitMQ health</h3>
          <p>{{ integrationHealth.rabbitmq.detail }}</p>
          <b :class="['status-pill', statusClass(integrationHealth.rabbitmq.status)]">{{ integrationHealth.rabbitmq.status }}</b>
        </article>
        <article>
          <h3>ProductStockCaches</h3>
          <p>Consume `stock.updated` để cập nhật tồn kho hiển thị cho khách.</p>
          <b>{{ products.length }}</b>
        </article>
        <article>
          <h3>Outbox order.created</h3>
          <p>Publish `order.created` sau khi đặt hàng thành công.</p>
          <b>{{ staffData.outbox.length }}</b>
        </article>
      </div>
    </section>
  </main>
</template>
