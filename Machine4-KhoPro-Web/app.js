const API_HOST = window.location.hostname || "localhost";
const GATEWAY_API = `http://${API_HOST}:7000/gateway`;
const USER_API = `${GATEWAY_API}/user/api`;
const PRODUCT_API = `${GATEWAY_API}/product/api`;
const ORDER_API = `${GATEWAY_API}/order/api`;
const USER_SERVICE_API = `http://${API_HOST}:8083/api`;

const state = {
  token: localStorage.getItem("khopro_token") || "",
  user: JSON.parse(localStorage.getItem("khopro_user") || "null"),
  loginRole: localStorage.getItem("khopro_login_role") || "user",
  page: localStorage.getItem("khopro_page") || "dashboard",
  products: [],
  categories: [],
  suppliers: [],
  movements: [],
  users: [],
  summary: null,
  saleCart: [],
  orders: JSON.parse(localStorage.getItem("khopro_orders") || "[]"),
  revenuePeriod: localStorage.getItem("khopro_revenue_period") || "day",
  revenueReferenceDate: localStorage.getItem("khopro_revenue_date") || "",
  inventoryHistoryPeriod: localStorage.getItem("khopro_inventory_history_period") || "day",
  inventoryHistoryReferenceDate: localStorage.getItem("khopro_inventory_history_date") || "",
  inventoryTab: localStorage.getItem("khopro_inventory_tab") || "adjustments",
  salesTab: localStorage.getItem("khopro_sales_tab") || "orders",
  inventorySelected: {
    adjustments: new Set(),
    receipts: new Set(),
    exports: new Set()
  },
  receipts: [],
  productPage: 1,
  userPage: 1,
  selectedProductIds: new Set(),
  selectedUserIds: new Set()
};

const $ = (selector, root = document) => root.querySelector(selector);
const $$ = (selector, root = document) => [...root.querySelectorAll(selector)];
let presenceTimer = null;
let revenueResizeTimer = null;
let reservationTimer = null;
let reservationSyncRunning = false;
let lastReservationSync = 0;
let inventorySyncTimer = null;

document.addEventListener("DOMContentLoaded", () => {
  $("#todayLabel").textContent = new Date().toLocaleDateString("vi-VN", {
    weekday: "long",
    day: "2-digit",
    month: "2-digit",
    year: "numeric"
  });
  $("#storeName").title = `API: ${PRODUCT_API} | ${USER_API}`;
  if (!state.revenueReferenceDate) {
    state.revenueReferenceDate = dateInputValue(new Date());
  }
  $("#revenueReferenceDate").value = state.revenueReferenceDate;
  if (!state.inventoryHistoryReferenceDate) {
    state.inventoryHistoryReferenceDate = dateInputValue(new Date());
  }
  $$("[data-inventory-history-date]").forEach((input) => {
    input.value = state.inventoryHistoryReferenceDate;
  });

  bindHome();
  bindAuth();
  bindNavigation();
  bindForms();
  bindModals();

  if (state.token) {
    if (canEnterWarehouse(state.user)) {
      showApp();
      loadAll();
    } else {
      clearWarehouseSession();
      redirectToShop();
    }
  } else {
    showAuth("login");
  }
});

window.addEventListener("resize", () => {
  clearTimeout(revenueResizeTimer);
  revenueResizeTimer = setTimeout(() => {
    if (state.page === "dashboard") {
      renderCharts();
    }
    if (state.page === "sales" && $('[data-sales-tab="revenue"]')?.classList.contains("active")) {
      drawDailyRevenueChart($("#dailyRevenueChart"), getRevenueSelection().rows);
    }
  }, 150);
});

window.addEventListener("pagehide", () => {
  notifyOffline();
});

function bindHome() {
  $$("[data-home-auth]").forEach((button) => {
    button.addEventListener("click", () => {
      showAuth(button.dataset.homeAuth);
    });
  });
}

function bindAuth() {
  setLoginRole(state.loginRole);

  $$("[data-login-role]").forEach((button) => {
    button.addEventListener("click", () => setLoginRole(button.dataset.loginRole));
  });

  $$("[data-auth-tab]").forEach((button) => {
    button.addEventListener("click", () => {
      $$("[data-auth-tab]").forEach((item) => item.classList.remove("active"));
      button.classList.add("active");
      const tab = button.dataset.authTab;
      $$("[data-auth-form]").forEach((form) => form.classList.toggle("hidden", form.dataset.authForm !== tab));
    });
  });

  $("#loginForm").addEventListener("submit", async (event) => {
    event.preventDefault();
    const form = event.currentTarget;
    const submit = form.querySelector('[type="submit"]');
    submit.disabled = true;
    try {
      const data = formData(form);
      const response = await request(`${USER_API}/auth/login`, {
        method: "POST",
        body: data,
        auth: false
      });

      state.token = response.accessToken;
      state.user = response.user;

      if (!canEnterWarehouse(state.user)) {
        clearWarehouseSession();
        showToast("TÃ i khoáº£n khÃ¡ch hÃ ng chá»‰ Ä‘Æ°á»£c Ä‘Äƒng nháº­p táº¡i web mua hÃ ng.");
        setTimeout(redirectToShop, 500);
        return;
      }

      localStorage.setItem("khopro_token", state.token);
      localStorage.setItem("khopro_user", JSON.stringify(state.user));
      localStorage.setItem("khopro_login_role", ["admin-user", "Admin"].includes(state.user.role) ? "admin" : "user");
      showToast("ÄÄƒng nháº­p thÃ nh cÃ´ng");
      showApp();
      await loadAll();
    } catch (error) {
      showToast(error.message.includes("401")
        ? "Email hoáº·c máº­t kháº©u khÃ´ng Ä‘Ãºng."
        : error.message || "KhÃ´ng thá»ƒ Ä‘Äƒng nháº­p.");
    } finally {
      submit.disabled = false;
    }
  });

  $("#registerForm").addEventListener("submit", async (event) => {
    event.preventDefault();
    const form = event.currentTarget;
    const submit = form.querySelector('[type="submit"]');
    submit.disabled = true;
    try {
      const payload = formData(form);
      try {
        await request(`${USER_SERVICE_API}/auth/register`, {
          method: "POST",
          body: payload,
          auth: false
        });
      } catch (directError) {
        console.warn("Direct user-service register failed, falling back to gateway:", directError);
        await request(`${USER_API}/auth/register`, {
          method: "POST",
          body: payload,
          auth: false
        });
      }
      const email = form.elements.email.value;
      form.reset();
      $("#loginForm").elements.email.value = email;
      showToast("Táº¡o tÃ i khoáº£n thÃ nh cÃ´ng. HÃ£y Ä‘Äƒng nháº­p.");
      $('[data-auth-tab="login"]').click();
    } catch (error) {
      showToast(error.message.includes("409")
        ? "Email nÃ y Ä‘Ã£ Ä‘Æ°á»£c Ä‘Äƒng kÃ½."
        : error.message || "KhÃ´ng thá»ƒ Ä‘Äƒng kÃ½ tÃ i khoáº£n.");
    } finally {
      submit.disabled = false;
    }
  });

  $("#logoutBtn").addEventListener("click", async () => {
    try {
      await request(`${USER_API}/auth/logout`, { method: "POST" });
    } catch {
      // Logout local even if API is down.
    }
    localStorage.removeItem("khopro_token");
    localStorage.removeItem("khopro_user");
    state.token = "";
    state.user = null;
    state.loginRole = "user";
    localStorage.removeItem("khopro_login_role");
    localStorage.removeItem("khopro_page");
    stopPresenceHeartbeat();
    stopReservationTimer();
    setLoginRole("user");
    showAuth();
  });
}

function bindNavigation() {
  $$("#nav .nav-item").forEach((button) => {
    button.addEventListener("click", () => setPage(button.dataset.page));
  });

  $("#refreshBtn").addEventListener("click", loadAll);
  $("#productSearch").addEventListener("input", () => {
    state.productPage = 1;
    renderProducts();
  });
  $("#productCategoryFilter").addEventListener("change", () => {
    state.productPage = 1;
    renderProducts();
  });
  $("#selectAllProducts").addEventListener("change", toggleAllProducts);
  $("#selectAllUsers").addEventListener("change", toggleAllUsers);
  $("#userSearch").addEventListener("input", () => {
    state.userPage = 1;
    renderUsers();
  });
  $("#bulkDeleteProductsBtn").addEventListener("click", deleteSelectedProducts);
  $("#bulkDeleteUsersBtn").addEventListener("click", deleteSelectedUsers);
  $("#addCategoryBtn").addEventListener("click", openCategoryCreate);
  $("#saleProductPicker").addEventListener("input", syncSaleProductPicker);
  $("#saleProductPicker").addEventListener("change", syncSaleProductPicker);
  $("#salePaymentMethod").addEventListener("change", updateSalePaymentType);
  $("#saleQuantityInput").addEventListener("input", clampSaleQuantity);
  $$("[data-inventory-tab]").forEach((button) => {
    button.addEventListener("click", () => setInventoryTab(button.dataset.inventoryTab));
  });
  $$(".inventory-form").forEach((form) => {
    const categorySelect = form.querySelector("[data-inventory-category]");
    const searchInput = form.querySelector("[data-inventory-search]");
    categorySelect?.addEventListener("change", () => renderInventoryProductSelect(form));
    searchInput?.addEventListener("input", () => renderInventoryProductSelect(form));
  });
  $$("[data-inventory-history-period]").forEach((button) => {
    button.addEventListener("click", () => {
      state.inventoryHistoryPeriod = button.dataset.inventoryHistoryPeriod;
      localStorage.setItem("khopro_inventory_history_period", state.inventoryHistoryPeriod);
      renderInventory();
    });
  });
  $$("[data-inventory-history-date]").forEach((input) => {
    input.addEventListener("change", (event) => {
      state.inventoryHistoryReferenceDate = event.currentTarget.value || dateInputValue(new Date());
      localStorage.setItem("khopro_inventory_history_date", state.inventoryHistoryReferenceDate);
      renderInventory();
    });
  });
  $$("[data-inventory-cancel-edit]").forEach((button) => {
    button.addEventListener("click", () => {
      const form = button.closest("form");
      if (form?.id === "movementForm") resetInventoryEdit("adjustments");
      if (form?.id === "receiptForm") resetInventoryEdit("receipts");
      if (form?.id === "exportForm") resetInventoryEdit("exports");
    });
  });
  document.addEventListener("click", (event) => {
    const productPageButton = event.target.closest("[data-product-page]");
    if (productPageButton) {
      state.productPage = Number(productPageButton.dataset.productPage) || 1;
      renderProducts();
      return;
    }
    const productPageNav = event.target.closest("[data-product-page-nav]");
    if (productPageNav) {
      const totalPages = Math.max(1, Math.ceil(getVisibleProducts().length / 5));
      if (productPageNav.dataset.productPageNav === "prev") {
        state.productPage = Math.max(1, state.productPage - 1);
      } else {
        state.productPage = Math.min(totalPages, state.productPage + 1);
      }
      renderProducts();
      return;
    }

    const userPageButton = event.target.closest("[data-user-page]");
    if (userPageButton) {
      state.userPage = Number(userPageButton.dataset.userPage) || 1;
      renderUsers();
      return;
    }
    const userPageNav = event.target.closest("[data-user-page-nav]");
    if (userPageNav) {
      const totalPages = Math.max(1, Math.ceil(getVisibleUsers().length / 5));
      if (userPageNav.dataset.userPageNav === "prev") {
        state.userPage = Math.max(1, state.userPage - 1);
      } else {
        state.userPage = Math.min(totalPages, state.userPage + 1);
      }
      renderUsers();
    }
  });
  $$("[data-sales-tab]").forEach((button) => {
    button.addEventListener("click", () => setSalesTab(button.dataset.salesTab));
  });
  $(".sales-tabs")?.addEventListener("click", (event) => {
    const button = event.target.closest("[data-sales-tab]");
    if (!button) return;
    setSalesTab(button.dataset.salesTab);
  });
  $$("[data-revenue-period]").forEach((button) => {
    button.addEventListener("click", () => {
      state.revenuePeriod = button.dataset.revenuePeriod;
      localStorage.setItem("khopro_revenue_period", state.revenuePeriod);
      renderRevenueAnalytics();
    });
  });
  $("#revenueReferenceDate").addEventListener("change", (event) => {
    state.revenueReferenceDate = event.currentTarget.value || dateInputValue(new Date());
    localStorage.setItem("khopro_revenue_date", state.revenueReferenceDate);
    renderRevenueAnalytics();
  });
  $("#revenueTodayBtn").addEventListener("click", () => {
    state.revenueReferenceDate = dateInputValue(new Date());
    $("#revenueReferenceDate").value = state.revenueReferenceDate;
    localStorage.setItem("khopro_revenue_date", state.revenueReferenceDate);
    renderRevenueAnalytics();
  });
}

