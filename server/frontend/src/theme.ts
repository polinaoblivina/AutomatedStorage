import type { ThemeConfig } from 'antd'

export const colors = {
  primary: '#0f766e',
  sidebar: '#0f172a',
  page: '#f3f5f8',
  success: '#16a34a',
  warning: '#d97706',
  danger: '#dc2626',
  muted: '#94a3b8',
}

export const theme: ThemeConfig = {
  token: {
    colorPrimary: colors.primary,
    colorLink: colors.primary,
    colorSuccess: colors.success,
    colorWarning: colors.warning,
    colorError: colors.danger,
    colorBgLayout: colors.page,
    borderRadius: 10,
    fontFamily: "Inter, 'Segoe UI', Roboto, 'Helvetica Neue', Arial, sans-serif",
    fontSize: 14,
  },
  components: {
    Layout: {
      siderBg: colors.sidebar,
      headerBg: '#ffffff',
      bodyBg: colors.page,
    },
    Menu: {
      darkItemBg: colors.sidebar,
      darkSubMenuItemBg: colors.sidebar,
      darkItemSelectedBg: colors.primary,
      darkItemColor: '#cbd5e1',
      itemHeight: 44,
      itemMarginInline: 12,
      itemBorderRadius: 8,
    },
    Card: {
      headerFontSize: 15,
    },
    Table: {
      headerBg: '#f8fafc',
      headerColor: '#475569',
      rowHoverBg: '#f1f5f9',
    },
  },
}
