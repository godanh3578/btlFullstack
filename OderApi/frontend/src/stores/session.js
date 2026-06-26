import { defineStore } from 'pinia'

export const useSessionStore = defineStore('session', {
  state: () => ({
    staffToken: localStorage.getItem('staffToken') || '',
    staffUser: null,
    customerUser: null
  }),
  actions: {
    setStaffToken(token) {
      this.staffToken = token || ''
      if (this.staffToken) {
        localStorage.setItem('staffToken', this.staffToken)
      } else {
        localStorage.removeItem('staffToken')
      }
    },
    setStaffUser(user) {
      this.staffUser = user || null
    },
    setCustomerUser(user) {
      this.customerUser = user || null
    }
  }
})
