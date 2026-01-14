import { defineConfig } from 'vitepress'

export default defineConfig({
  title: 'Azathrix Framework',
  description: 'Unity 模块化游戏框架',
  lang: 'zh-CN',
  base: '/AzathrixFramework/',

  head: [
    ['link', { rel: 'icon', href: '/favicon.ico' }]
  ],

  themeConfig: {
    logo: '/logo.png',

    nav: [
      { text: '首页', link: '/' },
      { text: '教程', link: '/guide/' },
      { text: 'API', link: '/api/' },
      { text: 'GitHub', link: 'https://github.com/azathrix/AzathrixFramework' }
    ],

    sidebar: {
      '/guide/': [
        {
          text: '入门',
          items: [
            { text: '快速开始', link: '/guide/' },
            { text: '安装', link: '/guide/installation' },
            { text: '创建系统', link: '/guide/systems' }
          ]
        },
        {
          text: '核心功能',
          items: [
            { text: '依赖注入', link: '/guide/injection' },
            { text: '事件系统', link: '/guide/events' },
            { text: '启动管线', link: '/guide/pipeline' },
            { text: '日志系统', link: '/guide/logging' }
          ]
        },
        {
          text: '进阶',
          items: [
            { text: '注册表', link: '/guide/registry' },
            { text: '编辑器工具', link: '/guide/editor' }
          ]
        }
      ],
      '/api/': [
        {
          text: 'API 参考',
          items: [
            { text: '概述', link: '/api/' },
            { text: 'AzathrixFramework', link: '/api/framework' },
            { text: 'EventDispatcher', link: '/api/events' },
            { text: '系统接口', link: '/api/system-interfaces' },
            { text: '属性', link: '/api/attributes' },
            { text: '启动管线', link: '/api/pipeline' }
          ]
        }
      ]
    },

    socialLinks: [
      { icon: 'github', link: 'https://github.com/azathrix/AzathrixFramework' }
    ],

    footer: {
      message: 'Released under the MIT License.',
      copyright: 'Copyright © 2024 Azathrix'
    },

    search: {
      provider: 'local'
    },

    outline: {
      level: [2, 3],
      label: '目录'
    },

    docFooter: {
      prev: '上一页',
      next: '下一页'
    },

    lastUpdated: {
      text: '最后更新'
    }
  }
})