function bindForms() {
  $("#productForm").addEventListener("submit", async (event) => {
    event.preventDefault();
    if (!canManageCatalog()) {
      showToast("TÃ i khoáº£n nÃ y chá»‰ cÃ³ quyá»n xem sáº£n pháº©m, khÃ´ng cÃ³ quyá»n thÃªm/sá»­a.");
      return;
    }

    const form = event.currentTarget;
    await applySelectedProductImage(form);
    const data = formData(form);
    delete data.imageFile;
    data.categoryId = await resolveCategoryId(data.categoryName);
    delete data.categoryName;
    const id = data.id;
    delete data.id;

    if (id) {
      await request(`${PRODUCT_API}/products/${id}`, { method: "PUT", body: data });
      showToast("ÄÃ£ cáº­p nháº­t sáº£n pháº©m");
    } else {
      await request(`${PRODUCT_API}/products`, { method: "POST", body: data });
      showToast("ÄÃ£ thÃªm sáº£n pháº©m");
    }

    $("#productModal").close();
    form.reset();
    await loadInventoryData();
    renderAll();
  });

  $("#productForm input[name='imageFile']").addEventListener("change", async (event) => {
    const file = event.currentTarget.files?.[0];
    if (!file) return;
    const image = await readImageFile(file);
    $("#productForm input[name='image']").value = image;
    renderProductImagePreview(image);
  });

  $("#productForm input[name='image']").addEventListener("input", (event) => {
    renderProductImagePreview(event.currentTarget.value);
  });

  $("#supplierForm").addEventListener("submit", async (event) => {
    event.preventDefault();
    if (!canManageCatalog()) {
      showToast("TÃ i khoáº£n nÃ y khÃ´ng cÃ³ quyá»n thÃªm/sá»­a nhÃ  cung cáº¥p.");
      return;
    }

    const form = event.currentTarget;
    const data = formData(form);
    const id = data.id;
    delete data.id;

    if (id) {
      await request(`${PRODUCT_API}/suppliers/${id}`, { method: "PUT", body: data });
      showToast("ÄÃ£ cáº­p nháº­t nhÃ  cung cáº¥p");
    } else {
      await request(`${PRODUCT_API}/suppliers`, { method: "POST", body: data });
      showToast("ÄÃ£ thÃªm nhÃ  cung cáº¥p");
    }

    $("#supplierModal").close();
    form.reset();
    await loadInventoryData();
    renderAll();
  });

  $("#userForm").addEventListener("submit", async (event) => {
    event.preventDefault();
    if (!isAdminMode()) {
      showToast("Báº¡n khÃ´ng cÃ³ quyá»n chá»‰nh sá»­a tÃ i khoáº£n.");
      return;
    }

    const form = event.currentTarget;
    const data = formData(form);
    const id = data.id;
    delete data.id;
    delete data.email;

    if (!id) {
      showToast("KhÃ´ng tÃ¬m tháº¥y tÃ i khoáº£n cáº§n sá»­a.");
      return;
    }

    if (Object.prototype.hasOwnProperty.call(data, "isActive")) {
      data.isActive = data.isActive === "true";
    }

    await request(`${USER_API}/users/${id}`, { method: "PUT", body: data });
    showToast("ÄÃ£ cáº­p nháº­t tÃ i khoáº£n");
    $("#userModal").close();
    form.reset();
    await loadUsersData();
    renderUsers();
  });

  $("#categoryForm").addEventListener("submit", async (event) => {
    event.preventDefault();
    if (!canManageCatalog()) {
      showToast("TÃ i khoáº£n nÃ y khÃ´ng cÃ³ quyá»n thÃªm danh má»¥c.");
      return;
    }

    const form = event.currentTarget;
    const data = formData(form);
    const id = data.id;
    delete data.id;

    if (id) {
      await request(`${PRODUCT_API}/categories/${id}`, {
        method: "PUT",
        body: data
      });
      showToast("ÄÃ£ cáº­p nháº­t danh má»¥c");
    } else {
      await request(`${PRODUCT_API}/categories`, {
        method: "POST",
        body: data
      });
      showToast("ÄÃ£ thÃªm danh má»¥c");
    }

    resetCategoryForm();
    await loadInventoryData();
    renderAll();
  });

  $("#cancelCategoryEditBtn").addEventListener("click", resetCategoryForm);

  $("#movementForm").addEventListener("submit", async (event) => {
    event.preventDefault();
    if (!canManageInventory()) {
      showToast("Tài khoản này không có quyền điều chỉnh tồn kho.");
      return;
    }

    const form = event.currentTarget;
    const data = formData(form);
    const requestId = data.requestId;
    const productId = data.productId;
    delete data.requestId;
    delete data.productId;
    data.quantity = Number(data.quantity || 0);

    if (requestId) {
      await request(`${PRODUCT_API}/inventory/movements/${requestId}`, {
        method: "PUT",
        body: data
      });
      showToast("Đã cập nhật lệnh điều chỉnh");
    } else {
      await request(`${PRODUCT_API}/products/${productId}/stock/adjust`, {
        method: "POST",
        body: data
      });
      showToast("Đã tạo lệnh điều chỉnh");
    }
    form.reset();
    form.querySelector("input[name='requestId']").value = "";
    form.querySelector("[data-inventory-cancel-edit]")?.classList.add("hidden");
    await loadInventoryData();
    renderAll();
  });

  $("#receiptForm").addEventListener("submit", async (event) => {
    event.preventDefault();
    if (!canManageInventory()) {
      showToast("Tài khoản này không có quyền tạo phiếu nhập.");
      return;
    }

    const form = event.currentTarget;
    const data = formData(form);
    const requestId = data.requestId;
    if (requestId) {
      await request(`${PRODUCT_API}/inventory/receipts/${requestId}`, {
        method: "PUT",
        body: {
          supplierId: data.supplierId || "",
          lines: [{
            productId: data.productId,
            quantity: Number(data.quantity || 0),
            note: data.note || ""
          }]
        }
      });
      showToast("Đã cập nhật phiếu nhập");
    } else {
      const receipt = await request(`${PRODUCT_API}/inventory/receipts`, {
        method: "POST",
        body: {
          supplierId: data.supplierId || "",
          lines: [{
            productId: data.productId,
            quantity: Number(data.quantity || 0),
            note: data.note || ""
          }]
        }
      });
      showToast("Đã tạo phiếu nhập chờ duyệt");
    }
    form.reset();
    form.querySelector("input[name='requestId']").value = "";
    form.querySelector("[data-inventory-cancel-edit]")?.classList.add("hidden");
    await loadInventoryData();
    renderAll();
  });

  $("#exportForm").addEventListener("submit", async (event) => {
    event.preventDefault();
    const form = event.currentTarget;
    const data = formData(form);
    const noteParts = [data.note || "", data.receiver ? `Người nhận: ${data.receiver}` : ""].filter(Boolean);

    if (data.requestId) {
      await request(`${PRODUCT_API}/inventory/movements/${data.requestId}`, {
        method: "PUT",
        body: {
          type: "out",
          quantity: Number(data.quantity || 0),
          note: noteParts.join(" - ")
        }
      });
      showToast("Đã cập nhật phiếu xuất kho");
    } else {
      await request(`${PRODUCT_API}/products/${data.productId}/stock/adjust`, {
        method: "POST",
        body: {
          type: "out",
          quantity: Number(data.quantity || 0),
          note: noteParts.join(" - ")
        }
      });
      showToast("Đã tạo phiếu xuất kho chờ duyệt");
    }

    form.reset();
    form.querySelector("input[name='requestId']").value = "";
    form.querySelector("[data-inventory-cancel-edit]")?.classList.add("hidden");
    await loadInventoryData();
    renderAll();
  });

  $("#profileForm").addEventListener("submit", async (event) => {
    event.preventDefault();
    const response = await request(`${USER_API}/users/me`, {
      method: "PUT",
      body: formData(event.currentTarget)
    });
    state.user = response.user;
    localStorage.setItem("khopro_user", JSON.stringify(state.user));
    renderUser();
    showToast("ÄÃ£ lÆ°u há»“ sÆ¡");
  });

  $("#addSaleLineBtn").addEventListener("click", addSaleLine);
  $("#salesForm").addEventListener("submit", createSaleOrder);
  $("#clearOrdersBtn").addEventListener("click", () => {
    if (state.orders.some((order) => order.status === "pending_cod" || order.status === "deposit_holding")) {
      showToast("HÃ£y xá»­ lÃ½ cÃ¡c Ä‘Æ¡n COD / á»©ng cá»c Ä‘ang giá»¯ hÃ ng trÆ°á»›c khi xÃ³a lá»‹ch sá»­.");
      return;
    }
    if (!confirm("XÃ³a lá»‹ch sá»­ Ä‘Æ¡n bÃ¡n local?")) return;
    state.orders = [];
    localStorage.setItem("khopro_orders", JSON.stringify(state.orders));
    renderSales();
  });
}

function bindModals() {
  $$("[data-open-modal]").forEach((button) => {
    button.addEventListener("click", () => {
      const modal = $(`#${button.dataset.openModal}`);
      if (modal.id === "productModal") openProductModal();
      if (modal.id === "supplierModal") openSupplierModal();
    });
  });

  $$("[data-close-modal]").forEach((button) => {
    button.addEventListener("click", () => button.closest("dialog").close());
  });
}

async function loadAll() {
  try {
    const results = await Promise.allSettled([
      loadInventoryData(),
      loadUsersData(),
      loadOrderData()
    ]);
    const failed = results.find((result) => result.status === "rejected");
    if (failed?.status === "rejected") {
      const error = failed.reason;
      showToast(error?.message || "KhÃƒÂ´ng tÃ¡ÂºÂ£i Ã„â€˜Ã†Â°Ã¡Â»Â£c mÃ¡Â»â„¢t phÃ¡ÂºÂ§n dÃ¡Â»Â¯ liÃ¡Â»â€¡u, Ã„â€˜ang dÃ¡Â»Â±a trÃƒÂªn dÃ¡Â»Â¯ liÃ¡Â»â€¡u Ã„â€˜ÃƒÂ£ lÃ†Â°u.");
      if (isAuthError(error)) {
        forceLogout("TÃƒÂ i khoÃ¡ÂºÂ£n Ã„â€˜ÃƒÂ£ bÃ¡Â»â€¹ khÃƒÂ³a hoÃ¡ÂºÂ·c phiÃƒÂªn Ã„â€˜Ã„Æ’ng nhÃ¡ÂºÂ­p khÃƒÂ´ng cÃƒÂ²n hÃ¡Â»Â£p lÃ¡Â»â€¡.");
        return;
      }
    }
    await syncReservations();
    renderAll();
    startReservationTimer();
  } catch (error) {
    showToast(error.message || "KhÃ´ng táº£i Ä‘Æ°á»£c dá»¯ liá»‡u");
    if (isAuthError(error)) {
      forceLogout("TÃ i khoáº£n Ä‘Ã£ bá»‹ khÃ³a hoáº·c phiÃªn Ä‘Äƒng nháº­p khÃ´ng cÃ²n há»£p lá»‡.");
    }
  }
}

async function loadOrderData() {
  if (!canAccessSales()) return;

  const localOrders = JSON.parse(localStorage.getItem("khopro_orders") || "[]");
  try {
    const remoteOrders = await request(`${ORDER_API}/Orders`);
    const normalizedRemote = remoteOrders.map(normalizeRemoteOrder);
    const remoteIds = new Set(normalizedRemote.map((order) => order.id));
    state.orders = [
      ...normalizedRemote,
      ...localOrders.filter((order) => !remoteIds.has(order.id))
    ].sort((a, b) => new Date(b.createdAt) - new Date(a.createdAt));
  } catch (error) {
    if (Array.isArray(localOrders) && localOrders.length) {
      state.orders = localOrders
        .slice()
        .sort((a, b) => new Date(b.createdAt) - new Date(a.createdAt));
    }
    throw error;
  }
}

function normalizeRemoteOrder(order) {
  const status = String(order.orderStatus || order.status || "Pending").toLowerCase();
  return {
    id: order.orderId || order.id,
    code: order.orderCode || order.code,
    customerId: order.customerId || "",
    customerName: order.customerName || "KhÃ¡ch hÃ ng",
    email: order.customerEmail || order.email || "",
    phone: order.customerPhone || order.phone || "",
    address: order.customerAddress || order.address || "",
    paymentMethod: order.paymentMethod || "Cash",
    paymentStatus: order.paymentStatus || "Unpaid",
    items: (order.items || []).map((item) => ({
      productId: item.productId,
      name: item.productName || item.name || "Sáº£n pháº©m",
      categoryName: item.categoryName || "",
      quantity: number(item.quantity),
      price: number(item.unitPrice ?? item.price),
      subTotal: number(item.subTotal)
    })),
    subTotal: number(order.totalAmount),
    discountAmount: number(order.discountAmount),
    finalAmount: number(order.finalAmount || order.totalAmount),
    paidAmount: number(order.paidAmount),
    debtAmount: number(order.debtAmount),
    createdAt: order.orderDate || order.createdAt,
    status: status === "pending"
      ? "web_pending"
      : status === "cancelled"
        ? "cancelled"
        : status === "confirmed" && String(order.paymentStatus || "").toLowerCase() === "partial"
          ? "deposit_holding"
          : "completed",
    source: order.source || "OrderAPI"
  };
}

async function loadInventoryData() {
  const [products, categories, suppliers, movements, receipts, summary] = await Promise.all([
    request(`${PRODUCT_API}/products`),
    request(`${PRODUCT_API}/categories`),
    request(`${PRODUCT_API}/suppliers`),
    request(`${PRODUCT_API}/inventory/movements`),
    request(`${PRODUCT_API}/inventory/receipts`),
    request(`${PRODUCT_API}/inventory/summary`)
  ]);

  state.products = products;
  state.categories = categories;
  state.suppliers = suppliers;
  state.movements = movements;
  state.receipts = receipts;
  state.summary = summary;
}

async function loadUsersData() {
  try {
    const me = await request(`${USER_API}/users/me`);
    state.user = me.user;
    localStorage.setItem("khopro_user", JSON.stringify(state.user));

    if (isAdminMode()) {
      state.users = await request(`${USER_API}/users`);
    } else {
      state.users = [state.user];
    }
  } catch (error) {
    if (isAuthError(error)) {
      throw error;
    }

    state.users = state.user ? [state.user] : [];
  }
}

async function request(url, options = {}) {
  const init = {
    method: options.method || "GET",
    headers: {
      "Content-Type": "application/json"
    }
  };

  if (options.auth !== false && state.token) {
    init.headers.Authorization = `Bearer ${state.token}`;
  }

  if (options.body !== undefined) {
    init.body = JSON.stringify(options.body);
  }

  const response = await fetch(url, init);
  if (!response.ok) {
    let message = `${response.status} ${response.statusText}`;
    try {
      const data = await response.json();
      message = data.message || message;
    } catch {
      // Keep HTTP message.
    }
    throw new Error(message);
  }

  const text = await response.text();
  return text ? JSON.parse(text) : null;
}

function isAuthError(error) {
  const message = String(error?.message || error || "");
  return message.includes("401") || message.includes("403") || message.toLowerCase().includes("unauthorized") || message.toLowerCase().includes("forbidden");
}

function forceLogout(message = "PhiÃªn Ä‘Äƒng nháº­p khÃ´ng cÃ²n há»£p lá»‡.") {
  localStorage.removeItem("khopro_token");
  localStorage.removeItem("khopro_user");
  localStorage.removeItem("khopro_login_role");
  localStorage.removeItem("khopro_page");
  state.token = "";
  state.user = null;
  state.users = [];
  state.loginRole = "user";
  stopPresenceHeartbeat();
  stopReservationTimer();
  setLoginRole("user");
  showToast(message);
  showAuth("login");
}

function startPresenceHeartbeat() {
  if (presenceTimer || !state.token) return;

  presenceTimer = setInterval(async () => {
    if (!state.token) {
      stopPresenceHeartbeat();
      return;
    }

    try {
      await request(`${USER_API}/users/me`);
      if (isAdminMode()) {
        await loadUsersData();
        renderUsers();
      }
      if (canAccessSales()) {
        await loadOrderData();
        renderSales();
      }
    } catch (error) {
      if (isAuthError(error)) {
        forceLogout("TÃ i khoáº£n Ä‘Ã£ bá»‹ khÃ³a hoáº·c phiÃªn Ä‘Äƒng nháº­p khÃ´ng cÃ²n há»£p lá»‡.");
      }
    }
  }, 1500);
}

function stopPresenceHeartbeat() {
  if (!presenceTimer) return;
  clearInterval(presenceTimer);
  presenceTimer = null;
}

function notifyOffline() {
  if (!state.token) return;

  const data = new URLSearchParams();
  data.set("accessToken", state.token);
  navigator.sendBeacon(`${USER_API}/auth/offline`, data);
}

function stopReservationTimer() {
  if (!reservationTimer) return;
  clearInterval(reservationTimer);
  reservationTimer = null;
}

function startInventorySync() {
  if (inventorySyncTimer) return;
  inventorySyncTimer = setInterval(async () => {
    if (state.page !== "inventory") return;
    try {
      await loadInventoryData();
      renderAll();
    } catch (error) {
      if (!isAuthError(error)) {
        console.warn("Inventory sync failed:", error);
      }
    }
  }, 5000);
}

function stopInventorySync() {
  if (!inventorySyncTimer) return;
  clearInterval(inventorySyncTimer);
  inventorySyncTimer = null;
}

