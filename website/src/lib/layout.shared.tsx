import type { BaseLayoutProps } from 'fumadocs-ui/layouts/shared';
import { Logo } from '@/components/logo';
import { i18n } from './i18n';
import { uiTranslations } from 'fumadocs-ui/i18n';
import { appName, gitConfig } from './shared';

export const translations = i18n
  .translations()
  .extend(uiTranslations())
  .add('ui', {
    en: {
      displayName: 'English',
    },
    zh: {
      displayName: '简体中文',
      search: '搜索文档',
      toc: '目录',
      lastUpdate: '上次更新',
      searchNoResult: '没有找到结果',
      previousPage: '上一页',
      nextPage: '下一页',
      chooseLanguage: '选择语言',
    },
  });

export function baseOptions(lang: string): BaseLayoutProps {
  return {
    nav: {
      title: (
        <span className="inline-flex items-center gap-2">
          <Logo className="size-5 text-[#EEA93B]" />
          {appName}
        </span>
      ),
    },
    links: [
      {
        text: lang === 'zh' ? '文档' : 'Documentation',
        url: `/${lang}/docs`,
        active: 'nested-url',
      },
    ],
    githubUrl: `https://github.com/${gitConfig.user}/${gitConfig.repo}`,
    i18n: true,
  };
}
