const USER_KEY = 'customerUser'
const CART_KEY = 'customerCart'
const PASSWORDS_KEY = 'customerDemoPasswords'
const DEMO_CUSTOMERS_KEY = 'customerDemoProfiles'

export function loadCustomerUser() {
  try {
    const raw = localStorage.getItem(USER_KEY)
    return raw ? JSON.parse(raw) : null
  } catch {
    return null
  }
}

export function saveCustomerUser(user) {
  if (user) {
    localStorage.setItem(USER_KEY, JSON.stringify(user))
  } else {
    localStorage.removeItem(USER_KEY)
  }
}

export function loadCustomerCart() {
  try {
    const raw = localStorage.getItem(CART_KEY)
    return raw ? JSON.parse(raw) : []
  } catch {
    return []
  }
}

export function saveCustomerCart(cart) {
  localStorage.setItem(CART_KEY, JSON.stringify(cart || []))
}

export function saveDemoPassword(phone, password) {
  const map = JSON.parse(localStorage.getItem(PASSWORDS_KEY) || '{}')
  map[String(phone)] = password
  localStorage.setItem(PASSWORDS_KEY, JSON.stringify(map))
}

export function verifyDemoPassword(phone, password) {
  const map = JSON.parse(localStorage.getItem(PASSWORDS_KEY) || '{}')
  const saved = map[String(phone)]
  if (!saved) return true
  return saved === password
}

export function saveDemoCustomer(customer) {
  if (!customer?.phone) return

  const map = JSON.parse(localStorage.getItem(DEMO_CUSTOMERS_KEY) || '{}')
  map[String(customer.phone)] = customer
  localStorage.setItem(DEMO_CUSTOMERS_KEY, JSON.stringify(map))
}

export function loadDemoCustomer(phone) {
  try {
    const map = JSON.parse(localStorage.getItem(DEMO_CUSTOMERS_KEY) || '{}')
    return map[String(phone)] || null
  } catch {
    return null
  }
}