function renderAll() {
  applyPermissions();
  renderUser();
  renderStats();
  renderCharts();
  renderProducts();
  renderInventory();
  renderCategories();
  renderSuppliers();
  renderUsers();
  renderProfile();
  renderSales();
  fillSelects();
}

function renderUser() {
  const name = state.user?.name || state.user?.email || "NgÆ°á»i dÃ¹ng";
  $("#userPill").textContent = `${name} Â· ${roleText(state.user?.role)}`;
  $("#storeName").textContent = state.user?.storeName || "Local";
}

function renderStats() {
  const summary = state.summary || {};
  const totalValue = state.products.reduce((sum, item) => sum + number(item.price) * number(item.stock), 0);
  const stats = [
    ["pe-7s-box1", "Sáº£n pháº©m", summary.totalProducts ?? state.products.length],
    ["pe-7s-repeat", "Tá»•ng tá»“n", summary.totalStock ?? 0],
    ["pe-7s-attention", "Sáº¯p háº¿t hÃ ng", summary.lowStockProducts ?? 0],
    ["pe-7s-cash", "GiÃ¡ trá»‹ tá»“n", money(totalValue)]
  ];

  $("#stats").innerHTML = stats.map(([icon, label, value]) => `
    <div class="stat">
      <i class="stat-icon ${icon}" aria-hidden="true"></i>
      <span>${label}</span>
      <strong>${value}</strong>
    </div>
  `).join("");

  const lowStock = state.products.filter((item) => number(item.stock) <= number(item.minimumStock));
  $("#lowStockList").innerHTML = lowStock.length
    ? lowStock.map((item) => listItem(item.name, `Tá»“n ${item.stock} / Tá»‘i thiá»ƒu ${item.minimumStock}`, `<span class="badge warn">Cáº§n nháº­p</span>`)).join("")
    : empty("ChÆ°a cÃ³ sáº£n pháº©m sáº¯p háº¿t hÃ ng");

  $("#recentMovements").innerHTML = state.movements.slice(0, 6).map((item) =>
    listItem(item.productName || "Sáº£n pháº©m", `${movementLabel(item.type)} ${item.quantity} - ${dateTime(item.createdAt)}`, `<span class="badge ${item.type === "out" ? "danger" : "ok"}">${item.type}</span>`)
  ).join("") || empty("ChÆ°a cÃ³ giao dá»‹ch kho");
}

function renderCharts() {
  const dashboard = $('[data-page-panel="dashboard"]');
  if (!dashboard || dashboard.classList.contains("hidden")) return;
  drawMovementLineChart($("#movementLineChart"));
  drawStockBarChart($("#stockBarChart"));
  drawStockPieChart($("#stockPieChart"));
}

function setupCanvas(canvas) {
  if (!canvas) return null;
  const rect = canvas.getBoundingClientRect();
  const width = Math.max(320, Math.floor(rect.width || canvas.parentElement?.clientWidth || 320));
  const height = Number(canvas.getAttribute("height")) || 230;
  const dpr = window.devicePixelRatio || 1;
  canvas.width = width * dpr;
  canvas.height = height * dpr;
  canvas.style.height = `${height}px`;
  const ctx = canvas.getContext("2d");
  ctx.setTransform(dpr, 0, 0, dpr, 0, 0);
  ctx.clearRect(0, 0, width, height);
  ctx.font = "12px Segoe UI, Arial, sans-serif";
  ctx.lineCap = "round";
  ctx.lineJoin = "round";
  return { ctx, width, height };
}

function drawEmptyChart(ctx, width, height, text) {
  ctx.fillStyle = "#f8fafc";
  ctx.fillRect(0, 0, width, height);
  ctx.fillStyle = "#94a3b8";
  ctx.textAlign = "center";
  ctx.fillText(text, width / 2, height / 2);
}

function drawMovementLineChart(canvas) {
  const chart = setupCanvas(canvas);
  if (!chart) return;
  const { ctx, width, height } = chart;
  const padding = { top: 48, right: 18, bottom: 34, left: 42 };
  const days = [...Array(7)].map((_, index) => {
    const date = new Date();
    date.setDate(date.getDate() - (6 - index));
    return date;
  });
  const labels = days.map((date) => date.toLocaleDateString("vi-VN", { day: "2-digit", month: "2-digit" }));
  const inData = days.map((date) => totalMovementByDate(date, "in"));
  const outData = days.map((date) => totalMovementByDate(date, "out"));
  const maxValue = Math.max(1, ...inData, ...outData);

  drawChartFrame(ctx, width, height, padding, maxValue);
  drawLineSeries(ctx, width, height, padding, inData, maxValue, "#0f766e");
  drawLineSeries(ctx, width, height, padding, outData, maxValue, "#2563eb");
  drawXAxisLabels(ctx, width, height, padding, labels);
  drawLegend(ctx, [["Nhap", "#0f766e"], ["Xuat", "#2563eb"]]);
}

function drawStockBarChart(canvas) {
  const chart = setupCanvas(canvas);
  if (!chart) return;
  const { ctx, width, height } = chart;
  const items = [...state.products]
    .sort((a, b) => number(b.stock) - number(a.stock))
    .slice(0, 6);

  if (!items.length) {
    drawEmptyChart(ctx, width, height, "Chua co du lieu san pham");
    return;
  }

  const padding = { top: 18, right: 18, bottom: 52, left: 38 };
  const maxValue = Math.max(1, ...items.map((item) => number(item.stock)));
  drawChartFrame(ctx, width, height, padding, maxValue);

  const innerWidth = width - padding.left - padding.right;
  const innerHeight = height - padding.top - padding.bottom;
  const barGap = 12;
  const barWidth = Math.max(16, (innerWidth - barGap * (items.length - 1)) / items.length);

  items.forEach((item, index) => {
    const value = number(item.stock);
    const barHeight = (value / maxValue) * innerHeight;
    const x = padding.left + index * (barWidth + barGap);
    const y = padding.top + innerHeight - barHeight;
    ctx.fillStyle = index % 2 ? "#2563eb" : "#0f766e";
    roundRect(ctx, x, y, barWidth, barHeight, 6);
    ctx.fill();
    ctx.fillStyle = "#64748b";
    ctx.textAlign = "center";
    ctx.fillText(shortLabel(item.name || "Sáº£n pháº©m"), x + barWidth / 2, height - 22);
  });
}

function drawStockPieChart(canvas) {
  const chart = setupCanvas(canvas);
  if (!chart) return;
  const { ctx, width, height } = chart;
  const low = state.products.filter((item) => number(item.stock) <= number(item.minimumStock)).length;
  const ok = state.products.filter((item) => number(item.stock) > number(item.minimumStock)).length;
  const off = state.products.filter((item) => item.status === "Ngung ban").length;
  const segments = [
    ["On dinh", Math.max(0, ok - off), "#0f766e"],
    ["Sap het", low, "#d97706"],
    ["Ngung ban", off, "#b42318"]
  ].filter((item) => item[1] > 0);

  if (!segments.length) {
    drawEmptyChart(ctx, width, height, "Chua co du lieu ton kho");
    return;
  }

  const total = segments.reduce((sum, item) => sum + item[1], 0);
  const radius = Math.min(width, height) * 0.28;
  const cx = width * 0.42;
  const cy = height * 0.48;
  let start = -Math.PI / 2;

  segments.forEach((segment) => {
    const angle = (segment[1] / total) * Math.PI * 2;
    ctx.beginPath();
    ctx.moveTo(cx, cy);
    ctx.arc(cx, cy, radius, start, start + angle);
    ctx.closePath();
    ctx.fillStyle = segment[2];
    ctx.fill();
    start += angle;
  });

  ctx.fillStyle = "#ffffff";
  ctx.beginPath();
  ctx.arc(cx, cy, radius * 0.55, 0, Math.PI * 2);
  ctx.fill();
  ctx.fillStyle = "#111827";
  ctx.textAlign = "center";
  ctx.font = "700 20px Segoe UI, Arial, sans-serif";
  ctx.fillText(String(total), cx, cy + 6);
  ctx.font = "12px Segoe UI, Arial, sans-serif";
  drawLegend(ctx, segments.map((item) => [item[0], item[2]]), width * 0.68, 62);
}

function drawChartFrame(ctx, width, height, padding, maxValue) {
  const innerWidth = width - padding.left - padding.right;
  const innerHeight = height - padding.top - padding.bottom;
  ctx.strokeStyle = "#e5e7eb";
  ctx.lineWidth = 1;
  ctx.fillStyle = "#94a3b8";
  ctx.textAlign = "right";

  for (let i = 0; i <= 3; i++) {
    const y = padding.top + (innerHeight / 3) * i;
    const value = Math.round(maxValue - (maxValue / 3) * i);
    ctx.beginPath();
    ctx.moveTo(padding.left, y);
    ctx.lineTo(padding.left + innerWidth, y);
    ctx.stroke();
    ctx.fillText(String(value), padding.left - 8, y + 4);
  }
}

function drawLineSeries(ctx, width, height, padding, data, maxValue, color) {
  const innerWidth = width - padding.left - padding.right;
  const innerHeight = height - padding.top - padding.bottom;
  const step = data.length > 1 ? innerWidth / (data.length - 1) : innerWidth;
  ctx.strokeStyle = color;
  ctx.lineWidth = 3;
  ctx.beginPath();
  data.forEach((value, index) => {
    const x = padding.left + index * step;
    const y = padding.top + innerHeight - (value / maxValue) * innerHeight;
    if (index === 0) ctx.moveTo(x, y);
    else ctx.lineTo(x, y);
  });
  ctx.stroke();

  data.forEach((value, index) => {
    const x = padding.left + index * step;
    const y = padding.top + innerHeight - (value / maxValue) * innerHeight;
    ctx.fillStyle = "#fff";
    ctx.beginPath();
    ctx.arc(x, y, 4, 0, Math.PI * 2);
    ctx.fill();
    ctx.strokeStyle = color;
    ctx.lineWidth = 2;
    ctx.stroke();
  });
}

function drawXAxisLabels(ctx, width, height, padding, labels) {
  const innerWidth = width - padding.left - padding.right;
  const step = labels.length > 1 ? innerWidth / (labels.length - 1) : innerWidth;
  ctx.fillStyle = "#64748b";
  ctx.textAlign = "center";
  labels.forEach((label, index) => {
    ctx.fillText(label, padding.left + index * step, height - 12);
  });
}

function drawLegend(ctx, items, x = 18, y = 18) {
  let offset = 0;
  ctx.textAlign = "left";
  ctx.font = "12px Segoe UI, Arial, sans-serif";
  items.forEach(([label, color]) => {
    ctx.fillStyle = color;
    roundRect(ctx, x, y + offset - 9, 10, 10, 3);
    ctx.fill();
    ctx.fillStyle = "#475569";
    ctx.fillText(label, x + 16, y + offset);
    offset += 22;
  });
}

function totalMovementByDate(date, type) {
  const key = date.toISOString().slice(0, 10);
  return state.movements
    .filter((item) => item.type === type && String(item.createdAt || "").slice(0, 10) === key)
    .reduce((sum, item) => sum + number(item.quantity), 0);
}

function shortLabel(text) {
  const value = String(text || "");
  return value.length > 9 ? `${value.slice(0, 8)}...` : value;
}

function roundRect(ctx, x, y, width, height, radius) {
  const r = Math.min(radius, width / 2, height / 2);
  ctx.beginPath();
  ctx.moveTo(x + r, y);
  ctx.arcTo(x + width, y, x + width, y + height, r);
  ctx.arcTo(x + width, y + height, x, y + height, r);
  ctx.arcTo(x, y + height, x, y, r);
  ctx.arcTo(x, y, x + width, y, r);
  ctx.closePath();
}

function pageSlice(items, page, pageSize) {
  const totalItems = items.length;
  const totalPages = Math.max(1, Math.ceil(totalItems / pageSize));
  const currentPage = Math.min(Math.max(1, page || 1), totalPages);
  const start = (currentPage - 1) * pageSize;
  return {
    totalItems,
    totalPages,
    currentPage,
    rows: items.slice(start, start + pageSize)
  };
}

function renderPagination(containerId, currentPage, totalPages, pageAttr) {
  const container = $(`#${containerId}`);
  if (!container) return;

  if (totalPages <= 1) {
    container.innerHTML = "";
    return;
  }

  const pages = [];
  const startPage = Math.max(1, currentPage - 2);
  const endPage = Math.min(totalPages, startPage + 4);
  for (let page = startPage; page <= endPage; page += 1) {
    pages.push(`<button class="product-page-btn ${page === currentPage ? "active" : ""}" type="button" ${pageAttr}="${page}">${page}</button>`);
  }

  container.innerHTML = `
    <button class="product-page-btn ${currentPage === 1 ? "disabled" : ""}" type="button" ${pageAttr}-nav="prev" ${currentPage === 1 ? "disabled" : ""}>â€¹</button>
    ${pages.join("")}
    <button class="product-page-btn ${currentPage === totalPages ? "disabled" : ""}" type="button" ${pageAttr}-nav="next" ${currentPage === totalPages ? "disabled" : ""}>â€º</button>
  `;
}

function renderProducts() {
  const rows = getVisibleProducts();
  const { rows: pageRows, totalItems, totalPages, currentPage } = pageSlice(rows, state.productPage, 5);
  state.productPage = currentPage;

  pruneSelectedProducts();
  $("#productsTable").innerHTML = pageRows.map((item) => `
    <tr>
      <td>
        <div class="product-cell">
          ${item.image ? `<img class="thumb" src="${escapeHtml(item.image)}" alt="">` : `<div class="thumb placeholder-thumb">${initial(item.name)}</div>`}
          <div>
            <strong>${escapeHtml(item.name || "ChÆ°a Ä‘áº·t tÃªn")}</strong>
            <p class="muted">${escapeHtml(item.description || "")}</p>
          </div>
        </div>
      </td>
      <td>
        ${canManageCatalog() ? `<input type="checkbox" onchange="toggleProductSelection('${item.id}', this.checked)" ${state.selectedProductIds.has(item.id) ? "checked" : ""}>` : ""}
      </td>
      <td>${escapeHtml(productCategoryName(item))}</td>
      <td>${money(item.price)}</td>
      <td><span class="badge ${number(item.stock) <= number(item.minimumStock) ? "warn" : "ok"}">${item.stock}</span></td>
      <td>${statusBadge(item.status)}</td>
      <td class="product-action-cell">
        ${canManageCatalog() ? `
          <div class="actions product-actions">
            <button class="user-icon-button edit" type="button" onclick="editProduct('${item.id}')" title="Sá»­a sáº£n pháº©m" aria-label="Sá»­a sáº£n pháº©m">
              <i class="pe-7s-pen" aria-hidden="true"></i>
            </button>
            <button class="user-icon-button delete" type="button" onclick="deleteProduct('${item.id}')" title="XÃ³a sáº£n pháº©m" aria-label="XÃ³a sáº£n pháº©m">
              <i class="pe-7s-trash" aria-hidden="true"></i>
            </button>
          </div>
        ` : `<span class="muted">Chá»‰ xem</span>`}
      </td>
    </tr>
  `).join("") || `<tr><td colspan="7" class="empty">ChÆ°a cÃ³ sáº£n pháº©m</td></tr>`;

  updateProductSelectionControls(pageRows);
  const info = $("#productPaginationInfo");
  if (info) {
    info.textContent = totalItems
      ? `Äang xem ${((currentPage - 1) * 5) + 1}-${Math.min(totalItems, currentPage * 5)} / ${totalItems} sáº£n pháº©m`
      : "ChÆ°a cÃ³ sáº£n pháº©m";
  }
  renderPagination("productPagination", currentPage, totalPages, "data-product-page");
}

