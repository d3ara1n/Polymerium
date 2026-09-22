import {copyFileSync, cpSync, mkdirSync, readFileSync, writeFileSync} from 'node:fs';

mkdirSync('assets/vendor', {recursive: true});
copyFileSync('node_modules/gsap/dist/gsap.min.js', 'assets/vendor/gsap.min.js');
const assets = JSON.parse(readFileSync('assets.json', 'utf8'));
writeFileSync('assets/manifest.js', `window.PROMO_ASSETS = ${JSON.stringify(assets, null, 2)};\n`);
cpSync('node_modules/@fontsource-variable/noto-sans-sc/files', 'assets/fonts', {recursive: true});
const fonts = readFileSync('node_modules/@fontsource-variable/noto-sans-sc/index.css', 'utf8');
writeFileSync('fonts.css', fonts.replaceAll('./files/', './assets/fonts/'));
