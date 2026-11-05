// @ts-check
const eslint = require('@eslint/js');
const tseslint = require('typescript-eslint');
const angular = require('angular-eslint');
const sonarjs = require('eslint-plugin-sonarjs');
const unusedImports = require('eslint-plugin-unused-imports');

module.exports = tseslint.config(
  {
    files: ['**/*.ts'],
    extends: [
      eslint.configs.recommended,
      ...tseslint.configs.strict,
      ...tseslint.configs.stylistic,
      ...angular.configs.tsRecommended,
      sonarjs.configs.recommended,
    ],
    plugins: { 'unused-imports': unusedImports },
    processor: angular.processInlineTemplates,
    rules: {
      '@angular-eslint/directive-selector': ['error', { type: 'attribute', prefix: 'cw', style: 'camelCase' }],
      '@angular-eslint/component-selector': ['error', { type: 'element', prefix: 'cw', style: 'kebab-case' }],
      '@angular-eslint/prefer-on-push-component-change-detection': 'error',
      '@angular-eslint/prefer-signals': 'error',
      '@angular-eslint/no-input-rename': 'error',
      '@angular-eslint/prefer-inject': 'error',
      '@typescript-eslint/explicit-member-accessibility': ['error', { accessibility: 'no-public' }],
      '@typescript-eslint/no-explicit-any': 'error',
      // Componentes/serviços podem ser classes vazias: o decorator já dá significado a elas
      '@typescript-eslint/no-extraneous-class': ['error', { allowWithDecorator: true }],
      // unused-imports substitui a regra padrão para conseguir remover imports com --fix
      '@typescript-eslint/no-unused-vars': 'off',
      'unused-imports/no-unused-imports': 'error',
      'unused-imports/no-unused-vars': ['error', { argsIgnorePattern: '^_', varsIgnorePattern: '^_' }],
      // Regra de arquitetura: módulos (features) nunca importam outro módulo nem são importados por camadas inferiores.
      'no-restricted-imports': [
        'error',
        { patterns: [{ group: ['@modules/*'], message: 'Features são carregadas só via lazy loading nas rotas; não importe @modules diretamente.' }] },
      ],
    },
  },
  {
    files: ['**/*.html'],
    extends: [...angular.configs.templateRecommended, ...angular.configs.templateAccessibility],
    rules: {
      '@angular-eslint/template/prefer-control-flow': 'error',
      '@angular-eslint/template/use-track-by-function': 'off',
    },
  },
  {
    files: ['**/*.spec.ts'],
    rules: { 'sonarjs/no-hardcoded-passwords': 'off' },
  },
);