function renderSales() {
  const completedOrders = state.orders.filter(isRevenueOrder);
  const totalRevenue = completedOrders.reduce((sum, order) => sum + number(order.finalAmount), 0);
  const itemsSold = completedOrders.reduce((sum, order) => sum + (order.items || []).reduce((itemSum, item) => itemSum + number(item.quantity), 0), 0);
  const lastOrder = state.orders[0];

  $("#salesOrderCount").textContent = state.orders.length;
  $("#salesRevenue").textContent = money(totalRevenue);
  $("#salesItemsSold").textContent = itemsSold;
  $("#salesLastOrder").textContent = lastOrder ? lastOrder.code : "-";

  applySalesTab(state.salesTab);
  if (state.salesTab === "revenue") {
    renderRevenueAnalytics();
  }
  renderSaleCart();

  $("#ordersList").innerHTML = state.orders.length
    ? state.orders.slice(0, 12).map(renderOrderItem).join("")
    : empty("ChÆ°a cÃ³ Ä‘Æ¡n bÃ¡n hÃ ng");
}

function renderOrderItem(order) {
  const pending = order.status === "pending_cod";
  const webPending = order.status === "web_pending";
  const depositHolding = order.status === "deposit_holding";
  const cancelled = ["expired", "cancelled"].includes(order.status);
  const status = webPending
    ? `<span class="badge warn">ÄÆ¡n web chá» xá»­ lÃ½</span>`
    : pending
    ? `<span class="badge warn">COD Ä‘ang giá»¯</span>`
    : depositHolding
    ? `<span class="badge warn">á»¨ng cá»c Ä‘ang giá»¯</span>`
    : cancelled
      ? `<span class="badge danger">${order.status === "expired" ? "ÄÃ£ háº¿t háº¡n" : "ÄÃ£ há»§y"}</span>`
      : `<span class="badge ok">ÄÃ£ thanh toÃ¡n</span>`;
  const countdown = pending
    ? `<span class="cod-countdown" data-cod-countdown="${order.id}">${formatCountdown(order.expiresAt)}</span>`
    : "";
  const actions = webPending
    ? `<div class="order-actions">
        <button class="btn btn-info btn-fill btn-sm" type="button" onclick="updateWebOrderStatus('${order.id}', 'Confirmed')">XÃ¡c nháº­n Ä‘Æ¡n</button>
        <button class="btn btn-danger btn-sm" type="button" onclick="updateWebOrderStatus('${order.id}', 'Cancelled')">Há»§y Ä‘Æ¡n</button>
      </div>`
    : pending
    ? `<div class="order-actions">
        <button class="btn btn-info btn-fill btn-sm" type="button" onclick="confirmCodOrder('${order.id}')">XÃ¡c nháº­n Ä‘Ã£ tráº£</button>
        <button class="btn btn-danger btn-sm" type="button" onclick="cancelCodOrder('${order.id}')">Há»§y & hoÃ n kho</button>
      </div>`
    : depositHolding
    ? `<div class="order-actions">
        <button class="btn btn-info btn-fill btn-sm" type="button" onclick="confirmDepositRemote('${order.id}')">XÃ¡c nháº­n Ä‘Ã£ tráº£ Ä‘á»§</button>
        <button class="btn btn-danger btn-sm" type="button" onclick="cancelDepositRemote('${order.id}')">Há»§y & hoÃ n cá»c</button>
      </div>`
    : "";

  return `
    <div class="list-item order-item ${pending ? "order-pending" : ""}">
      <div class="order-main">
        <strong>${escapeHtml(`${order.code} - ${order.customerName || "KhÃ¡ch láº»"}`)}</strong>
        <p>${dateTime(order.createdAt)} - ${money(order.finalAmount)} - ${escapeHtml(order.paymentMethod || "-")}</p>
        ${order.source ? `<p><strong>${escapeHtml(order.source)}</strong> Â· ${escapeHtml(order.phone || "ChÆ°a cÃ³ SÄT")} Â· ${escapeHtml(order.email || "ChÆ°a cÃ³ email")}</p>` : ""}
        ${order.address ? `<p>Äá»‹a chá»‰: ${escapeHtml(order.address)}</p>` : ""}
        ${order.items?.length ? `<p>${order.items.map((item) => `${escapeHtml(item.name)} Ã— ${number(item.quantity)}`).join(" Â· ")}</p>` : ""}
        ${pending ? `<p class="cod-hold-status">Kho Ä‘ang giá»¯ hÃ ng Â· cÃ²n ${countdown}</p>` : ""}
        ${cancelled ? `<p>HÃ ng Ä‘Ã£ Ä‘Æ°á»£c tráº£ vá» kho.</p>` : ""}
      </div>
      <div class="order-side">${status}${actions}</div>
    </div>
  `;
}

window.updateWebOrderStatus = async (id, status) => {
  const order = state.orders.find((item) => item.id === id);
  if (status === "Cancelled" && order) {
    for (const item of order.items || []) {
      await request(`${PRODUCT_API}/products/${item.productId}/stock/adjust`, {
        method: "POST",
        body: {
          type: "in",
          quantity: number(item.quantity),
          note: `HoÃ n kho do há»§y Ä‘Æ¡n web ${order.code}`
        }
      });
    }
  }

  await request(`${ORDER_API}/Orders/${id}/status`, {
    method: "PUT",
    body: { status }
  });
  showToast(status === "Confirmed" ? "ÄÃ£ xÃ¡c nháº­n Ä‘Æ¡n hÃ ng web" : "ÄÃ£ há»§y Ä‘Æ¡n hÃ ng web");
  await Promise.all([loadOrderData(), loadInventoryData()]);
  renderSales();
  renderInventory();
  renderStats();
};

async function syncReservations() {
  if (!state.token || reservationSyncRunning) return;
  reservationSyncRunning = true;
  try {
    const reservations = await request(`${PRODUCT_API}/inventory/reservations`);
    const byId = new Map(reservations.map((item) => [item.id, item]));
    let changed = false;

    state.orders.forEach((order) => {
      if (!order.reservationId) return;
      const reservation = byId.get(order.reservationId);
      if (!reservation) return;
      const nextStatus = reservation.status === "pending"
        ? "pending_cod"
        : reservation.status === "confirmed"
          ? "completed"
          : reservation.status;
      if (order.status !== nextStatus || order.expiresAt !== reservation.expiresAt) {
        order.status = nextStatus;
        order.expiresAt = reservation.expiresAt;
        if (nextStatus === "completed") {
          order.paidAmount = order.finalAmount;
          order.debtAmount = 0;
          order.confirmedAt = reservation.confirmedAt;
        }
        changed = true;
      }
    });

    if (changed) {
      saveOrders();
      renderSales();
      await loadInventoryData();
      renderInventory();
      renderStats();
    }
  } finally {
    reservationSyncRunning = false;
    lastReservationSync = Date.now();
  }
}

function startReservationTimer() {
  if (reservationTimer) return;
  reservationTimer = setInterval(() => {
    updateCodCountdowns();
    if (Date.now() - lastReservationSync >= 5000 && state.orders.some((order) => order.status === "pending_cod")) {
      syncReservations().catch((error) => console.error("COD sync failed", error));
    }
  }, 1000);
}

function updateCodCountdowns() {
  $$("[data-cod-countdown]").forEach((element) => {
    const order = state.orders.find((item) => item.id === element.dataset.codCountdown);
    if (order) element.textContent = formatCountdown(order.expiresAt);
  });
}

function formatCountdown(expiresAt) {
  const remaining = Math.max(0, new Date(expiresAt).getTime() - Date.now());
  if (remaining <= 0) return "Ä‘ang hoÃ n kho...";
  const minutes = Math.floor(remaining / 60000);
  const seconds = Math.floor((remaining % 60000) / 1000);
  return `${String(minutes).padStart(2, "0")}:${String(seconds).padStart(2, "0")}`;
}

window.confirmCodOrder = async (orderId) => {
  const order = state.orders.find((item) => item.id === orderId && item.status === "pending_cod");
  if (!order) return;
  if (!confirm(`XÃ¡c nháº­n khÃ¡ch Ä‘Ã£ thanh toÃ¡n Ä‘Æ¡n ${order.code}?`)) return;
  await request(`${PRODUCT_API}/inventory/reservations/${order.reservationId}/confirm`, { method: "POST" });
  order.status = "completed";
  order.paidAmount = order.finalAmount;
  order.debtAmount = 0;
  order.confirmedAt = new Date().toISOString();
  // Äá»“ng bá»™ tráº¡ng thÃ¡i lÃªn Order API (NhÃ³m 2)
  const codeToSync = order.serverCode || order.code;
  try {
    await request(`${ORDER_API}/Orders/by-code/${codeToSync}/status`, {
      method: "PUT",
      body: { status: "Completed" }
    });
  } catch (e) {
    console.warn("KhÃ´ng thá»ƒ Ä‘á»“ng bá»™ tráº¡ng thÃ¡i COD confirm lÃªn Order API:", e?.message || e);
  }
  saveOrders();
  renderSales();
  showToast("ÄÃ£ xÃ¡c nháº­n thanh toÃ¡n COD. HÃ ng chÃ­nh thá»©c xuáº¥t kho.");
};

window.cancelCodOrder = async (orderId) => {
  const order = state.orders.find((item) => item.id === orderId && item.status === "pending_cod");
  if (!order) return;
  if (!confirm(`Há»§y Ä‘Æ¡n ${order.code} vÃ  hoÃ n toÃ n bá»™ hÃ ng vá» kho?`)) return;
  await request(`${PRODUCT_API}/inventory/reservations/${order.reservationId}/cancel`, { method: "POST" });
  order.status = "cancelled";
  order.cancelledAt = new Date().toISOString();
  // Äá»“ng bá»™ tráº¡ng thÃ¡i lÃªn Order API (NhÃ³m 2)
  const codeToSync = order.serverCode || order.code;
  try {
    await request(`${ORDER_API}/Orders/by-code/${codeToSync}/status`, {
      method: "PUT",
      body: { status: "Cancelled" }
    });
  } catch (e) {
    console.warn("KhÃ´ng thá»ƒ Ä‘á»“ng bá»™ tráº¡ng thÃ¡i COD cancel lÃªn Order API:", e?.message || e);
  }
  saveOrders();
  await loadInventoryData();
  renderAll();
  showToast("ÄÃ£ há»§y COD vÃ  hoÃ n hÃ ng vá» kho.");
};

window.confirmDepositRemote = async (orderId) => {
  const order = state.orders.find((item) => item.id === orderId && item.status === "deposit_holding");
  if (!order) return;
  if (!confirm(`XÃ¡c nháº­n khÃ¡ch Ä‘Ã£ thanh toÃ¡n Ä‘á»§ Ä‘Æ¡n á»©ng cá»c ${order.code}?`)) return;
  const codeToSync = order.serverCode || order.code;
  try {
    await request(`${ORDER_API}/Orders/by-code/${codeToSync}/status`, {
      method: "PUT",
      body: { status: "Completed" }
    });
  } catch (e) {
    console.warn("KhÃ´ng thá»ƒ Ä‘á»“ng bá»™ xÃ¡c nháº­n á»©ng cá»c lÃªn Order API:", e?.message || e);
  }
  order.status = "completed";
  order.paidAmount = order.finalAmount;
  order.debtAmount = 0;
  order.confirmedAt = new Date().toISOString();
  saveOrders();
  renderSales();
  showToast("ÄÃ£ xÃ¡c nháº­n thanh toÃ¡n Ä‘á»§ Ä‘Æ¡n á»©ng cá»c. HÃ ng chÃ­nh thá»©c xuáº¥t kho.");
};

window.cancelDepositRemote = async (orderId) => {
  const order = state.orders.find((item) => item.id === orderId && item.status === "deposit_holding");
  if (!order) return;
  if (!confirm(`Há»§y Ä‘Æ¡n á»©ng cá»c ${order.code} vÃ  hoÃ n cá»c cho khÃ¡ch?`)) return;
  const codeToSync = order.serverCode || order.code;
  try {
    await request(`${ORDER_API}/Orders/by-code/${codeToSync}/status`, {
      method: "PUT",
      body: { status: "Cancelled" }
    });
  } catch (e) {
    console.warn("KhÃ´ng thá»ƒ Ä‘á»“ng bá»™ há»§y á»©ng cá»c lÃªn Order API:", e?.message || e);
  }
  order.status = "cancelled";
  order.cancelledAt = new Date().toISOString();
  saveOrders();
  renderSales();
  showToast("ÄÃ£ há»§y Ä‘Æ¡n á»©ng cá»c vÃ  hoÃ n tiá»n cá»c cho khÃ¡ch.");
};

function saveOrders() {
  localStorage.setItem("khopro_orders", JSON.stringify(state.orders));
}

function isRevenueOrder(order) {
  return !order.status || ["completed", "confirmed"].includes(order.status);
}

function renderSaleCart() {
  if (!state.saleCart.length) {
    $("#saleCart").className = "sale-cart empty";
    $("#saleCart").innerHTML = "Don hang chua co san pham";
    return;
  }

  $("#saleCart").className = "sale-cart";
  $("#saleCart").innerHTML = state.saleCart.map((line, index) => `
    <div class="sale-line">
      <div>
        <strong>${escapeHtml(line.name)}</strong>
        <p>${line.quantity} x ${money(line.price)}</p>
      </div>
      <div class="actions">
        <strong>${money(number(line.quantity) * number(line.price))}</strong>
        <button class="danger" type="button" onclick="removeSaleLine(${index})">Xoa</button>
      </div>
    </div>
  `).join("");
}

function addSaleLine() {
  const productId = $("#saleProductSelect").value;
  const product = state.products.find((item) => item.id === productId);
  clampSaleQuantity();
  const quantity = Math.max(1, Math.floor(number($("#saleQuantityInput").value || 1)));

  if (!product) {
    showToast("Hay chon san pham can ban");
    return;
  }

  const available = getSaleAvailableQuantity(productId);
  if (available <= 0) {
    showToast("Sáº£n pháº©m nÃ y Ä‘Ã£ háº¿t tá»“n kho hoáº·c Ä‘Ã£ Ä‘á»§ sá»‘ lÆ°á»£ng trong Ä‘Æ¡n");
    updateSaleQuantityLimit();
    return;
  }

  if (quantity > available) {
    showToast("So luong ban lon hon ton kho hien tai");
    updateSaleQuantityLimit();
    return;
  }

  const existing = state.saleCart.find((item) => item.productId === productId);
  if (existing) {
    const nextQuantity = existing.quantity + quantity;
    if (nextQuantity > number(product.stock)) {
      showToast("Tong so luong trong don vuot ton kho");
      return;
    }
    existing.quantity = nextQuantity;
  } else {
    state.saleCart.push({
      productId,
      name: product.name || "San pham",
      categoryId: product.categoryId || "",
      categoryName: productCategoryName(product),
      quantity,
      price: number(product.price)
    });
  }

  renderSaleCart();
  updateSaleQuantityLimit();
}

