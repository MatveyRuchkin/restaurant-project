import { ORDER_STATUS_TEXTS, ORDER_STATUSES } from '@/constants'

export const getStatusText = (status) => {
  return ORDER_STATUS_TEXTS[status] || status
}

export const getStatusClass = (status) => {
  return `status-${status.toLowerCase()}`
}

export { ORDER_STATUSES, ORDER_STATUS_TEXTS }

