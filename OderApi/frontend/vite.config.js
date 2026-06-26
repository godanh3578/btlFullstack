import { fileURLToPath, URL } from 'node:url'
import { defineConfig, loadEnv } from 'vite'
import vue from '@vitejs/plugin-vue'
import vueDevTools from 'vite-plugin-vue-devtools'

export default defineConfig(({ mode }) => {
  const env = loadEnv(mode, process.cwd(), '')
  const apiProxy  = env.VITE_DEV_API_PROXY       || 'http://127.0.0.1:5000'
  const orderProxy = env.VITE_DEV_ORDER_API_PROXY || apiProxy

  const orderApiPaths = [
    '/api/Orders', '/api/Customers', '/api/Debts', '/api/Payments',
    '/api/Returns', '/api/Sales', '/api/SalesInvoices', '/api/Suppliers',
    '/api/WalletTopUps', '/api/AuditLogs', '/api/OutboxMessages', '/api/ProductStockCaches',
  ]

  return {
    plugins: [vue(), vueDevTools()],
    resolve: {
      alias: { '@': fileURLToPath(new URL('./src', import.meta.url)) },
    },
    server: {
      host: '0.0.0.0',
      port: 3000,
      strictPort: true,
      proxy: {
        ...Object.fromEntries(
          orderApiPaths.map(p => [p, { target: orderProxy, changeOrigin: true }])
        ),
        '/api':    { target: apiProxy,   changeOrigin: true },
        '/health': { target: orderProxy, changeOrigin: true },
      },
    },
  }
})
