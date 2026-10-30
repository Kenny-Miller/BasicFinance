export default {
  extends: ['stylelint-config-standard', 'stylelint-config-recess-order'],
  rules: {
    'no-empty-source': null,
    'at-rule-prelude-no-invalid': null,
    // oxfmt's sortTailwindcss resolves the v4 theme by following bare @import
    // specifiers; the url() form breaks that resolution.
    'import-notation': 'string',
  },
};
