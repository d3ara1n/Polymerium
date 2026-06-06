import type { BaseLayoutProps } from 'fumadocs-ui/layouts/shared';
import { appName, gitConfig, navItems } from './shared';

export function baseOptions(): BaseLayoutProps {
  return {
    nav: {
      title: (
        <span className="font-bold text-lg tracking-tight">{appName}</span>
      ),
      transparentMode: 'top',
    },
    links: navItems,
    githubUrl: `https://github.com/${gitConfig.user}/${gitConfig.repo}`,
  };
}