async function createSaleOrder(event) {
  event.preventDefault();
  if (!state.saleCart.length) {
    showToast("Don hang chua co san pham");
    return;
  }

  const form = event.currentTarget;
  const data = formData(form);
  const subTotal = state.saleCart.reduce((sum, line) => sum + number(line.price) * number(line.quantity), 0);
  const discountAmount = Math.min(number(data.discountAmount), subTotal);
  const finalAmount = Math.max(0, subTotal - discountAmount);
  const isCod = data.paymentMethod === "COD";
  const paidAmount = isCod ? 0 : data.paidAmount === "" ? finalAmount : number(data.paidAmount);
  const debtAmount = Math.max(0, finalAmount - paidAmount);
  const orderId = crypto.randomUUID();
  const orderCode = `DH${new Date().getTime().toString().slice(-8)}`;
  let reservation = null;

  if (isCod) {
    reservation = await request(`${PRODUCT_API}/inventory/reservations`, {
      method: "POST",
      body: {
        orderId,
        customerName: data.customerName || "KhÃ¡ch láº»",
        lines: state.saleCart.map((line) => ({
          productId: line.productId,
          quantity: line.quantity
        }))
      }
    });
  } else {
    for (const line of state.saleCart) {
      await request(`${PRODUCT_API}/products/${line.productId}/stock/sell`, {
        method: "POST",
        body: {
          quantity: line.quantity,
          note: `BÃ¡n hÃ ng ${orderCode} - ${data.customerName || "KhÃ¡ch láº»"}`
        }
      });
    }
  }

  const order = {
    id: orderId,
    code: orderCode,
    customerName: data.customerName || "KhÃ¡ch láº»",
    paymentType: isCod ? "cod" : "direct",
    paymentMethod: isCod ? "COD" : data.paymentMethod || "Cash",
    items: state.saleCart,
    subTotal,
    discountAmount,
    finalAmount,
    paidAmount,
    debtAmount,
    createdAt: new Date().toISOString(),
    status: isCod ? "pending_cod" : "completed",
    reservationId: reservation?.id || "",
    expiresAt: reservation?.expiresAt || null,
    source: "KhoPro"
  };

  // Äá»“ng bá»™ Ä‘Æ¡n hÃ ng lÃªn Order API (NhÃ³m 2)
  try {
    const serverOrder = await request(`${ORDER_API}/Orders`, {
      method: "POST",
      body: {
        customerId: 0,
        customerName: order.customerName,
        discountAmount: order.discountAmount,
        discountType: "Fixed",
        discountValue: order.discountAmount,
        createdByUserId: 1,
        createdBy: state.user?.username || state.user?.name || "khopro",
        paymentMethod: order.paymentMethod,
        paidAmount: order.paidAmount,
        source: "KhoPro",
        items: state.saleCart.map((line) => ({
          productId: line.productId,
          productCode: line.categoryId || "",
          productName: line.name,
          quantity: line.quantity,
          unitPrice: line.price,
          discountAmount: 0
        }))
      }
    });
    // Cáº­p nháº­t local order vá»›i orderId thá»±c tá»« server
    if (serverOrder && serverOrder.orderId) {
      order.serverId = serverOrder.orderId;
      order.serverCode = serverOrder.orderCode;
    }
  } catch (e) {
    console.warn("KhÃ´ng thá»ƒ Ä‘á»“ng bá»™ Ä‘Æ¡n lÃªn Order API:", e?.message || e);
  }

  state.orders = [order, ...state.orders];
  localStorage.setItem("khopro_orders", JSON.stringify(state.orders));
  state.saleCart = [];
  form.reset();
  $("#salePaymentMethod").value = "Cash";
  $("#saleProductPicker").value = "";
  $("#saleProductSelect").value = "";
  updateSalePaymentType();
  $("#saleQuantityInput").value = 1;
  updateSaleQuantityLimit();
  showToast(isCod
    ? "ÄÃ£ giá»¯ hÃ ng COD trong 10 phÃºt vÃ  trá»« tá»“n kho."
    : "ÄÃ£ táº¡o Ä‘Æ¡n, thanh toÃ¡n vÃ  trá»« kho.");
  await loadInventoryData();
  renderAll();
}

function renderRevenueAnalytics() {
  const selection = getRevenueSelection();
  const selectedOrders = ordersBetween(selection.start, selection.end);
  const totalRevenue = sumOrderRevenue(selectedOrders);
  const itemCount = selectedOrders.reduce((sum, order) => sum + (order.items || [])
    .reduce((itemSum, item) => itemSum + number(item.quantity), 0), 0);
  const averageOrder = selectedOrders.length ? totalRevenue / selectedOrders.length : 0;

  $$("[data-revenue-period]").forEach((button) => {
    button.classList.toggle("active", button.dataset.revenuePeriod === selection.period);
  });
  $("#revenueReferenceDate").value = dateInputValue(selection.reference);
  $("#revenuePeriodTitle").textContent = selection.title;
  $("#revenuePeriodRange").textContent = selection.rangeLabel;
  $("#revenuePrimaryLabel").textContent = selection.totalLabel;
  $("#revenueToday").textContent = money(totalRevenue);
  $("#revenueTodayOrders").textContent = `${selectedOrders.length} Ä‘Æ¡n hÃ ng`;
  $("#revenueWeek").textContent = itemCount.toLocaleString("vi-VN");
  $("#revenueWeekRange").textContent = "Tá»•ng sá»‘ lÆ°á»£ng sáº£n pháº©m";
  $("#revenueMonth").textContent = money(averageOrder);
  $("#revenueMonthLabel").textContent = selectedOrders.length
    ? `TrÃªn ${selectedOrders.length} Ä‘Æ¡n hÃ ng`
    : "ChÆ°a cÃ³ Ä‘Æ¡n hÃ ng";
  $("#revenueChartTitle").textContent = selection.chartTitle;
  $("#revenueChartSubtitle").textContent = selection.rangeLabel;
  $("#revenueTableTitle").textContent = selection.tableTitle;
  $("#revenueTableSubtitle").textContent = `Dá»¯ liá»‡u tá»« ${selection.rangeLabel.toLowerCase()}`;

  renderDailyRevenueTable(selection.rows);
  renderCategoryRevenue(selectedOrders);
  requestAnimationFrame(() => drawDailyRevenueChart($("#dailyRevenueChart"), selection.rows));
}

function getRevenueSelection() {
  const period = ["day", "week", "month"].includes(state.revenuePeriod)
    ? state.revenuePeriod
    : "day";
  const reference = parseDateInput(state.revenueReferenceDate) || startOfDay(new Date());
  let start;
  let end;
  let rows;
  let title;
  let totalLabel;
  let chartTitle;
  let tableTitle;

  if (period === "week") {
    start = startOfWeek(reference);
    end = addDays(start, 7);
    rows = buildRevenueRows(start, end);
    title = "Doanh thu theo tuáº§n";
    totalLabel = "Tá»•ng doanh thu tuáº§n Ä‘Ã£ chá»n";
    chartTitle = "Doanh thu tá»«ng ngÃ y trong tuáº§n";
    tableTitle = "Chi tiáº¿t doanh thu tuáº§n";
  } else if (period === "month") {
    start = new Date(reference.getFullYear(), reference.getMonth(), 1);
    end = new Date(reference.getFullYear(), reference.getMonth() + 1, 1);
    rows = buildRevenueRows(start, end);
    title = "Doanh thu theo thÃ¡ng";
    totalLabel = "Tá»•ng doanh thu thÃ¡ng Ä‘Ã£ chá»n";
    chartTitle = "Doanh thu tá»«ng ngÃ y trong thÃ¡ng";
    tableTitle = "Chi tiáº¿t doanh thu thÃ¡ng";
  } else {
    start = startOfDay(reference);
    end = addDays(start, 1);
    rows = buildRevenueRows(start, end);
    title = "Doanh thu theo ngÃ y";
    totalLabel = "Doanh thu ngÃ y Ä‘Ã£ chá»n";
    chartTitle = "Doanh thu trong ngÃ y Ä‘Ã£ chá»n";
    tableTitle = "Chi tiáº¿t doanh thu ngÃ y";
  }

  return {
    period,
    reference,
    start,
    end,
    rows,
    title,
    totalLabel,
    chartTitle,
    tableTitle,
    rangeLabel: period === "day"
      ? fullDate(start)
      : `${fullDate(start)} - ${fullDate(addDays(end, -1))}`
  };
}

function buildRevenueRows(start, end) {
  const rows = [];
  for (let day = new Date(start); day < end; day = addDays(day, 1)) {
    const nextDay = addDays(day, 1);
    const orders = ordersBetween(day, nextDay);
    rows.push({
      date: new Date(day),
      orders: orders.length,
      items: orders.reduce((sum, order) => sum + (order.items || [])
        .reduce((itemSum, item) => itemSum + number(item.quantity), 0), 0),
      revenue: sumOrderRevenue(orders)
    });
  }
  return rows;
}

function buildDailyRevenue(dayCount, endDate) {
  const lastDay = startOfDay(endDate);
  return Array.from({ length: dayCount }, (_, index) => {
    const day = addDays(lastDay, index - dayCount + 1);
    const nextDay = addDays(day, 1);
    const orders = ordersBetween(day, nextDay);
    return {
      date: day,
      orders: orders.length,
      items: orders.reduce((sum, order) => sum + (order.items || [])
        .reduce((itemSum, item) => itemSum + number(item.quantity), 0), 0),
      revenue: sumOrderRevenue(orders)
    };
  });
}

function renderDailyRevenueTable(rows) {
  $("#dailyRevenueTable").innerHTML = [...rows].reverse().map((row) => `
    <tr>
      <td>${fullDate(row.date)}</td>
      <td>${row.orders}</td>
      <td>${row.items}</td>
      <td><strong>${money(row.revenue)}</strong></td>
    </tr>
  `).join("");
}

function renderCategoryRevenue(orders = state.orders) {
  const categoryTotals = new Map();

  orders.forEach((order) => {
    const lines = order.items || [];
    const grossTotal = lines.reduce((sum, line) => sum + number(line.price) * number(line.quantity), 0);
    const orderRevenue = number(order.finalAmount);

    lines.forEach((line) => {
      const product = state.products.find((item) => item.id === line.productId);
      const category = line.categoryName
        || (line.categoryId ? categoryName(line.categoryId) : "")
        || (product ? productCategoryName(product) : "")
        || "ChÆ°a phÃ¢n loáº¡i";
      const lineGross = number(line.price) * number(line.quantity);
      const allocatedRevenue = grossTotal > 0 ? orderRevenue * lineGross / grossTotal : 0;
      categoryTotals.set(category, (categoryTotals.get(category) || 0) + allocatedRevenue);
    });
  });

  const rows = [...categoryTotals.entries()]
    .map(([name, revenue]) => ({ name, revenue }))
    .sort((a, b) => b.revenue - a.revenue);
  const total = rows.reduce((sum, row) => sum + row.revenue, 0);

  $("#categoryRevenueList").innerHTML = rows.length
    ? rows.map((row, index) => {
      const percent = total > 0 ? row.revenue / total * 100 : 0;
      return `
        <div class="category-revenue-item">
          <div class="category-revenue-head">
            <span><i style="--category-color:${revenueColor(index)}"></i>${escapeHtml(row.name)}</span>
            <strong>${money(row.revenue)}</strong>
          </div>
          <div class="category-revenue-track">
            <span style="width:${percent.toFixed(2)}%;--category-color:${revenueColor(index)}"></span>
          </div>
          <small>${percent.toLocaleString("vi-VN", { maximumFractionDigits: 1 })}% tá»•ng doanh thu</small>
        </div>
      `;
    }).join("")
    : empty("ChÆ°a cÃ³ doanh thu theo danh má»¥c");
}

function drawDailyRevenueChart(canvas, rows) {
  if (!canvas || canvas.offsetWidth === 0) return;
  const ratio = window.devicePixelRatio || 1;
  const width = Math.max(canvas.offsetWidth, 320);
  const height = 260;
  canvas.width = width * ratio;
  canvas.height = height * ratio;
  const ctx = canvas.getContext("2d");
  ctx.scale(ratio, ratio);
  ctx.clearRect(0, 0, width, height);

  const padding = { top: 20, right: 18, bottom: 42, left: 62 };
  const chartWidth = width - padding.left - padding.right;
  const chartHeight = height - padding.top - padding.bottom;
  const maxRevenue = Math.max(...rows.map((row) => row.revenue), 1);
  const roundedMax = Math.ceil(maxRevenue / 100000) * 100000 || 100000;

  ctx.font = "11px sans-serif";
  ctx.strokeStyle = "#e8edf3";
  ctx.fillStyle = "#8a94a6";
  ctx.lineWidth = 1;

  for (let step = 0; step <= 4; step += 1) {
    const y = padding.top + chartHeight * step / 4;
    ctx.beginPath();
    ctx.moveTo(padding.left, y);
    ctx.lineTo(width - padding.right, y);
    ctx.stroke();
    const value = roundedMax * (4 - step) / 4;
    ctx.textAlign = "right";
    ctx.fillText(compactMoney(value), padding.left - 9, y + 4);
  }

  const gap = rows.length > 20 ? 4 : 7;
  const barWidth = Math.min(72, Math.max(8, (chartWidth - gap * (rows.length - 1)) / rows.length));
  const barsWidth = barWidth * rows.length + gap * Math.max(0, rows.length - 1);
  const barsOffset = Math.max(0, (chartWidth - barsWidth) / 2);
  const labelStep = rows.length > 20 ? 3 : rows.length > 10 ? 2 : 1;
  rows.forEach((row, index) => {
    const x = padding.left + barsOffset + index * (barWidth + gap);
    const barHeight = row.revenue / roundedMax * chartHeight;
    const y = padding.top + chartHeight - barHeight;
    const gradient = ctx.createLinearGradient(0, y, 0, padding.top + chartHeight);
    gradient.addColorStop(0, "#1dc7ea");
    gradient.addColorStop(1, "#3478e5");
    ctx.fillStyle = gradient;
    ctx.fillRect(x, y, barWidth, barHeight);

    if (index % labelStep === 0 || index === rows.length - 1) {
      ctx.fillStyle = "#8a94a6";
      ctx.textAlign = "center";
      ctx.fillText(shortDate(row.date), x + barWidth / 2, height - 16);
    }
  });
}

function ordersBetween(start, end) {
  return state.orders.filter((order) => {
    const createdAt = new Date(order.createdAt);
    return isRevenueOrder(order)
      && !Number.isNaN(createdAt.getTime())
      && createdAt >= start
      && createdAt < end;
  });
}

function sumOrderRevenue(orders) {
  return orders.reduce((sum, order) => sum + number(order.finalAmount), 0);
}

function startOfDay(value) {
  const date = new Date(value);
  return new Date(date.getFullYear(), date.getMonth(), date.getDate());
}

function startOfWeek(value) {
  const date = startOfDay(value);
  const day = date.getDay() || 7;
  return addDays(date, 1 - day);
}

function parseDateInput(value) {
  if (!value) return null;
  const [year, month, day] = value.split("-").map(Number);
  if (!year || !month || !day) return null;
  const date = new Date(year, month - 1, day);
  return Number.isNaN(date.getTime()) ? null : date;
}

function dateInputValue(value) {
  const date = new Date(value);
  const year = date.getFullYear();
  const month = String(date.getMonth() + 1).padStart(2, "0");
  const day = String(date.getDate()).padStart(2, "0");
  return `${year}-${month}-${day}`;
}

function addDays(value, amount) {
  const date = new Date(value);
  date.setDate(date.getDate() + amount);
  return date;
}

function shortDate(value) {
  return new Date(value).toLocaleDateString("vi-VN", { day: "2-digit", month: "2-digit" });
}

function fullDate(value) {
  return new Date(value).toLocaleDateString("vi-VN", {
    weekday: "short",
    day: "2-digit",
    month: "2-digit",
    year: "numeric"
  });
}

function compactMoney(value) {
  if (value >= 1000000000) return `${(value / 1000000000).toLocaleString("vi-VN", { maximumFractionDigits: 1 })} tá»·`;
  if (value >= 1000000) return `${(value / 1000000).toLocaleString("vi-VN", { maximumFractionDigits: 1 })} tr`;
  if (value >= 1000) return `${Math.round(value / 1000)}k`;
  return `${Math.round(value)}`;
}

function revenueColor(index) {
  return ["#1dc7ea", "#87cb16", "#ff9500", "#ff4a55", "#9368e9", "#23ccef"][index % 6];
}

function setInventoryTab(tab) {
  const nextTab = ["adjustments", "receipts", "exports"].includes(tab) ? tab : "adjustments";
  state.inventoryTab = nextTab;
  localStorage.setItem("khopro_inventory_tab", nextTab);

  $$("[data-inventory-tab]").forEach((button) => {
    button.classList.toggle("active", button.dataset.inventoryTab === nextTab);
  });
  $$("[data-inventory-panel]").forEach((panel) => {
    panel.classList.toggle("hidden", panel.dataset.inventoryPanel !== nextTab);
  });
  renderInventory();
}

function inventoryItemsForTab(tab) {
  if (tab === "receipts") return state.receipts || [];
  if (tab === "exports") {
    return state.movements.filter((item) => String(item.type || "").toLowerCase() === "out");
  }
  return state.movements.filter((item) => ["in", "out", "set"].includes(String(item.type || "").toLowerCase()));
}

function inventoryStatusLabel(status) {
  const value = String(status || "pending").toLowerCase();
  const map = {
    pending: ["Chờ duyệt", "warn"],
    approved: ["Đã duyệt", "ok"],
    cancelled: ["Đã hủy", "danger"]
  };
  return map[value] || [status || "Đã duyệt", "ok"];
}

function renderInventoryTable(kind, items) {
  const tableId = kind === "adjustments" ? "adjustmentsHistoryTable" : kind === "receipts" ? "receiptsHistoryTable" : "exportsHistoryTable";
  const rows = items.map((item) => {
    const statusValue = String(item.status || "pending").toLowerCase();
    const [statusText, statusClass] = inventoryStatusLabel(statusValue);
    const isCreator = String(item.createdById || "") === String(state.user?.id || "");
    const isOwner = String(item.ownerId || "") === String(state.user?.id || "");
    const canEdit = statusValue === "pending" && (canReviewInventory() || isCreator || isOwner);
    const canApprove = statusValue === "pending" && canReviewInventory();
    const canCancel = statusValue === "pending" && canReviewInventory();
    const createdByEmail = item.createdByEmail || item.createdByName || item.createdBy || item.userName
      || (item.createdById === state.user?.id ? state.user?.email : "")
      || (item.createdById === "system" ? "Hệ thống" : "Không lưu - dữ liệu cũ");
    const createdByRole = item.createdByRole ? roleText(item.createdByRole) : "";
    const reviewedByEmail = item.reviewedByEmail || item.approvedByName || item.approvedBy
      || (statusValue === "approved" || statusValue === "cancelled" ? "Không lưu - dữ liệu cũ" : "");
    const reviewedByRole = item.reviewedByRole ? roleText(item.reviewedByRole) : "";
    const approvedAt = item.reviewedAt || item.approvedAt || item.confirmedAt || "-";
    const quantity = number(item.quantity || item.totalQuantity || item.lines?.reduce((sum, line) => sum + number(line.quantity), 0));
    const productName = item.productName || item.product || item.lines?.map((line) => {
      const product = state.products.find((entry) => String(entry.id) === String(line.productId));
      return line.productName || line.name || product?.name || line.productId;
    }).filter(Boolean).join(", ") || "-";
    const supplierName = state.suppliers.find((supplier) => String(supplier.id) === String(item.supplierId))?.name || "-";
    const typeText = kind === "receipts"
      ? "Nhập"
      : kind === "exports"
        ? "Xuất"
        : movementLabel(item.type);
    const personCell = (email, role) => email
      ? `<span class="inventory-person-email">${escapeHtml(email)}</span>${role ? `<span class="inventory-person-role">${escapeHtml(role)}</span>` : ""}`
      : `<span class="inventory-person-empty">-</span>`;

    const detailCells = kind === "receipts"
      ? `
        <td>${escapeHtml(supplierName)}</td>
        <td>${escapeHtml(productName)}</td>
      `
      : `
        <td>${escapeHtml(productName)}</td>
        <td><span class="badge ${kind === "exports" ? "danger" : statusClass}">${escapeHtml(typeText)}</span></td>
      `;

    return `
      <tr>
        <td>${dateTime(item.createdAt)}</td>
        <td class="inventory-person">${personCell(createdByEmail, createdByRole)}</td>
        <td>${approvedAt === "-" ? "-" : dateTime(approvedAt)}</td>
        <td class="inventory-person">${personCell(reviewedByEmail, reviewedByRole)}</td>
        ${detailCells}
        <td>${quantity}</td>
        <td><span class="badge ${statusClass}">${escapeHtml(statusText)}</span></td>
        <td>${escapeHtml(item.note || item.reviewNote || "")}</td>
        <td>
          <div class="actions inventory-actions">
            ${canEdit ? `<button class="user-icon-button edit" type="button" onclick="editInventoryItem('${kind}', '${item.id}')" title="Sửa lệnh" aria-label="Sửa lệnh"><i class="pe-7s-pen" aria-hidden="true"></i></button>` : ""}
            ${canApprove ? `<button class="user-icon-button lock" type="button" onclick="approveInventoryItem('${kind}', '${item.id}')" title="Duyệt lệnh" aria-label="Duyệt lệnh"><i class="pe-7s-check" aria-hidden="true"></i></button>` : ""}
            ${canCancel ? `<button class="user-icon-button delete" type="button" onclick="cancelInventoryItem('${kind}', '${item.id}')" title="Hủy lệnh" aria-label="Hủy lệnh"><i class="pe-7s-close" aria-hidden="true"></i></button>` : ""}
          </div>
        </td>
      </tr>
    `;
  }).join("");

  const table = $(`#${tableId}`);
  if (table) {
    table.innerHTML = rows || `<tr><td colspan="10" class="empty">Không có dữ liệu</td></tr>`;
  }
}

function renderInventory() {
  const activeTab = ["adjustments", "receipts", "exports"].includes(state.inventoryTab) ? state.inventoryTab : "adjustments";

  const referenceDate = state.inventoryHistoryReferenceDate || dateInputValue(new Date());
  $$("[data-inventory-history-date]").forEach((input) => {
    input.value = referenceDate;
  });
  $$("[data-inventory-history-period]").forEach((button) => {
    button.classList.toggle("active", button.dataset.inventoryHistoryPeriod === state.inventoryHistoryPeriod);
  });
  $$("[data-inventory-tab]").forEach((button) => {
    button.classList.toggle("active", button.dataset.inventoryTab === activeTab);
  });
  $$("[data-inventory-panel]").forEach((panel) => {
    panel.classList.toggle("hidden", panel.dataset.inventoryPanel !== activeTab);
  });

  renderInventoryTable("adjustments", inventoryItemsForTab("adjustments"));
  renderInventoryTable("receipts", inventoryItemsForTab("receipts"));
  renderInventoryTable("exports", inventoryItemsForTab("exports"));
}

function renderCategories() {
  $("#addCategoryBtn")?.classList.toggle("hidden", !canManageCatalog());
  $("#categoriesList").innerHTML = state.categories.map((item) => {
    const count = state.products.filter((product) => product.categoryId === item.id).length;
    return `
      <div class="list-item category-item">
        <button class="category-link" type="button" onclick="showProductsByCategory('${item.id}')">
          <strong>${escapeHtml(item.name || "Danh má»¥c")}</strong>
          <p>${escapeHtml(`${count} sáº£n pháº©m${item.parentId ? ` - Danh má»¥c cha: ${categoryName(item.parentId)}` : ""}`)}</p>
      </button>
      ${canManageCatalog() ? `
        <div class="actions category-actions">
          <button class="user-icon-button catalog-icon-button edit" type="button" onclick="editCategory('${item.id}')" title="Sá»­a danh má»¥c" aria-label="Sá»­a danh má»¥c">
            <i class="pe-7s-pen" aria-hidden="true"></i>
          </button>
          <button class="user-icon-button catalog-icon-button delete" type="button" onclick="deleteCategory('${item.id}')" title="XÃ³a danh má»¥c" aria-label="XÃ³a danh má»¥c">
            <i class="pe-7s-trash" aria-hidden="true"></i>
          </button>
        </div>
      ` : ""}
      </div>
    `;
  }).join("") || empty("ChÆ°a cÃ³ danh má»¥c");
}

function renderSuppliers() {
  $("#suppliersList").innerHTML = state.suppliers.map((item) =>
    listItem(item.name || "NhÃ  cung cáº¥p", `${item.phone || "ChÆ°a cÃ³ SÄT"} ${item.email ? "- " + item.email : ""}`, `
      ${canManageCatalog() ? `
        <div class="actions supplier-actions">
          <button class="user-icon-button catalog-icon-button edit" type="button" onclick="editSupplier('${item.id}')" title="Sá»­a nhÃ  cung cáº¥p" aria-label="Sá»­a nhÃ  cung cáº¥p">
            <i class="pe-7s-pen" aria-hidden="true"></i>
          </button>
          <button class="user-icon-button catalog-icon-button delete" type="button" onclick="deleteSupplier('${item.id}')" title="XÃ³a nhÃ  cung cáº¥p" aria-label="XÃ³a nhÃ  cung cáº¥p">
            <i class="pe-7s-trash" aria-hidden="true"></i>
          </button>
        </div>
      ` : ""}
    `)
  ).join("") || empty("ChÆ°a cÃ³ nhÃ  cung cáº¥p");
}

function renderUsers() {
  pruneSelectedUsers();
  const filtered = getVisibleUsers();
  const { rows, totalItems, totalPages, currentPage } = pageSlice(filtered, state.userPage, 5);
  state.userPage = currentPage;
  $("#usersTable").innerHTML = rows.map((item) => {
    const isSelf = item.id === state.user?.id;
    return `
      <tr>
        <td>
          <input type="checkbox" onchange="toggleUserSelection('${item.id}', this.checked)" ${state.selectedUserIds.has(item.id) ? "checked" : ""} ${isSelf ? "disabled" : ""}>
        </td>
        <td>${escapeHtml(item.email)}</td>
        <td>${escapeHtml(item.name || "-")}</td>
        <td class="user-role-cell">
          <select class="table-select user-role-select" aria-label="PhÃ¢n quyá»n cho ${escapeHtml(item.email)}" onchange="updateUserRole('${item.id}', this.value)" ${isSelf ? "disabled" : ""}>
            <option value="user" ${item.role === "user" ? "selected" : ""}>NgÆ°á»i dÃ¹ng</option>
            <option value="Sales" ${item.role === "Sales" ? "selected" : ""}>BÃ¡n hÃ ng</option>
            <option value="Warehouse" ${item.role === "Warehouse" ? "selected" : ""}>Thá»§ kho</option>
            <option value="admin-user" ${item.role === "admin-user" ? "selected" : ""}>Quáº£n trá»‹ há»‡ thá»‘ng</option>
          </select>
        </td>
        <td>${item.isActive ? `<span class="badge ok">Hoáº¡t Ä‘á»™ng</span>` : `<span class="badge danger">KhÃ³a</span>`}</td>
        <td>${item.isOnline ? `<span class="badge ok">Online</span>` : `<span class="badge warn">Offline</span>`}</td>
        <td class="user-action-cell">
          <div class="actions user-actions">
            <button class="user-icon-button edit" type="button" onclick="editUser('${item.id}')" title="Sá»­a thÃ´ng tin" aria-label="Sá»­a thÃ´ng tin">
              <i class="pe-7s-pen" aria-hidden="true"></i>
            </button>
            <button class="user-icon-button ${item.isActive ? "lock" : "unlock"}" type="button" onclick="toggleUserActive('${item.id}', ${item.isActive})" title="${item.isActive ? "KhÃ³a tÃ i khoáº£n" : "Má»Ÿ khÃ³a tÃ i khoáº£n"}" aria-label="${item.isActive ? "KhÃ³a tÃ i khoáº£n" : "Má»Ÿ khÃ³a tÃ i khoáº£n"}" ${isSelf ? "disabled" : ""}>
              <i class="${item.isActive ? "pe-7s-lock" : "pe-7s-unlock"}" aria-hidden="true"></i>
            </button>
            <button class="user-icon-button delete" type="button" onclick="deleteUser('${item.id}')" title="XÃ³a tÃ i khoáº£n" aria-label="XÃ³a tÃ i khoáº£n" ${isSelf ? "disabled" : ""}>
              <i class="pe-7s-trash" aria-hidden="true"></i>
            </button>
          </div>
        </td>
      </tr>
    `;
  }).join("") || `<tr><td colspan="7" class="empty">${state.users.length ? "KhÃ´ng tÃ¬m tháº¥y tÃ i khoáº£n phÃ¹ há»£p" : "KhÃ´ng táº£i Ä‘Æ°á»£c danh sÃ¡ch ngÆ°á»i dÃ¹ng"}</td></tr>`;

  updateUserSelectionControls();
  const info = $("#userPaginationInfo");
  if (info) {
    info.textContent = totalItems
      ? `Äang xem ${((currentPage - 1) * 5) + 1}-${Math.min(totalItems, currentPage * 5)} / ${totalItems} tÃ i khoáº£n`
      : "ChÆ°a cÃ³ tÃ i khoáº£n";
  }
  renderPagination("userPagination", currentPage, totalPages, "data-user-page");
}

function renderProfile() {
  if (!state.user) return;
  const form = $("#profileForm");
  ["name", "storeName", "province", "phone", "birthday", "gender"].forEach((field) => {
    form.elements[field].value = state.user[field] || "";
  });
}

function fillSelects() {
  const categoryOptions = `<option value="">KhÃ´ng cÃ³</option>` + state.categories.map((item) => `<option value="${item.id}">${escapeHtml(item.name)}</option>`).join("");
  const inventoryCategoryOptions = `<option value="">Táº¥t cáº£ danh má»¥c</option>` + state.categories.map((item) => `<option value="${item.id}">${escapeHtml(item.name)}</option>`).join("");
  const categorySuggestionOptions = state.categories.map((item) => `<option value="${escapeHtml(item.name)}"></option>`).join("");
  const supplierOptions = `<option value="">KhÃ´ng chá»n</option>` + state.suppliers.map((item) => `<option value="${item.id}">${escapeHtml(item.name)}</option>`).join("");

  $("#productCategoryFilter").innerHTML = `<option value="">Táº¥t cáº£ danh má»¥c</option>` + state.categories.map((item) => `<option value="${item.id}">${escapeHtml(item.name)}</option>`).join("");
  $("#productCategorySuggestions").innerHTML = categorySuggestionOptions;
  $("#categoryForm select[name='parentId']").innerHTML = categoryOptions;
  $("#receiptForm select[name='supplierId']").innerHTML = supplierOptions;
  $$(".inventory-form").forEach((form) => {
    const categorySelect = form.querySelector("[data-inventory-category]");
    const selectedCategory = categorySelect.value;
    categorySelect.innerHTML = inventoryCategoryOptions;
    categorySelect.value = selectedCategory;
    renderInventoryProductSelect(form);
  });
  renderSaleProductOptions();
  updateSalePaymentType();
}

function renderInventoryProductSelect(form) {
  const productSelect = form.querySelector("select[name='productId']");
  const categoryId = form.querySelector("[data-inventory-category]")?.value || "";
  const query = (form.querySelector("[data-inventory-search]")?.value || "").trim().toLowerCase();
  const selectedId = productSelect.value;
  const products = state.products.filter((item) => {
    const matchesCategory = !categoryId || item.categoryId === categoryId;
    const matchesSearch = !query || `${item.name || ""}`.toLowerCase().includes(query);
    return matchesCategory && matchesSearch;
  });

  productSelect.innerHTML = products.length
    ? products.map((item) => `<option value="${item.id}">${escapeHtml(item.name || "Sáº£n pháº©m")} Â· tá»“n ${number(item.stock)}</option>`).join("")
    : `<option value="">KhÃ´ng tÃ¬m tháº¥y sáº£n pháº©m</option>`;

  if (products.some((item) => item.id === selectedId)) {
    productSelect.value = selectedId;
  }
}

function renderSaleProductOptions() {
  const options = $("#saleProductOptions");
  if (!options) return;
  options.innerHTML = state.products
    .filter((item) => number(item.stock) > 0)
    .map((item) => `<option value="${escapeHtml(saleProductLabel(item))}"></option>`)
    .join("");
  syncSaleProductPicker();
  updateSaleQuantityLimit();
}

function saleProductLabel(product) {
  return `${product.name || "Sáº£n pháº©m"} Â· ${productCategoryName(product)} Â· tá»“n ${product.stock}`;
}

function syncSaleProductPicker() {
  const picker = $("#saleProductPicker");
  const hidden = $("#saleProductSelect");
  if (!picker || !hidden) return;

  const value = picker.value.trim();
  const product = state.products.find((item) => saleProductLabel(item) === value);
  hidden.value = product?.id || "";
  updateSaleQuantityLimit();
}

function inventoryFormForKind(kind) {
  return ({
    adjustments: $("#movementForm"),
    receipts: $("#receiptForm"),
    exports: $("#exportForm")
  })[kind] || null;
}

function inventoryCancelButton(form) {
  return form?.querySelector("[data-inventory-cancel-edit]") || null;
}

function resetInventoryEdit(kind) {
  const form = inventoryFormForKind(kind);
  if (!form) return;
  form.reset();
  if (form.elements.requestId) form.elements.requestId.value = "";
  inventoryCancelButton(form)?.classList.add("hidden");
}

function fillInventoryForm(kind, item) {
  const form = inventoryFormForKind(kind);
  if (!form || !item) return;
  const firstLine = item.lines?.[0] || null;
  if (form.elements.requestId) form.elements.requestId.value = item.id || "";
  if (form.elements.productId) form.elements.productId.value = item.productId || firstLine?.productId || "";
  if (form.elements.quantity) form.elements.quantity.value = number(item.quantity ?? firstLine?.quantity) || "";
  if (form.elements.note) form.elements.note.value = item.note || firstLine?.note || item.reviewNote || "";
  if (form.elements.supplierId) form.elements.supplierId.value = item.supplierId || "";
  if (form.elements.receiver) form.elements.receiver.value = item.receiver || "";
  inventoryCancelButton(form)?.classList.remove("hidden");
}

function inventoryEndpoint(kind, id, action) {
  const base = kind === "receipts" ? `${PRODUCT_API}/inventory/receipts` : `${PRODUCT_API}/inventory/movements`;
  return `${base}/${id}${action ? `/${action}` : ""}`;
}

window.editInventoryItem = (kind, id) => {
  const source = kind === "receipts" ? state.receipts : state.movements;
  const item = source.find((entry) => String(entry.id) === String(id));
  if (!item) {
    showToast("Không tìm thấy lệnh để sửa.");
    return;
  }

  setInventoryTab(kind);
  fillInventoryForm(kind, item);
};

window.approveInventoryItem = async (kind, id) => {
  if (!confirm("Xác nhận duyệt lệnh này?")) return;
  await request(inventoryEndpoint(kind, id, "approve"), { method: "POST" });
  showToast("Đã duyệt lệnh thành công");
  await loadInventoryData();
  renderInventory();
};

window.cancelInventoryItem = async (kind, id) => {
  if (!confirm("Xác nhận hủy lệnh này?")) return;
  await request(inventoryEndpoint(kind, id, "cancel"), { method: "POST" });
  showToast("Đã hủy lệnh thành công");
  await loadInventoryData();
  renderInventory();
};

function updateSalePaymentType() {
  const isCod = $("#salePaymentMethod")?.value === "COD";
  const form = $("#salesForm");
  const paidInput = form?.elements.paidAmount;
  $("#codHoldNote")?.classList.toggle("hidden", !isCod);
  $("#paidAmountLabel")?.classList.toggle("is-disabled", isCod);
  if (paidInput) {
    paidInput.disabled = isCod;
    paidInput.value = isCod ? "0" : paidInput.value;
  }
  if ($("#createSaleOrderBtn")) {
    $("#createSaleOrderBtn").textContent = isCod
      ? "Táº¡o COD & giá»¯ hÃ ng 10 phÃºt"
      : "Táº¡o Ä‘Æ¡n vÃ  trá»« kho";
  }
}

function openProductModal(product = null) {
  const form = $("#productForm");
  form.reset();
  fillSelects();
  $("#productModalTitle").textContent = product ? "Sá»­a sáº£n pháº©m" : "ThÃªm sáº£n pháº©m";
  renderProductImagePreview(product?.image || "");

  if (product) {
    Object.keys(product).forEach((key) => {
      if (form.elements[key]) form.elements[key].value = product[key] || "";
    });
    const currentCategoryName = categoryName(product.categoryId);
    form.elements.categoryName.value = currentCategoryName === "-" ? "" : currentCategoryName;
    renderProductImagePreview(product.image || "");
  }

  $("#productModal").showModal();
}

function openSupplierModal(supplier = null) {
  const form = $("#supplierForm");
  form.reset();
  $("#supplierModalTitle").textContent = supplier ? "Sá»­a nhÃ  cung cáº¥p" : "ThÃªm nhÃ  cung cáº¥p";

  if (supplier) {
    Object.keys(supplier).forEach((key) => {
      if (form.elements[key]) form.elements[key].value = supplier[key] || "";
    });
  }

  $("#supplierModal").showModal();
}

function openUserModal(user) {
  const form = $("#userForm");
  const isSelf = user.id === state.user?.id;
  form.reset();
  $("#userModalTitle").textContent = `Chá»‰nh sá»­a: ${user.email}`;

  ["id", "email", "name", "storeName", "province", "phone", "birthday", "gender", "role"].forEach((key) => {
    if (form.elements[key]) form.elements[key].value = user[key] || "";
  });
  form.elements.isActive.value = String(Boolean(user.isActive));
  form.elements.role.disabled = isSelf;
  form.elements.isActive.disabled = isSelf;

  $("#userModal").showModal();
}

window.editProduct = (id) => {
  const product = state.products.find((item) => item.id === id);
  if (!product) {
    showToast("KhÃ´ng tÃ¬m tháº¥y sáº£n pháº©m Ä‘á»ƒ sá»­a. HÃ£y báº¥m LÃ m má»›i rá»“i thá»­ láº¡i.");
    return;
  }

  openProductModal(product);
};

window.deleteProduct = async (id) => {
  if (!confirm("XÃ³a sáº£n pháº©m nÃ y?")) return;
  await request(`${PRODUCT_API}/products/${id}`, { method: "DELETE" });
  state.selectedProductIds.delete(id);
  showToast("ÄÃ£ xÃ³a sáº£n pháº©m");
  await loadInventoryData();
  renderAll();
};

window.toggleProductSelection = (id, checked) => {
  if (checked) state.selectedProductIds.add(id);
  else state.selectedProductIds.delete(id);
  updateProductSelectionControls();
};

async function deleteSelectedProducts() {
  const ids = [...state.selectedProductIds].filter((id) => state.products.some((item) => item.id === id));
  if (!ids.length) {
    showToast("ChÆ°a chá»n sáº£n pháº©m Ä‘á»ƒ xÃ³a.");
    return;
  }

  if (!confirm(`XÃ³a ${ids.length} sáº£n pháº©m Ä‘Ã£ chá»n?`)) return;
  for (const id of ids) {
    await request(`${PRODUCT_API}/products/${id}`, { method: "DELETE" });
  }

  state.selectedProductIds.clear();
  showToast(`ÄÃ£ xÃ³a ${ids.length} sáº£n pháº©m`);
  await loadInventoryData();
  renderAll();
}

window.editSupplier = (id) => {
  const supplier = state.suppliers.find((item) => item.id === id);
  if (supplier) openSupplierModal(supplier);
};

window.deleteSupplier = async (id) => {
  if (!confirm("XÃ³a nhÃ  cung cáº¥p nÃ y?")) return;
  await request(`${PRODUCT_API}/suppliers/${id}`, { method: "DELETE" });
  showToast("ÄÃ£ xÃ³a nhÃ  cung cáº¥p");
  await loadInventoryData();
  renderAll();
};

window.deleteCategory = async (id) => {
  if (!confirm("XÃ³a danh má»¥c nÃ y?")) return;
  await request(`${PRODUCT_API}/categories/${id}`, { method: "DELETE" });
  showToast("ÄÃ£ xÃ³a danh má»¥c");
  await loadInventoryData();
  renderAll();
};

function openCategoryCreate() {
  const form = $("#categoryForm");
  resetCategoryForm();
  form.classList.remove("hidden");
  form.elements.name.focus();
}

window.editCategory = (id) => {
  const category = state.categories.find((item) => item.id === id);
  if (!category) {
    showToast("KhÃ´ng tÃ¬m tháº¥y danh má»¥c. HÃ£y báº¥m LÃ m má»›i rá»“i thá»­ láº¡i.");
    return;
  }

  const form = $("#categoryForm");
  fillSelects();
  form.classList.remove("hidden");
  form.elements.id.value = category.id;
  form.elements.name.value = category.name || "";
  form.elements.parentId.value = category.parentId || "";
  $("#cancelCategoryEditBtn").classList.remove("hidden");
  form.elements.name.focus();
};

function resetCategoryForm() {
  const form = $("#categoryForm");
  form.reset();
  if (form.elements.id) form.elements.id.value = "";
  form.classList.add("hidden");
  $("#cancelCategoryEditBtn")?.classList.add("hidden");
  fillSelects();
}

window.showProductsByCategory = (id) => {
  setPage("products");
  $("#productCategoryFilter").value = id;
  renderProducts();
};

window.removeSaleLine = (index) => {
  state.saleCart.splice(index, 1);
  renderSaleCart();
  updateSaleQuantityLimit();
};

window.editUser = (id) => {
  const user = state.users.find((item) => item.id === id);
  if (!user) {
    showToast("KhÃ´ng tÃ¬m tháº¥y tÃ i khoáº£n. HÃ£y báº¥m LÃ m má»›i rá»“i thá»­ láº¡i.");
    return;
  }

  openUserModal(user);
};

window.deleteUser = async (id) => {
  if (id === state.user?.id) {
    showToast("KhÃ´ng thá»ƒ xÃ³a chÃ­nh tÃ i khoáº£n Ä‘ang Ä‘Äƒng nháº­p.");
    return;
  }

  const user = state.users.find((item) => item.id === id);
  const label = user?.email || "tÃ i khoáº£n nÃ y";
  if (!confirm(`XÃ³a tÃ i khoáº£n ${label}? NgÆ°á»i dÃ¹ng sáº½ khÃ´ng Ä‘Äƒng nháº­p Ä‘Æ°á»£c ná»¯a.`)) return;

  await request(`${USER_API}/users/${id}`, { method: "DELETE" });
  state.selectedUserIds.delete(id);
  showToast("ÄÃ£ xÃ³a tÃ i khoáº£n");
  await loadUsersData();
  renderUsers();
};

window.toggleUserSelection = (id, checked) => {
  if (id === state.user?.id) return;
  if (checked) state.selectedUserIds.add(id);
  else state.selectedUserIds.delete(id);
  updateUserSelectionControls();
};

async function deleteSelectedUsers() {
  const ids = [...state.selectedUserIds].filter((id) => id !== state.user?.id && state.users.some((item) => item.id === id));
  if (!ids.length) {
    showToast("ChÆ°a chá»n tÃ i khoáº£n Ä‘á»ƒ xÃ³a.");
    return;
  }

  if (!confirm(`XÃ³a ${ids.length} tÃ i khoáº£n Ä‘Ã£ chá»n?`)) return;
  for (const id of ids) {
    await request(`${USER_API}/users/${id}`, { method: "DELETE" });
  }

  state.selectedUserIds.clear();
  showToast(`ÄÃ£ xÃ³a ${ids.length} tÃ i khoáº£n`);
  await loadUsersData();
  renderUsers();
}

window.updateUserRole = async (id, role) => {
  await request(`${USER_API}/users/${id}/role`, {
    method: "PUT",
    body: { role }
  });
  showToast("ÄÃ£ cáº­p nháº­t quyá»n ngÆ°á»i dÃ¹ng");
  await loadUsersData();
  renderUsers();
};

window.toggleUserActive = async (id, isActive) => {
  await request(`${USER_API}/users/${id}`, {
    method: "PUT",
    body: { isActive: !isActive }
  });
  showToast(isActive ? "ÄÃ£ khÃ³a tÃ i khoáº£n" : "ÄÃ£ má»Ÿ tÃ i khoáº£n");
  await loadUsersData();
  renderUsers();
};

function toggleAllProducts(event) {
  if (!canManageCatalog()) return;
  const rows = pageSlice(getVisibleProducts(), state.productPage, 5).rows;
  rows.forEach((item) => {
    if (event.currentTarget.checked) state.selectedProductIds.add(item.id);
    else state.selectedProductIds.delete(item.id);
  });
  renderProducts();
}

function toggleAllUsers(event) {
  const ids = pageSlice(getVisibleUsers(), state.userPage, 5).rows.filter((item) => item.id !== state.user?.id).map((item) => item.id);
  ids.forEach((id) => {
    if (event.currentTarget.checked) state.selectedUserIds.add(id);
    else state.selectedUserIds.delete(id);
  });
  renderUsers();
}

function getVisibleUsers() {
  const query = ($("#userSearch")?.value || "").trim().toLowerCase();
  if (!query) return state.users;

  return state.users.filter((item) => {
    const role = {
      "admin-user": "quáº£n trá»‹ há»‡ thá»‘ng admin",
      Admin: "quáº£n trá»‹ há»‡ thá»‘ng admin",
      Sales: "nhÃ¢n viÃªn bÃ¡n hÃ ng sales",
      Warehouse: "thá»§ kho warehouse",
      user: "ngÆ°á»i dÃ¹ng"
    }[item.role] || item.role || "";
    const status = `${item.isActive ? "hoáº¡t Ä‘á»™ng" : "khÃ³a"} ${item.isOnline ? "online" : "offline"}`;
    return `${item.email || ""} ${item.name || ""} ${role} ${status}`.toLowerCase().includes(query);
  });
}

function getVisibleProducts() {
  const query = $("#productSearch").value.trim().toLowerCase();
  const category = $("#productCategoryFilter").value;
  return state.products.filter((item) => {
    const matchText = `${item.name || ""}`.toLowerCase().includes(query);
    const matchCategory = !category || item.categoryId === category;
    return matchText && matchCategory;
  });
}

function pruneSelectedProducts() {
  const ids = new Set(state.products.map((item) => item.id));
  state.selectedProductIds = new Set([...state.selectedProductIds].filter((id) => ids.has(id)));
}

function pruneSelectedUsers() {
  const ids = new Set(state.users.map((item) => item.id));
  state.selectedUserIds = new Set([...state.selectedUserIds].filter((id) => ids.has(id) && id !== state.user?.id));
}

function updateProductSelectionControls(rows = getVisibleProducts()) {
  const checkbox = $("#selectAllProducts");
  const button = $("#bulkDeleteProductsBtn");
  if (!checkbox || !button) return;

  const selectable = canManageCatalog() ? rows.map((item) => item.id) : [];
  const selectedVisible = selectable.filter((id) => state.selectedProductIds.has(id));
  checkbox.disabled = selectable.length === 0;
  checkbox.checked = selectable.length > 0 && selectedVisible.length === selectable.length;
  checkbox.indeterminate = selectedVisible.length > 0 && selectedVisible.length < selectable.length;
  button.disabled = state.selectedProductIds.size === 0 || !canManageCatalog();
}

function updateUserSelectionControls() {
  const checkbox = $("#selectAllUsers");
  const button = $("#bulkDeleteUsersBtn");
  if (!checkbox || !button) return;

  const selectable = pageSlice(getVisibleUsers(), state.userPage, 5).rows.filter((item) => item.id !== state.user?.id).map((item) => item.id);
  const selectedVisible = selectable.filter((id) => state.selectedUserIds.has(id));
  checkbox.disabled = selectable.length === 0 || !isAdminMode();
  checkbox.checked = selectable.length > 0 && selectedVisible.length === selectable.length;
  checkbox.indeterminate = selectedVisible.length > 0 && selectedVisible.length < selectable.length;
  button.disabled = state.selectedUserIds.size === 0 || !isAdminMode();
}

function getSaleAvailableQuantity(productId = $("#saleProductSelect").value) {
  const product = state.products.find((item) => item.id === productId);
  if (!product) return 0;
  const inCart = state.saleCart
    .filter((item) => item.productId === productId)
    .reduce((sum, item) => sum + number(item.quantity), 0);
  return Math.max(0, Math.floor(number(product.stock) - inCart));
}

function updateSaleQuantityLimit() {
  const input = $("#saleQuantityInput");
  const button = $("#addSaleLineBtn");
  if (!input) return;

  const available = getSaleAvailableQuantity();
  const product = state.products.find((item) => item.id === $("#saleProductSelect").value);
  if ($("#saleStockInfo")) {
    $("#saleStockInfo").innerHTML = product
      ? `<span>Tá»“n kháº£ dá»¥ng</span><strong>${available}</strong><small>${escapeHtml(productCategoryName(product))}</small>`
      : "Chá»n sáº£n pháº©m Ä‘á»ƒ xem tá»“n kho";
  }
  input.max = String(available);
  input.disabled = available <= 0;
  if (button) button.disabled = available <= 0;
  if (available <= 0) {
    input.value = 0;
    return;
  }
  input.value = Math.min(available, Math.max(1, Math.floor(number(input.value || 1))));
}

function clampSaleQuantity() {
  const input = $("#saleQuantityInput");
  if (!input) return;
  const max = Math.max(0, Math.floor(number(input.max || getSaleAvailableQuantity())));
  if (max <= 0) {
    input.value = 0;
    return;
  }
  input.value = Math.min(max, Math.max(1, Math.floor(number(input.value || 1))));
}

function setPage(page) {
  if (!canAccessPage(page)) {
    showToast("Báº¡n khÃ´ng cÃ³ quyá»n vÃ o má»¥c nÃ y.");
    page = "dashboard";
  }

  state.page = page;
  localStorage.setItem("khopro_page", page);
  $$("#nav .nav-item").forEach((item) => {
    const isActive = item.dataset.page === page;
    item.classList.toggle("active", isActive);
    item.closest("li")?.classList.toggle("active", isActive);
  });
  $$("[data-page-panel]").forEach((panel) => panel.classList.toggle("hidden", panel.dataset.pagePanel !== page));
  if (page === "dashboard") requestAnimationFrame(renderCharts);
  if (page === "sales") {
    requestAnimationFrame(() => setSalesTab(state.salesTab || localStorage.getItem("khopro_sales_tab") || "orders"));
  }
  $("#pageTitle").textContent = $(`#nav .nav-item[data-page="${page}"]`)?.textContent || "Tá»•ng quan";
}

function applySalesTab(tab) {
  const activeTab = tab === "revenue" ? "revenue" : "orders";
  $$("[data-sales-tab]").forEach((button) => {
    button.classList.toggle("active", button.dataset.salesTab === activeTab);
  });
  $$("[data-sales-panel]").forEach((panel) => {
    panel.classList.toggle("hidden", panel.dataset.salesPanel !== activeTab);
  });
  return activeTab;
}

function setSalesTab(tab) {
  const activeTab = applySalesTab(tab);
  state.salesTab = activeTab;
  localStorage.setItem("khopro_sales_tab", activeTab);

  if (activeTab === "revenue") {
    renderRevenueAnalytics();
  }
}

function showHome() {
  showAuth("login");
}

function showAuth(tab = "login") {
  $('[data-view="home"]').classList.add("hidden");
  $('[data-view="auth"]').classList.remove("hidden");
  $('[data-view="app"]').classList.add("hidden");

  const targetTab = tab === "register" ? "register" : "login";
  const tabButton = $(`[data-auth-tab="${targetTab}"]`);
  if (tabButton) tabButton.click();
}

function showApp() {
  if (!canEnterWarehouse(state.user)) {
    clearWarehouseSession();
    redirectToShop();
    return;
  }
  $('[data-view="home"]').classList.add("hidden");
  $('[data-view="auth"]').classList.add("hidden");
  $('[data-view="app"]').classList.remove("hidden");
  startPresenceHeartbeat();
  startInventorySync();
  applyPermissions();
  setPage(state.page);
}

function canEnterWarehouse(user) {
  return ["admin-user", "admin", "sales", "warehouse"]
    .includes(String(user?.role || "").toLowerCase());
}

function clearWarehouseSession() {
  state.token = "";
  state.user = null;
  localStorage.removeItem("khopro_token");
  localStorage.removeItem("khopro_user");
  localStorage.removeItem("khopro_login_role");
  stopPresenceHeartbeat();
  stopReservationTimer();
  stopInventorySync();
}

function redirectToShop() {
  window.location.href = `http://${window.location.hostname || "localhost"}:5173`;
}

function setLoginRole(role) {
  state.loginRole = role === "admin" ? "admin" : "user";
  $$("[data-login-role]").forEach((button) => {
    button.classList.toggle("active", button.dataset.loginRole === state.loginRole);
  });
}

function isAdminMode() {
  return state.loginRole === "admin" && ["admin-user", "Admin"].includes(state.user?.role);
}

function roleName() {
  return String(state.user?.role || "").toLowerCase();
}

function isWarehouseRole() {
  return ["warehouse"].includes(roleName());
}

function isSalesRole() {
  return ["sales"].includes(roleName());
}

function canAccessSales() {
  return isAdminMode() || isSalesRole();
}

function canAccessWarehouse() {
  return isAdminMode() || isWarehouseRole();
}

function canAccessPage(page) {
  if (page === "users") return isAdminMode();
  if (page === "sales") return canAccessSales();
  if (page === "inventory") return canAccessWarehouse();
  return true;
}

function applyPermissions() {
  $$("[data-admin-only]").forEach((item) => {
    item.classList.toggle("hidden", !isAdminMode());
  });

  $$("[data-sales-only]").forEach((item) => {
    item.classList.toggle("hidden", !canAccessSales());
  });

  $$("[data-warehouse-only]").forEach((item) => {
    item.classList.toggle("hidden", !canAccessWarehouse());
  });

  $$("[data-open-modal='productModal']").forEach((item) => {
    item.classList.toggle("hidden", !canManageCatalog());
  });

  $$("[data-open-modal='supplierModal']").forEach((item) => {
    item.classList.toggle("hidden", !canManageCatalog());
  });

  $("#bulkDeleteProductsBtn")?.classList.toggle("hidden", !canManageCatalog());
  $("#selectAllProducts")?.classList.toggle("hidden", !canManageCatalog());
  $("#bulkDeleteUsersBtn")?.classList.toggle("hidden", !isAdminMode());
  $("#selectAllUsers")?.classList.toggle("hidden", !isAdminMode());

  if (!canAccessPage(state.page)) {
    state.page = "dashboard";
  }
}

function canManageInventory() {
  const role = roleName();
  return ["admin-user", "admin", "warehouse"].includes(role);
}

function canReviewInventory() {
  return ["admin-user", "admin"].includes(roleName());
}

function canManageCatalog() {
  const role = roleName();
  return ["admin-user", "admin", "sales", "warehouse"].includes(role);
}

function formData(form) {
  const data = {};
  for (const [key, value] of new FormData(form).entries()) {
    if (value instanceof File) continue;
    data[key] = value;
  }
  return data;
}

async function resolveCategoryId(name) {
  const normalized = normalizeCategoryName(name);
  if (!normalized) return "";

  const existing = state.categories.find((item) => normalizeCategoryName(item.name) === normalized);
  if (existing) return existing.id;

  const created = await request(`${PRODUCT_API}/categories`, {
    method: "POST",
    body: { name: String(name).trim(), parentId: "" }
  });
  state.categories = [created, ...state.categories];
  fillSelects();
  return created.id;
}

function normalizeCategoryName(value) {
  return String(value || "").trim().toLocaleLowerCase("vi-VN");
}

async function applySelectedProductImage(form) {
  const file = form.elements.imageFile?.files?.[0];
  if (!file) return;
  form.elements.image.value = await readImageFile(file);
}

function readImageFile(file) {
  return new Promise((resolve, reject) => {
    const reader = new FileReader();
    reader.onload = () => resolve(String(reader.result || ""));
    reader.onerror = reject;
    reader.readAsDataURL(file);
  });
}

function renderProductImagePreview(src) {
  const preview = $("#productImagePreview");
  if (!preview) return;
  if (!src) {
    preview.className = "image-preview";
    preview.innerHTML = "Chua co anh san pham";
    return;
  }

  preview.className = "image-preview has-image";
  preview.innerHTML = `<img src="${escapeHtml(src)}" alt="Anh san pham">`;
}

function showToast(message) {
  const toast = $("#toast");
  toast.textContent = message;
  toast.classList.remove("hidden");
  clearTimeout(showToast.timer);
  showToast.timer = setTimeout(() => toast.classList.add("hidden"), 3200);
}

function listItem(title, subtitle, aside = "") {
  return `
    <div class="list-item">
      <div>
        <strong>${escapeHtml(title)}</strong>
        <p>${escapeHtml(subtitle || "")}</p>
      </div>
      ${aside}
    </div>
  `;
}

function empty(text) {
  return `<div class="empty">${text}</div>`;
}

function statusBadge(status) {
  const off = status === "Ngung ban";
  const label = off ? "Ngá»«ng bÃ¡n" : "Äang bÃ¡n";
  return `<span class="badge ${off ? "danger" : "ok"}">${label}</span>`;
}

function roleBadge(role) {
  const map = {
    "admin-user": ["ok", "Quáº£n trá»‹ há»‡ thá»‘ng"],
    Admin: ["ok", "Quáº£n trá»‹ há»‡ thá»‘ng"],
    Sales: ["warn", "NhÃ¢n viÃªn bÃ¡n hÃ ng"],
    Warehouse: ["ok", "Thá»§ kho"],
    user: ["warn", "NgÆ°á»i dÃ¹ng"]
  };
  const [style, label] = map[role] || ["warn", role || "NgÆ°á»i dÃ¹ng"];
  return `<span class="badge ${style}">${escapeHtml(label)}</span>`;
}

function roleText(role) {
  const normalizedRole = String(role || "").toLowerCase();
  const map = {
    "admin-user": "Quản trị hệ thống",
    admin: "Quản trị hệ thống",
    sales: "Nhân viên bán hàng",
    warehouse: "Thủ kho",
    user: "Người dùng",
    system: "Hệ thống"
  };
  return map[normalizedRole] || role || "Người dùng";
}

function movementLabel(type) {
  const map = { in: "Nhập", out: "Xuất", set: "Đặt lại" };
  return map[type] || type || "-";
}

function categoryName(id) {
  return state.categories.find((item) => item.id === id)?.name || "-";
}

function productCategoryName(product) {
  return product.categoryName || categoryName(product.categoryId);
}

function money(value) {
  return number(value).toLocaleString("vi-VN", { style: "currency", currency: "VND", maximumFractionDigits: 0 });
}

function number(value) {
  const parsed = Number(value);
  return Number.isFinite(parsed) ? parsed : 0;
}

function dateTime(value) {
  if (!value) return "-";
  return new Date(value).toLocaleString("vi-VN");
}

function initial(text) {
  return escapeHtml((text || "K").trim().slice(0, 1).toUpperCase());
}

function escapeHtml(value) {
  return String(value ?? "")
    .replaceAll("&", "&amp;")
    .replaceAll("<", "&lt;")
    .replaceAll(">", "&gt;")
    .replaceAll('"', "&quot;")
    .replaceAll("'", "&#039;");
}
